using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyRoomUI : MonoBehaviour
{
    [SerializeField] private GameObject startBtn;
    [SerializeField] private GameObject readyBtn;

    private void Awake()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host");
            Destroy(readyBtn);
        }
        else
        {
            Debug.Log("Client Join");
            Destroy(startBtn);
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
