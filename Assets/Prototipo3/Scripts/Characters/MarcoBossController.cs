using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Prototipo3.Narrative;

namespace Prototipo3
{
    /// <summary>
    /// Controlador especializado de combate para Marco el Tejón (Conserje Nocturno / Tanque Excavador).
    /// Gestiona 4 patrones de movimiento (Patrulla Pesada de Conserje, Arranque Frenético, Paso Subterráneo Corto, Ventana de Fatiga)
    /// y 3 ataques tácticos telegrafiados (Embestida Excavadora con choque contra muros/barricadas, Golpe Sísmico de Escombros,
    /// y Lanzamiento de Chatarra Tóxica).
    /// </summary>
    public class MarcoBossController : MonoBehaviour
    {
        private enum BossActionState
        {
            None,
            JanitorInspect,
            BurrowReposition,
            RecoveryFatigue,
            WallCrashStun,
            LungeWindup,
            LungeCharge,
            SlamWindup,
            ScrapBarrageWindup
        }

        [Header("Referencias Base")]
        [SerializeField] private EnemyAIPrototipo3 ai;
        [SerializeField] private DualRoleCharacter dualRole;
        [SerializeField] private Transform visualMeshContainer;

        [Header("Cooldowns de Ataques (s)")]
        public float lungeCooldown = 7.5f;
        public float slamCooldown = 6.0f;
        public float scrapCooldown = 8.5f;

        [Header("Tiempos de Fases")]
        public float lungeWindupDuration = 0.80f;
        public float lungeChargeDuration = 0.60f;
        public float lungeChargeSpeed = 14.2f;
        public float slamWindupDuration = 0.75f;
        public float slamRadius = 5.2f;
        public float scrapWindupDuration = 0.65f;
        public float wallCrashStunDuration = 2.6f;
        public float fatigueRecoveryDuration = 1.8f;
        public float burrowDuration = 0.50f;
        public float janitorInspectDuration = 1.0f;

        // Estado activo
        private BossActionState currentAction = BossActionState.None;
        private float actionTimer = 0f;
        private float activeStunDuration = 2.6f;
        private int specialAttacksInARow = 0;

        // Timers de cooldown
        private float lungeCooldownTimer = 2.0f;
        private float slamCooldownTimer = 3.5f;
        private float scrapCooldownTimer = 4.5f;

        // Timers de movimiento táctico
        private float janitorPatrolTimer = 2.0f;
        private float frenzyDistanceTimer = 0f;
        private float frenzyBurstTimer = 0f;
        private bool isFrenzyBurstActive = false;
        private float burrowStuckTimer = 0f;
        private Vector3 burrowWarpDestination;
        private bool hasWarpedInBurrow = false;

        // Datos de ataque en curso
        private Vector3 lockedChargeDir = Vector3.forward;
        private bool hasHitKiloInLunge = false;
        private readonly List<Vector3> cachedScrapTargets = new List<Vector3>();

        // Respaldo de eye light
        private float originalEyeLightIntensity = 1.0f;
        private bool hasOriginalEyeIntensity = false;

        // Objetos visuales procedimentales activos
        private GameObject lungeTelegraphObj;
        private Material lungeTelegraphMat;

        private GameObject slamWarningDiscObj;
        private Material slamWarningDiscMat;

        private readonly List<GameObject> activeScrapReticles = new List<GameObject>();
        private readonly List<Material> activeScrapReticleMats = new List<Material>();

        private GameObject dizzyVisualObj;
        private Material dizzyMat;

        private GameObject fatigueVisualObj;
        private Material fatigueMat;

        private GameObject janitorDustObj;
        private Material janitorDustMat;

        public bool IsPerformingSpecialAction => currentAction != BossActionState.None;

        public void Initialize(EnemyAIPrototipo3 ai, DualRoleCharacter dualRole)
        {
            this.ai = ai;
            this.dualRole = dualRole;

            CacheVisualMeshContainer();

            if (dualRole != null && dualRole.feralEyeLight != null)
            {
                originalEyeLightIntensity = dualRole.feralEyeLight.intensity;
                hasOriginalEyeIntensity = true;
            }

            // Cooldowns iniciales de cortesía para el encuentro
            lungeCooldownTimer = 2.5f;
            slamCooldownTimer = 3.8f;
            scrapCooldownTimer = 5.0f;
            janitorPatrolTimer = 2.0f;
            frenzyDistanceTimer = 0f;
            frenzyBurstTimer = 0f;
            isFrenzyBurstActive = false;
            burrowStuckTimer = 0f;
            specialAttacksInARow = 0;
            currentAction = BossActionState.None;
        }

        private void CacheVisualMeshContainer()
        {
            if (visualMeshContainer != null) return;

            visualMeshContainer = transform.Find("VisualMesh_ReplaceMe");
            if (visualMeshContainer == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.Equals("VisualMesh_ReplaceMe", StringComparison.OrdinalIgnoreCase))
                    {
                        visualMeshContainer = child;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Tick principal de la IA del Jefe Marco.
        /// Devuelve true si Marco está ejecutando una acción especial (windup, embestida, golpe sísmico, lanzamiento, aturdimiento o fatiga)
        /// que asume el control del NavMeshAgent. Devuelve false cuando debe correr el Wander/Chase habitual.
        /// </summary>
        public bool TryTickBossAI(EnemyAIPrototipo3 aiRef, NavMeshAgent agent, HamsterController playerHC, bool isFeral)
        {
            if (ai == null) ai = aiRef;
            if (agent == null || !agent.enabled) return false;

            CacheVisualMeshContainer();

            // 1. CONDICIONES DE CANCELACIÓN INMEDIATA (Cinemáticas, trampas activas, muerte de Kilo, pacificación, amanecer)
            bool shouldCancel = !isFeral ||
                                (dualRole != null && dualRole.isPacified) ||
                                (playerHC == null || playerHC.IsDead) ||
                                (CutsceneManager.Instance != null && CutsceneManager.Instance.IsPlayingCutscene) ||
                                (RespawnManagerPrototipo3.Instance != null && RespawnManagerPrototipo3.Instance.IsRespawning) ||
                                Prototipo3.UI.FeralGameUI.BlocksGameplay ||
                                (TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Day) ||
                                (ai != null && (ai.IsParalyzedByStatic || ai.IsNetImmobilized || ai.IsDistractedByDecoy));

            if (shouldCancel)
            {
                if (IsPerformingSpecialAction)
                {
                    InterruptActiveAttack(false);
                }
                return false;
            }

            // 2. ACTUALIZACIÓN DE TIMERS DE COOLDOWN
            float dt = Time.deltaTime;
            if (lungeCooldownTimer > 0f) lungeCooldownTimer -= dt;
            if (slamCooldownTimer > 0f) slamCooldownTimer -= dt;
            if (scrapCooldownTimer > 0f) scrapCooldownTimer -= dt;

            // 3. ACTUALIZACIÓN DE ACCIÓN ESPECIAL EN CURSO
            if (currentAction != BossActionState.None)
            {
                return UpdateActiveAction(agent, playerHC, dt);
            }

            // 4. EVALUAR DESENCADENAMIENTO DE ATAQUES ESPECIALES
            float distToKilo = Vector3.Distance(transform.position, playerHC.transform.position);
            bool isInCombatState = (ai != null && (ai.CurrentState == EnemyAI.EnemyState.Chase || ai.CurrentState == EnemyAI.EnemyState.Attack));

            if (isInCombatState)
            {
                // Ataque 2: Golpe Sísmico de Escombros (<= 4.6m)
                if (distToKilo <= 4.6f && slamCooldownTimer <= 0f)
                {
                    StartSlamWindup(agent);
                    return true;
                }

                // Ataque 1: Embestida Excavadora (3.8m - 11.5m con línea de visión limpia)
                if (distToKilo >= 3.8f && distToKilo <= 11.5f && lungeCooldownTimer <= 0f)
                {
                    Vector3 eyePos = transform.position + Vector3.up * 0.75f;
                    Vector3 playerEyePos = playerHC.transform.position + Vector3.up * 0.35f;
                    if (HasClearLineOfSight(eyePos, playerEyePos))
                    {
                        StartLungeWindup(agent, playerHC.transform.position);
                        return true;
                    }
                }

                // Ataque 3: Lanzamiento de Chatarra Tóxica (5.5m - 13.5m en Chase)
                if (ai.CurrentState == EnemyAI.EnemyState.Chase && distToKilo >= 5.5f && distToKilo <= 13.5f && scrapCooldownTimer <= 0f)
                {
                    if (distToKilo <= 7.5f || !TrapSystemPrototipo3.IsPollenCloudActive)
                    {
                        StartScrapBarrageWindup(agent, playerHC);
                        return true;
                    }
                }
            }

            // 5. MODIFICADORES DE MOVIMIENTO TÁCTICO CUANDO NO HAY ACCIÓN ESPECIAL ACTIVA

            // Patrón 1: Patrulla Pesada de Conserje (JanitorPatrolInspect) en Wander
            if (ai != null && ai.CurrentState == EnemyAI.EnemyState.Wander)
            {
                janitorPatrolTimer += dt;
                if (janitorPatrolTimer >= 5.5f)
                {
                    StartJanitorInspect(agent);
                    return true;
                }
            }
            else
            {
                janitorPatrolTimer = 0f;
            }

            // Patrones en Chase: FrenzyBurstChase y BurrowStepReposition
            if (ai != null && ai.CurrentState == EnemyAI.EnemyState.Chase)
            {
                // Patrón 3: Paso Subterráneo Corto (BurrowStepReposition si está atascado > 2.2s)
                if (agent.velocity.sqrMagnitude < 0.15f)
                {
                    burrowStuckTimer += dt;
                    if (burrowStuckTimer > 2.2f)
                    {
                        if (TryStartBurrowReposition(agent, playerHC.transform.position))
                        {
                            return true;
                        }
                        burrowStuckTimer = 1.0f; // Pequeño retroceso si no encontró NavMesh válido
                    }
                }
                else
                {
                    burrowStuckTimer = 0f;
                }

                // Patrón 2: Arranque Frenético (FrenzyBurstChase si Kilo se mantiene entre 5.0m y 11.5m por > 1.8s)
                if (distToKilo >= 5.0f && distToKilo <= 11.5f)
                {
                    frenzyDistanceTimer += dt;
                    if (frenzyDistanceTimer >= 1.8f && !isFrenzyBurstActive)
                    {
                        isFrenzyBurstActive = true;
                        frenzyBurstTimer = 1.4f;
                        frenzyDistanceTimer = 0f;
                        BoostFeralEyeLight(true);
                    }
                }
                else
                {
                    frenzyDistanceTimer = 0f;
                }

                if (isFrenzyBurstActive)
                {
                    frenzyBurstTimer -= dt;
                    float baseSpeed = ai.chaseSpeed;
                    float spikeFactor = ai.IsSpikeSlowed ? 0.5f : 1.0f;
                    agent.speed = (baseSpeed * 1.25f) * spikeFactor;

                    if (frenzyBurstTimer <= 0f)
                    {
                        isFrenzyBurstActive = false;
                        BoostFeralEyeLight(false);
                    }
                }
            }
            else
            {
                if (isFrenzyBurstActive)
                {
                    isFrenzyBurstActive = false;
                    BoostFeralEyeLight(false);
                }
                frenzyDistanceTimer = 0f;
                burrowStuckTimer = 0f;
            }

            // Permitir que el Wander/Chase habitual continúe
            return false;
        }

        private bool UpdateActiveAction(NavMeshAgent agent, HamsterController playerHC, float dt)
        {
            actionTimer += dt;

            switch (currentAction)
            {
                case BossActionState.JanitorInspect:
                    UpdateJanitorInspect(agent);
                    return true;

                case BossActionState.BurrowReposition:
                    UpdateBurrowReposition(agent);
                    return true;

                case BossActionState.WallCrashStun:
                    UpdateWallCrashStun(agent);
                    return true;

                case BossActionState.RecoveryFatigue:
                    UpdateRecoveryFatigue(agent, playerHC);
                    return true;

                case BossActionState.LungeWindup:
                    UpdateLungeWindup(agent);
                    return true;

                case BossActionState.LungeCharge:
                    UpdateLungeCharge(agent, playerHC, dt);
                    return true;

                case BossActionState.SlamWindup:
                    UpdateSlamWindup(agent, playerHC);
                    return true;

                case BossActionState.ScrapBarrageWindup:
                    UpdateScrapBarrageWindup(agent);
                    return true;
            }

            return false;
        }

        #region ATAQUE 1: EMBESTIDA EXCAVADORA (Feral Lunge)

        private void StartLungeWindup(NavMeshAgent agent, Vector3 targetKiloPos)
        {
            currentAction = BossActionState.LungeWindup;
            actionTimer = 0f;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            lockedChargeDir = targetKiloPos - transform.position;
            lockedChargeDir.y = 0f;
            if (lockedChargeDir.sqrMagnitude < 0.01f) lockedChargeDir = transform.forward;
            lockedChargeDir.Normalize();

            transform.rotation = Quaternion.LookRotation(lockedChargeDir, Vector3.up);

            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;
            CreateLungeTelegraph(transform.position, lockedChargeDir, 0.95f, 9.5f, surfaceY);
        }

        private void UpdateLungeWindup(NavMeshAgent agent)
        {
            UpdateLungeTelegraph();

            // Vibración sísmica previa de VisualMesh_ReplaceMe
            if (visualMeshContainer != null)
            {
                float shake = Mathf.Sin(Time.time * 48f) * 0.05f;
                visualMeshContainer.localPosition = new Vector3(shake, 0f, 0f);
            }

            if (actionTimer >= lungeWindupDuration)
            {
                DestroyLungeTelegraph();
                if (visualMeshContainer != null) visualMeshContainer.localPosition = Vector3.zero;

                currentAction = BossActionState.LungeCharge;
                actionTimer = 0f;
                hasHitKiloInLunge = false;
            }
        }

        private void UpdateLungeCharge(NavMeshAgent agent, HamsterController playerHC, float dt)
        {
            float speed = lungeChargeSpeed * ((ai != null && ai.IsSpikeSlowed) ? 0.5f : 1.0f);
            Vector3 moveDelta = lockedChargeDir * (speed * dt);

            if (agent.isOnNavMesh)
            {
                agent.Move(moveDelta);
            }
            else
            {
                transform.position += moveDelta;
            }

            transform.rotation = Quaternion.LookRotation(lockedChargeDir, Vector3.up);

            // 1. Verificación de impacto con Kilo
            Vector3 toKilo = playerHC.transform.position - transform.position;
            toKilo.y = 0f;
            if (toKilo.sqrMagnitude <= (0.85f * 0.85f) && !playerHC.IsDead)
            {
                hasHitKiloInLunge = true;
                playerHC.ReceiveDamage();

                HamsterControllerPrototipo3 protoHC = playerHC as HamsterControllerPrototipo3;
                if (protoHC != null)
                {
                    protoHC.ApplyExternalSlow(0.45f, 1.5f);
                }

                EndLungeCharge(true, playerHC);
                return;
            }

            // 2. Verificación de choque contra Muros de Edificios o Barricada de Madera
            Vector3 checkCenter = transform.position + Vector3.up * 0.5f + lockedChargeDir * 0.55f;
            Collider[] hits = Physics.OverlapSphere(checkCenter, 0.55f, ~0, QueryTriggerInteraction.Ignore);
            bool wallCrash = false;
            GameObject barricadeToDestroy = null;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform) || col.CompareTag("Player"))
                {
                    continue;
                }

                // Barricada de madera
                if (col.gameObject.name.Contains("Barricada_Madera") || (col.transform.parent != null && col.transform.parent.gameObject.name.Contains("Barricada_Madera")))
                {
                    barricadeToDestroy = col.gameObject.name.Contains("Barricada_Madera") ? col.gameObject : col.transform.parent.gameObject;
                    wallCrash = true;
                    break;
                }

                // Muro de edificio
                if (col.GetComponentInParent<BuildingData>() != null || col.GetComponentInParent<CitySocket>() != null)
                {
                    string cName = col.gameObject.name;
                    if (cName.Contains("Pavement") || cName.Contains("Floor") || cName.Contains("Ground"))
                    {
                        continue;
                    }
                    wallCrash = true;
                    break;
                }
            }

            if (wallCrash)
            {
                if (barricadeToDestroy != null)
                {
                    Destroy(barricadeToDestroy);
                }

                // Choque violento contra obstáculo sólido: aturdimiento WallCrashStun
                InterruptActiveAttack(applyStun: true, stunDuration: wallCrashStunDuration);
                return;
            }

            // 3. Fin de la embestida tras transcurrir el tiempo de carga
            if (actionTimer >= lungeChargeDuration)
            {
                EndLungeCharge(false, playerHC);
            }
        }

        private void EndLungeCharge(bool hitKilo, HamsterController playerHC)
        {
            if (visualMeshContainer != null)
            {
                visualMeshContainer.localPosition = Vector3.zero;
                visualMeshContainer.localRotation = Quaternion.identity;
                visualMeshContainer.localScale = Vector3.one;
            }

            // Recompensa narrativa / mecánica si Kilo esquivó la embestida
            if (!hitKilo && playerHC != null)
            {
                float distKilo = Vector3.Distance(transform.position, playerHC.transform.position);
                if (playerHC.IsDashing || distKilo > 2.0f)
                {
                    QuestCompiler.Instance?.AdvanceActionIfMatches("dash_escape", "", transform);
                }
            }

            lungeCooldownTimer = lungeCooldown;
            specialAttacksInARow++;

            if (specialAttacksInARow >= 2)
            {
                StartRecoveryFatigue();
            }
            else
            {
                currentAction = BossActionState.None;
                if (ai != null && ai.GetComponent<NavMeshAgent>() is NavMeshAgent ag && ag.isOnNavMesh)
                {
                    ag.isStopped = false;
                }
            }
        }

        #endregion

        #region ATAQUE 2: GOLPE SÍSMICO DE ESCOMBROS (Ground Slam Shockwave)

        private void StartSlamWindup(NavMeshAgent agent)
        {
            currentAction = BossActionState.SlamWindup;
            actionTimer = 0f;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // Marco se estira hacia arriba y se inclina amenazante
            if (visualMeshContainer != null)
            {
                visualMeshContainer.localScale = new Vector3(0.92f, 1.25f, 0.92f);
                visualMeshContainer.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            }

            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;
            CreateSlamWarningDisc(transform.position, slamRadius, surfaceY);
        }

        private void UpdateSlamWindup(NavMeshAgent agent, HamsterController playerHC)
        {
            UpdateSlamWarningDisc();

            if (actionTimer >= slamWindupDuration)
            {
                DestroySlamWarningDisc();

                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localScale = Vector3.one;
                    visualMeshContainer.localRotation = Quaternion.identity;
                }

                // Generar anillo sísmico en expansión
                float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;
                GameObject ringObj = new GameObject("MarcoShockwaveRing");
                ringObj.transform.position = new Vector3(transform.position.x, surfaceY + 0.12f, transform.position.z);
                MarcoShockwaveRing ring = ringObj.AddComponent<MarcoShockwaveRing>();
                ring.Initialize(playerHC);

                slamCooldownTimer = slamCooldown;
                specialAttacksInARow++;

                if (specialAttacksInARow >= 2)
                {
                    StartRecoveryFatigue();
                }
                else
                {
                    currentAction = BossActionState.None;
                    if (agent.isOnNavMesh) agent.isStopped = false;
                }
            }
        }

        #endregion

        #region ATAQUE 3: LANZAMIENTO DE CHATARRA TÓXICA (Scrap Barrage)

        private void StartScrapBarrageWindup(NavMeshAgent agent, HamsterController playerHC)
        {
            currentAction = BossActionState.ScrapBarrageWindup;
            actionTimer = 0f;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            CalculateScrapTargets(playerHC);

            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;
            DestroyAllScrapReticles();
            for (int i = 0; i < cachedScrapTargets.Count; i++)
            {
                CreateScrapReticle(cachedScrapTargets[i], 1.75f, surfaceY);
            }

            // Inclinación hacia atrás para preparar el lanzamiento de escombros
            if (visualMeshContainer != null)
            {
                visualMeshContainer.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            }
        }

        private void CalculateScrapTargets(HamsterController playerHC)
        {
            cachedScrapTargets.Clear();
            Vector3 pCenter = playerHC.transform.position;
            Vector3 fwd = playerHC.transform.forward;
            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;

            // Diana 1: Directa al jugador con ligera anticipación frontal
            Vector3 t0 = pCenter + fwd * 0.8f;
            t0.y = surfaceY;
            cachedScrapTargets.Add(ValidateTargetPoint(t0, surfaceY));

            // Diana 2: Flanco izquierdo (~2.2m)
            Vector3 leftOffset = Quaternion.Euler(0f, -65f, 0f) * fwd * 2.2f;
            Vector3 t1 = pCenter + leftOffset;
            t1.y = surfaceY;
            cachedScrapTargets.Add(ValidateTargetPoint(t1, surfaceY));

            // Diana 3: Flanco derecho (~2.2m)
            Vector3 rightOffset = Quaternion.Euler(0f, 65f, 0f) * fwd * 2.2f;
            Vector3 t2 = pCenter + rightOffset;
            t2.y = surfaceY;
            cachedScrapTargets.Add(ValidateTargetPoint(t2, surfaceY));
        }

        private Vector3 ValidateTargetPoint(Vector3 candidate, float surfaceY)
        {
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3.5f, NavMesh.AllAreas))
            {
                if (hit.position.y >= surfaceY - 0.25f)
                {
                    return hit.position;
                }
            }
            candidate.y = surfaceY;
            return candidate;
        }

        private void UpdateScrapBarrageWindup(NavMeshAgent agent)
        {
            UpdateScrapReticles();

            if (actionTimer >= scrapWindupDuration)
            {
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localRotation = Quaternion.identity;
                }

                Vector3 launchOrigin = transform.position + Vector3.up * 1.3f + transform.forward * 0.6f;

                for (int i = 0; i < cachedScrapTargets.Count; i++)
                {
                    Vector3 targetPos = cachedScrapTargets[i];
                    GameObject reticle = (i < activeScrapReticles.Count) ? activeScrapReticles[i] : null;

                    GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    projObj.name = $"Marco_ScrapProjectile_{i}";
                    projObj.transform.localScale = new Vector3(0.40f, 0.45f, 0.35f);
                    Collider c = projObj.GetComponent<Collider>();
                    if (c != null) Destroy(c);

                    Renderer r = projObj.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                        Material m = new Material(sh) { color = new Color(0.42f, 0.32f, 0.20f) };
                        r.material = m;
                    }

                    MarcoScrapProjectile proj = projObj.AddComponent<MarcoScrapProjectile>();
                    proj.Launch(launchOrigin, targetPos, reticle, 0.70f, 2.6f);
                }

                // El proyectil asume la posesión de las dianas reticulares para destruirlas al impactar
                activeScrapReticles.Clear();
                activeScrapReticleMats.Clear();

                scrapCooldownTimer = scrapCooldown;
                specialAttacksInARow++;

                if (specialAttacksInARow >= 2)
                {
                    StartRecoveryFatigue();
                }
                else
                {
                    currentAction = BossActionState.None;
                    if (agent.isOnNavMesh) agent.isStopped = false;
                }
            }
        }

        #endregion

        #region PATRONES DE MOVIMIENTO (Janitor, Burrow, Fatigue, Stun)

        private void StartJanitorInspect(NavMeshAgent agent)
        {
            currentAction = BossActionState.JanitorInspect;
            actionTimer = 0f;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // Inclinación hacia adelante como si examinara/raspara el suelo de Solis
            if (visualMeshContainer != null)
            {
                visualMeshContainer.localRotation = Quaternion.Euler(24f, 0f, 0f);
            }

            CreateJanitorDustRing();
        }

        private void UpdateJanitorInspect(NavMeshAgent agent)
        {
            float progress = Mathf.Clamp01(actionTimer / janitorInspectDuration);
            UpdateJanitorDustRing(progress);

            if (actionTimer >= janitorInspectDuration)
            {
                DestroyJanitorDustRing();
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localRotation = Quaternion.identity;
                }

                janitorPatrolTimer = 0f;
                currentAction = BossActionState.None;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
        }

        private bool TryStartBurrowReposition(NavMeshAgent agent, Vector3 targetKiloPos)
        {
            Vector3 dirToKilo = targetKiloPos - transform.position;
            dirToKilo.y = 0f;
            float dist = dirToKilo.magnitude;
            if (dist < 0.1f) return false;
            dirToKilo.Normalize();

            float warpDistance = Mathf.Min(3.2f, dist * 0.75f);
            Vector3 candidate = transform.position + dirToKilo * warpDistance;
            float surfaceY = ai != null ? ai.minCitySurfaceY : transform.position.y;
            candidate.y = surfaceY;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                Vector3 homePos = dualRole != null ? dualRole.HomeAnchorPosition : transform.position;
                float leashDist = ai != null ? ai.bossLeashDistance : 26f;

                if (hit.position.y >= surfaceY - 0.25f && Vector3.Distance(hit.position, homePos) <= leashDist)
                {
                    burrowWarpDestination = hit.position;
                    currentAction = BossActionState.BurrowReposition;
                    actionTimer = 0f;
                    hasWarpedInBurrow = false;

                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity = Vector3.zero;
                    }

                    CreateBurrowDustPuff(transform.position);
                    return true;
                }
            }

            return false;
        }

        private void UpdateBurrowReposition(NavMeshAgent agent)
        {
            float halfTime = burrowDuration * 0.5f;

            if (actionTimer < halfTime)
            {
                // Hundimiento en el suelo (-0.45m)
                float sinkProgress = actionTimer / halfTime;
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localPosition = new Vector3(0f, Mathf.Lerp(0f, -0.45f, sinkProgress), 0f);
                }
            }
            else
            {
                // Teletransporte subterráneo en el punto medio
                if (!hasWarpedInBurrow)
                {
                    hasWarpedInBurrow = true;
                    if (agent.isOnNavMesh)
                    {
                        agent.Warp(burrowWarpDestination);
                    }
                    else
                    {
                        transform.position = burrowWarpDestination;
                    }
                    CreateBurrowDustPuff(transform.position);
                }

                // Resurgimiento a la superficie
                float riseProgress = (actionTimer - halfTime) / halfTime;
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localPosition = new Vector3(0f, Mathf.Lerp(-0.45f, 0f, riseProgress), 0f);
                }
            }

            if (actionTimer >= burrowDuration)
            {
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localPosition = Vector3.zero;
                }

                burrowStuckTimer = 0f;
                currentAction = BossActionState.None;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
        }

        private void StartRecoveryFatigue()
        {
            currentAction = BossActionState.RecoveryFatigue;
            actionTimer = 0f;

            CreateFatigueVisual();

            if (ai != null && ai.GetComponent<NavMeshAgent>() is NavMeshAgent ag && ag.isOnNavMesh)
            {
                ag.isStopped = false;
                ag.speed = 1.6f;
            }
        }

        private void UpdateRecoveryFatigue(NavMeshAgent agent, HamsterController playerHC)
        {
            UpdateFatigueVisual();

            // Desplazamiento lento y cansado hacia Kilo
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = 1.6f;
                if (playerHC != null)
                {
                    agent.SetDestination(playerHC.transform.position);
                }
            }

            if (actionTimer >= fatigueRecoveryDuration)
            {
                DestroyFatigueVisual();
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localScale = Vector3.one;
                }

                specialAttacksInARow = 0;
                currentAction = BossActionState.None;
            }
        }

        private void UpdateWallCrashStun(NavMeshAgent agent)
        {
            UpdateDizzyVisual();

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            if (actionTimer >= activeStunDuration)
            {
                DestroyDizzyVisual();
                if (visualMeshContainer != null)
                {
                    visualMeshContainer.localPosition = Vector3.zero;
                    visualMeshContainer.localRotation = Quaternion.identity;
                    visualMeshContainer.localScale = Vector3.one;
                }

                specialAttacksInARow = 0;
                currentAction = BossActionState.None;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
        }

        #endregion

        #region CONTROL DE INTERRUPCIÓN Y CANCELACIÓN

        /// <summary>
        /// Cancela de inmediato cualquier ataque o acción en curso, destruye telegraphs activos,
        /// restablece la malla visual a origen e impone opcionalmente aturdimiento por choque (WallCrashStun).
        /// </summary>
        public void InterruptActiveAttack(bool applyStun = false, float stunDuration = 0f)
        {
            // 1. Destrucción limpia de efectos visuales y materiales en memoria
            DestroyLungeTelegraph();
            DestroySlamWarningDisc();
            DestroyAllScrapReticles();
            DestroyDizzyVisual();
            DestroyFatigueVisual();
            DestroyJanitorDustRing();

            // 2. Restauración de aceleración frenética y ojos emisivos
            if (isFrenzyBurstActive)
            {
                isFrenzyBurstActive = false;
                frenzyBurstTimer = 0f;
                BoostFeralEyeLight(false);
            }

            // 3. Reseteo del contenedor visual VisualMesh_ReplaceMe
            if (visualMeshContainer != null)
            {
                visualMeshContainer.localPosition = Vector3.zero;
                visualMeshContainer.localRotation = Quaternion.identity;
                visualMeshContainer.localScale = Vector3.one;
            }

            NavMeshAgent agent = GetComponent<NavMeshAgent>();

            // 4. Aplicación de aturdimiento o liberación limpia del NavMeshAgent
            if (applyStun)
            {
                currentAction = BossActionState.WallCrashStun;
                activeStunDuration = stunDuration > 0f ? stunDuration : wallCrashStunDuration;
                actionTimer = 0f;

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                CreateDizzyVisual();
            }
            else
            {
                currentAction = BossActionState.None;
                actionTimer = 0f;

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }
        }

        private void BoostFeralEyeLight(bool boost)
        {
            if (dualRole != null && dualRole.feralEyeLight != null)
            {
                if (!hasOriginalEyeIntensity)
                {
                    originalEyeLightIntensity = dualRole.feralEyeLight.intensity;
                    hasOriginalEyeIntensity = true;
                }
                dualRole.feralEyeLight.intensity = boost ? (originalEyeLightIntensity * 1.8f) : originalEyeLightIntensity;
            }
        }

        private bool HasClearLineOfSight(Vector3 fromPos, Vector3 toPos)
        {
            Vector3 dir = toPos - fromPos;
            float dist = dir.magnitude;
            if (dist <= 0.05f) return true;

            RaycastHit[] hits = Physics.RaycastAll(fromPos, dir.normalized, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform) || col.CompareTag("Player"))
                {
                    continue;
                }

                if (col.gameObject.name.Contains("Barricada_Madera") || (col.transform.parent != null && col.transform.parent.gameObject.name.Contains("Barricada_Madera")))
                {
                    return false;
                }

                if (col.GetComponentInParent<BuildingData>() != null || col.GetComponentInParent<CitySocket>() != null)
                {
                    string cName = col.gameObject.name;
                    if (cName.Contains("Pavement") || cName.Contains("Floor") || cName.Contains("Ground"))
                    {
                        continue;
                    }
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region CONSTRUCCIÓN Y LIMPIEZA DE VISUALES PROCEDIMENTALES

        private void CreateLungeTelegraph(Vector3 startPos, Vector3 fwdDir, float width, float length, float surfaceY)
        {
            DestroyLungeTelegraph();

            lungeTelegraphObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lungeTelegraphObj.name = "Marco_LungeTelegraph";
            Collider c = lungeTelegraphObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Vector3 center = startPos + fwdDir * (length * 0.5f);
            center.y = surfaceY + 0.04f;
            lungeTelegraphObj.transform.position = center;
            lungeTelegraphObj.transform.rotation = Quaternion.LookRotation(fwdDir, Vector3.up);
            lungeTelegraphObj.transform.localScale = new Vector3(width, 0.02f, length);

            Renderer r = lungeTelegraphObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                lungeTelegraphMat = new Material(sh) { color = new Color(1.0f, 0.28f, 0.05f, 0.58f) };
                r.material = lungeTelegraphMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private void UpdateLungeTelegraph()
        {
            if (lungeTelegraphMat != null)
            {
                Color c = lungeTelegraphMat.color;
                c.a = 0.42f + Mathf.PingPong(Time.time * 6f, 0.35f);
                lungeTelegraphMat.color = c;
            }
        }

        private void DestroyLungeTelegraph()
        {
            if (lungeTelegraphObj != null)
            {
                Destroy(lungeTelegraphObj);
                lungeTelegraphObj = null;
            }
            if (lungeTelegraphMat != null)
            {
                Destroy(lungeTelegraphMat);
                lungeTelegraphMat = null;
            }
        }

        private void CreateSlamWarningDisc(Vector3 center, float radius, float surfaceY)
        {
            DestroySlamWarningDisc();

            slamWarningDiscObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slamWarningDiscObj.name = "Marco_SlamWarningDisc";
            Collider c = slamWarningDiscObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            center.y = surfaceY + 0.05f;
            slamWarningDiscObj.transform.position = center;
            slamWarningDiscObj.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            Renderer r = slamWarningDiscObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                slamWarningDiscMat = new Material(sh) { color = new Color(0.95f, 0.35f, 0.08f, 0.45f) };
                r.material = slamWarningDiscMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private void UpdateSlamWarningDisc()
        {
            if (slamWarningDiscMat != null)
            {
                Color c = slamWarningDiscMat.color;
                c.a = 0.32f + Mathf.Sin(Time.time * 8f) * 0.18f;
                slamWarningDiscMat.color = c;
            }
        }

        private void DestroySlamWarningDisc()
        {
            if (slamWarningDiscObj != null)
            {
                Destroy(slamWarningDiscObj);
                slamWarningDiscObj = null;
            }
            if (slamWarningDiscMat != null)
            {
                Destroy(slamWarningDiscMat);
                slamWarningDiscMat = null;
            }
        }

        private void CreateScrapReticle(Vector3 center, float radius, float surfaceY)
        {
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reticle.name = "Marco_ScrapReticle";
            Collider c = reticle.GetComponent<Collider>();
            if (c != null) Destroy(c);

            center.y = surfaceY + 0.04f;
            reticle.transform.position = center;
            reticle.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            Renderer r = reticle.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                Material mat = new Material(sh) { color = new Color(0.75f, 0.85f, 0.15f, 0.45f) };
                r.material = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                activeScrapReticleMats.Add(mat);
            }
            activeScrapReticles.Add(reticle);
        }

        private void UpdateScrapReticles()
        {
            for (int i = 0; i < activeScrapReticleMats.Count; i++)
            {
                Material m = activeScrapReticleMats[i];
                if (m != null)
                {
                    Color c = m.color;
                    c.a = 0.30f + Mathf.PingPong(Time.time * 5f, 0.30f);
                    m.color = c;
                }
            }
        }

        private void DestroyAllScrapReticles()
        {
            for (int i = 0; i < activeScrapReticles.Count; i++)
            {
                if (activeScrapReticles[i] != null) Destroy(activeScrapReticles[i]);
            }
            activeScrapReticles.Clear();

            for (int i = 0; i < activeScrapReticleMats.Count; i++)
            {
                if (activeScrapReticleMats[i] != null) Destroy(activeScrapReticleMats[i]);
            }
            activeScrapReticleMats.Clear();
        }

        private void CreateDizzyVisual()
        {
            DestroyDizzyVisual();

            dizzyVisualObj = new GameObject("Marco_DizzyStunEffect");
            dizzyVisualObj.transform.SetParent(transform, false);
            dizzyVisualObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);

            Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            dizzyMat = new Material(sh) { color = new Color(1.0f, 0.85f, 0.18f, 0.95f) };

            for (int i = 0; i < 3; i++)
            {
                float angle = (i / 3f) * Mathf.PI * 2f;
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = $"DizzyOrb_{i}";
                star.transform.SetParent(dizzyVisualObj.transform, false);
                star.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.45f, 0f, Mathf.Sin(angle) * 0.45f);
                star.transform.localScale = Vector3.one * 0.18f;

                Collider col = star.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer rend = star.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = dizzyMat;
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private void UpdateDizzyVisual()
        {
            if (dizzyVisualObj != null)
            {
                dizzyVisualObj.transform.Rotate(Vector3.up, 380f * Time.deltaTime, Space.Self);
                dizzyVisualObj.transform.localPosition = new Vector3(0f, 1.85f + Mathf.Sin(Time.time * 6f) * 0.08f, 0f);
            }
        }

        private void DestroyDizzyVisual()
        {
            if (dizzyVisualObj != null)
            {
                Destroy(dizzyVisualObj);
                dizzyVisualObj = null;
            }
            if (dizzyMat != null)
            {
                Destroy(dizzyMat);
                dizzyMat = null;
            }
        }

        private void CreateFatigueVisual()
        {
            DestroyFatigueVisual();

            fatigueVisualObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fatigueVisualObj.name = "Marco_FatigueRing";
            fatigueVisualObj.transform.SetParent(transform, false);
            fatigueVisualObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            fatigueVisualObj.transform.localScale = new Vector3(2.6f, 0.02f, 2.6f);

            Collider c = fatigueVisualObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = fatigueVisualObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                fatigueMat = new Material(sh) { color = new Color(0.65f, 0.58f, 0.38f, 0.45f) };
                r.material = fatigueMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void UpdateFatigueVisual()
        {
            if (fatigueVisualObj != null && fatigueMat != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.12f;
                fatigueVisualObj.transform.localScale = new Vector3(2.6f * pulse, 0.02f, 2.6f * pulse);
                Color col = fatigueMat.color;
                col.a = 0.35f + Mathf.Sin(Time.time * 4f) * 0.15f;
                fatigueMat.color = col;
            }

            if (visualMeshContainer != null)
            {
                float heave = 1f + Mathf.Sin(Time.time * 4.5f) * 0.06f;
                visualMeshContainer.localScale = new Vector3(1f, heave, 1f);
            }
        }

        private void DestroyFatigueVisual()
        {
            if (fatigueVisualObj != null)
            {
                Destroy(fatigueVisualObj);
                fatigueVisualObj = null;
            }
            if (fatigueMat != null)
            {
                Destroy(fatigueMat);
                fatigueMat = null;
            }
            if (visualMeshContainer != null)
            {
                visualMeshContainer.localScale = Vector3.one;
            }
        }

        private void CreateJanitorDustRing()
        {
            DestroyJanitorDustRing();

            janitorDustObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            janitorDustObj.name = "Marco_JanitorDustRing";
            janitorDustObj.transform.position = transform.position + Vector3.up * 0.06f;
            janitorDustObj.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);

            Collider c = janitorDustObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = janitorDustObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                janitorDustMat = new Material(sh) { color = new Color(0.85f, 0.65f, 0.25f, 0.55f) };
                r.material = janitorDustMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void UpdateJanitorDustRing(float progress)
        {
            if (janitorDustObj != null)
            {
                float scale = Mathf.Lerp(0.8f, 2.2f, progress);
                janitorDustObj.transform.localScale = new Vector3(scale, 0.02f, scale);
                if (janitorDustMat != null)
                {
                    Color c = janitorDustMat.color;
                    c.a = Mathf.Lerp(0.55f, 0f, progress);
                    janitorDustMat.color = c;
                }
            }
        }

        private void DestroyJanitorDustRing()
        {
            if (janitorDustObj != null)
            {
                Destroy(janitorDustObj);
                janitorDustObj = null;
            }
            if (janitorDustMat != null)
            {
                Destroy(janitorDustMat);
                janitorDustMat = null;
            }
        }

        private void CreateBurrowDustPuff(Vector3 worldPos)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Marco_BurrowDustPuff";
            ring.transform.position = worldPos + Vector3.up * 0.05f;
            ring.transform.localScale = new Vector3(1.4f, 0.02f, 1.4f);

            Collider c = ring.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = ring.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                Material m = new Material(sh) { color = new Color(0.48f, 0.35f, 0.22f, 0.65f) };
                r.material = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Destroy(m, 0.8f);
            }
            Destroy(ring, 0.8f);
        }

        #endregion

        private void OnDisable()
        {
            InterruptActiveAttack(false);
        }

        private void OnDestroy()
        {
            InterruptActiveAttack(false);
        }
    }

    /// <summary>
    /// Anillo sísmico procedimental que se expande tras el Golpe Sísmico de Marco (GroundSlamShockwave).
    /// Kilo puede esquivarlo completamente saltando en el aire.
    /// </summary>
    public class MarcoShockwaveRing : MonoBehaviour
    {
        private const float Duration = 0.75f;
        private const float StartRadius = 0.6f;
        private const float EndRadius = 5.2f;
        private const float RingThickness = 0.55f;

        private HamsterController playerHC;
        private HamsterControllerPrototipo3 playerProto;
        private Vector3 origin;
        private float elapsed = 0f;
        private bool hasHitPlayer = false;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh ringMesh;
        private Material ringMat;

        public void Initialize(HamsterController player)
        {
            this.playerHC = player;
            this.playerProto = player as HamsterControllerPrototipo3;
            this.origin = transform.position;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            ringMesh = new Mesh { name = "ShockwaveRingMesh" };
            meshFilter.mesh = ringMesh;

            Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            ringMat = new Material(sh) { color = new Color(1.0f, 0.55f, 0.10f, 0.78f) };
            meshRenderer.material = ringMat;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            UpdateRingGeometry(StartRadius, 0.78f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Duration);
            float currentRadius = Mathf.Lerp(StartRadius, EndRadius, progress);
            float alpha = Mathf.Lerp(0.78f, 0.0f, progress * progress);

            UpdateRingGeometry(currentRadius, alpha);

            // Verificación del frente de onda con Kilo
            if (!hasHitPlayer && playerHC != null && !playerHC.IsDead)
            {
                Vector3 toPlayer = playerHC.transform.position - origin;
                float distOrigin = new Vector2(toPlayer.x, toPlayer.z).magnitude;

                if (Mathf.Abs(distOrigin - currentRadius) <= (RingThickness * 0.65f))
                {
                    hasHitPlayer = true;

                    // Counter-play: si Kilo está en el aire (saltando o despegado del suelo), esquiva la onda sísmica
                    bool isDodgingInAir = playerProto != null && (!playerProto.IsGrounded || playerProto.IsJumping);

                    if (!isDodgingInAir)
                    {
                        if (playerProto != null)
                        {
                            playerProto.ApplyExternalSlow(0.38f, 2.4f);
                        }
                        playerHC.currentStamina = Mathf.Max(0f, playerHC.currentStamina - 18f);

                        // Impacto a quemarropa (< 2.0m)
                        if (distOrigin <= 2.0f)
                        {
                            playerHC.ReceiveDamage();
                        }
                    }
                }
            }

            if (elapsed >= Duration)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateRingGeometry(float currentRadius, float alpha)
        {
            const int segments = 32;
            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            int[] triangles = new int[segments * 6];

            float rInner = Mathf.Max(0.1f, currentRadius - RingThickness * 0.5f);
            float rOuter = currentRadius + RingThickness * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * rInner, 0f, sin * rInner);
                vertices[i * 2 + 1] = new Vector3(cos * rOuter, 0f, sin * rOuter);

                if (i < segments)
                {
                    int baseIdx = i * 2;
                    int triBase = i * 6;

                    triangles[triBase] = baseIdx;
                    triangles[triBase + 1] = baseIdx + 1;
                    triangles[triBase + 2] = baseIdx + 2;

                    triangles[triBase + 3] = baseIdx + 1;
                    triangles[triBase + 4] = baseIdx + 3;
                    triangles[triBase + 5] = baseIdx + 2;
                }
            }

            ringMesh.Clear();
            ringMesh.vertices = vertices;
            ringMesh.triangles = triangles;
            ringMesh.RecalculateNormals();

            if (ringMat != null)
            {
                Color c = ringMat.color;
                c.a = alpha;
                ringMat.color = c;
            }
        }

        private void OnDestroy()
        {
            if (ringMesh != null) Destroy(ringMesh);
            if (ringMat != null) Destroy(ringMat);
        }
    }

    /// <summary>
    /// Proyectil parabólico de chatarra lanzado por Marco en ScrapBarrage.
    /// Al aterrizar genera una ToxicScrapHazardZone y destruye su retícula diana.
    /// </summary>
    public class MarcoScrapProjectile : MonoBehaviour
    {
        private Vector3 startPos;
        private Vector3 targetPos;
        private float flightTime = 0.70f;
        private float arcHeight = 2.6f;
        private GameObject associatedReticle;
        private float elapsed = 0f;
        private Material scrapMat;

        public void Launch(Vector3 start, Vector3 target, GameObject reticle, float duration = 0.70f, float height = 2.6f)
        {
            this.startPos = start;
            this.targetPos = target;
            this.associatedReticle = reticle;
            this.flightTime = duration;
            this.arcHeight = height;
            transform.position = start;

            Renderer r = GetComponent<Renderer>();
            if (r != null) scrapMat = r.material;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightTime);

            Vector3 current = Vector3.Lerp(startPos, targetPos, t);
            current.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = current;

            transform.Rotate(240f * Time.deltaTime, 360f * Time.deltaTime, 120f * Time.deltaTime);

            if (elapsed >= flightTime)
            {
                // Destruir diana reticular asociada
                if (associatedReticle != null)
                {
                    Destroy(associatedReticle);
                    associatedReticle = null;
                }

                // Desplegar zona de peligro de chatarra tóxica
                GameObject zoneObj = new GameObject("ToxicScrapHazardZone");
                zoneObj.transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);
                ToxicScrapHazardZone zone = zoneObj.AddComponent<ToxicScrapHazardZone>();
                zone.duration = 5.0f;
                zone.radius = 1.75f;
                zone.slowMultiplier = 0.50f;

                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (associatedReticle != null)
            {
                Destroy(associatedReticle);
                associatedReticle = null;
            }
            if (scrapMat != null)
            {
                Destroy(scrapMat);
            }
        }
    }

    /// <summary>
    /// Zona de peligro dejada por la chatarra tóxica de Marco.
    /// Ralentiza a Kilo si camina sobre ella estando en el suelo.
    /// </summary>
    public class ToxicScrapHazardZone : MonoBehaviour
    {
        public float duration = 5.0f;
        public float radius = 1.75f;
        public float slowMultiplier = 0.50f;
        public Color hazardColor = new Color(0.68f, 0.78f, 0.12f, 0.42f);

        private float elapsed = 0f;
        private Renderer puddleRenderer;
        private Material hazardMaterial;

        private void Start()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ToxicHazardVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            visual.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            Collider col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            puddleRenderer = visual.GetComponent<Renderer>();
            if (puddleRenderer != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                hazardMaterial = new Material(sh) { color = hazardColor };
                puddleRenderer.material = hazardMaterial;
                puddleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                puddleRenderer.receiveShadows = false;
            }

            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            float lifeNormalized = 1f - (elapsed / duration);
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
            transform.localScale = new Vector3(pulse, 1f, pulse);

            if (hazardMaterial != null)
            {
                Color c = hazardColor;
                c.a = hazardColor.a * Mathf.Clamp01(lifeNormalized * 1.3f);
                hazardMaterial.color = c;
            }

            // Ralentización a Kilo cuando está en el suelo
            HamsterControllerPrototipo3 player = FindAnyObjectByType<HamsterControllerPrototipo3>();
            if (player != null && !player.IsDead && player.IsGrounded)
            {
                Vector3 delta = player.transform.position - transform.position;
                if (Mathf.Abs(delta.y) <= 1.4f)
                {
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= radius * radius)
                    {
                        player.ApplyExternalSlow(slowMultiplier, 0.4f);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (hazardMaterial != null)
            {
                Destroy(hazardMaterial);
            }
        }
    }
}
