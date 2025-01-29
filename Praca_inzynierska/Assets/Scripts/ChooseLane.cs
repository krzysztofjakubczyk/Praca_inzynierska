using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] Waypoint[] wayPointForLane;
    [SerializeField] float firstLaneProbability = 20f; // 20% dla pierwszego pasa, 80% dla drugiego

    public int choosedLane;
    public bool wantWarnings;

    private void OnTriggerEnter(Collider other)
    {
        if (wantWarnings) { print(other.name); }

        controller = other.GetComponent<CarController>();
        if (controller == null)
        {
            Debug.LogWarning("Obiekt nie ma komponentu CarController!");
            return;
        }

        choosedLane = GetLaneIndex(); // Wybór pasa z prawdopodobieñstwem 20% / 80%
        controller.SetAgentDestination(wayPointForLane[choosedLane].transform.position);
        controller.CurrentWaypoint = wayPointForLane[choosedLane];
    }

    private int GetLaneIndex()
    {
        float randomValue = Random.Range(0f, 100f); // Losowa wartoœæ 0-100
        return (randomValue < firstLaneProbability) ? 0 : 1; // 20% szans na pas 0, 80% na pas 1
    }
}
