using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LightMonitorForCar : MonoBehaviour
{
    NavMeshAgent agent;
    LocalTrafficController trafficController;
    private bool isMonitoring = false;

    private void OnTriggerEnter(Collider other)
    {
        agent = other.GetComponent<NavMeshAgent>();
        trafficController = other.GetComponent<CarController>().controller;

        if (!isMonitoring)
        {
            isMonitoring = true;
            StartCoroutine(MonitorTrafficLight());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        StopCoroutine(MonitorTrafficLight());
        agent.speed = 3f; // Przywr�cenie pe�nej pr�dko�ci
        isMonitoring = false;
    }

    private IEnumerator MonitorTrafficLight()
    {
        while (isMonitoring)
        {
            switch (trafficController.currentLight)
            {
                case TrafficLightColor.Green:
                    agent.speed = 3f; // Pe�na pr�dko��
                    break;
                case TrafficLightColor.Red:
                    agent.speed = 0f; // Zatrzymanie pojazdu
                    break;
                case TrafficLightColor.Yellow:
                    agent.speed = 1.5f; // Zwolnienie przed �wiat�em
                    break;
            }
            yield return new WaitForSeconds(1f); // Unikanie blokowania systemu
        }
    }
}
