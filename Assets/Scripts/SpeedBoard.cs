using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoard : MonoBehaviour
{
    [SerializeField] private float speedBoost;
    [SerializeField] private float jumpBoost;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponentInParent<Rigidbody>();
            var force = other.transform.forward * speedBoost;
            //var force = transform.forward * -speedBoost;
            rb.AddForce(force, ForceMode.Acceleration);

            var jumpForce = other.transform.up * jumpBoost;
            var pos = other.GetComponentInParent<CarController>().jumpAnchor.position;
            rb.AddForceAtPosition(jumpForce, pos, ForceMode.Impulse);
        }
    }
}
