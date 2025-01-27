using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{
    public int countOfVehicles;
    private void OnTriggerEnter(Collider other)
    {
        countOfVehicles++;
    }
}
