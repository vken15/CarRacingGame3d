using Cinemachine;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public struct InputPayLoad : INetworkSerializable
    {
        public int tick;
        public Vector2 inputVector;
        public bool brake;
        public bool nitro;
        public DateTime timeStamp;
        //public Vector3 position; //test only

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref inputVector);
            serializer.SerializeValue(ref brake);
            serializer.SerializeValue(ref nitro);
            serializer.SerializeValue(ref timeStamp);
            //serializer.SerializeValue(ref position);
        }
    }

    public struct StatePayLoad : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref angularVelocity);
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
        [SerializeField] private float driftStiffness = 0.5f;

        [Header("Boost")]
        [SerializeField] private float nitroAcceleration = 5000.0f;
        [SerializeField] private float maxNitroFuel = 100.0f;
        [SerializeField] private float nitroFuel = 100.0f;
        [SerializeField] private float nitroDuration = 5.0f;
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

        private bool isActiveNitro = false;
        private float currentNitroDuration = 0f;

        private DriverInput driverInput;
        private Rigidbody carRb;
        private ClientNetworkTransform clientNetworkTransform;

        //Netcode general
        public Transform jumpAnchor;
        public bool immunity;
        NetworkTimer networkTimer;
        const float serverTickRate = 60.0f; //60 fps
        const int bufferSize = 1024;

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
        //[SerializeField] private GameObject serverCube;
        //[SerializeField] private GameObject clientCube;

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

            networkTimer = new(serverTickRate);
            clientInputBuffer = new CircularBuffer<InputPayLoad>(bufferSize);
            clientStateBuffer = new CircularBuffer<StatePayLoad>(bufferSize);

            serverStateBuffer = new CircularBuffer<StatePayLoad>(bufferSize);
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

        private void Start()
        {
            if (GetComponent<CarAIHandler>().enabled)
            {
                playerCamera.Priority = -1;
                playerAudioListener.enabled = false;
            }
        }

        void Update()
        {
            WheelEffects();

            networkTimer.Update(Time.deltaTime);
            reconciliationCooldown.Tick(Time.deltaTime);
            extrapolationCooldown.Tick(Time.deltaTime);

            //Extravolate();
        }

        void FixedUpdate()
        {
            while (networkTimer.ShouldTick())
            {
                ClientTick();
                ServerTick();
                Extravolate();
            }

            if (GameManager.instance.networkStatus == NetworkStatus.offline)
            {
                Nitro();
                Move();
            }
        }

        void ServerTick()
        {
            if (!IsServer || (IsHost && IsOwner)) return;

            var bufferIndex = -1;
            InputPayLoad inputPayLoad = default;
            while (serverInputQueue.Count > 0)
            {
                inputPayLoad = serverInputQueue.Dequeue();

                bufferIndex = inputPayLoad.tick % bufferSize;

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
                //carRb.MovePosition(extrapolationState.position.With(y: 0) * Time.fixedDeltaTime);
            }
        }

        bool ShouldExtrapolate(float latency) => latency < extrapolationLimit && latency > Time.fixedDeltaTime;

        void Extrapolation(StatePayLoad statePayLoad, float latency)
        {
            if (ShouldExtrapolate(latency))
            {
                float axisLength = latency * statePayLoad.angularVelocity.magnitude * Mathf.Rad2Deg;
                Quaternion angularRotation = Quaternion.AngleAxis(axisLength, statePayLoad.angularVelocity);
                /*
                if (extrapolationState.position != default)
                {
                    statePayLoad = extrapolationState;
                }
                */
                extrapolationState.position = extrapolationMulti * latency * statePayLoad.velocity;
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

        static float CalculateLatencyInMillis(InputPayLoad inputPayLoad)
        {
            return (DateTime.Now - inputPayLoad.timeStamp).Milliseconds / 1000.0f;
        }

        void ClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = networkTimer.CurrentTick;
            var bufferIndex = currentTick % bufferSize;

            InputPayLoad inputPayLoad = new()
            {
                tick = currentTick,
                inputVector = driverInput.Move,
                brake = driverInput.Brake,
                nitro = driverInput.Nitro,
                timeStamp = DateTime.Now,
                //position = transform.position
            };

            clientInputBuffer.Add(inputPayLoad, bufferIndex);
            SendToServerRpc(inputPayLoad);

            StatePayLoad statePayLoad = ProcessMovement(inputPayLoad);
            clientStateBuffer.Add(statePayLoad, bufferIndex);

            ServerReconciliation();
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

            bufferIndex = lastServerState.tick % bufferSize;

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

            clientStateBuffer.Add(rewindState, rewindState.tick % bufferSize);

            // Replay all inputs from the rewind state to the current state
            int tickToReplay = lastServerState.tick;

            while (tickToReplay < networkTimer.CurrentTick)
            {
                int bufferIndex = tickToReplay % bufferSize;
                StatePayLoad statePayLoad = ProcessMovement(clientInputBuffer.Get(bufferIndex));
                clientStateBuffer.Add(statePayLoad, bufferIndex);
                tickToReplay++;
            }
        }

        StatePayLoad ProcessMovement(InputPayLoad input)
        {
            driverInput = new DriverInput
            {
                Move = input.inputVector,
                Brake = input.brake,
                Nitro = input.nitro,
            };

            Nitro();
            Move();

            return new()
            {
                tick = input.tick,
                position = transform.position,
                rotation = transform.rotation,
                velocity = carRb.velocity,
                angularVelocity = carRb.angularVelocity,
            };
        }

        void Move()
        {
            //float motor = maxAcceleration * input.y;
            //float steer = maxSteerAngle * input.x;
            float motor = maxAcceleration * driverInput.Move.y;
            float steer = maxSteerAngle * driverInput.Move.x;

            UpdateWheels(motor, steer);
            UpdateBanking();

            carVelocity = transform.InverseTransformDirection(carRb.velocity);

            Debug.DrawRay(transform.position, carVelocity * 10, Color.green);
            Debug.DrawRay(transform.position, carRb.velocity * 10, Color.blue);

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
                if (driverInput.Brake && 
                    (wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && (carRb.velocity.magnitude >= 10.0f) ||
                    (!IsOwner && GameManager.instance.networkStatus == NetworkStatus.online)))
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
            if (nitroFuel < maxNitroFuel && !isActiveNitro)
                nitroFuel += Time.deltaTime / 2;

            if (!isActiveNitro && nitroFuel > maxNitroFuel * 0.33f && driverInput.Nitro && GameManager.instance.GetGameState() != GameStates.Countdown)
            {
                isActiveNitro = true;
                nitroFuel -= maxNitroFuel * 0.33f;
                currentNitroDuration = nitroDuration;
            }

            if (IsNitro())
            {
                currentNitroDuration -= networkTimer.MinTimeBetweenTicks;
                if (currentNitroDuration > 0)
                {
                    var force = 4 * nitroAcceleration * transform.forward;
                    carRb.AddForce(force, ForceMode.Force);
                }
                else isActiveNitro = false;
            }
        }

        public bool IsNitro()
        {
            return isActiveNitro;
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

            if (driverInput.Brake && Vector2.Dot(transform.forward, carRb.velocity) > 0)
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
            //clientCube.transform.position = inputPayLoad.position.With(y: 4 + transform.position.y);
            serverInputQueue.Enqueue(inputPayLoad);
        }

        [ClientRpc]
        void SendToClientRpc(StatePayLoad statePayLoad)
        {
            //serverCube.transform.position = statePayLoad.position.With(y: 4 + transform.position.y);
            if (!IsOwner) return;
            lastServerState = statePayLoad;
        }
    }
}