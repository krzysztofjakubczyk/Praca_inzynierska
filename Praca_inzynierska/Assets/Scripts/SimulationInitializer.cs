using UnityEngine;

public class SimulationInitializer : MonoBehaviour
{
    [SerializeField] private TraficController traficController; // stary sterownik (nieużywany w scenach)
    [SerializeField] private MainTrafficController mainTrafficController;
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

        if (traficController != null)
            traficController.useFuzzyLogic = scenario.useFuzzyLogic;

        // Wcześniej flaga trafiała tylko do starego TraficController, którego nie ma w scenach,
        // więc wybór "Logika rozmyta" w menu nic nie zmieniał. Teraz ustawiamy właściwy sterownik.
        if (mainTrafficController == null)
            mainTrafficController = FindObjectOfType<MainTrafficController>();

        if (mainTrafficController != null && TryGetSystemType(scenario, out systemType type))
            mainTrafficController.SetSystemType(type);

        if (countOfVehiclesManager != null && scenario.vehicleCountsByHour != null)
            countOfVehiclesManager.countOfVehiclesToSpawn = scenario.vehicleCountsByHour;

        Debug.Log($"Załadowano scenariusz: {scenario.scenarioName} | sterowanie: {scenario.controlSystem} | Fuzzy Logic: {scenario.useFuzzyLogic}");

        RoadClosureRuntime.Configure(scenario.roadClosureEvent);

        if (scenario.roadClosureEvent != null && scenario.roadClosureEvent.enabled)
        {
            Debug.Log($"Zdarzenie drogowe skonfigurowane: start {scenario.roadClosureEvent.startTimeSeconds}s, " +
                      $"czas trwania {scenario.roadClosureEvent.durationSeconds}s.");
        }
    }

    private static bool TryGetSystemType(SimulationScenarioSO scenario, out systemType type)
    {
        switch (scenario.controlSystem)
        {
            case ScenarioControlSystem.FromUseFuzzyLogicFlag:
                type = scenario.useFuzzyLogic ? systemType.fuzzySystem : systemType.staticSystem;
                return true;
            case ScenarioControlSystem.Static: type = systemType.staticSystem; return true;
            case ScenarioControlSystem.Fuzzy: type = systemType.fuzzySystem; return true;
            case ScenarioControlSystem.MachineLearning: type = systemType.machineLearning; return true;
            case ScenarioControlSystem.Actuated: type = systemType.actuatedSystem; return true;
            default: type = systemType.staticSystem; return false;
        }
    }
}
