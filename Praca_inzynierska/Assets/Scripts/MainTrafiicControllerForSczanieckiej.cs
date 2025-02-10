using System.Collections;
using System.Collections.Generic;
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
    public float currentCycleTime = 0f;
    private const int fullCycleTime = 120;
    [SerializeField] private float yellowLightDuration = 3f;
    [SerializeField] private bool useFuzzyLogic ;
    private float initialFillingTime = 20f; // Czas wypełniania skrzyżowania
    private bool isFillingPhase = true; // Czy trwa faza wypełniania?
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

    private void Start()
    {
        listToChangeColors = new List<List<LineLightManager>> { Phase1, Phase2, Phase3 };
        StartCoroutine(StartTrafficCycleWithDelay());
        fuzzyLogicHandler = new FuzzyLogicHandler();

        var carCountMemberships = new Dictionary<string, (double a, double b, double c, double d)>
{
    { "Low", (0,10,15,20) },
    { "Medium", (15,20,30,35) },
    { "High", (30,35,45,50) } // Większy zakres, aby Faza 1 była traktowana jako High
};

        var queueLengthMemberships = new Dictionary<string, (double a, double b, double c, double d)>
{
    { "Small", (0,30,50,70) },
    { "Medium", (50,70,90,110) },
    { "Big", (90,110,150,180) } // Dostosowujemy do rzeczywistej długości kolejek
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

        Debug.Log($"🔄 Aktualizacja czasów faz (co {cyclesUntilUpdate} cykle)");

        double totalGreenTime = 0;
        List<double> calculatedTimes = new List<double>();

        foreach (var phase in listToChangeColors)
        {
            double carCount = 0;
            double queueLength = 0;

            // Pobieramy rzeczywiste wartości z sensorów
            foreach (var line in phase)
            {
                carCount += CarCountOnInlet.ContainsKey(line) ? CarCountOnInlet[line] : 0;
                queueLength += CarQueueOnInlet.ContainsKey(line) ? CarQueueOnInlet[line] : 0;
            }

            Debug.Log($"📊 Faza {listToChangeColors.IndexOf(phase) + 1}: Auta: {carCount}, Kolejka: {queueLength}");

            // Fuzzification - przetwarzamy dane wejściowe
            var fuzzifiedInputs = fuzzyLogicHandler.Fuzzify(carCount, queueLength);

            Debug.Log("🔎 Wyniki fuzzification:");
            foreach (var variable in fuzzifiedInputs)
            {
                foreach (var membership in variable.Value)
                {
                    Debug.Log($"   - {variable.Key}: {membership.Key} -> {membership.Value:F3}");
                }
            }

            // Zastosowanie reguł logiki rozmytej
            var aggregatedOutputs = fuzzyLogicHandler.ApplyRules(fuzzifiedInputs);

            Debug.Log("📜 Zagregowane wartości po zastosowaniu reguł:");
            foreach (var output in aggregatedOutputs)
            {
                Debug.Log($"   - {output.Key}: {output.Value:F3}");
            }

            // Defuzzification - obliczenie czasu zielonego światła
            double greenTime = fuzzyLogicHandler.Defuzzify(aggregatedOutputs, "centroid");
            Debug.Log($"🎯 Wynik defuzyfikacji dla fazy {listToChangeColors.IndexOf(phase) + 1}: {greenTime:F2}s");

            calculatedTimes.Add(greenTime);
            totalGreenTime += greenTime;
        }

        // 💡 Sprawdzenie, czy suma czasów jest wystarczająca (minimum 94.5s)
        if (totalGreenTime < 94.5)
        {
            double missingTime = 94.5 - totalGreenTime; // Ile sekund brakuje
            Debug.Log($"⚠️ Czas faz jest za krótki o {missingTime:F2}s, zwiększamy proporcjonalnie!");

            double scaleFactor = 1 + (missingTime / totalGreenTime); // Skala wydłużenia

            for (int i = 0; i < calculatedTimes.Count; i++)
            {
                calculatedTimes[i] *= scaleFactor; // Wydłużamy proporcjonalnie
            }
        }

        // Aktualizacja czasów faz w słowniku dla danej godziny
        phaseDurationsByHour[choosedHour] = new List<int>
    {
        (int)calculatedTimes[0],
        (int)calculatedTimes[1],
        (int)calculatedTimes[2]
    };

        Debug.Log($"✅ **Nowe czasy faz:** {calculatedTimes[0]:F2}s, {calculatedTimes[1]:F2}s, {calculatedTimes[2]:F2}s");
    }




    private void UpdateTrafficData()
    {
        foreach (var phase in listToChangeColors)
        {
            foreach (var line in phase)
            {
                int vehicleCount = 0;
                float queueLength = 0f;

                foreach (var sensor in line.sensors) // Pobieramy dane z sensorów
                {
                    vehicleCount += sensor.VehicleCount;
                    queueLength += sensor.QueueLength;
                }

                CarCountOnInlet[line] = vehicleCount;
                CarQueueOnInlet[line] = queueLength;
            }
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

        isFillingPhase = false; // Zakończono fazę wypełniania

        StartCoroutine(TraficCycle()); // Start normalnego cyklu
    }

    private IEnumerator TraficCycle()
    {
        while (true)
        {
            UpdateTrafficData(); // 🔄 Pobranie aktualnych danych o ruchu
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
                yield return new WaitForSeconds(greenTime + yellowLightDuration);

                currentCycleTime += greenTime;
            }


            yield return new WaitForSeconds(fullCycleTime - currentCycleTime);
        }
    }


    private IEnumerator HandleLaneCycle(LineLightManager line, int greenTime)
    {


        line.ChangeColor(TrafficLightColor.green);
        yield return new WaitForSeconds(greenTime);


        line.ChangeColor(TrafficLightColor.yellow);
        yield return new WaitForSeconds(yellowLightDuration);

 
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
        isFillingPhase = true; // Ponowne uruchomienie fazy wypełniania

        StartCoroutine(StartTrafficCycleWithDelay()); // Ponowne uruchomienie cyklu świateł
    }
}
