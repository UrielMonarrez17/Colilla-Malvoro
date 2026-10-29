using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace Prototipo3
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAIPrototipo3 : EnemyAI
    {
        [Header("Ajustes de Ciudad (Prototipo3)")]
        [Tooltip("Altura mínima del suelo de la ciudad.")]
        public float minCitySurfaceY = 0f;

        [Tooltip("Distancia de chequeo frontal para evitar atravesar colliders de edificios.")]
        public float buildingCheckDistance = 1.6f;

        [Header("Perfil de Jefe y Mecánicas Tácticas (Prototipo 3)")]
        public BossProfileType bossProfile = BossProfileType.None;

        [Tooltip("Rector Morales: Detecta carreras ruidosas (Shift sin agacharse C) a gran distancia.")]
        public bool sensitiveToNoisySprint = false;
        public float noisySprintDetectionRadius = 25f;
        public float acousticDecoyVulnerabilityMultiplier = 1.0f;

        [Tooltip("Sultán el Lobo: Multiplicador de olfato (proximidad extrema x2.5).")]
        public float baseSmellRadius = 2.5f;
        public float smellMultiplier = 1.0f;

        [Tooltip("Clara Valdés: Deja un rastro temporal de nubes de gas anestésico.")]
        public bool leavesAnestheticGasTrail = false;
        public float gasSpawnInterval = 1.6f;
        public GameObject anestheticGasCloudPrefab;

        private NavMeshAgent protoAgent;
        private Rigidbody protoRb;
        private float obstacleRedirectCooldown = 0f;
        private Vector3 lastValidCityPosition;

        private DualRoleCharacter dualRole;

        private static readonly MethodInfo MiUpdateAnimator = typeof(EnemyAI).GetMethod("UpdateAnimatorFromMovement", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiUpdateVisionCone = typeof(EnemyAI).GetMethod("UpdateVisionConeVisual", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiEnsureVisionCone = typeof(EnemyAI).GetMethod("EnsureVisionConeObjects", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiFindPlayer = typeof(EnemyAI).GetMethod("FindPlayerIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiSubscribeTime = typeof(EnemyAI).GetMethod("SubscribeToTimeManager", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiChangeState = typeof(EnemyAI).GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiUpdateWander = typeof(EnemyAI).GetMethod("UpdateWander", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiUpdateChase = typeof(EnemyAI).GetMethod("UpdateChase", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiUpdateAttack = typeof(EnemyAI).GetMethod("UpdateAttack", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MiCanDetectInitial = typeof(EnemyAI).GetMethod("CanDetectPlayerInitial", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FiIsImmobilized = typeof(EnemyAI).GetField("isImmobilized", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FiAttackTimer = typeof(EnemyAI).GetField("attackTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FiPlayerController = typeof(EnemyAI).GetField("playerController", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FiVisionConeObj = typeof(EnemyAI).GetField("visionConeObj", BindingFlags.Instance | BindingFlags.NonPublic);

        // Valores base cacheados para restaurar tras efectos de trampas
        private bool baseValuesCached = false;
        private float baseVisionNormal = 15f;
        private float baseVisionCrouched = 5f;

        // Estado de Trampa 1 (Señuelo Acústico)
        private float acousticDecoyTimer = 0f;
        private Vector3 acousticDecoyPos;

        // Estado de Trampa 3 (Parálisis por Descargador Estático)
        private float staticParalysisTimer = 0f;
        private GameObject paralysisVfxObj;

        // Temporizador de gas de Clara
        private float gasTrailTimer = 0f;

        public bool IsDistractedByDecoy => acousticDecoyTimer > 0f;
        public bool IsParalyzedByStatic => staticParalysisTimer > 0f;

        public bool IsFeralActiveNow()
        {
            if (dualRole == null) dualRole = GetComponent<DualRoleCharacter>();
            if (dualRole != null)
            {
                return dualRole.IsCurrentlyFeral;
            }
            return IsNight();
        }

        private void ForceBaseState(EnemyState targetState)
        {
            if (CurrentState != targetState && MiChangeState != null)
            {
                MiChangeState.Invoke(this, new object[] { targetState });
            }
            if (targetState != EnemyState.Attack && FiAttackTimer != null)
            {
                FiAttackTimer.SetValue(this, 0f);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            dualRole = GetComponent<DualRoleCharacter>();

            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] == null || scripts[i] == this) continue;
                string typeName = scripts[i].GetType().Name;
                if (typeName == "AnimationShowcase" || typeName == "MovePlayerInput" || typeName == "CreatureMover")
                {
                    scripts[i].enabled = false;
                }
            }

            if (CityGeneratorPrototipo3.Instance != null)
            {
                SetPatrolWaypoints(null);
                if (wanderRadius < 30f)
                {
                    wanderRadius = 35f;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            protoAgent = GetComponent<NavMeshAgent>();
            protoRb = GetComponent<Rigidbody>();
            dualRole = GetComponent<DualRoleCharacter>();

            CacheBaseDetectionValues();

            if (CityGeneratorPrototipo3.Instance != null)
            {
                minCitySurfaceY = CityGeneratorPrototipo3.Instance.roadHeightY;
                ConfigureAgentForCity();
                SnapToCitySurfaceImmediate();
                SetPatrolWaypoints(null);
            }
            else
            {
                ConfigureAgentForCity();
            }
        }

        public void CacheBaseDetectionValues()
        {
            if (!baseValuesCached)
            {
                baseVisionNormal = visionRadiusNormal;
                baseVisionCrouched = visionRadiusCrouched;
                if (baseSmellRadius <= 0.1f) baseSmellRadius = backDetectionRadius;
                baseValuesCached = true;
            }
        }

        public void HideVisionConeImmediate()
        {
            showVisionCone = false;
            Transform cone = transform.Find("VisionCone_Visual");
            if (cone != null) cone.gameObject.SetActive(false);
        }

        private void ConfigureAgentForCity()
        {
            if (protoAgent == null) protoAgent = GetComponent<NavMeshAgent>();
            if (protoAgent != null)
            {
                protoAgent.baseOffset = 0f;
                protoAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                protoAgent.autoRepath = true;
            }

            if (protoRb == null) protoRb = GetComponent<Rigidbody>();
            if (protoRb != null)
            {
                protoRb.isKinematic = true;
                protoRb.useGravity = false;
            }
        }

        /// <summary>
        /// Trampa 1 (Señuelo Acústico): Obliga al enemigo a investigar el señuelo durante duration segundos.
        /// </summary>
        public void AttractToAcousticDecoy(Vector3 decoyPos, float duration)
        {
            CacheBaseDetectionValues();
            acousticDecoyPos = decoyPos;
            acousticDecoyTimer = Mathf.Max(acousticDecoyTimer, duration);
            ForceBaseState(EnemyState.Wander);

            if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh && staticParalysisTimer <= 0f)
            {
                protoAgent.isStopped = false;
                protoAgent.speed = Mathf.Max(wanderSpeed * 1.2f, chaseSpeed * 0.75f);
                protoAgent.SetDestination(decoyPos);
            }
        }

        /// <summary>
        /// Trampa 3 (Descargador Estático): Paraliza al enemigo durante seconds segundos.
        /// </summary>
        public void ApplyStaticParalysis(float seconds)
        {
            CacheBaseDetectionValues();
            staticParalysisTimer = Mathf.Max(staticParalysisTimer, seconds);
            ForceBaseState(EnemyState.Wander);

            if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
            {
                protoAgent.isStopped = true;
                protoAgent.velocity = Vector3.zero;
            }

            EnsureParalysisVfx(true);
        }

        private void EnsureParalysisVfx(bool active)
        {
            if (!active)
            {
                if (paralysisVfxObj != null) paralysisVfxObj.SetActive(false);
                return;
            }

            if (paralysisVfxObj == null)
            {
                paralysisVfxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                paralysisVfxObj.name = "StaticParalysisRing";
                paralysisVfxObj.transform.SetParent(transform, false);
                paralysisVfxObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                paralysisVfxObj.transform.localScale = new Vector3(1.5f, 0.45f, 1.5f);
                Collider c = paralysisVfxObj.GetComponent<Collider>();
                if (c != null) Destroy(c);

                Renderer r = paralysisVfxObj.GetComponent<Renderer>();
                if (r != null)
                {
                    Material m = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                    m.color = new Color(0.25f, 0.85f, 1f, 0.45f);
                    r.material = m;
                }
            }

            paralysisVfxObj.SetActive(true);
        }

        private void Update()
        {
            CacheBaseDetectionValues();
            UpdateTrapAndBossModifiers();

            MiUpdateAnimator?.Invoke(this, null);
            UpdateFeralVisionConeVisual();

            if (protoAgent == null) protoAgent = GetComponent<NavMeshAgent>();
            bool baseImmobilized = FiIsImmobilized != null && (bool)FiIsImmobilized.GetValue(this);
            if (protoAgent == null || !protoAgent.enabled || baseImmobilized) return;

            // Si está paralizado (Trampa 3) o distraído por el Señuelo Acústico (Trampa 1),
            // bloqueamos por completo la persecución y el ataque hacia el jugador.
            if (staticParalysisTimer > 0f || acousticDecoyTimer > 0f)
            {
                ForceBaseState(EnemyState.Wander);
                return;
            }

            MiFindPlayer?.Invoke(this, null);
            MiSubscribeTime?.Invoke(this, null);

            bool isFeral = IsFeralActiveNow();
            if (!isFeral)
            {
                ForceBaseState(EnemyState.Wander);
                MiUpdateWander?.Invoke(this, null);
                return;
            }

            HamsterController hc = FiPlayerController != null ? FiPlayerController.GetValue(this) as HamsterController : null;
            if (hc == null && player != null) hc = player.GetComponent<HamsterController>();

            if (hc != null && hc.IsDead)
            {
                ForceBaseState(EnemyState.Wander);
                MiUpdateWander?.Invoke(this, null);
                return;
            }

            switch (CurrentState)
            {
                case EnemyState.Wander:
                    MiUpdateWander?.Invoke(this, null);
                    bool detected = MiCanDetectInitial != null && (bool)MiCanDetectInitial.Invoke(this, null);
                    if (detected)
                    {
                        ForceBaseState(EnemyState.Chase);
                    }
                    break;

                case EnemyState.Chase:
                    MiUpdateChase?.Invoke(this, null);
                    break;

                case EnemyState.Attack:
                    MiUpdateAttack?.Invoke(this, null);
                    break;
            }
        }

        private void UpdateFeralVisionConeVisual()
        {
            bool isFeral = IsFeralActiveNow();
            if (IsNight() || !isFeral || !showVisionCone)
            {
                MiUpdateVisionCone?.Invoke(this, null);
                return;
            }

            // Si es de día pero el jefe está en estado Feral forzado (ej. Morales en el apagón o Kross en la Torre),
            // aseguramos que su cono de visión también se dibuje.
            MiEnsureVisionCone?.Invoke(this, null);
            GameObject coneObj = FiVisionConeObj != null ? FiVisionConeObj.GetValue(this) as GameObject : null;
            if (coneObj != null && !coneObj.activeSelf)
            {
                coneObj.SetActive(true);
            }
        }

        private void LateUpdate()
        {
            if (CityGeneratorPrototipo3.Instance != null)
            {
                minCitySurfaceY = CityGeneratorPrototipo3.Instance.roadHeightY;
            }

            EnforceCitySurfaceHeight();
            PreventBuildingPenetration();
        }

        private void UpdateTrapAndBossModifiers()
        {
            // 1. Parálisis eléctrica (Trampa 3)
            if (staticParalysisTimer > 0f)
            {
                staticParalysisTimer -= Time.deltaTime;
                visionRadiusNormal = 0.01f;
                visionRadiusCrouched = 0.01f;
                backDetectionRadius = 0.01f;
                ForceBaseState(EnemyState.Wander);

                if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
                {
                    protoAgent.isStopped = true;
                    protoAgent.velocity = Vector3.zero;
                }

                if (staticParalysisTimer <= 0f)
                {
                    EnsureParalysisVfx(false);
                    if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
                    {
                        protoAgent.isStopped = false;
                    }
                }
                return;
            }

            // 2. Distracción por Señuelo Acústico (Trampa 1)
            if (acousticDecoyTimer > 0f)
            {
                acousticDecoyTimer -= Time.deltaTime;
                // Suprimir visión hacia el jugador para que vaya al señuelo
                visionRadiusNormal = 0.05f;
                visionRadiusCrouched = 0.05f;
                backDetectionRadius = 0.05f;
                ForceBaseState(EnemyState.Wander);

                if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
                {
                    float distToDecoy = Vector3.Distance(transform.position, acousticDecoyPos);
                    if (distToDecoy > 1.35f)
                    {
                        protoAgent.isStopped = false;
                        protoAgent.speed = Mathf.Max(wanderSpeed * 1.2f, 4.5f);
                        protoAgent.SetDestination(acousticDecoyPos);
                    }
                    else
                    {
                        protoAgent.isStopped = true;
                    }
                }
                return;
            }

            // 3. Efecto de Bomba de Polen (Trampa 2): desactiva olfato y reduce al 50% visión
            bool pollenActive = TrapSystemPrototipo3.IsPollenCloudActive;
            if (pollenActive)
            {
                visionRadiusNormal = baseVisionNormal * 0.5f;
                visionRadiusCrouched = baseVisionCrouched * 0.5f;
                backDetectionRadius = bossProfile == BossProfileType.Sultan_Lobo ? 0.4f : 0f;
            }
            else
            {
                visionRadiusNormal = baseVisionNormal;
                visionRadiusCrouched = baseVisionCrouched;
                backDetectionRadius = baseSmellRadius * Mathf.Max(1f, smellMultiplier);
            }

            bool isFeral = IsFeralActiveNow();

            // 4. Sensibilidad a carrera ruidosa del Búho Morales (Shift sin C)
            if (sensitiveToNoisySprint && isFeral && player != null && !pollenActive)
            {
                HamsterController hc = player.GetComponent<HamsterController>();
                if (hc != null && hc.IsSprinting && !hc.isCrouching && !hc.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, player.position);
                    if (dist <= noisySprintDetectionRadius)
                    {
                        ForceChasePlayer();
                    }
                }
            }

            // 5. Rastro de gas anestésico de Clara Valdés ("La Dama de Gas")
            if (leavesAnestheticGasTrail && (isFeral || CurrentState == EnemyState.Chase))
            {
                gasTrailTimer += Time.deltaTime;
                if (gasTrailTimer >= gasSpawnInterval)
                {
                    gasTrailTimer = 0f;
                    SpawnAnestheticGasCloud();
                }
            }
        }

        private void SpawnAnestheticGasCloud()
        {
            Vector3 spawnPos = transform.position;
            spawnPos.y = minCitySurfaceY;

            GameObject cloudObj;
            if (anestheticGasCloudPrefab != null)
            {
                cloudObj = Instantiate(anestheticGasCloudPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                cloudObj = new GameObject("Clara_AnestheticGasCloud");
                cloudObj.transform.position = spawnPos;
                cloudObj.AddComponent<AnestheticGasCloud>();
            }
        }

        public void SnapToCitySurfaceImmediate()
        {
            Vector3 pos = transform.position;
            float targetY = FindCitySurfaceHeight(pos);
            pos.y = targetY;
            transform.position = pos;
            lastValidCityPosition = pos;

            if (protoAgent != null && protoAgent.enabled)
            {
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    protoAgent.Warp(hit.position);
                    lastValidCityPosition = hit.position;
                }
            }
        }

        private void EnforceCitySurfaceHeight()
        {
            if (CityGeneratorPrototipo3.Instance == null)
            {
                if (transform.position.y < -8.0f && protoAgent != null && protoAgent.enabled)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit fallbackHit, 15f, NavMesh.AllAreas))
                    {
                        protoAgent.Warp(fallbackHit.position);
                    }
                }
                return;
            }

            Vector3 pos = transform.position;
            if (pos.y < minCitySurfaceY - 0.15f)
            {
                float surfaceY = FindCitySurfaceHeight(pos);
                pos.y = surfaceY;

                if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                    {
                        protoAgent.Warp(hit.position);
                        lastValidCityPosition = hit.position;
                    }
                    else
                    {
                        transform.position = pos;
                    }
                }
                else
                {
                    transform.position = pos;
                }
            }
            else
            {
                lastValidCityPosition = pos;
            }
        }

        private float FindCitySurfaceHeight(Vector3 worldPos)
        {
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit navHit, 4.0f, NavMesh.AllAreas))
            {
                return navHit.position.y;
            }

            Vector3 rayOrigin = new Vector3(worldPos.x, Mathf.Max(worldPos.y, minCitySurfaceY) + 6f, worldPos.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);

            float bestY = minCitySurfaceY;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                if (col.GetComponentInParent<EnemyAI>() != null) continue;
                if (col.GetComponentInParent<HamsterController>() != null) continue;

                if (hits[i].point.y >= minCitySurfaceY - 0.3f && hits[i].point.y <= minCitySurfaceY + 0.6f)
                {
                    float d = Mathf.Abs(hits[i].point.y - minCitySurfaceY);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestY = hits[i].point.y;
                    }
                }
            }

            return bestY;
        }

        private void PreventBuildingPenetration()
        {
            if (protoAgent == null || !protoAgent.enabled || !protoAgent.isOnNavMesh) return;

            if (obstacleRedirectCooldown > 0f)
            {
                obstacleRedirectCooldown -= Time.deltaTime;
            }

            Vector3 checkCenter = transform.position + Vector3.up * 0.6f;
            float checkRadius = Mathf.Max(0.45f, protoAgent.radius);

            Collider[] overlaps = Physics.OverlapSphere(checkCenter, checkRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider col = overlaps[i];
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;

                if (IsBuildingCollider(col))
                {
                    Collider selfCol = GetComponent<Collider>();
                    if (selfCol != null && Physics.ComputePenetration(
                        selfCol, transform.position, transform.rotation,
                        col, col.transform.position, col.transform.rotation,
                        out Vector3 pushDir, out float pushDist))
                    {
                        pushDir.y = 0f;
                        if (pushDir.sqrMagnitude > 0.001f)
                        {
                            pushDir.Normalize();
                            Vector3 safePos = transform.position + pushDir * (pushDist + 0.25f);
                            if (CityGeneratorPrototipo3.Instance != null)
                            {
                                safePos.y = Mathf.Max(safePos.y, minCitySurfaceY);
                            }
                            if (NavMesh.SamplePosition(safePos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                            {
                                if (CityGeneratorPrototipo3.Instance == null || navHit.position.y >= minCitySurfaceY - 0.25f)
                                {
                                    protoAgent.Warp(navHit.position);
                                }
                            }
                        }
                    }

                    RedirectAwayFromBuilding(col.bounds.center);
                    return;
                }
            }

            Vector3 velocity = protoAgent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.1f && obstacleRedirectCooldown <= 0f)
            {
                Vector3 moveDir = velocity.normalized;
                if (Physics.SphereCast(checkCenter, checkRadius * 0.7f, moveDir, out RaycastHit hit, buildingCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (IsBuildingCollider(hit.collider))
                    {
                        RedirectAwayFromBuilding(hit.point);
                    }
                }
            }
        }

        private bool IsBuildingCollider(Collider col)
        {
            if (col == null) return false;
            if (col.GetComponentInParent<BuildingData>() != null || col.GetComponentInParent<CitySocket>() != null)
            {
                if (col.gameObject.name.Contains("Pavement") || col.gameObject.name.Contains("Floor") || col.gameObject.name.Contains("Ground")) return false;
                return true;
            }
            return false;
        }

        private void RedirectAwayFromBuilding(Vector3 obstaclePoint)
        {
            if (obstacleRedirectCooldown > 0f || protoAgent == null || !protoAgent.isOnNavMesh) return;
            obstacleRedirectCooldown = 0.6f;

            if (CurrentState == EnemyState.Wander && acousticDecoyTimer <= 0f)
            {
                Vector3 awayDir = transform.position - obstaclePoint;
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.01f) awayDir = -transform.forward;
                awayDir.Normalize();

                for (int attempt = 0; attempt < 12; attempt++)
                {
                    Vector3 randomOffset = Quaternion.Euler(0f, UnityEngine.Random.Range(-110f, 110f), 0f) * awayDir;
                    float dist = UnityEngine.Random.Range(8f, wanderRadius);
                    Vector3 candidate = transform.position + randomOffset * dist;
                    candidate.y = minCitySurfaceY;

                    if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 6.0f, NavMesh.AllAreas) && hit.position.y >= minCitySurfaceY - 0.2f)
                    {
                        protoAgent.SetDestination(hit.position);
                        break;
                    }
                }
            }
        }
    }
}
