using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    CarController controller;
    [SerializeField] Waypoint[] wayPointForLane;
    [SerializeField] float firstLaneProbability = 20f; // 20% dla pierwszego pasa (na wprost), 80% dla skrętu
    public bool isDrivingStraight; // Czy pojazd jedzie na wprost?
    private int resetStraightTime = 4;

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

        choosedLane = GetLaneIndex(); // Wybór pasa: 0 (na wprost) lub 1 (skręt)
        controller.SetAgentDestination(wayPointForLane[choosedLane].transform.position);
        controller.CurrentWaypoint = wayPointForLane[choosedLane];

        // ✅ Jeśli wybieramy index 0, to jedziemy na wprost
        isDrivingStraight = (choosedLane == 0);
        StartCoroutine(ResetDrivingStraight());
    }

    private IEnumerator ResetDrivingStraight()
    {
        yield return new WaitForSeconds(resetStraightTime);
        isDrivingStraight = false; // Reset flagi po upływie czasu
        Debug.Log("🔄 isDrivingStraight zresetowane do FALSE.");
    }

    private int GetLaneIndex()
    {
        float randomValue = Random.Range(0f, 100f); // Losowa wartość 0-100
        return (randomValue < firstLaneProbability) ? 0 : 1; // 20% szans na wprost (index 0), 80% na skręt (index 1)
    }
}
