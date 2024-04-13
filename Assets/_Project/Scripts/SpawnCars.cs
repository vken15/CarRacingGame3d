using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class SpawnCars : NetworkBehaviour
    {
        [SerializeField] CameraFollow minimapCameraFollow;

        private int numberOfCarsSpawned = 0;

        private readonly Color[] playerColors = { Color.black, Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.gray, Color.cyan, Color.black };

        // Start is called before the first frame update
        private void Start()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.offline || 
                (IsServer && GameManager.instance.networkStatus == NetworkStatus.online))
                Spawn();
        }

        public void Spawn()
        {
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints = spawnPoints.ToList().OrderBy(s => s.name).ToArray();

            //Load the car data
            CarData[] carDatas = Resources.LoadAll<CarData>("CarData/");

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
                ushort selectedCarID = driver.CarID;
                if (driver.IsAI && GameManager.instance.networkStatus == NetworkStatus.online)
                {
                    selectedCarID = (ushort)Random.Range(1, carDatas.Length);
                }

                //Find the player cars prefab
                foreach (CarData carData in carDatas)
                {
                    if (carData.CarID == selectedCarID)
                    {
                        GameObject car = Instantiate(carData.CarPrefab, spawnPoint.position, spawnPoint.rotation);
                        if (GameManager.instance.networkStatus == NetworkStatus.online)
                        {
                            car.GetComponent<NetworkObject>().SpawnWithOwnership(driver.ClientId, true);
                            SpawnCarsClientRpc(car, driver.Name, driver.PlayerNumber, i, driver.IsAI);
                        }
                        else
                        {
                            SetSpawnCarInfo(car, driver, i);
                        }

                        numberOfCarsSpawned++;
                        break;
                    }
                }
                //Remove the spawned driver
                driverList.Remove(driver);
            }
        }

        public int GetNumberOfCarsSpawned()
        {
            return numberOfCarsSpawned;
        }

        private void SetSpawnCarInfo(GameObject car, Driver driver, int i)
        {
            Color nameplateColor = Color.black;
            car.name = driver.Name;
            car.GetComponent<CarInputHandler>().playerNumber = driver.PlayerNumber;
            car.GetComponent<CarInputHandler>().SetMinimapColor();
            car.GetComponent<CarLapCounter>().carPosition = i + 1;
            if (driver.IsAI)
            {
                car.GetComponent<CarInputHandler>().enabled = false;
                car.tag = "AI";
                //car.GetComponent<CarAIHandler>().SetAIDifficult(driver.Difficult);
            }
            else
            {
                car.GetComponent<CarAIHandler>().enabled = false;
                //car.GetComponent<AStarLite>().enabled = false;
                car.tag = "Player";
                nameplateColor = playerColors[driver.PlayerNumber];
                if (minimapCameraFollow != null)
                    minimapCameraFollow.SetTarget(car.transform);
            }

            car.GetComponentInChildren<NameplateUIHandler>().SetData(driver.Name, nameplateColor);
        }

        private void SetClientSpawnCarInfo(GameObject car, string name, int playerNumber, int i, bool IsAI)
        {
            car.name = name;
            car.GetComponent<CarInputHandler>().playerNumber = playerNumber;
            car.GetComponent<CarInputHandler>().SetMinimapColor();
            car.GetComponent<CarLapCounter>().carPosition = i + 1;
            Color nameplateColor = playerColors[playerNumber];
            car.GetComponentInChildren<NameplateUIHandler>().SetData(name, nameplateColor);
            if (IsAI)
            {
                car.GetComponent<CarAIHandler>().enabled = true;
                car.GetComponent<CarInputHandler>().enabled = false;
            } else
            {
                car.GetComponent<CarAIHandler>().enabled = false;
                if (car.GetComponent<NetworkObject>().IsOwner)
                {
                    car.GetComponentInChildren<NameplateUIHandler>().gameObject.SetActive(false);
                    FindObjectOfType<SpecialFuelUIHandler>().controller = car.GetComponent<CarController>();

                    if (minimapCameraFollow != null)
                        minimapCameraFollow.SetTarget(car.transform);
                }
            }
        }

        [ClientRpc]
        private void SpawnCarsClientRpc(NetworkObjectReference car, string name, int playerNumber, int i, bool isAI)
        {
            SetClientSpawnCarInfo((GameObject)car, name, playerNumber, i, isAI);
        }
    }
}