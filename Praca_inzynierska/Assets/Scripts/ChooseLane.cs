using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] Waypoint[] wayPointForLane;
    [SerializeField] List<int> laneDistribution; // Lista procentowego podzia�u dla ka�dego pasa (np. 50, 30, 20)

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

        int randomValue = Random.Range(0, 100); // Losowanie warto�ci od 0 do 99
        choosedLane = GetLaneIndexFromPercentage(randomValue); // Wyb�r pasa na podstawie procentowego rozk�adu

        controller.SetAgentDestination(wayPointForLane[choosedLane].transform.position);
        controller.CurrentWaypoint = wayPointForLane[choosedLane];
    }

    // Funkcja do wybrania indeksu pasa na podstawie losowej warto�ci i procentowego rozk�adu
    private int GetLaneIndexFromPercentage(int randomValue)
    {
        int cumulativePercentage = 0;

        for (int i = 0; i < laneDistribution.Count; i++)
        {
            cumulativePercentage += laneDistribution[i];
            if (randomValue < cumulativePercentage)
            {
                return i; // Zwr�� indeks pasa, je�li warto�� randomValue mie�ci si� w zakresie
            }
        }

        return laneDistribution.Count - 1; // Zwr�� ostatni pas w razie b��du (zabezpieczenie)
    }
}

