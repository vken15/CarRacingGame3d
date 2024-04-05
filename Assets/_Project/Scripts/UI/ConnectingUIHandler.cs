using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ConnectingUIHandler : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] GameObject loadingSpinner;

        void Awake()
        {
            Hide();
        }

        void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            loadingSpinner.SetActive(false);
        }

        public void ShowConnecting()
        {
            void OnTimeElapsed()
            {
                Hide();
            }

            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            var maxConnectAttempts = utp.MaxConnectAttempts;
            var connectTimeoutMS = utp.ConnectTimeoutMS;
            StartCoroutine(DisplayUTPConnectionDuration(maxConnectAttempts, connectTimeoutMS, OnTimeElapsed));

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            loadingSpinner.SetActive(true);
        }

        IEnumerator DisplayUTPConnectionDuration(int maxReconnectAttempts, int connectTimeoutMS, Action endAction)
        {
            var connectionDuration = maxReconnectAttempts * connectTimeoutMS / 1000f;

            var seconds = Mathf.CeilToInt(connectionDuration);

            while (seconds > 0)
            {
                titleText.text = $"Connecting...\n{seconds}";
                yield return new WaitForSeconds(1f);
                seconds--;
            }
            titleText.text = "Connecting...";

            endAction();
        }

        public void OnCancelJoinButtonPressed()
        {
            Hide();
            StopAllCoroutines();

            var connectionManager = ConnectionManager.instance;
            if (connectionManager && connectionManager.NetworkManager)
                connectionManager.RequestShutdown();
        }
    }
}
