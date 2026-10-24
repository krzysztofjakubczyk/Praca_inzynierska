using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenManager : MonoBehaviour
{
    public static StartScreenManager Instance { get; private set; }

    public Button startButton;
    public TMP_Dropdown simulationModeDropdown;

    [Header("Lista wszystkich scenariuszy (Pliki SO)")]
    [SerializeField] private List<SimulationScenarioSO> scenarios;

    [Header("Nazwa g³ównej sceny symulacji")]
    [SerializeField] private string mainSceneName = "Sczanieckiej_Main";

    // Wybrany scenariusz, do którego odwo³a siê nowa scena po za³adowaniu
    public static SimulationScenarioSO SelectedScenario { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        PopulateDropdown();
        startButton.onClick.AddListener(StartSimulation);
    }

    private void PopulateDropdown()
    {
        simulationModeDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var scenario in scenarios)
        {
            if (scenario != null)
                options.Add(scenario.scenarioName);
        }

        simulationModeDropdown.AddOptions(options);
        simulationModeDropdown.value = 0;
    }

    private void StartSimulation()
    {
        int selectedIndex = simulationModeDropdown.value;

        if (selectedIndex >= 0 && selectedIndex < scenarios.Count)
        {
            SelectedScenario = scenarios[selectedIndex];
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogWarning("Wybrano nieprawid³owy scenariusz z listy!");
        }
    }
}