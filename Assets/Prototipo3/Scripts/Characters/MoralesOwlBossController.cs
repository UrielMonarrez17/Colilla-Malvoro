using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Prototipo3.Narrative;

namespace Prototipo3
{
    /// <summary>
    /// Controlador de Combate e IA Táctica para el Jefe Rector H. Morales ("El Silenciador" — BossProfileType.Morales_Buho).
    /// Ejecuta patrones de sigilo acústico, escaneo centinela en 300° y 3 ataques variados (Vuelo Rasante, Chirrido Ultrasónico, Ráfaga de Plumas).
    /// </summary>
    public class MoralesOwlBossController : MonoBehaviour
    {
        public enum BossAttackType
        {
            None,
            SilentTalonSwoop,
            SonicScreechWave,
            FeatherGaleBarrage
        }

        [Header("Tiempos de Reutilización (Cooldowns)")]
        [Tooltip("Cooldown de Vuelo Rasante de Garras (segundos)")]
        public float talonSwoopCooldown = 6.5f;
        [Tooltip("Cooldown de Chirrido Ultrasónico (segundos)")]
        public float sonicScreechCooldown = 8.0f;
        [Tooltip("Cooldown de Ráfaga de Plumas Cortantes (segundos)")]
        public float featherBarrageCooldown = 5.5f;

        [Header("Configuración de Ataque 1: Vuelo Rasante de Garras")]
        public float talonSwoopSpeed = 18.5f;
        public float talonWindupDuration = 0.85f;
        public float talonLockDirectionTime = 0.45f;
        public float talonSwoopDuration = 0.45f;
        public float talonRecoveryDuration = 0.85f;
        public float talonHitRadius = 1.05f;

        [Header("Configuración de Ataque 2: Chirrido Ultrasónico")]
        public float screechWindupDuration = 0.95f;
        public float screechExecutionDuration = 0.55f;
        public float screechRecoveryDuration = 0.35f;
        public float screechMaxRadius = 10.5f;
        public float screechStaminaDrain = 45f;
        public float screechPointBlankRadius = 2.4f;

        [Header("Configuración de Ataque 3: Ráfaga de Plumas")]
        public float featherWindupDuration = 0.65f;
        public float featherRecoveryDuration = 0.45f;
        public float featherProjectileSpeed = 13.0f;
        public float featherLifetime = 1.6f;

        [Header("Estado Interno")]
        [SerializeField] private BossAttackType currentAttack = BossAttackType.None;
        [SerializeField] private int attackPhase = 0; // 0 = windup, 1 = execution, 2 = recovery
        [SerializeField] private float attackPhaseTimer = 0f;
        [SerializeField] private bool isPerchScanning = false;
        [SerializeField] private float perchIntervalTimer = 0f;
        [SerializeField] private float perchScanTimer = 0f;
        [SerializeField] private float acousticBoostTimer = 0f;

        // Referencias a componentes base
        private EnemyAIPrototipo3 ai;
        private DualRoleCharacter dualRole;
        private NavMeshAgent agent;
        private Transform visualMesh;

        // Timers de Cooldown
        private float talonSwoopCooldownTimer = 2.0f;
        private float sonicScreechCooldownTimer = 4.0f;
        private float featherBarrageCooldownTimer = 6.0f;

        // Variables de Vuelo Rasante
        private Vector3 lockedSwoopDir = Vector3.forward;
        private float swoopTargetDist = 8f;
        private bool swoopHasDealtDamage = false;
        private float currentSwoopRecovery = 0.85f;
        private GameObject swoopLaneTelegraph;
        private Material swoopLaneMaterial;

        // Variables de Chirrido
        private GameObject screechGroundCircle;
        private GameObject screechContractingRing;

        // Variables de Ráfaga de Plumas
        private GameObject[] featherAimLines;

        // Aro visual sobre la cabeza para acústica
        private GameObject acousticHeadRingObj;
        private float acousticRingTimer = 0f;

        // Registro de objetos y materiales generados para limpieza sin memory leaks
        private readonly List<GameObject> activeTelegraphs = new List<GameObject>();
        private readonly List<Material> createdMaterials = new List<Material>();

        public bool IsPerformingSpecialAction => currentAttack != BossAttackType.None || isPerchScanning;

        private void Awake()
        {
            ai = GetComponent<EnemyAIPrototipo3>();
            dualRole = GetComponent<DualRoleCharacter>();
            agent = GetComponent<NavMeshAgent>();
            ResolveVisualMesh();
        }

        private void OnDisable()
        {
            InterruptActiveAttack();
        }

        private void OnDestroy()
        {
            InterruptActiveAttack();
            if (acousticHeadRingObj != null)
            {
                Destroy(acousticHeadRingObj);
            }
        }

        public void Initialize(EnemyAIPrototipo3 aiRef, DualRoleCharacter dualRoleRef)
        {
            ai = aiRef ?? GetComponent<EnemyAIPrototipo3>();
            dualRole = dualRoleRef ?? GetComponent<DualRoleCharacter>();
            agent = GetComponent<NavMeshAgent>();
            ResolveVisualMesh();

            talonSwoopCooldownTimer = 2.0f;
            sonicScreechCooldownTimer = 3.5f;
            featherBarrageCooldownTimer = 5.0f;
        }

        private void ResolveVisualMesh()
        {
            if (visualMesh != null) return;

            visualMesh = transform.Find("VisualMesh_ReplaceMe");
            if (visualMesh == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.Contains("VisualMesh"))
                    {
                        visualMesh = child;
                        break;
                    }
                }
            }

            if (visualMesh == null)
            {
                GameObject vmGo = new GameObject("VisualMesh_ReplaceMe");
                vmGo.transform.SetParent(transform, false);
                vmGo.transform.localPosition = Vector3.zero;
                visualMesh = vmGo.transform;
            }
        }

        /// <summary>
        /// Se invoca cada frame desde EnemyAIPrototipo3 para ejecutar la IA del Jefe Morales.
        /// Retorna true si Morales está ejecutando una acción especial que controla NavMeshAgent,
        /// o false si la navegación normal (Wander/Chase) debe continuar.
        /// </summary>
        public bool TryTickBossAI(EnemyAIPrototipo3 aiRef, NavMeshAgent agentRef, HamsterController playerHC, bool isFeral)
        {
            if (ai == null) ai = aiRef ?? GetComponent<EnemyAIPrototipo3>();
            if (agent == null) agent = agentRef ?? GetComponent<NavMeshAgent>();
            if (dualRole == null) dualRole = GetComponent<DualRoleCharacter>();
            ResolveVisualMesh();

            // Chequeo de interrupción global de jugabilidad (menús, cinemáticas, respawn o muerte)
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay ||
                (CutsceneManager.Instance != null && CutsceneManager.Instance.IsPlayingCutscene) ||
                (RespawnManagerPrototipo3.Instance != null && RespawnManagerPrototipo3.Instance.IsRespawning) ||
                playerHC == null || playerHC.IsDead)
            {
                InterruptActiveAttack();
                return false;
            }

            // Chequeo de estado feral y pacificación
            if (!isFeral || (dualRole != null && dualRole.isPacified))
            {
                InterruptActiveAttack();
                return false;
            }

            // Efectos de Trampas Activas sobre Morales
            if (ai != null)
            {
                // Sobrecarga en Señuelo Acústico: sacude la cabeza y cancela ataques
                if (ai.IsDistractedByDecoy)
                {
                    InterruptActiveAttack();
                    if (visualMesh != null)
                    {
                        float yawWobble = Mathf.Sin(Time.time * 26f) * 24f;
                        visualMesh.localRotation = Quaternion.Euler(0f, yawWobble, 0f);
                    }
                    return false;
                }

                // Trampas inmovilizadoras / eléctricas
                if (ai.IsParalyzedByStatic || ai.IsNetImmobilized)
                {
                    InterruptActiveAttack();
                    return false;
                }
            }

            // Actualizar aro sonoro sobre la cabeza si está activo
            UpdateAcousticHeadRing();

            // Si hay un ataque especial activo, se procesa su máquina de estados
            if (currentAttack != BossAttackType.None)
            {
                TickActiveAttack(playerHC);
                return true;
            }

            // Decrementar cooldowns de ataques
            if (talonSwoopCooldownTimer > 0f) talonSwoopCooldownTimer -= Time.deltaTime;
            if (sonicScreechCooldownTimer > 0f) sonicScreechCooldownTimer -= Time.deltaTime;
            if (featherBarrageCooldownTimer > 0f) featherBarrageCooldownTimer -= Time.deltaTime;

            // Pivote Acústico: detección de carrera ruidosa de Kilo
            CheckAcousticStalkerPivot(playerHC);

            // Manejo por estado base de EnemyAI
            EnemyAI.EnemyState state = ai != null ? ai.CurrentState : EnemyAI.EnemyState.Wander;

            if (state == EnemyAI.EnemyState.Chase || state == EnemyAI.EnemyState.Attack)
            {
                // Si estaba en escaneo centinela, se cancela al detectar/perseguir
                if (isPerchScanning)
                {
                    EndPerchScan();
                }

                // Evaluar si se puede detonar uno de los 3 ataques especiales
                if (TryTriggerBossAttack(playerHC))
                {
                    return true;
                }

                // Si no hay ataque activo, Morales planea en ras de suelo con oscilación y tilt
                ApplySilentLowAltitudeGlide();
                return false;
            }
            else // EnemyAI.EnemyState.Wander
            {
                return TickSentinelPerchScan();
            }
        }

        // ===================================================================================
        //  PATRONES DE MOVIMIENTO Y COMPORTAMIENTO
        // ===================================================================================

        /// <summary>
        /// Posado Centinela y Escaneo 300°: en Wander feral, cada 4.5s pausa por 1.8s,
        /// eleva la malla a 0.45m y rota bruscamente la cabeza cada 0.55s alternando ±105°.
        /// </summary>
        private bool TickSentinelPerchScan()
        {
            if (isPerchScanning)
            {
                perchScanTimer -= Time.deltaTime;

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                if (visualMesh != null)
                {
                    visualMesh.localPosition = new Vector3(0f, 0.45f, 0f);
                    int snapStep = Mathf.FloorToInt((1.8f - perchScanTimer) / 0.55f);
                    float targetYaw = (snapStep % 2 == 0) ? 105f : -105f;
                    visualMesh.localRotation = Quaternion.Euler(0f, targetYaw, 0f);
                }

                if (dualRole != null && dualRole.feralEyeLight != null)
                {
                    float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                    dualRole.feralEyeLight.intensity = Mathf.Lerp(2.8f, 4.5f, pulse);
                }

                if (perchScanTimer <= 0f)
                {
                    EndPerchScan();
                }

                return true;
            }
            else
            {
                perchIntervalTimer += Time.deltaTime;
                if (perchIntervalTimer >= 4.5f)
                {
                    StartPerchScan();
                    return true;
                }

                // Retorno suave a la posición neutra en el suelo
                if (visualMesh != null)
                {
                    visualMesh.localPosition = Vector3.Lerp(visualMesh.localPosition, Vector3.zero, Time.deltaTime * 5f);
                    visualMesh.localRotation = Quaternion.Slerp(visualMesh.localRotation, Quaternion.identity, Time.deltaTime * 5f);
                }

                return false;
            }
        }

        private void StartPerchScan()
        {
            isPerchScanning = true;
            perchScanTimer = 1.8f;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        private void EndPerchScan()
        {
            isPerchScanning = false;
            perchIntervalTimer = 0f;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            if (visualMesh != null)
            {
                visualMesh.localPosition = Vector3.zero;
                visualMesh.localRotation = Quaternion.identity;
            }

            if (dualRole != null && dualRole.feralEyeLight != null)
            {
                dualRole.feralEyeLight.intensity = 2.8f;
            }
        }

        /// <summary>
        /// Planeo Silencioso a Ras de Suelo: anima VisualMesh_ReplaceMe a 0.55m + oscilación senoidal con 16° de tilt.
        /// </summary>
        private void ApplySilentLowAltitudeGlide()
        {
            if (visualMesh != null)
            {
                float hoverY = 0.55f + Mathf.Sin(Time.time * 6f) * 0.12f;
                visualMesh.localPosition = new Vector3(0f, hoverY, 0f);
                visualMesh.localRotation = Quaternion.Euler(16f, 0f, 0f);
            }
        }

        /// <summary>
        /// Pivote Acústico: si Kilo corre ruidosamente en el radio sensible, Morales recibe boost de velocidad por 2s.
        /// </summary>
        private void CheckAcousticStalkerPivot(HamsterController playerHC)
        {
            if (playerHC == null || playerHC.IsDead || ai == null) return;

            bool isNoisySprint = playerHC.IsSprinting && !playerHC.isCrouching && !TrapSystemPrototipo3.IsPollenCloudActive;
            if (isNoisySprint)
            {
                float dist = Vector3.Distance(transform.position, playerHC.transform.position);
                if (dist <= ai.noisySprintDetectionRadius)
                {
                    acousticBoostTimer = 2.0f;
                    TriggerAcousticHeadRing(0.85f);

                    if (ai.CurrentState == EnemyAI.EnemyState.Wander)
                    {
                        ai.ForceChasePlayer();
                    }
                }
            }

            if (acousticBoostTimer > 0f)
            {
                acousticBoostTimer -= Time.deltaTime;
                if (agent != null && agent.enabled && (ai.CurrentState == EnemyAI.EnemyState.Chase || ai.CurrentState == EnemyAI.EnemyState.Attack))
                {
                    float boost = 1.15f;
                    if (ai.IsSpikeSlowed) boost *= 0.5f;
                    agent.speed = ai.chaseSpeed * boost;
                }
            }
        }

        private void TriggerAcousticHeadRing(float duration)
        {
            if (acousticHeadRingObj == null)
            {
                acousticHeadRingObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                acousticHeadRingObj.name = "Morales_AcousticHeadRing";
                acousticHeadRingObj.transform.SetParent(transform, false);
                acousticHeadRingObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                acousticHeadRingObj.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);

                Collider col = acousticHeadRingObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer ren = acousticHeadRingObj.GetComponent<Renderer>();
                if (ren != null)
                {
                    Material mat = CreateMaterial(new Color(1f, 0.75f, 0.15f, 0.70f));
                    ren.material = mat;
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    ren.receiveShadows = false;
                }
            }

            acousticRingTimer = duration;
            acousticHeadRingObj.SetActive(true);
        }

        private void UpdateAcousticHeadRing()
        {
            if (acousticRingTimer > 0f)
            {
                acousticRingTimer -= Time.deltaTime;
                if (acousticHeadRingObj != null)
                {
                    float pulse = 0.8f + Mathf.Sin(Time.time * 14f) * 0.15f;
                    acousticHeadRingObj.transform.localScale = new Vector3(pulse, 0.02f, pulse);
                }

                if (acousticRingTimer <= 0f && acousticHeadRingObj != null)
                {
                    acousticHeadRingObj.SetActive(false);
                }
            }
        }

        // ===================================================================================
        //  SELECCIÓN Y DETONACIÓN DE ATAQUES
        // ===================================================================================

        private bool TryTriggerBossAttack(HamsterController playerHC)
        {
            float dist = Vector3.Distance(transform.position, playerHC.transform.position);
            bool hasLOS = HasClearLineOfSight(transform.position, playerHC.transform.position);

            // Ataque 1: Vuelo Rasante (4.0m a 13.5m con línea de visión despejada)
            if (talonSwoopCooldownTimer <= 0f && dist >= 4.0f && dist <= 13.5f && hasLOS)
            {
                StartTalonSwoop(playerHC, dist);
                return true;
            }

            // Ataque 2: Chirrido Ultrasónico (<= 11.0m)
            if (sonicScreechCooldownTimer <= 0f && dist <= 11.0f)
            {
                StartSonicScreech();
                return true;
            }

            // Ataque 3: Ráfaga de Plumas (5.5m a 15.0m con línea de visión)
            if (featherBarrageCooldownTimer <= 0f && dist >= 5.5f && dist <= 15.0f && hasLOS)
            {
                StartFeatherBarrage(playerHC);
                return true;
            }

            return false;
        }

        private void TickActiveAttack(HamsterController playerHC)
        {
            switch (currentAttack)
            {
                case BossAttackType.SilentTalonSwoop:
                    TickTalonSwoop(playerHC);
                    break;
                case BossAttackType.SonicScreechWave:
                    TickSonicScreech(playerHC);
                    break;
                case BossAttackType.FeatherGaleBarrage:
                    TickFeatherBarrage(playerHC);
                    break;
            }
        }

        // ===================================================================================
        //  ATAQUE 1: VUELO RASANTE DE GARRAS (SILENT TALON SWOOP)
        // ===================================================================================

        private void StartTalonSwoop(HamsterController playerHC, float dist)
        {
            currentAttack = BossAttackType.SilentTalonSwoop;
            attackPhase = 0; // Windup
            attackPhaseTimer = 0f;
            swoopHasDealtDamage = false;
            currentSwoopRecovery = talonRecoveryDuration;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            Vector3 toPlayer = playerHC.transform.position - transform.position;
            toPlayer.y = 0f;
            lockedSwoopDir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
            swoopTargetDist = dist;

            CreateSwoopLaneTelegraph();
        }

        private void TickTalonSwoop(HamsterController playerHC)
        {
            attackPhaseTimer += Time.deltaTime;

            if (attackPhase == 0) // Windup (0.85s)
            {
                // Elevación de la malla a 1.35m
                float tRise = Mathf.Clamp01(attackPhaseTimer / talonWindupDuration);
                if (visualMesh != null)
                {
                    visualMesh.localPosition = new Vector3(0f, Mathf.Lerp(0.55f, 1.35f, tRise), 0f);
                }

                // Golden eye flare
                if (dualRole != null && dualRole.feralEyeLight != null)
                {
                    dualRole.feralEyeLight.intensity = Mathf.Lerp(2.8f, 5.0f, tRise);
                }

                // Seguimiento en los primeros 0.45s; luego se bloquea la dirección
                if (attackPhaseTimer <= talonLockDirectionTime)
                {
                    Vector3 toP = playerHC.transform.position - transform.position;
                    toP.y = 0f;
                    if (toP.sqrMagnitude > 0.01f)
                    {
                        lockedSwoopDir = toP.normalized;
                        swoopTargetDist = toP.magnitude;
                        transform.rotation = Quaternion.LookRotation(lockedSwoopDir, Vector3.up);
                    }
                    UpdateSwoopLaneTelegraph(lockedSwoopDir, swoopTargetDist, false);
                }
                else
                {
                    // Dirección bloqueada: Kilo tiene 0.40s para esquivar
                    UpdateSwoopLaneTelegraph(lockedSwoopDir, swoopTargetDist, true);
                }

                if (attackPhaseTimer >= talonWindupDuration)
                {
                    // Pasar a Fase 1: Ejecución
                    attackPhase = 1;
                    attackPhaseTimer = 0f;
                    CleanupActiveTelegraphs();
                }
            }
            else if (attackPhase == 1) // Ejecución (0.45s)
            {
                float speed = talonSwoopSpeed * (ai != null && ai.IsSpikeSlowed ? 0.5f : 1.0f);
                Vector3 moveStep = lockedSwoopDir * (speed * Time.deltaTime);

                // Chequeo de colisión frontal contra edificios o barricada de madera
                if (Physics.SphereCast(transform.position + Vector3.up * 0.6f, 0.45f, lockedSwoopDir, out RaycastHit obstHit, moveStep.magnitude + 0.35f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (IsWoodBarricadeCollider(obstHit.collider))
                    {
                        // Rebote contra barricada de madera: 1.0s de recuperación de aterrizaje
                        EnterTalonRecovery(1.0f);
                        return;
                    }
                    else if (IsBuildingOrBarricade(obstHit.collider))
                    {
                        // Detención temprana contra edificio
                        EnterTalonRecovery(talonRecoveryDuration);
                        return;
                    }
                }

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.Move(moveStep);
                }
                else
                {
                    transform.position += moveStep;
                }

                if (visualMesh != null)
                {
                    visualMesh.localPosition = new Vector3(0f, 1.35f, 0f);
                    visualMesh.localRotation = Quaternion.LookRotation(lockedSwoopDir, Vector3.up);
                }

                // Daño a Kilo si está a <= 1.05m
                if (!swoopHasDealtDamage && playerHC != null && !playerHC.IsDead)
                {
                    Vector3 delta = playerHC.transform.position - transform.position;
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= talonHitRadius * talonHitRadius)
                    {
                        swoopHasDealtDamage = true;
                        playerHC.ReceiveDamage();

                        HamsterControllerPrototipo3 protoPlayer = playerHC as HamsterControllerPrototipo3 ?? playerHC.GetComponent<HamsterControllerPrototipo3>();
                        protoPlayer?.ApplyExternalSlow(0.50f, 1.5f);
                    }
                }

                if (attackPhaseTimer >= talonSwoopDuration)
                {
                    EnterTalonRecovery(talonRecoveryDuration);
                }
            }
            else if (attackPhase == 2) // Recuperación y aterrizaje
            {
                float tLand = Mathf.Clamp01(attackPhaseTimer / currentSwoopRecovery);
                if (visualMesh != null)
                {
                    visualMesh.localPosition = Vector3.Lerp(new Vector3(0f, 1.35f, 0f), Vector3.zero, tLand);
                    visualMesh.localRotation = Quaternion.Slerp(visualMesh.localRotation, Quaternion.identity, tLand);
                }

                if (dualRole != null && dualRole.feralEyeLight != null)
                {
                    dualRole.feralEyeLight.intensity = Mathf.Lerp(5.0f, 2.8f, tLand);
                }

                if (attackPhaseTimer >= currentSwoopRecovery)
                {
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                    talonSwoopCooldownTimer = talonSwoopCooldown;
                    currentAttack = BossAttackType.None;
                    attackPhase = 0;
                }
            }
        }

        private void EnterTalonRecovery(float recoveryDuration)
        {
            attackPhase = 2;
            attackPhaseTimer = 0f;
            currentSwoopRecovery = recoveryDuration;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        private void CreateSwoopLaneTelegraph()
        {
            if (swoopLaneTelegraph != null) return;

            swoopLaneTelegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
            swoopLaneTelegraph.name = "Telegraph_Morales_TalonLane";
            Collider col = swoopLaneTelegraph.GetComponent<Collider>();
            if (col != null) Destroy(col);

            swoopLaneMaterial = CreateMaterial(new Color(1f, 0.72f, 0.12f, 0.38f));
            Renderer ren = swoopLaneTelegraph.GetComponent<Renderer>();
            if (ren != null)
            {
                ren.material = swoopLaneMaterial;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ren.receiveShadows = false;
            }

            RegisterTelegraph(swoopLaneTelegraph);
            UpdateSwoopLaneTelegraph(lockedSwoopDir, swoopTargetDist, false);
        }

        private void UpdateSwoopLaneTelegraph(Vector3 dir, float dist, bool locked)
        {
            if (swoopLaneTelegraph == null) return;

            float length = dist + 2.5f;
            Vector3 center = transform.position + dir * (length * 0.5f);
            float laneY = ai != null ? (ai.minCitySurfaceY + 0.04f) : (transform.position.y + 0.04f);

            swoopLaneTelegraph.transform.position = new Vector3(center.x, laneY, center.z);
            swoopLaneTelegraph.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            swoopLaneTelegraph.transform.localScale = new Vector3(1.3f, 0.02f, length);

            if (swoopLaneMaterial != null)
            {
                Color c = locked ? new Color(1f, 0.38f, 0.05f, 0.65f) : new Color(1f, 0.72f, 0.12f, 0.38f);
                swoopLaneMaterial.color = c;
                if (swoopLaneMaterial.HasProperty("_BaseColor")) swoopLaneMaterial.SetColor("_BaseColor", c);
            }
        }

        // ===================================================================================
        //  ATAQUE 2: CHIRRIDO ULTRASONICO (SONIC SCREECH WAVE)
        // ===================================================================================

        private void StartSonicScreech()
        {
            currentAttack = BossAttackType.SonicScreechWave;
            attackPhase = 0; // Windup (0.95s)
            attackPhaseTimer = 0f;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            CreateSonicWindupTelegraphs();
        }

        private void TickSonicScreech(HamsterController playerHC)
        {
            attackPhaseTimer += Time.deltaTime;

            if (attackPhase == 0) // Windup (0.95s)
            {
                UpdateSonicWindupTelegraphs();

                if (dualRole != null && dualRole.feralEyeLight != null)
                {
                    float pulse = Mathf.PingPong(Time.time * 9f, 1f);
                    dualRole.feralEyeLight.intensity = Mathf.Lerp(3.2f, 5.5f, pulse);
                }

                if (attackPhaseTimer >= screechWindupDuration)
                {
                    attackPhase = 1;
                    attackPhaseTimer = 0f;
                    CleanupActiveTelegraphs();

                    // Instanciar la onda expansiva de chirrido
                    SpawnSonicScreechRing();
                }
            }
            else if (attackPhase == 1) // Ejecución (0.55s)
            {
                if (attackPhaseTimer >= screechExecutionDuration)
                {
                    attackPhase = 2;
                    attackPhaseTimer = 0f;
                }
            }
            else if (attackPhase == 2) // Recuperación (0.35s)
            {
                if (attackPhaseTimer >= screechRecoveryDuration)
                {
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                    if (dualRole != null && dualRole.feralEyeLight != null)
                    {
                        dualRole.feralEyeLight.intensity = 2.8f;
                    }
                    sonicScreechCooldownTimer = sonicScreechCooldown;
                    currentAttack = BossAttackType.None;
                    attackPhase = 0;
                }
            }
        }

        private void CreateSonicWindupTelegraphs()
        {
            float surfaceY = ai != null ? (ai.minCitySurfaceY + 0.03f) : (transform.position.y + 0.03f);

            // 1. Círculo de advertencia en el suelo (radio 10.5m)
            screechGroundCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            screechGroundCircle.name = "Telegraph_Morales_ScreechWarningGround";
            Collider c1 = screechGroundCircle.GetComponent<Collider>();
            if (c1 != null) Destroy(c1);

            Material matGround = CreateMaterial(new Color(0.95f, 0.65f, 0.15f, 0.28f));
            Renderer renGround = screechGroundCircle.GetComponent<Renderer>();
            if (renGround != null)
            {
                renGround.material = matGround;
                renGround.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renGround.receiveShadows = false;
            }
            screechGroundCircle.transform.position = new Vector3(transform.position.x, surfaceY, transform.position.z);
            screechGroundCircle.transform.localScale = new Vector3(screechMaxRadius * 2f, 0.02f, screechMaxRadius * 2f);
            RegisterTelegraph(screechGroundCircle);

            // 2. Anillo que se contrae hacia Morales
            screechContractingRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            screechContractingRing.name = "Telegraph_Morales_ScreechContracting";
            Collider c2 = screechContractingRing.GetComponent<Collider>();
            if (c2 != null) Destroy(c2);

            Material matRing = CreateMaterial(new Color(0.2f, 0.85f, 1f, 0.45f));
            Renderer renRing = screechContractingRing.GetComponent<Renderer>();
            if (renRing != null)
            {
                renRing.material = matRing;
                renRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renRing.receiveShadows = false;
            }
            screechContractingRing.transform.position = new Vector3(transform.position.x, surfaceY + 0.02f, transform.position.z);
            screechContractingRing.transform.localScale = new Vector3(6f, 0.03f, 6f);
            RegisterTelegraph(screechContractingRing);
        }

        private void UpdateSonicWindupTelegraphs()
        {
            float t = Mathf.Clamp01(attackPhaseTimer / screechWindupDuration);
            float contractingRad = Mathf.Lerp(6.0f, 0.6f, t);
            float surfaceY = ai != null ? (ai.minCitySurfaceY + 0.05f) : (transform.position.y + 0.05f);

            if (screechContractingRing != null)
            {
                screechContractingRing.transform.position = new Vector3(transform.position.x, surfaceY, transform.position.z);
                screechContractingRing.transform.localScale = new Vector3(contractingRad * 2f, 0.03f, contractingRad * 2f);
            }
        }

        private void SpawnSonicScreechRing()
        {
            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;
            GameObject waveGo = new GameObject("OwlSonicScreechWave_Active");
            OwlSonicScreechRing wave = waveGo.AddComponent<OwlSonicScreechRing>();
            wave.Setup(transform.position, surfaceY, transform);
        }

        // ===================================================================================
        //  ATAQUE 3: RÁFAGA DE PLUMAS CORTANTES (FEATHER GALE BARRAGE)
        // ===================================================================================

        private void StartFeatherBarrage(HamsterController playerHC)
        {
            currentAttack = BossAttackType.FeatherGaleBarrage;
            attackPhase = 0; // Windup (0.65s)
            attackPhaseTimer = 0f;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            Vector3 toP = playerHC.transform.position - transform.position;
            toP.y = 0f;
            if (toP.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(toP.normalized, Vector3.up);
            }

            CreateFeatherFanTelegraphs(playerHC);
        }

        private void TickFeatherBarrage(HamsterController playerHC)
        {
            attackPhaseTimer += Time.deltaTime;

            if (attackPhase == 0) // Windup (0.65s)
            {
                float tRise = Mathf.Clamp01(attackPhaseTimer / featherWindupDuration);
                if (visualMesh != null)
                {
                    visualMesh.localPosition = new Vector3(0f, Mathf.Lerp(0.55f, 0.95f, tRise), 0f);
                }

                if (attackPhaseTimer >= featherWindupDuration)
                {
                    attackPhase = 1;
                    attackPhaseTimer = 0f;
                    CleanupActiveTelegraphs();

                    // Disparar las 4 plumas proyectil en abanico de 40°
                    SpawnFeatherProjectiles(playerHC);
                }
            }
            else if (attackPhase == 1) // Ejecución instantánea
            {
                attackPhase = 2;
                attackPhaseTimer = 0f;
            }
            else if (attackPhase == 2) // Recuperación (0.45s)
            {
                float tRec = Mathf.Clamp01(attackPhaseTimer / featherRecoveryDuration);
                if (visualMesh != null)
                {
                    visualMesh.localPosition = Vector3.Lerp(new Vector3(0f, 0.95f, 0f), Vector3.zero, tRec);
                }

                if (attackPhaseTimer >= featherRecoveryDuration)
                {
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                    featherBarrageCooldownTimer = featherBarrageCooldown;
                    currentAttack = BossAttackType.None;
                    attackPhase = 0;
                }
            }
        }

        private void CreateFeatherFanTelegraphs(HamsterController playerHC)
        {
            Vector3 forwardDir = (playerHC.transform.position - transform.position);
            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude < 0.01f) forwardDir = transform.forward;
            forwardDir.Normalize();

            float[] fanAngles = new float[] { -20f, -6.67f, 6.67f, 20f };
            float surfaceY = ai != null ? (ai.minCitySurfaceY + 0.04f) : (transform.position.y + 0.04f);
            featherAimLines = new GameObject[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"Telegraph_Morales_FeatherLine_{i + 1}";
                Collider col = line.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Material mat = CreateMaterial(new Color(1f, 0.82f, 0.2f, 0.38f));
                Renderer ren = line.GetComponent<Renderer>();
                if (ren != null)
                {
                    ren.material = mat;
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    ren.receiveShadows = false;
                }

                Vector3 rayDir = Quaternion.Euler(0f, fanAngles[i], 0f) * forwardDir;
                float lineLen = 15f;
                Vector3 center = transform.position + rayDir * (lineLen * 0.5f);

                line.transform.position = new Vector3(center.x, surfaceY, center.z);
                line.transform.rotation = Quaternion.LookRotation(rayDir, Vector3.up);
                line.transform.localScale = new Vector3(0.12f, 0.02f, lineLen);

                featherAimLines[i] = line;
                RegisterTelegraph(line);
            }
        }

        private void SpawnFeatherProjectiles(HamsterController playerHC)
        {
            Vector3 forwardDir = (playerHC.transform.position - transform.position);
            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude < 0.01f) forwardDir = transform.forward;
            forwardDir.Normalize();

            float[] fanAngles = new float[] { -20f, -6.67f, 6.67f, 20f };
            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;

            for (int i = 0; i < 4; i++)
            {
                Vector3 shootDir = Quaternion.Euler(0f, fanAngles[i], 0f) * forwardDir;
                Vector3 spawnPos = transform.position + shootDir * 0.75f;
                spawnPos.y = surfaceY + 0.45f;

                GameObject projGo = new GameObject($"MoralesFeatherProjectile_{i + 1}");
                projGo.transform.position = spawnPos;

                OwlFeatherProjectile proj = projGo.AddComponent<OwlFeatherProjectile>();
                proj.speed = featherProjectileSpeed;
                proj.lifetime = featherLifetime;
                proj.Setup(shootDir, surfaceY);
            }
        }

        // ===================================================================================
        //  EVENTO DE APAGÓN DEL LABORATORIO DE QUÍMICA
        // ===================================================================================

        /// <summary>
        /// Se invoca al ocurrir el apagón en la Escuela / Laboratorio de Química.
        /// Emite un anillo expansivo dorado, reubica su HomeAnchor al patio y activa el boost acústico.
        /// </summary>
        public void OnSchoolBlackoutAlert(Vector3 labWorldPos)
        {
            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;

            // Emitir anillo dorado de alerta de apagón
            GameObject alertRingGo = new GameObject("Morales_BlackoutAlertRing");
            OwlBlackoutAlertRing alertRing = alertRingGo.AddComponent<OwlBlackoutAlertRing>();
            alertRing.Setup(transform.position, surfaceY);

            // Si está lejos, desplazar su ancla territorial hacia el patio del colegio
            if (dualRole != null)
            {
                Vector3 newAnchor = Vector3.Lerp(transform.position, labWorldPos, 0.45f);
                dualRole.SetHomeAnchor(newAnchor);
            }

            // Fijar destino hacia el laboratorio de química
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(labWorldPos);
            }

            // Primar el boost de persecución acústica
            acousticBoostTimer = 4.0f;
            TriggerAcousticHeadRing(1.5f);
        }

        // ===================================================================================
        //  CANCELACIÓN Y LIMPIEZA
        // ===================================================================================

        public void InterruptActiveAttack()
        {
            currentAttack = BossAttackType.None;
            attackPhase = 0;
            attackPhaseTimer = 0f;
            isPerchScanning = false;
            perchScanTimer = 0f;

            CleanupActiveTelegraphs();

            if (visualMesh != null)
            {
                visualMesh.localPosition = Vector3.zero;
                visualMesh.localRotation = Quaternion.identity;
                visualMesh.localScale = Vector3.one;
            }

            if (dualRole != null && dualRole.feralEyeLight != null)
            {
                dualRole.feralEyeLight.intensity = 2.8f;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        private void CleanupActiveTelegraphs()
        {
            for (int i = 0; i < activeTelegraphs.Count; i++)
            {
                if (activeTelegraphs[i] != null)
                {
                    Destroy(activeTelegraphs[i]);
                }
            }
            activeTelegraphs.Clear();

            for (int i = 0; i < createdMaterials.Count; i++)
            {
                if (createdMaterials[i] != null)
                {
                    Destroy(createdMaterials[i]);
                }
            }
            createdMaterials.Clear();

            swoopLaneTelegraph = null;
            swoopLaneMaterial = null;
            screechGroundCircle = null;
            screechContractingRing = null;
            featherAimLines = null;
        }

        private Material CreateMaterial(Color col)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Material m = new Material(sh);
            m.color = col;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            m.renderQueue = 3000;

            createdMaterials.Add(m);
            return m;
        }

        private void RegisterTelegraph(GameObject go)
        {
            if (go != null && !activeTelegraphs.Contains(go))
            {
                activeTelegraphs.Add(go);
            }
        }

        // ===================================================================================
        //  HELPERS ESTÁTICOS DE COLISIÓN Y LÍNEA DE VISIÓN
        // ===================================================================================

        public static bool IsBuildingOrBarricade(Collider col)
        {
            if (col == null) return false;
            string n = col.gameObject.name;
            if (n.Contains("Barricada_Madera") || n.Contains("Barricada")) return true;

            Transform p = col.transform.parent;
            while (p != null)
            {
                if (p.name.Contains("Barricada_Madera") || p.name.Contains("Barricada")) return true;
                p = p.parent;
            }

            if (col.GetComponentInParent<BuildingData>() != null || col.GetComponentInParent<CitySocket>() != null)
            {
                if (n.Contains("Pavement") || n.Contains("Floor") || n.Contains("Ground")) return false;
                return true;
            }

            return false;
        }

        public static bool IsWoodBarricadeCollider(Collider col)
        {
            if (col == null) return false;
            string n = col.gameObject.name;
            if (n.Contains("Barricada_Madera") || n.Contains("Barricada")) return true;

            Transform p = col.transform.parent;
            while (p != null)
            {
                if (p.name.Contains("Barricada_Madera") || p.name.Contains("Barricada")) return true;
                p = p.parent;
            }

            return false;
        }

        public static bool HasClearLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 origin = from + Vector3.up * 0.7f;
            Vector3 target = to + Vector3.up * 0.5f;
            Vector3 dir = target - origin;
            float dist = dir.magnitude;
            if (dist <= 0.05f) return true;

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsBuildingOrBarricade(hit.collider))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // =======================================================================================
    //  COMPONENTE HELPER: ONDA EXPANSIVA DE CHIRRIDO ULTRASÓNICO
    // =======================================================================================

    /// <summary>
    /// Onda sónica expansiva (0.5m a 10.5m en 0.55s).
    /// El agacharse [C] o estar tras cobertura de edificio/barricada niega el 100% del castigo.
    /// </summary>
    public class OwlSonicScreechRing : MonoBehaviour
    {
        public float duration = 0.55f;
        public float maxRadius = 10.5f;
        public float minCitySurfaceY = 0f;
        public Vector3 originPosition;
        public Transform moralesTransform;

        private float elapsed = 0f;
        private bool hasHitPlayer = false;
        private Transform ringVisual;
        private Material ringMat;

        public void Setup(Vector3 origin, float minCityY, Transform morales)
        {
            originPosition = origin;
            minCitySurfaceY = minCityY;
            moralesTransform = morales;

            transform.position = new Vector3(origin.x, minCitySurfaceY + 0.04f, origin.z);

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "ScreechRingVisual";
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localScale = new Vector3(1f, 0.02f, 1f);

            Collider col = cylinder.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer ren = cylinder.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");

                ringMat = new Material(sh);
                Color initCol = new Color(0.95f, 0.75f, 0.15f, 0.55f);
                ringMat.color = initCol;
                if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", initCol);
                ringMat.renderQueue = 3000;

                ren.material = ringMat;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ren.receiveShadows = false;
            }

            ringVisual = cylinder.transform;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentRadius = Mathf.Lerp(0.5f, maxRadius, t);

            if (ringVisual != null)
            {
                ringVisual.localScale = new Vector3(currentRadius * 2f, 0.02f, currentRadius * 2f);
                if (ringMat != null)
                {
                    Color c = Color.Lerp(new Color(1f, 0.78f, 0.12f, 0.55f), new Color(0.2f, 0.88f, 1f, 0.15f), t);
                    ringMat.color = c;
                    if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", c);
                }
            }

            if (!hasHitPlayer)
            {
                CheckPlayerHit(currentRadius);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        private void CheckPlayerHit(float currentRadius)
        {
            HamsterController playerHC = FindAnyObjectByType<HamsterController>();
            if (playerHC == null || playerHC.IsDead) return;

            Vector3 pPos = playerHC.transform.position;
            float dist = Vector3.Distance(originPosition, pPos);

            if (dist <= currentRadius + 0.65f && dist >= currentRadius - 1.4f)
            {
                hasHitPlayer = true;

                bool isCrouched = playerHC.isCrouching;
                bool inCover = CheckCoverToPlayer(pPos);

                if (isCrouched || inCover)
                {
                    // 100% de la penalización cancelada por sigilo o cobertura
                    QuestCompiler.Instance?.AdvanceActionIfMatches("agacharse_sigilo", "", moralesTransform != null ? moralesTransform : transform);
                }
                else
                {
                    // Penalización completa por estar de pie o corriendo en abierto
                    HamsterControllerPrototipo3 playerProto = playerHC as HamsterControllerPrototipo3 ?? playerHC.GetComponent<HamsterControllerPrototipo3>();
                    playerProto?.ApplyExternalSlow(0.40f, 3.0f);
                    playerHC.currentStamina = Mathf.Max(0f, playerHC.currentStamina - 45f);

                    if (dist < 2.4f)
                    {
                        playerHC.ReceiveDamage();
                    }
                }
            }
        }

        private bool CheckCoverToPlayer(Vector3 playerPos)
        {
            Vector3 origin = (moralesTransform != null ? moralesTransform.position : originPosition) + Vector3.up * 0.7f;
            Vector3 target = playerPos + Vector3.up * 0.5f;
            Vector3 dir = target - origin;
            float d = dir.magnitude;
            if (d <= 0.05f) return false;

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, d, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && MoralesOwlBossController.IsBuildingOrBarricade(hit.collider))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            if (ringMat != null)
            {
                Destroy(ringMat);
            }
        }
    }

    // =======================================================================================
    //  COMPONENTE HELPER: PROYECTIL DE PLUMA CORTANTE
    // =======================================================================================

    /// <summary>
    /// Proyectil de pluma en abanico (13.0 m/s, vida 1.6s).
    /// Salto [Espacio] por encima de 0.85m permite esquivarlo.
    /// Se rompe contra edificios y barricadas de madera.
    /// </summary>
    public class OwlFeatherProjectile : MonoBehaviour
    {
        public Vector3 direction = Vector3.forward;
        public float speed = 13.0f;
        public float lifetime = 1.6f;
        public float minCitySurfaceY = 0f;
        public float damageRadius = 0.50f;
        public float pushForce = 7.5f;

        private float elapsed = 0f;
        private Material featherMat;
        private bool hasHit = false;

        public void Setup(Vector3 dir, float startY)
        {
            direction = dir.normalized;
            minCitySurfaceY = startY;

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 pos = transform.position;
            pos.y = minCitySurfaceY + 0.45f;
            transform.position = pos;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "FeatherVisualMesh";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.18f, 0.08f, 0.85f);

            Collider col = body.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer ren = body.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");

                featherMat = new Material(sh);
                Color featherColor = new Color(1f, 0.82f, 0.18f, 0.95f);
                featherMat.color = featherColor;
                if (featherMat.HasProperty("_BaseColor")) featherMat.SetColor("_BaseColor", featherColor);

                ren.material = featherMat;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ren.receiveShadows = false;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            Vector3 origin = transform.position;

            // Detección frontal de choque contra edificios o barricada de madera
            if (Physics.Raycast(origin, direction, out RaycastHit hit, step + 0.25f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (MoralesOwlBossController.IsBuildingOrBarricade(hit.collider))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position += direction * step;

            if (!hasHit)
            {
                CheckPlayerHit();
            }
        }

        private void CheckPlayerHit()
        {
            HamsterController playerHC = FindAnyObjectByType<HamsterController>();
            if (playerHC == null || playerHC.IsDead) return;

            Vector3 pPos = playerHC.transform.position;
            Vector3 delta = pPos - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude <= damageRadius * damageRadius)
            {
                HamsterControllerPrototipo3 protoPlayer = playerHC as HamsterControllerPrototipo3 ?? playerHC.GetComponent<HamsterControllerPrototipo3>();

                // Salto por encima del proyectil
                bool isJumping = protoPlayer != null && (!protoPlayer.IsGrounded || protoPlayer.IsJumping);
                bool isHighEnough = playerHC.transform.position.y > (minCitySurfaceY + 0.85f);

                if (isJumping && isHighEnough)
                {
                    // Kilo saltó por encima de la pluma
                    return;
                }

                hasHit = true;
                Rigidbody rb = playerHC.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(direction * pushForce, ForceMode.Impulse);
                }

                protoPlayer?.ApplyExternalSlow(0.55f, 2.0f);
                playerHC.ReceiveDamage();

                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (featherMat != null)
            {
                Destroy(featherMat);
            }
        }
    }

    // =======================================================================================
    //  COMPONENTE HELPER: ANILLO DE ALERTA DE APAGÓN
    // =======================================================================================

    /// <summary>
    /// Anillo de alerta dorado expansivo durante el evento del apagón.
    /// </summary>
    public class OwlBlackoutAlertRing : MonoBehaviour
    {
        public float duration = 1.3f;
        public float maxRadius = 18f;
        private float elapsed = 0f;
        private Transform ringVisual;
        private Material ringMat;

        public void Setup(Vector3 center, float surfaceY)
        {
            transform.position = new Vector3(center.x, surfaceY + 0.04f, center.z);

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "BlackoutAlertVisual";
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localScale = new Vector3(1f, 0.02f, 1f);

            Collider col = cylinder.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer ren = cylinder.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");

                ringMat = new Material(sh);
                Color c = new Color(1f, 0.65f, 0.08f, 0.65f);
                ringMat.color = c;
                if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", c);
                ringMat.renderQueue = 3000;

                ren.material = ringMat;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ren.receiveShadows = false;
            }

            ringVisual = cylinder.transform;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentRadius = Mathf.Lerp(1.0f, maxRadius, t);

            if (ringVisual != null)
            {
                ringVisual.localScale = new Vector3(currentRadius * 2f, 0.02f, currentRadius * 2f);
                if (ringMat != null)
                {
                    Color c = new Color(1f, 0.65f, 0.08f, (1f - t) * 0.55f);
                    ringMat.color = c;
                    if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", c);
                }
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (ringMat != null)
            {
                Destroy(ringMat);
            }
        }
    }
}
