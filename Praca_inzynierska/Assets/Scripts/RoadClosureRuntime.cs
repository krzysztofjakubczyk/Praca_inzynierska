using UnityEngine;

public static class RoadClosureRuntime
{
    private static RoadClosureEventSettings currentEvent;
    private static TimeManager timeManager;

    public static void Configure(RoadClosureEventSettings eventSettings)
    {
        currentEvent = eventSettings;
        timeManager = Object.FindObjectOfType<TimeManager>();
    }

    public static bool IsMovementClosed(int laneId, TrafficMovement movement)
    {
        if (currentEvent == null || !currentEvent.enabled || currentEvent.durationSeconds <= 0f)
        {
            return false;
        }

        if (timeManager == null)
        {
            timeManager = Object.FindObjectOfType<TimeManager>();
        }

        if (timeManager == null || currentEvent.closedLaneMovements == null) return false;

        float eventEndTime = currentEvent.startTimeSeconds + currentEvent.durationSeconds;
        bool eventIsActive = timeManager.SimulationTime >= currentEvent.startTimeSeconds
            && timeManager.SimulationTime < eventEndTime;

        if (!eventIsActive) return false;

        foreach (LaneMovementClosure closure in currentEvent.closedLaneMovements)
        {
            if (closure != null && closure.laneId == laneId && closure.movement == movement)
            {
                return true;
            }
        }

        return false;
    }

    public static void Reset()
    {
        currentEvent = null;
        timeManager = null;
    }
}
