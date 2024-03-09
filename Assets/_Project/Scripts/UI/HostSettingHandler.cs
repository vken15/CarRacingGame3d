using System;
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

        private GameObject mapDisplay;

        private void Start()
        {
            if (GameManager.instance.map != null)
            {
                mapDisplay = Instantiate(mapPrefab, spawnOnTransform);
                mapDisplay.GetComponent<Button>().enabled = false;
                ChangeMap();
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
