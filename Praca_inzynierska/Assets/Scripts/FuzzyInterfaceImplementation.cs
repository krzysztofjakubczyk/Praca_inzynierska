using System.Collections.Generic;
using UnityEngine;

public class FuzzyInterfaceImplementation : ITrafficTimingStrategy
{
    private List<PhaseSO> phasesInCycle = new List<PhaseSO>();

    private Dictionary<PhaseSO, int> carCountOnInlet = new Dictionary<PhaseSO, int>();
    private Dictionary<PhaseSO, float> carQueueOnInlet = new Dictionary<PhaseSO, float>();
    private int fullCycleTime = 120;

    public FuzzyInterfaceImplementation(
    List<PhaseSO> phases,
    Dictionary<PhaseSO, int> carCounts,
    Dictionary<PhaseSO, float> carQueues,
    int cycleTime)
    {
        this.phasesInCycle = phases;
        this.carCountOnInlet = carCounts;
        this.carQueueOnInlet = carQueues;
        this.fullCycleTime = cycleTime;
        InitializeFuzzyLogic();
    }
    private int cycleCounter;
    private int cyclesUntilUpdate;

    public override void AdjustGreenLightDurations()
    {
        cycleCounter++;
        if (cycleCounter < cyclesUntilUpdate) return; // Sprawdzamy, czy czas na aktualizacjê
        cycleCounter = 0;
        cyclesUntilUpdate = Random.Range(2, 4); // Losujemy, co ile cykli aktualizowaæ

        double totalGreenTime = 0;
        List<double> calculatedTimes = new List<double>();
        for (int phaseIndex = 0; phaseIndex < phasesInCycle.Count; phaseIndex++)
        {
            double avgCarCountInPhase = carCountOnInlet[phasesInCycle[phaseIndex]];
            double avgCarQueueInPhase = carQueueOnInlet[phasesInCycle[phaseIndex]];


            var fuzzifiedInputs = fuzzyLogicHandler.Fuzzify(avgCarCountInPhase, avgCarQueueInPhase);
            var aggregatedOutputs = fuzzyLogicHandler.ApplyRules(fuzzifiedInputs);

            double greenTime = fuzzyLogicHandler.Defuzzify(aggregatedOutputs, "centroid");

            calculatedTimes.Add(greenTime);
            totalGreenTime += greenTime;
        }

        if (totalGreenTime < fullCycleTime)
        {
            double missingTime = fullCycleTime - totalGreenTime;
            double scaleFactor = 1 + (missingTime / totalGreenTime);
            if (totalGreenTime <= 0.01) return;
            for (int i = 0; i < calculatedTimes.Count; i++)
            {
                foreach (var lane in phasesInCycle[i].lanes)
                {
                    lane.timeOfGreenLight = calculatedTimes[i] * scaleFactor;
                    if (i > 0)
                    {
                        lane.timeToStartGreenLight = phasesInCycle[i - 1].lanes[i - 1].timeToStartGreenLight +
                                                     phasesInCycle[i - 1].lanes[i - 1].timeOfGreenLight +
                                                     phasesInCycle[i - 1].lanes[i - 1].timeOfBeforeYellowLight +
                                                     phasesInCycle[i - 1].lanes[i - 1].timeOfAfterYellowLight;
                    }
                }
            }
        }

    }

}
