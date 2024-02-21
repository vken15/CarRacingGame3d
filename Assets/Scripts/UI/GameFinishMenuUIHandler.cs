using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        //else
        //    SceneTransitionHandler.sceneTransitionHandler.SwitchScene(GameManager.instance.GetMapScene());
    }
    public void OnBackToMenu()
    {
        SceneManager.LoadScene("Menu");
        if (GameManager.instance.networkStatus == NetworkStatus.offline)
            SceneManager.LoadScene("Menu");
        else
        {
            NetworkManager.Singleton.Shutdown();
            GameManager.instance.networkStatus = NetworkStatus.offline; 
        //    SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
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
        if (GameManager.instance.GetGameState() == GameStates.raceOver)
        {
            StartCoroutine(ShowMenuCO());
        }
    }

    private void OnDestroy()
    {
        GameManager.instance.OnGameStateChanged -= OnGameStateChanged;
    }
}
