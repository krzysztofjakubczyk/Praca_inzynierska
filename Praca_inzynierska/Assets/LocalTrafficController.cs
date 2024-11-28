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
    private Dictionary<TrafficLightColor, int> TimeForTrafficLightColor = new Dictionary<TrafficLightColor, int>();

    public bool wantWarning;

    private void Awake()
    {
        currentLight = TrafficLightColor.Red;
        TimeForTrafficLightColor[TrafficLightColor.Red] = 10;    // Domyœlny czas czerwonego
        TimeForTrafficLightColor[TrafficLightColor.Yellow] = 5;  // Domyœlny czas ¿ó³tego
        TimeForTrafficLightColor[TrafficLightColor.Green] = 5;   // Domyœlny czas zielonego
    }

    private void Start()
    {
        StartCoroutine(getCarCount());
    }

    private IEnumerator getCarCount()
    {
        while (true)
        {
            carCountOnEntrance = sensor.CarCount;
            yield return new WaitForSeconds(5.0f);  // Co 5 sekund odœwie¿amy liczbê samochodów
        }
    }

    // Ustawienie cyklu œwiate³ (mo¿emy zmieniaæ czasy trwania ka¿dego koloru)
    public void SetTrafficLightCycle(Dictionary<TrafficLightColor, int> cycleToSet)
    {
        foreach (var entry in cycleToSet)
        {
            TimeForTrafficLightColor[entry.Key] = entry.Value;
        }
        StartCoroutine(ChangeTrafficLights());  // Uruchamiamy zmianê œwiate³
    }

    // Korutyna do zmiany œwiate³ na podstawie cyklu
    private IEnumerator ChangeTrafficLights()
    {
        // Tworzymy kopiê kluczy z TimeForTrafficLightColor
        List<TrafficLightColor> colors = new List<TrafficLightColor>(TimeForTrafficLightColor.Keys);

        // Iterujemy po kopii listy kolorów (nie po oryginalnym s³owniku)
        foreach (var color in colors)
        {
            currentLight = color;  // Ustawiamy obecny kolor
            yield return new WaitForSeconds(TimeForTrafficLightColor[color]);  // Czekamy, a¿ czas minie
        }
    }


    public Dictionary<TrafficLightColor, int> GetTrafficLightCycle()
    {
        if (wantWarning)
        {
            foreach (var entry in TimeForTrafficLightColor)
            {
                print("controller: " + this.name + " key: " + entry.Key + " value: " + entry.Value);
            }
        }
        return TimeForTrafficLightColor;  // Zwracamy cykl œwiate³
    }
}
