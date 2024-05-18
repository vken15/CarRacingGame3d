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
        private bool avoiding = false;
        private bool leftObstacles = false;
        private bool rightObstacles = false;

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
        }

        void Start()
        {
            allWayPoints = FindObjectsByType<WayPointNode>(FindObjectsSortMode.None);
        }

        void FixedUpdate()
        {
            if (GameManager.instance.GetGameState() == GameStates.Countdown)
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

            DriverInput driverInput = new()
            {
                Move = inputVector,
                Nitro = false,
                Brake = false
            };

            carController.SetInput(driverInput);
        }

        private void FollowWayPoints()
        {
            if (currentWayPoint == null)
            {
                currentWayPoint = FindClosestWayPoint();
                previousWayPoint = currentWayPoint;
                nextWayPoint = currentWayPoint.nextWayPointNode[Random.Range(0, currentWayPoint.nextWayPointNode.Length - 1)];
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
                    //Debug.DrawLine(transform.position, targetPosition, Color.black);
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
            if (isAvoidingCars && IsObstaclesInFrontOf(out float avoidMultiplier))
            {
                if (leftObstacles && rightObstacles)
                    return -avoidMultiplier;

                return avoidMultiplier;
            }
            else
            {
                angleToTarget = Vector3.SignedAngle(transform.forward, vectorToTarget, -transform.up);
                angleToTarget *= -1;
                float steerAmount = angleToTarget / 30.0f;
                steerAmount = Mathf.Clamp(steerAmount, -1.0f, 1.0f);
                return steerAmount;
            }
        }

        private bool IsObstaclesInFrontOf(out float avoidMultiplier)
        {
            Vector3 sensorStartPos = transform.position;
            sensorStartPos += transform.forward * sensorPosition.z;
            sensorStartPos += transform.up * sensorPosition.y;
            avoidMultiplier = 0f;
            avoiding = false;

            //right sensor
            sensorStartPos += transform.right * sideSensorPosition;
            if (Physics.Raycast(sensorStartPos, transform.forward, out RaycastHit hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("CarBody"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    leftObstacles = true;
                    avoidMultiplier -= 1f;
                    if (Vector3.Distance(sensorStartPos, hit.point) < 1f)
                    {
                        rightObstacles = true;
                    }
                }
            }
            //right angle sensor
            else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(sensorAngle, transform.up) * transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("CarBody"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    leftObstacles = true;
                    avoidMultiplier -= 0.5f;
                    if (Vector3.Distance(sensorStartPos, hit.point) < 1f)
                    {
                        rightObstacles = true;
                    }
                }
            } else
            {
                leftObstacles = false;
            }

            //left sensor
            sensorStartPos -= 2 * sideSensorPosition * transform.right;
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("CarBody"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    rightObstacles = true;
                    avoidMultiplier += 1f;
                    if (Vector3.Distance(sensorStartPos, hit.point) < 1f)
                    {
                        leftObstacles = true;
                    }
                }
            }
            //left angle sensor
            else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-sensorAngle, transform.up) * transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("CarBody"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    rightObstacles = true;
                    avoidMultiplier += 0.5f;
                    if (Vector3.Distance(sensorStartPos, hit.point) < 1f)
                    {
                        leftObstacles = true;
                    }
                }
            } else
            {
                rightObstacles = false;
            }

            //center sensor
            if (avoidMultiplier == 0 && Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (hit.collider.CompareTag("Terrain") || hit.collider.CompareTag("CarBody"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    rightObstacles = true;
                    if (hit.normal.x < 0f)
                    {
                        avoidMultiplier = -1;
                    }
                    else
                    {
                        avoidMultiplier = 1;
                    }
                }
            }

            return avoiding;
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

            if (leftObstacles && rightObstacles)
                throttle = -1;

            return throttle;
        }
    }
}
