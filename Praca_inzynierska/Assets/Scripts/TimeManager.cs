using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropDownDay;
    [SerializeField] private TMP_Dropdown dropDownHour;
    [SerializeField] private List<VehicleSpawner> spawners;
    [SerializeField] private Dictionary<VehicleSpawner, int> countForVehicles = new Dictionary<VehicleSpawner, int>();
    [SerializeField] private int[] VehicleCountToSpawn;
    [SerializeField] private TMP_Text timeText;

    private bool isPaused = false; // Game pause flag
    private bool isSpeeded = false;
    private float simulationTime = 0f; // Time in seconds for simulation
    private const float realToSimRatio = 1f; // 15 seconds in simulation = 1 second in real-time

    private void Start()
    {
        // Initialize dictionary for tracking vehicle counts for each spawner
        foreach (var spawner in spawners)
        {
            countForVehicles[spawner] = 0;
        }

        // Set initial simulation speed
        Time.timeScale = realToSimRatio;
    }

    private void Update()
    {
        // Update simulation time
        simulationTime += Time.deltaTime * realToSimRatio;
        timeText.text = FormatSimulationTime(simulationTime);

        // Toggle pause on key press
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }

        // Toggle speed-up on key press
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleSpeedUp();
        }
    }

    private void ToggleSpeedUp()
    {
        if (isSpeeded)
        {
            StopSpeedUpSimulation();
        }
        else
        {
            SpeedUpSimulation();
        }
    }

    public void ChoosedDateAndHour()
    {
        ResetSimulation();

        // Get selected day
        int dayChoosed = dropDownDay.value;
        string dayChoosedString = dropDownDay.options[dayChoosed].text;

        // Get selected hour
        int indexOfHour = dropDownHour.value;
        int choosedHour = 7;
        switch (indexOfHour)
        {
            case 0:
                choosedHour = 7;
                break;
            case 1:
                choosedHour = 12;
                break;
            case 2:
                choosedHour = 15;
                break;
            case 3:
                choosedHour = 21;
                break;
        }
        string hourChoosedString = dropDownHour.options[indexOfHour].text;
        // Adjust vehicle counts based on the selected day and hour
        AdjustVehicleCountsForDayAndHour(dayChoosedString, indexOfHour);

        // Define vehicle counts per intersection
        int[] totalVehicles = { 11879, 7629, 2599, 3924 };

        // Define the hour factors (calculated earlier)
        float[] hourFactors = { 1.2f, 0.9f, 1.08f, 0.72f }; // For 7, 12, 15, 21 hours

        for (int i = 0; i < spawners.Count; i++)
        {
            float totalFactor = hourFactors[indexOfHour];
            int vehiclesForThisSpawner = Mathf.RoundToInt(totalVehicles[i] * totalFactor / 15.6f); // Adjust by totalFactor

            // Ustaw maksymaln¹ liczbê pojazdów w spawnerze
            spawners[i].SetMaxVehicles(vehiclesForThisSpawner);
            countForVehicles[spawners[i]] = vehiclesForThisSpawner;

            // Spawner automatycznie obs³uguje spawnowanie
            Debug.Log($"Spawner {spawners[i].name} ustawiono na {vehiclesForThisSpawner} pojazdów.");
        }
        print($"Wybrany dzieñ: {dayChoosedString}, Wybrana godzina: {hourChoosedString}");
    }

    private void ResetSimulation()
    {
        GameObject[] Cars = GameObject.FindGameObjectsWithTag("Car");
        VehicleSpawner[] Spawners = VehicleSpawner.FindObjectsOfType<VehicleSpawner>();
        foreach (GameObject Car in Cars)
        {
            Destroy(Car);
        }
        foreach (VehicleSpawner Spawner in Spawners)
        {
            Spawner.ResetSpawner();
        }
        print("reset simulation");
    }

    private void AdjustVehicleCountsForDayAndHour(string day, int hour)
    {
        // Day factor
        float dayFactor = day switch
        {
            "Monday" or "Tuesday" or "Wednesday" or "Thursday" => 1.2f,
            "Friday" => 1.5f,
            "Saturday" => 0.8f,
            "Sunday" => 0.6f,
            _ => 1.0f
        };

        // Hour factor
        float hourFactor = hour switch
        {
            7 => 2.0f, // Morning rush  
            12 => 1.5f, // Lunch hours
            15 => 1.8f, // Afternoon rush
            21 => 1.2f, // Evening traffic
            _ => 1.0f
        };

        // Adjust vehicle counts using the combined factor
        float totalFactor = dayFactor * hourFactor;
        for (int i = 0; i < VehicleCountToSpawn.Length; i++)
        {
            VehicleCountToSpawn[i] = Mathf.RoundToInt(VehicleCountToSpawn[i] * totalFactor);
        }
    }

    private void SpeedUpSimulation()
    {
        Time.timeScale = 3f; // Speed up time further
        isSpeeded = true;
    }

    private void StopSpeedUpSimulation()
    {
        Time.timeScale = realToSimRatio; // Restore normal time
        isSpeeded = false;
    }

    private void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f; // Pause time
        isPaused = true;
    }

    private void ResumeGame()
    {
        Time.timeScale = realToSimRatio; // Resume time
        isPaused = false;
    }

    private string FormatSimulationTime(float timeInSeconds)
    {
        int hours = Mathf.FloorToInt(timeInSeconds / 3600);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}
