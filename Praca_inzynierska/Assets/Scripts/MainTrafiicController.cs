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
    [SerializeField] private double greenLightDuration;
    [SerializeField] private float yellowLightDuration;

    [Header("Count of vehicles to spawn in cycle")]
    [SerializeField] private List<int> timeForFirstPhase = new List<int>();
    [SerializeField] private List<int> timeForSecondPhase = new List<int>();
    [SerializeField] private List<int> timeForThirdPhase = new List<int>();
    [SerializeField] private List<int> timeForFourthPhase = new List<int>();
    [SerializeField] private List<int> timeForFivethPhase = new List<int>();

    private float timeBeforeGreenLights = 10f;

    private FuzzyLogicHandler fuzzyLogicHandler;

    private void Start()
    {
        InitializeFuzzyLogic();
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
            Dictionary<LineLightManager, int> updatedCarCount = new Dictionary<LineLightManager, int>(CarCountOnInlet);
            Dictionary<LineLightManager, float> updatedCarQueue = new Dictionary<LineLightManager, float>(CarQueueOnInlet);

            foreach (var kvp in CarCountOnInlet)
            {
                var lineManager = kvp.Key;
                updatedCarCount[lineManager] = lineManager.countOfVehicles;
                updatedCarQueue[lineManager] = lineManager.queueLength;
            }

            foreach (var kvp in updatedCarCount)
            {
                CarCountOnInlet[kvp.Key] = kvp.Value;
                CarQueueOnInlet[kvp.Key] = updatedCarQueue[kvp.Key];
            }

            yield return new WaitForSeconds(5f);
        }
    }

    private IEnumerator CycleTrafficLights()
    {
        while (true)
        {
            // Iteracja przez fazy
            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                var phase = listToChangeColors[phaseIndex]; // Lista pasów w danej fazie
                List<int> timesForPhase = GetPhaseTimes(phaseIndex); // Pobranie czasów dla fazy

                if (phase.Count != timesForPhase.Count)
                {
                    Debug.LogError($"Niezgodnoœæ liczby pasów i czasów w fazie {phaseIndex + 1}: Pasy={phase.Count}, Czasy={timesForPhase.Count}");
                    yield break; // Zatrzymanie, jeœli dane s¹ niezgodne
                }

                List<Coroutine> activeCoroutines = new List<Coroutine>();

                // Rozpoczêcie zielonych œwiate³ jednoczeœnie dla wszystkich pasów w fazie
                for (int i = 0; i < phase.Count; i++)
                {
                    var line = phase[i];
                    int greenTime = timesForPhase[i];

                    // Rozpocznij korutynê dla ka¿dego pasa
                    Coroutine coroutine = StartCoroutine(HandleLaneCycle(line, greenTime));
                    activeCoroutines.Add(coroutine);
                }

                // Poczekaj, a¿ wszystkie pasy w tej fazie zakoñcz¹ swój cykl
                foreach (var coroutine in activeCoroutines)
                {
                    yield return coroutine;
                }

                // Przerwa miêdzy fazami
                yield return new WaitForSeconds(timeBeforeGreenLights);
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
            case 3:
                return timeForFourthPhase;
            case 4:
                return timeForFivethPhase;
            default:
                Debug.LogError($"Niepoprawny indeks fazy: {phaseIndex}");
                return new List<int>(); // Zwraca pust¹ listê, aby unikn¹æ b³êdów
        }
    }
    private IEnumerator HandleLaneCycle(LineLightManager line, int greenTime)
    {
        // Zielone œwiat³o
        line.ChangeColor(TrafficLightColor.green);
        yield return new WaitForSeconds(greenTime);

        // ¯ó³te œwiat³o
        line.ChangeColor(TrafficLightColor.yellow);
        yield return new WaitForSeconds(yellowLightDuration);

        // Czerwone œwiat³o
        line.ChangeColor(TrafficLightColor.red);
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
