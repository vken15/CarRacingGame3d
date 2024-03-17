using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    /// <summary>
    /// A Temporary animation script that rotates the image on the game
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField]
        private float rotationSpeed;

        void Update()
        {
            transform.Rotate(new Vector3(0, 0, rotationSpeed * Mathf.PI * Time.deltaTime));
        }
    }
}
