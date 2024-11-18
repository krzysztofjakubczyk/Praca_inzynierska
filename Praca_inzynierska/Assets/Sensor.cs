using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Sensor : MonoBehaviour
{
    public int CarCount { get; set; }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        AddCar();
    }
    public void AddCar()
    {
        CarCount++;
    }

    public void ResetCarCount()
    {
        CarCount = 0;
    }


}
