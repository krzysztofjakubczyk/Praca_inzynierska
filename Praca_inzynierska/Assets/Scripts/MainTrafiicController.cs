using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTrafficController : MonoBehaviour
{
    [SerializeField] private List<LocalTrafficController> ControllerList;
    [SerializeField] private Dictionary<LocalTrafficController, int> CarCountOnInlet = new Dictionary<LocalTrafficController, int>();
    [SerializeField] private Dictionary<LocalTrafficController, int> TramCountOnInlet = new Dictionary<LocalTrafficController, int>();
    public bool wantWarning;
    [SerializeField] private float greenLightDuration;  // Duration of the green light
    [SerializeField] private float greenLightDurationForTram;  // Duration of the green light
    [SerializeField] private float yellowLightDuration; // Duration of the yellow light
    [SerializeField] private float yellowLightDurationForTram; // Duration of the yellow light
    [SerializeField]private int currentActiveControllerIndex = 0; // Indeks aktywnego kontrolera
    [SerializeField]private int countOfCars; // Indeks aktywnego kontrolera
    private float timeBeforeGreenLights = 5f;
    private void Start()
    {
        // Pocz¹tkowe ustawienie dla ka¿dego kontrolera
        foreach (var controller in ControllerList)
        {
            CarCountOnInlet.Add(controller, 0);
            TramCountOnInlet.Add(controller, 0);
            controller.SetTrafficLight(TrafficLightColor.Red);
        }

        StartCoroutine(GetVehicleCountOnEntrance());
        StartCoroutine(CycleTrafficLights());
    }

    // Korutyna do zbierania liczby samochodów na wlocie
    private IEnumerator GetVehicleCountOnEntrance()
    {
        while (true)
        {
            Dictionary<LocalTrafficController, int> updatedCarCounts = new Dictionary<LocalTrafficController, int>();
            Dictionary<LocalTrafficController, int> updatedTramCounts = new Dictionary<LocalTrafficController, int>();

            foreach (var kvp in CarCountOnInlet)
            {
                LocalTrafficController trafficController = kvp.Key;
                int carCount = trafficController.carCountOnEntrance;
                updatedCarCounts[trafficController] = carCount;
            }
            foreach (var tram in TramCountOnInlet)
            {
                LocalTrafficController trafficController = tram.Key;
                int tramCountToSave = trafficController.tramCount;
                updatedTramCounts[trafficController] = tramCountToSave;
            }
            foreach (var kvp in updatedCarCounts)
            {
                CarCountOnInlet[kvp.Key] = kvp.Value;
            }
            foreach (var tram in updatedTramCounts)
            {
                TramCountOnInlet[tram.Key] = tram.Value;
            }

            yield return new WaitForSeconds(5f);  // Odœwie¿enie liczby pojazdów co 5 sekund
        }
    }

    // Korutyna do ustawiania cykli œwiate³ w kontrolerach lokalnych
    private IEnumerator CycleTrafficLights()
    {
        while (true)
        {
            // Get the active controller

            LocalTrafficController activeController = ControllerList[currentActiveControllerIndex];
            int carCountOnActiveController = CarCountOnInlet[activeController];
            print(carCountOnActiveController);
            countOfCars = carCountOnActiveController;
            if (activeController.hasTrams)
            {
                int tramCountOnActiveController = TramCountOnInlet[activeController];
                if (tramCountOnActiveController > 0)
                {
                    greenLightDurationForTram = 15f;
                    yield return new WaitForSeconds(greenLightDurationForTram);

                    yellowLightDurationForTram = 3f;
                    yield return new WaitForSeconds(yellowLightDurationForTram);

                }
                activeController.SetTrafficLight(TrafficLightColor.Red);

            }
            string sizeOfJam = null;
            if (carCountOnActiveController <= 3)
            {
                sizeOfJam = "small";
            }
            else if (carCountOnActiveController > 3 && carCountOnActiveController <= 6)
            {
                sizeOfJam = "medium";
                if (wantWarning) print("medium ustawiono");
            }
            else if (carCountOnActiveController > 6 && carCountOnActiveController <= 9)
            {
                sizeOfJam = "big";
            }
            switch (sizeOfJam)
            {
                case "small":
                    if (wantWarning) print("small Jam");
                    greenLightDuration = 10;
                    yellowLightDuration = 5;
                    break;
                case "medium":
                    if (wantWarning) print("medium Jam");
                    greenLightDuration = 15;
                    yellowLightDuration = 5;
                    break;
                case "big":
                    if (wantWarning) print("big Jam");
                    greenLightDuration = 20;
                    yellowLightDuration = 5;
                    break;
                case "nulls":
                    break;
            }
            yield return new WaitForSeconds(timeBeforeGreenLights);
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
