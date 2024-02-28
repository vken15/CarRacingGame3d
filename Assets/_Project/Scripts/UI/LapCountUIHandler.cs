using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class LapCountUIHandler : MonoBehaviour
    {
        private Text lapText;
        private void Awake()
        {
            lapText = GetComponent<Text>();
            lapText.text = $"LAP 1/{GameManager.instance.GetNumberOfLaps()}";
        }

        public void SetLapText(string text)
        {
            lapText.text = text;
        }
    }
}