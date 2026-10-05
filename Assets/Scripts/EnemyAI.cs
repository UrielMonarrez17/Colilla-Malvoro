using UnityEngine;
using UnityEngine.AI;
using System.Collections; // NUEVO: Necesario para usar Corrutinas

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))] // NUEVO: Aseguramos tener Rigidbody para la trampa de empuje
public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    private HamsterController playerController;
    private NavMeshAgent agent;
    private Rigidbody rbEnemigo; // NUEVO: Referencia a las físicas del enemigo

    [Header("Visión y Sigilo")]
    public float visionRadiusNormal = 15f;
    public float visionRadiusCrouched = 5f;
    public float backDetectionRadius = 2f; // Radio de detección trasera (omite ángulo)
    [Range(0, 360)] 
    public float visionAngle = 90f;
    
    [Tooltip("Capas que bloquean la vista (Paredes, Casas)")]
    public LayerMask obstacleMask; 

    private bool isChasing = false;
    private float velocidadOriginalAgent; 
    private bool isImmobilized = false; // Controla si el enemigo está atrapado en una red// NUEVO: Para guardar la velocidad base

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rbEnemigo = GetComponent<Rigidbody>(); // NUEVO
        velocidadOriginalAgent = agent.speed; // NUEVO
        
        if (player == null) 
            player = GameObject.FindGameObjectWithTag("Player").transform;
            
        playerController = player.GetComponent<HamsterController>();
    }

    void Update()
    {
        if (!agent.enabled) return;

        if (isChasing)
        {
            if (HasLineOfSight() && Vector3.Distance(transform.position, player.position) <= visionRadiusNormal)
            {
                // ¡NUEVO! Solo le permitimos moverse y actualizar destino si no está atrapado
                if (!isImmobilized) 
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                LosePlayer();
            }
        }
        else
        {
            if (CanDetectPlayerInitial()) StartChase();
            else LosePlayer();
        }
    }

    // --- LÓGICA DE DETECCIÓN ---

    private bool CanDetectPlayerInitial()
    {
        float currentVisionRadius = playerController.isCrouching ? visionRadiusCrouched : visionRadiusNormal;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Detección Trasera
        if (distanceToPlayer <= backDetectionRadius)
        {
            return true; // Lo sintió muy cerca, sin importar el ángulo
        }

        // 2. Detección Frontal (Cono)
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
        if (agent.enabled) agent.isStopped = true;
    }

    // ===================================================================================
    //  SISTEMA DE TRAMPAS (NUEVO)
    // ===================================================================================

    public void RecibirTrampa(TipoTrampa tipo, Vector3 posicionPuerta)
    {
        switch (tipo)
        {
            case TipoTrampa.Puas:
                StartCoroutine(EfectoPuas());
                break;
            case TipoTrampa.Red:
                StartCoroutine(EfectoRed());
                break;
            case TipoTrampa.Madera:
                StartCoroutine(EfectoMadera(posicionPuerta));
                break;
        }
    }

    private IEnumerator EfectoPuas()
    {
        Debug.Log("Enemigo pisó Púas. Velocidad reducida.");
        // Reducción del 45% de velocidad
        agent.speed = velocidadOriginalAgent * 0.55f; 
        yield return new WaitForSeconds(4f); // Duración del debuff
        agent.speed = velocidadOriginalAgent;
    }

   private IEnumerator EfectoRed()
    {
        Debug.Log("Enemigo atrapado en Red.");
        
        isImmobilized = true; // Le avisamos al Update que no interfiera
        agent.isStopped = true; // Detenemos al agente
        
        yield return new WaitForSeconds(5f); // Esperamos 5 segundos
        
        isImmobilized = false; // Liberamos al enemigo
        
        if (agent.enabled && isChasing) 
        {
            agent.isStopped = false; 
        }
    }

    private IEnumerator EfectoMadera(Vector3 posicionPuerta)
    {
        Debug.Log("Enemigo empujado por trampa de Madera.");
        
        // 1. Desactivar el NavMeshAgent para permitir físicas de Rigidbody
        agent.enabled = false;
        
        // 2. Habilitamos las físicas
        rbEnemigo.isKinematic = false;

        // 3. Calcular dirección opuesta a la puerta para el empuje
        Vector3 direccionEmpuje = (transform.position - posicionPuerta).normalized;
        direccionEmpuje.y = 0.5f; // Ligero arco hacia arriba
        
        // 4. Aplicamos la fuerza
        rbEnemigo.AddForce(direccionEmpuje * 15f, ForceMode.Impulse);

        // 5. Esperar a que termine de volar/rodar
        yield return new WaitForSeconds(1.5f);
        
        // 6. Restaurar el estado de IA
        rbEnemigo.linearVelocity = Vector3.zero; // Frenamos inercia
        rbEnemigo.isKinematic = true; // Volvemos a proteger las físicas del NavMesh
        agent.enabled = true; // Reactivamos IA
        
        // Si estábamos persiguiéndolo antes del golpe, reasignamos el destino
        if (isChasing)
        {
            agent.SetDestination(player.position);
        }
    }
}