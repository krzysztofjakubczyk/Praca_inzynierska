using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefabs;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Waypoint firstWaypoint;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int intervalForBus; // Co który pojazd pojawia się autobus
    [SerializeField] private int maxVehicles;
    [SerializeField] private float raycastDistance = 8f;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private bool isSpawningBuses;

    private int spawnedVehicles = 0;
    private float busSpawnTimer = 0f; // 3 minuty

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
        Invoke(nameof(StartSpawning), 2f);
    }

    public void StartSpawning()
    {
        StartCoroutine(SpawnVehicles());
        if(isSpawningBuses) StartCoroutine(SpawnBusRoutine());

    }

    private IEnumerator SpawnVehicles()
    {
        while (spawnedVehicles < maxVehicles)
        {
            if (CanSpawnVehicle())
            {
                SpawnVehicle();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator SpawnBusRoutine()
    {
        while (spawnedVehicles < maxVehicles)
        {
            yield return new WaitForSeconds(intervalForBus);
            if (CanSpawnVehicle())
            {
                SpawnBus();
            }
            
        }
    }

    private bool CanSpawnVehicle()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 rayDirection = transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance, detectionLayer))
        {
            return false;
        }
        return true;
    }

    private void SpawnVehicle()
    {
        if (vehiclePrefabs == null || firstWaypoint == null)
        {
            Debug.LogError($"Spawner {gameObject.name} ma brakujące elementy: prefab lub pierwszy waypoint!");
            return;
        }

        GameObject vehicle = Instantiate(vehiclePrefabs, transform.position, transform.rotation);
        CarController carController = vehicle.GetComponent<CarController>();

        if (carController != null)
        {
            carController.SetFirstWaypoint(firstWaypoint);
        }

        spawnedVehicles++;
    }

    private void SpawnBus()
    {
        if (busPrefab == null || firstWaypoint == null)
        {
            Debug.LogError("Brak autobusu lub pierwszego waypointa!");
            return;
        }

        GameObject bus = Instantiate(busPrefab, transform.position, transform.rotation);
        CarController carController = bus.GetComponent<CarController>();

        if (carController != null)
        {
            carController.SetFirstWaypoint(firstWaypoint);
        }
        spawnedVehicles++;
    }

    public float GetSpawnInterval()
    {
        return spawnInterval;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * raycastDistance);
    }
}