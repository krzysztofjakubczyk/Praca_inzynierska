using FLS;
using FLS.MembershipFunctions;
using FLS.Rules;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainTrafficController : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<LineLightManager> Phase1 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase2 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase3 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase4 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase5 = new List<LineLightManager>();
    [SerializeField] private List<List<LineLightManager>> listToChangeColors = new List<List<LineLightManager>>();

    [SerializeField] private Dictionary<LineLightManager, int> CarCountOnInlet = new Dictionary<LineLightManager, int>();
    [SerializeField] private Dictionary<LineLightManager, float> CarQueueOnInlet = new Dictionary<LineLightManager, float>();

    [Header("Floats and ints")]
    [SerializeField] private float greenLightDuration;  // Duration of the green light
    [SerializeField] private float yellowLightDuration; // Duration of the yellow light
    [SerializeField] private int countOfCars; // Indeks aktywnego kontrolera

    private float timeBeforeGreenLights = 5f;
    public bool wantWarning;

    IFuzzyEngine fuzzyEngine;
    LinguisticVariable carCount, queueLength, greenLightDurationVar;
    IMembershipFunction nullCount, lowCount, mediumCount, HighCount, nullQueue, smallQueue,
    mediumQueue, bigQueue, nullGreen, shortGreen, mediumGreen, longGreen;
    FuzzyRule rule0, rule1, rule2, rule3, rule4, rule5, rule6, rule7, rule8, rule9;
    private void Start()
    {
        fuzzyEngine = new FuzzyEngineFactory().Default();


        carCount = new LinguisticVariable("CarCount");
        nullCount = carCount.MembershipFunctions.AddTrapezoid("Null", 0, 0, 0, 0);
        lowCount = carCount.MembershipFunctions.AddTrapezoid("Low", 0, 0, 10, 20);
        mediumCount = carCount.MembershipFunctions.AddTrapezoid("Medium", 10, 15, 20, 20);
        HighCount = carCount.MembershipFunctions.AddTrapezoid("High", 20, 30, 30, 20);

        queueLength = new LinguisticVariable("QueueLength");
        nullQueue = queueLength.MembershipFunctions.AddTrapezoid("Null", 0, 0, 0, 0);
        smallQueue = queueLength.MembershipFunctions.AddTrapezoid("Low", 0, 0, 10, 20);
        mediumQueue = queueLength.MembershipFunctions.AddTrapezoid("Medium", 10, 15, 20, 20);
        bigQueue = queueLength.MembershipFunctions.AddTrapezoid("High", 20, 30, 30, 20);

        greenLightDurationVar = new LinguisticVariable("GreenLightDuration");
        nullGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Null", 0, 0, 0, 0);
        shortGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Short", 5, 10, 15, 20);
        mediumGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Medium", 10, 15, 20, 20);
        longGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Long", 15, 20, 25, 20);


        rule0 = Rule.If(carCount.Is(nullCount).And(queueLength.Is(nullQueue))).Then(greenLightDurationVar.Is(nullGreen));
        rule1 = Rule.If(carCount.Is(lowCount).And(queueLength.Is(smallQueue))).Then(greenLightDurationVar.Is(shortGreen));
        rule2 = Rule.If(carCount.Is(lowCount).And(queueLength.Is(mediumQueue))).Then(greenLightDurationVar.Is(shortGreen));
        rule3 = Rule.If(carCount.Is(lowCount).And(queueLength.Is(bigQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule4 = Rule.If(carCount.Is(mediumCount).And(queueLength.Is(smallQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule5 = Rule.If(carCount.Is(mediumCount).And(queueLength.Is(mediumQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule6 = Rule.If(carCount.Is(mediumCount).And(queueLength.Is(bigQueue))).Then(greenLightDurationVar.Is(longGreen));
        rule7 = Rule.If(carCount.Is(HighCount).And(queueLength.Is(smallQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule8 = Rule.If(carCount.Is(HighCount).And(queueLength.Is(mediumCount))).Then(greenLightDurationVar.Is(longGreen));
        rule9 = Rule.If(carCount.Is(HighCount).And(queueLength.Is(bigQueue))).Then(greenLightDurationVar.Is(longGreen));

        listToChangeColors.Add(Phase1);
        listToChangeColors.Add(Phase2);
        listToChangeColors.Add(Phase3);
        listToChangeColors.Add(Phase4);
        listToChangeColors.Add(Phase5);
        foreach (var listForPhase in listToChangeColors)
        {
            foreach (var lineInPhase in listForPhase)
            {
                CarCountOnInlet.Add(lineInPhase, 0);
                CarQueueOnInlet.Add(lineInPhase, 0);
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
            Dictionary<LineLightManager, float> updatedCarQueue = new Dictionary<LineLightManager, float>();
            // tymczasowy s�ownik by nie edytowa� przetwarzanego s�ownika

            foreach (var kvp in CarCountOnInlet) //przejdz sobie po ilosci aut
            {
                LineLightManager LineController = kvp.Key; //lineManager
                int countOfVehiclesOnLine = LineController.countOfVehicles; //tymczasowa zmienna, kt�ra wyra�a ilo�� aut na danym pasie
                float queueOfVehiclesOnLine = LineController.queueLength;
                updatedCarCount[LineController] = countOfVehiclesOnLine; //wartosc dla inta w slownika sterownik�w jest zapisana do tymczasowego s�ownika
                updatedCarQueue[LineController] = queueOfVehiclesOnLine;
            }

            foreach (var kvp in updatedCarCount) // Przejd� po tymczasowym s�owniku
            {
                LineLightManager LineController = kvp.Key; //lineManager
                int countOfVehiclesOnLine = LineController.countOfVehicles; //tymczasowa zmienna, kt�ra wyra�a ilo�� aut na danym pasie
                float countOfQueueOnLine = LineController.queueLength; //tymczasowa zmienna, kt�ra wyra�a ilo�� aut na danym pasie
                CarCountOnInlet[LineController] = countOfVehiclesOnLine; // Przenie� dane z tymczasowego s�ownika do g��wnego
                CarQueueOnInlet[LineController] = countOfQueueOnLine; // Przenie� dane z tymczasowego s�ownika do g��wnego
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
        float carQueueOnActivePhase = 0;
        foreach (var line in listOfPhase)
        {
            if (CarCountOnInlet.ContainsKey(line) && CarQueueOnInlet.ContainsKey(line))
            {
                carCountOnActivePhase += CarCountOnInlet[line];
                carQueueOnActivePhase += CarQueueOnInlet[line];
            }
            if (carCountOnActivePhase > 0 && carQueueOnActivePhase >= 0)
            {
                // Uruchomienie silnika rozmytego i obliczenie warto�ci wyj�ciowej
                var fuzzyResult = fuzzyEngine.Defuzzify(new { carCount = carCountOnActivePhase, queueLength = carQueueOnActivePhase });

                // Ustawienie wyniku jako czas trwania zielonego �wiat�a
                greenLightDuration = (float)fuzzyResult;
            }

        }
       
    }
}
