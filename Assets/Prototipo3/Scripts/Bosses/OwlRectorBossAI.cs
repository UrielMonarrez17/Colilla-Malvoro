using System;
using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Jefe 3 (Final de Demo): RECTOR H. MORALES — "EL SILENCIADOR" (Búho Rector).
    /// Soporta los dos escenarios de la escuela:
    /// 1) SchoolFirstEscape: Encuentro inicial en los pasillos de la escuela (invencible, debes huir hasta el agujero).
    /// 2) BasketballFinalBoss: Pelea final multi-fase en la Cancha de Básquetbol, derrotado con la Pistola Lanza Redes.
    /// </summary>
    public class OwlRectorBossAI : MonoBehaviour
    {
        public enum EncounterMode
        {
            SchoolFirstEscape,
            BasketballFinalBoss
        }

        [Header("Modo de Encuentro")]
        public EncounterMode mode = EncounterMode.BasketballFinalBoss;
        public float maxCaptureHealth = 100f;
        public float currentCaptureHealth = 100f;
        public GameObject featherProjectilePrefab;

        [Header("Geometría de la Cancha / Pasillo")]
        public Vector3 arenaCenter = Vector3.zero;
        public Vector2 arenaHalfExtents = new Vector2(13.5f, 8.0f);
        public float cruiseAltitudeY = 3.2f;
        public float groundAltitudeY = 0.05f;

        public bool IsDefeated { get; private set; }
        public event Action OnOwlDefeated;

        private enum ArenaState
        {
            AerialGlide,
            WindupSwoop,
            SwoopDiving,
            PerchedRecovering,
            NettedStunned
        }

        private ArenaState state = ArenaState.AerialGlide;
        private HamsterController player;
        private Animator anim;
        private LineRenderer swoopTelegraph;
        private GameObject activeNetWrapVfx;

        private Vector3 targetHoverPoint;
        private Vector3 swoopStart;
        private Vector3 swoopEnd;
        private float stateTimer = 0f;
        private float featherVolleyTimer = 2.0f;
        private float meleeHitCooldown = 0f;
        private int aerialNetHitsAccumulated = 0;

        private void Awake()
        {
            gameObject.tag = "Enemy";
            currentCaptureHealth = maxCaptureHealth;
            anim = GetComponentInChildren<Animator>();
            EnsureSwoopTelegraph();
        }

        private void EnsureSwoopTelegraph()
        {
            GameObject go = new GameObject("OwlSwoopTelegraph");
            go.transform.SetParent(transform, false);
            swoopTelegraph = go.AddComponent<LineRenderer>();
            swoopTelegraph.positionCount = 2;
            swoopTelegraph.startWidth = 1.55f;
            swoopTelegraph.endWidth = 1.55f;
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            Color col = new Color(1f, 0.25f, 0.08f, 0.52f);
            m.color = col;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            swoopTelegraph.material = m;
            swoopTelegraph.enabled = false;
        }

        private void OnEnable()
        {
            TrapSystemPrototipo3.OnProto3TrapDeployed += HandleTrapDeployed;
            PickNextAerialPoint();
        }

        private void OnDisable()
        {
            TrapSystemPrototipo3.OnProto3TrapDeployed -= HandleTrapDeployed;
            if (BossHudPrototipo3.Instance != null) BossHudPrototipo3.Instance.HideBoss();
        }

        public Vector3 GetAimCenterWorld() => transform.position + Vector3.up * 1.1f;

        private void HandleTrapDeployed(TipoTrampa tipo)
        {
            if (IsDefeated || player == null) return;
            float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.transform.position.x, player.transform.position.z));
            if (distXZ <= 9.5f)
            {
                if (mode == EncounterMode.SchoolFirstEscape)
                {
                    stateTimer = 2.2f;
                    state = ArenaState.NettedStunned;
                    if (UI.FeralGameUI.Instance != null)
                        UI.FeralGameUI.Instance.ShowLootFeedback("¡Aturdiste un instante al Búho Rector! ¡Huye hacia el agujero!", 2.5f);
                }
                else
                {
                    ForceDazzleGrounding(3.8f, "¡La trampa derribó al Búho Rector! ¡DISPÁRALE CON EL LANZA REDES [CLICK IZQ / E]!");
                }
            }
        }

        public void ForceDazzleGrounding(float stunDuration, string feedbackMsg = null)
        {
            if (IsDefeated) return;
            if (swoopTelegraph != null) swoopTelegraph.enabled = false;
            state = ArenaState.PerchedRecovering;
            stateTimer = Mathf.Max(stateTimer, stunDuration);
            transform.position = new Vector3(transform.position.x, groundAltitudeY, transform.position.z);
            SetAnimFlags(flying: false, swooping: false, stunned: true);
            if (!string.IsNullOrEmpty(feedbackMsg) && UI.FeralGameUI.Instance != null)
            {
                UI.FeralGameUI.Instance.ShowLootFeedback(feedbackMsg, 3.2f);
            }
        }

        public void OnHitByNetLauncher(Vector3 hitPoint)
        {
            if (IsDefeated) return;

            if (mode == EncounterMode.SchoolFirstEscape)
            {
                state = ArenaState.NettedStunned;
                stateTimer = 2.8f;
                return;
            }

            bool vulnerableOnGround = state == ArenaState.PerchedRecovering || state == ArenaState.NettedStunned || state == ArenaState.SwoopDiving || transform.position.y <= groundAltitudeY + 1.4f;

            if (vulnerableOnGround)
            {
                // Impacto crítico directo: -25% de barra de captura y queda envuelto en red en el piso
                currentCaptureHealth = Mathf.Max(0f, currentCaptureHealth - 25f);
                state = ArenaState.NettedStunned;
                stateTimer = 3.2f;
                if (swoopTelegraph != null) swoopTelegraph.enabled = false;
                transform.position = new Vector3(transform.position.x, groundAltitudeY, transform.position.z);
                EnsureNetWrapVfx(true);
                SetAnimFlags(flying: false, swooping: false, stunned: true);
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.2f, 0.9f, 1f), 0.30f);

                if (UI.FeralGameUI.Instance != null)
                {
                    UI.FeralGameUI.Instance.ShowLootFeedback("¡IMPACTO DIRECTO DE RED! ¡Sigue disparando para inmovilizarlo por completo!", 2.4f);
                }
            }
            else
            {
                // Impacto en pleno vuelo: daña -12.5% y al segundo impacto aéreo le enreda las alas y lo estrella contra la cancha!
                currentCaptureHealth = Mathf.Max(0f, currentCaptureHealth - 12.5f);
                aerialNetHitsAccumulated++;
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.2f, 0.9f, 1f), 0.18f);

                if (aerialNetHitsAccumulated >= 2)
                {
                    aerialNetHitsAccumulated = 0;
                    EnsureNetWrapVfx(true);
                    ForceDazzleGrounding(4.2f, "¡Sus alas quedaron enredadas! ¡Cayó a la duela, remátalo con el Lanza Redes!");
                }
                else if (UI.FeralGameUI.Instance != null)
                {
                    UI.FeralGameUI.Instance.ShowLootFeedback("¡Red enganchada en el aire! ¡Un disparo más o usa un reflector [F] para derribarlo!", 2.4f);
                }
            }

            if (currentCaptureHealth <= 0.01f)
            {
                CompleteDefeat();
            }
        }

        private void CompleteDefeat()
        {
            IsDefeated = true;
            currentCaptureHealth = 0f;
            if (swoopTelegraph != null) swoopTelegraph.enabled = false;
            transform.position = new Vector3(transform.position.x, groundAltitudeY, transform.position.z);
            EnsureNetWrapVfx(true);
            SetAnimFlags(flying: false, swooping: false, stunned: true);
            BossHudPrototipo3.EnsureInstance().HideBoss();
            OnOwlDefeated?.Invoke();
        }

        private void EnsureNetWrapVfx(bool active)
        {
            if (!active)
            {
                if (activeNetWrapVfx != null) activeNetWrapVfx.SetActive(false);
                return;
            }
            if (activeNetWrapVfx == null)
            {
                GameObject netPrefab = Resources.Load<GameObject>("Weapons/Prefab_NetProjectile");
                if (netPrefab != null)
                {
                    activeNetWrapVfx = Instantiate(netPrefab, transform, false);
                    FiredCaptureNetProjectile comp = activeNetWrapVfx.GetComponent<FiredCaptureNetProjectile>();
                    if (comp != null) Destroy(comp);
                }
                else
                {
                    activeNetWrapVfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(activeNetWrapVfx.GetComponent<Collider>());
                    activeNetWrapVfx.transform.SetParent(transform, false);
                }
                activeNetWrapVfx.name = "TangledNetWrapVFX";
                activeNetWrapVfx.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                activeNetWrapVfx.transform.localScale = new Vector3(1.55f, 1.15f, 1.55f);
            }
            activeNetWrapVfx.SetActive(true);
        }

        private void Update()
        {
            if (IsDefeated) return;
            if (PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay) return;

            if (player == null)
            {
                player = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (player == null) return;
            }

            if (player.IsDead) return;
            if (meleeHitCooldown > 0f) meleeHitCooldown -= Time.deltaTime;

            if (mode == EncounterMode.SchoolFirstEscape)
            {
                TickFirstSchoolEscape();
            }
            else
            {
                TickBasketballBossArena();
            }
        }

        private void TickFirstSchoolEscape()
        {
            BossHudPrototipo3.EnsureInstance().ShowBoss(
                "RECTOR H. MORALES  ·  EL SILENCIADOR",
                "¡Demasiado alto y veloz sin arma antiaérea! ¡Corre de vuelta al agujero de entrada para escapar!",
                1f,
                new Color(0.86f, 0.20f, 0.26f, 1f));

            if (state == ArenaState.NettedStunned)
            {
                stateTimer -= Time.deltaTime;
                SetAnimFlags(false, false, true);
                if (stateTimer <= 0f) state = ArenaState.AerialGlide;
                return;
            }

            SetAnimFlags(true, false, false);
            Vector3 desired = new Vector3(player.transform.position.x, 1.35f, player.transform.position.z);
            Vector3 toPlayer = desired - transform.position;
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (flat.sqrMagnitude > 0.02f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized, Vector3.up), Time.deltaTime * 6f);
            }

            // Persecución tensa pero justa para permitir la huida por los casilleros
            transform.position = Vector3.MoveTowards(transform.position, desired, 5.8f * Time.deltaTime);

            float distXZ = flat.magnitude;
            if (distXZ <= 1.7f && meleeHitCooldown <= 0f)
            {
                meleeHitCooldown = 1.8f;
                player.ReceiveDamage();
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.95f, 0.2f, 0.1f), 0.30f);
            }

            featherVolleyTimer -= Time.deltaTime;
            if (featherVolleyTimer <= 0f && distXZ >= 3.5f)
            {
                featherVolleyTimer = 2.6f;
                FireFeatherFan(3, 22f, 12.5f);
            }
        }

        private void TickBasketballBossArena()
        {
            float hpRatio = Mathf.Clamp01(currentCaptureHealth / Mathf.Max(1f, maxCaptureHealth));
            int phaseNum = hpRatio > 0.66f ? 1 : (hpRatio > 0.33f ? 2 : 3);

            string hint = (state == ArenaState.PerchedRecovering || state == ArenaState.NettedStunned)
                ? "¡ESTÁ EN EL SUELO! ¡Dispárale con la Pistola Lanza Redes [CLICK IZQ / E]!"
                : $"Fase {phaseNum}/3 · Esquiva sus plumas, activa reflectores [F] o dispárale con el Lanza Redes [CLICK IZQ / E]";

            BossHudPrototipo3.EnsureInstance().ShowBoss(
                "RECTOR H. MORALES  ·  EL SILENCIADOR",
                hint,
                hpRatio,
                state == ArenaState.NettedStunned ? new Color(0.2f, 0.88f, 1f, 1f) : new Color(0.88f, 0.22f, 0.18f, 1f));

            switch (state)
            {
                case ArenaState.AerialGlide:
                    EnsureNetWrapVfx(false);
                    SetAnimFlags(flying: true, swooping: false, stunned: false);
                    TickAerialGlide(phaseNum);
                    break;

                case ArenaState.WindupSwoop:
                    TickWindupSwoop();
                    break;

                case ArenaState.SwoopDiving:
                    SetAnimFlags(flying: false, swooping: true, stunned: false);
                    TickSwoopDiving();
                    break;

                case ArenaState.PerchedRecovering:
                    SetAnimFlags(flying: false, swooping: false, stunned: false);
                    stateTimer -= Time.deltaTime;
                    FacePlayerFlat();
                    if (stateTimer <= 0f)
                    {
                        PickNextAerialPoint();
                        state = ArenaState.AerialGlide;
                    }
                    break;

                case ArenaState.NettedStunned:
                    SetAnimFlags(flying: false, swooping: false, stunned: true);
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        EnsureNetWrapVfx(false);
                        PickNextAerialPoint();
                        state = ArenaState.AerialGlide;
                    }
                    break;
            }
        }

        private void TickAerialGlide(int phaseNum)
        {
            float flySpeed = 7.5f + phaseNum * 1.4f;
            transform.position = Vector3.MoveTowards(transform.position, targetHoverPoint, flySpeed * Time.deltaTime);
            FacePlayerFlat();

            featherVolleyTimer -= Time.deltaTime;
            if (featherVolleyTimer <= 0f)
            {
                featherVolleyTimer = Mathf.Max(1.35f, 2.5f - phaseNum * 0.35f);
                int count = phaseNum == 1 ? 3 : (phaseNum == 2 ? 5 : 7);
                FireFeatherFan(count, 36f + phaseNum * 6f, 13.5f + phaseNum * 1.5f);
                if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger("Attack");
            }

            stateTimer -= Time.deltaTime;
            if (Vector3.Distance(transform.position, targetHoverPoint) < 0.6f || stateTimer <= 0f)
            {
                if (UnityEngine.Random.value < 0.52f)
                {
                    StartSwoopDive();
                }
                else
                {
                    PickNextAerialPoint();
                }
            }
        }

        private void PickNextAerialPoint()
        {
            float rx = UnityEngine.Random.Range(-arenaHalfExtents.x * 0.85f, arenaHalfExtents.x * 0.85f);
            float rz = UnityEngine.Random.Range(-arenaHalfExtents.y * 0.80f, arenaHalfExtents.y * 0.80f);
            targetHoverPoint = arenaCenter + new Vector3(rx, cruiseAltitudeY, rz);
            stateTimer = 3.2f;
        }

        private void StartSwoopDive()
        {
            state = ArenaState.WindupSwoop;
            stateTimer = 0.85f;
            swoopStart = new Vector3(transform.position.x, groundAltitudeY + 0.55f, transform.position.z);

            Vector3 dir = (player.transform.position - swoopStart);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f) dir = transform.forward;
            dir.Normalize();

            swoopEnd = swoopStart + dir * 16f;
            swoopEnd.x = Mathf.Clamp(swoopEnd.x, arenaCenter.x - arenaHalfExtents.x, arenaCenter.x + arenaHalfExtents.x);
            swoopEnd.z = Mathf.Clamp(swoopEnd.z, arenaCenter.z - arenaHalfExtents.y, arenaCenter.z + arenaHalfExtents.y);
            swoopEnd.y = groundAltitudeY;

            if (swoopTelegraph != null)
            {
                swoopTelegraph.enabled = true;
                swoopTelegraph.SetPosition(0, new Vector3(swoopStart.x, groundAltitudeY + 0.08f, swoopStart.z));
                swoopTelegraph.SetPosition(1, new Vector3(swoopEnd.x, groundAltitudeY + 0.08f, swoopEnd.z));
            }
        }

        private void TickWindupSwoop()
        {
            FacePlayerFlat();
            transform.position = Vector3.Lerp(transform.position, new Vector3(swoopStart.x, groundAltitudeY + 1.15f, swoopStart.z), Time.deltaTime * 5f);
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                if (swoopTelegraph != null) swoopTelegraph.enabled = false;
                state = ArenaState.SwoopDiving;
                stateTimer = 1.15f;
            }
        }

        private void TickSwoopDiving()
        {
            Vector3 toEnd = swoopEnd - transform.position;
            Vector3 flat = new Vector3(toEnd.x, 0f, toEnd.z);
            if (flat.sqrMagnitude > 0.02f) transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

            transform.position = Vector3.MoveTowards(transform.position, swoopEnd, 18.5f * Time.deltaTime);

            float distToPlayer = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(player.transform.position.x, player.transform.position.z));

            if (distToPlayer <= 1.65f && meleeHitCooldown <= 0f && !player.IsDashing)
            {
                meleeHitCooldown = 1.6f;
                player.ReceiveDamage();
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.95f, 0.2f, 0.1f), 0.30f);
            }

            stateTimer -= Time.deltaTime;
            if (Vector3.Distance(transform.position, swoopEnd) <= 0.35f || stateTimer <= 0f)
            {
                // Tras el vuelo rasante, el búho aterriza exhausto 3.5 segundos en la duela: ventana clara para dispararle con el Lanza Redes
                transform.position = new Vector3(transform.position.x, groundAltitudeY, transform.position.z);
                state = ArenaState.PerchedRecovering;
                stateTimer = 3.5f;
            }
        }

        private void FireFeatherFan(int count, float totalSpreadDegrees, float projSpeed)
        {
            Vector3 origin = transform.position + Vector3.up * 0.85f;
            Vector3 toTarget = (player.transform.position + Vector3.up * 0.35f) - origin;
            if (toTarget.sqrMagnitude < 0.05f) toTarget = transform.forward;
            Vector3 baseDir = toTarget.normalized;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1)) - 0.5f;
                float yaw = t * totalSpreadDegrees;
                Vector3 dir = (Quaternion.Euler(0f, yaw, 0f) * baseDir).normalized;
                SpawnSingleFeather(origin + dir * 0.6f, dir, projSpeed);
            }
        }

        private void SpawnSingleFeather(Vector3 pos, Vector3 dir, float spd)
        {
            GameObject prefab = featherProjectilePrefab != null ? featherProjectilePrefab : Resources.Load<GameObject>("Weapons/Prefab_OwlFeatherProjectile");
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, Quaternion.LookRotation(dir, Vector3.up));
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(go.GetComponent<Collider>());
                go.transform.position = pos;
                go.transform.localScale = new Vector3(0.16f, 0.05f, 0.65f);
            }
            BossProjectilePrototipo3 bp = go.GetComponent<BossProjectilePrototipo3>();
            if (bp == null) bp = go.AddComponent<BossProjectilePrototipo3>();
            bp.dealsDirectHitDamage = true;
            bp.slowDuration = 1.0f;
            bp.slowMultiplier = 0.7f;
            bp.hitRadius = 0.65f;
            bp.InitLinear(BossProjectileKind.OwlRazorFeather, dir, spd, transform);
        }

        private void FacePlayerFlat()
        {
            if (player == null) return;
            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.02f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), Time.deltaTime * 7f);
            }
        }

        private void SetAnimFlags(bool flying, bool swooping, bool stunned)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            anim.SetFloat("Speed", flying || swooping ? 1f : 0f);
            anim.SetBool("IsFlying", flying);
            anim.SetBool("IsSwooping", swooping);
            anim.SetBool("IsStunned", stunned);
        }
    }
}
