using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Car Detection & Obstacle")]
    [SerializeField] private float stopDistance;
    [SerializeField] private float obstacleCheckRadius;
    private bool isAtMiddleOfIntersection = false; // NOWE: Czy auto faktycznie jest na środku?

    [Header("General")]
    public Waypoint CurrentWaypoint;
    public float CurretSpeed;
    public int vehicleLength;

    public LineLightManager lineManager;    // Aktualny sygnalizator, pod który podlega auto

    public bool stopForLight = false;      // Czy musimy się zatrzymać z powodu czerwonego/żółtego światła
    public bool isAfterCar = false;        // Flaga używana przy detekcji innego samochodu
    public bool stopForCollision = false;        // Flaga używana przy detekcji innego samochodu
    private bool isLaneBlocked = false;        // Flaga używana przy detekcji innego samochodu
    private Waypoint previousWaypoint;

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

        StartCoroutine(moveCoroutine());
        StartCoroutine(CheckCarInFront());
        StartCoroutine(ShouldStop());
    }

    private IEnumerator moveCoroutine()
    {
        // Czekaj, aż zostanie ustawiony pierwszy waypoint
        while (CurrentWaypoint == null)
        {
            yield return null;
        }

        while (true)
        {
            if (CurrentWaypoint.isFirstWaypoint && CurrentWaypoint.linkedController != null)
            {
                lineManager = CurrentWaypoint.linkedController;
                //Jeżeli pojazd jedzie w kierunku pierwszego punktu oraz nie ma przypisanego kontrolera swiateł- ustaw kontroler
            }
            //Zatrzymanie pojazdu zależy od wyniku metody ShouldStop
            if (!agent.pathPending && agent.remainingDistance < 5f)
            {

                //Jeżeli pojazd porusza się oraz najbliższy punkt jest mniej niż 2 jednostki
                if (CurrentWaypoint.isBeforeTrafiicLight)
                {
                    previousWaypoint = CurrentWaypoint;
                    agent.SetDestination(CurrentWaypoint.laneChooser.transform.position);
                }
                else
                {
                    // Aktualizacja na następny waypoint
                    previousWaypoint = CurrentWaypoint; // Zapis poprzedniego waypointu
                    CurrentWaypoint = CurrentWaypoint.NextWaypoint; //Podmień obecny punkt na następny 
                    if (CurrentWaypoint != null)
                        agent.SetDestination((CurrentWaypoint.transform.position)); // Ustaw cel poruszania się 
                }
            }


            yield return null;
        }
    }
    private IEnumerator ShouldStop()
    {
        while (true)
        {
            agent.isStopped = isAfterCar || stopForLight || stopForCollision || isLaneBlocked;
            yield return null;
        }

    }
    private IEnumerator CheckCarInFront()
    {
        while (true)
        {
            isAfterCar = false;
            RaycastHit hit;
            Vector3 rayStartPosition = transform.position; // Podniesienie Raycasta na wysokość maski auta
            Vector3 forwardDirection = transform.forward;

            if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, stopDistance))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isAfterCar = true;
                }
                else if (hit.collider.CompareTag("Obstacle"))
                {
                    Vector3 directionToTarget = (agent.destination - transform.position).normalized;
                    directionToTarget.y = 0;
                    transform.rotation = Quaternion.LookRotation(directionToTarget);
                }
            }
            else
            {
                if (isAfterCar)
                {
                    isAfterCar = false; // Auto przed nami odjechało
                }
            }

            yield return new WaitForSeconds(0.5f); // Sprawdzaj co 0.5 sekundy
        }
    }
    public void SetMiddleIntersectionState(bool state)
    {
        isAtMiddleOfIntersection = state;

        if (isAtMiddleOfIntersection)
        {
            StartCoroutine(CheckCollisionLaneState());
        }
        else
        {
            // Jeśli wychodzimy ze strefy kolizyjnej, upewnij się, że przestajemy blokować
            stopForCollision = false;
        }
    }

    private IEnumerator CheckCollisionLaneState()
    {
        while (isAtMiddleOfIntersection)
        {
            // Auto jedzie prosto – nie musi czekać
            if (previousWaypoint.laneChooser != null && previousWaypoint.laneChooser.isDrivingStraight)
            {
                stopForCollision = false;
                break;
            }
            // Auto skręca w lewo – sprawdzamy, czy droga jest wolna
            else if (lineManager != null && lineManager.leftTurnAllowed)
            {
                stopForCollision = false;
                isAtMiddleOfIntersection = false; // Kończymy oczekiwanie
                break;
            }
            else
            {
                stopForCollision = true; // Musi czekać
            }

            yield return null;
        }
    }

    private IEnumerator monitorTrafficLightForVehicle()
    {
        while (true)
        {
            if (lineManager != null)
            {
                isLaneBlocked = lineManager.isBlocked;
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

    public void StartTrafficLightMonitoring()
    {
        if (trafficLightCoroutine == null)
        {
            trafficLightCoroutine = StartCoroutine(monitorTrafficLightForVehicle());
        }
    }

    public void StopTrafficLightMonitoring()
    {
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = null;
        }
        stopForLight = false;         // skoro wyjeżdżamy z obszaru sygnalizacji, nie musimy stać
    }
    private void OnDrawGizmos()
    {
        Debug.DrawRay(transform.position, transform.forward * stopDistance, Color.green);
    }
    public void SetAgentDestination(Vector3 destinationTransform)
    {
        agent.SetDestination(destinationTransform);
    }
    private void Update()
    {
        CurretSpeed = agent.speed;
    }
}