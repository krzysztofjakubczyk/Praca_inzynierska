using UnityEngine;
using System.Collections.Generic;

public class ExitTrigger : MonoBehaviour
{
    public static int TotalVehiclesExited { get; private set; }
    private static readonly HashSet<int> CountedVehicleIds = new HashSet<int>();

    public static void ResetTotalVehiclesExited()
    {
        TotalVehiclesExited = 0;
        CountedVehicleIds.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            if (CountedVehicleIds.Add(other.transform.root.gameObject.GetInstanceID()))
                TotalVehiclesExited++;
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
