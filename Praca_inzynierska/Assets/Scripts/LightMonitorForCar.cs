using UnityEngine;

public class LightMonitorForCar : MonoBehaviour
{
    public bool wantWarning;

    private void OnTriggerEnter(Collider other)
    {
        // Szukamy kontrolera również na obiekcie rodzica (transform.root)
        CarController carController = other.GetComponentInParent<CarController>();
        if (carController != null)
        {
            carController.StartTrafficLightMonitoring();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CarController carController = other.GetComponentInParent<CarController>();
        if (carController != null)
        {
            carController.StopTrafficLightMonitoring();
        }
    }
}