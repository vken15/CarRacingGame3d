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
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            else
                SceneTransitionHandler.Instance.SwitchScene("Room");
        }
        public void OnBackToMenu()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
                SceneManager.LoadScene("MainMenu");
            else
            {
                NetworkManager.Singleton.Shutdown();
                GameManager.instance.networkStatus = NetworkStatus.offline;
                SceneTransitionHandler.Instance.ExitAndLoadStartMenu();
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
                SceneTransitionHandler.Instance.SwitchScene("PostGame");
            else
                SceneTransitionHandler.Instance.SwitchScene("Room");
        }

        private void OnDestroy()
        {
            GameManager.instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}