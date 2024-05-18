using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemTornado : BaseItem
    {
        [SerializeField] private GameObject Tornado;

        public override void UseItem(CarController car)
        {
            gameObject.transform.parent = null;
            Tornado.SetActive(true);
            Tornado.GetComponent<Tornado>().moveDir = car.transform.forward;
            Tornado.GetComponent<Tornado>().owner = car.GetComponent<CarInputHandler>().playerNumber;
            Tornado.GetComponent<ParticleSystem>().Play();
            StartCoroutine(ItemDuration(5));
        }

        protected override IEnumerator ItemDuration(float value)
        {
            yield return new WaitForSeconds(value);
            Destroy(gameObject);
        }
    }
}
