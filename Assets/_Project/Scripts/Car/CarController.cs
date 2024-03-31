using Cinemachine;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class CarController : NetworkBehaviour
    {
        [Serializable]
        private struct Wheel
        {
            public List<GameObject> wheelModels;
            public WheelCollider wheelCollider;
            public GameObject wheelEffectObj;
            public ParticleSystem smokeParticle;
            public Axel axel;
        }

        [Header("Acceleration")]
        [SerializeField] private float maxAcceleration = 3000.0f;
        [SerializeField] private float maxSpeed = 50.0f;

        [Header("Steering")]
        [SerializeField] private float maxSteerAngle = 30.0f;
        [SerializeField] private AnimationCurve turnCurve;
        [SerializeField] private float turnStrength = 1500.0f;

        [Header("Braking and Drifting")]
        [SerializeField] private float brakeAcceleration = 10000.0f;
        [SerializeField] private float driftSteerMulti = 1.5f;
        [SerializeField] private float driftStiffness = 0.5f;

        [Header("Boost")]
        [SerializeField] private float nitroAcceleration = 5000.0f;
        [SerializeField] private float maxNitroFuel = 100.0f;
        [SerializeField] private float nitroFuel = 100.0f;
        [SerializeField] private float nitroSpeedMultiplier = 1.2f;

        [Header("Physics")]
        [SerializeField] private Vector3 centerOfMass;
        [SerializeField] private float downForce = 100.0f;
        [SerializeField] private float lateralGScale = 10.0f;
        [SerializeField] private float gravity = Physics.gravity.y;

        [Header("Banking")]
        [SerializeField] private float maxBankAngle = 5.0f;
        [SerializeField] private float bankSpeed = 2.0f;

        [Header("Wheels")]
        [SerializeField] private DriveStyle style = DriveStyle.RWD;
        [SerializeField] private List<Wheel> wheels;

        [Header("Refs")]
        [SerializeField] private CinemachineVirtualCamera playerCamera;
        [SerializeField] private AudioListener playerAudioListener;

        const float thresholdSpeed = 10.0f;
        const float centerOfMassOffset = -0.25f;
        private Vector3 originalCenterOfMass;
        private WheelFrictionCurve sidewaysFrictionRear, forwardFrictionRear;

        private Vector3 carVelocity;
        private float brakeVelocity;
        private float driftVelocity;

        private bool isRechargeNitro = false;

        private DriverInput driverInput;
        private Rigidbody carRb;

        const float serverTickRate = 60.0f;

        public Transform jumpAnchor;
        public bool Immunity;
        NetworkTimer networkTimer;

        void Awake()
        {
            carRb = GetComponent<Rigidbody>();

            carRb.centerOfMass = centerOfMass;
            originalCenterOfMass = centerOfMass;

            var wheel = wheels.FirstOrDefault(w => w.axel == Axel.Rear);
            sidewaysFrictionRear = wheel.wheelCollider.sidewaysFriction;
            forwardFrictionRear = wheel.wheelCollider.forwardFriction;
            networkTimer = new(serverTickRate);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
            {
                playerCamera.Priority = -1;
                playerAudioListener.enabled = false;
                return;
            }

            playerCamera.Priority = 5;
            playerAudioListener.enabled = true;
        }

        void Update()
        {
            WheelEffects();
            networkTimer.Update(Time.deltaTime);
        }

        void FixedUpdate()
        {
            Nitro();
            Move();
        }

        void Move()
        {
            float motor = maxAcceleration * driverInput.Move.y;
            float steer = maxSteerAngle * driverInput.Move.x;

            Nitro();
            UpdateWheels(motor, steer);
            UpdateBanking();

            carVelocity = transform.InverseTransformDirection(carRb.velocity);

            if (IsGrounded())
                GroundedMovement();
            else
                AirborneMovement();
        }

        void AirborneMovement()
        {
            //Apply gravity to car while its airborne
            carRb.velocity = Vector3.Lerp(carRb.velocity, carRb.velocity + Vector3.down * gravity, Time.deltaTime * gravity);
        }

        void GroundedMovement()
        {
            //Turn
            if (Mathf.Abs(driverInput.Move.y) > 0.1f || Mathf.Abs(carVelocity.z) > 1)
            {
                float turnMulti = IsNitro() ? Mathf.Clamp01(turnCurve.Evaluate(carVelocity.magnitude / (maxSpeed * nitroSpeedMultiplier))) : Mathf.Clamp01(turnCurve.Evaluate(carVelocity.magnitude / maxSpeed));
                carRb.AddTorque(Mathf.Sign(carVelocity.z) * driverInput.Move.x * turnMulti * turnStrength * Vector3.up);
            }

            //Acceleration
            if (!driverInput.Brake)
            {
                float targetSpeed = driverInput.Move.y < 0.0f 
                    ? driverInput.Move.y * maxSpeed * 0.5f : IsNitro()
                    ? driverInput.Move.y * maxSpeed * nitroSpeedMultiplier : driverInput.Move.y * maxSpeed;
                Vector3 forwardWithoutY = transform.forward.With(y: 0).normalized;
                carRb.velocity = Vector3.Lerp(carRb.velocity, forwardWithoutY * targetSpeed, networkTimer.MinTimeBetweenTicks);
                //carRb.MovePosition(transform.position + Vector3.Lerp(carRb.velocity, forwardWithoutY * targetSpeed, networkTimer.MinTimeBetweenTicks));
            }

            //Downforce
            float speedFactor = Mathf.Clamp01(carRb.velocity.magnitude / maxSpeed);
            float lateralG = Mathf.Abs(Vector3.Dot(carRb.velocity, transform.right));
            float downForceFactor = Mathf.Max(speedFactor, lateralG / lateralGScale);
            carRb.AddForce(-transform.up * (downForce * carRb.mass * downForceFactor));

            //Shift center of mass
            float speed = carRb.velocity.magnitude;
            Vector3 centerOfMassAdjustment = (speed > thresholdSpeed)
                ? new Vector3(0.0f, 0.0f, Mathf.Abs(driverInput.Move.y) > 0.1f ? Mathf.Sign(driverInput.Move.y) * centerOfMassOffset : 0.0f)
                : Vector3.zero;
            carRb.centerOfMass = originalCenterOfMass + centerOfMassAdjustment;
        }

        void UpdateBanking()
        {
            // Bank the car in the opposite direction of the turn
            float targetBankAngle = driverInput.Move.x * -maxBankAngle;
            Vector3 currentEuler = transform.localEulerAngles;
            currentEuler.z = Mathf.LerpAngle(currentEuler.z, targetBankAngle, Time.deltaTime * bankSpeed);
            transform.localEulerAngles = currentEuler;
        }

        void UpdateWheels(float motor, float steer)
        {
            foreach (var wheel in wheels)
            {
                //Acceleration
                if (wheel.axel == Axel.Rear && style == DriveStyle.RWD)
                    wheel.wheelCollider.motorTorque = motor;

                //Steer
                if (wheel.axel == Axel.Front)
                {
                    float steerMulti = driverInput.Brake ? driftSteerMulti : 1.0f;
                    wheel.wheelCollider.steerAngle = steer * steerMulti;
                    //wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, steerMulti, 0.6f);
                }

                //Brake and drift
                if (wheel.axel == Axel.Rear && style == DriveStyle.RWD)
                    BrakeAndDrift(wheel.wheelCollider);

                //Animate wheel
                wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
                foreach (var wheelModel in wheel.wheelModels)
                {
                    wheelModel.transform.SetPositionAndRotation(pos, rot);
                }
            }
        }

        void BrakeAndDrift(WheelCollider wheel)
        {
            if (driverInput.Brake)
            {
                carRb.constraints = RigidbodyConstraints.FreezeRotationX;

                float newZ = Mathf.SmoothDamp(carRb.velocity.z, 0, ref brakeVelocity, 1.0f);
                carRb.velocity = carRb.velocity.With(z: newZ);
                wheel.brakeTorque = brakeAcceleration;
                ApplyDriftFriction(wheel);
            }
            else
            {
                carRb.constraints = RigidbodyConstraints.None;

                wheel.brakeTorque = 0;
                ResetDriftFriction(wheel);
            }
        }

        void ResetDriftFriction(WheelCollider wheel)
        {
            wheel.forwardFriction = forwardFrictionRear;
            wheel.sidewaysFriction = sidewaysFrictionRear;
        }

        void ApplyDriftFriction(WheelCollider wheel)
        {
            if (wheel.GetGroundHit(out var _))
            {
                wheel.forwardFriction = UpdateFriction(wheel.forwardFriction);
                wheel.sidewaysFriction = UpdateFriction(wheel.sidewaysFriction);
            }
        }

        WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction)
        {
            friction.stiffness = Mathf.SmoothDamp(friction.stiffness, driftStiffness, ref driftVelocity, Time.deltaTime * 2.0f);
            return friction;
        }

        void WheelEffects()
        {
            foreach (var wheel in wheels)
            {
                if (driverInput.Brake && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && (carRb.velocity.magnitude >= 10.0f))
                {
                    wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = true;
                    wheel.smokeParticle.Emit(1);
                }
                else
                {
                    wheel.wheelEffectObj.GetComponentInChildren<TrailRenderer>().emitting = false;
                }
            }
        }

        private bool IsGrounded()
        {
            if (!Physics.Raycast(transform.position, -transform.up, 5))
            {
                return false;
            }
            return true;
        }

        void Nitro()
        {
            if (isRechargeNitro)
                nitroFuel += Time.deltaTime / 2;

            if (nitroFuel >= maxNitroFuel)
                isRechargeNitro = false;

            if (IsNitro())
            {
                nitroFuel -= (nitroFuel <= 0) ? 0 : Time.deltaTime * 4;
                if (nitroFuel > 0)
                {
                    var force = 4 * nitroAcceleration * transform.forward;
                    carRb.AddForce(force, ForceMode.Force);
                }
                else isRechargeNitro = true;
            }
        }

        public bool IsNitro()
        {
            return driverInput.Nitro && !isRechargeNitro && driverInput.Move.y >= 0.0f && GameManager.instance.GetGameState() != GameStates.Countdown;
        }

        public float GetNitroFuelPercent()
        {
            return nitroFuel / maxNitroFuel;
        }

        public void ReFillFuel(float amount)
        {
            nitroFuel = Mathf.Clamp(nitroFuel + maxNitroFuel * amount, nitroFuel, maxNitroFuel);
        }

        private float GetLateralVelocity()
        {
            return Vector3.Dot(transform.right, carRb.velocity);
        }

        public float GetVelocityMagnitude()
        {
            return carRb.velocity.magnitude;
        }

        public bool IsTireScreeching(out float lateralVelocity, out bool isBraking)
        {
            lateralVelocity = GetLateralVelocity();
            isBraking = false;
            if (!IsGrounded())
            {
                return false;
            }

            if (driverInput.Move.y < 0 && Vector2.Dot(transform.forward, carRb.velocity) > 0)
            {
                isBraking = true;
                return true;
            }

            if (Mathf.Abs(GetLateralVelocity()) > 4.0f)
                return true;

            return false;
        }

        public void SetInput(DriverInput input)
        {
            driverInput = input;
        }
    }
}