using System.Collections.Generic;
using UnityEngine;

public class ChooseLane : MonoBehaviour
{
    [SerializeField] private Waypoint[] lane0Waypoints; // Trasa na wprost
    [SerializeField] private Waypoint[] lane1Waypoints; // Trasa alternatywna
    [SerializeField] private TrafficMovement lane0Movement = TrafficMovement.Straight;
    [SerializeField] private TrafficMovement lane1Movement = TrafficMovement.Left;

    [Header("Debug")]
    public bool wantWarnings;
    private readonly List<CarController> waitingControllers = new List<CarController>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car") && !other.CompareTag("Bus")) return;

        CarController controller = other.GetComponentInParent<CarController>();
        if (controller == null) return;

        if (controller.hasChosenLaneRoute) return;
        if (!TryChooseRoute(controller) && !waitingControllers.Contains(controller))
        {
            waitingControllers.Add(controller);
        }
    }

    private void Update()
    {
        // If this lane has no open route during the closure, keep vehicles at the stop
        // point and retry after the event changes or ends.
        for (int i = waitingControllers.Count - 1; i >= 0; i--)
        {
            CarController controller = waitingControllers[i];
            if (controller == null || TryChooseRoute(controller))
            {
                waitingControllers.RemoveAt(i);
            }
        }
    }

    private bool TryChooseRoute(CarController controller)
    {
        if (controller.hasChosenLaneRoute) return true;

        int preferredIndex = controller.isTurningLeft ? 1 : 0;
        TrafficMovement preferredMovement = preferredIndex == 0 ? lane0Movement : lane1Movement;
        Waypoint[] preferredPath = GetPath(preferredIndex);

        bool preferredPathAvailable = IsValidPath(preferredPath)
            && !RoadClosureRuntime.IsMovementClosed(controller.sourceLaneId, preferredMovement);

        int selectedIndex = preferredIndex;
        if (!preferredPathAvailable)
        {
            List<int> openRoutes = new List<int>();
            AddRouteIfOpen(openRoutes, 0, controller.sourceLaneId);
            AddRouteIfOpen(openRoutes, 1, controller.sourceLaneId);

            if (openRoutes.Count == 0)
            {
                controller.stopForRoadClosure = true;
                return false;
            }

            selectedIndex = openRoutes[Random.Range(0, openRoutes.Count)];
        }

        TrafficMovement selectedMovement = selectedIndex == 0 ? lane0Movement : lane1Movement;
        Waypoint[] selectedPath = GetPath(selectedIndex);
        controller.isTurningLeft = selectedMovement == TrafficMovement.Left;
        controller.stopForRoadClosure = false;
        controller.SetFirstWaypoint(selectedPath);
        controller.hasChosenLaneRoute = true;

        if (wantWarnings)
        {
            Debug.Log($"Pas {controller.sourceLaneId}: wybrano {selectedMovement} " +
                      $"(pojazd {controller.name}).");
        }

        return true;
    }

    private void AddRouteIfOpen(List<int> routes, int index, int laneId)
    {
        Waypoint[] path = GetPath(index);
        TrafficMovement movement = index == 0 ? lane0Movement : lane1Movement;
        if (IsValidPath(path) && !RoadClosureRuntime.IsMovementClosed(laneId, movement))
        {
            routes.Add(index);
        }
    }

    private Waypoint[] GetPath(int index)
    {
        return index == 0 ? lane0Waypoints : lane1Waypoints;
    }

    private bool IsValidPath(Waypoint[] path)
    {
        return path != null && path.Length > 0 && path[0] != null;
    }
}
