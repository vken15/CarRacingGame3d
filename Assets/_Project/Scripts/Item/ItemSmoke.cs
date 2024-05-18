using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemSmoke : BaseItem
    {
        [SerializeField] private GameObject smoke;

        public override void UseItem(CarController car)
        {
            smoke.SetActive(true);
            gameObject.transform.parent = null;
            StartCoroutine(ItemDuration(5));
        }

        protected override IEnumerator ItemDuration(float value)
        {
            yield return new WaitForSeconds(value);
            Destroy(gameObject);
        }
    }
}
