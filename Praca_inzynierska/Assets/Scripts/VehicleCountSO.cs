using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "VehicleCount", menuName = "ScriptableObjects/VehicleCountScriptableObject", order = 1)]

public class VehicleCountSO : ScriptableObject
{
    public List<int> CountOfVehicles;
    public HourSO ChoosedHour;
}
