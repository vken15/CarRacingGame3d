using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class SpawnCars : NetworkBehaviour
    {
        private int numberOfCarsSpawned = 0;

        [SerializeField] private GameObject carNameplate;
        [SerializeField] private GameObject carCamera;

        //Temp
        [SerializeField] private GameObject carPrefab;

        // Start is called before the first frame update
        private void Start()
        {
            //if (GameManager.instance.networkStatus == NetworkStatus.offline)
            Spawn();
        }

        public void Spawn()
        {
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints = spawnPoints.ToList().OrderBy(s => s.name).ToArray();

            //Load the car data
            //CarData[] carDatas = Resources.LoadAll<CarData>("CarData/");

            List<Driver> driverList = new(GameManager.instance.GetDriverList());
            driverList = driverList.OrderBy(d => d.LastRacePosition).ToList();

            //print(driverList.Count);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (driverList.Count == 0)
                {
                    return;
                }

                Transform spawnPoint = spawnPoints[i].transform;

                Driver driver = driverList[0];

                //int selectedCarID = driver.CarID;

                //Find the player cars prefab
                //foreach (CarData carData in carDatas)
                //{
                //    if (carData.CarID == selectedCarID)
                //    {
                //GameObject car = Instantiate(carData.CarPrefab, spawnPoint.position, spawnPoint.rotation);
                GameObject car = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
                GameObject nameplate = Instantiate(carNameplate);
                GameObject camera = Instantiate(carCamera);
                if (GameManager.instance.networkStatus == NetworkStatus.online)
                {
                    car.GetComponent<NetworkObject>().SpawnWithOwnership(driver.NetworkId);
                    camera.GetComponent<NetworkObject>().SpawnWithOwnership(driver.NetworkId);
                    nameplate.GetComponent<NetworkObject>().SpawnWithOwnership(driver.NetworkId);
                    Debug.Log(driver.NetworkId + " Spawned");
                    //SetSpawnCarInfo(car, nameplate, driver, i);

                    SpawnCarsClientRpc(car, nameplate, camera, driver.Name, driver.PlayerNumber, i);
                }
                else
                {
                    SetSpawnCarInfo(car, nameplate, camera, driver, i);
                }

                numberOfCarsSpawned++;
                //break;
                //    }
                //}
                //Remove the spawned driver
                driverList.Remove(driver);
            }
        }

        public int GetNumberOfCarsSpawned()
        {
            return numberOfCarsSpawned;
        }

        private void SetSpawnCarInfo(GameObject car, GameObject nameplate, GameObject camera, Driver driver, int i)
        {
            Color nameplateColor = Color.black;

            car.name = driver.Name;
            car.GetComponent<CarInputHandler>().playerNumber = driver.PlayerNumber;
            car.GetComponentInChildren<CarLapCounter>().carPosition = i + 1;
            if (driver.IsAI)
            {
                car.GetComponent<CarInputHandler>().enabled = false;
                car.tag = "AI";
                //car.GetComponent<CarAIHandler>().SetAIDifficult(driver.Difficult);
            }
            else
            {
                car.GetComponent<CarAIHandler>().enabled = false;
                camera.GetComponent<CameraFollow>().SetTarget(car.transform);
                //car.GetComponent<AStarLite>().enabled = false;
                car.tag = "Player";
                if (driver.PlayerNumber == 1)
                {
                    camera.GetComponent<Camera>().depth = 1;
                    nameplateColor = Color.red;
                }
                //else if (driver.PlayerNumber == 2)
                //{
                //    nameplateColor = Color.blue;
                //}
                //else
                //{
                //    nameplateColor = Color.yellow;
                //}
            }

            //nameplate.GetComponent<NameplateUIHandler>().SetData(driver.Name, car.GetComponent<Transform>(), nameplateColor);
        }

        private void SetClientSpawnCarInfo(GameObject car, GameObject nameplate, GameObject camera, string name, int playerNumber, int i)
        {
            Color nameplateColor = Color.black;

            car.name = name;
            car.GetComponent<CarInputHandler>().playerNumber = playerNumber;
            car.GetComponentInChildren<CarLapCounter>().carPosition = i + 1;
            car.GetComponent<CarAIHandler>().enabled = false;
            camera.GetComponent<CameraFollow>().SetTarget(car.transform);

            if (car.GetComponent<NetworkObject>().IsOwner)
                camera.GetComponent<Camera>().depth = 1;

            if (playerNumber == 1)
            {
                nameplateColor = Color.red;
            }
            else if (playerNumber == 2)
            {
                nameplateColor = Color.blue;
            }
            else
            {
                nameplateColor = Color.yellow;
            }

            //nameplate.GetComponent<NameplateUIHandler>().SetData(name, car.GetComponent<Transform>(), nameplateColor);
        }

        [ClientRpc]
        private void SpawnCarsClientRpc(NetworkObjectReference car, NetworkObjectReference nameplate, NetworkObjectReference camera, string name, int playerNumber, int i)
        {
            SetClientSpawnCarInfo((GameObject)car, (GameObject)nameplate, (GameObject)camera, name, playerNumber, i);
        }
    }
}