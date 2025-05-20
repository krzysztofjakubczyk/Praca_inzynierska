using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropDownHour;
    [SerializeField] private List<Counter> Counters;
    [SerializeField] private List<string> hours;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private int simulationSpeedUpValue;
    [SerializeField] public bool isAfternoonPeakOnly; // Czy symulacja działa tylko w szczycie popołudniowym?

    private bool isPaused = false;
    private bool isSpeeded = false;
    private float simulationTime = 0f;
    private const float realToSimRatio = 1f;
    public int choosedHour;
    private bool hasStartedStatistics = false;
    [SerializeField]private CountOfVehiclesManager countOfVehiclesManager;
    private void Start()
    {
        Time.timeScale = realToSimRatio;
        OnHourChanged(0); // Ustawienie godziny
        dropDownHour.AddOptions(hours);
        dropDownHour.value = 0;
        dropDownHour.RefreshShownValue();
        dropDownHour.onValueChanged.AddListener(OnHourChanged);
    }

    private void OnHourChanged(int index)
    {
        choosedHour = index;
        string hourString = hours[index]; // np. "07:00"
        choosedHour = int.Parse(hourString.Substring(0, 2)); // 7
        ResetSimulation();
        ResetAllSensors();
        ResetTrafficLights(); // Reset świateł
        ResetStatistics(); // Reset statystyk
        foreach (var so in countOfVehiclesManager.countOfVehiclesToSpawn)
        {
            if (so.ChoosedHour.Hour == choosedHour)
            {
                countOfVehiclesManager.AssignVehicleCountsToSpawners(so);
            }
        }
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

    private void ResetTrafficLights()
    {
        TraficController trafficController = FindObjectOfType<TraficController>();

        if (trafficController != null)
        {
            trafficController.ResetTrafficCycle();
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
        foreach (GameObject bus in GameObject.FindGameObjectsWithTag("Bus")) Destroy(bus, 0.5f);
        foreach (VehicleSpawner spawner in countOfVehiclesManager.spawners) spawner.ResetSpawner();
        foreach (VehicleSpawner tramSpawner in countOfVehiclesManager.tramSpawners) tramSpawner.ResetSpawner();
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
