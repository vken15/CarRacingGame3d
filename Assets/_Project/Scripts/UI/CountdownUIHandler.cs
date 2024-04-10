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
        [SerializeField] private AudioSource countdownAudioSource;
        //[SerializeField] private AudioSource bgmAudioSource;

        [SerializeField]
        [Tooltip("Time Remaining until the game starts")]
        private float delayedStartTime = 3.0f;

        private bool clientGameOver;
        private bool clientGameStarted;
        private bool clientStartCountdown;

        private bool replicatedTimeSent = false;
        private float timeRemaining;

        private NetworkVariable<bool> NetworkCountdownStarted = new(false);
        private NetworkVariable<bool> NetworkHasGameStarted { get; } = new(false);
        private NetworkVariable<bool> NetworkRaceOverCountdown { get; } = new(false);
        private NetworkVariable<bool> NetworkIsGameOver { get; } = new(false);

        private bool raceOverCountdown = false;

        private void Awake()
        {
            countdownText.text = "";

            if (IsServer)
            {
                NetworkHasGameStarted.Value = false;

                //Set our time remaining locally
                timeRemaining = delayedStartTime;

                //Set for server side
                replicatedTimeSent = false;
            }
            else
            {
                //We do a check for the client side value upon instantiating the class (should be zero)
                Debug.LogFormat("Client side we started with a timer value of {0}", timeRemaining);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient && !IsServer)
            {
                clientGameOver = false;
                clientStartCountdown = false;
                clientGameStarted = false;

                NetworkCountdownStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    clientStartCountdown = newValue;
                    Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
                };

                NetworkHasGameStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    clientGameStarted = newValue;
                    GameManager.instance.OnRaceStart();
                    timeRemaining = 0.0f;
                    //bgmAudioSource.clip = GameManager.instance.BGM;
                    //bgmAudioSource.Play();
                    StartCoroutine(HideCountdownText());
                    Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
                };

                NetworkRaceOverCountdown.OnValueChanged += (oldValue, newValue) =>
                {
                    raceOverCountdown = newValue;
                    countdownText.gameObject.SetActive(true);
                    Debug.LogFormat("Client side we were notified the game end count down state was {0}", newValue);
                };

                NetworkIsGameOver.OnValueChanged += (oldValue, newValue) =>
                {
                    clientGameOver = newValue;
                    GameManager.instance.OnRaceOver();
                    StartCoroutine(HideCountdownText());
                    Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
                };
            }

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

                if (!NetworkRaceOverCountdown.Value && GameManager.instance.GetGameState() == GameStates.RaceOverCountDown && IsServer)
                {
                    NetworkRaceOverCountdown.Value = true;
                    timeRemaining = delayedStartTime = 10.0f;
                    replicatedTimeSent = false;
                    countdownText.gameObject.SetActive(true);
                }

                UpdateGameTimer();
            }
            else
            {
                if (!raceOverCountdown && GameManager.instance.GetGameState() == GameStates.RaceOverCountDown)
                {
                    raceOverCountdown = true;
                    StartCoroutine(CountDownCO(10, true));
                }
            }
        }

        private bool HasGameStarted()
        {
            if (IsServer)
                return NetworkHasGameStarted.Value;
            return clientGameStarted;
        }

        private bool HasRaceOverCountDown()
        {
            if (IsServer)
                return NetworkRaceOverCountdown.Value;
            return raceOverCountdown;
        }

        private bool IsCurrentGameOver()
        {
            if (IsServer)
                return NetworkIsGameOver.Value;
            return clientGameOver;
        }

        private bool ShouldStartCountDown()
        {
            //If the game has started, then don't bother with the rest of the count down checks.
            if ((HasGameStarted() && !HasRaceOverCountDown()) || (HasRaceOverCountDown() && IsCurrentGameOver())) return false;
            if (IsServer)
            {
                NetworkCountdownStarted.Value = SceneTransitionHandler.Instance.AllClientsAreLoaded();
                //While we are counting down, continually set the replicated time remaining value for clients (client should only receive the update once)
                if (NetworkCountdownStarted.Value && !replicatedTimeSent)
                {
                    SetReplicatedTimeRemainingClientRPC(delayedStartTime);
                    replicatedTimeSent = true;
                }

                return NetworkCountdownStarted.Value;
            }

            return clientStartCountdown;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (replicatedTimeSent)
            {
                // Send the RPC only to the newly connected client
                SetReplicatedTimeRemainingClientRPC(timeRemaining, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong>() { clientId } } });
            }
        }

        [ClientRpc]
        private void SetReplicatedTimeRemainingClientRPC(float delayedStartTime, ClientRpcParams clientRpcParams = new ClientRpcParams())
        {
            // See the ShouldStartCountDown method for when the server updates the value
            if (timeRemaining == 0)
            {
                Debug.LogFormat("Client side our first timer update value is {0}", delayedStartTime);
                timeRemaining = delayedStartTime;
            }
            else
            {
                Debug.Log(timeRemaining);
                Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", delayedStartTime);
            }
        }

        private void UpdateGameTimer()
        {
            if (!ShouldStartCountDown()) return;
            if ((!HasGameStarted() && timeRemaining > 0.0f) || (HasRaceOverCountDown() && timeRemaining > 0.0f))
            {
                if (timeRemaining <= 3.0f && timeRemaining > 0.0f && !countdownAudioSource.isPlaying)
                {
                    countdownAudioSource.Play();
                }

                timeRemaining -= Time.deltaTime;

                if (timeRemaining <= 0.0f)
                {
                    if (IsServer) // Only the server should be updating this
                    {
                        timeRemaining = 0.0f;
                        if (!HasRaceOverCountDown())
                        {
                            NetworkHasGameStarted.Value = true;
                            //bgmAudioSource.clip = GameManager.instance.BGM;
                            //bgmAudioSource.Play();
                            GameManager.instance.OnRaceStart();
                            
                        }
                        else
                        {
                            NetworkIsGameOver.Value = true;
                            GameManager.instance.OnRaceOver();
                        }
                    }

                    StartCoroutine(HideCountdownText());
                } else
                {
                    countdownText.SetText("{0}", Mathf.FloorToInt(timeRemaining) + 1);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private IEnumerator HideCountdownText()
        {
            if (HasRaceOverCountDown())
                countdownText.text = "Over!";
            else
                countdownText.text = "Start!";

            yield return new WaitForSeconds(1.0f);
            countdownText.gameObject.SetActive(false);
        }

        // state = false => race start, state = true => race over countdown
        //Countdown for offline mode
        private IEnumerator CountDownCO(int count, bool state)
        {
            yield return new WaitForSeconds(0.3f);

            while (true)
            {
                if (state && GameManager.instance.GetGameState() == GameStates.RaceOver)
                {
                    gameObject.SetActive(false);
                }

                if (count == 3)
                {
                    countdownAudioSource.Play();
                }

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