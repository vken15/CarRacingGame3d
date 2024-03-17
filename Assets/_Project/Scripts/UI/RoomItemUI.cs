using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.VolumeComponent;

namespace CarRacingGame3d
{
    public class RoomItemUI : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Text numPlayerText;

        LocalLobby m_Data;

        public void SetRoomName(string name)
        {
            nameText.text = name;
        }

        public void SetPlayerNumber(ushort cur, ushort max)
        {
            numPlayerText.text = $"{cur}/{max}";
        }

        public void SetData(LocalLobby data)
        {
            m_Data = data;
            nameText.text = data.LobbyName;
            numPlayerText.text = $"{data.PlayerCount}/{data.MaxPlayerCount}";
        }

        public LocalLobby GetData()
        {
            return m_Data;
        }
    }
}
