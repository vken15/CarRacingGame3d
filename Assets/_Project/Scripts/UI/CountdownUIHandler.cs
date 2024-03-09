using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class CountdownUIHandler : NetworkBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        //[SerializeField] private AudioSource countdownAudioSource;
        //[SerializeField] private AudioSource bgmAudioSource;

        [SerializeField]
        [Tooltip("Time Remaining until the game starts")]
        private float m_DelayedStartTime = 3.0f;

        private bool m_ClientGameOver;
        private bool m_ClientGameStarted;
        private bool m_ClientStartCountdown;

        private bool m_ReplicatedTimeSent = false;
        private float m_TimeRemaining;

        private NetworkVariable<bool> m_CountdownStarted = new(false);
        private NetworkVariable<bool> hasGameStarted { get; } = new(false);
        private NetworkVariable<bool> raceOverCountdown { get; } = new(false);
        private NetworkVariable<bool> isGameOver { get; } = new(false);

        private bool m_raceOverCountdown = false;

        private void Awake()
        {
            countdownText.text = "";

            if (IsServer)
            {
                hasGameStarted.Value = false;

                //Set our time remaining locally
                m_TimeRemaining = m_DelayedStartTime;

                //Set for server side
                m_ReplicatedTimeSent = false;
            }
            else
            {
                //We do a check for the client side value upon instantiating the class (should be zero)
                Debug.LogFormat("Client side we started with a timer value of {0}", m_TimeRemaining);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient && !IsServer)
            {
                m_ClientGameOver = false;
                m_ClientStartCountdown = false;
                m_ClientGameStarted = false;

                m_CountdownStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    m_ClientStartCountdown = newValue;
                    Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
                };

                hasGameStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    m_ClientGameStarted = newValue;
                    GameManager.instance.OnRaceStart();
                    m_TimeRemaining = 0.0f;
                    //bgmAudioSource.clip = GameManager.instance.BGM;
                    //bgmAudioSource.Play();
                    countdownText.gameObject.SetActive(!m_ClientGameStarted);
                    Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
                };

                raceOverCountdown.OnValueChanged += (oldValue, newValue) =>
                {
                    m_raceOverCountdown = newValue;
                    countdownText.gameObject.SetActive(true);
                    Debug.LogFormat("Client side we were notified the game end count down state was {0}", newValue);
                };

                isGameOver.OnValueChanged += (oldValue, newValue) =>
                {
                    m_ClientGameOver = newValue;
                    GameManager.instance.OnRaceOver();
                    countdownText.gameObject.SetActive(false);
                    Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
                };
            }

            //Both client and host/server will set the scene state to "ingame" which places the PlayerControl into the SceneTransitionHandler.SceneStates.INGAME
            //and in turn makes the players visible and allows for the players to be controlled.
            SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Ingame);

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }

            base.OnNetworkSpawn();
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
                StartCoroutine(CountDownCO(3, false));
        }

        private void Update()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.online)
            {
                if (IsCurrentGameOver()) return;

                if (!raceOverCountdown.Value && GameManager.instance.GetGameState() == GameStates.raceOverCountDown && IsServer)
                {
                    print("RUN");
                    raceOverCountdown.Value = true;
                    m_TimeRemaining = m_DelayedStartTime = 10.0f;
                    m_ReplicatedTimeSent = false;
                    countdownText.gameObject.SetActive(true);
                }

                UpdateGameTimer();
            }
            else
            {
                if (!m_raceOverCountdown && GameManager.instance.GetGameState() == GameStates.raceOverCountDown)
                {
                    m_raceOverCountdown = true;
                    StartCoroutine(CountDownCO(10, true));
                }
            }
        }

        private bool HasGameStarted()
        {
            if (IsServer)
                return hasGameStarted.Value;
            return m_ClientGameStarted;
        }

        private bool HasRaceOverCountDown()
        {
            if (IsServer)
                return raceOverCountdown.Value;
            return m_raceOverCountdown;
        }

        private bool IsCurrentGameOver()
        {
            if (IsServer)
                return isGameOver.Value;
            return m_ClientGameOver;
        }

        private bool ShouldStartCountDown()
        {
            //If the game has started, then don't bother with the rest of the count down checks.
            if ((HasGameStarted() && !HasRaceOverCountDown()) || (HasRaceOverCountDown() && IsCurrentGameOver())) return false;
            Debug.Log("B1");
            if (IsServer)
            {
                m_CountdownStarted.Value = SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();
                //While we are counting down, continually set the replicated time remaining value for clients (client should only receive the update once)
                Debug.Log("B2 " + m_CountdownStarted.Value);
                if (m_CountdownStarted.Value && !m_ReplicatedTimeSent)
                {
                    SetReplicatedTimeRemainingClientRPC(m_DelayedStartTime);
                    m_ReplicatedTimeSent = true;
                }

                return m_CountdownStarted.Value;
            }

            return m_ClientStartCountdown;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (m_ReplicatedTimeSent)
            {
                // Send the RPC only to the newly connected client
                SetReplicatedTimeRemainingClientRPC(m_TimeRemaining, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong>() { clientId } } });
            }
        }

        [ClientRpc]
        private void SetReplicatedTimeRemainingClientRPC(float delayedStartTime, ClientRpcParams clientRpcParams = new ClientRpcParams())
        {
            // See the ShouldStartCountDown method for when the server updates the value
            if (m_TimeRemaining == 0)
            {
                Debug.LogFormat("Client side our first timer update value is {0}", delayedStartTime);
                m_TimeRemaining = delayedStartTime;
            }
            else
            {
                Debug.Log(m_TimeRemaining);
                Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", delayedStartTime);
            }
        }

        private void UpdateGameTimer()
        {
            if (!ShouldStartCountDown()) return;
            if ((!HasGameStarted() && m_TimeRemaining > 0.0f) || (HasRaceOverCountDown() && m_TimeRemaining > 0.0f))
            {
                if (m_TimeRemaining == 3.0f)
                {
                    //countdownAudioSource.Play();
                }

                m_TimeRemaining -= Time.deltaTime;

                if (m_TimeRemaining < 0.1f)
                {
                    if (HasRaceOverCountDown())
                        countdownText.text = "Over!";
                    else
                        countdownText.text = "Start!";
                }

                if (m_TimeRemaining <= 0.0f)
                {
                    if (IsServer) // Only the server should be updating this
                    {
                        m_TimeRemaining = 0.0f;
                        if (!HasRaceOverCountDown())
                        {
                            hasGameStarted.Value = true;
                            //bgmAudioSource.clip = GameManager.instance.BGM;
                            //bgmAudioSource.Play();
                            GameManager.instance.OnRaceStart();
                        }
                        else
                        {
                            isGameOver.Value = true;
                            GameManager.instance.OnRaceOver();
                        }
                        countdownText.gameObject.SetActive(false);
                    }
                }

                if (m_TimeRemaining > 0.1f)
                    countdownText.SetText("{0}", Mathf.FloorToInt(m_TimeRemaining) + 1);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        /*
        public void StartCountDown()
        {
            StartCoroutine(CountDownCO(3, false));
        }
        */

        // state = false => race start, state = true => race over countdown
        //Countdown for offline mode
        private IEnumerator CountDownCO(int count, bool state)
        {
            yield return new WaitForSeconds(0.3f);

            while (true)
            {
                if (state && GameManager.instance.GetGameState() == GameStates.raceOver)
                {
                    gameObject.SetActive(false);
                }

                //if (count == 3)
                //{
                //    countdownAudioSource.Play();
                //}

                if (count != 0)
                {
                    countdownText.text = count.ToString();
                }
                else
                {
                    if (state)
                    {
                        countdownText.text = "Over!";
                        GameManager.instance.OnRaceOver();
                    }
                    else
                    {
                        countdownText.text = "Start!";
                        GameManager.instance.OnRaceStart();
                        //bgmAudioSource.clip = GameManager.instance.BGM;
                        //bgmAudioSource.Play();
                    }
                    break;
                }
                count--;
                yield return new WaitForSeconds(1.0f);
            }
            yield return new WaitForSeconds(0.5f);

            countdownText.text = "";

        }
    }
}