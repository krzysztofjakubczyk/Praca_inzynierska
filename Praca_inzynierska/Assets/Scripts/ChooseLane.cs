using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] Waypoint[] wayPointForLane;
    [SerializeField] List<int> laneDistribution; // Lista procentowego podzia³u dla ka¿dego pasa (np. 50, 30, 20)

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

        int randomValue = Random.Range(0, 100); // Losowanie wartoœci od 0 do 99
        choosedLane = GetLaneIndexFromPercentage(randomValue); // Wybór pasa na podstawie procentowego rozk³adu

        controller.SetAgentDestination(wayPointForLane[choosedLane].transform.position);
        controller.CurrentWaypoint = wayPointForLane[choosedLane];
    }

    // Funkcja do wybrania indeksu pasa na podstawie losowej wartoœci i procentowego rozk³adu
    private int GetLaneIndexFromPercentage(int randomValue)
    {
        int cumulativePercentage = 0;

        for (int i = 0; i < laneDistribution.Count; i++)
        {
            cumulativePercentage += laneDistribution[i];
            if (randomValue < cumulativePercentage)
            {
                return i; // Zwróæ indeks pasa, jeœli wartoœæ randomValue mieœci siê w zakresie
            }
        }

        return laneDistribution.Count - 1; // Zwróæ ostatni pas w razie b³êdu (zabezpieczenie)
    }
}

