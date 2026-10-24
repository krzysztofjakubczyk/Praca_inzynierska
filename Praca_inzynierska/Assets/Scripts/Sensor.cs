using System.Collections.Generic;
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
        // 1. Obsługujemy zarówno samochody osobowe, jak i autobusy
        if (!other.CompareTag("Car") && !other.CompareTag("Bus")) return;

        // 2. Pobieramy główny obiekt (Root), na wypadek gdyby trigger dotknął koła lub karoserii
        GameObject rootVehicle = other.transform.root.gameObject;

        if (!vehiclesInSensor.Contains(rootVehicle))
        {
            vehiclesInSensor.Add(rootVehicle);

            CarController car = rootVehicle.GetComponent<CarController>();
            if (car != null)
            {
                QueueLength += car.vehicleLength;
            }

            // POPRAWKA: Usunięto wywoływanie car.SetMiddleIntersectionState(true)!
            // Auta na skrzyżowaniu nie mogą zamrażać ruchu w pętli kolizji.

            CheckIfBlocking();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Car") && !other.CompareTag("Bus")) return;

        GameObject rootVehicle = other.transform.root.gameObject;

        if (vehiclesInSensor.Contains(rootVehicle))
        {
            vehiclesInSensor.Remove(rootVehicle);

            CarController car = rootVehicle.GetComponent<CarController>();
            if (car != null)
            {
                QueueLength = Mathf.Max(0f, QueueLength - car.vehicleLength);

                // Po opuszczeniu skrzyżowania upewniamy się, że flaga kolizji jest wyłączona
                car.SetMiddleIntersectionState(false);
            }

            CheckIfBlocking();
        }
    }

    private void CheckIfBlocking()
    {
        // Czyszczenie martwych referencji (np. gdy auto zostanie zniszczone wewnątrz sensora)
        vehiclesInSensor.RemoveWhere(v => v == null);
        VehicleCount = vehiclesInSensor.Count;

        // Jeśli na skrzyżowaniu znajdują się więcej niż 2 auta, oznaczamy pas jako zablokowany.
        // Nowe auta zatrzymają się PRZED skrzyżowaniem, a te wewnątrz spokojnie zjadą.
        if (isForCollision && VehicleCount > 2)
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
        vehiclesInSensor.Clear();
        VehicleCount = 0;
        QueueLength = 0;
        isBlockingTraffic = false;
    }

    public bool HasStraightDrivingVehicles()
    {
        vehiclesInSensor.RemoveWhere(v => v == null);

        foreach (var vehicle in vehiclesInSensor)
        {
            if (vehicle == null) continue;
            CarController car = vehicle.GetComponent<CarController>();
            if (car != null && !car.isTurningLeft)
            {
                return true; // Co najmniej jedno auto jedzie na wprost -> BLOKADA
            }
        }
        return false; // Brak aut na wprost (pusto lub auta też skręcają w lewo)
    }
}