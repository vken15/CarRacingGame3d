using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class Startup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InstantiatePrefabs()
        {
            Debug.Log("-- Loading --");
            GameObject[] prefabsToInstantiate = Resources.LoadAll<GameObject>("InstantiateOnLoad/");
            foreach (GameObject pref in prefabsToInstantiate)
            {
                Debug.Log($"{pref.name}");
                GameObject.Instantiate(pref);
            }
            Debug.Log("-- Done --");
        }
    }
}