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

    [SerializeField] private Sensor sensor;
    public int carCountOnEntrance { get; private set; }
    public TrafficLightColor currentLight;
    Dictionary<TrafficLightColor, int> TimeForTrafficLightColor = new Dictionary<TrafficLightColor, int>();
    private void Start()
    {
        StartCoroutine(getCarCount());
        currentLight = TrafficLightColor.Red;
        TimeForTrafficLightColor[TrafficLightColor.Red] = 10;
        TimeForTrafficLightColor[TrafficLightColor.Yellow] = 5;
        TimeForTrafficLightColor[TrafficLightColor.Green] = 5;
    }

    private IEnumerator getCarCount()
    {
        while (true)
        {
            carCountOnEntrance = sensor.CarCount;
            yield return new WaitForSeconds(5.0f);
        }
    }
    public void SetTrafficLightCycle(Dictionary<TrafficLightColor, int> cycleToSet)
    {
        foreach (var entry in cycleToSet)
        {
            TimeForTrafficLightColor[entry.Key] = entry.Value; // Zmie� warto�� czasu dla kolor�w poszczeg�lnych
        }
        StartCoroutine(ChangeTrafficLights());
    }

    // Korutyna do zmiany �wiate� na podstawie nowego cyklu
    private IEnumerator ChangeTrafficLights()
    {
        // Tworzymy kopi� kluczy z TimeForTrafficLightColor
        List<TrafficLightColor> colors = new List<TrafficLightColor>(TimeForTrafficLightColor.Keys);

        // Iterujemy po kopii listy kolor�w
        foreach (var color in colors)
        {
            currentLight = color; // Ustawiamy obecny kolor �wiate�
            yield return new WaitForSeconds(TimeForTrafficLightColor[color]); // Czekamy na przej�cie do nast�pnego koloru
        }
    }
    public Dictionary<TrafficLightColor, int> GetTrafficLightCycle()
    {
        return new Dictionary<TrafficLightColor, int>(TimeForTrafficLightColor);
    }

}
