using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ItemController : NetworkBehaviour
    {
        GameObject[] itemGameObjects;

        private int index = 0;
        private bool hasItem = false;

        private IEnumerator OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("ItemBox"))
            {
                //Item box disappear animation
                other.GetComponent<MeshCollider>().enabled = false;
                other.GetComponent<Animator>().SetBool("Enlarge", false);
                other.GetComponent<MeshRenderer>().enabled = false;
                other.GetComponentInChildren<MeshRenderer>().enabled = false;
                other.GetComponentInChildren<ParticleSystemRenderer>().enabled = false;
                //Get item
                GetItem();
                //Respawn item box
                yield return new WaitForSeconds(1);
                other.GetComponent<Animator>().SetBool("Enlarge", true);
                other.GetComponent<MeshRenderer>().enabled = true;
                other.GetComponentInChildren<MeshRenderer>().enabled = true;
                other.GetComponentInChildren<ParticleSystemRenderer>().enabled = true;
                other.GetComponent<MeshCollider>().enabled = true;
            }
        }

        private void GetItem()
        {
            if (!hasItem)
            {
                //Random item
                //index = UnityEngine.Random.Range(0, itemGameObjects.Length);
                //Set sprite
                if (IsServer)
                {
                    SendItemToClientRpc(index, DateTime.Now.Millisecond);
                } else if (GameManager.instance.networkStatus == NetworkStatus.offline)
                {
                    StartCoroutine(ShowItem(index, 3));
                }
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

        private IEnumerator ShowItem(int index, float t)
        {
            yield return new WaitForSeconds(t);
            itemGameObjects[index].SetActive(true);
            hasItem = true;
        }

        [ClientRpc]
        void SendItemToClientRpc(int index, int time)
        {
            float t = (DateTime.Now.Millisecond - time) / 1000.0f;
            StartCoroutine(ShowItem(index, 3.0f - t));
        }
    }
}
