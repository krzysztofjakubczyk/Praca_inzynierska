using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Car Detection & Obstacle")]
    [SerializeField] private float detectionDistance = 10f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float obstacleCheckRadius = 1.5f;

    [Header("General")]
    [SerializeField] private float stuckTimeThreshold = 10f;
    public Waypoint CurrentWaypoint;
    public int FullSpeed = 10;
    public int vehicleLength;

    public LineLightManager lineManager;    // Aktualny sygnalizator, pod który podlega auto

    private bool stopForCar = false;        // Czy musimy się zatrzymać z powodu innego auta
    private bool stopForLight = false;      // Czy musimy się zatrzymać z powodu czerwonego/żółtego światła
    private bool isOnTrafficLight = false;  // Czy znajdujemy się w obszarze skrzyżowania ze światłami
    private bool isAfterCar = false;        // Flaga używana przy detekcji innego samochodu

    private Vector3 lastPosition;
    private float stuckTimer;

    private Coroutine trafficLightCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        if (agent == null)
        {
            Debug.LogError($"🚗 {gameObject.name} nie ma przypisanego `NavMeshAgent`! Sprawdź prefab.");
            return;
        }
        StartCoroutine(MoveCoroutine());
        StartCoroutine(CheckIfStuck());
    }

    private IEnumerator MoveCoroutine()
    {
        // Czekaj na wybranie waypointu startowego
        while (CurrentWaypoint == null)
        {
            yield return null;
        }

        // Główna pętla
        while (true)
        {
            // Jeżeli dany waypoint to "pierwszy" – ustaw lineManager, jeśli jest tam przypisany
            if (CurrentWaypoint.isFirstWaypoint && CurrentWaypoint.linkedController != null)
            {
                lineManager = CurrentWaypoint.linkedController;
            }

            // Specjalny waypoint o nazwie "CarDestroyer"
            if (CurrentWaypoint.name == "CarDestroyer")
            {
                Destroy(gameObject, 0.5f);
            }

            // Gdy dotarliśmy do waypointu, wyznacz kolejny cel (o ile agent nie jest zatrzymany)
            if (!agent.pathPending && agent.remainingDistance < 2f)
            {
                if (!agent.isStopped)
                {
                    if (CurrentWaypoint.isBeforeTrafiicLight)
                    {
                        // Kierujemy się do "laneChooser"
                        agent.SetDestination(CurrentWaypoint.laneChooser.transform.position);
                    }
                    else
                    {
                        // Następny waypoint
                        CurrentWaypoint = CurrentWaypoint.NextWaypoint;
                        if (CurrentWaypoint != null)
                            agent.SetDestination(CurrentWaypoint.transform.position);
                    }
                }
            }

            // Sprawdzenie, czy przed nami jest inne auto lub przeszkoda
            CheckCarInFront();

            // **Tu łączymy stany**: finalna decyzja o zatrzymaniu
            agent.isStopped = stopForCar || stopForLight;

            yield return null;
        }
    }

    /// <summary>
    /// Sprawdza Raycastem, czy przed nami jest auto lub przeszkoda i ustawia "stopForCar".
    /// </summary>
    private void CheckCarInFront()
    {
        RaycastHit hit;
        Vector3 rayStartPosition = transform.position + Vector3.up;
        Vector3 forwardDirection = transform.forward;

        // Zresetuj wartość na początku każdej klatki – zaraz sprawdzimy, czy mamy się zatrzymać
        stopForCar = false;

        if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, detectionDistance))
        {
            // 1) Wykryto inny samochód
            if (hit.collider.CompareTag("Car"))
            {
                isAfterCar = true;
                // Jeśli jest bardzo blisko
                if (hit.distance < stopDistance)
                {
                    stopForCar = true;
                }
            }
            // 2) Przeszkoda
            else if (hit.collider.CompareTag("Obstacle"))
            {
                // Obracanie się w stronę celu (jeśli tak ma działać logika)
                Vector3 directionToTarget = (agent.destination - transform.position).normalized;
                directionToTarget.y = 0;
                transform.rotation = Quaternion.LookRotation(directionToTarget);
                stopForCar = false;
            }
        }
        else
        {
            // Nic nie wykryto
            if (isAfterCar)
            {
                // Dla czytelności logów:
                //Debug.Log($"[DetectCars] Samochód przed nami odjechał. Wznawiamy ruch.");
                isAfterCar = false;
            }
        }
    }

    /// <summary>
    /// Korutyna ciągle monitorująca kolor światła i ustawiająca "stopForLight".
    /// </summary>
    private IEnumerator MonitorTrafficLightForVehicle()
    {
        while (true)
        {
            if (lineManager != null)
            {
                isOnTrafficLight = true;

                switch (lineManager.currentColor)
                {
                    case TrafficLightColor.green:
                        stopForLight = false;
                        break;

                    case TrafficLightColor.red:
                        stopForLight = true;
                        break;

                    case TrafficLightColor.yellow:
                        stopForLight = true;
                        break;
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// Sprawdza, czy pojazd się nie "zawiesił" w miejscu (poza światłami i bez auta z przodu).
    /// Jeśli tak – usuwa go.
    /// </summary>
    private IEnumerator CheckIfStuck()
    {
        while (true)
        {
            // Jeśli jedziemy albo jesteśmy tuż przy celu, reset timera
            if (agent.velocity.magnitude > 0.1f || agent.remainingDistance < 0.5f)
            {
                stuckTimer = 0f;
            }
            else
            {
                stuckTimer += 1f;
            }

            lastPosition = transform.position;

            // Jeśli zbyt długo stoimy, a nie mamy czerwonego światła ani nie ma auta z przodu
            if (stuckTimer >= stuckTimeThreshold && !stopForLight && !isAfterCar)
            {
                Destroy(gameObject, 0.5f);
            }

            // Sprawdzenie, czy pojazd nie jest "w środku" przeszkody
            if (IsInObstacle() && !stopForLight && !isAfterCar)
            {
                Destroy(gameObject, 0.5f);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Sprawdza, czy samochód znajduje się wewnątrz colidera z tagiem "Obstacle".
    /// </summary>
    private bool IsInObstacle()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, obstacleCheckRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Obstacle"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Ustawienie waypointu startowego.
    /// </summary>
    public void SetFirstWaypoint(Waypoint waypoint)
    {
        if (waypoint == null)
        {
            Debug.LogError($"🚗 {gameObject.name} otrzymał `null` jako pierwszy waypoint! Sprawdź `VehicleSpawner`.");
            return;
        }

        CurrentWaypoint = waypoint;

        if (agent != null)
        {
            agent.SetDestination(CurrentWaypoint.transform.position);
        }
    }

    /// <summary>
    /// Rozpoczęcie korutyny monitorującej światła.
    /// </summary>
    public void StartTrafficLightMonitoring()
    {
        if (trafficLightCoroutine == null)
        {
            trafficLightCoroutine = StartCoroutine(MonitorTrafficLightForVehicle());
        }
    }

    /// <summary>
    /// Zatrzymanie korutyny monitorującej światła.
    /// </summary>
    public void StopTrafficLightMonitoring()
    {
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = null;
        }
        isOnTrafficLight = false;
        stopForLight = false;         // skoro wyjeżdżamy z obszaru sygnalizacji, nie musimy stać
    }
    private void OnDrawGizmos()
    {
        // Wizualizacja promienia do wykrywania samochodu z przodu
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * detectionDistance);

        // Wizualizacja sfery do wykrywania przeszkód "od środka"
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleCheckRadius);
    }
    public void SetAgentDestination(Vector3 destinationTransform)
    {
        agent.SetDestination(destinationTransform);
    }
}