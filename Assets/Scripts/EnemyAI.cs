using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    private HamsterController playerController;
    private NavMeshAgent agent;

    [Header("Visión y Sigilo")]
    public float visionRadiusNormal = 15f;
    public float visionRadiusCrouched = 5f;
    public float backDetectionRadius = 2f; // NUEVO: Radio de detección trasera (omite ángulo)
    [Range(0, 360)] 
    public float visionAngle = 90f;
    
    [Tooltip("Capas que bloquean la vista (Paredes, Casas)")]
    public LayerMask obstacleMask; 

    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (player == null) 
            player = GameObject.FindGameObjectWithTag("Player").transform;
            
        playerController = player.GetComponent<HamsterController>();
    }

    void Update()
    {
        // ARREGLO DE ROTACIÓN: Primero verificamos si YA estamos persiguiendo.
        if (isChasing)
        {
            // Si ya estamos persiguiendo, solo nos importa el Raycast y la distancia.
            // Ignoramos el cono de visión angular.
            if (HasLineOfSight() && Vector3.Distance(transform.position, player.position) <= visionRadiusNormal)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                // Si el rayo se bloquea por un obstáculo, lo pierde de vista.
                LosePlayer();
            }
        }
        else
        {
            // Lógica de detección inicial (igual que antes)
            if (CanDetectPlayerInitial())
            {
                StartChase();
            }
            else
            {
                LosePlayer();
            }
        }
    }

    // --- NUEVA LÓGICA DE DETECCIÓN ---

    private bool CanDetectPlayerInitial()
    {
        float currentVisionRadius = playerController.isCrouching ? visionRadiusCrouched : visionRadiusNormal;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Detección Trasera (NUEVO)
        if (distanceToPlayer <= backDetectionRadius)
        {
            return true; // Lo sintió muy cerca, sin importar el ángulo
        }

        // 2. Detección Frontal (Cono) (Igual que antes)
        if (distanceToPlayer <= currentVisionRadius)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToPlayer) < visionAngle / 2f)
            {
                if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
                {
                    return true; // ¡Lo vio!
                }
            }
        }
        return false;
    }

    // Solo verifica el Raycast para mantener la persecución
    private bool HasLineOfSight()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // El Raycast debe ir hacia el jugador pero ignorar el cono frontal.
        if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
        {
            return true; // La línea de visión se mantiene
        }
        return false;
    }

    private void StartChase()
    {
        isChasing = true;
    }

    private void LosePlayer()
    {
        isChasing = false;
        agent.isStopped = true;
    }
}