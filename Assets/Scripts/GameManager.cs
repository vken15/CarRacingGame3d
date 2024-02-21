using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    private GameStates gameState = GameStates.countdown;
    public NetworkStatus networkStatus = NetworkStatus.offline;

    //Time
    private float raceStartedTime = 0;
    private float raceCompletedTime = 0;

    //
    private readonly List<Driver> driverList = new();
    private int numberOfLaps = 2;
    private int numberOfCarsRaceComplete = 0;

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
        driverList.Add(new Driver(1, "P1", 1, false, AIDifficult.Normal, 0));
    }
    private void LoadMap()
    {
        ChangeGameState(GameStates.countdown);

        Debug.Log("Map loaded");
    }
    public float GetRaceTime()
    {
        if (gameState == GameStates.countdown)
        {
            return 0;
        }
        else if (gameState == GameStates.raceOver)
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
        ChangeGameState(GameStates.running);
        Debug.Log("Race started");
    }
    public void OnRaceCompleted()
    {
        numberOfCarsRaceComplete++;
        if (numberOfCarsRaceComplete >= 4)//map.MaxCars)
            OnRaceOver();
        else
            ChangeGameState(GameStates.raceOverCountDown);
        Debug.Log("Race completed");
    }
    public void OnRaceOver()
    {
        raceCompletedTime = Time.time;
        numberOfCarsRaceComplete = 0;
        ChangeGameState(GameStates.raceOver);
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
        LoadMap();
    }
    //Map
    //public void SetMap(MapData m)
    //{
    //    map = m;
    //}
    public void SetNumberOfLaps(int number)
    {
        numberOfLaps = number;
    }
    public int GetNumberOfLaps()
    {
        return numberOfLaps;
    }
    //public string GetMapScene()
    //{
    //    return map.Scene;
    //}
    //public int GetMaxCars()
    //{
    //    return map.MaxCars;
    //}
    //Driver
    public void AddDriverToList(int playerNumber, string name, int carID, bool isAI, AIDifficult difficult, ulong networkId)
    {
        driverList.Add(new Driver(playerNumber, name, carID, isAI, difficult, networkId));
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
        driver.Points += points;
    }
    public void ClearDriverList()
    {
        driverList.Clear();
    }
}
