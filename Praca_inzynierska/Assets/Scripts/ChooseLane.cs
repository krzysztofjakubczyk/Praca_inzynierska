using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    [SerializeField] private Waypoint[] lane0Waypoints; // Trasa na wprost
    [SerializeField] private Waypoint[] lane1Waypoints; // Trasa w lewo

    [Header("Debug")]
    public bool wantWarnings;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car") && !other.CompareTag("Bus")) return;

        CarController controller = other.GetComponentInParent<CarController>();
        if (controller == null) return;

        Waypoint[] selectedPath;

        if (RoadClosureRuntime.IsStraightMovementClosed(controller.sourceLaneId))
        {
            controller.isTurningLeft = true;
        }

        selectedPath = controller.isTurningLeft ? lane1Waypoints : lane0Waypoints;

        controller.SetFirstWaypoint(selectedPath);

        if (wantWarnings)
        {
            Debug.Log($"Pojazd {other.name} wjechał w ChooseLane -> Skręt w lewo: {controller.isTurningLeft}");
        }
    }
}
