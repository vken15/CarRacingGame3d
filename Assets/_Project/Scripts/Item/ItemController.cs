using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemController : MonoBehaviour
    {
        private bool hasItem = false;

        private IEnumerator OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("ItemBox"))
            {
                //Item box disappear animation

                //Get item
                StartCoroutine(GetItem());
                //Respawn item box
                yield return new WaitForSeconds(1);

            }
        }

        private IEnumerator GetItem()
        {
            if (!hasItem)
            {
                //Random item

                yield return new WaitForSeconds(3);
                //

                hasItem = true;
            }
        }

        public void UseItem()
        {
            if (hasItem)
            {
                hasItem = false;

                //Item disappear
            }
        }
    }
}
