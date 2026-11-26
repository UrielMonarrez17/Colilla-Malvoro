using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Prototipo3
{
    /// <summary>
    /// Generador procedural de ciudad para Prototipo 3.
    /// Mantiene la arquitectura matemática (fases 1 a 4 + SocketSpawnRule) de CityGeneratorMathVersion,
    /// integrando refugio seguro (InteriorBuilding), horneado de NavMesh, loots en bordes exteriores de edificios
    /// y delegación de población enemiga en CityEnemyPopulationSpawner.
    /// </summary>
    public class CityGeneratorPrototipo3 : MonoBehaviour
    {
        public static CityGeneratorPrototipo3 Instance { get; private set; }

        [Header("Motor Matematico (PRNG)")]
        public int seed = 12345;

        [Header("Dimensiones de la Ciudad")]
        public int mapWidth = 30;
        public int mapLength = 30;
        public float cellSize = 10f;

        [Header("Listas de Edificios")]
        public List<GameObject> specialBuildings = new List<GameObject>();
        public List<GameObject> normalBuildings = new List<GameObject>();

        [Header("Refugio Seguro (InteriorBuilding)")]
        public GameObject safeRefugePrefab;

        [Header("Objetos junto a edificios (loot, props, etc.)")]
        public List<SocketSpawnRule> socketSpawns = new List<SocketSpawnRule>();

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

        [Header("Supervivencia (Jugador, Crafteo y Loot)")]
        public Transform hamsterTransform;
        public GameObject craftTablePrefab;
        public GameObject lootPrefab;
        [Range(0f, 1f)] public float lootProbability = 0.65f;

        // Datos públicos para sistemas de juego y enemigos
        public Vector3 PlayerSpawnPoint { get; private set; }
        public Vector3 CraftTableSpawnPoint { get; private set; }
        public Vector3 SafeRefugeCenter { get; private set; }
        public Vector3 SafeRefugeEntrancePoint { get; private set; }
        public List<Vector3> LootSpawnPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> EnemySpawnPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> IntersectionPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> DeadEndPoints { get; private set; } = new List<Vector3>();
        public List<Vector3> LoopCenterPoints { get; private set; } = new List<Vector3>();

        public CityGrid Grid { get; private set; }
        public RoadNetwork LastNetwork { get; private set; }

        public event Action OnCityGenerated;

        private RoadNetwork lastNetwork;
        private GameObject safeRefugeInstance;
        private Vector3 safeRefugeFacingDir = Vector3.back;

        private const string PendingSeedPrefKey = "Feral_PendingSeed";
        private const string PendingWidthPrefKey = "Feral_PendingWidth";
        private const string PendingLengthPrefKey = "Feral_PendingLength";

        private static int? pendingSeed = null;
        private static int? pendingWidth = null;
        private static int? pendingLength = null;

        public static void SetPendingSeed(int newSeed)
        {
            pendingSeed = newSeed;
            PlayerPrefs.SetInt(PendingSeedPrefKey, newSeed);
            PlayerPrefs.Save();
            Debug.Log($"[CityGeneratorPrototipo3] Semilla pendiente configurada: {newSeed}");
        }

        public static void SetPendingCityConfig(int newSeed, int width = 30, int length = 30)
        {
            int w = Mathf.Clamp(width, 20, 60);
            int l = Mathf.Clamp(length, 20, 60);
            pendingSeed = newSeed;
            pendingWidth = w;
            pendingLength = l;
            PlayerPrefs.SetInt(PendingSeedPrefKey, newSeed);
            PlayerPrefs.SetInt(PendingWidthPrefKey, w);
            PlayerPrefs.SetInt(PendingLengthPrefKey, l);
            PlayerPrefs.Save();
            Debug.Log($"[CityGeneratorPrototipo3] Configuración pendiente guardada: semilla={newSeed}, tamaño={w}x{l}");
        }

        public static bool TryConsumePendingSeed(out int consumedSeed)
        {
            return TryConsumePendingCityConfig(out consumedSeed, out _, out _);
        }

        public static bool TryConsumePendingCityConfig(out int consumedSeed, out int consumedWidth, out int consumedLength)
        {
            bool hasSeed = false;
            consumedSeed = 0;
            consumedWidth = 0;
            consumedLength = 0;

            if (pendingSeed.HasValue)
            {
                consumedSeed = pendingSeed.Value;
                pendingSeed = null;
                hasSeed = true;
            }
            else if (PlayerPrefs.HasKey(PendingSeedPrefKey))
            {
                consumedSeed = PlayerPrefs.GetInt(PendingSeedPrefKey);
                hasSeed = true;
            }

            if (pendingWidth.HasValue && pendingLength.HasValue)
            {
                consumedWidth = pendingWidth.Value;
                consumedLength = pendingLength.Value;
                pendingWidth = null;
                pendingLength = null;
            }
            else if (PlayerPrefs.HasKey(PendingWidthPrefKey) && PlayerPrefs.HasKey(PendingLengthPrefKey))
            {
                consumedWidth = PlayerPrefs.GetInt(PendingWidthPrefKey);
                consumedLength = PlayerPrefs.GetInt(PendingLengthPrefKey);
            }

            if (hasSeed)
            {
                PlayerPrefs.DeleteKey(PendingSeedPrefKey);
                PlayerPrefs.DeleteKey(PendingWidthPrefKey);
                PlayerPrefs.DeleteKey(PendingLengthPrefKey);
                PlayerPrefs.Save();
            }

            return hasSeed;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);
        }

        private void Start()
        {
            if (TryConsumePendingCityConfig(out int incomingSeed, out int incomingW, out int incomingL))
            {
                seed = incomingSeed;
                if (incomingW >= 20 && incomingL >= 20)
                {
                    mapWidth = incomingW;
                    mapLength = incomingL;
                }
            }

            if (transform.childCount == 0 || pendingSeed.HasValue)
            {
                GenerateCity();
            }
        }

        public void RegenerateCityRuntime(int? customSeed = null, int? customWidth = null, int? customLength = null)
        {
            seed = customSeed ?? UnityEngine.Random.Range(10000, 999999);
            if (customWidth.HasValue) mapWidth = Mathf.Clamp(customWidth.Value, 20, 60);
            if (customLength.HasValue) mapLength = Mathf.Clamp(customLength.Value, 20, 60);
            Debug.Log($"[CityGeneratorPrototipo3] Regenerando ciudad con semilla {seed} ({mapWidth}x{mapLength})...");
            GenerateCity();
        }

        [ContextMenu("Generar Ciudad")]
        public void GenerateCity()
        {
            ClearCity();

            mapWidth = Mathf.Clamp(mapWidth, 20, 60);
            mapLength = Mathf.Clamp(mapLength, 20, 60);

            var rng = new CityRandom(new MathPRNG(seed));
            var grid = new CityGrid(mapWidth, mapLength);
            var network = new RoadNetwork(grid, settings);
            var spawner = new BuildingSpawner(transform, cellSize, grid, rng);

            // 1. Red de caminos (igual que CityGeneratorMathVersion)
            new RoadNetworkBuilder(settings, rng, network).Build();

            // 1.5 Reservar e instanciar el Refugio Seguro en un lote adyacente a la calle central (no a media calle)
            bool hasRefuge = TryReserveAndPlaceSafeRefuge(grid);

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
            lastNetwork = network;
            LastNetwork = network;

            // 5. Losa continua anti-caídas bajo toda la ciudad, colliders sólidos en edificios, NavMesh y posicionamiento
            EnsureContinuousCityGroundSlab(grid);
            ConfigureCityCollidersAndBakeNavMesh();
            AnalyzeRoadTopology(grid, network);
            PlacePlayerAndCraftTable(grid, hasRefuge);

            // 6. Loots abundantes sobre bordes de edificios + aceras, callejones e intersecciones
            EnsureDefaultLootSocketRule();
            List<Bounds> buildingBounds = CollectBuildingWorldBounds();
            SpawnSocketObjectsOnBuildingBorders(rng, grid, buildingBounds);
            SpawnAbundantStreetAndAlleyLoot(rng, grid, buildingBounds);
            SpawnStarterLootNearBase(rng, grid, buildingBounds);

            // 7. Población de enemigos delegada exclusivamente a CityEnemyPopulationSpawner + faroles nocturnos prácticos
            SpawnCityEnemies(rng);
            SnapAllAgentsToNavMesh();
            CityStreetLampManagerPrototipo3.ConfigureCityLamps(transform);

            OnCityGenerated?.Invoke();
            Debug.Log($"[CityGeneratorPrototipo3] Ciudad lista. Semilla={seed}, Tamaño={mapWidth}x{mapLength}, Refugio={SafeRefugeCenter}, Spawn={PlayerSpawnPoint}, Enemigos={EnemySpawnPoints.Count}, Loot={LootSpawnPoints.Count}");
        }

        private bool TryReserveAndPlaceSafeRefuge(CityGrid grid)
        {
            SafeRefugeCenter = Vector3.zero;
            SafeRefugeEntrancePoint = Vector3.zero;
            safeRefugeInstance = null;
            safeRefugeFacingDir = Vector3.back;

            if (safeRefugePrefab == null) return false;

            Vector2 mapCenter = new Vector2(grid.Width * 0.5f, grid.Length * 0.5f);
            Vector2Int bestCell = Vector2Int.zero;
            Vector2Int bestRoadDir = Vector2Int.zero;
            float bestScore = float.MaxValue;
            bool found = false;

            for (int x = 2; x < grid.Width - 2; x++)
            {
                for (int y = 2; y < grid.Length - 2; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (grid.Get(c) != CellType.Empty) continue;

                    int roadNeighbors = grid.RoadNeighborCount(c);
                    if (roadNeighbors == 0) continue;

                    Vector2Int chosenDir = Vector2Int.zero;
                    float bestDirAlignment = float.MaxValue;
                    for (int d = 0; d < CityGrid.Directions.Length; d++)
                    {
                        Vector2Int dir = CityGrid.Directions[d];
                        Vector2Int n = c + dir;
                        if (!grid.IsRoad(n)) continue;
                        float alignSq = ((Vector2)n - mapCenter).sqrMagnitude;
                        if (alignSq < bestDirAlignment)
                        {
                            bestDirAlignment = alignSq;
                            chosenDir = dir;
                        }
                    }

                    // Preferir celdas cercanas al centro con 1 o 2 calles adyacentes (lote residencial real, no isla entre 4 calles)
                    float distSq = ((Vector2)c - mapCenter).sqrMagnitude;
                    float roadPenalty = roadNeighbors >= 3 ? 36f : 0f;
                    float score = distSq + roadPenalty;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCell = c;
                        bestRoadDir = chosenDir;
                        found = true;
                    }
                }
            }

            if (!found) return false;

            grid.Set(bestCell, CellType.SpecialBuilding);

            Vector3 worldPos = CellToWorldCenter(bestCell);
            worldPos.y = roadHeightY;

            // La entrada del InteriorBuilding está en su cara local -Z, así que transform.forward apunta opuesto a bestRoadDir
            Vector3 worldRoadDir = new Vector3(bestRoadDir.x, 0f, bestRoadDir.y).normalized;
            Quaternion refugeRot = Quaternion.LookRotation(-worldRoadDir, Vector3.up);

            safeRefugeInstance = Instantiate(safeRefugePrefab, worldPos, refugeRot, transform);
            safeRefugeInstance.name = "RefugioSeguro_Base";

            SafeRefugeZone zone = safeRefugeInstance.GetComponent<SafeRefugeZone>();
            if (zone == null) zone = safeRefugeInstance.AddComponent<SafeRefugeZone>();
            zone.NormalizeRefugeFloorAndAnchors();

            SafeRefugeCenter = worldPos;
            safeRefugeFacingDir = worldRoadDir;

            Transform outsideAnchor = safeRefugeInstance.transform.Find("DoorwayOutsidePoint");
            SafeRefugeEntrancePoint = outsideAnchor != null
                ? outsideAnchor.position
                : worldPos + worldRoadDir * (cellSize * 0.58f);
            SafeRefugeEntrancePoint = new Vector3(SafeRefugeEntrancePoint.x, roadHeightY, SafeRefugeEntrancePoint.z);

            Transform spawnAnchor = safeRefugeInstance.transform.Find("PlayerSpawnAnchor");
            PlayerSpawnPoint = spawnAnchor != null
                ? new Vector3(spawnAnchor.position.x, roadHeightY + 0.08f, spawnAnchor.position.z)
                : worldPos + Vector3.up * 0.08f;

            if (outsideAnchor != null)
            {
                zone.doorwayOutsidePoint = outsideAnchor;
            }

            return true;
        }

        private void EnsureDefaultLootSocketRule()
        {
            if (lootPrefab == null) return;
            for (int i = 0; i < socketSpawns.Count; i++)
            {
                if (socketSpawns[i] != null && socketSpawns[i].prefab == lootPrefab)
                {
                    return;
                }
            }

            socketSpawns.Add(new SocketSpawnRule
            {
                name = "LootContenedor",
                prefab = lootPrefab,
                probability = lootProbability,
                yOffset = 0f,
                alignToBuilding = true
            });
        }

        private List<Bounds> CollectBuildingWorldBounds()
        {
            var list = new List<Bounds>();
            BuildingData[] buildings = GetComponentsInChildren<BuildingData>(true);
            for (int i = 0; i < buildings.Length; i++)
            {
                BuildingData b = buildings[i];
                if (b == null || b.GetComponentInParent<SafeRefugeZone>() != null) continue;
                list.Add(ComputeBuildingWorldBounds(b.gameObject));
            }
            return list;
        }

        private static Bounds ComputeBuildingWorldBounds(GameObject obj)
        {
            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box != null && box.enabled) return box.bounds;

            Renderer[] rens = obj.GetComponentsInChildren<Renderer>(true);
            Bounds b = default;
            bool init = false;
            for (int i = 0; i < rens.Length; i++)
            {
                if (rens[i] == null) continue;
                if (!init)
                {
                    b = rens[i].bounds;
                    init = true;
                }
                else
                {
                    b.Encapsulate(rens[i].bounds);
                }
            }
            return init ? b : new Bounds(obj.transform.position, new Vector3(7f, 4f, 7f));
        }

        private void SpawnSocketObjectsOnBuildingBorders(CityRandom rng, CityGrid grid, List<Bounds> buildingBounds)
        {
            if (socketSpawns == null || socketSpawns.Count == 0) return;

            BuildingData[] buildings = GetComponentsInChildren<BuildingData>(true);
            Vector3[] cardinalDirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

            // Máximo 1 contenedor por edificio (rotando variedad de prefab) para abundancia equilibrada (~150 loots en vez de 655) sin tirones al inicio!
            for (int b = 0; b < buildings.Length; b++)
            {
                BuildingData building = buildings[b];
                if (building == null || building.GetComponentInParent<SafeRefugeZone>() != null) continue;
                if (!rng.Chance(0.52f)) continue;

                SocketSpawnRule rule = socketSpawns[rng.Range(0, socketSpawns.Count)];
                if (rule == null || rule.prefab == null) continue;

                TrySpawnOnBuildingExteriorEdge(rule, building.gameObject, rng, grid, buildingBounds, cardinalDirs);
            }
        }

        private void EnsureContinuousCityGroundSlab(CityGrid grid)
        {
            Transform oldSlab = transform.Find("CityGroundSafetySlab");
            if (oldSlab != null)
            {
                if (Application.isPlaying) Destroy(oldSlab.gameObject);
                else DestroyImmediate(oldSlab.gameObject);
            }

            float totalW = (grid.Width + 8) * cellSize;
            float totalL = (grid.Length + 8) * cellSize;
            Vector3 center = new Vector3(grid.Width * cellSize * 0.5f, roadHeightY - 0.51f, grid.Length * cellSize * 0.5f);

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "CityGroundSafetySlab";
            slab.transform.SetParent(transform, false);
            slab.transform.position = center;
            slab.transform.localScale = new Vector3(totalW, 1.0f, totalL);

            Renderer r = slab.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material m = new Material(sh);
                Color asphalt = new Color(0.19f, 0.20f, 0.22f, 1f);
                m.color = asphalt;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", asphalt);
                m.SetFloat("_Smoothness", 0.15f);
                r.sharedMaterial = m;
            }

            BoxCollider bc = slab.GetComponent<BoxCollider>();
            if (bc != null)
            {
                bc.enabled = true;
                bc.isTrigger = false;
            }
        }

        private void SpawnAbundantStreetAndAlleyLoot(CityRandom rng, CityGrid grid, List<Bounds> buildingBounds)
        {
            List<GameObject> lootVariants = new List<GameObject>();
            if (socketSpawns != null)
            {
                for (int i = 0; i < socketSpawns.Count; i++)
                {
                    if (socketSpawns[i] != null && socketSpawns[i].prefab != null &&
                        socketSpawns[i].prefab.GetComponentInChildren<LootContainer>() != null)
                    {
                        lootVariants.Add(socketSpawns[i].prefab);
                    }
                }
            }
            if (lootVariants.Count == 0 && lootPrefab != null) lootVariants.Add(lootPrefab);
            if (lootVariants.Count == 0) return;

            var candidates = new List<Vector3>();
            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Length - 1; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    CellType ct = grid.Get(c);
                    if (ct != CellType.Road && ct != CellType.Empty) continue;

                    Vector3 cellCenter = CellToWorldCenter(c);
                    // Colocar en las esquinas/aceras de las celdas para no estorbar el centro del carril
                    candidates.Add(cellCenter + new Vector3(3.4f, 0f, 3.4f));
                    candidates.Add(cellCenter + new Vector3(-3.4f, 0f, -3.4f));
                    if (ct == CellType.Empty)
                    {
                        candidates.Add(cellCenter + new Vector3(-3.2f, 0f, 3.2f));
                    }
                }
            }

            ShuffleList(candidates, rng);
            int extraTarget = Mathf.Clamp((grid.Width * grid.Length) / 20, 36, 65);
            int spawned = 0;

            for (int i = 0; i < candidates.Count && spawned < extraTarget; i++)
            {
                Vector3 candidate = candidates[i];
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2.2f, NavMesh.AllAreas)) continue;
                Vector3 snapped = new Vector3(navHit.position.x, roadHeightY, navHit.position.z);
                if (!IsLootSpotClear(snapped, buildingBounds)) continue;

                GameObject chosen = lootVariants[rng.Range(0, lootVariants.Count)];
                GameObject obj = Instantiate(chosen, snapped, Quaternion.Euler(0f, rng.Range(0, 360), 0f), transform);
                obj.tag = "Interactable";
                LootSpawnPoints.Add(snapped);
                spawned++;
            }
        }

        private void TrySpawnOnBuildingExteriorEdge(
            SocketSpawnRule rule,
            GameObject buildingObj,
            CityRandom rng,
            CityGrid grid,
            List<Bounds> buildingBounds,
            Vector3[] cardinalDirs)
        {
            Bounds b = ComputeBuildingWorldBounds(buildingObj);

            // Probar primero caras hacia calle (CellType.Road) y como respaldo espacios vacíos (CellType.Empty)
            var roadCandidates = new List<(Vector3 pos, Vector3 dir)>();
            var emptyCandidates = new List<(Vector3 pos, Vector3 dir)>();

            for (int d = 0; d < cardinalDirs.Length; d++)
            {
                Vector3 dir = cardinalDirs[d];
                float halfExt = Mathf.Abs(dir.x) > 0.5f ? b.extents.x : b.extents.z;
                float tangentHalf = Mathf.Abs(dir.x) > 0.5f ? b.extents.z : b.extents.x;
                Vector3 tangent = new Vector3(-dir.z, 0f, dir.x);

                for (int attempt = 0; attempt < 3; attempt++)
                {
                    float slide = tangentHalf > 1.2f ? (rng.Range(-40, 41) * 0.01f * Mathf.Min(tangentHalf * 0.55f, 2.5f)) : 0f;
                    Vector3 pos = new Vector3(b.center.x, roadHeightY, b.center.z)
                                + dir * (halfExt + 0.85f)
                                + tangent * slide;

                    Vector2Int cell = WorldToCell(pos);
                    if (!grid.InBounds(cell)) continue;

                    CellType ct = grid.Get(cell);
                    if (ct == CellType.Road) roadCandidates.Add((pos, dir));
                    else if (ct == CellType.Empty) emptyCandidates.Add((pos, dir));
                }
            }

            ShuffleList(roadCandidates, rng);
            ShuffleList(emptyCandidates, rng);
            roadCandidates.AddRange(emptyCandidates);

            for (int i = 0; i < roadCandidates.Count; i++)
            {
                (Vector3 candidatePos, Vector3 outwardDir) = roadCandidates[i];

                Vector3 snapped = candidatePos;
                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
                {
                    snapped = navHit.position;
                }
                else
                {
                    continue;
                }

                if (!IsLootSpotClear(snapped, buildingBounds)) continue;

                snapped.y = roadHeightY + rule.yOffset;
                Quaternion rot = rule.alignToBuilding
                    ? Quaternion.LookRotation(outwardDir, Vector3.up)
                    : Quaternion.Euler(0f, rng.Range(0, 360), 0f);

                GameObject spawned = Instantiate(rule.prefab, snapped, rot, transform);
                if (spawned.GetComponentInChildren<LootContainer>() != null)
                {
                    spawned.tag = "Interactable";
                    LootSpawnPoints.Add(snapped);
                }
                return;
            }
        }

        private bool IsLootSpotClear(Vector3 worldPos, List<Bounds> buildingBounds)
        {
            if (SafeRefugeCenter != Vector3.zero)
            {
                if (Mathf.Abs(worldPos.x - SafeRefugeCenter.x) < 4.9f &&
                    Mathf.Abs(worldPos.z - SafeRefugeCenter.z) < 4.9f)
                {
                    return false;
                }
                if (Vector3.Distance(worldPos, SafeRefugeEntrancePoint) < 3.0f)
                {
                    return false;
                }
            }

            for (int i = 0; i < buildingBounds.Count; i++)
            {
                Bounds bb = buildingBounds[i];
                if (worldPos.x >= bb.min.x - 0.35f && worldPos.x <= bb.max.x + 0.35f &&
                    worldPos.z >= bb.min.z - 0.35f && worldPos.z <= bb.max.z + 0.35f)
                {
                    return false;
                }
            }

            for (int i = 0; i < LootSpawnPoints.Count; i++)
            {
                if (Vector3.Distance(worldPos, LootSpawnPoints[i]) < 2.2f) return false;
            }

            Collider[] hits = Physics.OverlapSphere(worldPos + Vector3.up * 0.65f, 0.72f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null) continue;
                string n = col.gameObject.name;
                if (n.Contains("Road") || n.Contains("Pavement") || n.Contains("Floor") || n.Contains("Ground") || n.Contains("Pato"))
                {
                    continue;
                }
                if (col.GetComponentInParent<BuildingData>() != null ||
                    col.GetComponentInParent<CitySocket>() != null ||
                    col.GetComponentInParent<SafeRefugeZone>() != null ||
                    col.bounds.size.y > 0.6f)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2Int WorldToCell(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x / cellSize), Mathf.FloorToInt(worldPos.z / cellSize));
        }

        private static void ShuffleList<T>(List<T> list, CityRandom rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int r = rng.Range(i, list.Count);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }

        private void ConfigureCityCollidersAndBakeNavMesh()
        {
            BuildingData[] buildings = GetComponentsInChildren<BuildingData>(true);
            for (int i = 0; i < buildings.Length; i++)
            {
                BuildingData b = buildings[i];
                if (b == null || b.GetComponentInParent<SafeRefugeZone>() != null) continue;
                GameObject bObj = b.gameObject;

                BoxCollider box = bObj.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = bObj.AddComponent<BoxCollider>();
                    Bounds enc = ComputeBuildingWorldBounds(bObj);
                    box.center = bObj.transform.InverseTransformPoint(enc.center);
                    Vector3 worldSize = enc.size;
                    Vector3 lossy = bObj.transform.lossyScale;
                    box.size = new Vector3(
                        lossy.x > 0.001f ? worldSize.x / lossy.x : worldSize.x,
                        lossy.y > 0.001f ? worldSize.y / lossy.y : worldSize.y,
                        lossy.z > 0.001f ? worldSize.z / lossy.z : worldSize.z
                    );
                }

                box.enabled = true;
                box.isTrigger = false;

                NavMeshObstacle obstacle = bObj.GetComponent<NavMeshObstacle>();
                if (obstacle == null) obstacle = bObj.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = box.center;
                obstacle.size = box.size;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.carvingMoveThreshold = 0.1f;
                obstacle.carvingTimeToStationary = 0.1f;
            }

            // Desactivar temporalmente MeshColliders cuyas mallas no tengan Read/Write activado (Pavement/Road) durante BuildNavMesh:
            // CityGroundSafetySlab + BoxColliders de edificios cubren toda la geometría caminable y esto elimina +500 stacktraces al arrancar!
            MeshCollider[] allMeshCols = GetComponentsInChildren<MeshCollider>(false);
            var tempDisabled = new List<MeshCollider>(allMeshCols.Length);
            for (int i = 0; i < allMeshCols.Length; i++)
            {
                MeshCollider mc = allMeshCols[i];
                if (mc != null && mc.enabled && mc.sharedMesh != null && !mc.sharedMesh.isReadable)
                {
                    mc.enabled = false;
                    tempDisabled.Add(mc);
                }
            }

            Physics.SyncTransforms();

            NavMeshSurface navSurface = GetComponent<NavMeshSurface>();
            if (navSurface == null) navSurface = gameObject.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();

            for (int i = 0; i < tempDisabled.Count; i++)
            {
                if (tempDisabled[i] != null) tempDisabled[i].enabled = true;
            }
            Physics.SyncTransforms();
        }

        private void AnalyzeRoadTopology(CityGrid grid, RoadNetwork network)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Length; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!grid.IsRoad(cell)) continue;

                    Vector3 worldPos = CellToWorldCenter(cell);
                    int neighbors = grid.RoadNeighborCount(cell);
                    if (neighbors == 1) DeadEndPoints.Add(worldPos);
                    else if (neighbors >= 3) IntersectionPoints.Add(worldPos);
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
                    if (count > 0) LoopCenterPoints.Add(center / count);
                }
            }
        }

        private void PlacePlayerAndCraftTable(CityGrid grid, bool hasRefuge)
        {
            if (hamsterTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) hamsterTransform = hc.transform;
            }

            if (hasRefuge && safeRefugeInstance != null)
            {
                CraftingTable refugeTable = safeRefugeInstance.GetComponentInChildren<CraftingTable>(true);
                if (refugeTable != null)
                {
                    CraftTableSpawnPoint = refugeTable.transform.position;
                }
                else if (craftTablePrefab != null)
                {
                    Vector3 tPos = SafeRefugeCenter + safeRefugeInstance.transform.TransformDirection(new Vector3(-2.3f, 0.04f, 1.5f));
                    GameObject tObj = Instantiate(craftTablePrefab, tPos, Quaternion.identity, safeRefugeInstance.transform);
                    CraftTableSpawnPoint = tObj.transform.position;
                }

                // Desactivar cualquier mesa duplicada que hubiera quedado suelta fuera del refugio
                CraftingTable[] allTables = FindObjectsByType<CraftingTable>(FindObjectsSortMode.None);
                for (int i = 0; i < allTables.Length; i++)
                {
                    if (allTables[i] != null && allTables[i] != refugeTable && !allTables[i].transform.IsChildOf(safeRefugeInstance.transform))
                    {
                        allTables[i].gameObject.SetActive(false);
                    }
                }

                PlaceHamsterAt(PlayerSpawnPoint, Quaternion.LookRotation(safeRefugeFacingDir, Vector3.up));
                return;
            }

            // Respaldo si no hay safeRefugePrefab asignado
            Vector2Int bestCell = new Vector2Int(grid.Width / 2, grid.Length / 2);
            float bestDistSq = float.MaxValue;
            Vector2 mapCenter = new Vector2(grid.Width * 0.5f, grid.Length * 0.5f);

            for (int x = 1; x < grid.Width - 1; x++)
            {
                for (int y = 1; y < grid.Length - 1; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (!grid.IsRoad(c)) continue;
                    float dSq = ((Vector2)c - mapCenter).sqrMagnitude;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        bestCell = c;
                    }
                }
            }

            Vector3 rawSpawn = CellToWorldCenter(bestCell);
            PlayerSpawnPoint = NavMesh.SamplePosition(rawSpawn, out NavMeshHit navHit, 5f, NavMesh.AllAreas)
                ? navHit.position + Vector3.up * 0.05f
                : rawSpawn + Vector3.up * 0.1f;

            Vector3 tableTarget = PlayerSpawnPoint + new Vector3(3.0f, 0f, 2.2f);
            CraftTableSpawnPoint = NavMesh.SamplePosition(tableTarget, out NavMeshHit tableHit, 4f, NavMesh.AllAreas)
                ? tableHit.position
                : tableTarget;

            PlaceHamsterAt(PlayerSpawnPoint, Quaternion.LookRotation(Vector3.forward));

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
        }

        private void PlaceHamsterAt(Vector3 spawnPos, Quaternion rotation)
        {
            if (hamsterTransform == null) return;
            Rigidbody rb = hamsterTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = spawnPos;
                rb.rotation = rotation;
            }
            hamsterTransform.position = spawnPos;
            hamsterTransform.rotation = rotation;
        }

        private void SpawnStarterLootNearBase(CityRandom rng, CityGrid grid, List<Bounds> buildingBounds)
        {
            GameObject prefabToUse = lootPrefab;
            if (prefabToUse == null && socketSpawns != null)
            {
                for (int i = 0; i < socketSpawns.Count; i++)
                {
                    if (socketSpawns[i] != null && socketSpawns[i].prefab != null &&
                        socketSpawns[i].prefab.GetComponentInChildren<LootContainer>() != null)
                    {
                        prefabToUse = socketSpawns[i].prefab;
                        break;
                    }
                }
            }
            if (prefabToUse == null) return;

            Vector3 forward = safeRefugeFacingDir.sqrMagnitude > 0.1f ? safeRefugeFacingDir.normalized : Vector3.back;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 anchorBase = SafeRefugeCenter != Vector3.zero ? SafeRefugeEntrancePoint : PlayerSpawnPoint;

            Vector3[] candidateOffsets =
            {
                right * 3.4f - forward * 0.4f,
                -right * 3.4f - forward * 0.4f,
                right * 2.8f + forward * 3.2f,
                -right * 2.8f + forward * 3.2f,
                forward * 4.2f + right * 1.8f,
                forward * 4.2f - right * 1.8f
            };
            ItemType[][] starterPacks =
            {
                new[] { ItemType.Metal, ItemType.Madera, ItemType.Desperdicios },
                new[] { ItemType.Metal, ItemType.Goma, ItemType.Semillas },
                new[] { ItemType.Madera, ItemType.Goma, ItemType.Semillas }
            };

            int spawnedCount = 0;
            for (int i = 0; i < candidateOffsets.Length && spawnedCount < starterPacks.Length; i++)
            {
                Vector3 pos = anchorBase + candidateOffsets[i];
                pos.y = roadHeightY;
                if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 3.5f, NavMesh.AllAreas)) continue;
                Vector3 snapped = new Vector3(hit.position.x, roadHeightY, hit.position.z);
                if (!IsLootSpotClear(snapped, buildingBounds)) continue;

                GameObject obj = Instantiate(prefabToUse, snapped, Quaternion.Euler(0f, rng.Range(0, 360), 0f), transform);
                obj.name = $"StarterLoot_{spawnedCount + 1}";
                obj.tag = "Interactable";
                LootContainer lc = obj.GetComponent<LootContainer>();
                if (lc == null) lc = obj.AddComponent<LootContainer>();
                lc.itemsGarantizados = new List<ItemType>(starterPacks[spawnedCount]);
                lc.ConfigurarEfectoResplandor();
                LootSpawnPoints.Add(snapped);
                spawnedCount++;
            }
        }

        private void SpawnCityEnemies(CityRandom rng)
        {
            CityEnemyPopulationSpawner popSpawner = GetComponent<CityEnemyPopulationSpawner>();
            if (popSpawner == null) popSpawner = FindAnyObjectByType<CityEnemyPopulationSpawner>();
            if (popSpawner == null)
            {
                popSpawner = gameObject.AddComponent<CityEnemyPopulationSpawner>();
            }
            popSpawner.SpawnEnemies(this, rng);
        }

        private void SnapAllAgentsToNavMesh()
        {
            NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            for (int i = 0; i < agents.Length; i++)
            {
                NavMeshAgent ag = agents[i];
                if (ag == null || !ag.enabled) continue;
                Vector3 pos = ag.transform.position;
                pos.y = roadHeightY;
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    ag.Warp(hit.position);
                }
            }
        }

        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            return new Vector3(cell.x * cellSize + cellSize * 0.5f, roadHeightY, cell.y * cellSize + cellSize * 0.5f);
        }

        [ContextMenu("Limpiar Ciudad")]
        public void ClearCity()
        {
            LootSpawnPoints.Clear();
            EnemySpawnPoints.Clear();
            IntersectionPoints.Clear();
            DeadEndPoints.Clear();
            LoopCenterPoints.Clear();
            SafeRefugeCenter = Vector3.zero;
            SafeRefugeEntrancePoint = Vector3.zero;
            safeRefugeInstance = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == hamsterTransform) continue;
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            CityLootSpawner lootSpawner = FindAnyObjectByType<CityLootSpawner>();
            if (lootSpawner != null) lootSpawner.ClearLoot();

            CityEnemyPopulationSpawner enemySpawner = FindAnyObjectByType<CityEnemyPopulationSpawner>();
            if (enemySpawner != null) enemySpawner.ClearEnemies();

            if (!Application.isPlaying)
            {
                NavMeshSurface navSurface = GetComponent<NavMeshSurface>();
                if (navSurface != null) navSurface.RemoveData();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (lastNetwork == null) return;

            Gizmos.color = Color.yellow;
            foreach (RoadLoop loop in lastNetwork.Loops.All)
            {
                foreach (int index in loop.CellIndices)
                {
                    int x = index % mapWidth;
                    int y = index / mapWidth;
                    Vector3 center = new Vector3(x * cellSize + cellSize / 2f, roadHeightY + 0.5f, y * cellSize + cellSize / 2f);
                    Gizmos.DrawWireCube(center, new Vector3(cellSize * 0.8f, 1f, cellSize * 0.8f));
                }
            }
        }
    }
}
