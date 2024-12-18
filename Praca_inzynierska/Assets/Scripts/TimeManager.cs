using System;
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

    private bool isPaused = false; // Game pause flag
    private bool isSpeeded = false;

    private void Start()
    {
        // Initialize dictionary for tracking vehicle counts for each spawner
        foreach (var spawner in spawners)
        {
            countForVehicles[spawner] = 0;
        }
    }

    private void Update()
    {
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

    public void ChoosedDay()
    {
        ResetSimulation();
        // Get selected day
        int dayChoosed = dropDownDay.value;
        string dayChoosedString = dropDownDay.options[dayChoosed].text;
        print(dayChoosedString);

        // Set VehicleCountToSpawn based on selected day
        SetVehicleCountsForDay(dayChoosedString);

        // Update spawner configurations
        for (int i = 0; i < spawners.Count; i++)
        {
            // Assign max vehicles directly
            spawners[i].maxVehicles = VehicleCountToSpawn[i];

            // Update dictionary for tracking
            countForVehicles[spawners[i]] = VehicleCountToSpawn[i];
        }
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

    private void SetVehicleCountsForDay(string day)
    {
        // Assign vehicle counts based on the selected day
        switch (day)
        {
            case "Monday":
                SetVehicleCounts(2, 2, 3, 3);
                break;
            case "Tuesday":
                SetVehicleCounts(3, 3, 4, 4);
                break;
            case "Wednesday":
                SetVehicleCounts(4, 4, 5, 5);
                break;
            case "Thursday":
                SetVehicleCounts(5, 5, 6, 6);
                break;
            case "Friday":
                SetVehicleCounts(7, 7, 8, 8);
                break;
            case "Saturday":
                SetVehicleCounts(8, 8, 9, 9);
                break;
            case "Sunday":
                SetVehicleCounts(9, 9, 10, 10);
                break;
        }
    }

    private void SetVehicleCounts(params int[] counts)
    {
        // Ensure VehicleCountToSpawn matches the provided counts
        for (int i = 0; i < counts.Length && i < VehicleCountToSpawn.Length; i++)
        {
            VehicleCountToSpawn[i] = counts[i];
        }
    }

    public void ChoosedHour()
    {
        // Get selected hour (logic can be added as needed)
        int hourChoosed = dropDownHour.value;
        string hourChoosedString = dropDownHour.options[hourChoosed].text;
        print(hourChoosedString);
        ResetSimulation();
    }

    private void SpeedUpSimulation()
    {
        Time.timeScale = 3f; // Speed up time
        isSpeeded = true;
    }

    private void StopSpeedUpSimulation()
    {
        Time.timeScale = 1f; // Restore normal time
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
        Time.timeScale = 1f; // Resume time
        isPaused = false;
    }
}
