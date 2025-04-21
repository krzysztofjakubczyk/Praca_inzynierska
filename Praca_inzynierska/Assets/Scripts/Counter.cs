using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public int laneID; // Numer pasa, do którego nale¿y ten licznik
    public int countOfVehicles;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car")) // Liczymy tylko pojazdy
        {
            countOfVehicles++;
        }
    }

    public void ResetCounter()
    {
        countOfVehicles = 0;
    }
}
