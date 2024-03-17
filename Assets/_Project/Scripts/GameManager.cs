using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRacingGame3d
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance = null;
        private GameStates gameState = GameStates.Countdown;
        public NetworkStatus networkStatus = NetworkStatus.offline;
        public GameMode gameMode = GameMode.Round;

        //Time
        private float raceStartedTime = 0;
        private float raceCompletedTime = 0;

        //
        private readonly List<Driver> driverList = new();
        private ushort numberOfLaps = 2;
        private ushort numberOfCarsRaceComplete = 0;
        public MapData map = null;

        //Round Mode
        public ushort maxRound = 1;
        private readonly ushort[] pointReward = { 0, 10, 8, 6, 5, 4, 3, 2, 1 };
        [HideInInspector] public ushort currentRound = 1;

        //Events
        public event Action<GameManager> OnGameStateChanged;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            //driverList.Add(new Driver(1, "P1", 1, false, AIDifficult.Normal, 0));
            //driverList.Add(new Driver(2, "P2", 1, true, AIDifficult.Normal, 0));
        }
        
        private void LoadMap()
        {
            ChangeGameState(GameStates.Countdown);

            Debug.Log("Map loaded");
        }
        
        public float GetRaceTime()
        {
            if (gameState == GameStates.Countdown)
            {
                return 0;
            }
            else if (gameState == GameStates.RaceOver)
            {
                return raceCompletedTime - raceStartedTime;
            }
            else
            {
                return Time.time - raceStartedTime;
            }
        }
        
        public void OnRaceStart()
        {
            raceStartedTime = Time.time;
            ChangeGameState(GameStates.Running);
            Debug.Log("Race started");
        }
        
        public void OnRaceCompleted()
        {
            numberOfCarsRaceComplete++;
            if (numberOfCarsRaceComplete >= driverList.Count)
                OnRaceOver();
            else
                ChangeGameState(GameStates.RaceOverCountDown);
            Debug.Log("Race completed");
        }
        
        public void OnRaceOver()
        {
            raceCompletedTime = Time.time;
            numberOfCarsRaceComplete = 0;

            if (gameMode == GameMode.Round 
                && (NetworkManager.Singleton.IsServer || networkStatus == NetworkStatus.offline))
            {
                List<CarLapCounter> carLapCounters = FindObjectsByType<CarLapCounter>(FindObjectsSortMode.None).ToList();
                carLapCounters = carLapCounters.OrderByDescending(s => s.GetNumberOfCheckPointsPassed()).ThenBy(s => s.GetTimeAtLastPassedCheckPoint()).ToList();
                foreach (var carLapCounter in carLapCounters)
                {
                    int playerNumber = carLapCounter.GetComponent<CarInputHandler>().playerNumber;
                    int carPosition = carLapCounters.IndexOf(carLapCounter) + 1;
                    Debug.Log(playerNumber + " " + carPosition);
                    AddPoints(playerNumber, pointReward[carPosition]);
                    SetDriverLastRacePosition(playerNumber, carPosition);
                }

                if (NetworkManager.Singleton.IsServer)
                {
                    foreach (var driver in driverList)
                    {
                        SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(driver.ClientId);
                        if (sessionPlayerData.HasValue)
                        {
                            var playerData = sessionPlayerData.Value;
                            playerData.Score = driver.Score;
                            SessionManager<SessionPlayerData>.Instance.SetPlayerData(driver.ClientId, playerData);
                        }
                    }
                }

                var leaderboardUIHandler = FindFirstObjectByType<LeaderboardUIHandler>();
                if (leaderboardUIHandler != null)
                {
                    leaderboardUIHandler.UpdateScore(driverList);
                }
            }

            ChangeGameState(GameStates.RaceOver);
            Debug.Log("Race over");
        }
        
        public GameStates GetGameState()
        {
            return gameState;
        }
        
        private void ChangeGameState(GameStates newGameState)
        {
            if (gameState != newGameState)
            {
                gameState = newGameState;
                OnGameStateChanged?.Invoke(this);
            }
        }
        
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (map != null && scene.name == map.Scene)
            {
                LoadMap();
            }
        }
        
        //Lap
        public void SetNumberOfLaps(ushort number)
        {
            numberOfLaps = number;
        }

        public ushort GetNumberOfLaps()
        {
            return numberOfLaps;
        }

        //Driver
        public void AddDriverToList(ushort playerNumber, string name, ushort carID, bool isAI, ulong networkId, AIDifficult difficult = AIDifficult.Easy)
        {
            driverList.Add(new Driver(playerNumber, name, carID, isAI, networkId, difficult));
        }
        
        public List<Driver> GetDriverList()
        {
            return driverList;
        }

        private Driver FindDriver(int playerNumber)
        {
            foreach (Driver driver in driverList)
            {
                if (playerNumber == driver.PlayerNumber)
                    return driver;
            }
            return null;
        }
        
        public void SetDriverLastRacePosition(int playerNumber, int position)
        {
            Driver driver = FindDriver(playerNumber);
            driver.LastRacePosition = position;
        }
        
        public void AddPoints(int playerNumber, int points)
        {
            Driver driver = FindDriver(playerNumber);
            driver.Score += points;
        }
        
        public void ClearDriverList()
        {
            driverList.Clear();
        }
    }
}