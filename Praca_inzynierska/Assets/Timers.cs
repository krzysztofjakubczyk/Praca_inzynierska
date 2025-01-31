using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timers : MonoBehaviour
{
    [SerializeField]private float entryTime;
    private bool isTracking = false;

    public void StartTimer()
    {
        entryTime = Time.time; // Zapisz czas wejœcia
        isTracking = true;
    }

    public void StopTimer(GameObject vehicle)
    {
        if (!isTracking) return;

        float exitTime = Time.time;
        float timeSpent = exitTime - entryTime;

        Debug.Log($"Pojazd {vehicle.name} spêdzi³ {timeSpent:F2} sekund na skrzy¿owaniu.");

        isTracking = false; // Zatrzymaj œledzenie
    }
}
