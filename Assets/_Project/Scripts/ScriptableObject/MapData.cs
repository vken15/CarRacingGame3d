using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "New Map Data", menuName = "Map Data", order = 51)]
    public class MapData : ScriptableObject
    {
        [SerializeField] private ushort mapID = 0;
        [SerializeField] private string mapName = "";
        [SerializeField] private Sprite mapUISprite;
        [SerializeField] private string scene;
        [SerializeField] private ushort numberOfLaps = 2;
        [SerializeField] private ushort difficulty;
        [SerializeField] private string discription = "";

        public ushort MapID
        {
            get { return mapID; }
        }
        public string MapName
        {
            get { return mapName; }
        }
        public Sprite MapUISprite
        {
            get { return mapUISprite; }
        }
        public string Scene
        {
            get { return scene; }
        }
        public ushort NumberOfLaps
        {
            get { return numberOfLaps; }
        }
        public int Difficulty
        {
            get { return difficulty; }
        }
        public string Discription
        {
            get { return discription; }
        }
    }
}
