using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountOfVehiclesManager : MonoBehaviour
{
    [SerializeField] public List<VehicleSpawner> spawners;
    [SerializeField] public List<VehicleSpawner> tramSpawners;
    [SerializeField] public List<VehicleCountSO> countOfVehiclesToSpawn;
    public int waitingTIme = 5;
    private VehicleCountSO currentSO;
    [SerializeField] private float spawnMultiplier = 4f;
    public float fillingPhaseDuration = 15f;
    private void Start()
    {
        foreach (var tramSpawner in tramSpawners)
        {
            tramSpawner.MaxVehicles = 5;
            tramSpawner.SetSpawnInterval(720); // Tramwaje co 12 minut
        }
        StartCoroutine(ManageFillingPhase());
    }
    private IEnumerator ManageFillingPhase()
    {
        yield return new WaitForSeconds(waitingTIme);
        foreach (VehicleSpawner spawner in spawners)
        {
            spawner.SetSpawnInterval(spawner.GetSpawnInterval() / spawnMultiplier);
        }

        yield return new WaitForSeconds(fillingPhaseDuration); // Czekamy 20 sekund


        foreach (VehicleSpawner spawner in spawners)
        {
            spawner.SetSpawnInterval(spawner.GetSpawnInterval() * spawnMultiplier);
        }
    }
    public void AssignVehicleCountsToSpawners(VehicleCountSO vehicleCounts)
    {

        for (int i = 0; i < spawners.Count; i++)
        {
            float vehiclesPerSecond = (float)vehicleCounts.CountOfVehicles[i] / 3600f;
            float spawnInterval = 1f / vehiclesPerSecond;

            spawners[i].SetSpawnInterval(spawnInterval);
            spawners[i].MaxVehicles = vehicleCounts.CountOfVehicles[i];
        }
    }
}
