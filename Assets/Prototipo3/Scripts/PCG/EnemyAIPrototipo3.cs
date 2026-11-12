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

        [Header("Patrulla Territorial de Jefes")]
        [Tooltip("Radio máximo de patrulla alrededor del spawner/homeAnchor para Jefes.")]
        public float bossTerritoryRadius = 15f;
        [Tooltip("Distancia máxima de persecución antes de que el Jefe rompa la persecución y regrese a su territorio.")]
        public float bossLeashDistance = 26f;

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
        private float baseBackDetectionRadius;
        private bool detectionModifiedByTrap;
        private bool movementModifiedByTrap;
        private bool stoppedByTrap;

        // Estado de Trampa 1 (Señuelo Acústico)
        private float acousticDecoyTimer = 0f;
        private Vector3 acousticDecoyPos;

        // Estado de Trampa 3 (Parálisis por Descargador Estático)
        private float staticParalysisTimer = 0f;
        private GameObject paralysisVfxObj;

        // Estado de Trampa 2 (Red Inmovilizadora por 5s)
        private float netImmobilizationTimer = 0f;
        private GameObject netVfxObj;

        // Estado de Trampa 1/3 (Púas: ralentización al 50% por 6s con chispas)
        private float spikeSlowTimer = 0f;
        private float spikeSlowMultiplier = 0.5f;
        private GameObject sparksVfxObj;

        private float gasTrailTimer = 0f;

        public bool IsDistractedByDecoy => acousticDecoyTimer > 0f;
        public bool IsParalyzedByStatic => staticParalysisTimer > 0f;
        public bool IsNetImmobilized => netImmobilizationTimer > 0f;
        public bool IsSpikeSlowed => spikeSlowTimer > 0f;
        private bool IsImmobilizedByTrap => IsNetImmobilized || IsParalyzedByStatic;

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
                if (IsBossCharacter())
                {
                    wanderRadius = bossTerritoryRadius;
                }
                else if (wanderRadius < 30f)
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
            // Los perfiles de jefe pueden configurarse después de OnEnable.
            // Nunca guardar como base los radios reducidos por una trampa.
            if (!baseValuesCached || !detectionModifiedByTrap)
            {
                baseVisionNormal = visionRadiusNormal;
                baseVisionCrouched = visionRadiusCrouched;
                baseBackDetectionRadius = backDetectionRadius;
                baseValuesCached = true;
            }
        }

        protected override void OnDisable()
        {
            ClearTrapEffects();
            base.OnDisable();
        }

        private void ClearTrapEffects()
        {
            acousticDecoyTimer = staticParalysisTimer = netImmobilizationTimer = spikeSlowTimer = 0f;
            EnsureParalysisVfx(false);
            EnsureNetVfx(false);
            EnsureSparksVfx(false);
            RestoreBaseDetection();
            ApplyTrapMovement();
            baseValuesCached = false;
        }

        protected override void ApplyTrapEffect(TipoTrampa tipo, Vector3 posicionPuerta)
        {
            // PuertaTrampa llega por EnemyAI. Usar los mismos efectos de Prototipo3
            // sin iniciar las corutinas antiguas de velocidad, red o empuje físico.
            switch (tipo)
            {
                case TipoTrampa.Puas:
                    ApplySpikeSlowAndSparks(TrapSystemPrototipo3.SpikeSpeedMultiplier, TrapSystemPrototipo3.SpikeDuration);
                    break;
                case TipoTrampa.Red:
                    ApplyNetImmobilization(TrapSystemPrototipo3.NetDuration);
                    break;
                case TipoTrampa.Madera:
                    Vector3 direction = transform.position - posicionPuerta;
                    direction.y = 0f;
                    Quaternion rotation = direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction) : transform.rotation;
                    TrapSystemPrototipo3.EnsureInstance().CreateWoodBarricade(posicionPuerta, rotation);
                    break;
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
            if (!isActiveAndEnabled || duration <= 0f) return;
            CacheBaseDetectionValues();
            acousticDecoyPos = decoyPos;
            acousticDecoyTimer = Mathf.Max(acousticDecoyTimer, duration);
            ForceBaseState(EnemyState.Wander);

            RefreshTrapDetection();
            ApplyTrapMovement();
        }

        /// <summary>
        /// Trampa 3 (Descargador Estático): Paraliza al enemigo durante seconds segundos.
        /// </summary>
        public void ApplyStaticParalysis(float seconds)
        {
            if (!isActiveAndEnabled || seconds <= 0f) return;
            CacheBaseDetectionValues();
            staticParalysisTimer = Mathf.Max(staticParalysisTimer, seconds);
            ForceBaseState(EnemyState.Wander);

            RefreshTrapDetection();
            ApplyTrapMovement();
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

        /// <summary>
        /// Trampa de Madera: Barricada física por 10s. Fuerza el recálculo de ruta del NavMeshAgent evitando la barricada.
        /// </summary>
        public void ApplyWoodBarricadeRepath(Vector3 barricadePos)
        {
            if (protoAgent == null) protoAgent = GetComponent<NavMeshAgent>();
            if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh &&
                (protoAgent.hasPath || protoAgent.pathPending))
            {
                Vector3 destination = protoAgent.destination;
                protoAgent.ResetPath();
                protoAgent.SetDestination(destination);
            }
            // Recalcular la ruta no cancela una red ni cambia una persecución a patrulla.
            ApplyTrapMovement();
        }

        /// <summary>
        /// Trampa de Red: Inmoviliza al NavMeshAgent por 5s y apaga temporalmente la detección por proximidad extrema.
        /// </summary>
        public void ApplyNetImmobilization(float duration = 5f)
        {
            if (!isActiveAndEnabled || duration <= 0f) return;
            CacheBaseDetectionValues();
            netImmobilizationTimer = Mathf.Max(netImmobilizationTimer, duration);
            ForceBaseState(EnemyState.Wander);

            RefreshTrapDetection();
            ApplyTrapMovement();
            EnsureNetVfx(true);
        }

        private void EnsureNetVfx(bool active)
        {
            if (!active)
            {
                if (netVfxObj != null) netVfxObj.SetActive(false);
                return;
            }

            if (netVfxObj == null)
            {
                netVfxObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                netVfxObj.name = "NetTrapVisualRing";
                netVfxObj.transform.SetParent(transform, false);
                netVfxObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                netVfxObj.transform.localScale = new Vector3(1.8f, 0.6f, 1.8f);
                Collider c = netVfxObj.GetComponent<Collider>();
                if (c != null) Destroy(c);

                Renderer r = netVfxObj.GetComponent<Renderer>();
                if (r != null)
                {
                    Material m = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                    m.color = new Color(0.85f, 0.85f, 0.25f, 0.5f);
                    r.material = m;
                }
            }

            netVfxObj.SetActive(true);
        }

        /// <summary>
        /// Trampa de Púas: Reduce la velocidad de persecución/patrulla al 50% por 6s con chispas eléctricas.
        /// </summary>
        public void ApplySpikeSlowAndSparks(float slowMultiplier = 0.5f, float duration = 6f)
        {
            if (!isActiveAndEnabled || duration <= 0f) return;
            CacheBaseDetectionValues();
            spikeSlowTimer = Mathf.Max(spikeSlowTimer, duration);
            spikeSlowMultiplier = Mathf.Clamp(slowMultiplier, 0.2f, 0.8f);

            ApplyTrapMovement();
            EnsureSparksVfx(true);
        }

        private void EnsureSparksVfx(bool active)
        {
            if (!active)
            {
                if (sparksVfxObj != null) sparksVfxObj.SetActive(false);
                return;
            }

            if (sparksVfxObj == null)
            {
                sparksVfxObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sparksVfxObj.name = "SpikeElectricSparksRing";
                sparksVfxObj.transform.SetParent(transform, false);
                sparksVfxObj.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                sparksVfxObj.transform.localScale = new Vector3(1.4f, 0.2f, 1.4f);
                Collider c = sparksVfxObj.GetComponent<Collider>();
                if (c != null) Destroy(c);

                Renderer r = sparksVfxObj.GetComponent<Renderer>();
                if (r != null)
                {
                    Material m = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                    m.color = new Color(0.2f, 0.6f, 1f, 0.65f);
                    r.material = m;
                }
            }

            sparksVfxObj.SetActive(true);
        }

        public bool IsBossCharacter()
        {
            if (bossProfile != BossProfileType.None) return true;
            if (dualRole == null) dualRole = GetComponent<DualRoleCharacter>();
            if (dualRole != null && (dualRole.roleType == CharacterRoleType.PacifiableBoss || dualRole.bossProfile != BossProfileType.None)) return true;
            return false;
        }

        private void EnforceBossTerritoryPatrol()
        {
            if (!IsBossCharacter()) return;
            if (dualRole == null) dualRole = GetComponent<DualRoleCharacter>();
            Vector3 home = dualRole != null ? dualRole.HomeAnchorPosition : transform.position;
            if (home == Vector3.zero) return;

            float distFromHome = Vector3.Distance(transform.position, home);
            if (distFromHome > bossTerritoryRadius)
            {
                if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh && staticParalysisTimer <= 0f && netImmobilizationTimer <= 0f)
                {
                    protoAgent.isStopped = false;
                    protoAgent.speed = wanderSpeed;
                    protoAgent.SetDestination(home);
                }
            }
        }

        private void EnforceBossChaseLeash()
        {
            if (!IsBossCharacter()) return;
            if (dualRole == null) dualRole = GetComponent<DualRoleCharacter>();
            Vector3 home = dualRole != null ? dualRole.HomeAnchorPosition : transform.position;
            if (home == Vector3.zero) return;

            float distFromHome = Vector3.Distance(transform.position, home);
            if (distFromHome > bossLeashDistance)
            {
                // El jugador abandonó el territorio del jefe: romper persecución y regresar a su ancla
                ForceBaseState(EnemyState.Wander);
                if (protoAgent != null && protoAgent.enabled && protoAgent.isOnNavMesh)
                {
                    protoAgent.ResetPath();
                    protoAgent.speed = wanderSpeed;
                    protoAgent.SetDestination(home);
                }
            }
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

            // Si está paralizado (Trampa 3), atrapado en red (Trampa 2) o distraído por el Señuelo Acústico (Trampa 1),
            // bloqueamos por completo la persecución y el ataque hacia el jugador.
            if (staticParalysisTimer > 0f || acousticDecoyTimer > 0f || netImmobilizationTimer > 0f)
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
                EnforceBossTerritoryPatrol();
                return;
            }

            HamsterController hc = FiPlayerController != null ? FiPlayerController.GetValue(this) as HamsterController : null;
            if (hc == null && player != null) hc = player.GetComponent<HamsterController>();

            if (hc != null && hc.IsDead)
            {
                ForceBaseState(EnemyState.Wander);
                MiUpdateWander?.Invoke(this, null);
                EnforceBossTerritoryPatrol();
                return;
            }

            switch (CurrentState)
            {
                case EnemyState.Wander:
                    MiUpdateWander?.Invoke(this, null);
                    EnforceBossTerritoryPatrol();
                    bool detected = MiCanDetectInitial != null && (bool)MiCanDetectInitial.Invoke(this, null);
                    if (detected)
                    {
                        ForceBaseState(EnemyState.Chase);
                    }
                    break;

                case EnemyState.Chase:
                    MiUpdateChase?.Invoke(this, null);
                    EnforceBossChaseLeash();
                    break;

                case EnemyState.Attack:
                    MiUpdateAttack?.Invoke(this, null);
                    EnforceBossChaseLeash();
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
            // La patrulla, los cambios de estado y los pulsos del señuelo escriben
            // la velocidad durante Update. El modificador se aplica al final, una sola vez.
            ApplyTrapMovement();
            MiUpdateAnimator?.Invoke(this, null);
        }

        private void RestoreBaseDetection()
        {
            if (!baseValuesCached || !detectionModifiedByTrap) return;
            visionRadiusNormal = baseVisionNormal;
            visionRadiusCrouched = baseVisionCrouched;
            backDetectionRadius = baseBackDetectionRadius;
            detectionModifiedByTrap = false;
        }

        private void RefreshTrapDetection()
        {
            if (IsImmobilizedByTrap || IsDistractedByDecoy)
            {
                detectionModifiedByTrap = true;
                visionRadiusNormal = 0.05f;
                visionRadiusCrouched = 0.05f;
                backDetectionRadius = 0f;
            }
            else if (TrapSystemPrototipo3.IsPollenCloudActive)
            {
                detectionModifiedByTrap = true;
                visionRadiusNormal = baseVisionNormal * 0.5f;
                visionRadiusCrouched = baseVisionCrouched * 0.5f;
                backDetectionRadius = bossProfile == BossProfileType.Sultan_Lobo ? 0.4f : 0f;
            }
            else
            {
                RestoreBaseDetection();
            }
        }

        private void ApplyTrapMovement()
        {
            if (protoAgent == null) protoAgent = GetComponent<NavMeshAgent>();
            bool active = IsImmobilizedByTrap || IsDistractedByDecoy || IsSpikeSlowed;
            if (active) movementModifiedByTrap = true;
            if (!movementModifiedByTrap || protoAgent == null || !protoAgent.enabled || !protoAgent.isOnNavMesh) return;

            float speed = IsDistractedByDecoy
                ? Mathf.Max(wanderSpeed * 1.2f, 4.5f)
                : (CurrentState == EnemyState.Wander ? wanderSpeed : chaseSpeed);
            protoAgent.speed = speed * (IsSpikeSlowed ? spikeSlowMultiplier : 1f);

            if (IsImmobilizedByTrap)
            {
                protoAgent.isStopped = true;
                protoAgent.velocity = Vector3.zero;
                stoppedByTrap = true;
            }
            else if (IsDistractedByDecoy)
            {
                bool arrived = Vector3.Distance(transform.position, acousticDecoyPos) <= 1.35f;
                protoAgent.isStopped = arrived;
                stoppedByTrap = true;
                if (!arrived) protoAgent.SetDestination(acousticDecoyPos);
            }
            else if (stoppedByTrap)
            {
                protoAgent.isStopped = false;
                stoppedByTrap = false;
            }

            if (IsSpikeSlowed && protoAgent.velocity.sqrMagnitude > protoAgent.speed * protoAgent.speed)
            {
                protoAgent.velocity = Vector3.ClampMagnitude(protoAgent.velocity, protoAgent.speed);
            }
            movementModifiedByTrap = active;
        }

        private void UpdateTrapAndBossModifiers()
        {
            // Los efectos simultáneos vencen por tiempo transcurrido, no en cadena.
            float delta = Time.deltaTime;
            netImmobilizationTimer = Mathf.Max(0f, netImmobilizationTimer - delta);
            staticParalysisTimer = Mathf.Max(0f, staticParalysisTimer - delta);
            spikeSlowTimer = Mathf.Max(0f, spikeSlowTimer - delta);
            acousticDecoyTimer = Mathf.Max(0f, acousticDecoyTimer - delta);
            EnsureNetVfx(IsNetImmobilized);
            EnsureParalysisVfx(IsParalyzedByStatic);
            EnsureSparksVfx(IsSpikeSlowed);
            RefreshTrapDetection();
            ApplyTrapMovement();

            if (IsImmobilizedByTrap || IsDistractedByDecoy)
            {
                ForceBaseState(EnemyState.Wander);
                ApplyTrapMovement();
                return;
            }

            bool pollenActive = TrapSystemPrototipo3.IsPollenCloudActive;

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

            if (player != null && CurrentState == EnemyState.Wander && Prototipo3.Narrative.QuestCompiler.Instance != null && !Prototipo3.Narrative.QuestCompiler.Instance.IsCurrentQuestComplete)
            {
                var cur = Prototipo3.Narrative.QuestCompiler.Instance.CurrentAction;
                if (cur != null && cur.actionType == "espiar_rutina")
                {
                    HamsterController hc = player.GetComponent<HamsterController>();
                    if (hc != null && hc.isCrouching && Vector3.Distance(transform.position, player.position) <= 22f)
                    {
                        Prototipo3.Narrative.QuestCompiler.Instance.AdvanceActionIfMatches("espiar_rutina", "", transform);
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
