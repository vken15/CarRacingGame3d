using Cinemachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Windows;

namespace CarRacingGame3d
{
    public struct InputPayLoad : INetworkSerializable
    {
        public int tick;
        public Vector2 inputVector;
        public DateTime timeStamp;
        //public ulong networkObjectId;
        public Vector3 position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref timeStamp);
            //serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref position);
        }
    }

    public struct StatePayLoad : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        //public ulong networkObjectId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref angularVelocity);
            //serializer.SerializeValue(ref networkObjectId);
        }
    }

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
        private ClientNetworkTransform clientNetworkTransform;

        public Transform jumpAnchor;
        public bool IsGrounded = true;

        //Netcode general
        NetworkTimer networkTimer;
        const float k_serverTickRate = 60.0f; //60 fps
        const int k_bufferSize = 1024;

        //Netcode client specific
        CircularBuffer<InputPayLoad> clientInputBuffer;
        CircularBuffer<StatePayLoad> clientStateBuffer;
        StatePayLoad lastServerState;
        StatePayLoad lastProcessedState;

        //Netcode server specific
        CircularBuffer<StatePayLoad> serverStateBuffer;
        Queue<InputPayLoad> serverInputQueue;

        [Header("Netcode")]
        [SerializeField] private float reconciliationThreshold = 50.0f;
        [SerializeField] private float reconciliationCooldownTime = 1.0f;
        [SerializeField] private float extrapolationLimit = 0.5f;
        [SerializeField] private float extrapolationMulti = 1.2f;
        //For test
        [SerializeField] private GameObject serverCube;
        [SerializeField] private GameObject clientCube;

        StatePayLoad extrapolationState;
        CountdownTimer extrapolationCooldown;

        CountdownTimer reconciliationCooldown;

        void Awake()
        {
            carRb = GetComponent<Rigidbody>();
            clientNetworkTransform = GetComponent<ClientNetworkTransform>();

            carRb.centerOfMass = centerOfMass;
            originalCenterOfMass = centerOfMass;

            var wheel = wheels.FirstOrDefault(w => w.axel == Axel.Rear);
            sidewaysFrictionRear = wheel.wheelCollider.sidewaysFriction;
            forwardFrictionRear = wheel.wheelCollider.forwardFriction;

            networkTimer = new(k_serverTickRate);
            clientInputBuffer = new CircularBuffer<InputPayLoad>(k_bufferSize);
            clientStateBuffer = new CircularBuffer<StatePayLoad>(k_bufferSize);

            serverStateBuffer = new CircularBuffer<StatePayLoad>(k_bufferSize);
            serverInputQueue = new Queue<InputPayLoad>();

            reconciliationCooldown = new(reconciliationCooldownTime);
            extrapolationCooldown = new(extrapolationLimit);
            reconciliationCooldown.OnTimerStart += () =>
            {
                extrapolationCooldown.Stop();
            };
            extrapolationCooldown.OnTimerStart += () => 
            {
                reconciliationCooldown.Stop();
                SwitchAuthorityMode(AuthorityMode.Server); 
            };
            extrapolationCooldown.OnTimerStop += () =>
            {
                extrapolationState = default;
                SwitchAuthorityMode(AuthorityMode.Client);
            };
        }

        public override void OnNetworkSpawn()
        {
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
            reconciliationCooldown.Tick(Time.deltaTime);
            extrapolationCooldown.Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            while (networkTimer.ShouldTick())
            {
                ClientTick();
                ServerTick();
            }
            Extravolate();

            //Nitro();
            //Move(driverInput.Move);
            //Debug.DrawRay(transform.position, carRb.velocity * 3);
            //Debug.DrawRay(transform.position, transform.forward * 10, Color.blue);
        }

        void ServerTick()
        {
            if (!IsServer) return;

            var bufferIndex = -1;
            InputPayLoad inputPayLoad = default;
            while (serverInputQueue.Count > 0)
            {
                inputPayLoad = serverInputQueue.Dequeue();

                bufferIndex = inputPayLoad.tick % k_bufferSize;

                StatePayLoad statePayLoad = ProcessMovement(inputPayLoad);
                serverStateBuffer.Add(statePayLoad, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendToClientRpc(serverStateBuffer.Get(bufferIndex));
            Extrapolation(serverStateBuffer.Get(bufferIndex), CalculateLatencyInMillis(inputPayLoad));
        }

        void Extravolate()
        {
            if (IsServer && extrapolationCooldown.IsRunning)
            {
                transform.position += extrapolationState.position.With(y: 0) * Time.fixedDeltaTime;
            }
        }

        bool ShouldExtrapolate(float latency) => latency < 1.0f && latency > extrapolationLimit;//Time.fixedDeltaTime;

        void Extrapolation(StatePayLoad statePayLoad, float latency)
        {
            if (ShouldExtrapolate(latency))
            {
                float axisLength = latency * statePayLoad.angularVelocity.magnitude * Mathf.Rad2Deg;
                Quaternion angularRotation = Quaternion.AngleAxis(axisLength, statePayLoad.angularVelocity);
                if (extrapolationState.position != default)
                {
                    statePayLoad = extrapolationState;
                }

                var posAdjustment = statePayLoad.velocity * (1 + latency * extrapolationMulti);
                extrapolationState.position = posAdjustment;
                extrapolationState.rotation = angularRotation * statePayLoad.rotation;
                extrapolationState.velocity = statePayLoad.velocity;
                extrapolationState.angularVelocity = statePayLoad.angularVelocity;
                extrapolationCooldown.Start();
                //Debug.Log("extrap Start" + latency + " " + statePayLoad.velocity + " " + posAdjustment);
            }
            else
            {
                extrapolationCooldown.Stop();
            }
        }

        void ClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = networkTimer.CurrentTick;
            var bufferIndex = currentTick % k_bufferSize;

            InputPayLoad inputPayLoad = new()
            {
                tick = currentTick,
                inputVector = driverInput.Move,
                timeStamp = DateTime.Now,
                //networkObjectId = NetworkObjectId,
                position = transform.position
            };

            clientInputBuffer.Add(inputPayLoad, bufferIndex);
            SendToServerRpc(inputPayLoad);

            StatePayLoad statePayLoad = ProcessMovement(inputPayLoad);
            clientStateBuffer.Add(statePayLoad, bufferIndex);

            ServerReconciliation();
        }

        static float CalculateLatencyInMillis(InputPayLoad inputPayLoad)
        {
            return (DateTime.Now - inputPayLoad.timeStamp).Milliseconds / 1000.0f;
        }

        bool ShouldReconcile()
        {
            bool isNewServerState = !lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default) || !lastProcessedState.Equals(lastServerState);

            return isNewServerState && isLastStateUndefinedOrDifferent && !reconciliationCooldown.IsRunning && !extrapolationCooldown.IsRunning;
        }

        void ServerReconciliation()
        {
            if (!ShouldReconcile()) return;

            float postitionError;
            int bufferIndex;

            bufferIndex = lastServerState.tick % k_bufferSize;

            if (bufferIndex - 1 < 0) return;

            StatePayLoad rewindState = IsHost ? serverStateBuffer.Get(bufferIndex - 1) : lastServerState;
            StatePayLoad clientState = IsHost ? clientStateBuffer.Get(bufferIndex - 1) : clientStateBuffer.Get(bufferIndex);
            postitionError = Vector3.Distance(rewindState.position, clientState.position);

            if (postitionError > reconciliationThreshold)
            {
                ReconcileState(rewindState);
                reconciliationCooldown.Start();
            }

            lastProcessedState = rewindState;
        }

        void ReconcileState(StatePayLoad rewindState)
        {
            transform.SetPositionAndRotation(rewindState.position, rewindState.rotation);
            carRb.velocity = rewindState.velocity;
            carRb.angularVelocity = rewindState.angularVelocity;

            if (!rewindState.Equals(lastServerState)) return;

            clientStateBuffer.Add(rewindState, rewindState.tick % k_bufferSize);

            // Replay all inputs from the rewind state to the current state
            int tickToReplay = lastServerState.tick;

            while (tickToReplay < networkTimer.CurrentTick)
            {
                int bufferIndex = tickToReplay % k_bufferSize;
                StatePayLoad statePayLoad = ProcessMovement(clientInputBuffer.Get(bufferIndex));
                clientStateBuffer.Add(statePayLoad, bufferIndex);
                tickToReplay++;
            }
        }

        StatePayLoad ProcessMovement(InputPayLoad input)
        {
            Move(input.inputVector);

            return new()
            {
                tick = input.tick,
                position = transform.position,
                rotation = transform.rotation,
                velocity = carRb.velocity,
                angularVelocity = carRb.angularVelocity,
                //networkObjectId = input.networkObjectId,
            };
        }

        void Move(Vector2 input)
        {
            float motor = maxAcceleration * input.y;
            float steer = maxSteerAngle * input.x;
            //float motor = maxAcceleration * driverInput.Move.y;
            //float steer = maxSteerAngle * driverInput.Move.x;

            Nitro();
            UpdateWheels(motor, steer);
            UpdateBanking(input);

            carVelocity = transform.InverseTransformDirection(carRb.velocity);

            if (IsGrounded)
                GroundedMovement(input);
            else
                AirborneMovement();
        }

        void AirborneMovement()
        {
            //Apply gravity to car while its airborne
            carRb.velocity = Vector3.Lerp(carRb.velocity, carRb.velocity + Vector3.down * gravity, Time.deltaTime * gravity);
        }

        void GroundedMovement(Vector2 input)
        {
            //Turn
            if (Mathf.Abs(input.y) > 0.1f || Mathf.Abs(carVelocity.z) > 1)
            {
                float turnMulti = Mathf.Clamp01(turnCurve.Evaluate(carVelocity.magnitude / maxSpeed));
                carRb.AddTorque((Mathf.Sign(carVelocity.z) * input.x * turnMulti * turnStrength) * Vector3.up);
            }

            //Acceleration
            if (!driverInput.Brake)
            {
                float targetSpeed = input.y < 0.0f 
                    ? input.y * maxSpeed * 0.5f : driverInput.Nitro 
                    ? input.y * maxSpeed * nitroSpeedMultiplier : input.y * maxSpeed;
                Vector3 forwardWithoutY = transform.forward.With(y: 0).normalized;
                carRb.velocity = Vector3.Lerp(carRb.velocity, forwardWithoutY * targetSpeed, networkTimer.MinTimeBetweenTicks);
            }

            //Downforce
            float speedFactor = Mathf.Clamp01(carRb.velocity.magnitude / maxSpeed);
            float lateralG = Mathf.Abs(Vector3.Dot(carRb.velocity, transform.right));
            float downForceFactor = Mathf.Max(speedFactor, lateralG / lateralGScale);
            carRb.AddForce(-transform.up * (downForce * carRb.mass * downForceFactor));

            //Shift center of mass
            float speed = carRb.velocity.magnitude;
            Vector3 centerOfMassAdjustment = (speed > thresholdSpeed)
                ? new Vector3(0.0f, 0.0f, Mathf.Abs(input.y) > 0.1f ? Mathf.Sign(input.y) * centerOfMassOffset : 0.0f)
                : Vector3.zero;
            carRb.centerOfMass = originalCenterOfMass + centerOfMassAdjustment;
        }

        void UpdateBanking(Vector2 input)
        {
            // Bank the car in the opposite direction of the turn
            float targetBankAngle = input.x * -maxBankAngle;
            //float targetBankAngle = input.x * -maxBankAngle;
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
                IsGrounded = true;
            }
        }

        WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction)
        {
            friction.stiffness = driverInput.Brake ? Mathf.SmoothDamp(friction.stiffness, 0.5f, ref driftVelocity, Time.deltaTime * 2.0f) : 1.0f;
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
                if (nitroFuel > 0)
                {
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
                if (driverInput.Brake && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.velocity.magnitude >= 10.0f)
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
        public void SetInput(DriverInput input)
        {
            driverInput = input;
        }

        public bool IsNitro()
        {
            return driverInput.Nitro && !isRechargeNitro && driverInput.Move.y >= 0.0f && GameManager.instance.GetGameState() != GameStates.countdown;
        }

        void SwitchAuthorityMode(AuthorityMode mode)
        {
            clientNetworkTransform.authorityMode = mode;
            bool shouldSync = mode == AuthorityMode.Client;
            clientNetworkTransform.SyncPositionX = shouldSync;
            clientNetworkTransform.SyncPositionY = shouldSync;
            clientNetworkTransform.SyncPositionZ = shouldSync;
        }

        [ServerRpc]
        void SendToServerRpc(InputPayLoad inputPayLoad)
        {
            clientCube.transform.position = inputPayLoad.position.With(y: 4);
            serverInputQueue.Enqueue(inputPayLoad);
        }

        [ClientRpc]
        void SendToClientRpc(StatePayLoad statePayLoad)
        {
            serverCube.transform.position = statePayLoad.position.With(y: 4);
            if (!IsOwner) return;
            lastServerState = statePayLoad;
        }
    }
}