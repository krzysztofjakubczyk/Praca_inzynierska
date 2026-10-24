using UnityEngine;

public class CarDestroyer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy czy wjecha³o auto lub autobus
        if (other.CompareTag("Car") || other.CompareTag("Bus"))
        {
            // Niszczymy CA£Y pojazd wraz z rodzicem (transform.root),
            // nawet jeœli Trigger dotkn¹³ tylko ko³a lub karoserii.
            Destroy(other.transform.root.gameObject);
        }
    }
}