using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public Waypoint NextWaypoint;
    public bool isBeforeTrafiicLight;
    public LocalTrafficController linkedController;
    public bool isFirstWaypoint;
}
