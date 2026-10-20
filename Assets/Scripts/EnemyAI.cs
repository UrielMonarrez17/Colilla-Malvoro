using UnityEngine;
using UnityEngine.AI;
using System;
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
    private Animator animator;

    [Header("Visión y Sigilo")]
    public float visionRadiusNormal = 15f;
    public float visionRadiusCrouched = 5f;
    public float backDetectionRadius = 2f; 
    [Range(0, 360)] 
    public float visionAngle = 90f;
    
    [Tooltip("Capas que bloquean la vista (Paredes, Casas)")]
    public LayerMask obstacleMask; 

    [Header("Visualización del Cono de Visión (Tutorial / Sigilo)")]
    [Tooltip("Muestra en el suelo el cono visual y cómo se reduce el radio de detección al agacharse (C).")]
    public bool showVisionCone = false;
    private GameObject visionConeObj;
    private MeshFilter visionConeMeshFilter;
    private MeshRenderer visionConeRenderer;
    private LineRenderer radiusCircleRenderer;
    private Mesh coneMesh;
    private float displayedVisionRadius;

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

    // Patrulla / Control de Tutorial
    private Vector3[] patrolWaypoints;
    private int currentWaypointIndex = 0;
    private bool usePatrolWaypoints = false;

    // Eventos para integración con Tutorial
    public event Action<EnemyState> OnEnemyStateChanged;
    public event Action OnPlayerLostFromChase;
    public event Action<TipoTrampa> OnTrapEffectApplied;

    // Variables internas
    private float wanderWaitTimer = 0f;
    private float wanderTimeoutTimer = 0f;
    private bool hasWanderDestination = false;
    private float attackTimer = 0f;
    private bool isSubscribedToTime = false;
    private bool hasSpeedParam = false;
    private bool hasVertParam = false;
    private bool hasStateParam = false;

    private void Awake()
    {
        // Desactivar AnimationShowcase o MovePlayerInput si vienen del paquete Animals_FREE
        // para que las teclas W/S del jugador no controlen las animaciones del enemigo
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] == null) continue;
            string typeName = scripts[i].GetType().Name;
            if (typeName == "AnimationShowcase" || typeName == "MovePlayerInput" || typeName == "CreatureMover")
            {
                scripts[i].enabled = false;
            }
        }

        if (!gameObject.CompareTag("Enemy"))
        {
            gameObject.tag = "Enemy";
        }
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rbEnemigo = GetComponent<Rigidbody>(); 
        if (rbEnemigo != null)
        {
            rbEnemigo.isKinematic = true;
            rbEnemigo.useGravity = false;
        }

        animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();

        velocidadOriginalAgent = chaseSpeed;
        displayedVisionRadius = visionRadiusNormal;
        
        if (agent != null)
        {
            agent.speed = wanderSpeed;
        }

        if (obstacleMask.value == 0)
        {
            // Por defecto, bloquear visión con Default (0), excepto Ignore Raycast (2)
            obstacleMask = ~LayerMask.GetMask("Ignore Raycast", "UI");
        }

        FindPlayerIfNeeded();
        SubscribeToTimeManager();

        ChangeState(EnemyState.Wander);
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "Speed") hasSpeedParam = true;
            else if (param.name == "Vert") hasVertParam = true;
            else if (param.name == "State") hasStateParam = true;
        }
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
        UpdateAnimatorFromMovement();
        UpdateVisionConeVisual();

        // ¡CANDADO MAESTRO!
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
        if (playerController != null && playerController.IsDead)
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

    private void UpdateAnimatorFromMovement()
    {
        if (animator == null) return;

        float currentSpeed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped && !isImmobilized)
        {
            currentSpeed = agent.velocity.magnitude;
        }

        float normalizedAnim = 0f;
        if (currentSpeed > 0.15f)
        {
            normalizedAnim = currentState == EnemyState.Chase ? 1f : 0.5f;
        }

        if (hasSpeedParam)
        {
            animator.SetFloat("Speed", normalizedAnim, 0.1f, Time.deltaTime);
        }
        if (hasVertParam)
        {
            animator.SetFloat("Vert", currentSpeed > 0.15f ? 1f : 0f, 0.1f, Time.deltaTime);
        }
        if (hasStateParam)
        {
            animator.SetFloat("State", currentState == EnemyState.Chase ? 1f : 0f, 0.1f, Time.deltaTime);
        }
    }

    // --- MÁQUINA DE ESTADOS ---

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        EnemyState previous = currentState;

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
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.speed = wanderSpeed;
                    agent.isStopped = false;
                }
                if (previous == EnemyState.Chase || previous == EnemyState.Attack)
                {
                    OnPlayerLostFromChase?.Invoke();
                }
                break;

            case EnemyState.Chase:
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.speed = chaseSpeed;
                    velocidadOriginalAgent = chaseSpeed;
                    agent.isStopped = false;
                }
                break;

            case EnemyState.Attack:
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                attackTimer = attackCooldown; 
                break;
        }

        OnEnemyStateChanged?.Invoke(currentState);
    }

    public void ForceChasePlayer()
    {
        FindPlayerIfNeeded();
        ChangeState(EnemyState.Chase);
        if (player != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
    }

    public void SetPatrolWaypoints(Vector3[] waypoints)
    {
        patrolWaypoints = waypoints;
        currentWaypointIndex = 0;
        usePatrolWaypoints = waypoints != null && waypoints.Length > 0;
        hasWanderDestination = false;
    }

    // --- LÓGICA DE CADA ESTADO ---

    private void UpdateWander()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.speed = wanderSpeed;

        if (usePatrolWaypoints && patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            if (!hasWanderDestination)
            {
                Vector3 target = patrolWaypoints[currentWaypointIndex];
                agent.isStopped = false;
                agent.SetDestination(target);
                hasWanderDestination = true;
                wanderWaitTimer = 0f;
            }
            else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f)
            {
                agent.isStopped = true;
                wanderWaitTimer += Time.deltaTime;
                if (wanderWaitTimer >= wanderWaitTime)
                {
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                    hasWanderDestination = false;
                    wanderWaitTimer = 0f;
                }
            }
            return;
        }

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
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
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
            agent.SetDestination(player.position);
        }
        else
        {
            ChangeState(EnemyState.Wander);
        }
    }

    private void UpdateAttack()
    {
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
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

    public float GetCurrentEffectiveVisionRadius()
    {
        return (playerController != null && playerController.isCrouching) 
            ? visionRadiusCrouched 
            : visionRadiusNormal;
    }

    private bool CanDetectPlayerInitial()
    {
        if (player == null) return false;

        float currentVisionRadius = GetCurrentEffectiveVisionRadius();
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= backDetectionRadius)
        {
            if (HasLineOfSight()) return true;
        }

        if (distanceToPlayer <= currentVisionRadius)
        {
            Vector3 directionToPlayer = (player.position - transform.position);
            directionToPlayer.y = 0f;
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                directionToPlayer.Normalize();
                if (Vector3.Angle(transform.forward, directionToPlayer) < visionAngle / 2f)
                {
                    if (HasLineOfSight())
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 0.6f;
        Vector3 target = player.position + Vector3.up * 0.4f;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (dist <= 0.05f) return true;
        dir /= dist;

        // Usamos RaycastAll para ignorar el propio collider del enemigo y triggers (como zonas del tutorial o contenedores)
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
            if (hitTransform == player || hitTransform.IsChildOf(player)) continue;
            if (hitTransform.GetComponentInParent<EnemyAI>() != null) continue;

            // Hay un obstáculo sólido (edificio, muro, esquina, puerta cerrada) bloqueando la línea de visión
            return false;
        }

        return true;
    }

    private bool GetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + UnityEngine.Random.insideUnitSphere * radius;
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

    // --- VISUALIZACIÓN DEL CONO DE VISIÓN Y RADIO DE DETECCIÓN ---

    private void EnsureVisionConeObjects()
    {
        if (visionConeObj != null) return;

        visionConeObj = new GameObject("VisionCone_Visual");
        visionConeObj.transform.SetParent(transform, false);
        visionConeObj.transform.localPosition = Vector3.zero;
        visionConeObj.transform.localRotation = Quaternion.identity;

        visionConeMeshFilter = visionConeObj.AddComponent<MeshFilter>();
        visionConeRenderer = visionConeObj.AddComponent<MeshRenderer>();
        coneMesh = new Mesh { name = "EnemyVisionConeMesh" };
        visionConeMeshFilter.mesh = coneMesh;

        Shader unlitShader = Shader.Find("Sprites/Default");
        if (unlitShader == null) unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        Material coneMat = new Material(unlitShader);
        coneMat.color = new Color(1f, 0.85f, 0.1f, 0.25f);
        visionConeRenderer.material = coneMat;
        visionConeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visionConeRenderer.receiveShadows = false;

        GameObject ringObj = new GameObject("DetectionRadiusRing");
        ringObj.transform.SetParent(visionConeObj.transform, false);
        radiusCircleRenderer = ringObj.AddComponent<LineRenderer>();
        radiusCircleRenderer.useWorldSpace = true;
        radiusCircleRenderer.loop = true;
        radiusCircleRenderer.startWidth = 0.12f;
        radiusCircleRenderer.endWidth = 0.12f;
        Material lineMat = new Material(unlitShader);
        lineMat.color = new Color(1f, 0.55f, 0.1f, 0.7f);
        radiusCircleRenderer.material = lineMat;
        radiusCircleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void UpdateVisionConeVisual()
    {
        bool shouldDisplay = showVisionCone && IsNight();
        if (!shouldDisplay)
        {
            if (visionConeObj != null && visionConeObj.activeSelf)
            {
                visionConeObj.SetActive(false);
            }
            return;
        }

        EnsureVisionConeObjects();
        if (!visionConeObj.activeSelf) visionConeObj.SetActive(true);

        // Compensar la escala del padre para dibujar en metros reales del mundo
        float parentScale = Mathf.Max(0.01f, transform.lossyScale.x);
        visionConeObj.transform.localScale = Vector3.one / parentScale;

        float targetRadius = GetCurrentEffectiveVisionRadius();
        displayedVisionRadius = Mathf.Lerp(displayedVisionRadius, targetRadius, Time.deltaTime * 8f);

        bool isChasing = currentState == EnemyState.Chase || currentState == EnemyState.Attack;
        bool isCrouched = playerController != null && playerController.isCrouching;

        Color coneColor = isChasing 
            ? new Color(1f, 0.18f, 0.18f, 0.34f) 
            : (isCrouched ? new Color(0.2f, 0.9f, 0.55f, 0.26f) : new Color(1f, 0.82f, 0.15f, 0.28f));

        if (visionConeRenderer != null && visionConeRenderer.material != null)
        {
            visionConeRenderer.material.color = coneColor;
        }

        // Construir geometría del cono con Raycasts para que se recorte contra las paredes/esquinas
        int segments = 28;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        Vector3 originWorld = transform.position + Vector3.up * 0.18f;
        vertices[0] = new Vector3(0f, 0.18f, 0f);

        float halfAngle = visionAngle * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angleDeg = -halfAngle + t * visionAngle;
            Vector3 dirWorld = Quaternion.Euler(0f, angleDeg, 0f) * transform.forward;
            float reach = displayedVisionRadius;

            RaycastHit[] hits = Physics.RaycastAll(originWorld + Vector3.up * 0.3f, dirWorld, displayedVisionRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int h = 0; h < hits.Length; h++)
            {
                Transform ht = hits[h].transform;
                if (ht == transform || ht.IsChildOf(transform)) continue;
                if (player != null && (ht == player || ht.IsChildOf(player))) continue;
                if (ht.GetComponentInParent<EnemyAI>() != null) continue;
                if (hits[h].distance < reach) reach = hits[h].distance;
            }

            Vector3 localDir = Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward;
            vertices[i + 1] = localDir * reach + new Vector3(0f, 0.18f, 0f);

            if (i < segments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();

        // Actualizar anillo perimetral que muestra el radio de detección (se encoge al agacharse con C)
        if (radiusCircleRenderer != null)
        {
            int ringSteps = 40;
            radiusCircleRenderer.positionCount = ringSteps;
            Color ringColor = isChasing 
                ? new Color(1f, 0.2f, 0.2f, 0.85f) 
                : (isCrouched ? new Color(0.25f, 0.95f, 0.6f, 0.8f) : new Color(1f, 0.65f, 0.15f, 0.75f));
            radiusCircleRenderer.material.color = ringColor;

            for (int i = 0; i < ringSteps; i++)
            {
                float rad = (float)i / ringSteps * Mathf.PI * 2f;
                Vector3 pos = originWorld + new Vector3(Mathf.Cos(rad) * displayedVisionRadius, 0f, Mathf.Sin(rad) * displayedVisionRadius);
                radiusCircleRenderer.SetPosition(i, pos);
            }
        }
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
        // IMPORTANTE: Si la referencia serializada apunta a un objeto inactivo (ej. Hamster desactivado en Tutorial.unity),
        // buscamos el objeto activo con Tag "Player" (Mesh_Hamster).
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && playerObj.activeInHierarchy)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<HamsterController>();
            }
            else
            {
                HamsterController activeHamster = FindAnyObjectByType<HamsterController>();
                if (activeHamster != null && activeHamster.gameObject.activeInHierarchy)
                {
                    player = activeHamster.transform;
                    playerController = activeHamster;
                }
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
        OnTrapEffectApplied?.Invoke(tipo);

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
        Debug.Log("Enemigo pisó Púas. Velocidad reducida un 45%.");
        if (agent != null)
        {
            float baseSpd = currentState == EnemyState.Chase ? chaseSpeed : wanderSpeed;
            agent.speed = baseSpd * 0.55f; 
        }
        yield return new WaitForSeconds(4f); 
        if (agent != null)
        {
            agent.speed = currentState == EnemyState.Chase ? chaseSpeed : wanderSpeed;
        }
    }

    private IEnumerator EfectoRed()
    {
        Debug.Log("Enemigo atrapado en Red por 5 segundos.");
        
        isImmobilized = true; 
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true; 
        
        yield return new WaitForSeconds(5f); 
        
        isImmobilized = false; 
    }

    private IEnumerator EfectoMadera(Vector3 posicionPuerta)
    {
        Debug.Log("Enemigo empujado por trampa de Madera.");
        
        if (agent != null) agent.enabled = false;
        if (rbEnemigo != null)
        {
            rbEnemigo.isKinematic = false;
            rbEnemigo.useGravity = true;

            Vector3 direccionEmpuje = (transform.position - posicionPuerta).normalized;
            if (direccionEmpuje.sqrMagnitude < 0.01f) direccionEmpuje = -transform.forward;
            direccionEmpuje.y = 0.35f; 
            
            rbEnemigo.AddForce(direccionEmpuje * 12f, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(1.5f);
        
        if (rbEnemigo != null)
        {
            rbEnemigo.linearVelocity = Vector3.zero; 
            rbEnemigo.isKinematic = true; 
            rbEnemigo.useGravity = false;
        }
        if (agent != null) agent.enabled = true; 
    }
}
