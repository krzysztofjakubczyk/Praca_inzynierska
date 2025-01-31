using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTrafficControllerForSczanieckiej : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<LineLightManager> Phase1 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase2 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase3 = new List<LineLightManager>();
    [SerializeField] private List<List<LineLightManager>> listToChangeColors = new List<List<LineLightManager>>();

    [SerializeField] private Dictionary<LineLightManager, int> CarCountOnInlet = new Dictionary<LineLightManager, int>();
    [SerializeField] private Dictionary<LineLightManager, float> CarQueueOnInlet = new Dictionary<LineLightManager, float>();

    [Header("Floats and ints")]
    [SerializeField] private double greenLightDuration;
    [SerializeField] private float yellowLightDuration;
    [SerializeField] private float currentCycleTime = 0f; // Aktualny czas w cyklu

    [Header("Green Light Start Times (in seconds) for each lane")]
    [SerializeField] private List<int> startTimesForFirstPhase = new List<int>();
    [SerializeField] private List<int> startTimesForSecondPhase = new List<int>();
    [SerializeField] private List<int> startTimesForThirdPhase = new List<int>();

    [Header("Green Light Durations (in seconds) for each lane")]
    [SerializeField] private List<int> timeForFirstPhase = new List<int>();
    [SerializeField] private List<int> timeForSecondPhase = new List<int>();
    [SerializeField] private List<int> timeForThirdPhase = new List<int>();

    private float timeBeforeGreenLights = 10f;
    private int fullCycleTime = 110;
    [SerializeField] private float timeBeforeNextPhase = 2f;

    private FuzzyLogicHandler fuzzyLogicHandler;
    public int activePhase;

    private void Start()
    {
        InitializeFuzzyLogic();
        listToChangeColors.Add(Phase1);
        listToChangeColors.Add(Phase2);
        listToChangeColors.Add(Phase3);

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
        StartCoroutine(GlobalTrafficCycle());
    }

    private void InitializeFuzzyLogic()
    {
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
    { "Short", (0, 5, 10, 15) },  // Dla ma³ych kolejek
    { "Medium", (15, 20, 25, 30) }, // Dla œrednich czasów
    { "Long", (25, 30, 35, 40) }    // Dla d³ugich czasów
};

        fuzzyLogicHandler.InitializeTrapezoidalMembershipFunctions(carCountMemberships, queueLengthMemberships, greenLightDurationMemberships);

        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Small", Output = "Short" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Medium", Output = "Short" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Big", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Small", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Medium", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Big", Output = "Long" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Small", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Medium", Output = "Long" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Big", Output = "Long" });
    }

    private IEnumerator GetVehicleCountOnEntrance()
    {
        while (true)
        {
            float elapsedTime = 0f;

            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                var phase = listToChangeColors[phaseIndex];
                List<int> laneTimes = GetPhaseTimes(phaseIndex);
                List<int> startTimes = GetPhaseStartTimes(phaseIndex);

                if (phase.Count != laneTimes.Count || phase.Count != startTimes.Count)
                {
                    Debug.LogError($"Mismatch in phase {phaseIndex + 1}: Lanes={phase.Count}, Times={laneTimes.Count}, StartTimes={startTimes.Count}");
                    yield break;
                }

                for (int i = 0; i < phase.Count; i++)
                {
                    var line = phase[i];
                    int startTime = startTimes[i];  // Kiedy dany pas ma siê w³¹czyæ w globalnym cyklu
                    int greenTime = laneTimes[i];  // Jak d³ugo ma trwaæ zielone œwiat³o

                    StartCoroutine(HandleLaneCycle(line, startTime, greenTime));
                }
            }

            yield return new WaitForSeconds(fullCycleTime);
        }
    }
    private IEnumerator GlobalTrafficCycle()
    {
        while (true)
        {
            currentCycleTime = 0f; // Resetujemy czas na pocz¹tku nowego cyklu

            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                var phase = listToChangeColors[phaseIndex];
                activePhase = phaseIndex;
                List<int> laneTimes = GetPhaseTimes(phaseIndex);
                List<int> startTimes = GetPhaseStartTimes(phaseIndex);

                if (phase.Count != laneTimes.Count || phase.Count != startTimes.Count)
                {
                    Debug.LogError($"Mismatch in phase {phaseIndex + 1}: Lanes={phase.Count}, Times={laneTimes.Count}, StartTimes={startTimes.Count}");
                    yield break;
                }

                for (int i = 0; i < phase.Count; i++)
                {
                    var line = phase[i];
                    int startTime = startTimes[i];
                    int greenTime = laneTimes[i];

                    StartCoroutine(HandleLaneCycle(line, startTime, greenTime));
                }
            }

            while (currentCycleTime < fullCycleTime) // Czekamy, a¿ cykl siê zakoñczy
            {
                yield return null; // Kontynuujemy pêtlê, aby aktualizowaæ currentCycleTime
            }
        }
    }

    private List<int> GetPhaseTimes(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0:
                return timeForFirstPhase;
            case 1:
                return timeForSecondPhase;
            case 2:
                return timeForThirdPhase;
            default:
                Debug.LogError($"Niepoprawny indeks fazy: {phaseIndex}");
                return new List<int>(); // Zwraca pust¹ listê, aby unikn¹æ b³êdów
        }
    }
    private List<int> GetPhaseStartTimes(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0:
                return startTimesForFirstPhase;
            case 1:
                return startTimesForSecondPhase;
            case 2:
                return startTimesForThirdPhase;
            default:
                Debug.LogError($"Invalid phase index: {phaseIndex}");
                return new List<int>();
        }
    }
    private IEnumerator HandleLaneCycle(LineLightManager line, int startTime, int greenTime)
    {
        while (currentCycleTime < startTime) // Czekamy na moment startu
        {
            yield return null;
        }

        int timeLeftInCycle = (int)(fullCycleTime - currentCycleTime);
        int timeInNextCycle = greenTime - timeLeftInCycle;

        //Debug.Log($"PAS {line.name} W£¥CZA ZIELONE: Start = {startTime}s, GreenTime = {greenTime}s, LeftInCycle = {timeLeftInCycle}s, NextCycle = {timeInNextCycle}s");

        line.ChangeColor(TrafficLightColor.green);

        if (timeInNextCycle > 0)
        {
            yield return new WaitForSeconds(timeLeftInCycle);

            //Debug.Log($"PAS {line.name} KOÑCZY BIE¯¥CY CYKL, CZEKA NA NOWY");

            // *** Zamiast `WaitUntil`, czekamy rêcznie ***
            float waitTime = 0f;
            while (currentCycleTime >= fullCycleTime)
            {
                waitTime += Time.deltaTime;
                yield return null;
                if (waitTime > 2f) // Jeœli po 2 sekundach nadal nic siê nie zmienia, wymuszamy reset
                {
                    //Debug.LogWarning($"PAS {line.name} UTKN¥£ NA ZIELONYM! RESETUJEMY ŒWIAT£O.");
                    break;
                }
            }

            //Debug.Log($"PAS {line.name} KONTYNUUJE W NOWYM CYKLU: {timeInNextCycle}s");

            line.ChangeColor(TrafficLightColor.green);
            yield return new WaitForSeconds(timeInNextCycle);
        }
        else
        {
            yield return new WaitForSeconds(greenTime);
        }

        // **Wymuszone ¿ó³te œwiat³o przed czerwonym**
        // Debug.Log($"PAS {line.name} PRZECHODZI NA ¯Ó£TE!");
        line.ChangeColor(TrafficLightColor.yellow);
        yield return new WaitForSeconds(yellowLightDuration);

        // // **Wymuszone czerwone œwiat³o**
        // Debug.Log($"PAS {line.name} PRZECHODZI NA CZERWONE!");
        line.ChangeColor(TrafficLightColor.red);
    }


    private void Update()
    {
        currentCycleTime += Time.deltaTime;
        if (currentCycleTime >= fullCycleTime)
        {
            currentCycleTime = 0f; // Reset cyklu
        }
    }

    private void ManageGreenLightDuration(List<LineLightManager> phase)
    {
        double carCount = 0;
        double queueLength = 0;

        foreach (var line in phase)
        {
            if (CarCountOnInlet.ContainsKey(line))
            {
                carCount += CarCountOnInlet[line];
                queueLength += CarQueueOnInlet[line];
            }
        }
        print("car count: " + carCount + " queue length: " + queueLength);
        var fuzzifiedInputs = fuzzyLogicHandler.Fuzzify(carCount, queueLength);
        var aggregatedOutputs = fuzzyLogicHandler.ApplyRules(fuzzifiedInputs);

        greenLightDuration = fuzzyLogicHandler.Defuzzify(aggregatedOutputs, "centroid");
        Debug.Log($"Calculated Green Light Duration: {greenLightDuration}");
    }
}
