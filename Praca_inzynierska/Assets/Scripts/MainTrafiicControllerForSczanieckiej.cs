using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class MainTrafficControllerSczanieckiej : MonoBehaviour
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

    private float timeBeforeGreenLights = 5f;

    private FuzzyLogicHandler fuzzyLogicHandler;

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
        StartCoroutine(CycleTrafficLights());
    }

    private void InitializeFuzzyLogic()
    {
        fuzzyLogicHandler = new FuzzyLogicHandler();

        var carCountMemberships = new Dictionary<string, (double a, double b, double c, double d)>
        {
            { "Low", (0, 0, 7, 10) },
            { "Medium", (8, 10, 20, 22) },
            { "High", (20, 22, 30, 30) }
        };

        var queueLengthMemberships = new Dictionary<string, (double a, double b, double c, double d)>
        {
            { "Small", (0, 0, 15, 45) },
            { "Medium", (40, 50, 60, 70) },
            { "Big", (60, 80, 90, 100) }
        };

        var greenLightDurationMemberships = new Dictionary<string, (double a, double b, double c, double d)>
        {
            { "Short", (0, 10, 20, 30) },
            { "Medium", (20, 30, 50, 60) },
            { "Long", (50, 60, 70, 80) }
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
            foreach (var phase in listToChangeColors)
            {
                ManageGreenLightDuration(phase);

                foreach (var line in phase)
                {
                    line.ChangeColor(TrafficLightColor.green);
                }

                yield return new WaitForSeconds((float)greenLightDuration);

                foreach (var line in phase)
                {
                    line.ChangeColor(TrafficLightColor.yellow);
                }

                yield return new WaitForSeconds(yellowLightDuration);

                foreach (var line in phase)
                {
                    line.ChangeColor(TrafficLightColor.red);
                }

                yield return new WaitForSeconds(timeBeforeGreenLights);
            }
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

        var fuzzifiedInputs = fuzzyLogicHandler.Fuzzify(carCount, queueLength);
        var aggregatedOutputs = fuzzyLogicHandler.ApplyRules(fuzzifiedInputs);

        greenLightDuration = fuzzyLogicHandler.Defuzzify(aggregatedOutputs, "centroid");

        Debug.Log($"Calculated Green Light Duration: {greenLightDuration}");
    }
}
