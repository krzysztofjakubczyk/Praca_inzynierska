using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountOfVehiclesManager : MonoBehaviour
{
    [SerializeField] public List<VehicleSpawner> spawners;
    [SerializeField] public List<VehicleSpawner> tramSpawners;
    [SerializeField] private bool useTrams = false;
    [SerializeField] public List<VehicleCountSO> countOfVehiclesToSpawn;
    public int waitingTIme = 5;
    [SerializeField] private float spawnMultiplier = 4f;
    public float fillingPhaseDuration = 15f;

    private void Awake()
    {
        for (int i = 0; i < spawners.Count; i++)
        {
            if (spawners[i] != null)
                spawners[i].SetTrafficLaneId(i + 1);
        }
    }

    private void Start()
    {
        if (useTrams)
        {
            foreach (var tramSpawner in tramSpawners)
            {
                tramSpawner.MaxVehicles = 5;
                tramSpawner.SetSpawnInterval(720); // Tramwaje co 12 minut
            }
        }

        StartCoroutine(ManageFillingPhase());
    }

    /// <summary>Ponownie uruchamia fazę wypełniania (np. na początku nowego epizodu treningowego).</summary>
    public void RestartFillingPhase()
    {
        StopAllCoroutines();
        SetMultiplierForAll(1f);
        StartCoroutine(ManageFillingPhase());
    }

    // Faza wypełniania używa mnożnika, a nie nadpisuje interwału – zmiana godziny w trakcie
    // wypełniania nie psuje już natężenia (wcześniej interwał był mnożony x4 na nowej wartości).
    private IEnumerator ManageFillingPhase()
    {
        yield return new WaitForSeconds(waitingTIme);
        SetMultiplierForAll(1f / Mathf.Max(0.01f, spawnMultiplier));
        yield return new WaitForSeconds(fillingPhaseDuration);
        SetMultiplierForAll(1f);
    }

    private void SetMultiplierForAll(float multiplier)
    {
        foreach (VehicleSpawner spawner in spawners)
        {
            if (spawner != null) spawner.SetIntervalMultiplier(multiplier);
        }
    }

    /// <param name="demandScale">Mnożnik natężenia (1 = dane z pomiarów). Używany do losowania popytu w treningu.</param>
    public void AssignVehicleCountsToSpawners(VehicleCountSO vehicleCounts, float demandScale = 1f)
    {
        for (int i = 0; i < spawners.Count; i++)
        {
            if (spawners[i] == null) continue;
            int baseCount = i < vehicleCounts.CountOfVehicles.Count ? vehicleCounts.CountOfVehicles[i] : 0;
            int count = Mathf.Max(0, Mathf.RoundToInt(baseCount * demandScale));

            spawners[i].MaxVehicles = count;
            spawners[i].SetSpawnInterval(count > 0 ? 3600f / count : float.PositiveInfinity);
        }
    }
}
