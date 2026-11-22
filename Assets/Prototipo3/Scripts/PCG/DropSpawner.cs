using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3
{
    [SelectionBase]
    public class DropSpawner : MonoBehaviour
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
                Debug.LogWarning($"[DropSpawner] {name} no tiene lootContainerPrefab asignado.");
                return null;
            }

            if (Random.value > spawnChance) return null;

            Vector3 spawnPos = transform.position;
            spawnedInstance = Instantiate(lootContainerPrefab, spawnPos, transform.rotation, transform);
            spawnedInstance.name = $"DropContainer_{name}";
            spawnedInstance.tag = "Interactable";

            LootContainer lc = spawnedInstance.GetComponent<LootContainer>();
            if (lc == null) lc = spawnedInstance.AddComponent<LootContainer>();
            if (guaranteedItems != null && guaranteedItems.Count > 0)
            {
                lc.itemsGarantizados = new List<ItemType>(guaranteedItems);
            }

            hasSpawned = true;
            return spawnedInstance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.4f, 0.5f);
            Gizmos.color = new Color(0.1f, 0.6f, 1f, 0.25f);
            Gizmos.DrawCube(transform.position + Vector3.up * 0.4f, new Vector3(0.6f, 0.8f, 0.6f));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.4f, 0.8f);
        }
    }
}
