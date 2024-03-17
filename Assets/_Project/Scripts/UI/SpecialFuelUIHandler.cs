using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class SpecialFuelUIHandler : MonoBehaviour
    {
        [SerializeField] private Slider fuelBarSlider;
        [SerializeField] private Image fuelBarFillImage;

        public CarController controller;

        private void Update()
        {
            if (controller != null)
            {
                UpdateFuelBar(controller.GetNitroFuelPercent());
            }
        }

        public void UpdateFuelBar(float newValue)
        {
            fuelBarSlider.value = newValue;
            if (newValue > 0.5f)
            {
                fuelBarFillImage.color = Color.green;
            }
            else if (newValue > 0.25f)
            {
                fuelBarFillImage.color = Color.yellow;
            }
            else
            {
                fuelBarFillImage.color = Color.red;
            }
        }
    }
}
