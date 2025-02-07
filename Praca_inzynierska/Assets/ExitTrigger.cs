using System.Collections.Generic;
using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    public delegate void VehicleExitEvent(string cameFrom, float travelTime);
    public static event VehicleExitEvent OnVehicleExit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StopTimer(other.gameObject);

                string cameFrom = timer.cameFrom;
                float travelTime = timer.timeSpent;

                // Przekazanie danych do StatisticManager
                OnVehicleExit?.Invoke(cameFrom, travelTime);
            }
        }
    }
}
