using UnityEngine;
using System.Collections.Generic;

public class DensitySensor : MonoBehaviour
{
    public int vehicleCount = 0; // Liczba pojazd�w na pasie
    public float laneLength = 0.5f; // D�ugo�� pasa w km

    private Dictionary<GameObject, float> waitingTimers = new Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            vehicleCount++;

            // Rozpocznij timer dla pojazdu
            if (!waitingTimers.ContainsKey(other.gameObject))
            {
                waitingTimers[other.gameObject] = Time.time;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            vehicleCount = Mathf.Max(0, vehicleCount - 1);

            // Je�li auto czeka�o na �wiat�o, zapisz jego czas
            if (waitingTimers.ContainsKey(other.gameObject))
            {
                float waitingTime = Time.time - waitingTimers[other.gameObject];
                StatisticManagerForSczanieckiej.RecordWaitingTime(waitingTime);
                waitingTimers.Remove(other.gameObject);
            }
        }
    }

    public float GetDensity()
    {
        return vehicleCount / laneLength; // pojazdy/km
    }
}
