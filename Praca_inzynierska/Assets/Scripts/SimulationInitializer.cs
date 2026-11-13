using UnityEngine;

public class SimulationInitializer : MonoBehaviour
{
    [SerializeField] private TraficController traficController;
    [SerializeField] private CountOfVehiclesManager countOfVehiclesManager;

    private void Awake()
    {
        RoadClosureRuntime.Reset();
        SimulationScenarioSO scenario = StartScreenManager.SelectedScenario;

        if (scenario == null)
        {
            Debug.LogWarning("Uruchomiono scenę bez wyboru z menu. Używam ustawień zapisanych w scenie.");
            return;
        }

        // 1. Ustawienie flage Fuzzy Logic w sterowniku świateł
        if (traficController != null)
        {
            traficController.useFuzzyLogic = scenario.useFuzzyLogic;
        }

        // 2. Podpięcie listy SO z natężeniem pojazdów dla poszczególnych godzin
        if (countOfVehiclesManager != null && scenario.vehicleCountsByHour != null)
        {
            countOfVehiclesManager.countOfVehiclesToSpawn = scenario.vehicleCountsByHour;
        }

        Debug.Log($"Załadowano scenariusz: {scenario.scenarioName} | Fuzzy Logic: {scenario.useFuzzyLogic}");

        RoadClosureRuntime.Configure(scenario.roadClosureEvent);

        if (scenario.roadClosureEvent != null && scenario.roadClosureEvent.enabled)
        {
            Debug.Log($"Zdarzenie drogowe skonfigurowane: start {scenario.roadClosureEvent.startTimeSeconds}s, " +
                      $"czas trwania {scenario.roadClosureEvent.durationSeconds}s.");
        }
    }
}
