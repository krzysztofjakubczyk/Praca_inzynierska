using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timers : MonoBehaviour
{
    [SerializeField] private float entryTime;
    [SerializeField] private float waitingStartTime; // ➤ Czas rozpoczęcia oczekiwania na światłach
    public float timeSpent;
    public float waitingTime; // ➤ Czas oczekiwania przed światłami
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
        timeSpent = exitTime - entryTime;

        isTracking = false; // Zatrzymaj śledzenie
    }

    public void StartWaitingTimer()
    {
        waitingStartTime = Time.time; // ➤ Zaczynamy liczyć czas oczekiwania
    }

    public void StopWaitingTimer()
    {
        waitingTime = Time.time - waitingStartTime; // ➤ Obliczamy czas oczekiwania przed światłami
    }

    public void SetCameFrom(string toSet)
    {
        cameFrom = toSet;
    }
}
