using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generuje popyt (pojazdy) na wlocie. Pojazd, który nie może wjechać, bo wjazd jest zajęty,
/// NIE przepada – trafia do kolejki wirtualnej (Backlog) i wjedzie, gdy zrobi się miejsce.
/// Dzięki temu korek sięgający poza symulację jest widoczny w nagrodzie i statystykach,
/// a agent nie może "pozbyć się" ruchu, blokując wjazd.
/// </summary>
public class VehicleSpawner : MonoBehaviour
{
    public static readonly List<VehicleSpawner> All = new List<VehicleSpawner>();

    /// <summary>Łączna liczba pojazdów czekających na wjazd we wszystkich spawnerach.</summary>
    public static int TotalBacklog
    {
        get
        {
            int total = 0;
            for (int i = 0; i < All.Count; i++) total += All[i].Backlog;
            return total;
        }
    }

    [Header("Prefabs & Waypoints")]
    [SerializeField] private GameObject vehiclePrefabs;
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Waypoint[] waypointList;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int intervalForBus = 15;
    [SerializeField] private int maxVehicles = 20;
    [SerializeField] private bool isSpawningBuses;

    [Header("Spawn Safeguard")]
    [Tooltip("Minimalny odstęp czasowy (w sekundach) między jakimikolwiek dwoma pojazdami z tego spawnera.")]
    [SerializeField] private float minSpawnCooldown = 2f;
    private float lastSpawnTime = -999f;

    [Header("Lane & Turn Settings")]
    [SerializeField] private bool isOnlyLeftTurn = false;
    [SerializeField] private bool isRandomTurn = true;
    [Range(0f, 100f)]
    [SerializeField] private float leftTurnProbability = 50f;

    [Header("Detection")]
    [SerializeField] private float raycastDistance = 8f;
    [SerializeField] private LayerMask detectionLayer;

    private int generatedVehicles;   // ile pojazdów popytu już wygenerowano (wjechały + czekają)
    private int pendingVehicles;     // samochody czekające na wjazd
    private int pendingBuses;        // autobusy czekające na wjazd
    private float intervalMultiplier = 1f;
    private int trafficLaneId;

    public int MaxVehicles
    {
        get => maxVehicles;
        set => maxVehicles = value;
    }

    public int Backlog => pendingVehicles + pendingBuses;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public void SetSpawnInterval(float interval) => spawnInterval = interval;
    public float GetSpawnInterval() => spawnInterval;

    /// <summary>Mnożnik interwału (np. 0.25 = 4x częściej w fazie wypełniania). Nie nadpisuje bazowego interwału.</summary>
    public void SetIntervalMultiplier(float multiplier) => intervalMultiplier = Mathf.Max(0.01f, multiplier);

    public void SetTrafficLaneId(int laneId) => trafficLaneId = laneId;

    public void ResetSpawner()
    {
        generatedVehicles = 0;
        pendingVehicles = 0;
        pendingBuses = 0;
        lastSpawnTime = -999f;
        StopAllCoroutines();
        CancelInvoke(nameof(StartSpawning)); // zabezpieczenie przed podwójnym startem
        Invoke(nameof(StartSpawning), 2f);
    }

    public void StartSpawning()
    {
        StopAllCoroutines();
        StartCoroutine(GenerateCarDemand());
        if (isSpawningBuses)
            StartCoroutine(GenerateBusDemand());
        StartCoroutine(ReleaseVehicles());
    }

    // Popyt samochodów: postęp liczony co klatkę, więc zmiana interwału/mnożnika działa od razu.
    private IEnumerator GenerateCarDemand()
    {
        float progress = 1f; // pierwszy pojazd od razu (jak w poprzedniej wersji)
        while (generatedVehicles < maxVehicles)
        {
            float interval = spawnInterval * intervalMultiplier;
            if (!float.IsInfinity(interval) && interval > 0f)
                progress += Time.deltaTime / interval;

            while (progress >= 1f && generatedVehicles < maxVehicles)
            {
                progress -= 1f;
                pendingVehicles++;
                generatedVehicles++;
            }
            yield return null;
        }
    }

    private IEnumerator GenerateBusDemand()
    {
        while (generatedVehicles < maxVehicles)
        {
            yield return new WaitForSeconds(intervalForBus);
            if (generatedVehicles >= maxVehicles) yield break;
            pendingBuses++;
            generatedVehicles++;
        }
    }

    private IEnumerator ReleaseVehicles()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);
        while (true)
        {
            bool busNext = pendingBuses > 0 && busPrefab != null;
            if (Backlog > 0 && Time.time - lastSpawnTime >= minSpawnCooldown &&
                CheckIfClearAhead() && IsSpawnAreaFree(busNext ? busPrefab : vehiclePrefabs))
            {
                if (pendingBuses > 0 && busPrefab != null)
                {
                    pendingBuses--;
                    SpawnBus();
                }
                else if (pendingVehicles > 0)
                {
                    pendingVehicles--;
                    SpawnVehicle();
                }
                else
                {
                    pendingBuses = 0; // brak prefabu autobusu – nie blokujemy kolejki
                }
                lastSpawnTime = Time.time;
            }
            yield return wait;
        }
    }

    private bool CheckIfClearAhead()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 3f, detectionLayer);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Car") || col.CompareTag("Bus"))
                return false;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(rayOrigin, 1.2f, transform.forward, out RaycastHit hit, raycastDistance, detectionLayer))
        {
            if (hit.collider.CompareTag("Car") || hit.collider.CompareTag("Bus"))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Czy pojazd o długości prefabu zmieści się w miejscu spawnu z zapasem przed nim (sprawdzane po pozycjach
    /// pojazdów, niezależnie od warstw fizyki). Wcześniej sprawdzano tylko kulę 3 m – 12-metrowy autobus
    /// pojawiał się na stojącym aucie.
    /// </summary>
    private bool IsSpawnAreaFree(GameObject prefab)
    {
        float newLength = PrefabLength(prefab);
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);

        foreach (CarController car in CarController.Active)
        {
            Vector3 offset = car.transform.position - transform.position;
            offset.y = 0f;
            if (Mathf.Abs(Vector3.Dot(offset, right)) > 2.2f) continue; // inny pas
            float along = Vector3.Dot(offset, forward);
            float halfLengths = newLength * 0.5f + car.Length * 0.5f;
            if (along > -halfLengths - 1f && along < halfLengths + 6f) return false;
        }
        return true;
    }

    private static readonly System.Collections.Generic.Dictionary<GameObject, float> LengthCache =
        new System.Collections.Generic.Dictionary<GameObject, float>();

    private static float PrefabLength(GameObject prefab)
    {
        if (prefab == null) return 4f;
        if (LengthCache.TryGetValue(prefab, out float length)) return length;
        BoxCollider box = prefab.GetComponent<BoxCollider>();
        length = box != null ? Mathf.Abs(box.size.z * prefab.transform.localScale.z) : 4f;
        LengthCache[prefab] = length;
        return length;
    }

    private void SpawnVehicle()
    {
        GameObject vehicle = Instantiate(vehiclePrefabs, transform.position, transform.rotation);
        CarController carController = vehicle.GetComponent<CarController>();
        if (carController != null)
        {
            carController.sourceLaneId = trafficLaneId;
            carController.SetFirstWaypoint(waypointList);

            if (isOnlyLeftTurn)
            {
                carController.isTurningLeft = true;
                carController.movement = TrafficMovement.Left;
            }
            else if (isRandomTurn)
            {
                float randomVal = Random.Range(0f, 100f);
                carController.isTurningLeft = randomVal < leftTurnProbability;
            }
        }
    }

    private void SpawnBus()
    {
        GameObject bus = Instantiate(busPrefab, transform.position, transform.rotation);
        CarController carController = bus.GetComponent<CarController>();
        if (carController != null)
        {
            carController.sourceLaneId = trafficLaneId;
            carController.SetFirstWaypoint(waypointList);
            carController.isTurningLeft = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOnlyLeftTurn ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, 3f);
        Gizmos.DrawRay(transform.position, transform.forward * raycastDistance);
    }
}
