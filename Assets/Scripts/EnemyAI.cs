using UnityEngine;
using UnityEngine.AI;
using System.Collections; // NUEVO: Necesario para usar Corrutinas

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))] // NUEVO: Aseguramos tener Rigidbody para la trampa de empuje
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Wander,
        Chase,
        Attack
    }

    [Header("Estado de la IA (Solo Lectura)")]
    [SerializeField] private EnemyState currentState = EnemyState.Wander;
    public EnemyState CurrentState => currentState;

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

    [Header("Configuración de Deambular (Wander)")]
    public float wanderRadius = 15f;
    public float wanderWaitTime = 3f;
    public float wanderSpeed = 4f;

    [Header("Configuración de Persecución (Chase)")]
    public float chaseSpeed = 8f;

    [Header("Configuración de Ataque")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    // Variables internas
    private float wanderWaitTimer = 0f;
    private float wanderTimeoutTimer = 0f;
    private bool hasWanderDestination = false;
    private float attackTimer = 0f;
    private bool isSubscribedToTime = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = wanderSpeed;
        }

        FindPlayerIfNeeded();
        SubscribeToTimeManager();

        ChangeState(EnemyState.Wander);
    }

    private void OnEnable()
    {
        SubscribeToTimeManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromTimeManager();
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        SubscribeToTimeManager();

        bool isNight = IsNight();

        // 1. REGLA DE DÍA: Solo deambular (Wander), ignorando al jugador
        if (!isNight)
        {
            if (currentState != EnemyState.Wander)
            {
                ChangeState(EnemyState.Wander);
            }

            UpdateWander();
            return;
        }

        // 2. Si el jugador está muerto, volver a deambular pacíficamente
        if (playerController != null && playerController.IsDead)
        {
            if (currentState != EnemyState.Wander)
            {
                ChangeState(EnemyState.Wander);
            }

            UpdateWander();
            return;
        }

        // 3. REGLA DE NOCHE: Máquina de estados completa (Wander, Chase, Attack)
        switch (currentState)
        {
            case EnemyState.Wander:
                UpdateWander();
                // Durante la noche, busca activamente al jugador mientras deambula
                if (CanDetectPlayerInitial())
                {
                    ChangeState(EnemyState.Chase);
                }
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.Attack:
                UpdateAttack();
                break;
        }
    }

    // --- MÁQUINA DE ESTADOS ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Limpieza de estado saliente
        switch (currentState)
        {
            case EnemyState.Wander:
                hasWanderDestination = false;
                wanderWaitTimer = 0f;
                wanderTimeoutTimer = 0f;
                break;

            case EnemyState.Attack:
                attackTimer = 0f;
                break;
        }

        currentState = newState;

        // Configuración de estado entrante
        switch (currentState)
        {
            case EnemyState.Wander:
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = wanderSpeed;
                    agent.isStopped = false;
                }
                break;

            case EnemyState.Chase:
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.speed = chaseSpeed;
                    agent.isStopped = false;
                }
                break;

            case EnemyState.Attack:
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                attackTimer = attackCooldown; // Listo para atacar pronto al entrar en rango
                break;
        }
    }

    // --- LÓGICA DE CADA ESTADO ---

    private void UpdateWander()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.speed = wanderSpeed;

        if (!hasWanderDestination)
        {
            if (GetRandomNavMeshPoint(transform.position, wanderRadius, out Vector3 targetPoint))
            {
                agent.isStopped = false;
                agent.SetDestination(targetPoint);
                hasWanderDestination = true;
                wanderWaitTimer = 0f;
                wanderTimeoutTimer = 0f;
            }
        }
        else
        {
            // Verificamos si llegó a su destino
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                agent.isStopped = true;
                wanderWaitTimer += Time.deltaTime;

                if (wanderWaitTimer >= wanderWaitTime)
                {
                    hasWanderDestination = false;
                    wanderWaitTimer = 0f;
                    wanderTimeoutTimer = 0f;
                }
            }
            else
            {
                // Se está desplazando hacia el punto
                agent.isStopped = false;
                wanderTimeoutTimer += Time.deltaTime;

                // Si tarda demasiado en llegar o la ruta es inválida, elige otro punto
                if (wanderTimeoutTimer >= 12f || agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    hasWanderDestination = false;
                    wanderWaitTimer = 0f;
                    wanderTimeoutTimer = 0f;
                }
            }
        }
    }

    private void UpdateChase()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
        {
            ChangeState(EnemyState.Wander);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si está a rango de ataque y con línea de visión, pasa a atacar
        if (distanceToPlayer <= attackRange && HasLineOfSight())
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Si mantiene visión y distancia aceptable, sigue persiguiendo
        if (HasLineOfSight() && distanceToPlayer <= visionRadiusNormal)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            // Perdió al jugador
            ChangeState(EnemyState.Wander);
        }
    }

    private void UpdateAttack()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
        {
            ChangeState(EnemyState.Wander);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si el jugador se alejó o se rompió la visión, volver a perseguir
        if (distanceToPlayer > attackRange * 1.3f || !HasLineOfSight())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Detenerse durante el ataque
        agent.isStopped = true;

        // Mirar hacia el jugador suavemente en el plano horizontal
        Vector3 direction = (player.position - transform.position);
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }

        // Ciclo de ataque
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            ExecuteAttack();
        }
    }

    private void ExecuteAttack()
    {
        if (playerController != null)
        {
            playerController.ReceiveDamage();
            Debug.Log($"[{gameObject.name}] ¡Atacó al jugador!");
        }
    }

    // --- DETECCIÓN Y VISIÓN ---

    private bool CanDetectPlayerInitial()
    {
        if (player == null) return false;

        float currentVisionRadius = (playerController != null && playerController.isCrouching) 
            ? visionRadiusCrouched 
            : visionRadiusNormal;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Detección Trasera
        if (distanceToPlayer <= backDetectionRadius)
        {
            return true;
        }

        // 2. Detección Frontal (Cono)
        if (distanceToPlayer <= currentVisionRadius)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToPlayer) < visionAngle / 2f)
            {
                if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
        {
            return true;
        }

        return false;
    }

    private bool GetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            randomPoint.y = center.y;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = center;
        return false;
    }

    // --- INTEGRACIÓN CON TIMEMANAGER ---

    public bool IsNight()
    {
        if (TimeManager.Instance == null)
        {
            // Fallback por defecto si no existe TimeManager en la escena (permite pruebas)
            return true;
        }

        return TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
    }

    private void SubscribeToTimeManager()
    {
        if (!isSubscribedToTime && TimeManager.Instance != null)
        {
            TimeManager.Instance.OnCycleStateChanged += HandleCycleStateChanged;
            isSubscribedToTime = true;
        }
    }

    private void UnsubscribeFromTimeManager()
    {
        if (isSubscribedToTime && TimeManager.Instance != null)
        {
            TimeManager.Instance.OnCycleStateChanged -= HandleCycleStateChanged;
            isSubscribedToTime = false;
        }
    }

    private void HandleCycleStateChanged(TimeManager.CycleState newState)
    {
        if (newState == TimeManager.CycleState.Day)
        {
            // Al amanecer se cancela persecución o ataque inmediatamente
            ChangeState(EnemyState.Wander);
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<HamsterController>();
            }
        }
        else if (playerController == null)
        {
            playerController = player.GetComponent<HamsterController>();
        }
    }

    // --- GIZMOS DE DEPURACIÓN EN ESCENA ---

    private void OnDrawGizmosSelected()
    {
        // Radio de deambular
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Radio de visión normal
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, visionRadiusNormal);

        // Radio de detección trasera
        Gizmos.color = Color.mif (agent.enabled) agenta;
        Gizmos.DrawWireSphere(transform.position, backDetectionRadius);

        // Rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Cono de visión
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.Euler(0, -visionAngle / 2f, 0);
        Quaternion rightRayRotation = Quaternion.Euler(0, visionAngle / 2f, 0);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, leftRayDirection * visionRadiusNormal);
        Gizmos.DrawRay(transform.position, rightRayDirection * visionRadiusNormal);
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