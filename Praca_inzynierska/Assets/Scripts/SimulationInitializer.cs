using UnityEngine;
using System.Collections;

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

        if (scenario.roadClosureEvent != null && scenario.roadClosureEvent.enabled)
        {
            StartCoroutine(ManageRoadClosureEvent(scenario.roadClosureEvent));
        }
    }
    private IEnumerator ManageRoadClosureEvent(RoadClosureEventSettings eventSettings)
    {
        TimeManager timeManager = null;
        while (timeManager == null)
        {
            timeManager = FindObjectOfType<TimeManager>();
            yield return null;
        }

        bool eventWasActive = false;
        float eventEndTime = eventSettings.startTimeSeconds + eventSettings.durationSeconds;
        while (true)
        {
            float simulationTime = timeManager.SimulationTime;
            bool eventIsActive = eventSettings.durationSeconds > 0f
                && simulationTime >= eventSettings.startTimeSeconds
                && simulationTime < eventEndTime;

            if (eventIsActive != eventWasActive)
            {
                if (countOfVehiclesManager != null)
                {
                    // Spawner indices 0 and 1 correspond to lanes 1 and 2.
                    countOfVehiclesManager.SetStraightClosureForLaneIds(new[] { 1, 2 }, eventIsActive);
                }

                Debug.Log(eventIsActive
                    ? "Zdarzenie rozpoczęte: zamknięto jazdę na wprost z Plater od Firlika; objazd prowadzi w lewo."
                    : "Zdarzenie zakończone: przywrócono normalny wybór trasy.");
                eventWasActive = eventIsActive;
            }

            yield return null;
        }
    }
}
