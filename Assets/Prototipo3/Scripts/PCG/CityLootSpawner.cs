using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Componente especializado SOLID (Principio de Responsabilidad Única) para la distribución
    /// y generación de contenedores de botín (Loot) en la ciudad procedural.
    /// </summary>
    public class CityLootSpawner : MonoBehaviour
    {
        [Header("Configuración de Loot")]
        public GameObject lootPrefab;
        [Range(0f, 1f)] public float lootProbability = 0.65f;

        public void ClearLoot()
        {
            Transform dropRoot = transform.Find("SPAWNERS_DROPS");
            if (dropRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(dropRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(dropRoot.gameObject);
                }
            }
        }

        public void SpawnLoot(CityGeneratorPrototipo3 cityGen, CityGrid grid, CityRandom rng)
        {
            if (cityGen == null || grid == null || rng == null) return;

            ClearLoot();

            GameObject dropRoot = new GameObject("SPAWNERS_DROPS");
            dropRoot.transform.SetParent(transform, false);

            GameObject effectiveLootPrefab = lootPrefab != null ? lootPrefab : cityGen.lootPrefab;
            float effectiveProbability = lootPrefab != null ? lootProbability : cityGen.lootProbability;

            int spawnerIndex = 1;

            // Garantizar 3 contenedores iniciales cerca de la base para suministros
            Vector3[] starterOffsets =
            {
                new Vector3(-3.2f, 0f, 2.5f),
                new Vector3(3.5f, 0f, -2.8f),
                new Vector3(-2.5f, 0f, -3.5f)
            };
            ItemType[][] starterPacks =
            {
                new[] { ItemType.Metal, ItemType.Desperdicios, ItemType.Madera },
                new[] { ItemType.Metal, ItemType.Semillas, ItemType.Goma },
                new[] { ItemType.Madera, ItemType.Goma, ItemType.Semillas }
            };

            for (int s = 0; s < starterOffsets.Length; s++)
            {
                Vector3 pos = cityGen.PlayerSpawnPoint + starterOffsets[s];
                pos.y = cityGen.roadHeightY;
                CreateLootContainerInstance(dropRoot.transform, $"StarterDrop_{s + 1}", pos, new List<ItemType>(starterPacks[s]), effectiveLootPrefab);
                cityGen.LootSpawnPoints.Add(pos);
            }

            if (effectiveLootPrefab == null) return;

            foreach (Vector3 deadEndPos in cityGen.DeadEndPoints)
            {
                if (Vector3.Distance(deadEndPos, cityGen.PlayerSpawnPoint) < 6.0f) continue;

                Vector3 dropPos = deadEndPos + new Vector3((float)rng.Range(-1, 2), 0, (float)rng.Range(-1, 2));
                dropPos.y = cityGen.roadHeightY;

                GameObject spawnerObj = new GameObject($"DropSpawner_Alley_{spawnerIndex++}");
                spawnerObj.transform.SetParent(dropRoot.transform, false);
                spawnerObj.transform.position = dropPos;

                DropSpawner spawner = spawnerObj.AddComponent<DropSpawner>();
                spawner.lootContainerPrefab = effectiveLootPrefab;
                spawner.spawnChance = 0.95f;
                spawner.guaranteedItems = new List<ItemType> { ItemType.Desperdicios, ItemType.Madera, ItemType.Goma };
                spawner.SpawnDrop();

                cityGen.LootSpawnPoints.Add(dropPos);
            }

            CitySocket[] spawnedBuildings = cityGen.GetComponentsInChildren<CitySocket>();
            foreach (CitySocket socket in spawnedBuildings)
            {
                if (socket == null || socket.frontSocket == null) continue;
                if (!rng.Chance(effectiveProbability)) continue;

                float z = socket.frontSocket.localPosition.z;
                Vector3[] localOffsets = {
                    new Vector3(z * 0.9f, 0, 0),
                    new Vector3(-z * 0.9f, 0, 0),
                    new Vector3(0, 0, -z * 0.9f)
                };

                for (int i = 0; i < localOffsets.Length; i++)
                {
                    int rand = rng.Range(i, localOffsets.Length);
                    (localOffsets[i], localOffsets[rand]) = (localOffsets[rand], localOffsets[i]);
                }

                foreach (Vector3 offset in localOffsets)
                {
                    Vector3 worldPos = socket.transform.TransformPoint(offset);
                    worldPos.y = cityGen.roadHeightY;

                    int gridX = Mathf.FloorToInt(worldPos.x / cityGen.cellSize);
                    int gridY = Mathf.FloorToInt(worldPos.z / cityGen.cellSize);
                    Vector2Int cell = new Vector2Int(gridX, gridY);

                    if (grid.InBounds(cell))
                    {
                        GameObject spawnerObj = new GameObject($"DropSpawner_Building_{spawnerIndex++}");
                        spawnerObj.transform.SetParent(dropRoot.transform, false);
                        spawnerObj.transform.position = worldPos;

                        DropSpawner spawner = spawnerObj.AddComponent<DropSpawner>();
                        spawner.lootContainerPrefab = effectiveLootPrefab;
                        spawner.spawnChance = 0.85f;
                        spawner.guaranteedItems = new List<ItemType> { ItemType.Metal, ItemType.Semillas };
                        spawner.SpawnDrop();

                        cityGen.LootSpawnPoints.Add(worldPos);
                        break;
                    }
                }
            }
        }

        public GameObject CreateLootContainerInstance(Transform parent, string containerName, Vector3 pos, List<ItemType> guaranteed, GameObject prefabToUse)
        {
            GameObject obj;
            if (prefabToUse != null)
            {
                obj = Instantiate(prefabToUse, pos, Quaternion.identity, parent);
                obj.name = containerName;
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = containerName;
                obj.transform.SetParent(parent, false);
                obj.transform.position = pos + new Vector3(0f, 0.35f, 0f);
                obj.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
                BoxCollider bc = obj.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.isTrigger = true;
                    bc.size = new Vector3(2.4f, 2.2f, 2.4f);
                }
            }

            obj.tag = "Interactable";
            LootContainer lc = obj.GetComponent<LootContainer>();
            if (lc == null) lc = obj.AddComponent<LootContainer>();
            lc.itemsGarantizados = guaranteed;
            lc.ConfigurarEfectoResplandor();
            return obj;
        }
    }
}
