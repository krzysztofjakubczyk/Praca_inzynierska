using UnityEngine.AI;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;
    public LocalTrafficController controller;
    [SerializeField] float detectionDistance = 10f;
    [SerializeField] float stopDistance = 8f;
    [SerializeField] float detectionInterval = 0.5f;
    public Waypoint currentWaypoint;  // Aktualny waypoint, na kt�ry pojazd zmierza
    private string currentTraficColor;

    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Ustawienie celu jako miejsce, do kt�rego pojazd ma si� uda�
        Transform nearestParkingSpot = FindLineChooser(agent.transform);
        SetAgentDestination(nearestParkingSpot.position);


        StartCoroutine(DetectCarsCoroutine());
    }
    private void Update()
    {
        if (currentWaypoint == null || currentWaypoint.NextWaypoint == null)
        {
            return;
        }   
        if(currentWaypoint.isFirstWaypoint)
        {
            controller = currentWaypoint.linkedController;
        }
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypoint = currentWaypoint.NextWaypoint;
            if (!currentWaypoint.isBeforeTrafiicLight)
            {
                agent.SetDestination(currentWaypoint.transform.position);
            }
           if(currentWaypoint.isBeforeTrafiicLight && controller.currentLight == "green")
            {
                agent.SetDestination(currentWaypoint.transform.position);
            }
           if(currentWaypoint.isBeforeTrafiicLight && controller.currentLight == "red")
            {
                print(agent.remainingDistance);

                if (agent.remainingDistance < 0.5f)
                {
                    agent.speed = 0;
                }
                else
                {
                    agent.speed = 3;
                    
                }
                agent.SetDestination(currentWaypoint.transform.position);
            }
    }
    }
    private Transform FindLineChooser(Transform carTransform)
    {
        GameObject[] parkingSpots = GameObject.FindGameObjectsWithTag("StopPoint");

        Transform nearestSpot = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject spot in parkingSpots)
        {
            float distance = Vector3.Distance(carTransform.position, spot.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSpot = spot.transform;
            }
        }

        return nearestSpot;
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
        Vector3 rayStartPosition = transform.position + Vector3.up * 2f;
        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;

        // Sprawdzenie, czy raycast trafia w inne auto
        if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, detectionDistance))
        {
            print(hit.collider.name + " + " + hit.distance);
            if (hit.collider.CompareTag("Car")) // Zak�adamy, �e inne samochody maj� tag "Car"
            {
                // Je�li wykryto inne auto, zatrzymaj si�, je�li jest za blisko
                if (hit.distance < stopDistance)
                {
                    print("Zatrzymano autko");
                    agent.isStopped = true; // Zatrzymaj pojazd
                }
                else
                {
                    agent.isStopped = false; // Wzn�w jazd�
                }
            }
        }
        else
        {
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
        Vector3 rayStartPosition = transform.position + Vector3.up * 2f;

        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;
        Debug.DrawRay(rayStartPosition, forwardDirection, Color.green); // Rysuje czerwony promie�
    }
} 