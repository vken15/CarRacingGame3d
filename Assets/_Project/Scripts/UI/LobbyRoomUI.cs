using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace CarRacingGame3d
{
    public class LobbyRoomUI : MonoBehaviour
    {
        [SerializeField] private GameObject startBtn;
        [SerializeField] private GameObject readyBtn;
        [SerializeField] private GameObject leaveBtn;
        [SerializeField] private GameObject changeMapBtn;
        [SerializeField] private GameObject changeCarBtn;

        private void Awake()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log("Host");
                startBtn.SetActive(true);
                changeMapBtn.SetActive(true);
            }
            else
            {
                Debug.Log("Client Join");
                readyBtn.SetActive(true);
                Text btnName = readyBtn.GetComponentInChildren<Text>();
                btnName.text = "Ready";
                readyBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (btnName.text.Equals("Ready"))
                    {
                        leaveBtn.GetComponent<Button>().interactable = false;
                        changeCarBtn.SetActive(false);
                        btnName.text = "Cancel";
                    }
                    else
                    {
                        leaveBtn.GetComponent<Button>().interactable = true;
                        changeCarBtn.SetActive(true);
                        btnName.text = "Ready";
                    }
                });
            }
        }
    }
}