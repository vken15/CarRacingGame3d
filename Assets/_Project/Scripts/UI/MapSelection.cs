using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class MapSelection : MonoBehaviour
    {
        [Header("Button")]
        [SerializeField] private GameObject cancelBtn;
        [SerializeField] private GameObject confirmBtn;

        [Header("Map prefab")]
        [SerializeField] private GameObject mapPrefab;

        [Header("Spawn on")]
        [SerializeField] private Transform spawnOnTransform;
        [SerializeField] private Transform listMapSpawnOnTransform;

        [Header("Map infomation")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text numberOfLapsText;
        //[SerializeField] private Text discriptionText;

        private MapData[] mapDatas;
        private int selectedMapIndex = 0;
        private GameObject mapDisplay;

        // Start is called before the first frame update
        private void Start()
        {
            //Load the map Data
            mapDatas = Resources.LoadAll<MapData>("MapData/");

            mapDisplay = Instantiate(mapPrefab, spawnOnTransform);
            mapDisplay.GetComponent<Button>().enabled = false;
            DisplayMap(0);
            
            //Remove this when complete create host
            GameManager.instance.map = mapDatas[0];
            GameManager.instance.SetNumberOfLaps(mapDatas[0].NumberOfLaps);
            //

            for (int i = 0; i < mapDatas.Length; i++)
            {
                var map = Instantiate(mapPrefab, listMapSpawnOnTransform);
                map.GetComponent<Image>().sprite = mapDatas[i].MapUISprite;
                map.GetComponent<MapIndexUI>().index = i;
                map.GetComponent<Button>().onClick.AddListener(() =>
                {
                    selectedMapIndex = map.GetComponent<MapIndexUI>().index;
                    DisplayMap(selectedMapIndex);
                });
            }

            gameObject.SetActive(false);
            gameObject.GetComponent<Canvas>().enabled = true;
        }

        private void DisplayMap(int index)
        {
            mapDisplay.GetComponent<Image>().sprite = mapDatas[index].MapUISprite;
            mapNameText.text = mapDatas[index].MapName;
            difficultyText.text = "Difficulty: " + mapDatas[index].Difficulty.ToString();
            numberOfLapsText.text = "Lap: " + mapDatas[index].NumberOfLaps.ToString();
        }

        public void OnConfirm()
        {
            GameManager.instance.map = mapDatas[selectedMapIndex];
            GameManager.instance.SetNumberOfLaps(mapDatas[selectedMapIndex].NumberOfLaps);
            
            if (GameManager.instance.networkStatus == NetworkStatus.online)
                SendMapChangedToClientRpc(mapDatas[selectedMapIndex].MapID);

            gameObject.SetActive(false);
        }

        public void OnCancel()
        {
            gameObject.SetActive(false);
        }

        [ClientRpc]
        private void SendMapChangedToClientRpc(ushort mapId)
        {

        }
    }
}
