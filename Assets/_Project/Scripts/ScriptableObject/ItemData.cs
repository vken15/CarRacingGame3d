using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CarRacingGame3d
{
    [CreateAssetMenu(fileName = "New Item Data", menuName = "Item Data", order = 53)]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private ushort itemId;
        [SerializeField] private Sprite itemSprite;
        [SerializeField] private GameObject item;

        public ushort ItemID
        {
            get { return itemId; }
        }
        public Sprite ItemSprite 
        {
            get { return itemSprite; } 
        }
        public GameObject Item
        {
            get { return item; }
        }
    }
}
