using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPointNode : MonoBehaviour
{
    [Header("Waypoint")]
    public float minDistanceToReachWayPoint = 5;
    public WayPointNode[] nextWayPointNode;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, minDistanceToReachWayPoint);
    }
}