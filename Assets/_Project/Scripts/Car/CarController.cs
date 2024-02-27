using System;
using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private float maxAcceleration = 1000.0f;
    [SerializeField] private float brakeAcceleration = 1500.0f;
    [SerializeField] private float turnSensitivity = 1.0f;
    [SerializeField] private float maxSteerAngle = 30.0f;
    [SerializeField] private float maxSpeed = 30.0f;
    [SerializeField] private float frictionMultiplier = 3f;
    [SerializeField] private float driftFactor = 0.93f;
    [SerializeField] private float brakeFactor = 1.0f;

    [Header("Boost")]
    [SerializeField] private float nitroAcceleration = 5000.0f;
    [SerializeField] private float maxNitroFuel = 100.0f;
    [SerializeField] private float nitroFuel = 100.0f;
    [SerializeField] private float nitroSpeedMultiplier = 1.2f;

    [Header("Others")]
    [SerializeField] private Vector3 _centerOfMass;
    [SerializeField] private List<Wheel> wheels;

    private float accelerationInput;
    private float steerInput;
    private bool nitroInput;
    private bool brakeInput;

    private float velocityVsUp = 0;
    private bool isRechargeNitro = false;
    private WheelFrictionCurve sidewaysFrictionFront, sidewaysFrictionRear;

    private Rigidbody carRb;
    public Transform jumpAnchor;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;
    }

    void Update()
    {
        AnimateWheels();
        WheelEffects();
    }

    void FixedUpdate()
    {
        if (GameManager.instance.GetGameState() == GameStates.countdown)
            return;

        DownForce();
        Nitro();
        Move();
        KillOrthogonalVelocity();
        Steer();
        AdjustTraction();
        Brake();
        Debug.DrawRay(transform.position, carRb.velocity * 3);
        Debug.DrawRay(transform.position, transform.forward * 10, Color.blue);
    }

    void Move()
    {
        velocityVsUp = Vector3.Dot(transform.forward, carRb.velocity);
        float maxSpd = maxSpeed;
        if (IsNitro())
            maxSpd = maxSpeed * nitroSpeedMultiplier;
        //
        AdjustDrag(maxSpd);

        //
        if ((velocityVsUp > maxSpd && accelerationInput > 0) ||
            (velocityVsUp < -maxSpd * 0.5f && accelerationInput < 0) ||
            carRb.velocity.sqrMagnitude > maxSpd * maxSpd && accelerationInput > 0)
        {
            return;
        }

        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.motorTorque = accelerationInput * maxAcceleration;
        }
    }

    void Steer()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                var _steerAngle = steerInput * turnSensitivity * maxSteerAngle;
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.6f);
            }
        }
    }

    void Brake()
    {
        if (brakeInput)
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = brakeAcceleration;
            }
        }
        else
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = 0;
            }
        }
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

    void DownForce()
    {
        carRb.AddForce(3.6f * carRb.velocity.magnitude * carRb.velocity.magnitude * -transform.up);
    }

    void KillOrthogonalVelocity()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(carRb.velocity, transform.forward);
        Vector3 rightVelocity = transform.right * Vector3.Dot(carRb.velocity, transform.right);
        Vector3 upVelocity = transform.up * Vector3.Dot(carRb.velocity, transform.up);

        carRb.velocity = forwardVelocity + rightVelocity * driftFactor + upVelocity;
    }

    void AdjustDrag(float maxSpd)
    {
        float upVelocity = Vector3.Dot(carRb.velocity, transform.up);

        if (velocityVsUp > maxSpd || velocityVsUp < -maxSpd * 0.5f || (accelerationInput == 0 && !IsNitro() && Mathf.Round(upVelocity) == 0))
            carRb.drag = Mathf.Lerp(carRb.drag, 3.0f, Time.fixedDeltaTime * 3);
        else if (accelerationInput == 0)
            carRb.drag = 0.05f;
        else
            carRb.drag = 0.02f;
    }

    void AdjustTraction()
    {
        if (brakeInput)
        {
            float velocity = 0;
            sidewaysFrictionFront = wheels[0].wheelCollider.sidewaysFriction;
            sidewaysFrictionRear = wheels[3].wheelCollider.sidewaysFriction;
            sidewaysFrictionFront.extremumValue = sidewaysFrictionFront.asymptoteValue = 1.5f;
            sidewaysFrictionRear.extremumValue = sidewaysFrictionRear.asymptoteValue = Mathf.SmoothDamp(sidewaysFrictionRear.asymptoteValue, brakeFactor, ref velocity, 0.05f * Time.deltaTime);
            foreach (var wheel in wheels)
            {
                if (wheel.axel == Axel.Front)
                    wheel.wheelCollider.sidewaysFriction = sidewaysFrictionFront;
                if (wheel.axel == Axel.Rear)
                    wheel.wheelCollider.sidewaysFriction = sidewaysFrictionRear;
            }
        } else
        {
            float velocity = Vector3.Dot(carRb.velocity, transform.forward);
            sidewaysFrictionFront = wheels[0].wheelCollider.sidewaysFriction;
            sidewaysFrictionRear = wheels[3].wheelCollider.sidewaysFriction;
            sidewaysFrictionFront.extremumValue = sidewaysFrictionFront.asymptoteValue = ((velocity * frictionMultiplier) / 300) + 1;
            sidewaysFrictionRear.extremumValue = sidewaysFrictionRear.asymptoteValue = ((velocity * frictionMultiplier) / 300) + 1;
            foreach (var wheel in wheels)
            {
                if (wheel.axel == Axel.Front)
                    wheel.wheelCollider.sidewaysFriction = sidewaysFrictionFront;
                if (wheel.axel == Axel.Rear)
                    wheel.wheelCollider.sidewaysFriction = sidewaysFrictionRear;
            }
        }
    }
    
    void AnimateWheels()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            foreach (var wheelModel in wheel.wheelModels)
            {
                wheelModel.transform.SetPositionAndRotation(pos, rot);
            }
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
