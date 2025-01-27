using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Timers timer = other.GetComponent<Timers>();
            if (timer != null)
            {
                timer.StopTimer(other.gameObject); // Zatrzymaj stoper przy wyjeüdzie
            }
        }
    }
}
