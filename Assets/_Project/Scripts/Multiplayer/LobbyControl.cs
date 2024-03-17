using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using TMPro;
using System;

namespace CarRacingGame3d
{
    public class LobbyControl : MonoBehaviour
    {
        [SerializeField] private Button createHostBtn;
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button joinBtn;
        [SerializeField] private Button refreshBtn;
        [SerializeField] private Button returnBtn;

        [SerializeField] private GameObject roomGroup;
        [SerializeField] private GameObject roomItem;
        [SerializeField] private GameObject hostCanvas;
        [SerializeField] private TMP_InputField inputName;

        [Header("Map prefab")]
        [SerializeField] private GameObject mapPrefab;

        [Header("Spawn on")]
        [SerializeField] private Transform spawnOnTransform;

        [Header("Map infomation")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text numberOfLapsText;

        [Header("In room")]
        [SerializeField] private Text roundText;
        [SerializeField] private Text playerText;

        private List<GameObject> roomList = new();
        private MapData[] mapDatas;
        private GameObject mapDisplay;

        ExampleNetworkDiscovery m_Discovery;

        NetworkManager m_NetworkManager;

        Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();

        private string iPAddress;
        private int port = 7777;

        private void Awake()
        {
            m_Discovery = FindAnyObjectByType<ExampleNetworkDiscovery>();
            m_NetworkManager = FindAnyObjectByType<NetworkManager>();

            //Load the map Data
            mapDatas = Resources.LoadAll<MapData>("MapData/");

            inputName.text = "Tester";

            GameManager.instance.ClearDriverList();

            string myAddressLocal = "";
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    myAddressLocal = ip.ToString();
                    break;
                }
            }
            if (myAddressLocal == "") myAddressLocal = "127.0.0.1";

            refreshBtn.onClick.AddListener(() =>
            {
                discoveredServers.Clear();
                foreach (GameObject room in roomList)
                {
                    Destroy(room);
                }
                m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
                joinBtn.interactable = false;
                mapNameText.text = "";
                difficultyText.text = "";
                numberOfLapsText.text = "";
                roundText.text = "";
                playerText.text = "";
                Destroy(mapDisplay);
                //ClientSearch();
            });

            createHostBtn.onClick.AddListener(() =>
            {
                ConnectionManager.instance.StartHostIp(inputName.text, myAddressLocal, port);
                /*
                UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                transport.SetConnectionData(myAddressLocal, 7777);
                if (m_NetworkManager.StartHost())
                {
                    m_Discovery.StartServer();
                    SceneTransitionHandler.Instance.RegisterCallbacks();
                    SceneTransitionHandler.Instance.SwitchScene("Room");
                }
                else
                {
                    Debug.LogError("Failed to start host.");
                };
                */
            });

            joinBtn.onClick.AddListener(() =>
            {
                ConnectionManager.instance.StartClientIp(inputName.text, iPAddress, port);
                /*
                if (!m_NetworkManager.StartClient())
                {
                    Debug.LogError("Failed to start client.");
                }
                */
            });

            hostBtn.onClick.AddListener(() => hostCanvas.SetActive(true));

            returnBtn.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("MainMenu");
            });

            m_Discovery.OnServerFound += OnServerFound;
        }

        private void Start()
        {
            m_Discovery.StartClient();
            m_Discovery.ClientBroadcast(new DiscoveryBroadcastData());
            //ClientSearch();
        }

        void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
        {
            Debug.Log("lobby found server");
            discoveredServers[sender.Address] = response;

            GameObject item = Instantiate(roomItem, roomGroup.transform);
            item.GetComponent<RoomItemUI>().SetRoomName(response.ServerName);
            item.GetComponent<RoomItemUI>().SetPlayerNumber(response.CurrentPlayer, response.MaxPlayer);
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                //UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                //transport.SetConnectionData(sender.Address.ToString(), response.Port);
                mapDisplay = Instantiate(mapPrefab, spawnOnTransform);
                mapDisplay.GetComponent<Button>().enabled = false;
                foreach (var mapData in mapDatas)
                {
                    if (mapData.MapID == response.MapId)
                    {
                        GameManager.instance.map = mapData;
                        mapDisplay.GetComponent<Image>().sprite = mapData.MapUISprite;
                        mapNameText.text = mapData.MapName;
                        difficultyText.text = "Difficulty: " + mapData.Difficulty.ToString();
                        numberOfLapsText.text = "Lap: " + mapData.NumberOfLaps.ToString();
                        break;
                    }
                }
                GameManager.instance.currentRound = response.CurRound;
                GameManager.instance.maxRound = response.MaxRound;
                ConnectionManager.instance.MaxConnectedPlayers = response.MaxPlayer;
                roundText.text = $"Round: {response.CurRound}/{response.MaxRound}";
                playerText.text = $"Player: {response.CurrentPlayer}/{response.MaxPlayer}";
                iPAddress = sender.Address.ToString();
                port = response.Port;
                joinBtn.interactable = true;
            });
            roomList.Add(item);
        }
        /*
        void ClientSearch()
        {
            Debug.Log("Search");
            foreach (var discoveredServer in discoveredServers)
            {
                GameObject item = Instantiate(roomItem, roomGroup.transform);

                item.GetComponent<Button>().onClick.AddListener(() =>
                {
                    UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                    transport.SetConnectionData(discoveredServer.Key.ToString(), discoveredServer.Value.Port);
                    joinBtn.interactable = true;
                });
                Debug.Log("Room");
                roomList.Add(item);

                //if (GUILayout.Button($"{discoveredServer.Value.ServerName}[{discoveredServer.Key.ToString()}]"))
            }
        }
        */
        private void OnDestroy()
        {
            if (m_Discovery.IsRunning && !m_NetworkManager.IsServer)
            {
                m_Discovery.StopDiscovery();
            }
            m_Discovery.OnServerFound -= OnServerFound;
        }
    }
}