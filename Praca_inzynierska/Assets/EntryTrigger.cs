using UnityEngine;

public class EntryTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StartTimer(); // Rozpocznij stoper przy wjeüdzie na skrzyøowanie
                Debug.Log($"Pojazd {other.name} rozpoczπ≥ przejazd.");
            }
        }
    }
}