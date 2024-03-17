using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

namespace CarRacingGame3d
{
    public class NameplateUIHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameplate;

        public void SetData(string name, Color color)
        {
            nameplate.text = name;
            nameplate.color = color;
        }
    }
}