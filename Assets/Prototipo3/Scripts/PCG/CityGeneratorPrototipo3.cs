using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Prototipo3.PCG
{
    public class CityGeneratorPrototipo3 : MonoBehaviour
    {
        public static CityGeneratorPrototipo3 Instance { get; private set; }

        [Header("Motor Matemático (PRNG)")]
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

        [Header("Enemigos Ferales")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public int maxEnemiesToSpawn = 10;
        public float minEnemyDistanceFromSpawn = 25f;

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
            GenerateCity();
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

            // 2. Especiales con camino garantizado
            new SpecialBuildingPlacer(settings, rng, network, spawner).PlaceAll(specialBuildings);

            // 3. Relleno con edificios normales
            new NormalBuildingPlacer(settings, rng, grid, spawner).PlaceAll(normalBuildings);

            // 4. Instanciar calles
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

            // Navegación y colisiones
            ConfigurarNavegacionYColisionesCiudad();

            // Análisis de topología
            AnalizarTopologiaVial(grid, network);

            // Ubicar refugio inicial y Barnaby
            UbicarRefugioInicial(grid, rng);

            // Generar Loot y Drops
            GenerarLootEnSockets(grid, rng);

            // Generar patrullas ferales
            GenerarEnemigosFerales(rng);

            OnCityGenerated?.Invoke();
        }

        private void ConfigurarNavegacionYColisionesCiudad()
        {
            BuildingData[] buildings = GetComponentsInChildren<BuildingData>(true);
            foreach (BuildingData b in buildings)
            {
                if (b == null) continue;
                GameObject bObj = b.gameObject;

                BoxCollider box = bObj.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = bObj.AddComponent<BoxCollider>();
                    Renderer ren = bObj.GetComponent<Renderer>() ?? bObj.GetComponentInChildren<Renderer>();
                    if (ren != null)
                    {
                        box.center = bObj.transform.InverseTransformPoint(ren.bounds.center);
                        Vector3 worldSize = ren.bounds.size;
                        Vector3 lossy = bObj.transform.lossyScale;
                        box.size = new Vector3(
                            lossy.x > 0.001f ? worldSize.x / lossy.x : worldSize.x,
                            lossy.y > 0.001f ? worldSize.y / lossy.y : worldSize.y,
                            lossy.z > 0.001f ? worldSize.z / lossy.z : worldSize.z
                        );
                    }
                }

                box.enabled = true;
                box.isTrigger = false;

                NavMeshObstacle obstacle = bObj.GetComponent<NavMeshObstacle>();
                if (obstacle == null)
                {
                    obstacle = bObj.AddComponent<NavMeshObstacle>();
                }
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = box.center;
                obstacle.size = box.size;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.carvingMoveThreshold = 0.1f;
                obstacle.carvingTimeToStationary = 0.1f;
            }

            NavMeshSurface navSurface = GetComponent<NavMeshSurface>();
            if (navSurface == null)
            {
                navSurface = gameObject.AddComponent<NavMeshSurface>();
            }
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();
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
            Vector2Int bestCell = new Vector2Int(-1, -1);
            float minDistance = float.MaxValue;

            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Length - 1; y++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if (grid.IsRoad(p))
                    {
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

            Vector3 rawSpawnPoint = new Vector3(bestCell.x * cellSize + cellSize * 0.5f, roadHeightY, bestCell.y * cellSize + cellSize * 0.5f);
            if (NavMesh.SamplePosition(rawSpawnPoint, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                PlayerSpawnPoint = navHit.position + Vector3.up * 0.05f;
            }
            else
            {
                PlayerSpawnPoint = new Vector3(bestCell.x * cellSize + cellSize * 0.5f, roadHeightY + 0.1f, bestCell.y * cellSize + cellSize * 0.5f);
            }

            CraftTableSpawnPoint = PlayerSpawnPoint + new Vector3(2.0f, 0f, 1.5f);
            if (NavMesh.SamplePosition(CraftTableSpawnPoint, out NavMeshHit tableHit, 3f, NavMesh.AllAreas))
            {
                CraftTableSpawnPoint = tableHit.position;
            }

            GameObject shelterMarker = new GameObject("SPAWN_HAMSTER_REFUGIO");
            shelterMarker.transform.SetParent(transform, false);
            shelterMarker.transform.position = PlayerSpawnPoint;

            if (hamsterTransform == null)
            {
                var hc = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (hc != null)
                {
                    hc.enabled = true;
                    hamsterTransform = hc.transform;
                }
            }

            if (hamsterTransform != null)
            {
                var rb = hamsterTransform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = PlayerSpawnPoint;
                }
                hamsterTransform.position = PlayerSpawnPoint;
                hamsterTransform.rotation = Quaternion.LookRotation(Vector3.forward);
            }
        }

        private void GenerarLootEnSockets(CityGrid grid, CityRandom rng)
        {
            if (lootPrefab == null) return;

            GameObject dropRoot = new GameObject("SPAWNERS_DROPS");
            dropRoot.transform.SetParent(transform, false);

            int spawnerIndex = 1;
            foreach (Vector3 deadEndPos in DeadEndPoints)
            {
                if (Vector3.Distance(deadEndPos, PlayerSpawnPoint) < 6.0f) continue;

                Vector3 dropPos = deadEndPos + new Vector3((float)rng.Range(-1, 2), 0, (float)rng.Range(-1, 2));
                dropPos.y = roadHeightY;

                GameObject spawnerObj = new GameObject($"DropSpawner_Alley_{spawnerIndex++}");
                spawnerObj.transform.SetParent(dropRoot.transform, false);
                spawnerObj.transform.position = dropPos;

                var spawner = spawnerObj.AddComponent<DropSpawnerPrototipo3>();
                spawner.lootContainerPrefab = lootPrefab;
                spawner.spawnChance = 0.95f;
                spawner.SpawnDrop();

                LootSpawnPoints.Add(dropPos);
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

                    var spawner = spawnerObj.AddComponent<EnemySpawnerPrototipo3>();
                    spawner.enemyPrefab = chosenPrefab;
                    spawner.spawnOnAwake = false;
                    spawner.SpawnEnemy();

                    EnemySpawnPoints.Add(spawnPos);
                }
            }
        }

        public void ClearCity()
        {
            LootSpawnPoints.Clear();
            EnemySpawnPoints.Clear();
            IntersectionPoints.Clear();
            DeadEndPoints.Clear();
            LoopCenterPoints.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == hamsterTransform) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            if (!Application.isPlaying)
            {
                var navSurface = GetComponent<NavMeshSurface>();
                if (navSurface != null) navSurface.RemoveData();
            }
        }
    }
}
