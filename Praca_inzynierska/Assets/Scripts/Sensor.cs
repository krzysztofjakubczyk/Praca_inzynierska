using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Sensor : MonoBehaviour
{
    [SerializeField] public int VehicleCount;
    [SerializeField] public float QueueLength;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        CarController car = other.GetComponent<CarController>();
        if (car != null)
        {
            QueueLength += car.vehicleLength; // Dodaj d�ugo�� pojazdu do kolejki
        }
        VehicleCount++;
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        CarController car = other.GetComponent<CarController>();
        if (car != null)
        {
            QueueLength -= car.vehicleLength; // Zmniejsz d�ugo�� kolejki
        }
        VehicleCount--;

    }
}
