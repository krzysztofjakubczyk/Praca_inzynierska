using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Sensor : MonoBehaviour
{
    [SerializeField] public int CarCount;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        CarCount++;
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        CarCount--;

    }
}
