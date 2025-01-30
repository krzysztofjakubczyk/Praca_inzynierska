using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropDownDay;
    [SerializeField] private TMP_Dropdown dropDownHour;
    [SerializeField] private List<VehicleSpawner> spawners;
    [SerializeField] private List<VehicleSpawner> tramSpawners;
    [SerializeField] private List<int> countOfVehiclesForSeven;
    [SerializeField] private List<int> countOfVehiclesForTwelve;
    [SerializeField] private List<int> countOfVehiclesForFifteen;
    [SerializeField] private List<int> countOfVehiclesForNinePM;
    [SerializeField] private TMP_Text timeText;

    private bool isPaused = false; // Game pause flag
    private bool isSpeeded = false;
    private float simulationTime = 0f; // Time in seconds for simulation
    private const float realToSimRatio = 1f; // 15 seconds in simulation = 1 second in real-time
    private int choosedHour;

    private void Start()
    {
        Time.timeScale = realToSimRatio;
        ChoosedDateAndHour();

        foreach (var tramSpawner in tramSpawners)
        {
            tramSpawner.MaxVehicles = 1;
            tramSpawner.SetSpawnInterval(720); // Tramwaje co 12 minut
        }
    }

    private void Update()
    {
        simulationTime += Time.deltaTime * realToSimRatio;
        string currentSimulationTime = GetFormattedSimulationTime();
        timeText.text = currentSimulationTime;

        if (Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (Input.GetKeyDown(KeyCode.Q)) ToggleSpeedUp();
    }

    public void ChoosedDateAndHour()
    {
        ResetSimulation();

        int indexOfHour = dropDownHour.value;
        string hourChoosedString = dropDownHour.options[indexOfHour].text;

        switch (indexOfHour)
        {
            case 0:
                choosedHour = 7;
                AssignVehicleCountsToSpawners(countOfVehiclesForSeven);
                break;
            case 1:
                choosedHour = 12;
                AssignVehicleCountsToSpawners(countOfVehiclesForTwelve);
                break;
            case 2:
                choosedHour = 15;
                AssignVehicleCountsToSpawners(countOfVehiclesForFifteen);
                break;
            case 3:
                choosedHour = 21;
                AssignVehicleCountsToSpawners(countOfVehiclesForNinePM);
                break;
            default:
                Debug.LogError("Niepoprawny wybór godziny!");
                break;
        }

        Debug.Log($"Wybrano godzinê: {hourChoosedString}");
    }

    private void AssignVehicleCountsToSpawners(List<int> vehicleCounts)
    {
        if (vehicleCounts.Count != spawners.Count)
        {
            Debug.LogError("Liczba pojazdów nie odpowiada liczbie spawnerów!");
            return;
        }

        for (int i = 0; i < spawners.Count; i++)
        {
            int totalVehiclesForSpawner = vehicleCounts[i];
            float vehiclesPerSecond = (float)totalVehiclesForSpawner / 3600f; // 3600 sekund w godzinie
            float spawnInterval = 1f / vehiclesPerSecond; // Interwa³ spawnowania

            spawners[i].SetSpawnInterval(spawnInterval);
            spawners[i].MaxVehicles = totalVehiclesForSpawner;

            //Debug.Log($"Spawner {spawners[i].name}: {totalVehiclesForSpawner} pojazdów, interwa³: {spawnInterval:F2} s.");
        }
    }

    private void ResetSimulation()
    {
        simulationTime = 0f;
        GameObject[] Cars = GameObject.FindGameObjectsWithTag("Car");
        foreach (GameObject Car in Cars)
        {
            Destroy(Car, 0.5f);
        }

        foreach (VehicleSpawner Spawner in spawners)
        {
            Spawner.ResetSpawner();
        }

    }

    private void ToggleSpeedUp()
    {
        if (isSpeeded) StopSpeedUpSimulation();
        else SpeedUpSimulation();
    }

    private void SpeedUpSimulation()
    {
        Time.timeScale = 4f;
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
        int baseHour = choosedHour; // Nowa wybrana godzina
        int baseMinute = 0;
        int baseSecond = 0;

        int elapsedSeconds = Mathf.FloorToInt(simulationTime); // Czas od pocz¹tku symulacji
        int currentHour = baseHour + (elapsedSeconds / 3600);
        int currentMinute = baseMinute + ((elapsedSeconds % 3600) / 60);
        int currentSecond = baseSecond + (elapsedSeconds % 60);

        if (currentHour >= 24)
        {
            currentHour %= 24; // Reset do 00:00 po pó³nocy
        }

        return $"{currentHour:D2}:{currentMinute:D2}:{currentSecond:D2}";
    }

}
