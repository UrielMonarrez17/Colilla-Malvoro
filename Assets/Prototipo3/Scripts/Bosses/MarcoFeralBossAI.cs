using System;
using UnityEngine;
using UnityEngine.AI;

namespace Prototipo3
{
    /// <summary>
    /// Jefe 1: MARCO EL TEJÓN (Feral — INVENCIBLE).
    /// - No puede ser derrotado: las trampas solo lo aturden/frenan brevemente (1.6s) para darte margen de escape.
    /// - Mecánicas únicas:
    ///   1. Embestida Sísmica con línea de advertencia naranja en el suelo.
    ///   2. Golpe de Escombros (lanza proyectiles parabólicos SM_BadgerDebrisProjectile).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MarcoFeralBossAI : MonoBehaviour
    {
        [Header("Parámetros de Jefe Invencible")]
        public float basePursuitSpeed = 6.7f;
        public float bullChargeSpeed = 11.8f;
        public float meleeHitRange = 1.95f;
        public float escapeDistanceRequired = 44f;
        public float minPursuitTimeBeforeEscape = 15f;
        public GameObject debrisProjectilePrefab;

        public event Action OnPlayerEscapedMarco;

        private NavMeshAgent agent;
        private Animator anim;
        private HamsterController player;
        private LineRenderer chargeTelegraphLine;
        private readonly NavMeshAgentGroundAligner groundAligner = new NavMeshAgentGroundAligner();

        private float activePursuitTimer = 0f;
        private float safeRefugeStayTimer = 0f;
        private float meleeCooldown = 0f;
        private float specialCooldown = 3.2f;
        private float staggerTimer = 0f;

        private enum BossSubState { Pursuing, PostHitRecovery, WindupCharge, Charging, ThrowingDebris }
        private BossSubState subState = BossSubState.Pursuing;
        private float stateTimer = 0f;
        private Vector3 chargeDirection;
        private bool escapeTriggered = false;

        private void Awake()
        {
            gameObject.tag = "Enemy";
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.stoppingDistance = 1.45f;

            // Hacer su collider trigger para que jamás pueda prensar ni trabar físicamente el Rigidbody del hámster
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            anim = GetComponentInChildren<Animator>();
            EnsureTelegraphLine();
        }

        private void EnsureTelegraphLine()
        {
            GameObject lineGo = new GameObject("MarcoChargeTelegraph");
            lineGo.transform.SetParent(transform, false);
            chargeTelegraphLine = lineGo.AddComponent<LineRenderer>();
            chargeTelegraphLine.positionCount = 2;
            chargeTelegraphLine.startWidth = 0.95f;
            chargeTelegraphLine.endWidth = 0.95f;
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            Color col = new Color(1f, 0.32f, 0.04f, 0.65f);
            m.color = col;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            chargeTelegraphLine.material = m;
            chargeTelegraphLine.enabled = false;
        }

        public void ApplyBriefStagger(float seconds = 1.6f)
        {
            staggerTimer = Mathf.Max(staggerTimer, seconds);
            subState = BossSubState.Pursuing;
            if (chargeTelegraphLine != null) chargeTelegraphLine.enabled = false;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            if (UI.FeralGameUI.Instance != null)
            {
                UI.FeralGameUI.Instance.ShowLootFeedback("¡Marco es INVENCIBLE! La trampa solo lo frenó un instante: ¡CORRE!", 2.8f);
            }
        }

        private void Start()
        {
            groundAligner.Initialize(transform, agent);
        }

        private void OnEnable()
        {
            TrapSystemPrototipo3.OnProto3TrapDeployed += HandlePlayerDeployedTrap;
            if (agent != null && agent.isOnNavMesh)
            {
                groundAligner.SnapImmediate(transform, agent);
            }
        }

        private void LateUpdate()
        {
            if (!escapeTriggered)
            {
                groundAligner.Tick(transform, agent);
            }
        }

        private void OnDisable()
        {
            TrapSystemPrototipo3.OnProto3TrapDeployed -= HandlePlayerDeployedTrap;
            if (BossHudPrototipo3.Instance != null) BossHudPrototipo3.Instance.HideBoss();
        }

        private void HandlePlayerDeployedTrap(TipoTrampa tipo)
        {
            if (player == null) return;
            if (Vector3.Distance(transform.position, player.transform.position) <= 8.5f)
            {
                ApplyBriefStagger(tipo == TipoTrampa.Red ? 2.1f : 1.5f);
            }
        }

        private void Update()
        {
            if (escapeTriggered) return;
            if (PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                return;
            }

            if (player == null)
            {
                player = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (player == null) return;
            }

            if (player.IsDead)
            {
                // Si atrapa a Kilo durante la huida, reposicionar un poco atrás al reaparecer sin romper la misión
                activePursuitTimer = 0f;
                return;
            }

            activePursuitTimer += Time.deltaTime;
            float dist = Vector3.Distance(transform.position, player.transform.position);

            // Detección inmediata y confiable al llegar al Refugio Seguro (como Marco aparece lejos a ~50m, llegar a casa te salva al instante!)
            bool insideRefugeNow = SafeRefugeZone.IsPointOrPlayerInRefuge(player.transform.position);
            if (insideRefugeNow)
            {
                safeRefugeStayTimer += Time.deltaTime;
            }
            else
            {
                safeRefugeStayTimer = 0f;
            }

            bool escapedByRefuge = safeRefugeStayTimer >= 0.18f;
            bool escapedByStreetDistance = activePursuitTimer >= minPursuitTimeBeforeEscape && dist >= escapeDistanceRequired;

            float refugeApproachProgress = 0f;
            if (CityGeneratorPrototipo3.Instance != null && CityGeneratorPrototipo3.Instance.SafeRefugeCenter != Vector3.zero)
            {
                float dToHome = Vector3.Distance(player.transform.position, CityGeneratorPrototipo3.Instance.SafeRefugeCenter);
                refugeApproachProgress = Mathf.Clamp01(1f - (dToHome - 4.8f) / 48f);
            }
            float distProgress = Mathf.Clamp01(dist / escapeDistanceRequired);
            float escapeProgress = insideRefugeNow ? 1f : Mathf.Max(refugeApproachProgress * 0.85f, distProgress * 0.85f);

            BossHudPrototipo3.EnsureInstance().ShowBoss(
                "MARCO EL TEJÓN (FERAL)  ·  INVENCIBLE",
                "¡NO PUEDES VENCERLO! ¡Sigue la flecha dorada hasta entrar a tu Refugio Seguro para escapar!",
                1f - escapeProgress * 0.90f,
                new Color(0.95f, 0.32f, 0.08f, 1f));

            if (escapedByRefuge || escapedByStreetDistance)
            {
                escapeTriggered = true;
                BossHudPrototipo3.EnsureInstance().HideBoss();
                OnPlayerEscapedMarco?.Invoke();
                return;
            }

            if (meleeCooldown > 0f) meleeCooldown -= Time.deltaTime;
            if (specialCooldown > 0f) specialCooldown -= Time.deltaTime;

            if (staggerTimer > 0f)
            {
                staggerTimer -= Time.deltaTime;
                UpdateAnimSpeed(0f);
                return;
            }

            switch (subState)
            {
                case BossSubState.Pursuing:
                    TickPursuit(dist);
                    break;
                case BossSubState.PostHitRecovery:
                    stateTimer -= Time.deltaTime;
                    if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                    UpdateAnimSpeed(0f);
                    if (stateTimer <= 0f) subState = BossSubState.Pursuing;
                    break;
                case BossSubState.WindupCharge:
                    TickWindupCharge();
                    break;
                case BossSubState.Charging:
                    TickCharging(dist);
                    break;
                case BossSubState.ThrowingDebris:
                    TickThrowingDebris();
                    break;
            }
        }

        private void TickPursuit(float dist)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                if (SafeRefugeZone.IsPlayerInsideSafeRefuge && SafeRefugeZone.ActiveZone != null)
                {
                    agent.isStopped = false;
                    agent.speed = basePursuitSpeed * 0.8f;
                    agent.SetDestination(SafeRefugeZone.ActiveZone.GetRepelOrigin());
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = basePursuitSpeed;
                    agent.SetDestination(player.transform.position);
                }
                UpdateAnimSpeed(agent.velocity.magnitude > 0.2f ? 1f : 0f);
            }

            if (!SafeRefugeZone.IsPlayerInsideSafeRefuge && dist <= meleeHitRange && meleeCooldown <= 0f)
            {
                meleeCooldown = 2.0f;
                TriggerAnimAttack();
                player.ReceiveDamage();
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(1f, 0.25f, 0.05f), 0.35f);
                // Pausa de rugido tras golpear para darle ventana limpia a Kilo de correr y nunca encimar su collider
                subState = BossSubState.PostHitRecovery;
                stateTimer = 1.25f;
                return;
            }

            if (!SafeRefugeZone.IsPlayerInsideSafeRefuge && specialCooldown <= 0f && dist >= 5.5f && dist <= 24f)
            {
                if (UnityEngine.Random.value < 0.55f)
                {
                    StartChargeWindup();
                }
                else
                {
                    StartDebrisThrow();
                }
            }
        }

        private void StartChargeWindup()
        {
            subState = BossSubState.WindupCharge;
            stateTimer = 0.72f;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            chargeDirection = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up);

            if (chargeTelegraphLine != null)
            {
                chargeTelegraphLine.enabled = true;
                Vector3 p0 = transform.position + Vector3.up * 0.12f;
                chargeTelegraphLine.SetPosition(0, p0);
                chargeTelegraphLine.SetPosition(1, p0 + chargeDirection * 13f);
            }
            UpdateAnimSpeed(0.2f);
        }

        private void TickWindupCharge()
        {
            stateTimer -= Time.deltaTime;
            if (chargeTelegraphLine != null)
            {
                Vector3 p0 = transform.position + Vector3.up * 0.12f;
                chargeTelegraphLine.SetPosition(0, p0);
                chargeTelegraphLine.SetPosition(1, p0 + chargeDirection * 13f);
            }
            if (stateTimer <= 0f)
            {
                if (chargeTelegraphLine != null) chargeTelegraphLine.enabled = false;
                subState = BossSubState.Charging;
                stateTimer = 0.95f;
            }
        }

        private void TickCharging(float dist)
        {
            stateTimer -= Time.deltaTime;
            Vector3 next = transform.position + chargeDirection * (bullChargeSpeed * Time.deltaTime);
            if (NavMesh.SamplePosition(next, out NavMeshHit hit, 1.8f, NavMesh.AllAreas) &&
                (!SafeRefugeZone.IsPlayerInsideSafeRefuge || SafeRefugeZone.ActiveZone == null || !SafeRefugeZone.ActiveZone.ContainsPoint(hit.position, 0.5f)))
            {
                if (agent != null && agent.isOnNavMesh) agent.Warp(hit.position);
                else transform.position = hit.position;
                UpdateAnimSpeed(1.2f);
            }
            else
            {
                stateTimer = 0f;
            }

            if (!SafeRefugeZone.IsPlayerInsideSafeRefuge && dist <= meleeHitRange + 0.35f && meleeCooldown <= 0f)
            {
                meleeCooldown = 1.6f;
                player.ReceiveDamage();
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(1f, 0.25f, 0.05f), 0.35f);
                stateTimer = 0f;
            }

            if (stateTimer <= 0f)
            {
                specialCooldown = 4.2f;
                subState = BossSubState.Pursuing;
            }
        }

        private void StartDebrisThrow()
        {
            subState = BossSubState.ThrowingDebris;
            stateTimer = 0.65f;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            TriggerAnimAttack();

            Vector3 targetBase = player.transform.position;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, i * 22f, 0f) * (targetBase - transform.position);
                Vector3 landPos = transform.position + offset;
                landPos.y = player.transform.position.y;
                SpawnDebrisChunk(transform.position + Vector3.up * 1.4f, landPos, 0.85f + Mathf.Abs(i) * 0.14f);
            }
        }

        private void SpawnDebrisChunk(Vector3 start, Vector3 end, float flightSeconds)
        {
            GameObject prefab = debrisProjectilePrefab != null ? debrisProjectilePrefab : Resources.Load<GameObject>("Weapons/Prefab_BadgerDebrisProjectile");
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, start, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.transform.position = start;
                go.transform.localScale = new Vector3(0.55f, 0.4f, 0.45f);
            }
            BossProjectilePrototipo3 bp = go.GetComponent<BossProjectilePrototipo3>();
            if (bp == null) bp = go.AddComponent<BossProjectilePrototipo3>();
            bp.InitArc(start, end, flightSeconds, 3.4f, transform);
        }

        private void TickThrowingDebris()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                specialCooldown = 4.5f;
                subState = BossSubState.Pursuing;
            }
        }

        private void UpdateAnimSpeed(float spd)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            anim.SetFloat("Speed", spd);
        }

        private void TriggerAnimAttack()
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            anim.SetTrigger("Attack");
        }
    }
}
