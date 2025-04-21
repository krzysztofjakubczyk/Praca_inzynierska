using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timers : MonoBehaviour
{
    [SerializeField] private float entryTime;
    [SerializeField] private float waitingStartTime; // ➤ Czas rozpoczęcia oczekiwania na światłach
    public float timeSpentOnTraffic;
    public float waitingTimeForGreenLight; // ➤ Czas oczekiwania przed światłami
    private bool isTracking = false;
    public string cameFrom;

    public void StartTimer()
    {
        entryTime = Time.time; // Zapisz czas wejścia
        isTracking = true;
    }

    public void StopTimer(GameObject vehicle)
    {
        if (!isTracking) return;

        float exitTime = Time.time;
        timeSpentOnTraffic = exitTime - entryTime;

        isTracking = false; // Zatrzymaj śledzenie
    }

    public void StartWaitingTimer()
    {
        waitingStartTime = Time.time; // ➤ Zaczynamy liczyć czas oczekiwania
    }

    public void StopWaitingTimer()
    {
        waitingTimeForGreenLight = Time.time - waitingStartTime; // ➤ Obliczamy czas oczekiwania przed światłami
    }

    public void SetCameFrom(string stringToSet)
    {
        cameFrom = stringToSet;
    }
}
