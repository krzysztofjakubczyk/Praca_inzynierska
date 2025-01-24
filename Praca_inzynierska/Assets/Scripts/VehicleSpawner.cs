using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] CarPrefab;
    [SerializeField] public int MaxVehicles = 100; // Maksymalna liczba pojazdów na drodze
    [SerializeField] private float spawnInterval; // Dynamicznie ustawiane w TimeManager
    public int CurrentVehicleCount { get; private set; } = 0;

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval; // Ustaw interwa³ spawnowania
    }

    private void Start()
    {
        StartCoroutine(SpawnVehicles());
    }

    private IEnumerator SpawnVehicles()
    {
        while (true)
        {
            if (CurrentVehicleCount < MaxVehicles)
            {
                SpawnVehicle();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnVehicle()
    {
        if (CarPrefab.Length > 0)
        {

            // Sprawdzanie, czy pojazd nie koliduje z innymi
            Vector3 spawnPosition = transform.position;

            Collider[] hitColliders = Physics.OverlapSphere(spawnPosition, 2f); // SprawdŸ, czy w promieniu 2m s¹ pojazdy
            if (hitColliders.Length == 0)
            {
                // Spawnowanie pojazdu
                GameObject vehiclePrefab = CarPrefab[Random.Range(0, CarPrefab.Length)];
                Instantiate(vehiclePrefab, spawnPosition, transform.rotation);
                CurrentVehicleCount++;
            }
        }
    }

    public void ResetSpawner()
    {
        CurrentVehicleCount = 0;
    }
}
