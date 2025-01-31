using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefab; // Prefab pojazdu
    [SerializeField] private Waypoint firstWaypoint; // Pierwszy waypoint, do którego pojazd ma się udać
    [SerializeField] private float spawnInterval = 5f; // Interwał spawnowania
    [SerializeField] private int maxVehicles; // Maksymalna liczba pojazdów do spawnowania

    [SerializeField]private int spawnedVehicles = 0; // Licznik spawnowanych pojazdów
    [SerializeField]private bool wantToSpawn; // Licznik spawnowanych pojazdów

    public int MaxVehicles
    {
        get => maxVehicles;
        set => maxVehicles = value;
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
    }

    public void ResetSpawner()
    {
        spawnedVehicles = 0;
        StopAllCoroutines();
        Invoke(nameof(StartSpawning),2f);
    }

    public void StartSpawning()
    {
        StartCoroutine(SpawnVehicles());
    }


    private IEnumerator SpawnVehicles()
    {
        while (spawnedVehicles < maxVehicles)   
        {
 
            SpawnVehicle();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    private void SpawnVehicle()
    {
        if (vehiclePrefab == null || firstWaypoint == null)
        {
            Debug.LogError($"Spawner {gameObject.name} ma brakujące elementy: prefab lub pierwszy waypoint!");
            return;
        }

        GameObject vehicle = Instantiate(vehiclePrefab, transform.position, transform.rotation);
        CarController carController = vehicle.GetComponent<CarController>();

        if (carController != null)
        {
            carController.SetFirstWaypoint(firstWaypoint); // Przypisz pierwszy waypoint
        }

        spawnedVehicles++;    }

    private void OnDrawGizmos()
    {
        // Wizualizacja połączenia do firstWaypoint
        if (firstWaypoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, firstWaypoint.transform.position);
        }
    }
}
