using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public enum Axel
    {
        Front,
        Rear
    }

    [Serializable]
    private struct Wheel
    {
        public GameObject wheelModel;
        public GameObject wheelModel_LOD1;
        public GameObject wheelModel_LOD2;
        public WheelCollider wheelCollider;
        public GameObject wheelEffectObj;
        public ParticleSystem smokeParticle;
        public Axel axel;
    }

    [SerializeField] private float maxAcceleration = 1000.0f;
    [SerializeField] private float brakeAcceleration = 1500.0f;
    [SerializeField] private float nitrusAcceleration = 5000.0f;

    [SerializeField] private float turnSensitivity = 1.0f;
    [SerializeField] private float maxSteerAngle = 30.0f;

    [SerializeField] private float maxSpeed = 30.0f;
    [SerializeField] private float nitrusMulti = 1.2f;

    [SerializeField] private Vector3 _centerOfMass;

    [SerializeField] private List<Wheel> wheels;

    private float accelerationInput;
    private float steerInput;
    private bool nitrusInput;
    private Rigidbody carRb;
    private float velocityVsUp = 0;

    public Transform jumpAnchor;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;
    }

    void Update()
    {
        GetInputs();
        AnimateWheels();
        WheelEffects();
    }

    void FixedUpdate()
    {
        Move();
        Steer();
        Brake();
    }


    /// <summary>
    /// 
    /// </summary>
    void GetInputs()
    {
        accelerationInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        nitrusInput = Input.GetKey(KeyCode.LeftShift);
    }
    
    void Move()
    {
        velocityVsUp = Vector3.Dot(transform.forward, carRb.velocity);
        float maxSpd = maxSpeed;
        if (nitrusInput)
            maxSpd = maxSpeed * nitrusMulti;
        //
        if ((accelerationInput == 0 && !nitrusInput) || ((velocityVsUp > maxSpd || velocityVsUp < -maxSpd * 0.5f)))
            carRb.drag = Mathf.Lerp(carRb.drag, 3.0f, Time.fixedDeltaTime * 3);
        else
            carRb.drag = 0;

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

        if (nitrusInput)
        {
            var force = transform.forward * nitrusAcceleration * 4;
            carRb.AddForce(force, ForceMode.Force);
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
        if (Input.GetKey(KeyCode.Space))
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

    void AnimateWheels()
    {
        foreach (var wheel in wheels)
        {
            wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheel.wheelModel.transform.position = pos;
            wheel.wheelModel.transform.rotation = rot;
            wheel.wheelModel_LOD1.transform.position = pos;
            wheel.wheelModel_LOD1.transform.rotation = rot;
            wheel.wheelModel_LOD2.transform.position = pos;
            wheel.wheelModel_LOD2.transform.rotation = rot;
        }
    }

    void WheelEffects()
    {
        foreach (var wheel in wheels)
        {
            //var dirtParticleMainSettings = wheel.smokeParticle.main;

            if (Input.GetKey(KeyCode.Space) && wheel.axel == Axel.Rear && wheel.wheelCollider.isGrounded == true && carRb.velocity.magnitude >= 10.0f)
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

    public bool IsNitro()
    {
        return nitrusInput;
    }
}
