using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CarRacingGame3d
{
    public class GameFinishMenuUIHandler : MonoBehaviour
    {
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;
            //Hook up events
            GameManager.instance.OnGameStateChanged += OnGameStateChanged;
        }
        public void OnRaceAgain()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
                SceneTransitionHandler.Instance.SwitchScene(SceneManager.GetActiveScene().name, false);
            else
                SceneTransitionHandler.Instance.SwitchScene("Room", true);
        }
        public void OnBackToMenu()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
                SceneTransitionHandler.Instance.SwitchScene("MainMenu", false);
            else
            {
                GameManager.instance.networkStatus = NetworkStatus.offline;
                ConnectionManager.instance.RequestShutdown();
            }
        }

        private IEnumerator ShowMenuCO()
        {
            yield return new WaitForSeconds(1);

            canvas.enabled = true;
        }

        //Events
        private void OnGameStateChanged(GameManager gameManager)
        {
            if (GameManager.instance.GetGameState() == GameStates.RaceOver)
            {
                if (GameManager.instance.gameMode == GameMode.Round)
                {
                    GameManager.instance.currentRound++;
                    StartCoroutine(WaitToEnd());
                }
                else 
                    StartCoroutine(ShowMenuCO());
            }
        }

        IEnumerator WaitToEnd()
        {
            yield return new WaitForSeconds(3);

            if (GameManager.instance.currentRound > GameManager.instance.maxRound)
                SceneTransitionHandler.Instance.SwitchScene("PostGame", GameManager.instance.networkStatus == NetworkStatus.online);
            else
                SceneTransitionHandler.Instance.SwitchScene("Room", GameManager.instance.networkStatus == NetworkStatus.online);
        }

        private void OnDestroy()
        {
            GameManager.instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}