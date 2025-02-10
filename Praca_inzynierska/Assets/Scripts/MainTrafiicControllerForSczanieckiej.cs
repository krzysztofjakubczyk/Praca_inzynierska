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
        { "Low", (0,5,9,13) },
        { "Medium", (9,13,17,21) },
        { "High", (17,21,25,29) }
    };

        var queueLengthMemberships = new Dictionary<string, (double a, double b, double c, double d)>
    {
        { "Small", (0,20,36,52) },
        { "Medium", ( 36, 52, 68, 84) },
        { "Big", (68, 84, 100, 116) }
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

            AdjustGreenLightDurations(); // 🔄 Aktualizacja czasów co 2-3 cykle

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
        Debug.Log("🔄 Cykl świateł został zresetowany.");

        StartCoroutine(StartTrafficCycleWithDelay()); // Ponowne uruchomienie cyklu świateł
    }
}
