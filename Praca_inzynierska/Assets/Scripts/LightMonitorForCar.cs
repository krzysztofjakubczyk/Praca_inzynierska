using UnityEngine;

public class LightMonitorForCar : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        print("Wjechano w trigger");

        // Pobierz komponent CarController i powiadom go o wejœciu w trigger
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StartTrafficLightMonitoring(); // Uruchom monitorowanie œwiate³
        }
    }

    private void OnTriggerExit(Collider other)
    {
        print("Wyjechano z triggera");

        // Pobierz komponent CarController i powiadom go o wyjœciu z triggera
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StopTrafficLightMonitoring(); // Zatrzymaj monitorowanie œwiate³
        }
    }
}
