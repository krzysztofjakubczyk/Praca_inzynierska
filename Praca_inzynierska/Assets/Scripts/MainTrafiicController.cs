using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public enum systemType { staticSystem, fuzzySystem, machineLearning };

public class MainTrafficController : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<PhaseSO> phasesInCycle = new List<PhaseSO>();

    private Dictionary<PhaseSO, int> carCountOnInlet = new Dictionary<PhaseSO, int>();
    private Dictionary<PhaseSO, float> carQueueOnInlet = new Dictionary<PhaseSO, float>();
    ITrafficTimingStrategy timingStrategy;
    bool hadPhillingPhase;
    [SerializeField] CountOfVehiclesManager countOfVehiclesManager;
    [SerializeField] private float currentCycleTime = 0f; // Aktualny czas w cyklu

    [SerializeField] private float timeBeforeNextPhase = 2f; // Czas ewakuacji skrzyżowania (All-Red)
    [SerializeField] private systemType currentChoosedSystemType;
    [SerializeField] private TrafficSignalMlAgent trafficSignalMlAgent;
    [SerializeField] private NNModel trainedPolicyModel;
    [Header("ML Agent Diagnostics")]
    [SerializeField, Min(1)] private int mlLogIntervalCycles = 10;
    private int completedMlCycles;
    public int fullCycleTime = 120;
    private int periodOfTimeToCheckCountOfVehicles = 4;
    private int sample = 0;

    public int activePhase; // Aktywna faza

    private void Start()
    {
        ExitTrigger.ResetTotalVehiclesExited();
        switch (currentChoosedSystemType)
        {
            case systemType.staticSystem:
                timingStrategy = new StaticInterfaceImplementation();
                break;
            case systemType.fuzzySystem:
                timingStrategy = new FuzzyInterfaceImplementation(phasesInCycle, carCountOnInlet, carQueueOnInlet, fullCycleTime);
                break;
            case systemType.machineLearning:
                if (trafficSignalMlAgent == null)
                    trafficSignalMlAgent = GetComponent<TrafficSignalMlAgent>();
                if (trafficSignalMlAgent == null)
                {
                    BehaviorParameters behavior = GetComponent<BehaviorParameters>();
                    if (behavior == null) behavior = gameObject.AddComponent<BehaviorParameters>();
                    behavior.BehaviorName = "TrafficSignal";
                    behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(3);
                    behavior.BrainParameters.VectorObservationSize = 12;
                    trafficSignalMlAgent = gameObject.AddComponent<TrafficSignalMlAgent>();
                }
                trafficSignalMlAgent.Initialize(this);
                if (trainedPolicyModel != null)
                    trafficSignalMlAgent.SetModel("TrafficSignal", trainedPolicyModel);
                break;
        }

        foreach (var phaseInCycle in phasesInCycle)
        {
            carCountOnInlet.Add(phaseInCycle, 0);
            carQueueOnInlet.Add(phaseInCycle, 0);
            SetPhaseColor(phaseInCycle, TrafficLightColor.red);
        }

        StartCoroutine(getVehicleCountOnEntrance());
        StartCoroutine(GlobalTrafficCycle());
    }

    private IEnumerator getVehicleCountOnEntrance()
    {
        Dictionary<PhaseSO, int> tempCarCount = new Dictionary<PhaseSO, int>();
        Dictionary<PhaseSO, float> tempQueueLength = new Dictionary<PhaseSO, float>();

        foreach (var phaseInCycle in phasesInCycle)
        {
            tempCarCount[phaseInCycle] = 0;
            tempQueueLength[phaseInCycle] = 0f;
        }

        while (true)
        {
            foreach (var phaseInCycle in phasesInCycle)
            {
                foreach (var laneInPhase in phaseInCycle.lanes)
                {
                    int countOfVehiclesOnLane = laneInPhase.countOfVehicles;
                    float queueLengthVehiclesOnLane = laneInPhase.queueLength;
                    tempCarCount[phaseInCycle] += countOfVehiclesOnLane;
                    tempQueueLength[phaseInCycle] += queueLengthVehiclesOnLane;
                }
            }

            sample++;
            if (sample >= periodOfTimeToCheckCountOfVehicles)
            {
                foreach (var phaseInCycle in phasesInCycle)
                {
                    carCountOnInlet[phaseInCycle] = tempCarCount[phaseInCycle] / sample;
                    carQueueOnInlet[phaseInCycle] = tempQueueLength[phaseInCycle] / sample;
                    tempCarCount[phaseInCycle] = 0;
                    tempQueueLength[phaseInCycle] = 0f;
                }
                sample = 0;
            }
            yield return new WaitForSeconds((float)fullCycleTime / periodOfTimeToCheckCountOfVehicles);
        }
    }

    private IEnumerator GlobalTrafficCycle()
    {
        while (true)
        {
            currentCycleTime = 0f;

            // 1. Przeliczenie czasów zielonego światła przez logikę rozmytą
            if (currentChoosedSystemType == systemType.fuzzySystem && timingStrategy != null)
            {
                timingStrategy.AdjustGreenLightDurations();
            }

            // 2. Faza wstępnego wypełniania skrzyżowania autami
            if (!hadPhillingPhase)
            {
                if (countOfVehiclesManager != null)
                {
                    yield return new WaitForSeconds(countOfVehiclesManager.fillingPhaseDuration + countOfVehiclesManager.waitingTIme);
                }
                hadPhillingPhase = true;
                currentCycleTime = 0f;
            }

            if (currentChoosedSystemType == systemType.machineLearning)
            {
                yield return StartCoroutine(RunMlControlledCycle());
                continue;
            }

            // 3. Sekwencyjne wykonywanie faz (Faza po Fazie)
            for (int phaseIndex = 0; phaseIndex < phasesInCycle.Count; phaseIndex++)
            {
                activePhase = phaseIndex;
                var currentPhase = phasesInCycle[phaseIndex];

                // BEZWZGLĘDNA BLOKADA: Upewniamy się, że wszystkie INNE fazy są czerwone
                ForceAllOtherPhasesToRed(phaseIndex);

                // Wykonujemy bieżącą fazę i CZEKAMY na jej pełne zakończenie (`yield return`)
                yield return StartCoroutine(ExecutePhaseSequence(currentPhase));

                // EWAKUACJA SKRZYŻOWANIA: Wszystkie światła czerwone przed kolejną fazą
                ForceAllPhasesToRed();
                yield return new WaitForSeconds(timeBeforeNextPhase);
            }
        }
    }

    private IEnumerator RunMlControlledCycle()
    {
        if (trafficSignalMlAgent == null || phasesInCycle.Count == 0)
        {
            ForceAllPhasesToRed();
            yield return new WaitForSeconds(1f);
            yield break;
        }

        float queueBefore = GetTotalQueueLength();
        int exitsBefore = ExitTrigger.TotalVehiclesExited;
        trafficSignalMlAgent.RequestCyclePlan();
        yield return new WaitUntil(() => trafficSignalMlAgent.HasAction);

        float[] greenDurations = AllocateGreenTimes(trafficSignalMlAgent.GreenTimePreferences);
        for (int phaseIndex = 0; phaseIndex < phasesInCycle.Count; phaseIndex++)
        {
            activePhase = phaseIndex;
            ForceAllOtherPhasesToRed(phaseIndex);
            yield return StartCoroutine(ExecutePhaseSequence(phasesInCycle[phaseIndex], greenDurations[phaseIndex]));
            ForceAllPhasesToRed();
            yield return new WaitForSeconds(timeBeforeNextPhase);
        }

        float queueAfter = GetTotalQueueLength();
        int vehiclesExited = ExitTrigger.TotalVehiclesExited - exitsBefore;
        float queuePenalty = queueAfter * fullCycleTime * 0.0001f;
        float queueChangeReward = (queueBefore - queueAfter) * 0.005f;
        float reward = vehiclesExited * 0.1f + queueChangeReward - queuePenalty;
        trafficSignalMlAgent.AddPhaseReward(reward);

        completedMlCycles++;
        if (mlLogIntervalCycles > 0 && completedMlCycles % mlLogIntervalCycles == 0)
        {
            string phaseTimings = string.Empty;
            for (int i = 0; i < greenDurations.Length; i++)
            {
                if (i > 0) phaseTimings += ", ";
                phaseTimings += $"Faza {i + 1}: {greenDurations[i]:F1}s";
            }

            Debug.Log($"[ML Traffic] Cykl {completedMlCycles}; {phaseTimings}; " +
                      $"pojazdy wyjechały: {vehiclesExited}; kolejka: {queueBefore:F1} -> {queueAfter:F1}; nagroda: {reward:F3}");
        }

        TimeManager timeManager = FindObjectOfType<TimeManager>();
        if (timeManager != null && timeManager.CompleteMlTrainingEpisode(trafficSignalMlAgent))
        {
            ForceAllPhasesToRed();
            foreach (var phase in phasesInCycle)
            {
                carCountOnInlet[phase] = 0;
                carQueueOnInlet[phase] = 0f;
            }
            sample = 0;
        }
    }

    private float[] AllocateGreenTimes(float[] preferences)
    {
        const float minimumGreenSeconds = 5f;
        const float yellowBeforeGreenSeconds = 1.5f;
        const float yellowAfterGreenSeconds = 2f;
        int phaseCount = phasesInCycle.Count;
        float[] result = new float[phaseCount];
        float fixedTime = phaseCount * (yellowBeforeGreenSeconds + yellowAfterGreenSeconds + timeBeforeNextPhase);
        float greenBudget = Mathf.Max(minimumGreenSeconds * phaseCount, fullCycleTime - fixedTime);
        float distributableTime = Mathf.Max(0f, greenBudget - minimumGreenSeconds * phaseCount);

        // Softmax turns the agent's three preferences into shares that sum to one.
        float maxPreference = 0f;
        for (int i = 0; i < phaseCount; i++)
            maxPreference = Mathf.Max(maxPreference, i < preferences.Length ? preferences[i] : 0f);

        float totalWeight = 0f;
        float[] weights = new float[phaseCount];
        for (int i = 0; i < phaseCount; i++)
        {
            float preference = i < preferences.Length ? preferences[i] : 0f;
            weights[i] = Mathf.Exp((preference - maxPreference) * 3f);
            totalWeight += weights[i];
        }

        float allocated = 0f;
        for (int i = 0; i < phaseCount; i++)
        {
            float extra = i == phaseCount - 1
                ? distributableTime - allocated
                : distributableTime * weights[i] / totalWeight;
            result[i] = minimumGreenSeconds + extra;
            allocated += extra;
        }

        return result;
    }

    public int PhaseCount => phasesInCycle != null ? phasesInCycle.Count : 0;

    public void AddPhaseObservations(VectorSensor sensor)
    {
        for (int phaseIndex = 0; phaseIndex < 3; phaseIndex++)
        {
            int vehicles = 0;
            float queue = 0f;
            bool blocked = false;
            if (phaseIndex < phasesInCycle.Count && phasesInCycle[phaseIndex] != null && phasesInCycle[phaseIndex].lanes != null)
            {
                foreach (var lane in phasesInCycle[phaseIndex].lanes)
                {
                    if (lane == null) continue;
                    vehicles += lane.countOfVehicles;
                    queue += lane.queueLength;
                    blocked |= lane.isBlocked;
                }
            }
            sensor.AddObservation(Mathf.Clamp01(vehicles / 40f));
            sensor.AddObservation(Mathf.Clamp01(queue / 100f));
            sensor.AddObservation(blocked ? 1f : 0f);
            sensor.AddObservation(activePhase == phaseIndex ? 1f : 0f);
        }
    }

    private float GetTotalQueueLength()
    {
        float total = 0f;
        foreach (var phase in phasesInCycle)
        {
            if (phase == null || phase.lanes == null) continue;
            foreach (var lane in phase.lanes)
                if (lane != null) total += lane.queueLength;
        }
        return total;
    }

    private IEnumerator ExecutePhaseSequence(PhaseSO phase, float? requestedGreenDuration = null)
    {
        if (phase == null || phase.lanes == null || phase.lanes.Count == 0)
            yield break;

        // Pobieramy czas zielonego światła (np. z pierwszego pasa w fazie)
        float greenDuration = requestedGreenDuration ?? (float)phase.lanes[0].timeOfGreenLight;
        if (greenDuration <= 0f) greenDuration = 10f; // Bezpieczny fallback

        // A. Czerwone + Żółte (Przygotowanie do ruchu)
        SetPhaseColor(phase, TrafficLightColor.yellow);
        yield return new WaitForSeconds(1.5f);

        // B. Zielone Światło
        SetPhaseColor(phase, TrafficLightColor.green);
        yield return new WaitForSeconds(greenDuration);

        // C. Żółte Światło (Wygaszanie fazy)
        SetPhaseColor(phase, TrafficLightColor.yellow);
        yield return new WaitForSeconds(2f);

        // D. Czerwone Światło
        SetPhaseColor(phase, TrafficLightColor.red);
    }

    private void SetPhaseColor(PhaseSO phase, TrafficLightColor color)
    {
        if (phase == null || phase.lanes == null) return;
        foreach (var lane in phase.lanes)
        {
            if (lane != null)
            {
                lane.ChangeColor(color);
            }
        }
    }

    private void ForceAllOtherPhasesToRed(int activePhaseIndex)
    {
        for (int i = 0; i < phasesInCycle.Count; i++)
        {
            if (i != activePhaseIndex)
            {
                SetPhaseColor(phasesInCycle[i], TrafficLightColor.red);
            }
        }
    }

    private void ForceAllPhasesToRed()
    {
        for (int i = 0; i < phasesInCycle.Count; i++)
        {
            SetPhaseColor(phasesInCycle[i], TrafficLightColor.red);
        }
    }

    private void Update()
    {
        currentCycleTime += Time.deltaTime;
        if (currentCycleTime >= fullCycleTime)
        {
            currentCycleTime = 0f;
        }
    }
}
