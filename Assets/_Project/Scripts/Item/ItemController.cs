using System.Collections;
using Unity.Netcode;
using Unity.Services.Analytics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class ItemController : NetworkBehaviour
    {
        GameObject itemGameObjects;

        Image itemImage;
        VerticalLayoutGroup itemLayoutGroup;
        ItemData[] itemDatas;

        private int itemIndex = 0;
        private bool hasItem = false;
        private bool waiting = false;

        CarController carController;

        void Awake()
        {
            carController = GetComponent<CarController>();

            //Load the item data
            itemDatas = Resources.LoadAll<ItemData>("ItemData/");

            if (GameManager.instance.networkStatus == NetworkStatus.online) return;

            itemImage = GameObject.FindGameObjectWithTag("Item").GetComponent<Image>();
            itemLayoutGroup = itemImage.GetComponentInParent<VerticalLayoutGroup>();
            itemLayoutGroup.gameObject.SetActive(false);
        }

        void Start()
        {
            if (!IsOwner || GetComponent<CarAIHandler>().enabled == true) return;

            StartCoroutine(SetUp());
        }


        /// <summary>
        /// This script spawn before the game object with tag 'Item' spawn so we wait for a second
        /// before setting up.
        /// </summary>
        /// <returns></returns>
        private IEnumerator SetUp()
        {
            yield return new WaitForSeconds(1);

            itemImage = GameObject.FindGameObjectWithTag("Item").GetComponent<Image>();
            itemLayoutGroup = itemImage.GetComponentInParent<VerticalLayoutGroup>();
            itemLayoutGroup.gameObject.SetActive(false);
        }

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
                StartCoroutine(GetItem());
                //Respawn item box
                yield return new WaitForSeconds(1);
                other.GetComponent<Animator>().SetBool("Enlarge", true);
                other.GetComponent<MeshRenderer>().enabled = true;
                other.GetComponentInChildren<MeshRenderer>().enabled = true;
                other.GetComponentInChildren<ParticleSystemRenderer>().enabled = true;
                other.GetComponent<MeshCollider>().enabled = true;
            }
        }

        private IEnumerator GetItem()
        {
            if (!hasItem && !waiting)
            {
                //Random item
                itemIndex = Random.Range(0, itemDatas.Length);
                
                //Set sprite
                if (IsServer)
                {
                    SendItemToClientRpc(itemIndex);
                } else if (GameManager.instance.networkStatus == NetworkStatus.offline)
                {
                    itemGameObjects = Instantiate(itemDatas[itemIndex].Item, carController.transform);
                    itemImage.sprite = itemDatas[itemIndex].ItemSprite;
                }

                if (itemLayoutGroup != null)
                {
                    itemLayoutGroup.gameObject.SetActive(true);
                    itemLayoutGroup.GetComponent<Animator>().SetBool("Random", true);
                }

                waiting = true;

                yield return new WaitForSeconds(4);
                if (itemLayoutGroup != null)
                {
                    itemLayoutGroup.GetComponent<Animator>().SetBool("Random", false);
                }
                //itemGameObjects.SetActive(true);
                hasItem = true;
                waiting = false;
            }
        }

        public void UseItem()
        {
            if (hasItem)
            {
                hasItem = false;
                itemGameObjects.GetComponent<BaseItem>().UseItem(carController);
                //Item disappear
                if (itemLayoutGroup != null) {
                    itemLayoutGroup.gameObject.SetActive(false);
                }
            }
        }

        [ClientRpc]
        void SendItemToClientRpc(int index)
        {
            itemIndex = index;
            itemGameObjects = Instantiate(itemDatas[itemIndex].Item, carController.transform);
            if (itemImage != null)
                itemImage.sprite = itemDatas[itemIndex].ItemSprite;
        }
    }
}
