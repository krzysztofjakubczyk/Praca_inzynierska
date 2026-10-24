using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NowyScenariusz", menuName = "ScriptableObjects/SimulationScenario", order = 1)]
public class SimulationScenarioSO : ScriptableObject
{
    [Header("Ustawienia Scenariusza")]
    public string scenarioName; // Nazwa w dropdownie (np. "Dokument Sczanieckiej - Logika rozmyta")
    public bool useFuzzyLogic;  // Czy sterownik ma u¿ywaæ Logiki Rozmytej

    [Header("Dane o natê¿eniu ruchu")]
    public List<VehicleCountSO> vehicleCountsByHour; // Lista plików VehicleCountSO dla poszczególnych godzin
}