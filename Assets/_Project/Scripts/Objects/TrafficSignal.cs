using UnityEngine;

namespace CarRacingGame3d
{
    public class TrafficSignal : MonoBehaviour
    {
        [SerializeField] private Light RedLight;
        [SerializeField] private Light YellowLight;
        [SerializeField] private Light GreenLight;
        [SerializeField] private float redLightMinTime = 5f;
        [SerializeField] private float yellowLightMinTime = 3f;
        [SerializeField] private float greenLightMinTime = 5f;
        [SerializeField] private bool isGreenLight;
        [SerializeField] GameObject trafficBlock;

        private int currentTrafficLight = 0;
        private float trafficTimer;

        private void Start()
        {
            RedLight.enabled = !isGreenLight;
            YellowLight.enabled = false;
            GreenLight.enabled = isGreenLight;
            currentTrafficLight = isGreenLight ? 0 : 2;
            if (trafficBlock != null)
            {
                trafficBlock.GetComponent<Animator>().SetBool("IsOpen", isGreenLight);
            }
        }

        private void Update()
        {
            if (GameManager.instance.GetGameState() == GameStates.Countdown) return;

            trafficTimer += Time.deltaTime;
           
            if (currentTrafficLight == 0 && trafficTimer >= greenLightMinTime)
            {
                currentTrafficLight++;
                GreenLight.enabled = false;
                YellowLight.enabled = true;
                trafficTimer = 0;
            } else if (currentTrafficLight == 1 && trafficTimer >= yellowLightMinTime)
            {
                if (trafficBlock != null)
                {
                    trafficBlock.GetComponent<Animator>().SetBool("IsOpen", false);
                }
                currentTrafficLight++;
                YellowLight.enabled = false;
                RedLight.enabled = true;
                trafficTimer = 0;
            } else if (currentTrafficLight == 2 && trafficTimer >= redLightMinTime)
            {
                if (trafficBlock != null)
                {
                    trafficBlock.GetComponent<Animator>().SetBool("IsOpen", true);
                }
                currentTrafficLight = 0;
                RedLight.enabled = false;
                GreenLight.enabled = true;
                trafficTimer = 0;
            }
        }

    }
}
