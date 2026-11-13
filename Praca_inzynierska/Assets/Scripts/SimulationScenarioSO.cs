using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadClosureEventSettings
{
    public bool enabled;
    [Min(0f)] public float startTimeSeconds;
    [Min(0f)] public float durationSeconds;

}

[CreateAssetMenu(fileName = "NowyScenariusz", menuName = "ScriptableObjects/SimulationScenario", order = 1)]
public class SimulationScenarioSO : ScriptableObject
{
    [Header("Ustawienia Scenariusza")]
    public string scenarioName; // Nazwa w dropdownie (np. "Dokument Sczanieckiej - Logika rozmyta")
    public bool useFuzzyLogic;  // Czy sterownik ma używać Logiki Rozmytej

    [Header("Dane o natężeniu ruchu")]
    public List<VehicleCountSO> vehicleCountsByHour; // Lista plików VehicleCountSO dla poszczególnych godzin

    [Header("Zdarzenie drogowe")]
    public RoadClosureEventSettings roadClosureEvent = new RoadClosureEventSettings();

    private void OnEnable()
    {
        if (roadClosureEvent == null)
        {
            roadClosureEvent = new RoadClosureEventSettings();
        }
    }
}
