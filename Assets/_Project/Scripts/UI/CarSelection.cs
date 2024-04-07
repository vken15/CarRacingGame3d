using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class CarSelection : MonoBehaviour
    {
        [Header("Button")]
        [SerializeField] private GameObject nextBtn;
        [SerializeField] private GameObject prevBtn;
        [SerializeField] private GameObject changeBtn;
        [SerializeField] private GameObject confirmBtn;
        [SerializeField] private GameObject startBtn;
        [SerializeField] private GameObject readyBtn;

        [Header("Car prefab")]
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private GameObject carSkinItem;

        [Header("Spawn on")]
        [SerializeField] private Transform spawnOnTransform;
        [SerializeField] private Transform spawnSkinItemTransform;

        [SerializeField] private Sprite defaultCarSprite;

        private CarData[] carDatas;
        private readonly List<CarData> carList = new();
        private readonly Dictionary<ushort, List<CarData>> skinList = new();
        private readonly List<GameObject> currentSkinList = new();
        private int selectedCarIndex = 0;
        private ushort carID = 0;
        private CarSelectUIHandler carSelectUIHandler = null;
        private bool isChangingCar = false;

        // Start is called before the first frame update
        private void Start()
        {
            //Load the car Data
            carDatas = Resources.LoadAll<CarData>("CarData/");
            foreach (CarData car in carDatas)
            {
                if (!skinList.ContainsKey(car.BaseCarID))
                    skinList.Add(car.BaseCarID, new() { car });
                else
                    skinList[car.BaseCarID].Add(car);

                if (!car.IsSkin)
                    carList.Add(car);
            }

            StartCoroutine(SpawnCarCO(true));
        }

        private void Update()
        {
            if (confirmBtn.activeInHierarchy)
            {
                float input = InputManager.instance.Controllers.Player.Move.ReadValue<Vector2>().x;
                if (input > 0)
                    OnNextCar();
                if (input < 0)
                    OnPreviousCar();
            }
        }

        public void OnChangeButtonPressed()
        {
            nextBtn.SetActive(true);
            prevBtn.SetActive(true);
            changeBtn.SetActive(false);
            confirmBtn.SetActive(true);
            startBtn.GetComponent<Button>().interactable = false;
            readyBtn.GetComponent<Button>().interactable = false;
            spawnSkinItemTransform.gameObject.SetActive(true);
        }

        public void OnConfirmButtonPressed()
        {
            nextBtn.SetActive(false);
            prevBtn.SetActive(false);
            changeBtn.SetActive(true);
            confirmBtn.SetActive(false);
            startBtn.GetComponent<Button>().interactable = true;
            readyBtn.GetComponent<Button>().interactable = true;
            spawnSkinItemTransform.gameObject.SetActive(false);
        }

        public void OnNextCar()
        {
            if (isChangingCar)
                return;

            selectedCarIndex++;
            if (selectedCarIndex > carList.Count - 1)
                selectedCarIndex = 0;

            StartCoroutine(SpawnCarCO(false));
        }

        public void OnPreviousCar()
        {
            if (isChangingCar)
                return;

            selectedCarIndex--;
            if (selectedCarIndex < 0)
                selectedCarIndex = carList.Count - 1;

            StartCoroutine(SpawnCarCO(true));
        }

        private IEnumerator SpawnCarCO(bool isCarAppearingOnRightSide)
        {
            isChangingCar = true;
            if (carSelectUIHandler != null)
                carSelectUIHandler.StartCarExitAnimation(!isCarAppearingOnRightSide);

            GameObject intantiatedCar = Instantiate(carPrefab, spawnOnTransform);
            carSelectUIHandler = intantiatedCar.GetComponent<CarSelectUIHandler>();
            carSelectUIHandler.SetupCar(carList[selectedCarIndex], spawnOnTransform);
            carSelectUIHandler.StartCarEntranceAnimation(isCarAppearingOnRightSide);
            carID = carList[selectedCarIndex].CarID;

            spawnSkinItemTransform.gameObject.SetActive(false);
            foreach (var skin in currentSkinList)
            {
                Destroy(skin);
            }
            currentSkinList.Clear();
            foreach (var skin in skinList[carID])
            {
                var car = Instantiate(carSkinItem, spawnSkinItemTransform);
                car.GetComponent<Image>().sprite = skin.CarUISprite;
                car.GetComponent<Button>().onClick.AddListener(() => {
                    carID = skin.CarID;
                    carSelectUIHandler.ChangeSkin(skin, carSelectUIHandler.transform);
                });
                currentSkinList.Add(car);
            }  

            yield return new WaitForSeconds(0.4f);
            if (confirmBtn.activeSelf)
                spawnSkinItemTransform.gameObject.SetActive(true);
            isChangingCar = false;
        }

        public ushort GetCarIDData()
        {
            return carID;
        }

        public Sprite GetCarSprite(ushort id)
        {
            if (carDatas == null) 
                return defaultCarSprite;

            foreach (CarData car in carDatas)
            {
                if (car.CarID == id)
                    return car.CarUISprite;
            }

            return carDatas[0].CarUISprite;
        }
    }
}
