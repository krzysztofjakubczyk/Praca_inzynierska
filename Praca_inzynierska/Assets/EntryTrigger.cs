using UnityEngine;

public class EntryTrigger : MonoBehaviour
{
    public int count;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StartTimer(); // Rozpocznij stoper przy wjeüdzie na skrzyøowanie
                count++;
            }
        }
    }
}