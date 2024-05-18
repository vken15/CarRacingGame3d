using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public abstract class BaseItem : MonoBehaviour
    {
        public abstract void UseItem(CarController car);

        protected abstract IEnumerator ItemDuration(float value);
    }
}
