using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrafficLightColor
{
    red,
    yellow,
    green,
}

public class LineLightManager : MonoBehaviour
{
    [SerializeField] public TrafficLightColor currentColor;
    [SerializeField] public int countOfVehicles;
    [SerializeField] public float queueLength;
    [SerializeField] public int idAtDraw;
    public List<Sensor> sensors;
    public bool isBlocked = false; // Czy pas jest zablokowany?
    public bool leftTurnAllowed = false; // Czy można skręcać w lewo?
    public bool leftTurnWaiting = false; // Czy są auta na środku skrzyżowania?

    private void Start()
    {
        StartCoroutine(UpdateTrafficStateRoutine());
    }

    private IEnumerator UpdateTrafficStateRoutine()
    {
        while (true)
        {
            UpdateTrafficState();
            yield return new WaitForSeconds(3f); // Sprawdzamy co 3 sekundy
        }
    }

    public void UpdateTrafficState()
    {
        countOfVehicles = 0;
        queueLength = 0;
        bool shouldBlock = false;
        bool leftTurnBlocked = false;
        leftTurnWaiting = false; // Resetujemy flagę

        foreach (var sensor in sensors)
        {
            if (sensor.isForCollision)
            {
                if (sensor.isBlockingTraffic)
                {
                    shouldBlock = true; // Jeśli chociaż jeden sensor zgłasza blokadę → blokujemy pas
                }
            }
            else if (sensor.isForLeftTurnCheck)
            {
                if (sensor.VehicleCount > 0)
                {
                    leftTurnBlocked = true; // Jeśli na wprost są auta, skręt w lewo jest zablokowany
                }
            }
            else if (sensor.isInMiddleOfIntersection)
            {
                if (sensor.VehicleCount > 0)
                {
                    leftTurnWaiting = true; // Auta czekają na środku skrzyżowania
                }
            }
            else
            {
                countOfVehicles += sensor.VehicleCount;
                queueLength += sensor.QueueLength;
            }
        }

        // PRZENIESIONE: Aktualizujemy PO pętli, nie w trakcie!
        leftTurnAllowed = !leftTurnBlocked; // Jeśli NIE ma aut na wprost, można skręcać w lewo

        if (shouldBlock)
        {
            BlockLane();
        }
        else
        {
            UnblockLane();
        }
    }

    private void BlockLane()
    {
        if (!isBlocked) // Jeśli pas NIE był zablokowany, to go blokujemy
        {
            isBlocked = true;
            if (currentColor != TrafficLightColor.red)
            {
                ChangeColor(TrafficLightColor.red);
                Debug.Log($"🚦 {gameObject.name} ZABLOKOWANY – czerwone światło!");
            }
        }
    }

    private void UnblockLane()
    {
        if (isBlocked) // Jeśli pas BYŁ zablokowany, to go odblokowujemy
        {
            isBlocked = false;
            if (currentColor != TrafficLightColor.green)
            {
                ChangeColor(TrafficLightColor.green);
                Debug.Log($"✅ {gameObject.name} ODBLOKOWANY – zielone światło!");
            }
        }
    }

    public void ChangeColor(TrafficLightColor color)
    {
        currentColor = color;
    }
}
