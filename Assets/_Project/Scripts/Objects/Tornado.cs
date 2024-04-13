using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class Tornado : MonoBehaviour
    {
        [SerializeField] AnimationCurve pullForceCurve;
        [SerializeField] AnimationCurve pullingCenterCurve;
        [SerializeField] Transform pullingCenter;
        [SerializeField] float refreshRate = 0.2f;
        [SerializeField] float pullForce = 0;
        [SerializeField] float movementSpeed = 50;
        [SerializeField] new ParticleSystem particleSystem;

        public Vector3 moveDir = Vector3.zero;
        public int owner;
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (moveDir != Vector3.zero)
            {
                rb.AddForce(movementSpeed * moveDir, ForceMode.Acceleration);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("CarBody") && other.GetComponentInParent<CarInputHandler>().playerNumber != owner)
            {
                StartCoroutine(PullObject(other, true));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("CarBody"))
            {
                StartCoroutine(PullObject(other, false));
            }
        }

        IEnumerator PullObject(Collider other, bool shouldPull)
        {
            if (shouldPull)
            {
                float force = pullForce * pullForceCurve.Evaluate((Time.time * 0.1f) % pullForceCurve.length);
                Vector3 forceDirection = pullingCenter.position - other.transform.position;
                
                if (other.GetComponentInParent<CarController>().immunity == false)
                    other.GetComponentInParent<Rigidbody>().AddForce(force * Time.deltaTime * forceDirection.normalized, ForceMode.Acceleration);
                
                pullingCenter.position = new Vector3(pullingCenter.position.x, 6 + transform.position.y + 4 * pullingCenterCurve.Evaluate((Time.time * 0.1f) % pullingCenterCurve.length), pullingCenter.position.z);
                yield return refreshRate;
                StartCoroutine(PullObject(other, shouldPull));
            } else
            {
                StopAllCoroutines();
            }
        }
    }
}
