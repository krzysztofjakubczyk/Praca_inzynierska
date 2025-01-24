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
    [SerializeField] private int[] VehicleCountToSpawn;
    [SerializeField] private TMP_Text timeText;

    private bool isPaused = false; // Game pause flag
    private bool isSpeeded = false;
    private float simulationTime = 0f; // Time in seconds for simulation
    private const float realToSimRatio = 1f; // 15 seconds in simulation = 1 second in real-time
    int choosedHour;
    int totalVehicles;
    float hourFactor;
    int indexOfHour = 0;
    private void Start()
    {
        // Set initial simulation speed
        Time.timeScale = realToSimRatio;
        ChoosedDateAndHour();
        foreach(var tramSpawner in tramSpawners)
        {
            tramSpawner.MaxVehicles = 1;
            tramSpawner.SetSpawnInterval(720);
        }
    }

    private void Update()
    {
        // Aktualizuj czas symulacji
        simulationTime += Time.deltaTime * realToSimRatio;

        // Oblicz dynamicznie zmieniaj¹c¹ siê godzinê symulacji
        string currentSimulationTime = GetFormattedSimulationTime();

        // Wyœwietl dynamiczny czas symulacji na ekranie
        timeText.text = currentSimulationTime;

        // Pauzowanie gry
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }

        // Przyspieszanie symulacji
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
        indexOfHour = dropDownHour.value;
        string hourChoosedString = dropDownHour.options[indexOfHour].text;

        switch (indexOfHour)
        {
            case 0:
                choosedHour = 7;
                totalVehicles = 3639;
                hourFactor = 1.2f;

                break;
            case 1:
                choosedHour = 12;
                totalVehicles = 2719;
                hourFactor = 0.9f;
                break;
            case 2:
                choosedHour = 15;
                totalVehicles = 3733;
                hourFactor = 1.08f;
                break;

            case 3:
                choosedHour = 21;
                totalVehicles = 1173;
                hourFactor = 0.72f;
                break;
        }

        for (int i = 0; i < spawners.Count; i++)
        {
            // Oblicz liczbê pojazdów na sekundê
            float vehiclesPerSecond = (totalVehicles / 3600f)/4; // 3600 sekund w godzinie
                                                             // Interwa³ spawnowania w sekundach (1 podzielone przez pojazdy na sekundê)
            float spawnInterval = 1f / vehiclesPerSecond;

            //print($"Total vehicles for hour {choosedHour}: {totalVehicles}");
            spawners[i].SetSpawnInterval(spawnInterval); // Ustaw interwa³ dla ka¿dego spawnera
            spawners[i].MaxVehicles = Mathf.CeilToInt(totalVehicles / 4f);

            //Debug.Log($"Spawner {spawners[i].name}: {vehiclesPerSecond:F2} pojazdów/sek., interwa³: {spawnInterval:F2} s.");
        }

        print($"Wybrany dzieñ: {dayChoosedString}, Wybrana godzina: {hourChoosedString}");
    }

    private void ResetSimulation()
    {
        GameObject[] Cars = GameObject.FindGameObjectsWithTag("Car");
        VehicleSpawner[] Spawners = VehicleSpawner.FindObjectsOfType<VehicleSpawner>();
        foreach (GameObject Car in Cars)
        {
            Destroy(Car, 0.5f);
        }
        foreach (VehicleSpawner Spawner in Spawners)
        {
            Spawner.ResetSpawner();
        }
        //print("reset simulation");
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

    private string GetFormattedSimulationTime()
    {
        // Oblicz aktualny czas w symulacji na podstawie wybranej godziny i postêpu symulacji
        int baseHour = choosedHour; // Wybrana godzina
        int baseMinute = 0;         // Minuty startowe (domyœlnie 0)

        // Dodaj czas symulacji (w sekundach)
        int elapsedSeconds = Mathf.FloorToInt(simulationTime);
        int currentHour = baseHour + (elapsedSeconds / 3600); // Dodaj pe³ne godziny
        int currentMinute = baseMinute + ((elapsedSeconds % 3600) / 60); // Dodaj minuty
        int currentSecond = elapsedSeconds % 60; // Oblicz sekundy

        // Jeœli godzina przekracza 23, zawijamy do 0
        if (currentHour >= 24)
        {
            currentHour -= 24;
        }

        // Sformatuj godzinê
        return $"{currentHour:D2}:{currentMinute:D2}:{currentSecond:D2}";
    }
}
