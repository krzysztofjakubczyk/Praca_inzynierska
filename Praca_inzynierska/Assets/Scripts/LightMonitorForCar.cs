using UnityEngine;

public class LightMonitorForCar : MonoBehaviour
{
    public bool wantWarning;
    private void OnTriggerEnter(Collider other)
    {
        if (wantWarning)
        {
            print("Wjechano w trigger");
        }
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StartTrafficLightMonitoring(); // Uruchom monitorowanie œwiate³
        }
    }   

    private void OnTriggerExit(Collider other)
    {
        //if (wantWarning)
        //{
        //    print("Wyjechano z triggera");
        //}
        // Pobierz komponent CarController i powiadom go o wyjœciu z triggera
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StopTrafficLightMonitoring(); // Zatrzymaj monitorowanie œwiate³
        }
    }
}
