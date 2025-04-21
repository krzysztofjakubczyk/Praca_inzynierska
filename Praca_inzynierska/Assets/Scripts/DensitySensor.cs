using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DensitySensor : MonoBehaviour
{
    public int vehicleCount = 0;
    public float laneLength = 0.5f;

    private Dictionary<GameObject, float> waitingTimers = new Dictionary<GameObject, float>();

    private int GetLaneID()
    {
        // Pobieramy pierwszy znak nazwy i konwertujemy na int
        if (char.IsDigit(gameObject.name[0]))
        {
            return int.Parse(gameObject.name[0].ToString());
        }
        else
        {
            Debug.LogError($"❌ DensitySensor {gameObject.name} ma niepoprawną nazwę! Pierwszy znak nie jest liczbą.");
            return -1; // Zwracamy -1, jeśli nie udało się odczytać numeru pasa
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            vehicleCount++;

            int laneID = GetLaneID();
            if (laneID != -1)
            {
                if (!StatisticManagerForSczanieckiej.vehicleCountPerLane.ContainsKey(laneID))
                {
                    StatisticManagerForSczanieckiej.vehicleCountPerLane[laneID] = 0;
                }
                StatisticManagerForSczanieckiej.vehicleCountPerLane[laneID]++;
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            vehicleCount = Mathf.Max(0, vehicleCount - 1);

            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StopWaitingTimer(); // Koniec liczenia czasu oczekiwania
                float waitingTime = timer.waitingTimeForGreenLight;

                int laneID = GetLaneID();
                if (laneID != -1)
                {
                    StatisticManagerForSczanieckiej.RecordWaitingTime(laneID, waitingTime);
                }
            }
        }
    }
    public void ResetCounter()
    {
        vehicleCount = 0;
    }

    public float GetDensity()
    {
        return vehicleCount / laneLength;
    }
}
