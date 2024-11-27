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
        agent.speed = 3f; // Przywrócenie pe³nej prêdkoœci
        isMonitoring = false;
    }

    private IEnumerator MonitorTrafficLight()
    {
        while (isMonitoring)
        {
            switch (trafficController.currentLight)
            {
                case TrafficLightColor.Green:
                    agent.speed = 3f; // Pe³na prêdkoœæ
                    break;
                case TrafficLightColor.Red:
                    agent.speed = 0f; // Zatrzymanie pojazdu
                    break;
                case TrafficLightColor.Yellow:
                    agent.speed = 1.5f; // Zwolnienie przed œwiat³em
                    break;
            }
            yield return new WaitForSeconds(1f); // Unikanie blokowania systemu
        }
    }
}
