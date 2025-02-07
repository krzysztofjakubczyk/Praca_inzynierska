using UnityEngine;
using System.Collections.Generic;

public class EntryTrigger : MonoBehaviour
{
    public int count;
    public List<float> travelTimes = new List<float>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.SetCameFrom(gameObject.name);
                timer.StartTimer();
                count++;
            }
        }
    }

    public void AddTravelTime(float time)
    {
        travelTimes.Add(time);
    }

    public float GetAverageTravelTime()
    {
        if (travelTimes.Count == 0) return 0;
        float sum = 0;
        foreach (float t in travelTimes)
        {
            sum += t;
        }
        return sum / travelTimes.Count;
    }
}
