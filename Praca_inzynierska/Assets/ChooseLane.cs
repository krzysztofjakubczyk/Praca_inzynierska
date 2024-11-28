using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] Waypoint[] wayPointForLane;
    public int choosedLane;
    public bool wantWarnings;

    private void OnTriggerEnter(Collider other)
    {
        if (wantWarnings) { print(other.name); }
        controller = other.GetComponent<CarController>();
        choosedLane = Random.Range(0, 2);
        controller.SetAgentDestination(wayPointForLane[choosedLane].transform.position);
        controller.currentWaypoint = wayPointForLane[choosedLane];
    }

}
