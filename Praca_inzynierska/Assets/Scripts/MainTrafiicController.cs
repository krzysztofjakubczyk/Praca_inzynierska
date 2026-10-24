using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum systemType { staticSystem, fuzzySystem };

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
    public int fullCycleTime = 120;
    private int periodOfTimeToCheckCountOfVehicles = 4;
    private int sample = 0;

    public int activePhase; // Aktywna faza

    private void Start()
    {
        switch (currentChoosedSystemType)
        {
            case systemType.staticSystem:
                timingStrategy = new StaticInterfaceImplementation();
                break;
            case systemType.fuzzySystem:
                timingStrategy = new FuzzyInterfaceImplementation(phasesInCycle, carCountOnInlet, carQueueOnInlet, fullCycleTime);
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

    private IEnumerator ExecutePhaseSequence(PhaseSO phase)
    {
        if (phase == null || phase.lanes == null || phase.lanes.Count == 0)
            yield break;

        // Pobieramy czas zielonego światła (np. z pierwszego pasa w fazie)
        float greenDuration = (float)phase.lanes[0].timeOfGreenLight;
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