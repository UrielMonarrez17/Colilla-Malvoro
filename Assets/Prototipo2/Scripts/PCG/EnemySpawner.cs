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

            Vector3 spawnPos = transform.position;
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            spawnedEnemyInstance = Instantiate(enemyPrefab, spawnPos, transform.rotation, transform);
            spawnedEnemyInstance.name = $"Feral_{name}";
            spawnedEnemyInstance.tag = "Enemy";

            EnemyAI ai = spawnedEnemyInstance.GetComponent<EnemyAI>();
            if (ai != null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) ai.player = hc.transform;

                // Configurar waypoints de patrulla alrededor del spawner
                Vector3 p1 = spawnPos + transform.forward * patrolRadius * 0.5f;
                Vector3 p2 = spawnPos - transform.forward * patrolRadius * 0.5f;
                ai.SetPatrolWaypoints(new Vector3[] { p1, p2 });
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
