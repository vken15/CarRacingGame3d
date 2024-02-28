using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
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

    const float thresholdSpeed = 10.0f;
    const float centerOfMassOffset = -0.25f;

    private float accelerationInput;
    private float steerInput;
    private bool nitroInput;
    private bool brakeInput;

    private Vector3 carVelocity;
    private float brakeVelocity;
    private float driftVelocity;
    private Vector3 originalCenterOfMass;
    private bool isRechargeNitro = false;
    private WheelFrictionCurve sidewaysFrictionRear, forwardFrictionRear;
    private Rigidbody carRb;

    public Transform jumpAnchor;
    public bool IsGrounded = true;

    void Awake()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = centerOfMass;
        originalCenterOfMass = centerOfMass;

        var wheel = wheels.FirstOrDefault(w => w.axel == Axel.Rear);
        sidewaysFrictionRear = wheel.wheelCollider.sidewaysFriction;
        forwardFrictionRear = wheel.wheelCollider.forwardFriction;
    }

    void Update()
    {
        WheelEffects();
    }

    void FixedUpdate()
    {
        if (GameManager.instance.GetGameState() == GameStates.countdown)
            return;

        Nitro();
        Move();
        //Debug.DrawRay(transform.position, carRb.velocity * 3);
        //Debug.DrawRay(transform.position, transform.forward * 10, Color.blue);
    }

    void Move()
    {
        float motor = maxAcceleration * accelerationInput;
        float steer = maxSteerAngle * steerInput;

        UpdateWheels(motor, steer);
        UpdateBanking();

        carVelocity = transform.InverseTransformDirection(carRb.velocity);

        if (IsGrounded)
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
        if (Mathf.Abs(accelerationInput) > 0.1f || Mathf.Abs(carVelocity.z) > 1)
        {
            float turnMulti = Mathf.Clamp01(turnCurve.Evaluate(carVelocity.magnitude / maxSpeed));
            carRb.AddTorque((Mathf.Sign(carVelocity.z) * steerInput * turnMulti * turnStrength) * Vector3.up);
        }

        //Acceleration
        if (!brakeInput)
        {
            float targetSpeed = nitroInput ? accelerationInput * maxSpeed * nitroSpeedMultiplier : accelerationInput * maxSpeed;
            Vector3 forwardWithoutY = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            carRb.velocity = Vector3.Lerp(carRb.velocity, forwardWithoutY * targetSpeed, Time.deltaTime);
        }

        //Downforce
        float speedFactor = Mathf.Clamp01(carRb.velocity.magnitude / maxSpeed);
        float lateralG = Mathf.Abs(Vector3.Dot(carRb.velocity, transform.right));
        float downForceFactor = Mathf.Max(speedFactor, lateralG / lateralGScale);
        carRb.AddForce(-transform.up * (downForce * carRb.mass * downForceFactor));

        //Shift center of mass
        float speed = carRb.velocity.magnitude;
        Vector3 centerOfMassAdjustment = (speed > thresholdSpeed)
            ? new Vector3(0.0f, 0.0f, Mathf.Abs(accelerationInput) > 0.1f ? Mathf.Sign(accelerationInput) * centerOfMassOffset : 0.0f)
            : Vector3.zero;
        carRb.centerOfMass = originalCenterOfMass + centerOfMassAdjustment;
    }

    void UpdateBanking()
    {
        // Bank the car in the opposite direction of the turn
        float targetBankAngle = steerInput * -maxBankAngle;
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
                float steerMulti = brakeInput ? driftSteerMulti : 1.0f;
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
        if (brakeInput)
        {
            carRb.constraints = RigidbodyConstraints.FreezeRotationX;

            float newZ = Mathf.SmoothDamp(carRb.velocity.z, 0, ref brakeVelocity, 1.0f);
            carRb.velocity = new Vector3(carRb.velocity.x, carRb.velocity.y, newZ);
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
            IsGrounded = true;
        }
    }

    WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction)
    {
        friction.stiffness = brakeInput ? Mathf.SmoothDamp(friction.stiffness, 0.5f, ref driftVelocity, Time.deltaTime * 2.0f) : 1.0f;
        return friction;
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
            if (nitroFuel > 0) {
                var force = 4 * nitroAcceleration * transform.forward;
                carRb.AddForce(force, ForceMode.Force);
            }
            else isRechargeNitro = true;
        }
    }

    void WheelEffects()
    {
        foreach (var wheel in wheels)
        {
            if (brakeInput && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.velocity.magnitude >= 10.0f)
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

    /// <summary>
    /// 
    /// </summary>
    public void SetInput(Vector2 inputVector, bool nitro, bool brake)
    {
        accelerationInput = inputVector.y;
        steerInput = inputVector.x;
        nitroInput = nitro;
        brakeInput = brake;
    }

    public bool IsNitro()
    {
        return nitroInput && !isRechargeNitro && GameManager.instance.GetGameState() != GameStates.countdown;
    }
}
