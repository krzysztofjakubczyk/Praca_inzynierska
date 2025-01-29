using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;
    public LineLightManager lineManager;
    [SerializeField] float detectionDistance;
    [SerializeField] float stopDistance;
    [SerializeField] float detectionInterval;
    [SerializeField] float stuckTimeThreshold = 5f; // Czas w sekundach do uznania pojazdu za zablokowany
    [SerializeField] float obstacleCheckRadius = 1.5f; // Promie� sprawdzania kolizji
    public float vehicleLength;
    public bool WantWarnings;
    public Waypoint CurrentWaypoint;  // Aktualny waypoint, na kt�ry pojazd zmierza
    public int FullSpeed;

    private Vector3 lastPosition;
    private float stuckTimer;

    private bool isOnTrafficLight;
    private bool isAfterCar;
    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        StartCoroutine(DetectCarsCoroutine());
        StartCoroutine(MoveCoroutine());
        StartCoroutine(CheckIfStuck());

    }

    private IEnumerator MoveCoroutine()
    {
        while (CurrentWaypoint == null)
        {
            if (WantWarnings)
            {
                Debug.LogWarning("Waiting to have currentWaypoint...");
            }
            yield return null;
        }

        while (true)
        {
            if (CurrentWaypoint.isFirstWaypoint)
            {
                lineManager = CurrentWaypoint.linkedController;
            }
            if(CurrentWaypoint.name == "CarDestroyer")
            {
                Destroy(gameObject, 0.5f);
            }
            if (!agent.pathPending && agent.remainingDistance < 2f)
            {
                
                if (CurrentWaypoint.isBeforeTrafiicLight)
                {
                    agent.SetDestination(CurrentWaypoint.laneChooser.transform.position);
                }
                else
                {
                    CurrentWaypoint = CurrentWaypoint.NextWaypoint;
                    agent.SetDestination(CurrentWaypoint.transform.position);
                }
            }

            yield return null;
        }
    }

    public IEnumerator MonitorTrafficLightForVehicle()
    {
        while (true)
        {
            if (lineManager != null)
            {
                isOnTrafficLight = true;
                var currentColor = lineManager.currentColor;
                switch (currentColor)
                {
                    case TrafficLightColor.green:
                        agent.speed = FullSpeed; // Pe�na pr�dko��
                        break;
                    case TrafficLightColor.red:
                        agent.speed = 0f; // Zatrzymanie pojazdu
                        break;
                    case TrafficLightColor.yellow:
                        agent.speed = FullSpeed; // Zwolnienie przed �wiat�em
                        break;
                }
            }
            yield return new WaitForSeconds(1f); // Regularne sprawdzanie stanu sygnalizacji
        }
    }
    private IEnumerator CheckIfStuck()
    {
        while (true)
        {
            // Sprawdzenie, czy pojazd si� porusza (minimalna pr�dko��)
            if (agent.velocity.magnitude > 0.1f || agent.remainingDistance < 0.5f)
            {
                stuckTimer = 0f; // Reset timera je�li pojazd si� rusza lub jest blisko celu
            }
            else
            {
                stuckTimer += 1f; // Je�li nie porusza si�, licz czas blokady
            }

            lastPosition = transform.position;

            // Usuwanie tylko, je�li samoch�d si� nie porusza, nie stoi na �wiat�ach i nie czeka w korku
            if (stuckTimer >= stuckTimeThreshold && !isOnTrafficLight && !isAfterCar)
            {
                //print("Pojazd zablokowany! Resetowanie pozycji...");
                Destroy(gameObject, 0.5f);
            }

            // Usuni�cie je�li pojazd jest wewn�trz przeszkody (ale nie na �wiat�ach)
            if (IsInObstacle() && !isOnTrafficLight && !isAfterCar)
            {
                //print("Pojazd w przeszkodzie! Usuwanie...");
                Destroy(gameObject, 0.5f);
            }

            yield return new WaitForSeconds(1f);
        }
    }


    private bool IsInObstacle()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, obstacleCheckRadius);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Obstacle"))
            {
                return true;
            }
        }
        return false;
    }

    public void SetFirstWaypoint(Waypoint waypoint)
    {
        if (waypoint != null)
        {
            CurrentWaypoint = waypoint;

        }
        else
        {
            Debug.LogWarning("Przekazany waypoint jest null!");
        }
    }

    private Coroutine trafficLightCoroutine; // Przechowuje referencj� do uruchomionej korutyny

    public void StartTrafficLightMonitoring()
    {
        if (trafficLightCoroutine == null)
        {
            trafficLightCoroutine = StartCoroutine(MonitorTrafficLightForVehicle());
        }
    }

    public void StopTrafficLightMonitoring()
    {
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            isOnTrafficLight = false;
            trafficLightCoroutine = null; // Resetuj referencj�
        }

        // Przywr�� pe�n� pr�dko��, gdy opuszczamy trigger
        agent.speed = FullSpeed;
    }

    private IEnumerator DetectCarsCoroutine()
    {
        while (true)
        {
            DetectCars();
            yield return new WaitForSeconds(detectionInterval); // Czekaj przez okre�lony czas
        }
    }

    private void DetectCars()
    {
        RaycastHit hit;
        // Ustawienie wektora kierunku do przodu
        Vector3 rayStartPosition = transform.position + Vector3.up;
        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;

        // Sprawdzenie, czy raycast trafia w inne auto
        if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, detectionDistance))
        {

            if (hit.collider.CompareTag("Car")) // Zak�adamy, �e inne samochody maj� tag "Car"
            {
                isAfterCar = true;
                // Je�li wykryto inne auto, zatrzymaj si�, je�li jest za blisko
                if (hit.distance < stopDistance)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
               
                else
                {
                    agent.isStopped = false;
                    agent.speed = FullSpeed; // Przywr�� pe�n� pr�dko��
                }
            }
            else if (hit.collider.CompareTag("Obstacle"))
            {
                Vector3 directionToTarget = (agent.destination - transform.position).normalized;
                directionToTarget.y = 0; // Upewniamy si�, �e auto nie obraca si� w pionie
                transform.rotation = Quaternion.LookRotation(directionToTarget);

                // Kontynuowanie jazdy
                agent.isStopped = false;
                agent.speed = FullSpeed;
            }

        }
        else
        {
            isAfterCar = false;
            // Nie wykryto przeszk�d, kontynuuj jazd�
            agent.isStopped = false;
        }
    }
    public void SetAgentDestination(Vector3 destinationTransform)
    {
        agent.SetDestination(destinationTransform);
    }
    private void OnDrawGizmos()
    {
        Vector3 rayStartPosition = transform.position + Vector3.up;

        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;
        Debug.DrawRay(rayStartPosition, forwardDirection, Color.green); // Rysuje czerwony promie�
    }
}