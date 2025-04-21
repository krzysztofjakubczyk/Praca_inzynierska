using UnityEngine;
using System.Collections.Generic;

public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StopTimer(other.gameObject);

                string cameFrom = timer.cameFrom;
                float travelTime = timer.timeSpentOnTraffic;

                int laneID = GetLaneID(cameFrom); // Pobieramy numer pasa
                if (laneID != -1)
                {
                    StatisticManagerForSczanieckiej.RecordTimeSpent(laneID, travelTime);
                }
            }
        }
    }

    private int GetLaneID(string cameFrom)
    {
        if (!string.IsNullOrEmpty(cameFrom) && char.IsDigit(cameFrom[0]))
        {
            return int.Parse(cameFrom[0].ToString());
        }
        return -1;
    }
}
