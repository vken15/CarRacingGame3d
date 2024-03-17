using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class HostSettingHandler : MonoBehaviour
    {
        [Header("Map prefab")]
        [SerializeField] private GameObject mapPrefab;

        [Header("Spawn on")]
        [SerializeField] private Transform spawnOnTransform;

        [Header("Map infomation")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text numberOfLapsText;

        [SerializeField] private GameObject mapSelectCanvas;
        [SerializeField] private TMP_Dropdown maxPlayerDropdown;
        [SerializeField] private TMP_Dropdown maxRoundDropdown;

        [Header("In room")]
        [SerializeField] private Text roundText;

        private GameObject mapDisplay;

        private void Start()
        {
            if (GameManager.instance.map != null)
            {
                mapDisplay = Instantiate(mapPrefab, spawnOnTransform);
                mapDisplay.GetComponent<Button>().enabled = false;
                ChangeMap();
            }

            if (maxPlayerDropdown != null)
            {
                List<string> options = new();
                for (int i = 2; i <= 8; i++)
                {
                    options.Add(i.ToString());
                }
                maxPlayerDropdown.AddOptions(options);
                maxPlayerDropdown.value = ConnectionManager.instance.MaxConnectedPlayers - 2;
                maxPlayerDropdown.RefreshShownValue();
                maxPlayerDropdown.onValueChanged.AddListener((index) => {
                    ConnectionManager.instance.MaxConnectedPlayers = (ushort)(index + 2);
                });
            }
            if (maxRoundDropdown != null)
            {
                List<string> options = new();
                for (int i = 1; i <= 5; i++)
                {
                    options.Add(i.ToString());
                }
                maxRoundDropdown.AddOptions(options);
                maxRoundDropdown.value = GameManager.instance.maxRound - 2;
                maxRoundDropdown.RefreshShownValue();
                maxRoundDropdown.onValueChanged.AddListener((index) => {
                    GameManager.instance.maxRound = (ushort)(index + 1);
                });
            }
            if (roundText != null)
            {
                roundText.text = $"Round: {GameManager.instance.currentRound}/{GameManager.instance.maxRound}";
            }
        }

        public void ChangeMap()
        {
            MapData map = GameManager.instance.map;
            mapDisplay.GetComponent<Image>().sprite = map.MapUISprite;
            mapNameText.text = map.MapName;
            difficultyText.text = "Difficulty: " + map.Difficulty.ToString();
            numberOfLapsText.text = "Lap: " + map.NumberOfLaps.ToString();
        }

        public void OnCancel()
        {
            gameObject.SetActive(false);
        }

        public void OnChangeMapPressed()
        {
            mapSelectCanvas.SetActive(true);
        }
    }
}
