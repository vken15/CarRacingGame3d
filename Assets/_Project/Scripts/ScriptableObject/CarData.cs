using UnityEngine;

namespace CarRacingGame3d
{
    [CreateAssetMenu(fileName = "New Car Data", menuName = "Car Data", order = 52)]
    public class CarData : ScriptableObject
    {
        [SerializeField] private int carID = 0;
        [SerializeField] private GameObject carPrefab;
        public int CarID
        {
            get { return carID; }
        }
        public GameObject CarPrefab
        {
            get { return carPrefab; }
        }
    }
}
