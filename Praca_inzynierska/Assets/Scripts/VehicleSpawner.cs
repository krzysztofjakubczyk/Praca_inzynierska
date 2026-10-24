using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [Header("Prefabs & Waypoints")]
    [SerializeField] private GameObject vehiclePrefabs;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Waypoint[] waypointList;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int intervalForBus = 15;
    [SerializeField] private int maxVehicles = 20;
    [SerializeField] private bool isSpawningBuses;

    [Header("Lane & Turn Safeguard")]
    [Tooltip("Jeśli zaznaczone, każdy zespawnowany pojazd automatycznie ma włączoną flagę skrętu w lewo.")]
    [SerializeField] private bool isOnlyLeftTurn = false; // <--- NOWE ZABEZPIECZENIE

    [Header("Detection")]
    [SerializeField] private float raycastDistance = 8f;
    [SerializeField] private LayerMask detectionLayer;

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
        StartCoroutine(CheckSpawnAvailabilityLoop());
        StartCoroutine(SpawnVehicles());
        if (isSpawningBuses)
            StartCoroutine(SpawnBusRoutine());
    }

    private IEnumerator CheckSpawnAvailabilityLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);
        while (true)
        {
            canSpawn = CheckIfClearAhead();
            yield return wait;
        }
    }

    private bool CheckIfClearAhead()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 3f, detectionLayer);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Car") || col.CompareTag("Bus"))
            {
                return false;
            }
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(rayOrigin, 1.2f, transform.forward, out RaycastHit hit, raycastDistance, detectionLayer))
        {
            if (hit.collider.CompareTag("Car") || hit.collider.CompareTag("Bus"))
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

            // ZABEZPIECZENIE: Jeśli spawner obsługuje pas wyłącznie do skrętu w lewo
            if (isOnlyLeftTurn)
            {
                carController.isTurningLeft = true;
            }
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

            // ZABEZPIECZENIE: Jeśli spawner obsługuje pas wyłącznie do skrętu w lewo
            if (isOnlyLeftTurn)
            {
                carController.isTurningLeft = true;
            }
        }
        spawnedVehicles++;
    }

    public float GetSpawnInterval()
    {
        return spawnInterval;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOnlyLeftTurn ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
        Gizmos.DrawRay(transform.position, transform.forward * raycastDistance);
    }
}