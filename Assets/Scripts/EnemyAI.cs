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
    [Tooltip("Segundos que mantiene la persecución tras perder línea de visión.")]
    public float chaseMemoryDuration = 3.5f;
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

    // Sistema Anti-Stuck / Anti-Stall y memoria de persecución
    private bool initializedComponents = false;
    private float chaseStuckTimer = 0f;
    private float chaseRepathTimer = 0f;
    private float chaseLostSightTimer = 0f;
    private Vector3 lastChaseSampledPos = Vector3.zero;
    private Vector3 lastKnownPlayerPos = Vector3.zero;
    private bool hasLastKnownPlayerPos = false;

    // Estado persistente de ralentización por trampas y materiales del cono visual
    private float trapSpeedMultiplier = 1f;
    private float trapSlowRemaining = 0f;
    private GameObject spikeSlowVfxObj;
    private Material visionConeMaterial;
    private Material radiusRingMaterial;

    protected virtual void Awake()
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
        EnsureInitialized();
        FindPlayerIfNeeded();
        SubscribeToTimeManager();

        if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
        {
            if (agent != null)
            {
                agent.speed = wanderSpeed;
            }
            ChangeState(EnemyState.Wander);
        }
    }

    private void EnsureInitialized()
    {
        if (initializedComponents) return;
        initializedComponents = true;

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

    protected virtual void OnEnable()
    {
        SubscribeToTimeManager();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeFromTimeManager();
    }

    private void Update()
    {
        TickTrapSpeedModifier();
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
                    agent.speed = GetCurrentMovementSpeed(false);
                    agent.isStopped = false;
                }
                if (previous == EnemyState.Chase || previous == EnemyState.Attack)
                {
                    OnPlayerLostFromChase?.Invoke();
                }
                break;

            case EnemyState.Chase:
                chaseStuckTimer = 0f;
                chaseRepathTimer = 0f;
                chaseLostSightTimer = 0f;
                lastChaseSampledPos = Vector3.zero;
                if (player != null)
                {
                    lastKnownPlayerPos = player.position;
                    hasLastKnownPlayerPos = true;
                }
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.speed = GetCurrentMovementSpeed(true);
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
        EnsureInitialized();
        FindPlayerIfNeeded();
        chaseLostSightTimer = 0f;
        ChangeState(EnemyState.Chase);
        if (player != null)
        {
            lastKnownPlayerPos = player.position;
            hasLastKnownPlayerPos = true;
        }
        if (player != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = GetCurrentMovementSpeed(true);
            Vector3 targetNavPos = player.position;
            if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                targetNavPos = hit.position;
            }
            agent.SetDestination(targetNavPos);
        }
    }

    public void SetPatrolWaypoints(Vector3[] waypoints)
    {
        patrolWaypoints = waypoints;
        currentWaypointIndex = 0;
        usePatrolWaypoints = waypoints != null && waypoints.Length > 0;
        hasWanderDestination = false;
        chaseLostSightTimer = 0f;
        hasLastKnownPlayerPos = false;
        if (usePatrolWaypoints && currentState != EnemyState.Wander)
        {
            ChangeState(EnemyState.Wander);
        }
    }

    // --- LÓGICA DE CADA ESTADO ---

    private void UpdateWander()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        agent.speed = GetCurrentMovementSpeed(false);

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
        bool hasLos = HasLineOfSight();

        if (distanceToPlayer <= attackRange && hasLos)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        if (hasLos && distanceToPlayer <= visionRadiusNormal)
        {
            chaseLostSightTimer = 0f;
            lastKnownPlayerPos = player.position;
            hasLastKnownPlayerPos = true;
            MoveChaseTowardPosition(lastKnownPlayerPos);
            return;
        }

        if (chaseLostSightTimer < chaseMemoryDuration)
        {
            chaseLostSightTimer += Time.deltaTime;
            // Rastro cercano inicial (1.25 s) para doblar esquinas sin perder al jugador de golpe
            if (chaseLostSightTimer <= 1.25f || !hasLastKnownPlayerPos)
            {
                lastKnownPlayerPos = player.position;
                hasLastKnownPlayerPos = true;
            }
            MoveChaseTowardPosition(lastKnownPlayerPos);
            return;
        }

        ChangeState(EnemyState.Wander);
    }

    private void MoveChaseTowardPosition(Vector3 targetWorldPos)
    {
        agent.isStopped = false;
        agent.speed = GetCurrentMovementSpeed(true);

        // Muestreo seguro del destino en el NavMesh (evita enviar al agente fuera de la malla)
        Vector3 targetNavPos = targetWorldPos;
        if (NavMesh.SamplePosition(targetWorldPos, out NavMeshHit navHit, 4.0f, NavMesh.AllAreas))
        {
            targetNavPos = navHit.position;
        }

        chaseRepathTimer += Time.deltaTime;
        if (chaseRepathTimer >= 0.12f || (targetNavPos - lastChaseSampledPos).sqrMagnitude > 1.0f)
        {
            chaseRepathTimer = 0f;
            lastChaseSampledPos = targetNavPos;
            agent.SetDestination(targetNavPos);
        }

        // DETECCIÓN Y RECUPERACIÓN DE ATASCO (ANTI-STALL)
        float distToTarget = Vector3.Distance(transform.position, targetNavPos);
        bool isStuck = !agent.pathPending &&
                       agent.velocity.sqrMagnitude < 0.08f &&
                       distToTarget > attackRange + 0.35f;

        if (isStuck)
        {
            chaseStuckTimer += Time.deltaTime;
            if (chaseStuckTimer >= 0.8f)
            {
                chaseStuckTimer = 0f;
                RecoverFromChaseStuck(targetNavPos);
            }
        }
        else
        {
            chaseStuckTimer = Mathf.Max(0f, chaseStuckTimer - Time.deltaTime * 2f);
        }
    }

    private void RecoverFromChaseStuck(Vector3 targetNavPos)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // 1. Si el camino es parcial, forzar navegación a la esquina más avanzada
        if (agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathPartial && agent.path.corners.Length > 1)
        {
            Vector3 lastCorner = agent.path.corners[agent.path.corners.Length - 1];
            if ((lastCorner - transform.position).sqrMagnitude > 0.4f)
            {
                agent.SetDestination(lastCorner);
                return;
            }
        }

        // 2. Probar puntos laterales/frontales en abanico para sortear la esquina o borde
        Vector3 forwardDir = (targetNavPos - transform.position);
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 0.01f) forwardDir = transform.forward;
        forwardDir.Normalize();

        float[] probeAngles = new float[] { 45f, -45f, 90f, -90f, 135f, -135f };
        for (int i = 0; i < probeAngles.Length; i++)
        {
            Vector3 probeDir = Quaternion.Euler(0f, probeAngles[i], 0f) * forwardDir;
            Vector3 candidate = transform.position + probeDir * 2.2f;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit probeHit, 2.0f, NavMesh.AllAreas))
            {
                NavMeshPath probePath = new NavMeshPath();
                if (agent.CalculatePath(probeHit.position, probePath) && probePath.status != NavMeshPathStatus.PathInvalid)
                {
                    agent.ResetPath();
                    agent.SetDestination(probeHit.position);
                    return;
                }
            }
        }

        // 3. Si sigue atascado contra un borde, alinearlo con la superficie más cercana
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit snapHit, 1.5f, NavMesh.AllAreas))
        {
            if ((snapHit.position - transform.position).sqrMagnitude > 0.04f)
            {
                agent.Warp(snapHit.position);
            }
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

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (dist <= 0.05f) return true;
        dir /= dist;

        float lowGroundCutoff = Mathf.Min(transform.position.y, player.position.y) + 0.35f;

        // Usamos RaycastAll para ignorar el propio collider del enemigo, suelos/aceras y triggers
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
            if (hitTransform == player || hitTransform.IsChildOf(player)) continue;
            if (hitTransform.GetComponentInParent<EnemyAI>() != null) continue;

            string objName = hitTransform.name;
            if (objName.Contains("Road") || objName.Contains("Pavement") ||
                objName.Contains("Floor") || objName.Contains("Ground") ||
                objName.StartsWith("Guia_Callejon"))
            {
                continue;
            }

            if (hits[i].collider != null && hits[i].collider.bounds.max.y <= lowGroundCutoff)
            {
                continue;
            }

            // Hay un obstáculo sólido (edificio, muro, esquina, puerta cerrada, cobertura alta) bloqueando la línea de visión
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
        if (visionConeObj != null && visionConeMaterial != null && radiusRingMaterial != null) return;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer < 0) ignoreRaycastLayer = 2;

        if (visionConeObj == null)
        {
            visionConeObj = new GameObject("VisionCone_Visual");
            visionConeObj.layer = ignoreRaycastLayer;
            visionConeObj.transform.SetParent(transform, false);
            visionConeObj.transform.localPosition = Vector3.zero;
            visionConeObj.transform.localRotation = Quaternion.identity;

            visionConeMeshFilter = visionConeObj.AddComponent<MeshFilter>();
            visionConeRenderer = visionConeObj.AddComponent<MeshRenderer>();
            coneMesh = new Mesh { name = "EnemyVisionConeMesh" };
            visionConeMeshFilter.mesh = coneMesh;
        }

        if (visionConeMaterial == null)
        {
            visionConeMaterial = CreateVisionTransparentMaterial(new Color(1f, 0.85f, 0.1f, 0.30f), 3600);
        }
        if (visionConeRenderer != null)
        {
            visionConeRenderer.sharedMaterial = visionConeMaterial;
            visionConeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visionConeRenderer.receiveShadows = false;
        }

        if (radiusCircleRenderer == null)
        {
            GameObject ringObj = new GameObject("DetectionRadiusRing");
            ringObj.layer = ignoreRaycastLayer;
            ringObj.transform.SetParent(visionConeObj.transform, false);
            radiusCircleRenderer = ringObj.AddComponent<LineRenderer>();
            radiusCircleRenderer.useWorldSpace = true;
            radiusCircleRenderer.loop = true;
            radiusCircleRenderer.startWidth = 0.12f;
            radiusCircleRenderer.endWidth = 0.12f;
        }

        if (radiusRingMaterial == null)
        {
            radiusRingMaterial = CreateVisionTransparentMaterial(new Color(1f, 0.58f, 0.12f, 0.72f), 3602);
        }
        radiusCircleRenderer.sharedMaterial = radiusRingMaterial;
        radiusCircleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        radiusCircleRenderer.receiveShadows = false;
    }

    private static Material CreateVisionTransparentMaterial(Color initialColor, int renderQueue)
    {
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
        Material mat = new Material(unlitShader);
        ConfigureVisionMaterial(mat, initialColor, renderQueue);
        return mat;
    }

    private static void ConfigureVisionMaterial(Material mat, Color color, int renderQueue)
    {
        if (mat == null) return;
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        }
        mat.renderQueue = renderQueue;
        SetMaterialColor(mat, color);
    }

    private static void SetMaterialColor(Material mat, Color color)
    {
        if (mat == null) return;
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }

    public void ResetVisionConeVisual()
    {
        trapSpeedMultiplier = 1f;
        trapSlowRemaining = 0f;
        EnsureSpikeSlowVfx(false);

        displayedVisionRadius = visionRadiusNormal;
        hasWanderDestination = false;
        wanderWaitTimer = 0f;
        wanderTimeoutTimer = 0f;
        ChangeState(EnemyState.Wander);

        EnsureVisionConeObjects();
        ConfigureVisionMaterial(visionConeMaterial, new Color(1f, 0.85f, 0.15f, 0.30f), 3600);
        ConfigureVisionMaterial(radiusRingMaterial, new Color(1f, 0.58f, 0.12f, 0.72f), 3602);
        if (visionConeRenderer != null) visionConeRenderer.sharedMaterial = visionConeMaterial;
        if (radiusCircleRenderer != null) radiusCircleRenderer.sharedMaterial = radiusRingMaterial;
        UpdateVisionConeVisual();
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
            ? new Color(1f, 0.22f, 0.18f, 0.36f)
            : (isCrouched ? new Color(0.2f, 0.92f, 0.55f, 0.28f) : new Color(1f, 0.84f, 0.18f, 0.30f));

        Color ringColor = isChasing
            ? new Color(1f, 0.28f, 0.2f, 0.78f)
            : new Color(1f, 0.58f, 0.12f, 0.72f);

        SetMaterialColor(visionConeMaterial, coneColor);
        SetMaterialColor(radiusRingMaterial, ringColor);

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
            Color ringCol = isChasing
                ? new Color(1f, 0.2f, 0.2f, 0.85f)
                : (isCrouched ? new Color(0.25f, 0.95f, 0.6f, 0.8f) : new Color(1f, 0.65f, 0.15f, 0.75f));
            SetMaterialColor(radiusRingMaterial, ringCol);

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

    private float GetCurrentMovementSpeed(bool chasing)
    {
        return (chasing ? chaseSpeed : wanderSpeed) * trapSpeedMultiplier;
    }

    private void TickTrapSpeedModifier()
    {
        if (trapSlowRemaining <= 0f) return;

        trapSlowRemaining -= Time.deltaTime;
        if (trapSlowRemaining <= 0f)
        {
            trapSlowRemaining = 0f;
            trapSpeedMultiplier = 1f;
            EnsureSpikeSlowVfx(false);
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = GetCurrentMovementSpeed(currentState == EnemyState.Chase);
        }
    }

    private void EnsureSpikeSlowVfx(bool active)
    {
        if (!active)
        {
            if (spikeSlowVfxObj != null) spikeSlowVfxObj.SetActive(false);
            return;
        }

        if (spikeSlowVfxObj == null)
        {
            spikeSlowVfxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spikeSlowVfxObj.name = "SpikeSlowAura_VFX";
            spikeSlowVfxObj.transform.SetParent(transform, false);
            spikeSlowVfxObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            float invScale = 1f / Mathf.Max(0.01f, transform.lossyScale.x);
            spikeSlowVfxObj.transform.localScale = new Vector3(1.5f * invScale, 0.08f * invScale, 1.5f * invScale);
            Collider col = spikeSlowVfxObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Renderer ren = spikeSlowVfxObj.GetComponent<Renderer>();
            if (ren != null)
            {
                ren.sharedMaterial = CreateVisionTransparentMaterial(new Color(0.25f, 0.78f, 1f, 0.52f), 3605);
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        spikeSlowVfxObj.SetActive(true);
    }

    private void SpawnTrapImpactParticles(Color color)
    {
        GameObject fx = new GameObject("TrapImpactSparks");
        fx.transform.position = transform.position + Vector3.up * 0.45f;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 4.2f;
        main.startSize = 0.22f;
        main.startColor = color;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;
        ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null)
            {
                Material m = new Material(s);
                SetMaterialColor(m, color);
                pr.sharedMaterial = m;
            }
        }
        ps.Play();
        Destroy(fx, 1.5f);
    }

    private IEnumerator EfectoPuas()
    {
        Debug.Log("Enemigo pisó Púas. Velocidad reducida un 45%.");
        trapSpeedMultiplier = 0.55f;
        trapSlowRemaining = 5f;
        EnsureSpikeSlowVfx(true);
        SpawnTrapImpactParticles(new Color(0.25f, 0.82f, 1f, 1f));

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.speed = GetCurrentMovementSpeed(currentState == EnemyState.Chase);
        }
        yield break;
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
