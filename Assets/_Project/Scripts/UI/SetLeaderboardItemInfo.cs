using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class SetLeaderboardItemInfo : MonoBehaviour
    {
        [SerializeField] private Text driverPositionText;
        [SerializeField] private Text driverNameText;
        [SerializeField] private Text driverFinishTimeText;
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

        public void SetDriverNameText(string newDriverName)
        {
            driverNameText.text = newDriverName;
        }

        public void SetDriverFinishTimeText(string newFinishTime)
        {
            if (!driverFinishTimeText.Equals("Fail"))
                driverFinishTimeText.text = newFinishTime;
        }
    }
}