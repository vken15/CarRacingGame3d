using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemFuel : BaseItem
    {
        const float refillAmount = 1.0f; //0 - 1 => 0% - 100%

        public override void UseItem(CarController car)
        {
            car.ReFillFuel(refillAmount);
            Destroy(gameObject);
        }
    }
}
