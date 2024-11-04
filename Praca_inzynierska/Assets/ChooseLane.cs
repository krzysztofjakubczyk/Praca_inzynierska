using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] Waypoint[] wayPointForLane;

    private void OnTriggerEnter(Collider other)
    {
        print(other.name);
        controller = other.GetComponent<CarController>();
        int choosedLane = Random.Range(0, 1);
        controller.SetAgentDestination(wayPointForLane[choosedLane].transform.position);
        controller.currentWaypoint = wayPointForLane[choosedLane];
    }

}
