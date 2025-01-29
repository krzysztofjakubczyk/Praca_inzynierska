using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefab; // Prefab pojazdu
    [SerializeField] private Waypoint firstWaypoint; // Pierwszy waypoint, do którego pojazd ma siê udaæ
    [SerializeField] private float spawnInterval = 5f; // Interwa³ spawnowania
    [SerializeField] private int maxVehicles; // Maksymalna liczba pojazdów do spawnowania

    [SerializeField]private int spawnedVehicles = 0; // Licznik spawnowanych pojazdów

    private void Start()
    {
        //StartSpawning();
    }

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
        StartSpawning();
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
        if (vehiclePrefab.name == "Tram") print("ITS TRAM!");
        if (vehiclePrefab == null || firstWaypoint == null)
        {
            Debug.LogError($"Spawner {gameObject.name} ma brakuj¹ce elementy: prefab lub pierwszy waypoint!");
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
        // Wizualizacja po³¹czenia do firstWaypoint
        if (firstWaypoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, firstWaypoint.transform.position);
        }
    }
}
