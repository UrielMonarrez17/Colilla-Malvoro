using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))] 
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
    private Rigidbody rbEnemigo; 

    [Header("Visión y Sigilo")]
    public float visionRadiusNormal = 15f;
    public float visionRadiusCrouched = 5f;
    public float backDetectionRadius = 2f; 
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
    private float velocidadOriginalAgent; 
    private bool isImmobilized = false;

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
        rbEnemigo = GetComponent<Rigidbody>(); 
        velocidadOriginalAgent = agent.speed;
        
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
        // ¡NUEVO CANDADO MAESTRO!
        // Si la IA está apagada (trampa de madera) o inmovilizada (trampa de red), pausamos la máquina de estados.
        if (agent == null || !agent.enabled || isImmobilized) return;

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
        if (playerController != null && playerController.IsDead) // Asegúrate de que IsDead exista y sea public en HamsterController
        {
            if (currentState != EnemyState.Wander)
            {
                ChangeState(EnemyState.Wander);
            }

            UpdateWander();
            return;
        }

        // 3. REGLA DE NOCHE: Máquina de estados completa
        switch (currentState)
        {
            case EnemyState.Wander:
                UpdateWander();
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
                attackTimer = attackCooldown; 
                break;
        }
    }

    // --- LÓGICA DE CADA ESTADO ---

    private void UpdateWander()
    {
        if (!agent.isOnNavMesh) return;

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
                agent.isStopped = false;
                wanderTimeoutTimer += Time.deltaTime;

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
        if (player == null || !agent.isOnNavMesh)
        {
            ChangeState(EnemyState.Wander);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && HasLineOfSight())
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        if (HasLineOfSight() && distanceToPlayer <= visionRadiusNormal)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            ChangeState(EnemyState.Wander);
        }
    }

    private void UpdateAttack()
    {
        if (player == null || !agent.isOnNavMesh)
        {
            ChangeState(EnemyState.Wander);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange * 1.3f || !HasLineOfSight())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        agent.isStopped = true;

        Vector3 direction = (player.position - transform.position);
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }

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

        if (distanceToPlayer <= backDetectionRadius) return true;

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
        if (TimeManager.Instance == null) return true;
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

    // ===================================================================================
    //  SISTEMA DE TRAMPAS 
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
        agent.speed = velocidadOriginalAgent * 0.55f; 
        yield return new WaitForSeconds(4f); 
        agent.speed = velocidadOriginalAgent;
    }

   private IEnumerator EfectoRed()
    {
        Debug.Log("Enemigo atrapado en Red.");
        
        isImmobilized = true; 
        if(agent != null && agent.isOnNavMesh) agent.isStopped = true; 
        
        yield return new WaitForSeconds(5f); 
        
        isImmobilized = false; 
        // No necesitamos reactivarlo manualmente aquí. 
        // Al volver isImmobilized a false, el Update volverá a correr y la máquina de estados
        // decidirá si debe perseguir o deambular en el siguiente frame.
    }

    private IEnumerator EfectoMadera(Vector3 posicionPuerta)
    {
        Debug.Log("Enemigo empujado por trampa de Madera.");
        
        agent.enabled = false;
        rbEnemigo.isKinematic = false;

        Vector3 direccionEmpuje = (transform.position - posicionPuerta).normalized;
        direccionEmpuje.y = 0.5f; 
        
        rbEnemigo.AddForce(direccionEmpuje * 15f, ForceMode.Impulse);

        yield return new WaitForSeconds(1.5f);
        
        rbEnemigo.linearVelocity = Vector3.zero; 
        rbEnemigo.isKinematic = true; 
        agent.enabled = true; 
    }
}