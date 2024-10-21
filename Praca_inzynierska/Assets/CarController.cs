using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    public Transform target;  // Transform celu, do kt�rego pojazd ma si� uda�
    private NavMeshAgent agent;

    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Ustawienie celu jako miejsce, do kt�rego pojazd ma si� uda�
        agent.SetDestination(target.position);
    }

    void Update()
    {
        // Je�li pojazd ma nowy cel lub potrzebujesz aktualizowa� go dynamicznie
        if (Vector3.Distance(agent.destination, target.position) > 0.1f)
        {
            agent.SetDestination(target.position);
        }
    }
}
