using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RaceTimeUIHandler : MonoBehaviour
{
    private Text timelapsedText;
    private float lastRaceTimeUpdate = 0;

    private void Awake()
    {
        timelapsedText = GetComponent<Text>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine(UpdateTimeCO());
    }

    private IEnumerator UpdateTimeCO()
    {
        while (true)
        {
            float raceTime = GameManager.instance.GetRaceTime();
            if (lastRaceTimeUpdate != raceTime)
            {
                int raceTimeMinutes = (int) Mathf.Floor(raceTime / 60);
                int raceTimeSeconds = (int) Mathf.Floor(raceTime % 60);
                timelapsedText.text = $"{raceTimeMinutes:00}:{raceTimeSeconds:00}";
                lastRaceTimeUpdate = raceTime;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

}
