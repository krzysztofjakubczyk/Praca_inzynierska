using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour
{
    [SerializeField] public int VehicleCount;
    [SerializeField] public float QueueLength;
    public bool isForCollision;
    public bool isForLeftTurnCheck;
    public bool isInMiddleOfIntersection; // NOWE: Czy sensor jest na œrodku skrzy¿owania?
    public bool isBlockingTraffic = false;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        CarController car = other.GetComponent<CarController>();
        if (car != null)
        {
            QueueLength += car.vehicleLength;
        }
        VehicleCount++;

        CheckIfBlocking();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        CarController car = other.GetComponent<CarController>();
        if (car != null)
        {
            QueueLength -= car.vehicleLength;
        }
        VehicleCount--;

        CheckIfBlocking();
    }

    private void CheckIfBlocking()
    {
        if (isForCollision && VehicleCount > 3) // Blokada wjazdu na skrzy¿owanie
        {
            isBlockingTraffic = true;
        }
        else
        {
            isBlockingTraffic = false;
        }
    }
}
