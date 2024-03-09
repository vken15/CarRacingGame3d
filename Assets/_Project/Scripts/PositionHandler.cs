using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class PositionHandler : MonoBehaviour
    {
        [SerializeField] private List<CarLapCounter> carLapCounters = new();
        private LeaderboardUIHandler leaderboardUIHandler;

        private readonly ushort[] pointReward = { 0, 10, 8, 6, 5, 4, 3, 2, 1 };

        // Start is called before the first frame update
        private void Start()
        {
            CarLapCounter[] carLapCounterArray = FindObjectsByType<CarLapCounter>(FindObjectsSortMode.None);
            carLapCounters = carLapCounterArray.ToList();
            foreach (CarLapCounter lapCounters in carLapCounters)
            {
                lapCounters.OnPassCheckPoint += OnPassCheckPoint;
            }
            leaderboardUIHandler = FindFirstObjectByType<LeaderboardUIHandler>();
            if (leaderboardUIHandler != null)
            {
                leaderboardUIHandler.UpdateList(carLapCounters);
            }
        }

        private void OnPassCheckPoint(CarLapCounter carLapCounter)
        {
            //Sort
            carLapCounters = carLapCounters.OrderByDescending(s => s.GetNumberOfCheckPointsPassed()).ThenBy(s => s.GetTimeAtLastPassedCheckPoint()).ToList();
            //Get car position
            int carPosition = carLapCounters.IndexOf(carLapCounter) + 1;
            carLapCounter.carPosition = carPosition;

            if (carLapCounter.IsRaceCompleted())
            {
                //Set player last position
                int playerNumber = carLapCounter.GetComponentInParent<CarInputHandler>().playerNumber;

                if (GameManager.instance.networkStatus == NetworkStatus.offline || NetworkManager.Singleton.IsServer)
                {
                    GameManager.instance.SetDriverLastRacePosition(playerNumber, carPosition);
                    GameManager.instance.AddPoints(playerNumber, pointReward[carPosition]);
                }
                //if (playerNumber == 1)
                //{
                //    int numberOfLaps = GameManager.instance.GetNumberOfLaps();
                //    float time = GameManager.instance.GetRaceTime() / numberOfLaps;
                //    int raceTimeMinutes = (int)Mathf.Floor(time / 60);
                //    int raceTimeSeconds = (int)Mathf.Floor(time % 60);
                //    string key = GameManager.instance.GetMapScene() + "Best Time";
                //    string bestTime = $"{raceTimeMinutes:00}:{raceTimeSeconds:00}";
                //    string oldBestTime = PlayerPrefs.GetString(key);
                //    int oldTime = int.MaxValue;
                //    if (oldBestTime.Length == 5)
                //        oldTime = int.Parse(oldBestTime[..2]) * 60 + int.Parse(oldBestTime[3..]);

                //    if (oldTime > time)
                //        PlayerPrefs.SetString(key, bestTime);
                //}
            }

            if (leaderboardUIHandler != null)
            {
                leaderboardUIHandler.UpdateList(carLapCounters);
                if (carLapCounter.IsRaceCompleted())
                {
                    leaderboardUIHandler.UpdateTimer(carLapCounter, GameManager.instance.GetRaceTime());
                }
            }
        }
    }
}