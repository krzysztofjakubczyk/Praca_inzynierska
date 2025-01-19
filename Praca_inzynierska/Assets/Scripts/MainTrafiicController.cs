using FLS;
using FLS.MembershipFunctions;
using FLS.Rules;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private double greenLightDuration;  // Duration of the green light
    [SerializeField] private float yellowLightDuration; // Duration of the yellow light
    [SerializeField] private int countOfCars; // Indeks aktywnego kontrolera

    private float timeBeforeGreenLights = 5f;
    public bool wantWarning;

    IFuzzyEngine fuzzyEngine;
    LinguisticVariable carCount, queueLength, greenLightDurationVar;
    IMembershipFunction nullCount, lowCount, mediumCount, highCount, nullQueue, smallQueue,
    mediumQueue, bigQueue, nullGreen, shortGreen, mediumGreen, longGreen;
    FuzzyRule rule0, rule1, rule2, rule3, rule4, rule5, rule6, rule7, rule8, rule9;
    private void Start()
    {
        fuzzyEngine = new FuzzyEngineFactory().Default();


        // Dodanie i dostosowanie funkcji przynale¿noœci
        // Dodanie i dostosowanie funkcji przynale¿noœci
        carCount = new LinguisticVariable("CarCount");
        nullCount = carCount.MembershipFunctions.AddTrapezoid("Null", 0.0, 0.0, 2.0, 5.0);
        lowCount = carCount.MembershipFunctions.AddTrapezoid("Low", 3.0, 5.0, 10.0, 15.0);
        mediumCount = carCount.MembershipFunctions.AddTrapezoid("Medium", 10.0, 15.0, 20.0, 25.0);
        highCount = carCount.MembershipFunctions.AddTrapezoid("High", 20.0, 25.0, 30.0, 35.0);

        queueLength = new LinguisticVariable("QueueLength");
        nullQueue = queueLength.MembershipFunctions.AddTrapezoid("Null", 0.0, 0.0, 4.5, 9.0);
        smallQueue = queueLength.MembershipFunctions.AddTrapezoid("Low", 4.5, 9.0, 13.5, 18.0);
        mediumQueue = queueLength.MembershipFunctions.AddTrapezoid("Medium", 13.5, 18.0, 22.5, 27.0);
        bigQueue = queueLength.MembershipFunctions.AddTrapezoid("High", 22.5, 27.0, 31.5, 36.0);

        greenLightDurationVar = new LinguisticVariable("GreenLightDuration");
        nullGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Null", 0.0, 0.0, 5.0, 10.0);
        shortGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Short", 5.0, 10.0, 15.0, 20.0);
        mediumGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Medium", 15.0, 20.0, 25.0, 30.0);
        longGreen = greenLightDurationVar.MembershipFunctions.AddTrapezoid("Long", 25.0, 30.0, 35.0, 40.0);



        rule0 = Rule.If(carCount.Is(nullCount).And(queueLength.Is(nullQueue))).Then(greenLightDurationVar.Is(nullGreen));
        rule1 = Rule.If(carCount.Is(lowCount).And(queueLength.Is(smallQueue))).Then(greenLightDurationVar.Is(shortGreen));
        rule2 = Rule.If(carCount.Is(lowCount).And(queueLength.Is(mediumQueue))).Then(greenLightDurationVar.Is(shortGreen));
        rule3 = Rule.If(carCount.Is(lowCount).And(queueLength.Is(bigQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule4 = Rule.If(carCount.Is(mediumCount).And(queueLength.Is(smallQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule5 = Rule.If(carCount.Is(mediumCount).And(queueLength.Is(mediumQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule6 = Rule.If(carCount.Is(mediumCount).And(queueLength.Is(bigQueue))).Then(greenLightDurationVar.Is(longGreen));
        rule7 = Rule.If(carCount.Is(highCount).And(queueLength.Is(smallQueue))).Then(greenLightDurationVar.Is(mediumGreen));
        rule8 = Rule.If(carCount.Is(highCount).And(queueLength.Is(mediumCount))).Then(greenLightDurationVar.Is(longGreen));
        rule9 = Rule.If(carCount.Is(highCount).And(queueLength.Is(bigQueue))).Then(greenLightDurationVar.Is(longGreen));

        fuzzyEngine.Rules.Add(rule1);
        fuzzyEngine.Rules.Add(rule2);
        fuzzyEngine.Rules.Add(rule3);
        fuzzyEngine.Rules.Add(rule4);
        fuzzyEngine.Rules.Add(rule5);
        fuzzyEngine.Rules.Add(rule6);
        fuzzyEngine.Rules.Add(rule7);
        fuzzyEngine.Rules.Add(rule8);
        fuzzyEngine.Rules.Add(rule9);

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

    // Korutyna do zbierania liczby samochodów na wlocie
    private IEnumerator GetVehicleCountOnEntrance()
    {
        while (true)
        {
            Dictionary<LineLightManager, int> updatedCarCount = new Dictionary<LineLightManager, int>();
            Dictionary<LineLightManager, float> updatedCarQueue = new Dictionary<LineLightManager, float>();
            // tymczasowy s³ownik by nie edytowaæ przetwarzanego s³ownika

            foreach (var kvp in CarCountOnInlet) //przejdz sobie po ilosci aut
            {
                LineLightManager LineController = kvp.Key; //lineManager
                int countOfVehiclesOnLine = LineController.countOfVehicles; //tymczasowa zmienna, która wyra¿a iloœæ aut na danym pasie
                float queueOfVehiclesOnLine = LineController.queueLength;
                updatedCarCount[LineController] = countOfVehiclesOnLine; //wartosc dla inta w slownika sterowników jest zapisana do tymczasowego s³ownika
                updatedCarQueue[LineController] = queueOfVehiclesOnLine;
            }

            foreach (var kvp in updatedCarCount) // PrzejdŸ po tymczasowym s³owniku
            {
                LineLightManager LineController = kvp.Key; //lineManager
                int countOfVehiclesOnLine = LineController.countOfVehicles; //tymczasowa zmienna, która wyra¿a iloœæ aut na danym pasie
                float countOfQueueOnLine = LineController.queueLength; //tymczasowa zmienna, która wyra¿a iloœæ aut na danym pasie
                CarCountOnInlet[LineController] = countOfVehiclesOnLine; // Przenieœ dane z tymczasowego s³ownika do g³ównego
                CarQueueOnInlet[LineController] = countOfQueueOnLine; // Przenieœ dane z tymczasowego s³ownika do g³ównego
            }
            yield return new WaitForSeconds(5f);  // Odœwie¿enie liczby pojazdów co 5 sekund
        }
    }

    // Korutyna do ustawiania cykli œwiate³ w kontrolerach lokalnych
    private IEnumerator CycleTrafficLights()
    {
        while (true)
        {
            // PrzejdŸ przez wszystkie fazy
            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                // Ustaw aktywn¹ fazê
                manageTimeOfColors(listToChangeColors[phaseIndex]);
                // Ustaw œwiat³a zielone tylko dla bie¿¹cej fazy
                foreach (var lineInPhase in listToChangeColors[phaseIndex])
                {
                    lineInPhase.ChangeColor(TrafficLightColor.green);
                }

                // Ustaw œwiat³a czerwone dla pozosta³ych faz
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

                // Odczekaj czas zielonego œwiat³a
                yield return new WaitForSeconds((float)greenLightDuration);

                // Zmieñ œwiat³a bie¿¹cej fazy na ¿ó³te
                foreach (var lineInPhase in listToChangeColors[phaseIndex])
                {
                    lineInPhase.ChangeColor(TrafficLightColor.yellow);
                }

                // Odczekaj czas ¿ó³tego œwiat³a
                yield return new WaitForSeconds(yellowLightDuration);

                // Ustaw czerwone œwiat³a na koñcu dla bie¿¹cej fazy
                foreach (var lineInPhase in listToChangeColors[phaseIndex])
                {
                    lineInPhase.ChangeColor(TrafficLightColor.red);
                }
            }
        }
    }


    private void manageTimeOfColors(List<LineLightManager> listOfPhase)
    {
        double carCountOnActivePhase = 0;
        double carQueueOnActivePhase = 0;
        foreach (var line in listOfPhase)
        {
            if (CarCountOnInlet.ContainsKey(line) && CarQueueOnInlet.ContainsKey(line))
            {
                carCountOnActivePhase += CarCountOnInlet[line];
                carQueueOnActivePhase += CarQueueOnInlet[line];
            }
            nullCount.Fuzzify(carCountOnActivePhase);
            nullQueue.Fuzzify(carQueueOnActivePhase);
            lowCount.Fuzzify(carCountOnActivePhase);
            smallQueue.Fuzzify(carQueueOnActivePhase);
            mediumCount.Fuzzify(carCountOnActivePhase);
            mediumQueue.Fuzzify(carQueueOnActivePhase);
            highCount.Fuzzify(carCountOnActivePhase);
            bigQueue.Fuzzify(carQueueOnActivePhase);

            print("CAR QUEUE : " + carQueueOnActivePhase + "CAR COUNT: " + carCountOnActivePhase);

            // Uruchomienie silnika rozmytego i obliczenie wartoœci wyjœciowej
            var fuzzyResult = fuzzyEngine.Defuzzify(new { carCount = carCountOnActivePhase, queueLength = carQueueOnActivePhase });
            print("CAHNGED NA: " + fuzzyResult.ToString());
            // Ustawienie wyniku jako czas trwania zielonego œwiat³a
            greenLightDuration = fuzzyResult;


        }

    }
}