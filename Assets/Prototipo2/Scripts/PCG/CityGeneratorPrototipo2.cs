using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo2
{
    public class CityGeneratorPrototipo2 : MonoBehaviour
    {
        public static CityGeneratorPrototipo2 Instance { get; private set; }

        [Header("Motor Matematico (PRNG)")]
        public int seed = 12345;

        [Header("Dimensiones de la Ciudad")]
        public int mapWidth = 40;
        public int mapLength = 40;
        public float cellSize = 10f;

        [Header("Listas de Edificios")]
        public List<GameObject> specialBuildings = new List<GameObject>();
        public List<GameObject> normalBuildings = new List<GameObject>();

        [Header("Loot / Drops")]
        public GameObject lootPrefab;
        [Range(0f, 1f)] public float lootProbability = 0.65f;

        [Header("Prefabs de Caminos y Suelos")]
        public GameObject roadForward;
        public GameObject roadAngle;
        public GameObject roadT;
        public GameObject roadX;
        public GameObject roadDeadEnd;
        public GameObject pedestrianPavement;

        [Header("Ajustes")]
        public float roadHeightY = 0f;
        public CityGenerationSettings settings = new CityGenerationSettings();

        [Header("Puntos de Inicio / Refugio")]
        public GameObject craftTablePrefab;
        public Transform hamsterTransform;

        [Header("Enemigos Ferales (Multiespecie)")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public int maxEnemiesToSpawn = 12;
        public float minEnemyDistanceFromSpawn = 30f;

        // Datos generados
        public Vector3 PlayerSpawnPoint { get; private set; }
        public Vector3 CraftTableSpawnPoint { get; private set; }
        public List<Vector3> LootSpawnPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> EnemySpawnPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> IntersectionPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> DeadEndPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> LoopCenterPoints { get; private set; } = new List<Vector3>();

        public CityGrid Grid { get; private set; }
        public RoadNetwork LastNetwork { get; private set; }

        public event Action OnCityGenerated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);
        }

        private void Start()
        {
            if (transform.childCount == 0)
            {
                GenerateCity();
            }
        }

        [ContextMenu("Generar Ciudad Completa en Editor")]
        public void GenerateCity()
        {
            ClearCity();

            LootSpawnPoints.Clear();
            EnemySpawnPoints.Clear();
            IntersectionPoints.Clear();
            DeadEndPoints.Clear();
            LoopCenterPoints.Clear();

            var rng = new CityRandom(new MathPRNG(seed));
            var grid = new CityGrid(mapWidth, mapLength);
            var network = new RoadNetwork(grid, settings);
            var spawner = new BuildingSpawner(transform, cellSize, grid, rng);

            // 1. Red de caminos
            new RoadNetworkBuilder(settings, rng, network).Build();

            // 2. Especiales con camino de acceso garantizado
            new SpecialBuildingPlacer(settings, rng, network, spawner).PlaceAll(specialBuildings);

            // 3. Relleno con edificios normales
            new NormalBuildingPlacer(settings, rng, grid, spawner).PlaceAll(normalBuildings);

            // 4. Instanciar calles y pavimento
            var roadPrefabs = new RoadPrefabs
            {
                forward = roadForward,
                angle = roadAngle,
                t = roadT,
                x = roadX,
                deadEnd = roadDeadEnd,
                pavement = pedestrianPavement
            };
            new CityInstantiator(grid, transform, cellSize, roadHeightY, roadPrefabs).Instantiate();

            Grid = grid;
            LastNetwork = network;

            // 5. Analizar características viales (intersecciones, callejones sin salida, rotondas/loops)
            AnalizarTopologiaVial(grid, network);

            // 6. Spawn seguro del Hámster y Mesa de Trabajo inicial
            UbicarRefugioInicial(grid, rng);

            // 7. Generar Loot usando Sockets de edificios
            GenerarLootEnSockets(grid, rng);

            // 8. Generar Enemigos Ferales multiespecie en loops, intersecciones y dead-ends
            GenerarEnemigosFerales(rng);

            OnCityGenerated?.Invoke();
        }

        private void AnalizarTopologiaVial(CityGrid grid, RoadNetwork network)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Length; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!grid.IsRoad(cell)) continue;

                    Vector3 worldPos = new Vector3(x * cellSize + cellSize * 0.5f, roadHeightY, y * cellSize + cellSize * 0.5f);
                    int neighbors = grid.RoadNeighborCount(cell);

                    if (neighbors == 1)
                    {
                        DeadEndPoints.Add(worldPos);
                    }
                    else if (neighbors >= 3)
                    {
                        IntersectionPoints.Add(worldPos);
                    }
                }
            }

            if (network != null && network.Loops != null)
            {
                foreach (RoadLoop loop in network.Loops.All)
                {
                    Vector3 center = Vector3.zero;
                    int count = 0;
                    foreach (int index in loop.CellIndices)
                    {
                        int lx = index % mapWidth;
                        int ly = index / mapWidth;
                        center += new Vector3(lx * cellSize + cellSize * 0.5f, roadHeightY, ly * cellSize + cellSize * 0.5f);
                        count++;
                    }
                    if (count > 0)
                    {
                        center /= count;
                        LoopCenterPoints.Add(center);
                    }
                }
            }
        }

        private void UbicarRefugioInicial(CityGrid grid, CityRandom rng)
        {
            // Buscar una celda de camino segura cerca del borde sur/oeste para que sea la base
            Vector2Int bestCell = new Vector2Int(-1, -1);
            float minDistance = float.MaxValue;

            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Length - 1; y++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if (grid.IsRoad(p))
                    {
                        // Priorizar esquinas o bordes perimetrales
                        float dist = p.x + p.y;
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestCell = p;
                        }
                    }
                }
            }

            if (bestCell.x < 0)
            {
                bestCell = new Vector2Int(grid.Width / 2, grid.Length / 2);
            }

            PlayerSpawnPoint = new Vector3(bestCell.x * cellSize + cellSize * 0.5f, roadHeightY + 0.5f, bestCell.y * cellSize + cellSize * 0.5f);
            CraftTableSpawnPoint = PlayerSpawnPoint + new Vector3(2.0f, 0f, 1.5f);

            // Marcador jerarquico explicito para el Spawn del Hamster y Refugio
            GameObject shelterMarker = new GameObject("SPAWN_HAMSTER_REFUGIO");
            shelterMarker.transform.SetParent(transform, false);
            shelterMarker.transform.position = PlayerSpawnPoint;

            // Mover el hámster si está asignado
            if (hamsterTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) hamsterTransform = hc.transform;
            }

            if (hamsterTransform != null)
            {
                Rigidbody rb = hamsterTransform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = PlayerSpawnPoint;
                }
                hamsterTransform.position = PlayerSpawnPoint;
                hamsterTransform.rotation = Quaternion.LookRotation(Vector3.forward);
            }

            // Spawn o reposicionar CraftTable
            CraftingTable existingTable = FindAnyObjectByType<CraftingTable>();
            if (existingTable != null)
            {
                existingTable.transform.position = CraftTableSpawnPoint;
                existingTable.transform.rotation = Quaternion.identity;
            }
            else if (craftTablePrefab != null)
            {
                Instantiate(craftTablePrefab, CraftTableSpawnPoint, Quaternion.identity, transform);
            }
            else
            {
                // Crear CraftTable de inicio proceduralmente
                GameObject tableObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableObj.name = "CraftTable_Prototipo2";
                tableObj.tag = "Interactable";
                tableObj.transform.SetParent(transform);
                tableObj.transform.position = CraftTableSpawnPoint + new Vector3(0, 0.45f, 0);
                tableObj.transform.localScale = new Vector3(1.5f, 0.8f, 1.0f);

                BoxCollider bc = tableObj.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.isTrigger = true;
                    bc.size = new Vector3(3.5f, 2.5f, 3.5f);
                }

                Renderer ren = tableObj.GetComponent<Renderer>();
                if (ren != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.48f, 0.30f, 0.15f);
                    ren.material = mat;
                }

                tableObj.AddComponent<CraftingTable>();
            }
        }

        private void GenerarLootEnSockets(CityGrid grid, CityRandom rng)
        {
            if (lootPrefab == null) return;

            GameObject dropRoot = new GameObject("SPAWNERS_DROPS");
            dropRoot.transform.SetParent(transform, false);

            int spawnerIndex = 1;

            // 1. Spawners en callejones sin salida (DeadEnds) - Ubicaciones privilegiadas de botín
            foreach (Vector3 deadEndPos in DeadEndPoints)
            {
                if (Vector3.Distance(deadEndPos, PlayerSpawnPoint) < 6.0f) continue;

                Vector3 dropPos = deadEndPos + new Vector3(rng.Range(-1.5f, 1.5f), 0, rng.Range(-1.5f, 1.5f));
                dropPos.y = roadHeightY;

                GameObject spawnerObj = new GameObject($"DropSpawner_Alley_{spawnerIndex++}");
                spawnerObj.transform.SetParent(dropRoot.transform, false);
                spawnerObj.transform.position = dropPos;

                DropSpawner spawner = spawnerObj.AddComponent<DropSpawner>();
                spawner.lootContainerPrefab = lootPrefab;
                spawner.spawnChance = 0.95f;
                spawner.guaranteedItems = new List<ItemType> { ItemType.Desperdicios, ItemType.Madera };
                spawner.SpawnDrop();

                LootSpawnPoints.Add(dropPos);
            }

            // 2. Spawners en fachadas y esquinas de edificios
            CitySocket[] spawnedBuildings = GetComponentsInChildren<CitySocket>();
            foreach (CitySocket socket in spawnedBuildings)
            {
                if (socket.frontSocket == null) continue;
                if (!rng.Chance(lootProbability)) continue;

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
                    worldPos.y = roadHeightY;

                    int gridX = Mathf.FloorToInt(worldPos.x / cellSize);
                    int gridY = Mathf.FloorToInt(worldPos.z / cellSize);
                    Vector2Int cell = new Vector2Int(gridX, gridY);

                    if (grid.InBounds(cell))
                    {
                        GameObject spawnerObj = new GameObject($"DropSpawner_Building_{spawnerIndex++}");
                        spawnerObj.transform.SetParent(dropRoot.transform, false);
                        spawnerObj.transform.position = worldPos;

                        DropSpawner spawner = spawnerObj.AddComponent<DropSpawner>();
                        spawner.lootContainerPrefab = lootPrefab;
                        spawner.spawnChance = 0.85f;
                        spawner.guaranteedItems = new List<ItemType> { ItemType.Desperdicios, ItemType.Metal };
                        spawner.SpawnDrop();

                        LootSpawnPoints.Add(worldPos);
                        break;
                    }
                }
            }
        }

        private void GenerarEnemigosFerales(CityRandom rng)
        {
            if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

            GameObject enemyRoot = new GameObject("SPAWNERS_ENEMIGOS");
            enemyRoot.transform.SetParent(transform, false);

            List<Vector3> candidatePoints = new List<Vector3>();
            candidatePoints.AddRange(LoopCenterPoints);
            candidatePoints.AddRange(IntersectionPoints);
            candidatePoints.AddRange(DeadEndPoints);

            List<Vector3> safeEnemySpawns = new List<Vector3>();
            foreach (Vector3 pt in candidatePoints)
            {
                if (Vector3.Distance(pt, PlayerSpawnPoint) >= minEnemyDistanceFromSpawn)
                {
                    safeEnemySpawns.Add(pt);
                }
            }

            for (int i = 0; i < safeEnemySpawns.Count; i++)
            {
                int r = rng.Range(i, safeEnemySpawns.Count);
                (safeEnemySpawns[i], safeEnemySpawns[r]) = (safeEnemySpawns[r], safeEnemySpawns[i]);
            }

            int countToSpawn = Mathf.Min(maxEnemiesToSpawn, safeEnemySpawns.Count);
            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos = safeEnemySpawns[i];
                spawnPos.y = roadHeightY;

                int prefabIdx = rng.Range(0, enemyPrefabs.Count);
                GameObject chosenPrefab = enemyPrefabs[prefabIdx];
                if (chosenPrefab != null)
                {
                    GameObject spawnerObj = new GameObject($"EnemySpawner_{i + 1}_{chosenPrefab.name}");
                    spawnerObj.transform.SetParent(enemyRoot.transform, false);
                    spawnerObj.transform.position = spawnPos;
                    spawnerObj.transform.rotation = Quaternion.Euler(0, rng.Range(0, 360), 0);

                    EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
                    spawner.enemyPrefab = chosenPrefab;
                    spawner.patrolRadius = 9f;
                    spawner.spawnOnlyAtNight = false; // Spawnea inmediatamente para que esté activo en la demo
                    spawner.SpawnEnemy();

                    EnemySpawnPoints.Add(spawnPos);
                }
            }
        }

        [ContextMenu("Limpiar Ciudad")]
        public void ClearCity()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                // No destruir al hamster si casualmente estuviera como hijo
                if (child == hamsterTransform) continue;
                Destroy(child.gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (LastNetwork == null) return;

            Gizmos.color = Color.yellow;
            foreach (RoadLoop loop in LastNetwork.Loops.All)
            {
                foreach (int index in loop.CellIndices)
                {
                    int x = index % mapWidth;
                    int y = index / mapWidth;
                    Vector3 center = new Vector3(x * cellSize + cellSize / 2f, roadHeightY + 0.5f,
                                                 y * cellSize + cellSize / 2f);
                    Gizmos.DrawWireCube(center, new Vector3(cellSize * 0.8f, 1f, cellSize * 0.8f));
                }
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(PlayerSpawnPoint, 2.5f);

            Gizmos.color = Color.red;
            foreach (Vector3 enemyPt in EnemySpawnPoints)
            {
                Gizmos.DrawWireSphere(enemyPt, 1.8f);
            }
        }
    }
}
