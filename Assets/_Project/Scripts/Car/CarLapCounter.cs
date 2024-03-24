using System;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Manage racing laps for car
    /// </summary>
    public class CarLapCounter : NetworkBehaviour
    {
        [SerializeField] private GameObject warningPrefab;

        private int passedCheckPointNumber = 0;
        private float timeAtLastPassedCheckPoint = 0;
        private int numberOfPassedCheckPoints = 0;
        private ushort lapsCompleted = 0;
        private ushort lapsToCompleted;
        private bool isRaceCompleted = false;
        private LapCountUIHandler lapsCountUIHandler;
        private int lastWarningNumber = -1;
        private GameObject warning;

        public int carPosition = 0;
        public event Action<CarLapCounter> OnPassCheckPoint;

        private void Start()
        {
            lapsToCompleted = GameManager.instance.GetNumberOfLaps();

            if (IsOwner || GameManager.instance.networkStatus == NetworkStatus.offline)
                warning = Instantiate(warningPrefab);

            if (!IsServer && GameManager.instance.networkStatus == NetworkStatus.online) return;

            if (CompareTag("Player"))
            {
                lapsCountUIHandler = FindFirstObjectByType<LapCountUIHandler>();
            }
        }
        public int GetNumberOfCheckPointsPassed()
        {
            return numberOfPassedCheckPoints;
        }
        public float GetTimeAtLastPassedCheckPoint()
        {
            return timeAtLastPassedCheckPoint;
        }
        public bool IsRaceCompleted()
        {
            return isRaceCompleted;
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (collision.CompareTag("CheckPoint"))
            {
                //Once a car has completed the race. Stop checking any checkpoints or laps.
                if (isRaceCompleted || GameManager.instance.GetGameState() == GameStates.RaceOver)
                    return;

                CheckPoint checkPoint = collision.GetComponent<CheckPoint>();
                //Make sure the car is passing the checkpoints in the correct order. 1 -> 2 -> 3 ...
                if (passedCheckPointNumber + 1 == checkPoint.checkPointNumber)
                {
                    passedCheckPointNumber = checkPoint.checkPointNumber;
                    numberOfPassedCheckPoints++;
                    timeAtLastPassedCheckPoint = Time.time;
                    if (warning != null)
                    {
                        warning.SetActive(false);
                        lastWarningNumber = -1;
                    }
                        
                    if (checkPoint.isFinishLine)
                    {
                        passedCheckPointNumber = 0;
                        lapsCompleted++;
                        if (lapsCompleted >= lapsToCompleted)
                        {
                            isRaceCompleted = true;
                        }
                        if (!isRaceCompleted && lapsCountUIHandler != null)
                        {
                            if (IsServer)
                            {
                                UpdateLapCountClientRpc(lapsCompleted);
                            }
                            else if (GameManager.instance.networkStatus == NetworkStatus.offline)
                            {
                                lapsCountUIHandler.SetLapText($"LAP {lapsCompleted + 1}/{lapsToCompleted}");
                            }
                        }
                    }

                    OnPassCheckPoint?.Invoke(this);

                    if (isRaceCompleted)
                    {
                        if (CompareTag("Player"))
                        {
                            GetComponentInParent<CarInputHandler>().enabled = false;
                            GetComponentInParent<CarAIHandler>().enabled = true;
                        }

                        if (GameManager.instance.GetGameState() != GameStates.RaceOver)
                            GameManager.instance.OnRaceCompleted();
                    }
                }
                //Wrong way warning
                else if ((IsOwner || GameManager.instance.networkStatus == NetworkStatus.offline) && !checkPoint.isFinishLine)
                {
                    if (warning != null && (lastWarningNumber == -1 || lastWarningNumber - 1 == checkPoint.checkPointNumber) && passedCheckPointNumber != checkPoint.checkPointNumber)
                    {
                        lastWarningNumber = checkPoint.checkPointNumber;
                        warning.SetActive(true);
                    } else if (lastWarningNumber + 1 == checkPoint.checkPointNumber)
                    {
                        lastWarningNumber = -1;
                        warning.SetActive(false);
                    }
                }
            }
        }

        [ClientRpc]
        private void UpdateLapCountClientRpc(ushort laps)
        {
            if (IsOwner)
            {
                if (lapsCountUIHandler == null)
                {
                    lapsCountUIHandler = FindFirstObjectByType<LapCountUIHandler>();
                }
                Debug.Log("Client: " + OwnerClientId + " lap: " + laps);
                lapsCountUIHandler.SetLapText($"LAP {laps + 1}/{lapsToCompleted}");
            }
        }
    }
}