using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropDownHour;
    [SerializeField] private List<VehicleSpawner> spawners;
    [SerializeField] private List<VehicleSpawner> tramSpawners;
    [SerializeField] private List<VehicleCountSO> countOfVehiclesToSpawn;
    [SerializeField] private List<Counter> Counters;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private int simulationSpeedUpValue;
    [SerializeField] public bool isAfternoonPeakOnly; // Czy symulacja działa tylko w szczycie popołudniowym?

    private VehicleCountSO currentSO;
    private bool isPaused = false;
    private bool isSpeeded = false;
    private float simulationTime = 0f;
    private const float realToSimRatio = 1f;
    private int choosedHour;
    private bool isFillingPhase = true;
    public float fillingPhaseDuration = 15f; // 20 sekund wypełniania
    private float spawnMultiplier = 4f; // 
    private bool hasStartedStatistics = false;

    private void Start()
    {

        Time.timeScale = realToSimRatio;
        ChoosedDateAndHour(); // Ustawienie godziny

        foreach (var tramSpawner in tramSpawners)
        {
            tramSpawner.MaxVehicles = 5;
            tramSpawner.SetSpawnInterval(720); // Tramwaje co 12 minut
        }
        StartCoroutine(ManageFillingPhase());

    }

    private void Update()
    {
        simulationTime += Time.deltaTime * realToSimRatio;
        timeText.text = GetFormattedSimulationTime();

        if (Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Q)) ToggleSpeedUp();
        if (simulationTime >= 3600f) // Jeśli upłynęła cała godzina lub 12 minut i 20 sekund, zatrzymaj symulację
        {
            PauseGame();
        }
        if (simulationTime >= 380f && !hasStartedStatistics) // Po 6 minutach i 20 sekundach
        {
            hasStartedStatistics = true;
            ResetStatistics(); // Rozpocznij zbieranie statystyk
        }
    }

    private IEnumerator ManageFillingPhase()
    {

        foreach (VehicleSpawner spawner in spawners)
        {
            spawner.SetSpawnInterval(spawner.GetSpawnInterval() / spawnMultiplier);
        }

        yield return new WaitForSeconds(fillingPhaseDuration); // Czekamy 20 sekund


        foreach (VehicleSpawner spawner in spawners)
        {
            spawner.SetSpawnInterval(spawner.GetSpawnInterval() * spawnMultiplier);
        }

        isFillingPhase = false;
    }


    public void ChoosedDateAndHour()
    {
        ResetSimulation();
        ResetAllSensors();
        ResetTrafficLights(); // Reset świateł
        ResetStatistics(); // Reset statystyk
        choosedHour = 7;
        foreach(var so in countOfVehiclesToSpawn)
        {
            if(so.ChoosedHour.Hour == choosedHour)
            {
                AssignVehicleCountsToSpawners(so);
            }
        }

    }
    private void ResetTrafficLights()
    {
        TraficController trafficController = FindObjectOfType<TraficController>();

        if (trafficController != null)
        {
            trafficController.ResetTrafficCycle();
        }

    }
    private void AssignVehicleCountsToSpawners(VehicleCountSO vehicleCounts)
    {

        for (int i = 0; i < spawners.Count; i++)
        {
            float vehiclesPerSecond = (float)vehicleCounts.CountOfVehicles[i] / 3600f;
            float spawnInterval = 1f / vehiclesPerSecond;

            spawners[i].SetSpawnInterval(spawnInterval);
            spawners[i].MaxVehicles = vehicleCounts.CountOfVehicles[i];
        }
    }

private void ResetStatistics()
{
    StatisticManagerForSczanieckiej statisticManager = FindObjectOfType<StatisticManagerForSczanieckiej>();

    if (statisticManager != null)
    {
        statisticManager.ResetStatistics();
    }

}

private void ResetSimulation()
{
    simulationTime = 0f;
    foreach (Counter counter in Counters) counter.ResetCounter();
    foreach (GameObject car in GameObject.FindGameObjectsWithTag("Car")) Destroy(car, 0.5f);
    foreach (VehicleSpawner spawner in spawners) spawner.ResetSpawner();
    foreach (VehicleSpawner tramSpawner in tramSpawners) tramSpawner.ResetSpawner();
}
private void ResetAllSensors()
{
    Sensor[] sensors = FindObjectsOfType<Sensor>();

    foreach (Sensor sensor in sensors)
    {
        sensor.ResetSensor();
    }
}

private void ToggleSpeedUp()
{
    if (isSpeeded) StopSpeedUpSimulation();
    else SpeedUpSimulation();
}

private void SpeedUpSimulation()
{
    Time.timeScale = simulationSpeedUpValue;
    isSpeeded = true;
}

private void StopSpeedUpSimulation()
{
    Time.timeScale = realToSimRatio;
    isSpeeded = false;
}

private void TogglePause()
{
    if (isPaused) ResumeGame();
    else PauseGame();
}

private void PauseGame()
{
    Time.timeScale = 0f;
    isPaused = true;
}

private void ResumeGame()
{
    Time.timeScale = realToSimRatio;
    isPaused = false;
}

private string GetFormattedSimulationTime()
{
    int baseHour = choosedHour;
    int elapsedSeconds = Mathf.FloorToInt(simulationTime);
    int currentHour = baseHour + (elapsedSeconds / 3600);
    int currentMinute = (elapsedSeconds % 3600) / 60;
    int currentSecond = elapsedSeconds % 60;

    if (currentHour >= 24) currentHour %= 24;

    return $"{currentHour:D2}:{currentMinute:D2}:{currentSecond:D2}";
}

public int GetChoosedHour()
{
    return choosedHour;
}
}
