using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.PCG
{
    [SelectionBase]
    public class DropSpawnerPrototipo3 : MonoBehaviour
    {
        [Header("Configuración del Drop")]
        public GameObject lootContainerPrefab;
        public List<ItemType> guaranteedItems = new List<ItemType> { ItemType.Desperdicios };
        [Range(0f, 1f)] public float spawnChance = 1.0f;
        public bool spawnOnAwake = true;

        [Header("Estado")]
        [SerializeField] private bool hasSpawned = false;
        private GameObject spawnedInstance;

        public bool HasSpawned => hasSpawned;
        public GameObject SpawnedInstance => spawnedInstance;

        private void Start()
        {
            if (spawnOnAwake && !hasSpawned)
            {
                SpawnDrop();
            }
        }

        public GameObject SpawnDrop()
        {
            if (hasSpawned && spawnedInstance != null) return spawnedInstance;
            if (lootContainerPrefab == null)
            {
                return null;
            }

            if (Random.value > spawnChance) return null;

            spawnedInstance = Instantiate(lootContainerPrefab, transform.position, transform.rotation, transform);
            hasSpawned = true;

            var container = spawnedInstance.GetComponent<LootContainer>() ?? spawnedInstance.GetComponentInChildren<LootContainer>();
            if (container != null && guaranteedItems != null && guaranteedItems.Count > 0)
            {
                container.itemsGarantizados.AddRange(guaranteedItems);
            }

            return spawnedInstance;
        }
    }
}
