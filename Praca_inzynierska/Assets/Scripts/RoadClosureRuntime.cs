using System.Collections.Generic;

public static class RoadClosureRuntime
{
    private static readonly HashSet<int> closedStraightMovementLaneIds = new HashSet<int>();

    public static bool IsStraightMovementClosed(int laneId)
    {
        return closedStraightMovementLaneIds.Contains(laneId);
    }

    public static void SetStraightMovementClosed(IEnumerable<int> laneIds, bool isClosed)
    {
        if (laneIds == null) return;

        foreach (int laneId in laneIds)
        {
            if (isClosed)
            {
                closedStraightMovementLaneIds.Add(laneId);
            }
            else
            {
                closedStraightMovementLaneIds.Remove(laneId);
            }
        }
    }

    public static void Reset()
    {
        closedStraightMovementLaneIds.Clear();
    }
}
