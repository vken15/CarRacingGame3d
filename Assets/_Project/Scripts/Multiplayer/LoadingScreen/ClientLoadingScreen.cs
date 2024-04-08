using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class ClientLoadingScreen : MonoBehaviour
    {
        protected class LoadingProgressBar
        {
            public Slider ProgressBar { get; set; }

            public Text NameText { get; set; }

            public LoadingProgressBar(Slider otherPlayerProgressBar, Text otherPlayerNameText)
            {
                ProgressBar = otherPlayerProgressBar;
                NameText = otherPlayerNameText;
            }

            public void UpdateProgress(float value, float newValue)
            {
                ProgressBar.value = newValue;
            }
        }

        [SerializeField] PersistentPlayerRuntimeCollection persistentPlayerRuntimeCollection;

        [SerializeField]
        CanvasGroup canvasGroup;

        [SerializeField]
        float delayBeforeFadeOut = 0.5f;

        [SerializeField]
        float fadeOutDuration = 0.1f;

        [SerializeField]
        Slider progressBar;

        [SerializeField]
        Text sceneName;

        [SerializeField]
        List<Slider> otherPlayersProgressBars;

        [SerializeField]
        List<Text> otherPlayerNamesTexts;

        [SerializeField]
        protected LoadingProgressManager loadingProgressManager;

        protected Dictionary<ulong, LoadingProgressBar> loadingProgressBars = new();

        bool loadingScreenRunning;

        Coroutine fadeOutCoroutine;

        void Awake()
        {
            DontDestroyOnLoad(this);
            Assert.AreEqual(otherPlayersProgressBars.Count, otherPlayerNamesTexts.Count, "There should be the same number of progress bars and name labels");
        }

        void Start()
        {
            SetCanvasVisibility(false);
            loadingProgressManager.OnTrackersUpdated += OnProgressTrackersUpdated;
        }

        void OnDestroy()
        {
            loadingProgressManager.OnTrackersUpdated -= OnProgressTrackersUpdated;
        }

        void Update()
        {
            if (loadingScreenRunning)
            {
                progressBar.value = loadingProgressManager.LocalProgress;
            }
        }

        void OnProgressTrackersUpdated()
        {
            // deactivate progress bars of clients that are no longer tracked
            var clientIdsToRemove = new List<ulong>();
            foreach (var clientId in loadingProgressBars.Keys)
            {
                if (!loadingProgressManager.ProgressTrackers.ContainsKey(clientId))
                {
                    clientIdsToRemove.Add(clientId);
                }
            }

            foreach (var clientId in clientIdsToRemove)
            {
                RemoveOtherPlayerProgressBar(clientId);
            }

            // Add progress bars for clients that are now tracked
            foreach (var progressTracker in loadingProgressManager.ProgressTrackers)
            {
                var clientId = progressTracker.Key;
                if (clientId != NetworkManager.Singleton.LocalClientId && !loadingProgressBars.ContainsKey(clientId))
                {
                    AddOtherPlayerProgressBar(clientId, progressTracker.Value);
                }
            }
        }

        public void StopLoadingScreen()
        {
            if (loadingScreenRunning)
            {
                if (fadeOutCoroutine != null)
                {
                    StopCoroutine(fadeOutCoroutine);
                }
                fadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
            }
        }

        public void StartLoadingScreen(string sceneName)
        {
            SetCanvasVisibility(true);
            loadingScreenRunning = true;
            UpdateLoadingScreen(sceneName);
            ReinitializeProgressBars();
        }

        void ReinitializeProgressBars()
        {
            // deactivate progress bars of clients that are no longer tracked
            var clientIdsToRemove = new List<ulong>();
            foreach (var clientId in loadingProgressBars.Keys)
            {
                if (!loadingProgressManager.ProgressTrackers.ContainsKey(clientId))
                {
                    clientIdsToRemove.Add(clientId);
                }
            }

            foreach (var clientId in clientIdsToRemove)
            {
                RemoveOtherPlayerProgressBar(clientId);
            }

            for (var i = 0; i < otherPlayersProgressBars.Count; i++)
            {
                otherPlayersProgressBars[i].gameObject.SetActive(false);
                otherPlayerNamesTexts[i].gameObject.SetActive(false);
            }

            var index = 0;

            foreach (var progressTracker in loadingProgressManager.ProgressTrackers)
            {
                var clientId = progressTracker.Key;
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    UpdateOtherPlayerProgressBar(clientId, index++);
                }
            }
        }

        protected virtual void UpdateOtherPlayerProgressBar(ulong clientId, int progressBarIndex)
        {
            loadingProgressBars[clientId].ProgressBar = otherPlayersProgressBars[progressBarIndex];
            loadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
            loadingProgressBars[clientId].NameText = otherPlayerNamesTexts[progressBarIndex];
            loadingProgressBars[clientId].NameText.gameObject.SetActive(true);
            loadingProgressBars[clientId].NameText.text = GetPlayerName(clientId);
        }

        protected virtual void AddOtherPlayerProgressBar(ulong clientId, NetworkedLoadingProgressTracker progressTracker)
        {
            if (loadingProgressBars.Count < otherPlayersProgressBars.Count && loadingProgressBars.Count < otherPlayerNamesTexts.Count)
            {
                var index = loadingProgressBars.Count;
                loadingProgressBars[clientId] = new LoadingProgressBar(otherPlayersProgressBars[index], otherPlayerNamesTexts[index]);
                progressTracker.Progress.OnValueChanged += loadingProgressBars[clientId].UpdateProgress;
                loadingProgressBars[clientId].ProgressBar.value = progressTracker.Progress.Value;
                loadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
                loadingProgressBars[clientId].NameText.gameObject.SetActive(true);
                loadingProgressBars[clientId].NameText.text = GetPlayerName(clientId);
            }
            else
            {
                throw new Exception("There are not enough progress bars to track the progress of all the players.");
            }
        }

        void RemoveOtherPlayerProgressBar(ulong clientId, NetworkedLoadingProgressTracker progressTracker = null)
        {
            if (progressTracker != null)
            {
                progressTracker.Progress.OnValueChanged -= loadingProgressBars[clientId].UpdateProgress;
            }
            loadingProgressBars[clientId].ProgressBar.gameObject.SetActive(false);
            loadingProgressBars[clientId].NameText.gameObject.SetActive(false);
            loadingProgressBars.Remove(clientId);
        }

        public void UpdateLoadingScreen(string sceneName)
        {
            if (loadingScreenRunning)
            {
                this.sceneName.text = sceneName;
                if (fadeOutCoroutine != null)
                {
                    StopCoroutine(fadeOutCoroutine);
                }
            }
        }

        void SetCanvasVisibility(bool visible)
        {
            canvasGroup.alpha = visible ? 1 : 0;
            canvasGroup.blocksRaycasts = visible;
        }

        IEnumerator FadeOutCoroutine()
        {
            yield return new WaitForSeconds(delayBeforeFadeOut);
            loadingScreenRunning = false;

            float currentTime = 0;
            while (currentTime < fadeOutDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(1, 0, currentTime / fadeOutDuration);
                yield return null;
                currentTime += Time.deltaTime;
            }

            SetCanvasVisibility(false);
        }

        string GetPlayerName(ulong clientId)
        {
            foreach (var player in persistentPlayerRuntimeCollection.Items)
            {
                if (clientId == player.OwnerClientId)
                {
                    return player.NetworkNameState.Name.Value;
                }
            }
            return "";
        }
    }
}
