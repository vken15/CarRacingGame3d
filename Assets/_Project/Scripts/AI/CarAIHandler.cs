using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarRacingGame3d
{
    public class CarAIHandler : MonoBehaviour
    {
        [SerializeField] private AIDifficult aiDifficult;
        [SerializeField] private float driveSpeed = 45;
        public bool isAvoidingCars = true;
        [SerializeField] private float track = 20.0f;

        [SerializeField] private Vector3 sensorPosition;
        [SerializeField] private float sideSensorPosition = 0.3f;
        [SerializeField] private float sensorAngle = 30.0f;
        [SerializeField] private float sensorLength = 5.0f;

        private Vector3 targetPosition = Vector3.zero;
        private Transform targetTransform = null;

        //Avoidance
        private Vector3 avoidanceVectorLerped = Vector3.zero;
        private bool avoidToLeft = false;

        private List<Vector3> temporaryWaypoints = new();
        private float angleToTarget = 0;
        private int stuckCheckCounter = 0;
        private WayPointNode currentWayPoint = null;
        private WayPointNode previousWayPoint = null;
        private WayPointNode nextWayPoint = null;
        private WayPointNode[] allWayPoints;

        private CarController carController;

        void Awake()
        {
            carController = GetComponent<CarController>();
            allWayPoints = FindObjectsByType<WayPointNode>(FindObjectsSortMode.None);
        }

        void FixedUpdate()
        {
            Avoidance();
            if (GameManager.instance.GetGameState() == GameStates.countdown)
            {
                return;
            }

            Vector2 inputVector = Vector2.zero;

            if (temporaryWaypoints.Count == 0)
            {
                FollowWayPoints();
            }
            //else FollowTemporaryWayPoints();

            inputVector.x = TurnTowardTarget();
            inputVector.y = ApplyThrottleOrBrake(inputVector.x);

            carController.SetInput(inputVector, false, false);
        }

        private void FollowWayPoints()
        {
            if (currentWayPoint == null)
            {
                currentWayPoint = FindClosestWayPoint();
                previousWayPoint = currentWayPoint;
                nextWayPoint = currentWayPoint.nextWayPointNode[Random.Range(0, currentWayPoint.nextWayPointNode.Length)];
            }
            else
            {
                targetPosition = currentWayPoint.transform.position;
                float distanceToWayPoint = (targetPosition - transform.position).magnitude;


                if (distanceToWayPoint > track)
                {
                    Vector3 nearestPointOnTheWayPointLine = FindNearestPointOnLine(previousWayPoint.transform.position, currentWayPoint.transform.position, transform.position);
                    float segments = distanceToWayPoint / track;
                    targetPosition = (targetPosition + nearestPointOnTheWayPointLine * segments) / (segments + 1);
                    Debug.DrawLine(transform.position, targetPosition, Color.black);
                }

                //float currentMinDistanceToReachWayPoint = GetMinDistanceToReachWayPoint();
                if (distanceToWayPoint <= currentWayPoint.minDistanceToReachWayPoint)
                {
                    previousWayPoint = currentWayPoint;
                    currentWayPoint = nextWayPoint;
                    nextWayPoint = nextWayPoint.nextWayPointNode[Random.Range(0, currentWayPoint.nextWayPointNode.Length)];
                }
            }
        }

        private WayPointNode FindClosestWayPoint()
        {
            return allWayPoints.OrderBy(w => Vector3.Distance(transform.position, w.transform.position)).FirstOrDefault();
        }

        private Vector3 FindNearestPointOnLine(Vector3 lineStartPosition, Vector3 lineEndPosition, Vector3 point)
        {
            Vector3 lineHeadingVector = (lineEndPosition - lineStartPosition);
            float maxDistance = lineHeadingVector.magnitude;
            lineHeadingVector.Normalize();

            Vector3 lineVectorStartToPoint = point - lineStartPosition;
            float dotProduct = Vector3.Dot(lineVectorStartToPoint, lineHeadingVector);
            dotProduct = Mathf.Clamp(dotProduct, 0.0f, maxDistance);
            return lineStartPosition + lineHeadingVector * dotProduct;
        }

        private float TurnTowardTarget()
        {
            Vector3 vectorToTarget = targetPosition - transform.position;
            vectorToTarget.Normalize();
            if (isAvoidingCars)
            {
                //Avoidance(vectorToTarget, out vectorToTarget);

            }
            angleToTarget = Vector3.SignedAngle(transform.forward, vectorToTarget, -transform.up);
            angleToTarget *= -1;
            float steerAmount = angleToTarget / 30.0f;
            steerAmount = Mathf.Clamp(steerAmount, -1.0f, 1.0f);
            return steerAmount;
        }

        private void Avoidance()
        {
            RaycastHit hit;
            Vector3 sensorStartPos = transform.position;
            sensorStartPos += transform.forward * sensorPosition.z;
            sensorStartPos += transform.up * sensorPosition.y;

            //center sensor
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("Player"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                }
            }
            //right sensor
            sensorStartPos += transform.right * sideSensorPosition;
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("Player"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                }
            }
            //right angle sensor
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(sensorAngle, transform.up) * transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("Player"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                }
            }
            //left sensor
            sensorStartPos -= 2 * sideSensorPosition * transform.right;
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("Player"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                }
            }
            //left angle sensor
            if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-sensorAngle, transform.up) * transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("Player"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                }
            }
        }

        private float ApplyThrottleOrBrake(float x)
        {
            /*
            if (carController.GetVelocityMagnitude() > driveSpeed)
            {
                return 0;
            }
            */

            float reduceSpeedDueToCornering = Mathf.Abs(x) / 1.0f;
            float throttle = 1.05f - reduceSpeedDueToCornering;

            if (temporaryWaypoints.Count() != 0)
            {
                if (angleToTarget > 70)
                    throttle *= -1;
                else if (angleToTarget < -70)
                    throttle *= -1;
                else if (stuckCheckCounter > 3 && stuckCheckCounter < 10)
                    throttle *= -1;
            }

            return throttle;
        }
    }
}
