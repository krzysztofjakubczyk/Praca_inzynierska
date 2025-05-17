using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefabs;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Waypoint[] waypointList;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int intervalForBus;
    [SerializeField] private int maxVehicles;
    [SerializeField] private float raycastDistance = 8f;
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private bool isSpawningBuses;

    private int spawnedVehicles = 0;
    private bool canSpawn = true;

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
        StartCoroutine(CheckSpawnAvailabilityLoop()); // 🔁 Dodane
        StartCoroutine(SpawnVehicles());
        if (isSpawningBuses)
            StartCoroutine(SpawnBusRoutine());
    }

    private IEnumerator CheckSpawnAvailabilityLoop()
    {
        while (true)
        {
            canSpawn = CheckIfClearAhead();
            yield return new WaitForSeconds(1f); // co 0.2 sekundy
        }
    }

    private bool CheckIfClearAhead()
    {
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, raycastDistance, detectionLayer);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Car"))
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator SpawnVehicles()
    {
        while (spawnedVehicles < maxVehicles)
        {
            if (canSpawn)
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
            if (canSpawn)
            {
                SpawnBus();
            }
        }
    }

    private void SpawnVehicle()
    {
        GameObject vehicle = Instantiate(vehiclePrefabs, transform.position, transform.rotation);
        CarController carController = vehicle.GetComponent<CarController>();

        if (carController != null)
        {
            carController.SetFirstWaypoint(waypointList);
        }

        spawnedVehicles++;
    }

    private void SpawnBus()
    {
        GameObject bus = Instantiate(busPrefab, transform.position, transform.rotation);
        CarController carController = bus.GetComponent<CarController>();

        if (carController != null)
        {
            carController.SetFirstWaypoint(waypointList);
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
        Gizmos.DrawRay(transform.position, transform.forward * raycastDistance);
    }
}
