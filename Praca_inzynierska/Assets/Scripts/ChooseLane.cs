using System.Collections;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    [SerializeField] private Waypoint[] lane0Waypoints; // Trasa dla pasa 0 (na wprost)
    [SerializeField] private Waypoint[] lane1Waypoints; // Trasa dla pasa 1 (skręt w lewo)

    [Header("Lane Probabilities")]
    [Range(0f, 100f)]
    [SerializeField] private float straightLaneProbability = 20f; // % szans na pas 0 (na wprost)

    [Header("Debug")]
    public bool wantWarnings;
    public bool isDrivingStraight;
    public int choosedLane;

    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy wjechał pojazd (Car lub Bus)
        if (!other.CompareTag("Car") && !other.CompareTag("Bus")) return;

        if (wantWarnings) Debug.Log($"🚦 Trigger z pojazdem: {other.name}");

        // Szukamy CarController również na obiektach nadrzędnych (rodzicu)
        CarController controller = other.GetComponentInParent<CarController>();
        if (controller == null)
        {
            Debug.LogWarning($"Obiekt {other.name} ani jego rodzic nie ma komponentu CarController!");
            return;
        }

        Waypoint[] selectedPath;

        if (other.CompareTag("Bus"))
        {
            // Autobus zawsze wybiera pas 0 (na wprost)
            choosedLane = 0;
            isDrivingStraight = true;
            selectedPath = lane0Waypoints;
        }
        else
        {
            // Wybieramy pas dla samochodu osobowego
            choosedLane = GetLaneIndex();
            isDrivingStraight = (choosedLane == 0);
            selectedPath = isDrivingStraight ? lane0Waypoints : lane1Waypoints;
        }

        // 1. Ustawiamy trasę waypointów
        controller.SetFirstWaypoint(selectedPath);

        // 2. BEZWZGLĘDNIE ustawiamy czy auto skręca w lewo (Pas 1 = skręt w lewo, Pas 0 = na wprost)
        controller.isTurningLeft = (choosedLane == 1);

        if (wantWarnings)
        {
            Debug.Log($"🚗 Pojazd {other.name} -> Pas: {choosedLane} | Skręt w lewo: {controller.isTurningLeft}");
        }
    }

    private int GetLaneIndex()
    {
        float randomValue = Random.Range(0f, 100f);
        return (randomValue < straightLaneProbability) ? 0 : 1;
    }
}