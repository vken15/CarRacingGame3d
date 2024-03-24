using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class LeaderboardUIHandler : MonoBehaviour
    {
        [SerializeField] private GameObject leaderboardItemPrefab;

        private SetLeaderboardItemInfo[] setLeaderboardItemInfo;
        private bool isInitilized = false;

        private Color[] playerColors = { Color.black, Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.gray, Color.cyan };

        //Oher components
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;

            //Hook up events
            GameManager.instance.OnGameStateChanged += OnGameStateChanged;
        }

        // Start is called before the first frame update
        private void Start()
        {
            VerticalLayoutGroup leaderboardLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>();

            //Get all Car lap counters in the scene. 
            CarLapCounter[] carLapCounterArray = FindObjectsByType<CarLapCounter>(FindObjectsSortMode.None);

            //Allocate the array
            setLeaderboardItemInfo = new SetLeaderboardItemInfo[carLapCounterArray.Length];

            //Create the leaderboard items
            for (int i = 0; i < carLapCounterArray.Length; i++)
            {
                //Set the position
                GameObject leaderboardInfoGameObject = Instantiate(leaderboardItemPrefab, leaderboardLayoutGroup.transform);
                var carInput = carLapCounterArray[i].GetComponent<CarInputHandler>();
                setLeaderboardItemInfo[i] = leaderboardInfoGameObject.GetComponent<SetLeaderboardItemInfo>();
                setLeaderboardItemInfo[i].SetDriverNameText(carInput.gameObject.name, playerColors[carInput.playerNumber]);
                setLeaderboardItemInfo[i].SetPositionText($"{i + 1}.");
                setLeaderboardItemInfo[i].SetDriverFinishTimeText("--:--");
                setLeaderboardItemInfo[i].SetDriverScoreText("0");
                setLeaderboardItemInfo[i].playerNumber = carInput.playerNumber;
            }

            Canvas.ForceUpdateCanvases();
            isInitilized = true;
        }

        private void Update()
        {
            if (InputManager.instance.Controllers.Player.Tab.IsPressed() || GameManager.instance.GetGameState() == GameStates.RaceOver)
                canvas.enabled = true;
            else
                canvas.enabled = false;
        }

        public void UpdateList(List<CarLapCounter> lapCounters)
        {
            if (!isInitilized)
                return;

            //Update the leaderboard items
            for (int i = 0; i < lapCounters.Count; i++)
                for (int j = 0; j < setLeaderboardItemInfo.Length; j++)
                    if (lapCounters[i] != null && lapCounters[i].GetComponent<CarInputHandler>().playerNumber == setLeaderboardItemInfo[j].playerNumber)
                    {
                        setLeaderboardItemInfo[j].SetPositionText($"{i + 1}.");
                        setLeaderboardItemInfo[j].transform.SetSiblingIndex(i);
                        break;
                    }
        }

        public void UpdateTimer(CarLapCounter lapCounter, float time)
        {
            foreach (SetLeaderboardItemInfo d in setLeaderboardItemInfo)
                if (d.playerNumber == lapCounter.GetComponent<CarInputHandler>().playerNumber)
                {
                    int raceTimeMinutes = (int)Mathf.Floor(time / 60);
                    int raceTimeSeconds = (int)Mathf.Floor(time % 60);
                    d.SetDriverFinishTimeText($"{raceTimeMinutes:00}:{raceTimeSeconds:00}");
                    break;
                }
        }

        public void UpdateScore(List<Driver> drivers)
        {
            for (int i = 0; i < setLeaderboardItemInfo.Length; i++)
                foreach (Driver driver in drivers)
                    if (driver.LastRacePosition == i + 1)
                    {
                        setLeaderboardItemInfo[i].SetDriverScoreText($"{driver.Score}");
                        break;
                    }
        }

        //Events 
        private void OnGameStateChanged(GameManager gameManager)
        {
            if (GameManager.instance.GetGameState() == GameStates.RaceOver)
            {
                foreach (SetLeaderboardItemInfo d in setLeaderboardItemInfo)
                    if (d.GetDriverFinishTime().Equals("--:--"))
                        d.SetDriverFinishTimeText("Fail");

                canvas.enabled = true;
            }
        }

        private void OnDestroy()
        {
            //Unhook events
            GameManager.instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}