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
    [SerializeField] private float turnSensitivity = 1.0f;
    [SerializeField] private float maxSteerAngle = 30.0f;
    [SerializeField] private float maxSpeed = 30.0f;

    [Header("Boost")]
    [SerializeField] private float nitroAcceleration = 5000.0f;
    [SerializeField] private float maxNitroFuel = 100.0f;
    [SerializeField] private float nitroFuel = 100.0f;
    [SerializeField] private float nitroSpeedMulti = 1.2f;

    [Header("Others")]
    [SerializeField] private Vector3 _centerOfMass;
    [SerializeField] private List<Wheel> wheels;

    private float accelerationInput;
    private float steerInput;
    private bool nitroInput;

    private float velocityVsUp = 0;
    private bool isRechargeNitro = false;

    private Rigidbody carRb;
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
        DownForce();
        Move();
        Steer();
        Brake();
        Nitro();
    }

    /// <summary>
    /// 
    /// </summary>
    void GetInputs()
    {
        accelerationInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        nitroInput = Input.GetKey(KeyCode.LeftShift);
    }
    
    void Move()
    {
        velocityVsUp = Vector3.Dot(transform.forward, carRb.velocity);
        float maxSpd = maxSpeed;
        if (IsNitro())
            maxSpd = maxSpeed * nitroSpeedMulti;
        //
        if ((accelerationInput == 0 && !IsNitro()) || ((velocityVsUp > maxSpd || velocityVsUp < -maxSpd * 0.5f)))
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
                var force = transform.forward * nitroAcceleration * 4;
                carRb.AddForce(force, ForceMode.Force);
            }
            else isRechargeNitro = true;
        }
    }

    void DownForce()
    {
        carRb.AddForce(-transform.up * 50 * carRb.velocity.magnitude);
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
        return nitroInput && !isRechargeNitro;
    }
}
