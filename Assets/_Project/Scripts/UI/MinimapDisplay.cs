using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class MinimapDisplay : MonoBehaviour
    {
        [SerializeField] private Camera followPlayerCam;
        [SerializeField] private Camera fullViewCam;

        private void Update()
        {
            if (InputManager.instance.Controllers.Player.Map.WasPressedThisFrame())
            {
                if (followPlayerCam.enabled)
                {
                    followPlayerCam.enabled = false;
                    fullViewCam.enabled = true;
                }
                else
                {
                    followPlayerCam.enabled = true;
                    fullViewCam.enabled = false;
                }
            }
        }
    }
}
