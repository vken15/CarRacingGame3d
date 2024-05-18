using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class SetLeaderboardItemInfo : MonoBehaviour
    {
        [SerializeField] private Text driverPositionText;
        [SerializeField] private Text driverNameText;
        [SerializeField] private Text driverFinishTimeText;
        [SerializeField] private Text driverLapText;
        [SerializeField] private Text driverScoreText;

        private int numberOfLaps;
        public int playerNumber;

        private void Awake()
        {
            numberOfLaps = GameManager.instance.GetNumberOfLaps();
        }

        public string GetDriverName()
        {
            return driverNameText.text;
        }

        public string GetDriverFinishTime()
        {
            return driverFinishTimeText.text;
        }

        public void SetPositionText(string newPosition)
        {
            driverPositionText.text = newPosition;
        }

        public void SetDriverNameText(string newDriverName, Color color)
        {
            driverNameText.text = newDriverName;
            driverNameText.color = color;
        }

        public void SetDriverFinishTimeText(string newFinishTime)
        {
            if (!driverFinishTimeText.Equals("Fail"))
                driverFinishTimeText.text = newFinishTime;
        }

        public void SetDriverLapText(int currentLap)
        {
            driverLapText.text = $"{currentLap}/{numberOfLaps}";
        }

        public void SetDriverScoreText(string newScore)
        {
            driverScoreText.text = newScore;
        }
    }
}