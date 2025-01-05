using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrafficLightColor
{
    Red,
    Yellow,
    Green
}

public class LocalTrafficController : MonoBehaviour
{
    [SerializeField] private List<Sensor> sensor;
    public int carCountOnEntrance;
    public TrafficLightColor currentLight;
    private Dictionary<TrafficLightColor, int> TimeForTrafficLightColor = new Dictionary<TrafficLightColor, int>();

    public bool wantWarning;

    private void Awake()
    {
        currentLight = TrafficLightColor.Red;
        TimeForTrafficLightColor[TrafficLightColor.Red] = 30;    // Domyœlny czas czerwonego
        TimeForTrafficLightColor[TrafficLightColor.Yellow] = 5;  // Domyœlny czas ¿ó³tego
        TimeForTrafficLightColor[TrafficLightColor.Green] = 5;   // Domyœlny czas zielonego
    }

    private void Start()
    {
        StartCoroutine(GetCarCount());
    }

    private IEnumerator GetCarCount()
    {
        while (true)
        {
            carCountOnEntrance = 0; // Reset car count before summing up

            foreach (var s in sensor) // Loop through all sensors
            {
                carCountOnEntrance += s.CarCount; // Add car count from each sensor
            }

            yield return new WaitForSeconds(0.5f); // Update car count every 0.5 seconds
        }
    }


    // Ustawienie cyklu œwiate³ (mo¿emy zmieniaæ czasy trwania ka¿dego koloru)
    public void SetTrafficLight(TrafficLightColor color)
    {
        currentLight = color;

        UpdateTrafficLightVisuals(color);
    }

    private void UpdateTrafficLightVisuals(TrafficLightColor color)
    {
        switch (color)
        {
            case TrafficLightColor.Red:
                if (wantWarning) Debug.Log(name + " set to RED");
                break;
            case TrafficLightColor.Yellow:
                if (wantWarning) Debug.Log(name + " set to YELLOW");
                break;
            case TrafficLightColor.Green:
                if (wantWarning) Debug.Log(name + " set to GREEN");
                break;
        }
    }

}
