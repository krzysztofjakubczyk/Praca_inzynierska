using UnityEngine;

public class LightMonitorForCar : MonoBehaviour
{
    public bool wantWarning;
    private void OnTriggerEnter(Collider other)
    {
        //if (wantWarning)
        //{
        //    print("Wjechano w trigger");
        //}
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StartTrafficLightMonitoring(); // Uruchom monitorowanie �wiate�
        }
    }   

    private void OnTriggerExit(Collider other)
    {
        CarController carController = other.GetComponent<CarController>();
        if (carController != null)
        {
            carController.StopTrafficLightMonitoring(); // Zatrzymaj monitorowanie �wiate�
        }
    }
}
