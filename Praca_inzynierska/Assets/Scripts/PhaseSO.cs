using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhaseSO : MonoBehaviour
{
    public List<LineLightManager> lanes;
    public float startTimeInCycle; // dotyczy ca�ej fazy a w LineManager znajduje si� dla poszczeg�lnego pasa w fazie cyklu.
    public float greenLightDuration;
}
