using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private Waypoint firstWaypoint;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxVehicles;
    [SerializeField] private float raycastDistance = 8f; // Długość Raycasta do sprawdzania miejsca
    [SerializeField] private LayerMask detectionLayer; // Warstwa, na której sprawdzamy pojazdy

    private int spawnedVehicles = 0;

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
    }

    private IEnumerator SpawnVehicles()
    {
        while (spawnedVehicles < maxVehicles)
        {
            if (CanSpawnVehicle()) // ✅ Sprawdzamy, czy można spawnować pojazd
            {
                SpawnVehicle();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// ✅ Używa Raycasta do sprawdzania, czy przed spawnerem jest miejsce na pojazd.

    private bool CanSpawnVehicle()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // 🟢 Podnosimy Raycast na wysokość zderzaka
        Vector3 rayDirection = transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance, detectionLayer))
        {
            return false; // Jest przeszkoda – nie spawnujemy
        }

        return true; // Brak przeszkód – można spawnować
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
        // Wizualizacja Raycasta w edytorze Unity
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * raycastDistance);
    }
}
