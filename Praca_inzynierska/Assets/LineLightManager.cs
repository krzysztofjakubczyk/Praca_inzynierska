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

    private void Start()
    {
        StartCoroutine(setNumberOfVehicles());
    }

    private IEnumerator setNumberOfVehicles()
    {
        while (true)
        {
            countOfVehicles = 0;
            queueLength = 0;
            foreach (var sensor in sensors)
            {
                countOfVehicles += sensor.VehicleCount;
                queueLength += sensor.QueueLength;
            }

            yield return new WaitForSeconds(5f);
        }
    }

    public void ChangeColor(TrafficLightColor color)
    {
        currentColor = color;
    }
}
