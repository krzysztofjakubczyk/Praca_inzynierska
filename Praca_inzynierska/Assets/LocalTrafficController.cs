using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalTrafficController : MonoBehaviour
{
    [SerializeField]private Sensor sensor;
    [SerializeField]private int carCountOnEntrance;
    private string carCountFusedLogic;
    public string currentLight { get; private set; }
    
    private void Start()
    {
        StartCoroutine(getCarCount());
        currentLight = "green";
    }

    private IEnumerator getCarCount()
    {
        while (true)
        {
            carCountOnEntrance = sensor.CarCount;
            yield return new WaitForSeconds(5.0f);
        }
    }



}
