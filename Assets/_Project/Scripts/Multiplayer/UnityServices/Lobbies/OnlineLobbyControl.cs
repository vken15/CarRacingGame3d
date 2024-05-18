using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CarRacingGame3d.UnityServices;
using Unity.Services.Core;
using TMPro;

namespace CarRacingGame3d
{
    public class OnlineLobbyControl : MonoBehaviour
    {
        [SerializeField] private Button createHostBtn;
        [SerializeField] private Button hostBtn;
        [SerializeField] private Button joinBtn;
        [SerializeField] private Button refreshBtn;
        [SerializeField] private Button returnBtn;
        [SerializeField] private Button codeBtn;
        [SerializeField] private Button connectBtn;
        [SerializeField] private Button cancelBtn;

        [SerializeField] private GameObject roomGroup;
        [SerializeField] private GameObject roomItem;
        [SerializeField] private GameObject hostCanvas;
        [SerializeField] private GameObject codeCanvas;
        [SerializeField] private TMP_InputField inputName;
        [SerializeField] private TMP_InputField inputCode;

        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] GameObject loadingSpinner;
        [SerializeField] TMP_Text emptyLobbyListLabel;

        [Header("Map prefab")]
        [SerializeField] private GameObject mapPrefab;

        [Header("Spawn on")]
        [SerializeField] private Transform spawnOnTransform;

        [Header("Room infomation")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text numberOfLapsText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text playerText;
        private MapData[] mapDatas;
        private GameObject mapDisplay;
        readonly List<RoomItemUI> lobbyListItems = new();
        LocalLobby lobbyData;
        UpdateRunner updateRunner;

        const string defaultLobbyName = "CRGServer";

        private void Awake()
        {
            updateRunner = FindAnyObjectByType<UpdateRunner>();

            //Load the map Data
            mapDatas = Resources.LoadAll<MapData>("MapData/");

            GameManager.instance.ClearDriverList();

            LocalLobbyUser.Instance.DisplayName = ProfileManager.Instance.AvailableProfile;
            inputName.text = ProfileManager.Instance.AvailableProfile;

            refreshBtn.onClick.AddListener(() =>
            {
                QueryLobbiesRequest(true);
                joinBtn.interactable = false;
                mapNameText.text = "";
                difficultyText.text = "";
                numberOfLapsText.text = "";
                roundText.text = "";
                playerText.text = "";
                Destroy(mapDisplay);
            });

            createHostBtn.onClick.AddListener(() =>
            {
                CreateLobbyRequest(inputName.text, false);
            });

            joinBtn.onClick.AddListener(() =>
            {
                JoinLobbyRequest(lobbyData.LobbyID, lobbyData.LobbyCode);
            });

            hostBtn.onClick.AddListener(() => hostCanvas.SetActive(true));

            returnBtn.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("MainMenu");
            });

            inputName.onValueChanged.AddListener((s) => OnInputNameChanged(s));

            codeBtn.onClick.AddListener(() => codeCanvas.SetActive(true));

            connectBtn.onClick.AddListener(() =>
            {
                JoinLobbyRequest(null, inputCode.text);
            });

            cancelBtn.onClick.AddListener(() => codeCanvas.SetActive(false));

            LobbyServiceFacade.Instance.OnLobbyListFetched += UpdateUI;
            updateRunner.Subscribe(PeriodicRefresh, 10f);
        }

        private void OnDestroy()
        {
            LobbyServiceFacade.Instance.OnLobbyListFetched -= UpdateUI;
            updateRunner.Unsubscribe(PeriodicRefresh);
        }

        void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - lobbyListItems.Count;

            for (int i = 0; i < delta; i++)
            {
                lobbyListItems.Add(CreateLobbyListItem());
            }

            for (int i = 0; i < lobbyListItems.Count; i++)
            {
                lobbyListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        void UpdateUI(LobbyListFetchedMessage message)
        {
            Debug.Log($"Number of room: {message.LocalLobbies.Count}");
            EnsureNumberOfActiveUISlots(message.LocalLobbies.Count);

            for (var i = 0; i < message.LocalLobbies.Count; i++)
            {
                var localLobby = message.LocalLobbies[i];
                lobbyListItems[i].SetData(localLobby);
            }

            if (message.LocalLobbies.Count == 0)
            {
                emptyLobbyListLabel.enabled = true;
            }
            else
            {
                emptyLobbyListLabel.enabled = false;
            }
        }

        RoomItemUI CreateLobbyListItem()
        {
            var listItem = Instantiate(roomItem, roomGroup.transform)
                .GetComponent<RoomItemUI>();
            listItem.gameObject.SetActive(true);
            listItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                lobbyData = listItem.GetData();

                if (mapDisplay != null)
                    Destroy(mapDisplay);

                mapDisplay = Instantiate(mapPrefab, spawnOnTransform);
                mapDisplay.GetComponent<Button>().enabled = false;
                foreach (var mapData in mapDatas)
                {
                    if (mapData.MapID == lobbyData.MapId)
                    {
                        GameManager.instance.map = mapData;
                        mapDisplay.GetComponent<Image>().sprite = mapData.MapUISprite;
                        mapNameText.text = mapData.MapName;
                        difficultyText.text = "Difficulty: " + mapData.Difficulty.ToString();
                        numberOfLapsText.text = "Lap: " + mapData.NumberOfLaps.ToString();
                        break;
                    }
                }
                GameManager.instance.currentRound = lobbyData.CurRound;
                GameManager.instance.maxRound = lobbyData.MaxRound;
                ConnectionManager.instance.MaxConnectedPlayers = lobbyData.MaxPlayerCount;
                roundText.text = $"Round: {lobbyData.CurRound}/{lobbyData.MaxRound}";
                playerText.text = $"Player: {lobbyData.PlayerCount}/{lobbyData.MaxPlayerCount}";
                joinBtn.interactable = true;
            });
            return listItem;
        }

        public async void CreateLobbyRequest(string lobbyName, bool isPrivate)
        {
            // before sending request to lobby service, populate an empty lobby name, if necessary
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = defaultLobbyName;
            }
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await AuthenticationServiceFacade.Instance.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var (Success, Lobby) = await LobbyServiceFacade.Instance.TryCreateLobbyAsync(lobbyName, ConnectionManager.instance.MaxConnectedPlayers, isPrivate);

            if (Success)
            {
                LocalLobbyUser.Instance.IsHost = true;
                LobbyServiceFacade.Instance.SetRemoteLobby(Lobby);

                Debug.Log($"Created lobby with ID: {LocalLobby.Instance.LobbyID} and code {LocalLobby.Instance.LobbyCode}");
                ConnectionManager.instance.StartHostLobby(LocalLobbyUser.Instance.DisplayName);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void QueryLobbiesRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            if (blockUI)
            {
                BlockUIWhileLoadingIsInProgress();
            }

            bool playerIsAuthorized = await AuthenticationServiceFacade.Instance.EnsurePlayerIsAuthorized();

            if (blockUI && !playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await LobbyServiceFacade.Instance.RetrieveAndPublishLobbyListAsync();

            if (blockUI)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void JoinLobbyRequest(string lobbyId, string lobbyCode)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await AuthenticationServiceFacade.Instance.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var (Success, Lobby) = await LobbyServiceFacade.Instance.TryJoinLobbyAsync(lobbyId, lobbyCode);

            if (Success)
            {
                OnJoinedLobby(Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnJoinedLobby(Unity.Services.Lobbies.Models.Lobby remoteLobby)
        {
            LobbyServiceFacade.Instance.SetRemoteLobby(remoteLobby);

            Debug.Log($"Joined lobby with code: {LocalLobby.Instance.LobbyCode}, Internal Relay Join Code{LocalLobby.Instance.RelayJoinCode}");
            ConnectionManager.instance.StartClientLobby(LocalLobbyUser.Instance.DisplayName);
        }

        void PeriodicRefresh(float _)
        {
            //this is a soft refresh without needing to lock the UI and such
            QueryLobbiesRequest(false);
        }

        void BlockUIWhileLoadingIsInProgress()
        {
            canvasGroup.interactable = false;
            loadingSpinner.SetActive(true);
        }

        void UnblockUIAfterLoadingIsComplete()
        {
            //this callback can happen after we've already switched to a different scene
            //in that case the canvas group would be null
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                loadingSpinner.SetActive(false);
            }
        }

        void OnInputNameChanged(string name)
        {
            LocalLobbyUser.Instance.DisplayName = name;
        }
    }
}
