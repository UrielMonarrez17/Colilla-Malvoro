using UnityEngine;

namespace Prototipo3.PCG
{
    [SelectionBase]
    public class EnemySpawnerPrototipo3 : MonoBehaviour
    {
        [Header("Configuración del Spawner")]
        public GameObject enemyPrefab;
        public bool spawnOnAwake = false;

        [Header("Estado")]
        [SerializeField] private bool hasSpawned = false;
        private GameObject spawnedInstance;

        public bool HasSpawned => hasSpawned;
        public GameObject SpawnedInstance => spawnedInstance;

        private void Start()
        {
            if (spawnOnAwake && !hasSpawned)
            {
                SpawnEnemy();
            }
        }

        public GameObject SpawnEnemy()
        {
            if (hasSpawned && spawnedInstance != null) return spawnedInstance;
            if (enemyPrefab == null) return null;

            spawnedInstance = Instantiate(enemyPrefab, transform.position, transform.rotation, transform);
            hasSpawned = true;

            var ai = spawnedInstance.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.enabled = true;
            }

            return spawnedInstance;
        }
    }
}
