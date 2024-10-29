using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] GameObject[] wayPointForLane;

    private void OnTriggerEnter(Collider other)
    {
        print(other.name);
        controller = other.GetComponent<CarController>();
        int choosedLane = Random.Range(0, wayPointForLane.Length);
        controller.agent.SetDestination(wayPointForLane[choosedLane].transform.position);
    }

}
