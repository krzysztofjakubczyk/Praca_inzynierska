using UnityEngine;

public class SimulationInitializer : MonoBehaviour
{
    [SerializeField] private TraficController traficController;
    [SerializeField] private CountOfVehiclesManager countOfVehiclesManager;

    private void Awake()
    {
        SimulationScenarioSO scenario = StartScreenManager.SelectedScenario;

        if (scenario == null)
        {
            Debug.LogWarning("⚠️ Uruchomiono scenę bez wyboru z Menu! Używam domyślnych ustawień w scenie.");
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

        Debug.Log($"✅ Pomyślnie załadowano scenariusz: {scenario.scenarioName} | Fuzzy Logic: {scenario.useFuzzyLogic}");
    }
}