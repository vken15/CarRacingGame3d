using System;
using UnityEngine;

public class CarLapCounter : MonoBehaviour
{
    private int passedCheckPointNumber = 0;
    private float timeAtLastPassedCheckPoint = 0;
    private int numberOfPassedCheckPoints = 0;
    private int lapsCompleted = 0;
    private int lapsToCompleted;
    private bool isRaceCompleted = false;
    private LapCountUIHandler lapsCountUIHandler;

    public int carPosition = 0;
    public event Action<CarLapCounter> OnPassCheckPoint;

    private void Start()
    {
        //lapsToCompleted = GameManager.instance.GetNumberOfLaps();
        lapsToCompleted = 4;

        //if (!IsServer && GameManager.instance.networkStatus == NetworkStatus.online) return;

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
            if (isRaceCompleted) //|| GameManager.instance.GetGameState() == GameStates.raceOver)
                return;
            
            CheckPoint checkPoint = collision.GetComponent<CheckPoint>();
            print(passedCheckPointNumber + 1);
            print(checkPoint.checkPointNumber);
            //Make sure the car is passing the checkpoints in the correct order. 1 -> 2 -> 3 ...
            if (passedCheckPointNumber + 1 == checkPoint.checkPointNumber)
            {
                passedCheckPointNumber = checkPoint.checkPointNumber;
                numberOfPassedCheckPoints++;
                timeAtLastPassedCheckPoint = Time.time;
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
                        lapsCountUIHandler.SetLapText($"LAP {lapsCompleted + 1}/{lapsToCompleted}");
                    }
                    //if (!isRaceCompleted && lapsCountUIHandler != null)
                    //{
                    //    if (IsServer)
                    //    {
                    //        UpdateLapCountClientRpc(lapsCompleted, lapsToCompleted);
                    //    }
                    //    else if (GameManager.instance.networkStatus == NetworkStatus.offline)
                    //    {
                    //        lapsCountUIHandler.SetLapText($"LAP {lapsCompleted + 1}/{lapsToCompleted}");
                    //    }
                    //}
                }

                OnPassCheckPoint?.Invoke(this);

                //Show the cars position
                if (isRaceCompleted)
                {
                    if (CompareTag("Player"))
                    {
                        GetComponentInParent<CarInputHandler>().enabled = false;
                        //CarAIHandler carAIHandler = GetComponent<CarAIHandler>();
                        //carAIHandler.enabled = true;
                        //carAIHandler.aiMode = CarAIHandler.AIMode.followWayPoints;
                        //GetComponent<AStarLite>().enabled = true;
                    }

                    if (GameManager.instance.GetGameState() != GameStates.raceOver)
                        GameManager.instance.OnRaceCompleted();
                }
            }
        }
    }
}
