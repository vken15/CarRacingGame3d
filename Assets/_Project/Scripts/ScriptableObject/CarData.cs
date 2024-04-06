using UnityEngine;

namespace CarRacingGame3d
{
    [CreateAssetMenu(fileName = "New Car Data", menuName = "Car Data", order = 52)]
    public class CarData : ScriptableObject
    {
        [SerializeField] private ushort carID = 0;
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private GameObject carSelectPrefab;
        [SerializeField] private Sprite carUISprite;
        [SerializeField] private bool isSkin = false;
        [SerializeField] private ushort baseCarID = 0;
        public ushort CarID
        {
            get { return carID; }
        }
        public GameObject CarPrefab
        {
            get { return carPrefab; }
        }
        public GameObject CarSelectPrefab
        {
            get { return carSelectPrefab; }
        }
        public Sprite CarUISprite
        {
            get { return carUISprite; }
        }
        public bool IsSkin
        {
            get { return isSkin; }
        }
        public ushort BaseCarID
        {
            get { return baseCarID; }
        }
    }
}
