using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRacingGame3d
{
    public class PauseMenuUIHandler : MonoBehaviour
    {
        [SerializeField] private GameObject optionsCanvas;
        [SerializeField] private Canvas gameFinishCanvas;

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;
        }
        private void Update()
        {
            if (InputManager.instance.Controllers.Player.ESC.WasPressedThisFrame() && !optionsCanvas.activeInHierarchy && !gameFinishCanvas.enabled)
            {
                canvas.enabled = !canvas.enabled;
                if (GameManager.instance.networkStatus == NetworkStatus.offline)
                {
                    AudioListener.pause = canvas.enabled;
                    Time.timeScale = canvas.enabled ? 0 : 1;
                }
            }
        }
        public void OnResume()
        {
            canvas.enabled = false;
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
            {
                AudioListener.pause = false;
                Time.timeScale = 1;
            }
        }
        public void OnOptions()
        {
            optionsCanvas.SetActive(true);
        }
        public void OnExit()
        {
            OnResume();
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
            {
                SceneTransitionHandler.Instance.SwitchScene("MainMenu", false);
            }
            else
            {
                ConnectionManager.instance.RequestShutdown();
            }
        }
    }
}
