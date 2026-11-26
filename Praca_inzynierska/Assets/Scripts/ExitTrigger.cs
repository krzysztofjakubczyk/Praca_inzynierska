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
        // Liczymy samochody i autobusy (wcześniej autobusy były pomijane).
        if (!other.CompareTag("Car") && !other.CompareTag("Bus")) return;

        GameObject root = other.transform.root.gameObject;
        if (CountedVehicleIds.Add(root.GetInstanceID()))
            TotalVehiclesExited++;

        Timers timer = root.GetComponentInChildren<Timers>();
        if (timer == null) return;

        timer.StopTimer(root);
        int laneID = GetLaneID(timer.cameFrom);
        if (laneID != -1)
            StatisticManagerForSczanieckiej.RecordTimeSpent(laneID, timer.timeSpentOnTraffic);
    }

    private int GetLaneID(string cameFrom)
    {
        if (!string.IsNullOrEmpty(cameFrom) && char.IsDigit(cameFrom[0]))
            return int.Parse(cameFrom[0].ToString());
        return -1;
    }
}
