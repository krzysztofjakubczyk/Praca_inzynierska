using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTrafficController : MonoBehaviour
{
    [SerializeField] private List<LocalTrafficController> ControllerList;
    [SerializeField] private Dictionary<LocalTrafficController, int> CarCountOnInlet = new Dictionary<LocalTrafficController, int>();
    public bool wantWarning;
    [SerializeField] private float greenLightDuration = 10f;  // Duration of the green light
    [SerializeField] private float yellowLightDuration = 3f; // Duration of the yellow light
    private int currentActiveControllerIndex = 0; // Indeks aktywnego kontrolera

    private void Start()
    {
        // Pocz¹tkowe ustawienie dla ka¿dego kontrolera
        foreach (var controller in ControllerList)
        {
            CarCountOnInlet.Add(controller, 0);
            controller.SetTrafficLight(TrafficLightColor.Red);
        }

        StartCoroutine(GetCarCountOnEntrance());
        StartCoroutine(CycleTrafficLights());
    }

    // Korutyna do zbierania liczby samochodów na wlocie
    private IEnumerator GetCarCountOnEntrance()
    {
        while (true)
        {
            Dictionary<LocalTrafficController, int> updatedCarCounts = new Dictionary<LocalTrafficController, int>();

            foreach (var kvp in CarCountOnInlet)
            {
                LocalTrafficController trafficController = kvp.Key;
                int carCount = trafficController.carCountOnEntrance;
                updatedCarCounts[trafficController] = carCount;
            }

            foreach (var kvp in updatedCarCounts)
            {
                CarCountOnInlet[kvp.Key] = kvp.Value;
            }

            yield return new WaitForSeconds(5f);  // Odœwie¿enie liczby samochodów co 5 sekund
        }
    }

    // Korutyna do ustawiania cykli œwiate³ w kontrolerach lokalnych
    private IEnumerator CycleTrafficLights()
    {
        while (true)
        {
            // Get the active controller
            
            var activeController = ControllerList[currentActiveControllerIndex];
            int carCountOnActiveController = CarCountOnInlet[activeController];
            string sizeOfJam = null;
            if(carCountOnActiveController <= 3)
            {
                sizeOfJam = "small";
            }
            else if(carCountOnActiveController > 3 && carCountOnActiveController <= 6)
            {
                sizeOfJam = "medium";
            }
            else if(carCountOnActiveController > 6 && carCountOnActiveController <= 9)
            {
                sizeOfJam = "big";
            }

            switch (sizeOfJam)
            {
                case "small":
                    print("small Jam");
                    greenLightDuration = 5;
                    yellowLightDuration = 3;
                    break;
                case "medium":
                    print("medium Jam");

                    greenLightDuration = 7;
                    yellowLightDuration = 2;
                    break;
                case "big":
                    print("big Jam");

                    greenLightDuration = 10;
                    yellowLightDuration = 2;
                    break;
                case "nulls":
                    break;
            }
            // Set the active controller to green
            activeController.SetTrafficLight(TrafficLightColor.Green);
            yield return new WaitForSeconds(greenLightDuration);

            // Transition to yellow
            activeController.SetTrafficLight(TrafficLightColor.Yellow);
            yield return new WaitForSeconds(yellowLightDuration);

            // Set the current controller to red
            activeController.SetTrafficLight(TrafficLightColor.Red);

            // Move to the next controller (loop back to the start if at the end)
            currentActiveControllerIndex = (currentActiveControllerIndex + 1) % ControllerList.Count;
        }
    }
}
