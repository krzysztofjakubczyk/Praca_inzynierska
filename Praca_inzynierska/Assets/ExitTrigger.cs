using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq; // Dodaj to!

public class ExitTrigger : MonoBehaviour
{
    public List<float> timeForRiding = new List<float>();
    public float meanOfComingTimes;
    public List<string> whereCameFrom = new List<string>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StopTimer(other.gameObject); // Zatrzymaj stoper przy wyjeŸdzie
                timeForRiding.Add(timer.timeSpent);
                whereCameFrom.Add(timer.cameFrom);
            }
        }
    }
    private void Update()
    {
        if(timeForRiding.Count > 0)
        meanOfComingTimes = timeForRiding.Average();        
    }
}
