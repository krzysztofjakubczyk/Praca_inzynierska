﻿using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour
{
    [SerializeField] public int VehicleCount;
    [SerializeField] public float QueueLength;
    public bool isForCollision;
    public bool isForLeftTurnCheck;
    public bool isBlockingTraffic = false;

    private HashSet<GameObject> vehiclesInSensor = new HashSet<GameObject>(); // Unikalne pojazdy w sensorze

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;

        if (!vehiclesInSensor.Contains(other.gameObject)) // Zapobiega wielokrotnemu dodawaniu
        {
            vehiclesInSensor.Add(other.gameObject);
            VehicleCount++;

            CarController car = other.GetComponent<CarController>();
            if (car != null)
            {
                QueueLength += car.vehicleLength;
            }

            if (isForCollision)
            {
                car.SetMiddleIntersectionState(true);
            }

            CheckIfBlocking();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Car")) return;

        if (vehiclesInSensor.Contains(other.gameObject)) // Usuwamy pojazd tylko jeśli był w sensorze
        {
            vehiclesInSensor.Remove(other.gameObject);
            VehicleCount = Mathf.Max(0, VehicleCount - 1);

            CarController car = other.GetComponent<CarController>();
            if (car != null)
            {
                QueueLength -= car.vehicleLength;
            }

            if (isForCollision)
            {
                car.SetMiddleIntersectionState(false);
            }

            CheckIfBlocking();
        }
    }

    private void CheckIfBlocking()
    {
        if (isForCollision && VehicleCount > 3) // Blokada wjazdu na skrzyżowanie
        {
            isBlockingTraffic = true;
        }
        else
        {
            isBlockingTraffic = false;
        }
    }

    public void ResetSensor()
    {
        VehicleCount = 0;
        QueueLength = 0;
        isBlockingTraffic = false;
    }

}
