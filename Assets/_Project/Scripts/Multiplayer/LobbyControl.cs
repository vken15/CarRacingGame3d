using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class LobbyControl : MonoBehaviour
    {
        [SerializeField] private Button createHostBtn;
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button joinBtn;
        [SerializeField] private Button refreshBtn;
        [SerializeField] private Button joinIPBtn;
        [SerializeField] private Button returnBtn;
        [SerializeField] private Button connectIPBtn;
        [SerializeField] private Button cancelJoinIPBtn;

        [SerializeField] private GameObject roomGroup;
        [SerializeField] private GameObject roomItem;
        [SerializeField] private GameObject hostCanvas;
        [SerializeField] private Canvas joinIPCanvas;
        [SerializeField] private TMP_InputField inputName;
        [SerializeField] private TMP_InputField inputIP;
        [SerializeField] private TMP_InputField inputPort;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ConnectingUIHandler connectingUI;
        [SerializeField] private TMP_Text emptyLobbyListLabel;

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

        private readonly List<GameObject> roomList = new();
        private MapData[] mapDatas;
        private GameObject mapDisplay;

        ExampleNetworkDiscovery discovery;
        UpdateRunner updateRunner;
        NetworkManager networkManager;
        readonly Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();

        private string iPAddress;
        private int port = 7777;

        private void Awake()
        {
            discovery = FindAnyObjectByType<ExampleNetworkDiscovery>();
            networkManager = FindAnyObjectByType<NetworkManager>();
            updateRunner = FindAnyObjectByType<UpdateRunner>();

            //Load the map Data
            mapDatas = Resources.LoadAll<MapData>("MapData/");

            inputName.text = ProfileManager.Instance.AvailableProfile;
            inputPort.text = port.ToString();

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
                OnRefresh(0);
            });
            
            createHostBtn.onClick.AddListener(() =>
            {
                ConnectionManager.instance.StartHostIp(ProfileManager.Instance.AvailableProfile, myAddressLocal, port);
            });

            joinBtn.onClick.AddListener(() =>
            {
                ConnectionManager.instance.StartClientIp(ProfileManager.Instance.AvailableProfile, iPAddress, port);
            });

            hostBtn.onClick.AddListener(() => hostCanvas.SetActive(true));

            returnBtn.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("MainMenu");
            });

            joinIPBtn.onClick.AddListener(() =>
            {
                joinIPCanvas.enabled = true;
            });

            connectIPBtn.onClick.AddListener(() =>
            {
                iPAddress = string.IsNullOrEmpty(inputIP.text.ToString()) ? "127.0.0.1" : inputIP.text;
                int.TryParse(inputPort.text, out port);
                if (port < 0)
                {
                    port = 7777;
                }
                ConnectionManager.instance.StartClientIp(ProfileManager.Instance.AvailableProfile, iPAddress, port);
                canvasGroup.interactable = false;
                hostCanvas.SetActive(false);
                connectingUI.ShowConnecting();
            });

            cancelJoinIPBtn.onClick.AddListener(() =>
            {
                joinIPCanvas.enabled = false;
                canvasGroup.interactable = true;
            });

            discovery.OnServerFound += OnServerFound;
            updateRunner.Subscribe(OnRefresh, 10f);
        }

        private void Start()
        {
            discovery.StartClient();
            ShowEmptyLabel();
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
                if (mapDisplay != null)
                    Destroy(mapDisplay);

                mapDisplay = Instantiate(mapPrefab, spawnOnTransform);
                mapDisplay.GetComponent<Button>().enabled = false;
                foreach (var mapData in mapDatas)
                {
                    if (mapData.MapID == response.MapId)
                    {
                        GameManager.instance.map = mapData;
                        GameManager.instance.SetNumberOfLaps(mapData.NumberOfLaps);
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
            ShowEmptyLabel();
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

        private void ShowEmptyLabel()
        {
            if (discoveredServers.Count == 0)
            {
                emptyLobbyListLabel.enabled = true;
            }
            else
            {
                emptyLobbyListLabel.enabled = false;
            }
        }

        private void OnRefresh(float _)
        {
            discoveredServers.Clear();
            foreach (GameObject room in roomList)
            {
                Destroy(room);
            }
            discovery.ClientBroadcast(new DiscoveryBroadcastData());
            joinBtn.interactable = false;
            mapNameText.text = "";
            difficultyText.text = "";
            numberOfLapsText.text = "";
            roundText.text = "";
            playerText.text = "";
            Destroy(mapDisplay);
            ShowEmptyLabel();
            //ClientSearch();
        }
        
        private void OnDestroy()
        {
            if (discovery.IsRunning && !networkManager.IsServer)
            {
                discovery.StopDiscovery();
            }
            discovery.OnServerFound -= OnServerFound;
            updateRunner.Unsubscribe(OnRefresh);
        }

        public void OnEndEdit(string name)
        {
            if (!ProfileManager.Instance.AvailableProfile.Equals(name))
            {
                ProfileManager.Instance.CreateProfile(name);
                ProfileManager.Instance.Profile = name;
            }
        }
    }
}