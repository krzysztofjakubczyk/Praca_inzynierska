using System.Collections.Generic;
using UnityEngine;

public class FuzzyInterfaceImplementation : ITrafficTimingStrategy
{
    private readonly List<PhaseSO> phasesInCycle;
    private readonly Dictionary<PhaseSO, int> carCountOnInlet;
    private readonly Dictionary<PhaseSO, float> carQueueOnInlet;
    private readonly float greenBudgetSeconds;
    private readonly float lostTimePerPhaseSeconds;

    private int cycleCounter;
    private int cyclesUntilUpdate;

    /// <param name="greenBudget">Łączny czas zielonego w cyklu (cykl minus żółte i all-red).</param>
    /// <param name="lostTimePerPhase">Czas żółtych i all-red przypadający na jedną fazę.</param>
    public FuzzyInterfaceImplementation(
        List<PhaseSO> phases,
        Dictionary<PhaseSO, int> carCounts,
        Dictionary<PhaseSO, float> carQueues,
        float greenBudget,
        float lostTimePerPhase)
    {
        phasesInCycle = phases;
        carCountOnInlet = carCounts;
        carQueueOnInlet = carQueues;
        greenBudgetSeconds = Mathf.Max(1f, greenBudget);
        lostTimePerPhaseSeconds = Mathf.Max(0f, lostTimePerPhase);
        InitializeFuzzyLogic();
    }

    public override void AdjustGreenLightDurations()
    {
        cycleCounter++;
        if (cycleCounter < cyclesUntilUpdate) return; // Czy już czas na aktualizację?
        cycleCounter = 0;
        cyclesUntilUpdate = Random.Range(2, 4); // Losujemy, co ile cykli aktualizować

        double totalGreenTime = 0;
        List<double> calculatedTimes = new List<double>();
        for (int phaseIndex = 0; phaseIndex < phasesInCycle.Count; phaseIndex++)
        {
            PhaseSO phase = phasesInCycle[phaseIndex];
            double avgCarCountInPhase = carCountOnInlet.TryGetValue(phase, out int count) ? count : 0;
            double avgCarQueueInPhase = carQueueOnInlet.TryGetValue(phase, out float queue) ? queue : 0f;

            var fuzzifiedInputs = fuzzyLogicHandler.Fuzzify(avgCarCountInPhase, avgCarQueueInPhase);
            var aggregatedOutputs = fuzzyLogicHandler.ApplyRules(fuzzifiedInputs);
            double greenTime = fuzzyLogicHandler.Defuzzify(aggregatedOutputs, "centroid");

            calculatedTimes.Add(greenTime);
            totalGreenTime += greenTime;
        }

        if (totalGreenTime <= 0.01) return;

        // Skalujemy w obie strony, tak aby suma zielonych + żółte + all-red = długość cyklu.
        // (Wcześniej czasy były skalowane do 120 s samego zielonego, więc cykl trwał ~136 s,
        // a przy sumie > 120 s nie były skalowane wcale.)
        double scaleFactor = greenBudgetSeconds / totalGreenTime;
        double startTime = 0;
        for (int i = 0; i < calculatedTimes.Count; i++)
        {
            double green = calculatedTimes[i] * scaleFactor;
            PhaseSO phase = phasesInCycle[i];
            if (phase == null || phase.lanes == null) continue;

            foreach (var lane in phase.lanes)
            {
                if (lane == null) continue;
                lane.timeOfGreenLight = green;
                lane.timeToStartGreenLight = startTime;
            }

            startTime += green + lostTimePerPhaseSeconds;
        }
    }
}
