using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRacingGame3d
{
    public class SceneTransitionHandler : NetworkBehaviour
    {
        static public SceneTransitionHandler Instance { get; protected set; }

        [SerializeField]
        ClientLoadingScreen clientLoadingScreen;

        [SerializeField]
        LoadingProgressManager loadingProgressManager;

        [SerializeField]
        private string DefaultMainMenu = "MainMenu";

        [HideInInspector]
        public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
        [HideInInspector]
        public event ClientLoadedSceneDelegateHandler OnClientLoadedScene;

        bool IsNetworkSceneManagementEnabled => NetworkManager != null && NetworkManager.SceneManager != null && NetworkManager.NetworkConfig.EnableSceneManagement;

        bool isInitialized;

        private int numberOfClientLoaded;

        private void Awake()
        {
            if (Instance != this && Instance != null)
            {
                Destroy(Instance.gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            SceneManager.LoadScene(DefaultMainMenu);
            SceneManager.sceneLoaded += OnSceneLoaded;
            NetworkManager.OnServerStarted += OnNetworkingSessionStarted;
            NetworkManager.OnClientStarted += OnNetworkingSessionStarted;
            NetworkManager.OnServerStopped += OnNetworkingSessionEnded;
            NetworkManager.OnClientStopped += OnNetworkingSessionEnded;
        }

        public override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (NetworkManager != null)
            {
                NetworkManager.OnServerStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnClientStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnServerStopped -= OnNetworkingSessionEnded;
                NetworkManager.OnClientStopped -= OnNetworkingSessionEnded;
            }
            base.OnDestroy();
        }

        public void SwitchScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (useNetworkSceneManager)
            {
                if (IsSpawned && IsNetworkSceneManagementEnabled && !NetworkManager.ShutdownInProgress)
                {
                    if (NetworkManager.IsServer)
                    {
                        numberOfClientLoaded = 0;
                        NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                    }
                }
            }
            else
            {
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (loadSceneMode == LoadSceneMode.Single)
                {
                    clientLoadingScreen.StartLoadingScreen(sceneName);
                    loadingProgressManager.LocalLoadOperation = loadOperation;
                }
            }
        }

        public bool AllClientsAreLoaded()
        {
            return numberOfClientLoaded >= NetworkManager.ConnectedClients.Count;
        }

        void OnNetworkingSessionStarted()
        {
            // This prevents this to be called twice on a host, which receives both OnServerStarted and OnClientStarted callbacks
            if (!isInitialized)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                }

                isInitialized = true;
            }
        }

        void OnNetworkingSessionEnded(bool unused)
        {
            if (isInitialized)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                }

                isInitialized = false;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!IsSpawned || NetworkManager.ShutdownInProgress)
            {
                clientLoadingScreen.StopLoadingScreen();
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    if (NetworkManager.IsClient)
                    {
                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                        {
                            clientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName);
                            loadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                        }
                        else
                        {
                            clientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName);
                            loadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                        }
                    }
                    break;
                case SceneEventType.LoadEventCompleted:
                    if (NetworkManager.IsClient)
                    {
                        clientLoadingScreen.StopLoadingScreen();
                    }
                    break;
                case SceneEventType.Synchronize:
                    {
                        if (NetworkManager.IsClient && !NetworkManager.IsHost)
                        {
                            if (NetworkManager.SceneManager.ClientSynchronizationMode == LoadSceneMode.Single)
                            {
                                UnloadAdditiveScenes();
                            }
                        }
                        break;
                    }
                case SceneEventType.SynchronizeComplete:
                    if (NetworkManager.IsServer)
                    {
                        StopLoadingScreenClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sceneEvent.ClientId } } });
                    }
                    break;
                case SceneEventType.LoadComplete:
                    if (NetworkManager.IsServer)
                    {
                        numberOfClientLoaded += 1;
                        OnClientLoadedScene?.Invoke(sceneEvent.ClientId);
                    }
                    break;
            }
        }

        void UnloadAdditiveScenes()
        {
            var activeScene = SceneManager.GetActiveScene();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != activeScene)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        [ClientRpc]
        void StopLoadingScreenClientRpc(ClientRpcParams clientRpcParams = default)
        {
            clientLoadingScreen.StopLoadingScreen();
        }
    }
}