using UnityEngine;

public class LightMonitorForCar : MonoBehaviour
{
    public bool wantWarning;
    private void OnTriggerEnter(Collider other)
    {
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {

            carController.StartTrafficLightMonitoring(); // Uruchom monitorowanie świateł
        }
    }   

    private void OnTriggerExit(Collider other)
    {
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StopTrafficLightMonitoring(); // Zatrzymaj monitorowanie świateł
        }
    }
}
