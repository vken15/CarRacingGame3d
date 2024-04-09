using TMPro;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ChatMessage : MonoBehaviour
    {
        [SerializeField] TMP_Text messageText;

        Color[] playerColors = { Color.black, Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.gray, Color.cyan, Color.black };

        public void SetMessage(string playerName, int playerNumber, string message)
        {
            messageText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(playerColors[playerNumber])}>{playerName}</color>: {message}";
        }

        public void SetOtherMessage(string message)
        {
            messageText.text = $"{message}";
        }
    }
}
