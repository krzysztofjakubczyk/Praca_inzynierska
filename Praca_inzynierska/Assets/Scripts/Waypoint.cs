using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Waypoint NextWaypoint;
    public ChooseLane laneChooser;
    public bool isBeforeTrafiicLight;
    public LineLightManager linkedController;
    public bool isFirstWaypoint;
    public bool isAfterTrafiicLight;

    public void Awake()
    {
        if (!isBeforeTrafiicLight)
        {
            laneChooser = null;
        }
        
    }
}
