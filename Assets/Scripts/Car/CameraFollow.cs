using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float moveSmoothness;
    public float rotSmoothness;

    public Vector3 moveOffset;
    public Vector3 rotOffset;

    //public Transform carTarget;
    [SerializeField] private Transform carTarget;

    //void Start()
    //{
    //    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    //    foreach (var player in players)
    //    {
    //        if (player.GetComponent<CarInputHandler>().playerNumber == 1)
    //        {
    //            carTarget = player.GetComponent<Transform>();
    //            break;
    //        }
    //    }
    //}

    void FixedUpdate()
    {
        if (carTarget != null)
            FollowTarget();
    }

    void FollowTarget()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        Vector3 targetPos = carTarget.TransformPoint(moveOffset);

        transform.position = Vector3.Lerp(transform.position, targetPos, moveSmoothness * Time.deltaTime);
    }

    void HandleRotation()
    {
        var direction = carTarget.position - transform.position;
        var rotation = Quaternion.LookRotation(direction + rotOffset, Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotSmoothness * Time.deltaTime);
    }

    public void SetTarget(Transform carTrans)
    {
        carTarget = carTrans;
    }
}
