using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhaseSO : MonoBehaviour
{
    public List<LineLightManager> lanes;
    public float startTimeInCycle; // dotyczy ca³ej fazy a w LineManager znajduje siê dla poszczególnego pasa w fazie cyklu.
    public float greenLightDuration;
}
