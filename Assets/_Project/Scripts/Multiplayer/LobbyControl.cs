using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

namespace CarRacingGame3d
{
    public class LobbyControl : MonoBehaviour
    {
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button joinBtn;
        [SerializeField] private Button refreshBtn;

        [SerializeField] private GameObject roomGroup;
        [SerializeField] private GameObject roomItem;

        private List<GameObject> roomList = new();

        ExampleNetworkDiscovery m_Discovery;

        NetworkManager m_NetworkManager;

        Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();

        private void Awake()
        {
            m_Discovery = FindAnyObjectByType<ExampleNetworkDiscovery>();
            m_NetworkManager = FindAnyObjectByType<NetworkManager>();

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
                //ClientSearch();
            });

            hostBtn.onClick.AddListener(() =>
            {
                UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                transport.SetConnectionData(myAddressLocal, 7777);
                if (m_NetworkManager.StartHost())
                {
                    m_Discovery.StartServer();
                    SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
                    SceneTransitionHandler.sceneTransitionHandler.SwitchScene("Room");
                }
                else
                {
                    Debug.LogError("Failed to start host.");
                };
            });

            joinBtn.onClick.AddListener(() =>
            {
                if (!m_NetworkManager.StartClient())
                {
                    Debug.LogError("Failed to start client.");
                }
                GameManager.instance.ClearDriverList();
            });

            m_Discovery.OnServerFound += OnServerFound;

            //Test
            //MapData[] mapDatas = Resources.LoadAll<MapData>("MapData/");
            //GameManager.instance.SetMap(mapDatas[0]);
            GameManager.instance.SetNumberOfLaps(2);
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
            
            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
                transport.SetConnectionData(sender.Address.ToString(), response.Port);
                joinBtn.interactable = true;
            });
            Debug.Log("Button added");
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
            if (m_Discovery.IsRunning)
            {
                m_Discovery.StopDiscovery();
            }
            m_Discovery.OnServerFound -= OnServerFound;
        }
    }
}