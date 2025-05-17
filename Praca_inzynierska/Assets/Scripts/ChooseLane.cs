using System.Collections;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    [SerializeField] private Waypoint[] lane0Waypoints; // Trasa dla pasa 0 (np. na wprost)
    [SerializeField] private Waypoint[] lane1Waypoints; // Trasa dla pasa 1 (np. skręt)

    [SerializeField] private float firstLaneProbability = 20f; // % szans na pas 0 (na wprost)
    public bool isDrivingStraight;
    public bool wantWarnings;
    public int choosedLane;

    private int resetStraightTime = 4;

    private void OnTriggerEnter(Collider other)
    {
        if (wantWarnings) Debug.Log($"🚦 Trigger with {other.name}");

        CarController controller = other.GetComponent<CarController>();
        if (controller == null)
        {
            Debug.LogWarning($"Obiekt {other.name} nie ma komponentu CarController!");
            return;
        }

        choosedLane = GetLaneIndex();
        isDrivingStraight = (choosedLane == 0);

        Waypoint[] selectedPath = (choosedLane == 0) ? lane0Waypoints : lane1Waypoints;
        controller.SetFirstWaypoint(selectedPath);

        StartCoroutine(ResetDrivingStraight());
    }

    private IEnumerator ResetDrivingStraight()
    {
        yield return new WaitForSeconds(resetStraightTime);
        isDrivingStraight = false;
    }

    private int GetLaneIndex()
    {
        float randomValue = Random.Range(0f, 100f);
        return (randomValue < firstLaneProbability) ? 0 : 1;
    }
}
