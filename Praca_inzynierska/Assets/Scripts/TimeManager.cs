using System.Collections.Generic;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private const float StatisticsWarmupSeconds = 400f; // Po 6 minutach i 20 sekundach

    [SerializeField] private TMP_Dropdown dropDownHour;
    [SerializeField] private List<Counter> Counters;
    [SerializeField] private List<string> hours;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private int simulationSpeedUpValue; // nieużywane – Q przełącza 1x/5x/10x
    [SerializeField] public bool isAfternoonPeakOnly; // Czy symulacja działa tylko w szczycie popołudniowym?
    [SerializeField] private CountOfVehiclesManager countOfVehiclesManager;

    [Header("Powtarzanie godziny (trening ML / serie pomiarowe)")]
    [Tooltip("Po zakończeniu epizodu symulacja tej samej godziny rusza od nowa. Działa dla wszystkich trybów – " +
             "każdy przebieg dopisuje wiersz do Results/kpi_runs.csv.")]
    [SerializeField] private bool repeatHourForMlTraining;
    [Tooltip("Długość epizodu w sekundach symulacji (domyślnie pełna godzina + 20 s).")]
    [SerializeField, Min(60f)] private float episodeLengthSeconds = 3620f;
    [Tooltip("Mnożnik natężenia ruchu (1 = dane pomiarowe). W treningu nadpisywany parametrem 'demand_scale'.")]
    [SerializeField, Min(0f)] private float demandScale = 1f;
    [Tooltip("0 = losowo. Inna wartość = powtarzalne przebiegi (ziarno + numer epizodu).")]
    [SerializeField] private int randomSeed = 0;

    private bool isPaused = false;
    private bool isSpeeded = false;
    private float simulationTime = 0f;
    private const float realToSimRatio = 1f;
    public int choosedHour;
    private bool hasStartedStatistics = false;
    private bool hasWrittenHourKpi = false;
    private int completedTrainingEpisodes;
    private float currentDemandScale = 1f;
    private MainTrafficController mainController;

    public float SimulationTime => simulationTime;
    public bool RepeatHourForMlTraining => repeatHourForMlTraining;
    public bool IsEpisodeOver => repeatHourForMlTraining && simulationTime >= CurrentEpisodeLength;

    /// <summary>Długość epizodu; w treningu można ją nadpisać parametrem środowiska 'episode_length' [s].</summary>
    private float CurrentEpisodeLength => Academy.IsInitialized
        ? Mathf.Max(120f, Academy.Instance.EnvironmentParameters.GetWithDefault("episode_length", episodeLengthSeconds))
        : episodeLengthSeconds;

    /// <summary>
    /// W treningu losujemy godzinę (parametr 'hour_index', 0..liczba godzin) – jeden model dla wszystkich
    /// profili ruchu zamiast przeuczenia na jedną godzinę.
    /// </summary>
    private void PickTrainingHour()
    {
        if (!Academy.IsInitialized || hours == null || hours.Count == 0) return;
        float value = Academy.Instance.EnvironmentParameters.GetWithDefault("hour_index", -1f);
        if (value < 0f) return;
        int index = Mathf.Clamp(Mathf.FloorToInt(value), 0, hours.Count - 1);
        choosedHour = int.Parse(hours[index].Substring(0, 2));
    }
    public float CurrentDemandScale => currentDemandScale;

    /// <summary>
    /// Gdy podłączony jest trener ML-Agents, to on ustala Time.timeScale (np. 20x).
    /// Wcześniej TimeManager nadpisywał go na 1x przy starcie i po każdym epizodzie.
    /// </summary>
    public static bool TrainerControlsTime => Academy.IsInitialized && Academy.Instance.IsCommunicatorOn;

    private void Start()
    {
        mainController = FindObjectOfType<MainTrafficController>();
        if (randomSeed != 0) Random.InitState(randomSeed);
        SetTimeScale(realToSimRatio);
        OnHourChanged(0); // Ustawienie godziny
        dropDownHour.AddOptions(hours);
        dropDownHour.value = 0;
        dropDownHour.RefreshShownValue();
        dropDownHour.onValueChanged.AddListener(OnHourChanged);
    }

    private void OnHourChanged(int index)
    {
        string hourString = hours[index]; // np. "07:00"
        choosedHour = int.Parse(hourString.Substring(0, 2)); // 7
        ResetSimulation();
        ResetAllSensors();
        ResetTrafficLights();
        ResetStatistics();
        AssignDemandForChosenHour();
    }

    private void AssignDemandForChosenHour()
    {
        currentDemandScale = demandScale;
        if (Academy.IsInitialized)
            currentDemandScale *= Academy.Instance.EnvironmentParameters.GetWithDefault("demand_scale", 1f);

        foreach (var so in countOfVehiclesManager.countOfVehiclesToSpawn)
        {
            if (so.ChoosedHour.Hour == choosedHour)
                countOfVehiclesManager.AssignVehicleCountsToSpawners(so, currentDemandScale);
        }
    }

    private void Update()
    {
        simulationTime += Time.deltaTime * realToSimRatio;
        timeText.text = GetFormattedSimulationTime();

        if (!TrainerControlsTime)
        {
            if (Input.GetKeyDown(KeyCode.P)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Q)) ToggleSpeedUp();
        }

        if (simulationTime >= StatisticsWarmupSeconds && !hasStartedStatistics)
        {
            hasStartedStatistics = true;
            ResetStatistics(); // Rozpocznij zbieranie statystyk
            SimulationKpi.BeginMeasurement();
        }

        if (repeatHourForMlTraining)
        {
            // W trybie ML epizod kończy sterownik (na granicy decyzji, z nagrodą końcową).
            bool controllerHandlesEpisode = mainController != null && mainController.HandlesEpisodeEnd;
            if (IsEpisodeOver && !controllerHandlesEpisode)
                FinishEpisodeAndRestart();
        }
        else if (simulationTime >= 3620f && !hasWrittenHourKpi) // Tryb zwykły kończy się po godzinie.
        {
            hasWrittenHourKpi = true;
            WriteKpiRow();
            if (TrainerControlsTime)
                Debug.LogWarning("Trening ML bez zaznaczonego 'Repeat Hour For Ml Training' – po godzinie spawnery są puste " +
                                 "i epizod się nie kończy. Zaznacz tę opcję w TimeManager.");
            PauseGame();
        }
    }

    private void ResetTrafficLights()
    {
        TraficController trafficController = FindObjectOfType<TraficController>();
        if (trafficController != null)
            trafficController.ResetTrafficCycle();
    }

    private void ResetStatistics()
    {
        StatisticManagerForSczanieckiej statisticManager = FindObjectOfType<StatisticManagerForSczanieckiej>();
        if (statisticManager != null)
            statisticManager.ResetStatistics();
    }

    private void ResetSimulation()
    {
        simulationTime = 0f;
        hasStartedStatistics = false;
        hasWrittenHourKpi = false;
        SimulationKpi.StopMeasurement();
        foreach (Counter counter in Counters) counter.ResetCounter();
        foreach (GameObject car in GameObject.FindGameObjectsWithTag("Car")) Destroy(car);
        foreach (GameObject bus in GameObject.FindGameObjectsWithTag("Bus")) Destroy(bus);
        foreach (VehicleSpawner spawner in countOfVehiclesManager.spawners) spawner.ResetSpawner();
        foreach (VehicleSpawner tramSpawner in countOfVehiclesManager.tramSpawners) tramSpawner.ResetSpawner();
    }

    /// <summary>
    /// Kończy epizod (zapisuje KPI), resetuje symulację tej samej godziny i restartuje sterownik
    /// razem z fazą wypełniania – każdy epizod zaczyna się w tych samych warunkach.
    /// </summary>
    public void FinishEpisodeAndRestart()
    {
        WriteKpiRow();
        completedTrainingEpisodes++;
        if (randomSeed != 0) Random.InitState(randomSeed + completedTrainingEpisodes);

        ResetSimulation();
        ResetAllSensors();
        ResetTrafficLights();
        ResetStatistics();
        ExitTrigger.ResetTotalVehiclesExited();
        PickTrainingHour();
        AssignDemandForChosenHour();
        countOfVehiclesManager.RestartFillingPhase();

        if (!TrainerControlsTime)
        {
            isPaused = false;
            SetTimeScale(CurrentSpeedUp);
        }

        if (mainController != null)
            mainController.RestartAfterEpisodeReset();

        Debug.Log($"[Epizod] Zakończono epizod {completedTrainingEpisodes}; godzina {choosedHour:D2}:00, " +
                  $"natężenie x{currentDemandScale:F2}.");
    }

    private void WriteKpiRow()
    {
        string system = mainController != null ? mainController.SystemLabel : "unknown";
        string mode = mainController != null ? mainController.ModeLabel : "unknown";
        SimulationKpi.WriteRow(system, mode, choosedHour, currentDemandScale, randomSeed, completedTrainingEpisodes);
    }

    private void ResetAllSensors()
    {
        foreach (Sensor sensor in FindObjectsOfType<Sensor>())
            sensor.ResetSensor();
    }

    private void SetTimeScale(float value)
    {
        if (TrainerControlsTime) return;
        Time.timeScale = value;
    }

    // Q przełącza cyklicznie: 1x -> 5x -> 10x -> 1x (ruch pojazdów liczony w stałym kroku fizyki,
    // więc wynik nie zależy od przyspieszenia).
    private static readonly float[] SpeedSteps = { 1f, 5f, 10f };
    private int speedStepIndex;

    private float CurrentSpeedUp => SpeedSteps[speedStepIndex];

    private void ToggleSpeedUp()
    {
        speedStepIndex = (speedStepIndex + 1) % SpeedSteps.Length;
        isSpeeded = speedStepIndex > 0;
        // Więcej kroków fizyki na klatkę przy dużym przyspieszeniu, żeby symulacja nie "gubiła" czasu.
        Time.maximumDeltaTime = isSpeeded ? 0.5f : 0.3333f;
        if (!isPaused) SetTimeScale(CurrentSpeedUp);
        Debug.Log($"[Czas] Przyspieszenie symulacji: x{CurrentSpeedUp:F0}");
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        SetTimeScale(0f);
        isPaused = true;
    }

    private void ResumeGame()
    {
        SetTimeScale(CurrentSpeedUp);
        isPaused = false;
    }

    private string GetFormattedSimulationTime()
    {
        int elapsedSeconds = Mathf.FloorToInt(simulationTime);
        int currentHour = choosedHour + (elapsedSeconds / 3600);
        int currentMinute = (elapsedSeconds % 3600) / 60;
        int currentSecond = elapsedSeconds % 60;
        if (currentHour >= 24) currentHour %= 24;
        return $"{currentHour:D2}:{currentMinute:D2}:{currentSecond:D2}";
    }

    public int GetChoosedHour() => choosedHour;
}
