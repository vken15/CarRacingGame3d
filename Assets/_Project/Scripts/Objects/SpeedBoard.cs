using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class SpeedBoard : MonoBehaviour
    {
        [SerializeField] private float speedBoost;
        [SerializeField] private float jumpBoost;
        [SerializeField] private bool speedUp = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("CarBody"))
            {
                Rigidbody rb = other.GetComponentInParent<Rigidbody>();
                var force = speedUp ? transform.forward * -speedBoost : transform.forward * speedBoost;
                rb.AddForce(force, ForceMode.Acceleration);
                rb.AddForceAtPosition(other.transform.up * jumpBoost, other.GetComponentInParent<CarController>().jumpAnchor.position, ForceMode.Impulse);
            }
        }
    }
}