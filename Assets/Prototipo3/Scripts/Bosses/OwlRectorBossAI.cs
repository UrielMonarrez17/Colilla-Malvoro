using System;
using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Jefe 3 (Final de Demo): RECTOR H. MORALES — "EL SILENCIADOR" (Búho Rector).
    /// Versión balanceada y retadora sin stunlock:
    /// 1) SchoolFirstEscape: Persecución intensa en la escuela donde debes escapar tras tomar los papeles confidenciales.
    /// 2) BasketballFinalBoss: Pelea de 3 fases en el Gimnasio:
    ///    - En el aire desvía las redes con ráfagas de sus alas (¡no puedes spamearle redes volando!).
    ///    - Debes cegarlo con un Reflector UV cargado [F] o esquivar sus vuelos rasantes en cadena hasta que aterrice exhausto.
    ///    - Cada ventana de vulnerabilidad permite exactamente 1 captura de red (-20% HP, 5 capturas totales): tras recibir la red,
    ///      rompe las cuerdas con una Nova de Plumas de 360° (inmune a stunlock) y vuelve al aire más agresivo.
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

        [Header("Geometría de la Cancha")]
        public Vector3 arenaCenter = Vector3.zero;
        public Vector2 arenaHalfExtents = new Vector2(13.2f, 7.6f);
        public float cruiseAltitudeY = 3.35f;
        public float groundAltitudeY = 0.05f;

        public bool IsDefeated { get; private set; }
        public event Action OnOwlDefeated;

        private enum ArenaState
        {
            AerialGlide,
            WindupSwoop,
            SwoopDiving,
            PerchedVulnerable,
            NettedBreakingFree
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
        private float featherVolleyTimer = 1.6f;
        private float meleeHitCooldown = 0f;
        private int remainingSwoopsInCombo = 0;
        private int glidesUntilSwoopCombo = 2;

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
            swoopTelegraph.startWidth = 1.7f;
            swoopTelegraph.endWidth = 1.7f;
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            Color col = new Color(1f, 0.22f, 0.06f, 0.58f);
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

        public Vector3 GetAimCenterWorld() => transform.position + Vector3.up * 1.05f;

        public int CurrentPhaseNumber
        {
            get
            {
                float r = currentCaptureHealth / Mathf.Max(1f, maxCaptureHealth);
                return r > 0.60f ? 1 : (r > 0.25f ? 2 : 3);
            }
        }

        private void HandleTrapDeployed(TipoTrampa tipo)
        {
            if (IsDefeated || player == null) return;
            float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.transform.position.x, player.transform.position.z));

            if (mode == EncounterMode.SchoolFirstEscape)
            {
                if (distXZ <= 8.5f)
                {
                    state = ArenaState.NettedBreakingFree;
                    stateTimer = 1.8f;
                    if (UI.FeralGameUI.Instance != null)
                        UI.FeralGameUI.Instance.ShowLootFeedback("¡Frenaste un instante al Búho Rector! ¡CORRE AL AGUJERO DE SALIDA!", 2.4f);
                }
                return;
            }

            // En la batalla final, solo si usas trampa justo cuando pasa rasante en picada cerca de ti lo derribas!
            if (state == ArenaState.SwoopDiving && distXZ <= 6.5f)
            {
                ForceDazzleGrounding(2.6f, "¡Tu trampa interceptó su vuelo rasante! ¡DISPÁRALE UNA RED AHORA [CLICK IZQ / E]!");
            }
        }

        public void ForceDazzleGrounding(float vulnerableDuration, string feedbackMsg = null)
        {
            if (IsDefeated || state == ArenaState.NettedBreakingFree) return;
            if (swoopTelegraph != null) swoopTelegraph.enabled = false;
            remainingSwoopsInCombo = 0;
            state = ArenaState.PerchedVulnerable;
            stateTimer = vulnerableDuration;
            transform.position = new Vector3(transform.position.x, groundAltitudeY, transform.position.z);
            SetAnimFlags(flying: false, swooping: false, stunned: false);
            if (!string.IsNullOrEmpty(feedbackMsg) && UI.FeralGameUI.Instance != null)
            {
                UI.FeralGameUI.Instance.ShowLootFeedback(feedbackMsg, 3.0f);
            }
        }

        public void OnHitByNetLauncher(Vector3 hitPoint)
        {
            if (IsDefeated) return;

            if (mode == EncounterMode.SchoolFirstEscape)
            {
                state = ArenaState.NettedBreakingFree;
                stateTimer = 2.0f;
                return;
            }

            // ANTI-STUNLOCK 1: Si ya está envuelto rompiendo la red, es inmune hasta que despegue!
            if (state == ArenaState.NettedBreakingFree)
            {
                if (UI.FeralGameUI.Instance != null)
                {
                    UI.FeralGameUI.Instance.ShowLootFeedback("¡Está desgarrando la red con sus garras! ¡Aléjate de su onda expansiva!", 1.8f);
                }
                return;
            }

            // ANTI-STUNLOCK 2: Si está volando alto, sus alas desvían la red! Debes derribarlo con Reflector UV [F] o esperar a que aterrice tras su picada!
            if (state == ArenaState.AerialGlide || state == ArenaState.WindupSwoop || state == ArenaState.SwoopDiving)
            {
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(1f, 0.65f, 0.2f), 0.14f);
                // Contraataca con una ráfaga rápida de 3 plumas al desviar la red
                FireFeatherFan(3, 24f, 16.5f);
                if (UI.FeralGameUI.Instance != null)
                {
                    UI.FeralGameUI.Instance.ShowLootFeedback("¡DESVIÓ LA RED CON SUS ALAS! Activa un Reflector UV [F] o esquiva sus picadas hasta que aterrice.", 2.8f);
                }
                return;
            }

            // VENTANA VULNERABLE EN EL SUELO (PerchedVulnerable): ¡Captura exitosa (-20% HP -> 5 capturas en total)!
            currentCaptureHealth = Mathf.Max(0f, currentCaptureHealth - 20f);
            BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.2f, 0.92f, 1f), 0.34f);

            if (currentCaptureHealth <= 0.01f)
            {
                CompleteDefeat();
                return;
            }

            // Entra a NettedBreakingFree (1.15s) donde sacude la red y al terminar lanza una Nova de 360° de plumas y despega!
            state = ArenaState.NettedBreakingFree;
            stateTimer = 1.15f;
            EnsureNetWrapVfx(true);
            SetAnimFlags(flying: false, swooping: false, stunned: true);

            int capturesLeft = Mathf.CeilToInt(currentCaptureHealth / 20f);
            if (UI.FeralGameUI.Instance != null)
            {
                UI.FeralGameUI.Instance.ShowLootFeedback($"¡CAPTURA EXITOSA! (Faltan {capturesLeft} redes) · ¡CUIDADO, VA A ROMPER LA RED!", 2.6f);
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
                "¡TIENES LOS PAPELES! ¡Corre de regreso por los pasillos hasta el agujero del muro para escapar!",
                1f,
                new Color(0.88f, 0.20f, 0.22f, 1f));

            if (state == ArenaState.NettedBreakingFree)
            {
                stateTimer -= Time.deltaTime;
                SetAnimFlags(false, false, true);
                if (stateTimer <= 0f) state = ArenaState.AerialGlide;
                return;
            }

            SetAnimFlags(true, false, false);
            Vector3 desired = new Vector3(player.transform.position.x, 1.25f, player.transform.position.z);
            Vector3 toPlayer = desired - transform.position;
            Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (flat.sqrMagnitude > 0.02f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized, Vector3.up), Time.deltaTime * 7f);
            }

            transform.position = Vector3.MoveTowards(transform.position, desired, 6.4f * Time.deltaTime);

            float distXZ = flat.magnitude;
            if (distXZ <= 1.65f && meleeHitCooldown <= 0f)
            {
                meleeHitCooldown = 1.65f;
                player.ReceiveDamage();
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.95f, 0.2f, 0.1f), 0.30f);
            }

            featherVolleyTimer -= Time.deltaTime;
            if (featherVolleyTimer <= 0f && distXZ >= 3.8f)
            {
                featherVolleyTimer = 2.2f;
                FireFeatherFan(3, 24f, 13.5f);
            }
        }

        private void TickBasketballBossArena()
        {
            float hpRatio = Mathf.Clamp01(currentCaptureHealth / Mathf.Max(1f, maxCaptureHealth));
            int phase = CurrentPhaseNumber;

            string hint;
            if (state == ArenaState.PerchedVulnerable)
                hint = $"¡VULNERABLE EN LA DUELA ({stateTimer:F1}s)! ¡DISPÁRALE CON EL LANZA REDES [CLICK IZQ / E]!";
            else if (state == ArenaState.NettedBreakingFree)
                hint = "¡ESTÁ ROMPIENDO LA RED! ¡Aléjate con Dash [Q] de su explosión de plumas!";
            else
                hint = $"Fase {phase}/3 · En vuelo desvía redes: cégalo con un Reflector UV [F] o esquiva sus {phase} picadas hasta que aterrice";

            BossHudPrototipo3.EnsureInstance().ShowBoss(
                $"RECTOR H. MORALES  ·  FASE {phase}/3",
                hint,
                hpRatio,
                state == ArenaState.PerchedVulnerable ? new Color(0.2f, 0.92f, 1f, 1f) : new Color(0.88f, 0.22f, 0.18f, 1f));

            switch (state)
            {
                case ArenaState.AerialGlide:
                    EnsureNetWrapVfx(false);
                    SetAnimFlags(flying: true, swooping: false, stunned: false);
                    TickAerialGlide(phase);
                    break;

                case ArenaState.WindupSwoop:
                    TickWindupSwoop();
                    break;

                case ArenaState.SwoopDiving:
                    SetAnimFlags(flying: false, swooping: true, stunned: false);
                    TickSwoopDiving(phase);
                    break;

                case ArenaState.PerchedVulnerable:
                    SetAnimFlags(flying: false, swooping: false, stunned: false);
                    stateTimer -= Time.deltaTime;
                    FacePlayerFlat();
                    if (stateTimer <= 0f)
                    {
                        glidesUntilSwoopCombo = 2;
                        PickNextAerialPoint();
                        state = ArenaState.AerialGlide;
                    }
                    break;

                case ArenaState.NettedBreakingFree:
                    SetAnimFlags(flying: false, swooping: false, stunned: true);
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        EnsureNetWrapVfx(false);
                        UnleashBreakFreeNovaAndTakeOff(phase);
                    }
                    break;
            }
        }

        private void UnleashBreakFreeNovaAndTakeOff(int phase)
        {
            // Al liberarse de una red, lanza un anillo de plumas 360° y empuja/ralentiza si Kilo se quedó pegado a él
            int ringCount = 8 + phase * 2;
            Vector3 origin = transform.position + Vector3.up * 0.65f;
            for (int i = 0; i < ringCount; i++)
            {
                float yaw = (360f / ringCount) * i;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                SpawnSingleFeather(origin + dir * 0.8f, dir, 13f + phase * 1.5f);
            }

            if (player != null && Vector3.Distance(player.transform.position, transform.position) <= 4.2f)
            {
                if (player is HamsterControllerPrototipo3 hcp) hcp.ApplyExternalSlow(0.55f, 1.5f);
            }

            BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(1f, 0.35f, 0.08f), 0.26f);
            glidesUntilSwoopCombo = 2;
            PickNextAerialPoint();
            transform.position = new Vector3(transform.position.x, cruiseAltitudeY * 0.7f, transform.position.z);
            state = ArenaState.AerialGlide;
        }

        private void TickAerialGlide(int phase)
        {
            float flySpeed = 9.0f + phase * 2.2f;
            transform.position = Vector3.MoveTowards(transform.position, targetHoverPoint, flySpeed * Time.deltaTime);
            FacePlayerFlat();

            featherVolleyTimer -= Time.deltaTime;
            if (featherVolleyTimer <= 0f)
            {
                featherVolleyTimer = Mathf.Max(0.95f, 1.95f - phase * 0.32f);
                int count = phase == 1 ? 5 : (phase == 2 ? 7 : 9);
                FireFeatherFan(count, 42f + phase * 8f, 15.0f + phase * 2.2f);
                if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger("Attack");
            }

            stateTimer -= Time.deltaTime;
            if (Vector3.Distance(transform.position, targetHoverPoint) < 0.65f || stateTimer <= 0f)
            {
                glidesUntilSwoopCombo--;
                if (glidesUntilSwoopCombo <= 0)
                {
                    // Fase 1: 1 picada | Fase 2: 2 picadas seguidas | Fase 3: 3 picadas seguidas antes de aterrizar!
                    remainingSwoopsInCombo = phase;
                    StartNextSwoopInCombo(phase);
                }
                else
                {
                    PickNextAerialPoint();
                }
            }
        }

        private void PickNextAerialPoint()
        {
            float rx = UnityEngine.Random.Range(-arenaHalfExtents.x * 0.88f, arenaHalfExtents.x * 0.88f);
            float rz = UnityEngine.Random.Range(-arenaHalfExtents.y * 0.84f, arenaHalfExtents.y * 0.84f);
            targetHoverPoint = arenaCenter + new Vector3(rx, cruiseAltitudeY, rz);
            stateTimer = 2.4f;
        }

        private void StartNextSwoopInCombo(int phase)
        {
            state = ArenaState.WindupSwoop;
            stateTimer = Mathf.Max(0.46f, 0.78f - phase * 0.10f);
            swoopStart = new Vector3(transform.position.x, groundAltitudeY + 0.55f, transform.position.z);

            Vector3 predictedTarget = player.transform.position;
            Rigidbody prb = player.GetComponent<Rigidbody>();
            if (prb != null) predictedTarget += new Vector3(prb.linearVelocity.x, 0f, prb.linearVelocity.z) * 0.35f;

            Vector3 dir = predictedTarget - swoopStart;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f) dir = transform.forward;
            dir.Normalize();

            swoopEnd = swoopStart + dir * 18.5f;
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
            transform.position = Vector3.Lerp(transform.position, new Vector3(swoopStart.x, groundAltitudeY + 1.15f, swoopStart.z), Time.deltaTime * 7f);
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                if (swoopTelegraph != null) swoopTelegraph.enabled = false;
                state = ArenaState.SwoopDiving;
                stateTimer = 0.95f;
            }
        }

        private void TickSwoopDiving(int phase)
        {
            Vector3 toEnd = swoopEnd - transform.position;
            Vector3 flat = new Vector3(toEnd.x, 0f, toEnd.z);
            if (flat.sqrMagnitude > 0.02f) transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

            float diveSpeed = 19.5f + phase * 2.5f;
            transform.position = Vector3.MoveTowards(transform.position, swoopEnd, diveSpeed * Time.deltaTime);

            float distToPlayer = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(player.transform.position.x, player.transform.position.z));

            if (distToPlayer <= 1.70f && meleeHitCooldown <= 0f && !player.IsDashing)
            {
                meleeHitCooldown = 1.35f;
                player.ReceiveDamage();
                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.95f, 0.2f, 0.1f), 0.30f);
            }

            stateTimer -= Time.deltaTime;
            if (Vector3.Distance(transform.position, swoopEnd) <= 0.35f || stateTimer <= 0f)
            {
                remainingSwoopsInCombo--;
                if (remainingSwoopsInCombo > 0)
                {
                    // Encadena inmediatamente la siguiente picada en Fase 2 y Fase 3!
                    StartNextSwoopInCombo(phase);
                }
                else
                {
                    // Tras completar todo el combo de picadas, aterriza brevemente jadeando (ventana justa de habilidad!)
                    transform.position = new Vector3(transform.position.x, groundAltitudeY, transform.position.z);
                    state = ArenaState.PerchedVulnerable;
                    stateTimer = Mathf.Max(1.85f, 2.55f - phase * 0.22f);
                }
            }
        }

        private void FireFeatherFan(int count, float totalSpreadDegrees, float projSpeed)
        {
            Vector3 origin = transform.position + Vector3.up * 0.85f;
            Vector3 targetPos = player.transform.position + Vector3.up * 0.32f;
            Rigidbody prb = player.GetComponent<Rigidbody>();
            if (prb != null) targetPos += new Vector3(prb.linearVelocity.x, 0f, prb.linearVelocity.z) * 0.28f;

            Vector3 toTarget = targetPos - origin;
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
            bp.slowDuration = 1.1f;
            bp.slowMultiplier = 0.68f;
            bp.hitRadius = 0.68f;
            bp.InitLinear(BossProjectileKind.OwlRazorFeather, dir, spd, transform);
        }

        private void FacePlayerFlat()
        {
            if (player == null) return;
            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.02f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), Time.deltaTime * 8f);
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
