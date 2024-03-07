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

        private void Awake()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log("Host");
                startBtn.SetActive(true);
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
                        btnName.text = "Cancel";
                    else
                        btnName.text = "Ready";
                });
            }
        }
    }
}