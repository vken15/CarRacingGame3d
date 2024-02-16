using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpawnCars : MonoBehaviour
{
    private int numberOfCarsSpawned = 0;

    [SerializeField] private GameObject carNameplate;

    //Temp
    [SerializeField] private GameObject carPrefab;

    // Start is called before the first frame update
    private void Start()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = spawnPoints.ToList().OrderBy(s => s.name).ToArray();

        //Load the car data
        //CarData[] carDatas = Resources.LoadAll<CarData>("CarData/");

        List<Driver> driverList = new(GameManager.instance.GetDriverList());
        driverList = driverList.OrderBy(d => d.LastRacePosition).ToList();

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
            GameObject car = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
            GameObject nameplate = Instantiate(carNameplate);
            SetSpawnCarInfo(car, nameplate, driver, i);
            numberOfCarsSpawned++;

            //foreach (CarData carData in carDatas)
            //{
            //    if (carData.CarID == selectedCarID)
            //    {
            //        GameObject car = Instantiate(carData.CarPrefab, spawnPoint.position, spawnPoint.rotation);
            //        GameObject nameplate = Instantiate(carNameplate);
            //        if (GameManager.instance.networkStatus == NetworkStatus.online)
            //        {
            //            car.GetComponent<NetworkObject>().SpawnWithOwnership(driver.NetworkId);
            //            nameplate.GetComponent<NetworkObject>().SpawnWithOwnership(driver.NetworkId);
            //            Debug.Log(driver.NetworkId + " Spawned");
            //            //SetSpawnCarInfo(car, nameplate, driver, i);
            //            SpawnCarsClientRpc(car, nameplate, driver.Name, driver.PlayerNumber, i);
            //        }
            //        else
            //        {
            //            SetSpawnCarInfo(car, nameplate, driver, i);
            //        }

            //        numberOfCarsSpawned++;
            //        break;
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

    private void SetSpawnCarInfo(GameObject car, GameObject nameplate, Driver driver, int i)
    {
        Color nameplateColor = Color.black;

        car.name = driver.Name;
        car.GetComponent<CarInputHandler>().playerNumber = driver.PlayerNumber;
        car.GetComponent<CarLapCounter>().carPosition = i + 1;
        if (driver.IsAI)
        {
            car.GetComponent<CarInputHandler>().enabled = false;
            car.tag = "AI";
            //car.GetComponent<CarAIHandler>().SetAIDifficult(driver.Difficult);
        }
        else
        {
            //if (GameManager.instance.networkStatus == NetworkStatus.online)
            //{
            //    car.GetComponent<CarInputHandler>().enabled = false;
            //}

            //car.GetComponent<CarAIHandler>().enabled = false;
            //car.GetComponent<AStarLite>().enabled = false;
            car.tag = "Player";
            //if (driver.PlayerNumber == 1)
            //{
            //    nameplateColor = Color.red;
            //}
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

    //private void SetClientSpawnCarInfo(GameObject car, GameObject nameplate, string name, int playerNumber, int i)
    //{
    //    Color nameplateColor = Color.black;

    //    car.name = name;
    //    car.GetComponent<CarInputHandler>().playerNumber = playerNumber;
    //    car.GetComponent<CarLapCounter>().CarPosition = i + 1;
    //    car.GetComponent<CarInputHandler>().enabled = false;
    //    car.GetComponent<CarAIHandler>().enabled = false;
    //    car.GetComponent<AStarLite>().enabled = false;
    //    if (playerNumber == 1)
    //    {
    //        nameplateColor = Color.red;
    //    }
    //    else if (playerNumber == 2)
    //    {
    //        nameplateColor = Color.blue;
    //    }
    //    else
    //    {
    //        nameplateColor = Color.yellow;
    //    }

    //    nameplate.GetComponent<NameplateUIHandler>().SetData(name, car.GetComponent<Transform>(), nameplateColor);
    //}

    //[ClientRpc]
    //private void SpawnCarsClientRpc(NetworkObjectReference car, NetworkObjectReference nameplate, string name, int playerNumber, int i)
    //{
    //    SetClientSpawnCarInfo((GameObject)car, (GameObject)nameplate, name, playerNumber, i);
    //}
}
