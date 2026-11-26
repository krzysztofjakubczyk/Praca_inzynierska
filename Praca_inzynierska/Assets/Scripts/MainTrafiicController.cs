using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Barracuda;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

// Kolejność wartości ma znaczenie (zapisane w scenach jako liczby) – nowe tryby tylko na końcu.
public enum systemType { staticSystem, fuzzySystem, machineLearning, actuatedSystem };

public class MainTrafficController : MonoBehaviour
{
    public const int MaxPhases = 4;
    public const int ObservationsPerPhase = 6;
    public const int GlobalObservations = 4;
    public const int ObservationSize = MaxPhases * ObservationsPerPhase + GlobalObservations; // 28
    private const float ObservationNormVehicles = 20f;

    private const float YellowBeforeGreen = 1.5f; // czerwone + żółte
    private const float YellowAfterGreen = 3f;     // żółte po zielonym (standard 3 s)
    private const float MinAllRedSeconds = 3f;     // minimalny all-red: czas na opuszczenie skrzyżowania (~40 m)

    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<PhaseSO> phasesInCycle = new List<PhaseSO>();

    private readonly Dictionary<PhaseSO, int> carCountOnInlet = new Dictionary<PhaseSO, int>();
    private readonly Dictionary<PhaseSO, float> carQueueOnInlet = new Dictionary<PhaseSO, float>();
    private ITrafficTimingStrategy timingStrategy;
    private bool hadPhillingPhase;
    [SerializeField] CountOfVehiclesManager countOfVehiclesManager;
    [SerializeField] private float currentCycleTime = 0f; // Aktualny czas w cyklu

    [SerializeField] private float timeBeforeNextPhase = 2f; // Czas ewakuacji skrzyżowania (All-Red), min. 3 s
    private float AllRedSeconds => Mathf.Max(timeBeforeNextPhase, MinAllRedSeconds);
    [SerializeField] private systemType currentChoosedSystemType;
    [SerializeField] private TrafficSignalMlAgent trafficSignalMlAgent;
    [SerializeField] private NNModel trainedPolicyModel;

    [Header("Diagnostyka")]
    [SerializeField, Min(1)] private int mlLogIntervalCycles = 10;
    [Tooltip("Ostrzeżenie o zakleszczeniu, gdy przez tyle sekund żaden pojazd nie przejechał linii zatrzymania.")]
    [SerializeField, Min(30f)] private float gridlockSeconds = 180f;

    [Header("Sterowanie adaptacyjne (ML i actuated)")]
    [Tooltip("Co ile sekund zielonego agent decyduje: przedłuż / przełącz.")]
    [SerializeField, Min(1f)] private float decisionInterval = 5f;
    [SerializeField, Min(1f)] private float minGreen = 8f;
    [SerializeField, Min(5f)] private float maxGreen = 60f;
    [Tooltip("Actuated: przełącz, gdy na bieżącej fazie czeka/nadjeżdża nie więcej niż tyle pojazdów, a na innej ktoś czeka.")]
    [SerializeField, Min(0)] private int actuatedGapOutVehicles = 0;
    [Tooltip("Pomijaj fazy, na których nikt nie czeka (jak sterownik akomodacyjny). Gdy nikt nie czeka nigdzie indziej, " +
             "przełączenie jest ignorowane – agent nie traci czasu na żółte/all-red bez potrzeby.")]
    [SerializeField] private bool skipEmptyPhases = true;
    [Tooltip("Stały cykl (fullCycleTime, 120 s): fazy w stałej kolejności, każda min. minGreen, ostatnia dostaje resztę. " +
             "Agent decyduje tylko o podziale zielonego w cyklu. Wyłącza pomijanie pustych faz.")]
    [SerializeField] private bool fixedCycleLength = true;
    [Tooltip("Stały cykl: najkrótsze zielone fazy jako ułamek równego podziału (0.5 = połowa z 97.5 s / liczba faz).")]
    [SerializeField, Range(0.2f, 1f)] private float phaseShareMin = 0.5f;
    [Tooltip("Stały cykl: najdłuższe zielone fazy jako krotność równego podziału.")]
    [SerializeField, Range(1f, 3f)] private float phaseShareMax = 1.5f;

    [Header("Nagroda ML")]
    [Tooltip("Normalizacja: nagroda = -(pojazdo-sekundy oczekiwania) / (ta liczba * decisionInterval). " +
             "50 daje nagrodę rzędu -0.1…-1.5 na krok przy natężeniach z pomiarów.")]
    [SerializeField, Min(1f)] private float rewardNormalizationVehicles = 50f;
    [Tooltip("Dodatkowa mała kara za każde przełączenie fazy (zniechęca do migotania).")]
    [SerializeField, Min(0f)] private float switchPenalty = 0.02f;

    [Header("Demonstracje (start uczenia od heurystyki)")]
    [Tooltip("W trybie ML bez trenera i bez modelu agent jedzie heurystyką actuated – zaznacz, aby nagrać to " +
             "jako demonstrację (Assets/Demonstrations/TrafficSignal.demo) do behavioral cloning.")]
    [SerializeField] private bool recordDemonstrations = false;
    [SerializeField, Min(100)] private int demonstrationSteps = 5000;

    public int fullCycleTime = 120;
    private readonly int periodOfTimeToCheckCountOfVehicles = 4;
    private int sample = 0;

    public int activePhase; // Aktywna faza

    // ---- Stan sterowania adaptacyjnego ----
    private readonly Dictionary<LineLightManager, int> phaseOfLane = new Dictionary<LineLightManager, int>();
    private readonly int[] haltedPerPhase = new int[MaxPhases];
    private readonly int[] approachingPerPhase = new int[MaxPhases];
    private readonly float[] lastServedGreen = new float[MaxPhases];
    private readonly float[] phaseLastEndTime = new float[MaxPhases]; // kiedy faza ostatnio miała zielone
    private float currentPhaseMaxGreen = 60f;  // limit zielonego bieżącej fazy (w stałym cyklu wynika z budżetu)
    private float currentPhaseMinGreen = 8f;   // minimum zielonego bieżącej fazy
    private float cycleBudgetLeft = 1f;        // ułamek budżetu zielonego cyklu, który został na początku fazy
    private float cycleGreenBudget = 103.5f;   // zielone w cyklu = fullCycleTime - żółte/all-red
    private int totalHalted;
    private float greenStartTime;
    private bool phaseIsGreen;
    private float haltedIntegral;          // pojazdo-sekundy oczekiwania od ostatniej decyzji
    private bool switchedSinceDecision;
    private int decisionsInEpisode;
    private float episodeReward;
    private int completedAdaptiveCycles;
    private int lastCrossedCount;
    private float lastProgressTime;
    private TimeManager timeManager;

    public int PhaseCount => phasesInCycle != null ? phasesInCycle.Count : 0;
    public float TimeInGreen => phaseIsGreen ? Time.time - greenStartTime : 0f;

    /// <summary>W trybie ML koniec epizodu obsługuje sterownik (nagroda końcowa + EpisodeInterrupted).</summary>
    public bool HandlesEpisodeEnd => currentChoosedSystemType == systemType.machineLearning && trafficSignalMlAgent != null;

    public string SystemLabel
    {
        get
        {
            switch (currentChoosedSystemType)
            {
                case systemType.fuzzySystem: return "fuzzy";
                case systemType.machineLearning: return "ml";
                case systemType.actuatedSystem: return "actuated";
                default: return "static";
            }
        }
    }

    public string ModeLabel => currentChoosedSystemType == systemType.machineLearning && trafficSignalMlAgent != null
        ? trafficSignalMlAgent.ModeLabel
        : "rule-based";

    /// <summary>Ustawienie trybu przed Start() (np. ze scenariusza wybranego w menu).</summary>
    public void SetSystemType(systemType type) => currentChoosedSystemType = type;

    private void Start()
    {
        ExitTrigger.ResetTotalVehiclesExited();
        CarController.ResetGlobalCounters();
        timeManager = FindObjectOfType<TimeManager>();

        for (int p = 0; p < phasesInCycle.Count; p++)
        {
            PhaseSO phaseInCycle = phasesInCycle[p];
            carCountOnInlet[phaseInCycle] = 0;
            carQueueOnInlet[phaseInCycle] = 0;
            SetPhaseColor(phaseInCycle, TrafficLightColor.red);
            if (phaseInCycle == null || phaseInCycle.lanes == null) continue;
            foreach (var lane in phaseInCycle.lanes)
            {
                if (lane != null && !phaseOfLane.ContainsKey(lane)) phaseOfLane.Add(lane, p);
            }
        }

        float lostTimePerPhase = YellowBeforeGreen + YellowAfterGreen + AllRedSeconds;
        switch (currentChoosedSystemType)
        {
            case systemType.staticSystem:
                timingStrategy = new StaticInterfaceImplementation();
                break;
            case systemType.fuzzySystem:
                timingStrategy = new FuzzyInterfaceImplementation(phasesInCycle, carCountOnInlet, carQueueOnInlet,
                    fullCycleTime - phasesInCycle.Count * lostTimePerPhase, lostTimePerPhase);
                break;
            case systemType.machineLearning:
                EnsureAgent();
                break;
        }

        if ((currentChoosedSystemType == systemType.machineLearning || currentChoosedSystemType == systemType.actuatedSystem)
            && phasesInCycle.Count > MaxPhases)
        {
            Debug.LogError($"Sterowanie adaptacyjne obsługuje maksymalnie {MaxPhases} fazy (jest {phasesInCycle.Count}).");
        }

        lastProgressTime = Time.time;
        StartCycleCoroutines();
    }

    private void EnsureAgent()
    {
        if (trafficSignalMlAgent == null)
            trafficSignalMlAgent = GetComponent<TrafficSignalMlAgent>();

        if (trafficSignalMlAgent == null)
        {
            BehaviorParameters behavior = GetComponent<BehaviorParameters>();
            if (behavior == null) behavior = gameObject.AddComponent<BehaviorParameters>();
            TrafficSignalMlAgent.ConfigureBehavior(behavior); // przed AddComponent<Agent>!
            trafficSignalMlAgent = gameObject.AddComponent<TrafficSignalMlAgent>();
        }

        trafficSignalMlAgent.Initialize(this);
        if (trainedPolicyModel != null)
            trafficSignalMlAgent.SetModel(TrafficSignalMlAgent.BehaviorNameValue, trainedPolicyModel);

        if (recordDemonstrations && trainedPolicyModel == null)
        {
            var recorder = GetComponent<Unity.MLAgents.Demonstrations.DemonstrationRecorder>();
            if (recorder == null) recorder = gameObject.AddComponent<Unity.MLAgents.Demonstrations.DemonstrationRecorder>();
            recorder.DemonstrationName = "TrafficSignal";
            recorder.DemonstrationDirectory = "Assets/Demonstrations";
            recorder.NumStepsToRecord = demonstrationSteps;
            recorder.Record = true;
            Debug.Log($"[ML Traffic] Nagrywanie demonstracji heurystyki: {demonstrationSteps} kroków -> Assets/Demonstrations/TrafficSignal.demo " +
                      "(uruchom BEZ mlagents-learn).");
        }
    }

    private void StartCycleCoroutines()
    {
        StartCoroutine(getVehicleCountOnEntrance());
        StartCoroutine(GlobalTrafficCycle());
    }

    /// <summary>Wywoływane przez TimeManager po zresetowaniu symulacji (nowy epizod / kolejny przebieg).</summary>
    public void RestartAfterEpisodeReset()
    {
        StopAllCoroutines();
        foreach (var phase in phasesInCycle)
        {
            carCountOnInlet[phase] = 0;
            carQueueOnInlet[phase] = 0f;
        }
        sample = 0;
        hadPhillingPhase = false;
        activePhase = 0;
        phaseIsGreen = false;
        ForceAllPhasesToRed();
        haltedIntegral = 0f;
        switchedSinceDecision = false;
        decisionsInEpisode = 0;
        episodeReward = 0f;
        CarController.ResetGlobalCounters();
        lastCrossedCount = 0;
        lastProgressTime = Time.time;
        StartCycleCoroutines();
    }

    // =====================================================================
    // Pomiar ruchu (co krok fizyki) – nagroda, obserwacje, KPI
    // =====================================================================

    private void FixedUpdate()
    {
        CountVehicles();
        float dt = Time.fixedDeltaTime;
        int backlog = VehicleSpawner.TotalBacklog;

        // Całka z liczby czekających pojazdów = łączny czas oczekiwania (pojazdo-sekundy).
        haltedIntegral += (totalHalted + backlog) * dt;
        SimulationKpi.Accumulate(totalHalted, backlog, dt);
        DetectGridlock();
    }

    private void CountVehicles()
    {
        Array.Clear(haltedPerPhase, 0, MaxPhases);
        Array.Clear(approachingPerPhase, 0, MaxPhases);
        totalHalted = 0;

        List<CarController> cars = CarController.Active;
        for (int i = 0; i < cars.Count; i++)
        {
            CarController car = cars[i];
            bool halted = car.IsHalted;
            if (halted) totalHalted++;
            if (car.HasCrossedStopLine) continue;

            LineLightManager light = car.GoverningLight;
            if (light == null || !phaseOfLane.TryGetValue(light, out int phase) || phase >= MaxPhases) continue;
            if (halted) haltedPerPhase[phase]++;
            else approachingPerPhase[phase]++;
        }
    }

    private void DetectGridlock()
    {
        int crossed = CarController.VehiclesCrossedStopLine;
        if (crossed != lastCrossedCount)
        {
            lastCrossedCount = crossed;
            lastProgressTime = Time.time;
        }
        else if (CarController.Active.Count > 10 && Time.time - lastProgressTime > gridlockSeconds)
        {
            SimulationKpi.GridlockEvents++;
            lastProgressTime = Time.time;
            Debug.LogWarning($"[Ruch] Przez {gridlockSeconds:F0} s żaden pojazd nie przejechał linii zatrzymania " +
                             $"({CarController.Active.Count} pojazdów w sieci). Możliwe zakleszczenie.");
        }
    }

    // =====================================================================
    // Cykl świateł
    // =====================================================================

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
                if (phaseInCycle == null || phaseInCycle.lanes == null) continue;
                foreach (var laneInPhase in phaseInCycle.lanes)
                {
                    if (laneInPhase == null) continue;
                    tempCarCount[phaseInCycle] += laneInPhase.countOfVehicles;
                    tempQueueLength[phaseInCycle] += laneInPhase.queueLength;
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
                timingStrategy.AdjustGreenLightDurations();

            // 2. Faza wstępnego wypełniania skrzyżowania autami
            if (!hadPhillingPhase)
            {
                if (countOfVehiclesManager != null)
                    yield return new WaitForSeconds(countOfVehiclesManager.fillingPhaseDuration + countOfVehiclesManager.waitingTIme);
                hadPhillingPhase = true;
                currentCycleTime = 0f;
            }

            if (currentChoosedSystemType == systemType.machineLearning || currentChoosedSystemType == systemType.actuatedSystem)
            {
                yield return StartCoroutine(RunAdaptiveControl()); // pętla bez końca (restart przez RestartAfterEpisodeReset)
                yield break;
            }

            // 3. Sekwencyjne wykonywanie faz (statyczny / rozmyty)
            for (int phaseIndex = 0; phaseIndex < phasesInCycle.Count; phaseIndex++)
            {
                activePhase = phaseIndex;
                ForceAllOtherPhasesToRed(phaseIndex);
                yield return StartCoroutine(ExecutePhaseSequence(phasesInCycle[phaseIndex]));
                ForceAllPhasesToRed();
                yield return new WaitForSeconds(AllRedSeconds);
            }
        }
    }

    /// <summary>
    /// Sterowanie "przedłuż / przełącz": stała kolejność faz, zielone trwa od minGreen do maxGreen,
    /// a co decisionInterval sekund decyduje agent ML (albo heurystyka actuated).
    /// </summary>
    private IEnumerator RunAdaptiveControl()
    {
        int count = Mathf.Min(phasesInCycle.Count, MaxPhases);
        if (count == 0)
        {
            ForceAllPhasesToRed();
            yield break;
        }

        bool useAgent = currentChoosedSystemType == systemType.machineLearning && trafficSignalMlAgent != null;
        bool fixedCycle = fixedCycleLength;
        bool skip = skipEmptyPhases && !fixedCycle;
        float lostPerPhase = YellowBeforeGreen + YellowAfterGreen + AllRedSeconds;
        float greenBudget = Mathf.Max(count * 1f, fullCycleTime - count * lostPerPhase); // 120 s -> 103.5 s zielonego
        float cycleMinGreen = Mathf.Min(minGreen, greenBudget / count);
        cycleGreenBudget = greenBudget;
        float cycleGreenUsed = 0f;
        int phaseIndex = 0;
        haltedIntegral = 0f;
        switchedSinceDecision = false;
        for (int p = 0; p < MaxPhases; p++) phaseLastEndTime[p] = Time.time;

        while (true)
        {
            activePhase = phaseIndex;
            PhaseSO phase = phasesInCycle[phaseIndex];
            ForceAllOtherPhasesToRed(phaseIndex);

            // Stały cykl: każda faza dostaje od phaseShareMin do phaseShareMax "sprawiedliwego udziału"
            // (zielone cyklu / liczba faz), a granice są tak liczone, by ostatnia faza – która domyka cykl
            // do dokładnie fullCycleTime (120 s) – też zmieściła się w tym przedziale.
            if (fixedCycle && phaseIndex == 0) cycleGreenUsed = 0f;
            bool lastInCycle = fixedCycle && phaseIndex == count - 1;
            float phaseMax, phaseMin;
            if (fixedCycle)
            {
                float baseShare = greenBudget / count;
                float floorGreen = Mathf.Max(cycleMinGreen, phaseShareMin * baseShare);
                float ceilGreen = Mathf.Max(floorGreen, phaseShareMax * baseShare);
                float remaining = greenBudget - cycleGreenUsed;
                int later = count - 1 - phaseIndex;
                if (lastInCycle)
                {
                    phaseMax = phaseMin = Mathf.Max(1f, remaining);
                }
                else
                {
                    phaseMax = Mathf.Min(ceilGreen, remaining - later * floorGreen);
                    phaseMin = Mathf.Max(floorGreen, remaining - later * ceilGreen);
                    phaseMin = Mathf.Min(phaseMin, phaseMax);
                }
            }
            else
            {
                phaseMax = maxGreen;
                phaseMin = minGreen;
            }
            currentPhaseMinGreen = phaseMin;
            currentPhaseMaxGreen = phaseMax;
            cycleBudgetLeft = fixedCycle ? (greenBudget - cycleGreenUsed) / greenBudget : 1f;

            SetPhaseColor(phase, TrafficLightColor.yellow); // czerwone + żółte
            yield return new WaitForSeconds(YellowBeforeGreen);

            SetPhaseColor(phase, TrafficLightColor.green);
            greenStartTime = Time.time;
            phaseIsGreen = true;
            yield return new WaitForSeconds(phaseMin);

            while (!lastInCycle)
            {
                if (TimeInGreen >= phaseMax - 0.01f)
                {
                    // Po maxGreen: jeśli nikt inny nie czeka, zostajemy na tej fazie (licznik zielonego od minGreen).
                    if (skip && NextPhaseWithDemand(phaseIndex, count) == phaseIndex)
                        greenStartTime = Time.time - minGreen;
                    else
                        break;
                }

                bool switchNow;
                if (useAgent)
                {
                    if (timeManager != null && timeManager.IsEpisodeOver)
                    {
                        EndMlEpisode();
                        yield break;
                    }

                    DeliverRewardAndRequestDecision();
                    yield return new WaitUntil(() => trafficSignalMlAgent.HasAction);
                    switchNow = trafficSignalMlAgent.SwitchRequested;
                }
                else
                {
                    switchNow = ActuatedWantsSwitch();
                }

                // Nikt nie czeka na innych fazach -> zmiana nic nie da, tylko straci czas na żółte/all-red.
                if (switchNow && skip && NextPhaseWithDemand(phaseIndex, count) == phaseIndex) switchNow = false;

                if (switchNow) break;
                yield return new WaitForSeconds(Mathf.Min(decisionInterval, Mathf.Max(0.01f, phaseMax - TimeInGreen)));
            }

            lastServedGreen[phaseIndex] = TimeInGreen;
            cycleGreenUsed += TimeInGreen;
            phaseIsGreen = false;
            switchedSinceDecision = !fixedCycle; // w stałym cyklu liczba przełączeń jest stała – bez kary
            phaseLastEndTime[phaseIndex] = Time.time;

            SetPhaseColor(phase, TrafficLightColor.yellow);
            yield return new WaitForSeconds(YellowAfterGreen);
            ForceAllPhasesToRed();
            yield return new WaitForSeconds(AllRedSeconds);

            int next = skip ? NextPhaseWithDemand(phaseIndex, count) : (phaseIndex + 1) % count;
            if (next == phaseIndex) next = (phaseIndex + 1) % count;
            if (next <= phaseIndex) LogAdaptiveCycle(count);
            phaseIndex = next;
        }
    }

    /// <summary>Następna faza w kolejności, na której ktoś czeka; jeśli nigdzie – zwraca bieżącą.</summary>
    private int NextPhaseWithDemand(int current, int count)
    {
        for (int step = 1; step < count; step++)
        {
            int candidate = (current + step) % count;
            if (PhaseDemand(candidate) > 0) return candidate;
        }
        return current;
    }

    private float TakeStepReward()
    {
        float reward = -haltedIntegral / (rewardNormalizationVehicles * decisionInterval);
        if (switchedSinceDecision) reward -= switchPenalty;
        haltedIntegral = 0f;
        switchedSinceDecision = false;
        return reward;
    }

    private void DeliverRewardAndRequestDecision()
    {
        float reward = TakeStepReward();
        // Pierwsza decyzja epizodu nie ma poprzedniej akcji – nagrody (np. z fazy wypełniania) nie przypisujemy.
        if (decisionsInEpisode > 0)
        {
            trafficSignalMlAgent.AddStepReward(reward);
            episodeReward += reward;
        }
        decisionsInEpisode++;
        trafficSignalMlAgent.RequestSwitchDecision(TimeInGreen >= currentPhaseMinGreen - 0.01f, TimeInGreen >= currentPhaseMaxGreen - 0.01f);
    }

    private void EndMlEpisode()
    {
        float reward = TakeStepReward();
        if (decisionsInEpisode > 0)
        {
            trafficSignalMlAgent.AddStepReward(reward);
            episodeReward += reward;
        }

        Debug.Log($"[ML Traffic] Koniec epizodu: decyzji {decisionsInEpisode}, suma nagród {episodeReward:F2}, " +
                  $"kolizje {CarController.CollisionCount}, zakleszczenia {CarController.StuckEvents}.");

        // Limit czasu to ucięcie epizodu, a nie stan końcowy – PPO poprawnie szacuje wartość stanu.
        trafficSignalMlAgent.EpisodeInterrupted();

        if (timeManager != null) timeManager.FinishEpisodeAndRestart(); // wywołuje RestartAfterEpisodeReset()
        else RestartAfterEpisodeReset();
    }

    private void LogAdaptiveCycle(int count)
    {
        completedAdaptiveCycles++;
        if (mlLogIntervalCycles <= 0 || completedAdaptiveCycles % mlLogIntervalCycles != 0) return;

        StringBuilder timings = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            if (i > 0) timings.Append(", ");
            timings.Append($"Faza {i + 1}: {lastServedGreen[i]:F0}s");
        }
        string label = currentChoosedSystemType == systemType.machineLearning ? "ML Traffic" : "Actuated";
        Debug.Log($"[{label}] Cykl {completedAdaptiveCycles}; zielone: {timings}; czekających: {totalHalted}, " +
                  $"przed wjazdem: {VehicleSpawner.TotalBacklog}; nagroda w epizodzie: {episodeReward:F2}");
    }

    /// <summary>
    /// Heurystyka actuated (gap-out): przełącz, gdy bieżąca faza się opróżniła, a na innej ktoś czeka.
    /// Używana jako osobny tryb porównawczy i jako Heuristic() agenta.
    /// </summary>
    public bool ActuatedWantsSwitch()
    {
        int count = Mathf.Min(phasesInCycle.Count, MaxPhases);
        if (activePhase < 0 || activePhase >= count) return false;
        if (TimeInGreen < currentPhaseMinGreen - 0.01f) return false;

        if (fixedCycleLength)
        {
            // Stały cykl: zielone proporcjonalne do liczby czekających na fazach, które zostały w tym cyklu
            // (bieżąca + kolejne). Faza bez pojazdów oddaje czas od razu.
            float remainingGreen = cycleBudgetLeft * cycleGreenBudget;
            float minG = Mathf.Min(minGreen, cycleGreenBudget / count);
            float total = 0f;
            for (int p = activePhase; p < count; p++) total += PhaseDemand(p) + 0.5f;
            float share = (PhaseDemand(activePhase) + 0.5f) / Mathf.Max(0.5f, total);
            int phasesLeft = count - activePhase;
            float target = Mathf.Clamp(minG + Mathf.Max(0f, remainingGreen - minG * phasesLeft) * share, currentPhaseMinGreen, currentPhaseMaxGreen);
            return TimeInGreen >= target || PhaseDemand(activePhase) == 0;
        }

        if (PhaseDemand(activePhase) > actuatedGapOutVehicles) return false;

        for (int p = 0; p < count; p++)
        {
            if (p != activePhase && PhaseDemand(p) > 0) return true;
        }
        return false;
    }

    private int PhaseDemand(int phase)
    {
        int demand = haltedPerPhase[phase] + approachingPerPhase[phase];
        return demand > 0 ? demand : SensorVehicles(phase);
    }

    private int SensorVehicles(int phase)
    {
        int vehicles = 0;
        PhaseSO phaseSO = phasesInCycle[phase];
        if (phaseSO == null || phaseSO.lanes == null) return 0;
        foreach (var lane in phaseSO.lanes)
        {
            if (lane != null) vehicles += lane.countOfVehicles;
        }
        return vehicles;
    }

    private bool PhaseBlocked(int phase)
    {
        PhaseSO phaseSO = phasesInCycle[phase];
        if (phaseSO == null || phaseSO.lanes == null) return false;
        foreach (var lane in phaseSO.lanes)
        {
            if (lane != null && lane.isBlocked) return true;
        }
        return false;
    }

    /// <summary>
    /// 28 obserwacji: 4 fazy x (czekające, nadjeżdżające, z czujników, blokada wyjazdu, aktywna,
    /// czas od ostatniego zielonego) + 3 globalne (czas bieżącego zielonego, kolejka przed wjazdem, wszystkie czekające).
    /// </summary>
    public void AddObservations(VectorSensor sensor)
    {
        const float norm = ObservationNormVehicles;
        int count = Mathf.Min(phasesInCycle.Count, MaxPhases);
        for (int p = 0; p < MaxPhases; p++)
        {
            if (p < count)
            {
                bool active = activePhase == p && phaseIsGreen;
                sensor.AddObservation(Mathf.Clamp(haltedPerPhase[p] / norm, 0f, 3f));
                sensor.AddObservation(Mathf.Clamp(approachingPerPhase[p] / norm, 0f, 3f));
                sensor.AddObservation(Mathf.Clamp(SensorVehicles(p) / norm, 0f, 3f));
                sensor.AddObservation(PhaseBlocked(p) ? 1f : 0f);
                sensor.AddObservation(active ? 1f : 0f);
                sensor.AddObservation(active ? 0f : Mathf.Clamp((Time.time - phaseLastEndTime[p]) / 120f, 0f, 2f));
            }
            else
            {
                for (int i = 0; i < ObservationsPerPhase; i++) sensor.AddObservation(0f);
            }
        }

        sensor.AddObservation(Mathf.Clamp01(TimeInGreen / Mathf.Max(1f, currentPhaseMaxGreen)));
        sensor.AddObservation(Mathf.Clamp01(cycleBudgetLeft)); // ile zielonego zostało w cyklu 120 s
        sensor.AddObservation(Mathf.Clamp(VehicleSpawner.TotalBacklog / norm, 0f, 3f));
        sensor.AddObservation(Mathf.Clamp(totalHalted / norm, 0f, 3f));
    }

    private IEnumerator ExecutePhaseSequence(PhaseSO phase)
    {
        if (phase == null || phase.lanes == null || phase.lanes.Count == 0)
            yield break;

        // Czas zielonego z pierwszego pasa w fazie
        float greenDuration = (float)phase.lanes[0].timeOfGreenLight;
        if (greenDuration <= 0f) greenDuration = 10f; // Bezpieczny fallback

        SetPhaseColor(phase, TrafficLightColor.yellow); // A. Czerwone + Żółte
        yield return new WaitForSeconds(YellowBeforeGreen);

        SetPhaseColor(phase, TrafficLightColor.green);  // B. Zielone
        yield return new WaitForSeconds(greenDuration);

        SetPhaseColor(phase, TrafficLightColor.yellow); // C. Żółte
        yield return new WaitForSeconds(YellowAfterGreen);

        SetPhaseColor(phase, TrafficLightColor.red);    // D. Czerwone
    }

    private void SetPhaseColor(PhaseSO phase, TrafficLightColor color)
    {
        if (phase == null || phase.lanes == null) return;
        foreach (var lane in phase.lanes)
        {
            if (lane != null) lane.ChangeColor(color);
        }
    }

    private void ForceAllOtherPhasesToRed(int activePhaseIndex)
    {
        for (int i = 0; i < phasesInCycle.Count; i++)
        {
            if (i != activePhaseIndex) SetPhaseColor(phasesInCycle[i], TrafficLightColor.red);
        }
    }

    private void ForceAllPhasesToRed()
    {
        for (int i = 0; i < phasesInCycle.Count; i++)
            SetPhaseColor(phasesInCycle[i], TrafficLightColor.red);
    }

    private void Update()
    {
        currentCycleTime += Time.deltaTime;
        if (currentCycleTime >= fullCycleTime) currentCycleTime = 0f;
    }
}
