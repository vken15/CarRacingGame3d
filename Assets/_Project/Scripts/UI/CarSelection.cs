using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Windows;

namespace CarRacingGame3d
{
    public class CarSelection : MonoBehaviour
    {
        [Header("Button")]
        [SerializeField] private GameObject nextBtn;
        [SerializeField] private GameObject prevBtn;
        [SerializeField] private GameObject changeBtn;
        [SerializeField] private GameObject confirmBtn;

        [Header("Car prefab")]
        [SerializeField] private GameObject carPrefab;

        [Header("Spawn on")]
        [SerializeField] private Transform spawnOnTransform;

        private CarData[] carDatas;
        private int selectedCarIndex = 0;
        private CarSelectUIHandler carSelectUIHandler = null;
        private bool isChangingCar = false;

        //Events
        //public event Action<CarController> OnSpawnCarChanged;

        // Start is called before the first frame update
        private void Start()
        {
            //Load the car Data
            carDatas = Resources.LoadAll<CarData>("CarData/");
            StartCoroutine(SpawnCarCO(true));
        }

        private void Update()
        {
            float input = InputManager.instance.Controllers.Player.Move.ReadValue<Vector2>().x;
            if (input > 0)
                OnNextCar();
            if (input < 0)
                OnPreviousCar();
        }

        public void OnChangeButtonPressed()
        {
            nextBtn.SetActive(true);
            prevBtn.SetActive(true);
            changeBtn.SetActive(false);
            confirmBtn.SetActive(true);
        }

        public void OnConfirmButtonPressed()
        {
            nextBtn.SetActive(false);
            prevBtn.SetActive(false);
            changeBtn.SetActive(true);
            confirmBtn.SetActive(false);
        }

        public void OnNextCar()
        {
            if (isChangingCar)
                return;

            selectedCarIndex++;
            if (selectedCarIndex > carDatas.Length - 1)
                selectedCarIndex = 0;

            StartCoroutine(SpawnCarCO(false));
        }

        public void OnPreviousCar()
        {
            if (isChangingCar)
                return;

            selectedCarIndex--;
            if (selectedCarIndex < 0)
                selectedCarIndex = carDatas.Length - 1;

            StartCoroutine(SpawnCarCO(true));
        }

        private IEnumerator SpawnCarCO(bool isCarAppearingOnRightSide)
        {
            isChangingCar = true;
            if (carSelectUIHandler != null)
                carSelectUIHandler.StartCarExitAnimation(!isCarAppearingOnRightSide);

            GameObject intantiatedCar = Instantiate(carPrefab, spawnOnTransform);
            carSelectUIHandler = intantiatedCar.GetComponent<CarSelectUIHandler>();
            carSelectUIHandler.SetupCar(carDatas[selectedCarIndex], spawnOnTransform);
            carSelectUIHandler.StartCarEntranceAnimation(isCarAppearingOnRightSide);
            //OnSpawnCarChanged?.Invoke(carDatas[selectedCarIndex].CarPrefab.GetComponent<CarController>());
            yield return new WaitForSeconds(0.4f);
            isChangingCar = false;
        }

        public ushort GetCarIDData()
        {
            return carDatas[selectedCarIndex].CarID;
        }
    }
}
