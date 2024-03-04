using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "New Map Data", menuName = "Map Data", order = 51)]
    public class MapData : ScriptableObject
    {
        [SerializeField] private int mapID = 0;
        [SerializeField] private Sprite mapUISprite;
        [SerializeField] private string scene;
        [SerializeField] private int difficulty;
        [SerializeField] private string discription = "";

        public int MapID
        {
            get { return mapID; }
        }
        public Sprite MapUISprite
        {
            get { return mapUISprite; }
        }
        public string Scene
        {
            get { return scene; }
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
