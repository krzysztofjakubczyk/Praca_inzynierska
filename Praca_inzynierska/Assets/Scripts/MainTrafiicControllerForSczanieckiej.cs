using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainTrafficControllerSczanieckiej : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<LineLightManager> Phase1 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase2 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase3 = new List<LineLightManager>();
    [SerializeField] private List<List<LineLightManager>> listToChangeColors = new List<List<LineLightManager>>();

    [SerializeField] private Dictionary<LineLightManager, int> CarCountOnInlet = new Dictionary<LineLightManager, int>();

    [Header("Floats and ints")]
    [SerializeField] private float greenLightDuration;  // Duration of the green light
    [SerializeField] private float yellowLightDuration; // Duration of the yellow light
    [SerializeField] private int countOfCars; // Indeks aktywnego kontrolera

    private float timeBeforeGreenLights = 5f;
    public bool wantWarning;
    private void Start()
    {
        listToChangeColors.Add(Phase1);
        listToChangeColors.Add(Phase2);
        listToChangeColors.Add(Phase3);
        foreach (var listForPhase in listToChangeColors)
        {
            foreach (var lineInPhase in listForPhase)
            {
                CarCountOnInlet.Add(lineInPhase, 0);
                lineInPhase.ChangeColor(TrafficLightColor.red);
            }
        }
        StartCoroutine(GetVehicleCountOnEntrance());
        StartCoroutine(CycleTrafficLights());
    }

    // Korutyna do zbierania liczby samochod�w na wlocie
    private IEnumerator GetVehicleCountOnEntrance()
    {
        while (true)
        {
            Dictionary<LineLightManager, int> updatedCarCount = new Dictionary<LineLightManager, int>();
            // tymczasowy s�ownik by nie edytowa� przetwarzanego s�ownika

            foreach (var kvp in CarCountOnInlet) //przejdz sobie po ilosci aut
            {
                LineLightManager LineController = kvp.Key; //lineManager
                int countOfVehiclesOnLine = LineController.countOfVehicles; //tymczasowa zmienna, kt�ra wyra�a ilo�� aut na danym pasie
                updatedCarCount[LineController] = countOfVehiclesOnLine; //wartosc dla inta w slownika sterownik�w jest zapisana do tymczasowego s�ownika
            }

            foreach (var kvp in updatedCarCount) // Przejd� po tymczasowym s�owniku
            {
                LineLightManager LineController = kvp.Key; //lineManager
                int countOfVehiclesOnLine = LineController.countOfVehicles; //tymczasowa zmienna, kt�ra wyra�a ilo�� aut na danym pasie
                CarCountOnInlet[LineController] = countOfVehiclesOnLine; // Przenie� dane z tymczasowego s�ownika do g��wnego
            }
            yield return new WaitForSeconds(5f);  // Od�wie�enie liczby pojazd�w co 5 sekund
        }
    }

    // Korutyna do ustawiania cykli �wiate� w kontrolerach lokalnych
    private IEnumerator CycleTrafficLights()
    {
        while (true)
        {
            // Przejd� przez wszystkie fazy
            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                // Ustaw aktywn� faz�
                manageTimeOfColors(listToChangeColors[phaseIndex]);
                // Ustaw �wiat�a zielone tylko dla bie��cej fazy
                foreach (var lineInPhase in listToChangeColors[phaseIndex])
                {
                    lineInPhase.ChangeColor(TrafficLightColor.green);
                }

                // Ustaw �wiat�a czerwone dla pozosta�ych faz
                for (int otherPhaseIndex = 0; otherPhaseIndex < listToChangeColors.Count; otherPhaseIndex++)
                {
                    if (otherPhaseIndex != phaseIndex)
                    {
                        foreach (var lineInPhase in listToChangeColors[otherPhaseIndex])
                        {
                            lineInPhase.ChangeColor(TrafficLightColor.red);
                        }
                    }
                }

                // Odczekaj czas zielonego �wiat�a
                yield return new WaitForSeconds(greenLightDuration);

                // Zmie� �wiat�a bie��cej fazy na ��te
                foreach (var lineInPhase in listToChangeColors[phaseIndex])
                {
                    lineInPhase.ChangeColor(TrafficLightColor.yellow);
                }

                // Odczekaj czas ��tego �wiat�a
                yield return new WaitForSeconds(yellowLightDuration);

                // Ustaw czerwone �wiat�a na ko�cu dla bie��cej fazy
                foreach (var lineInPhase in listToChangeColors[phaseIndex])
                {
                    lineInPhase.ChangeColor(TrafficLightColor.red);
                }
            }
        }
    }


    private void manageTimeOfColors(List<LineLightManager> listOfPhase)
    {
        int carCountOnActivePhase = 0;
        foreach (var line in listOfPhase)
        {
            if (CarCountOnInlet.ContainsKey(line))
            {
                carCountOnActivePhase += CarCountOnInlet[line]; 
            }
        }
        string sizeOfJam = null;
        if (carCountOnActivePhase <= 10)
        {
            sizeOfJam = "small";
        }
        else if (carCountOnActivePhase > 10 && carCountOnActivePhase <= 15)
        {
            sizeOfJam = "medium";
            if (wantWarning) print("medium ustawiono");
        }
        else if (carCountOnActivePhase > 15 && carCountOnActivePhase <= 30)
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
    }
}
