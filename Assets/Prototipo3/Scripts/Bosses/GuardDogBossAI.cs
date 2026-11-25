using UnityEngine;
using UnityEngine.AI;

namespace Prototipo3
{
    /// <summary>
    /// Jefe 2 (Errante de Mapa): EL PERRO GUARDIÁN K-9.
    /// - Patrulla las avenidas cercanas al Taller del Mapache y la entrada de la Escuela.
    /// - Se comporta como un jefe errante con olfato agudo (anulable con Polen/Red), Ladrido Sónico ralentizador
    ///   (SM_SonicBarkWave) y Salto de Presa (Lunge).
    /// </summary>
    [RequireComponent(typeof(EnemyAIPrototipo3))]
    public class GuardDogBossAI : MonoBehaviour
    {
        [Header("Mecánicas del Perro Guardián")]
        public GameObject sonicBarkPrefab;
        public float sonicBarkCooldown = 5.2f;
        public float sonicBarkRange = 15.5f;
        public float lungeCooldown = 6.5f;
        public float lungeSpeed = 13.5f;
        public float bossHudShowRadius = 22f;

        private EnemyAIPrototipo3 baseAi;
        private NavMeshAgent agent;
        private Animator anim;
        private HamsterController player;

        private float barkTimer = 2.5f;
        private float lungeTimer = 4.0f;
        private float activeLungeRemaining = 0f;
        private Vector3 lungeDir;
        private bool showingHud = false;

        private void Awake()
        {
            baseAi = GetComponent<EnemyAIPrototipo3>();
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponentInChildren<Animator>();

            if (baseAi != null)
            {
                baseAi.wanderSpeed = 4.3f;
                baseAi.chaseSpeed = 7.6f;
                baseAi.visionRadiusNormal = 18.5f;
                baseAi.visionRadiusCrouched = 7.5f;
                baseAi.baseSmellRadius = 6.8f;
                baseAi.attackRange = 2.0f;
                baseAi.showVisionCone = true;
            }
        }

        private void OnDisable()
        {
            if (showingHud && BossHudPrototipo3.Instance != null)
            {
                BossHudPrototipo3.Instance.HideBoss();
                showingHud = false;
            }
        }

        private void Update()
        {
            if (PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay) return;
            if (baseAi == null || !baseAi.isActiveAndEnabled) return;

            if (player == null)
            {
                player = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (player == null) return;
            }

            bool feralActive = baseAi.IsFeralActiveNow();
            float dist = Vector3.Distance(transform.position, player.transform.position);

            // Mostrar barra de Jefe Errante únicamente si no hay pelea principal con Marco/Búho y el perro está acechando cerca de noche
            bool chasingNearby = feralActive && !SafeRefugeZone.IsPlayerInsideSafeRefuge &&
                                 (baseAi.CurrentState == EnemyAI.EnemyState.Chase || dist <= bossHudShowRadius) &&
                                 FindAnyObjectByType<MarcoFeralBossAI>() == null &&
                                 FindAnyObjectByType<OwlRectorBossAI>() == null;

            if (chasingNearby && baseAi.CurrentState == EnemyAI.EnemyState.Chase)
            {
                showingHud = true;
                BossHudPrototipo3.EnsureInstance().ShowBoss(
                    "PERRO GUARDIÁN K-9  ·  JEFE ERRANTE",
                    "Olfato agudo y Ladrido Sónico  ·  Usa Polen/Red [2] o Barricada [3] para cortarle el paso",
                    1f,
                    new Color(0.95f, 0.52f, 0.12f, 1f));
            }
            else if (showingHud)
            {
                showingHud = false;
                BossHudPrototipo3.EnsureInstance().HideBoss();
            }

            if (!feralActive || player.IsDead || SafeRefugeZone.IsPlayerInsideSafeRefuge) return;
            if (baseAi.IsNetImmobilized || baseAi.IsParalyzedByStatic || baseAi.IsDistractedByDecoy) return;

            if (barkTimer > 0f) barkTimer -= Time.deltaTime;
            if (lungeTimer > 0f) lungeTimer -= Time.deltaTime;

            if (activeLungeRemaining > 0f)
            {
                activeLungeRemaining -= Time.deltaTime;
                Vector3 next = transform.position + lungeDir * (lungeSpeed * Time.deltaTime);
                if (NavMesh.SamplePosition(next, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    if (agent != null && agent.isOnNavMesh) agent.Warp(hit.position);
                }
                return;
            }

            if (baseAi.CurrentState != EnemyAI.EnemyState.Chase) return;

            // 1. Ladrido Sónico a media distancia
            if (barkTimer <= 0f && dist >= 4.2f && dist <= sonicBarkRange)
            {
                barkTimer = sonicBarkCooldown;
                FireSonicBarkWave();
                return;
            }

            // 2. Salto de Presa (Lunge)
            if (lungeTimer <= 0f && dist >= 3.8f && dist <= 9.2f)
            {
                lungeTimer = lungeCooldown;
                Vector3 dir = (player.transform.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.05f)
                {
                    lungeDir = dir.normalized;
                    transform.rotation = Quaternion.LookRotation(lungeDir, Vector3.up);
                    activeLungeRemaining = 0.36f;
                    if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger("Attack");
                }
            }
        }

        private void FireSonicBarkWave()
        {
            Vector3 dir = player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger("Attack");

            Vector3 spawnPos = transform.position + Vector3.up * 0.65f + dir * 0.9f;
            GameObject prefab = sonicBarkPrefab != null ? sonicBarkPrefab : Resources.Load<GameObject>("Weapons/Prefab_SonicBarkWave");
            GameObject wave;
            if (prefab != null)
            {
                wave = Instantiate(prefab, spawnPos, Quaternion.LookRotation(dir, Vector3.up));
            }
            else
            {
                wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(wave.GetComponent<Collider>());
                wave.transform.position = spawnPos;
                wave.transform.localScale = new Vector3(1.6f, 0.08f, 0.6f);
            }

            BossProjectilePrototipo3 bp = wave.GetComponent<BossProjectilePrototipo3>();
            if (bp == null) bp = wave.AddComponent<BossProjectilePrototipo3>();
            bp.dealsDirectHitDamage = false;
            bp.slowDuration = 3.0f;
            bp.slowMultiplier = 0.45f;
            bp.maxLifetime = 1.6f;
            bp.InitLinear(BossProjectileKind.DogSonicBarkWave, dir, 15.5f, transform);
        }
    }
}
