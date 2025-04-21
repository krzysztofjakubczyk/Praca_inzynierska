using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainTrafficControllerForSczanieckiej : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<LineLightManager> Phase1 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase2 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase3 = new List<LineLightManager>();
    private List<List<LineLightManager>> listToChangeColors;
    private Dictionary<LineLightManager, int> CarCountOnInlet = new Dictionary<LineLightManager, int>();
    private Dictionary<LineLightManager, float> CarQueueOnInlet = new Dictionary<LineLightManager, float>();


    [Header("Time Management")]
    public int choosedHour;
    public float intervalForCheckingSensorsData;
    public float currentCycleTime = 0f;
    private const int fullCycleTime = 120;
    [SerializeField] private float yellowLightDuration = 3f;
    [SerializeField] public bool useFuzzyLogic;
    private float initialFillingTime = 20f; // Czas wypełniania skrzyżowania
    private int cycleCounter = 0; // Licznik pełnych cykli
    private int cyclesUntilUpdate = 2; // Początkowo zmieniamy co 2 cykle
    private FuzzyLogicHandler fuzzyLogicHandler;


    private Dictionary<int, List<int>> phaseStartTimesByHour = new Dictionary<int, List<int>>
    {
        { 7, new List<int> { 1, 36, 87 } },
        { 12, new List<int> { 1, 36, 87 } },
        { 15, new List<int> { 1, 36, 77 } },
        { 21, new List<int> { 1, 36, 77 } }
    };

    private Dictionary<int, List<int>> phaseDurationsByHour = new Dictionary<int, List<int>>
    {
        { 7, new List<int> { 30, 46, 27 } },
        { 12, new List<int> { 30, 46, 27 } },
        { 15, new List<int> { 30, 36, 37 } },
        { 21, new List<int> { 30, 36, 37 } }
    };
    private Dictionary<int, (double totalCarCount, double totalQueueLength, int samples)> trafficData = new Dictionary<int, (double, double, int)>
    {
        { 0, (0, 0, 0) }, // Faza 1
        { 1, (0, 0, 0) }, // Faza 2
        { 2, (0, 0, 0) }  // Faza 3
    };
    private void Start()
    {
        listToChangeColors = new List<List<LineLightManager>> { Phase1, Phase2, Phase3 };
        StartCoroutine(StartTrafficCycleWithDelay());
        fuzzyLogicHandler = new FuzzyLogicHandler();

        var carCountMemberships = new Dictionary<string, (double a, double b, double c, double d)>
{
    { "Low", (0 ,3 , 6, 9) },
    { "Medium", (6 ,9 , 12, 15) },
    { "High", (12,15,18,21 )} // Większy zakres, aby Faza 1 była traktowana jako High
};

        var queueLengthMemberships = new Dictionary<string, (double a, double b, double c, double d)>
{
    { "Small", (0,12,24,36) },
    { "Medium", (24,36,48,60) },
    { "Big", (48,60,72,84) } // Dostosowujemy do rzeczywistej długości kolejek
};

        var greenLightDurationMemberships = new Dictionary<string, (double a, double b, double c, double d)>
    {
        { "Short", (0, 5, 10, 15) },
        { "Medium", (15, 20, 25, 30) },
        { "Long", (25, 30, 35, 40) }
    };
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Small", Output = "Short" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Medium", Output = "Short" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Big", Output = "Medium" });

        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Small", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Medium", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Big", Output = "Long" });

        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Small", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Medium", Output = "Long" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Big", Output = "Long" });

        fuzzyLogicHandler.InitializeTrapezoidalMembershipFunctions(carCountMemberships, queueLengthMemberships, greenLightDurationMemberships);
    }



    private void AdjustGreenLightDurations()
    {
        cycleCounter++;
        if (cycleCounter < cyclesUntilUpdate) return; // Sprawdzamy, czy czas na aktualizację
        cycleCounter = 0;
        cyclesUntilUpdate = Random.Range(2, 4); // Losujemy, co ile cykli aktualizować

        double totalGreenTime = 0;
        List<double> calculatedTimes = new List<double>();

        for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
        {
            var (totalCarCount, totalQueueLength, samples) = trafficData[phaseIndex];

            if (samples == 0) continue;

            double avgCarCount = totalCarCount / samples;
            double avgQueueLength = totalQueueLength / samples;

            var fuzzifiedInputs = fuzzyLogicHandler.Fuzzify(avgCarCount, avgQueueLength);
            var aggregatedOutputs = fuzzyLogicHandler.ApplyRules(fuzzifiedInputs);
            Debug.Log("📜 Zagregowane wartości po zastosowaniu reguł:");
            foreach (var output in aggregatedOutputs)
            {
                Debug.Log($"   - {output.Key}: {output.Value:F3}");
            }

            Debug.Log("📘 Aktywowane reguły logiki rozmytej:");
            foreach (var rule in fuzzyLogicHandler.Rules)
            {
                Debug.Log($"   - Reguła: {rule.Condition} -> {rule.Output}");
            }
           

            double greenTime = fuzzyLogicHandler.Defuzzify(aggregatedOutputs, "centroid");

            calculatedTimes.Add(greenTime);
            totalGreenTime += greenTime;
            print("total green time:" + totalGreenTime);
        }

        if (totalGreenTime < 103)
        {
            double missingTime = 103 - totalGreenTime;
            double scaleFactor = 1 + (missingTime / totalGreenTime);
            for (int i = 0; i < calculatedTimes.Count; i++)
            {
                calculatedTimes[i] *= scaleFactor;
            }
        }

        phaseDurationsByHour[choosedHour] = new List<int>
        {
            (int)calculatedTimes[0],
            (int)calculatedTimes[1],
            (int)calculatedTimes[2]
        };


        Debug.Log($"✅ Nowe czasy faz: {calculatedTimes[0]:F2}s, {calculatedTimes[1]:F2}s, {calculatedTimes[2]:F2}s");

        for (int i = 0; i < trafficData.Count; i++)
        {
            trafficData[i] = (0, 0, 0);
        }
    }

    private IEnumerator updateDataInTime()
    {
        while (true)
        {
            UpdateTrafficData();
            yield return new WaitForSeconds(intervalForCheckingSensorsData);
        }
    }


    private void UpdateTrafficData()
    {
        for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
        {
            double totalCarCount = trafficData[phaseIndex].totalCarCount;
            double totalQueueLength = trafficData[phaseIndex].totalQueueLength;
            int sampleCount = trafficData[phaseIndex].samples; // Pobieramy liczbę próbek

            foreach (var line in listToChangeColors[phaseIndex])
            {
                int vehicleCount = 0;
                float queueLength = 0f;

                foreach (var sensor in line.sensors) // Pobieramy dane z sensorów
                {
                    vehicleCount += sensor.VehicleCount;
                    queueLength += sensor.QueueLength;
                }

                // Debugging, żeby zobaczyć realne wartości
                CarCountOnInlet[line] = vehicleCount;
                CarQueueOnInlet[line] = queueLength;

                totalCarCount += vehicleCount;
                totalQueueLength += queueLength;
            }

            // 🔹 Dopiero teraz aktualizujemy całą fazę (po zebraniu wszystkich wartości)
            sampleCount += 1; // Teraz inkrementujemy poprawnie!
            trafficData[phaseIndex] = (totalCarCount, totalQueueLength, sampleCount);

            // Debugging, żeby zobaczyć poprawne sumy
        }
    }



    private IEnumerator StartTrafficCycleWithDelay()
    {
        yield return new WaitForSeconds(2f); // Czekamy 2 sekundy na ustawienie godziny

        TimeManager timeManager = FindObjectOfType<TimeManager>();
        if (timeManager != null)
        {
            choosedHour = timeManager.GetChoosedHour();
        }

        StartCoroutine(FillIntersection()); // Rozpoczynamy fazę wypełniania!
        StartCoroutine(updateDataInTime());
    }

    // 🚦 FAZA WYPEŁNIANIA - przez pierwsze X sekund wszystko zielone
    private IEnumerator FillIntersection()
    {

        foreach (var phase in listToChangeColors)
        {
            foreach (var line in phase)
            {
                line.ChangeColor(TrafficLightColor.red); // Wszystko na zielone
            }
        }

        yield return new WaitForSeconds(initialFillingTime); // Czekamy np. 20 sekund


        StartCoroutine(TraficCycle()); // Start normalnego cyklu
    }

    private IEnumerator TraficCycle()
    {
        while (true)
        {
            if (useFuzzyLogic)
            {
                AdjustGreenLightDurations();
            }
            currentCycleTime = 0f;
            List<int> startTimes = phaseStartTimesByHour[choosedHour];
            List<int> durations = phaseDurationsByHour[choosedHour];

            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                if (phaseIndex >= startTimes.Count || phaseIndex >= durations.Count)
                {
                    Debug.LogError("❌ Błąd przypisania czasów faz.");
                    continue;
                }

                int startTime = startTimes[phaseIndex];
                int greenTime = durations[phaseIndex];


                yield return new WaitForSeconds(startTime - currentCycleTime);

                List<Coroutine> runningCoroutines = new List<Coroutine>();

                foreach (var line in listToChangeColors[phaseIndex])
                {
                    runningCoroutines.Add(StartCoroutine(HandleLaneCycle(line, greenTime)));
                }

                currentCycleTime = startTime;
                yield return new WaitForSeconds(greenTime);

                currentCycleTime += greenTime;
            }


            yield return new WaitForSeconds(fullCycleTime - currentCycleTime);
        }
    }


    private IEnumerator HandleLaneCycle(LineLightManager line, int greenTime)
    {
        line.ChangeColor(TrafficLightColor.green);
        yield return new WaitForSeconds(greenTime);
        line.ChangeColor(TrafficLightColor.red);
    }

    public void ResetTrafficCycle()
    {
        StopAllCoroutines(); // Zatrzymanie wszystkich korutyn

        foreach (var phase in listToChangeColors)
        {
            foreach (var line in phase)
            {
                line.ChangeColor(TrafficLightColor.red); // Wszystkie światła na czerwone
            }
        }

        currentCycleTime = 0f; // Resetowanie licznika cyklu
        StartCoroutine(StartTrafficCycleWithDelay()); // Ponowne uruchomienie cyklu świateł
    }
}
