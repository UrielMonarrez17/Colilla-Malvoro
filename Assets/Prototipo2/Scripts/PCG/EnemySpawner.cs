using UnityEngine;
using UnityEngine.AI;

namespace Prototipo2
{
    [SelectionBase]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Configuración del Enemigo Feral")]
        public GameObject enemyPrefab;
        public bool spawnOnAwake = true;
        public bool spawnOnlyAtNight = false;
        public float patrolRadius = 8f;

        [Header("Estado")]
        [SerializeField] private bool hasSpawned = false;
        private GameObject spawnedEnemyInstance;

        public bool HasSpawned => hasSpawned;
        public GameObject SpawnedEnemyInstance => spawnedEnemyInstance;

        private void Start()
        {
            if (spawnOnAwake && !hasSpawned)
            {
                if (!spawnOnlyAtNight || IsNight())
                {
                    SpawnEnemy();
                }
            }
        }

        private void Update()
        {
            if (spawnOnlyAtNight && !hasSpawned && IsNight())
            {
                SpawnEnemy();
            }
        }

        private bool IsNight()
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            }
            return true;
        }

        public GameObject SpawnEnemy()
        {
            if (hasSpawned && spawnedEnemyInstance != null) return spawnedEnemyInstance;
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] {name} no tiene enemyPrefab asignado.");
                return null;
            }

            float citySurfaceY = CityGeneratorPrototipo2.Instance != null ? CityGeneratorPrototipo2.Instance.roadHeightY : 0f;
            Vector3 spawnPos = transform.position;
            spawnPos.y = citySurfaceY;

            // Proyectar directamente sobre el NavMesh horneado de las calles de la ciudad
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10.0f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else
            {
                // Fallback por raycast sobre la superficie de la calle si el NavMesh estuviera en proceso
                Vector3 rayOrigin = new Vector3(spawnPos.x, citySurfaceY + 5f, spawnPos.z);
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (hits[i].point.y >= citySurfaceY - 0.5f && hits[i].point.y <= citySurfaceY + 1.0f)
                    {
                        spawnPos.y = hits[i].point.y;
                        break;
                    }
                }
            }

            spawnedEnemyInstance = Instantiate(enemyPrefab, spawnPos, transform.rotation, transform);
            spawnedEnemyInstance.name = $"Feral_{name}";
            spawnedEnemyInstance.tag = "Enemy";

            // Asegurar que la posición mundial quede sobre la ciudad incluso si el prefab tenía offset residual
            spawnedEnemyInstance.transform.position = spawnPos;

            NavMeshAgent agent = spawnedEnemyInstance.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.baseOffset = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                if (agent.enabled)
                {
                    agent.Warp(spawnPos);
                }
            }

            if (spawnedEnemyInstance.GetComponent<EnemyOnCollision>() == null)
            {
                spawnedEnemyInstance.AddComponent<EnemyOnCollision>();
            }

            EnemyAI ai = spawnedEnemyInstance.GetComponent<EnemyAI>();
            if (ai != null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) ai.player = hc.transform;

                // Dejar todo a la máquina de estados del enemigo: sin waypoints de patrulla y con radio de wander amplio
                ai.SetPatrolWaypoints(null);
                if (ai.wanderRadius < 30f)
                {
                    ai.wanderRadius = 35f;
                }
            }

            EnemyAIPrototipo2 protoAi = spawnedEnemyInstance.GetComponent<EnemyAIPrototipo2>();
            if (protoAi != null)
            {
                protoAi.minCitySurfaceY = citySurfaceY;
                protoAi.SnapToCitySurfaceImmediate();
            }

            hasSpawned = true;
            return spawnedEnemyInstance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.75f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.6f, 0.75f);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.6f, new Vector3(1f, 1.2f, 1f));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
    }
}
