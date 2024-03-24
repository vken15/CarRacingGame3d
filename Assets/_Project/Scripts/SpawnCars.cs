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

                //Find the player cars prefab
                foreach (CarData carData in carDatas)
                {
                    if (carData.CarID == selectedCarID)
                    {
                        GameObject car = Instantiate(carData.CarPrefab, spawnPoint.position, spawnPoint.rotation);
                        if (GameManager.instance.networkStatus == NetworkStatus.online)
                        {
                            car.GetComponent<NetworkObject>().SpawnWithOwnership(driver.ClientId, true);
                            Debug.Log(driver.ClientId + " Spawned");
                            //SetSpawnCarInfo(car, nameplate, driver, i);

                            SpawnCarsClientRpc(car, driver.Name, driver.PlayerNumber, i);
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

        private void SetClientSpawnCarInfo(GameObject car, string name, int playerNumber, int i)
        {
            car.name = name;
            car.GetComponent<CarInputHandler>().playerNumber = playerNumber;
            car.GetComponent<CarInputHandler>().SetMinimapColor();
            car.GetComponent<CarLapCounter>().carPosition = i + 1;
            car.GetComponent<CarAIHandler>().enabled = false;
            Color nameplateColor = playerColors[playerNumber];
            car.GetComponentInChildren<NameplateUIHandler>().SetData(name, nameplateColor);

            if (car.GetComponent<NetworkObject>().IsOwner)
            {
                car.GetComponentInChildren<NameplateUIHandler>().gameObject.SetActive(false);
                FindObjectOfType<SpecialFuelUIHandler>().controller = car.GetComponent<CarController>();

                if (minimapCameraFollow != null)
                    minimapCameraFollow.SetTarget(car.transform);
            }
        }

        [ClientRpc]
        private void SpawnCarsClientRpc(NetworkObjectReference car, string name, int playerNumber, int i)
        {
            SetClientSpawnCarInfo((GameObject)car, name, playerNumber, i);
        }
    }
}