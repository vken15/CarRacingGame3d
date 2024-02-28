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

                setLeaderboardItemInfo[i] = leaderboardInfoGameObject.GetComponent<SetLeaderboardItemInfo>();
                setLeaderboardItemInfo[i].SetDriverNameText(carLapCounterArray[i].GetComponentInParent<CarController>().gameObject.name);
                setLeaderboardItemInfo[i].SetPositionText($"{i + 1}.");
                setLeaderboardItemInfo[i].SetDriverFinishTimeText($"00:00");
            }

            Canvas.ForceUpdateCanvases();
            isInitilized = true;
        }
        public void UpdateList(List<CarLapCounter> lapCounters)
        {
            if (!isInitilized)
                return;
            //Update the leaderboard items
            for (int i = 0; i < lapCounters.Count; i++)
            {
                setLeaderboardItemInfo[i].SetDriverNameText(lapCounters[i].GetComponentInParent<CarController>().gameObject.name);
            }
        }
        public void UpdateTimer(CarLapCounter lapCounter, float time)
        {
            foreach (SetLeaderboardItemInfo d in setLeaderboardItemInfo)
                if (d.GetDriverName() == lapCounter.GetComponentInParent<CarController>().gameObject.name)
                {
                    int raceTimeMinutes = (int)Mathf.Floor(time / 60);
                    int raceTimeSeconds = (int)Mathf.Floor(time % 60);
                    d.SetDriverFinishTimeText($"{raceTimeMinutes:00}:{raceTimeSeconds:00}");
                    break;
                }
        }

        //Events 
        private void OnGameStateChanged(GameManager gameManager)
        {
            if (GameManager.instance.GetGameState() == GameStates.raceOver)
            {
                foreach (SetLeaderboardItemInfo d in setLeaderboardItemInfo)
                    if (d.GetDriverFinishTime().Equals("00:00"))
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