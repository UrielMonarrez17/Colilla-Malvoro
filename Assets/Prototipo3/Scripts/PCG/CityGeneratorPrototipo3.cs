using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Prototipo3
{
    /// <summary>
    /// PASO 2: Generador Procedural de Ciudad con "Anclas de Distancia Manhattan" y Brújula de Rumores Cardinales.
    /// Punto 0: Base de Kilo + Mesa de Fabricación + Puesto adyacente de Marco (Tejón Conserje).
    /// Anillo Cercano (3 a 5 celdas): Prefab_EscuelaBellavista.
    /// Anillo Medio (6 a 9 celdas, cuadrantes distintos): Prefab_BosqueCeniza y Prefab_HospitalSanJudas.
    /// Anillo Lejano (10+ celdas o esquina más alejada): Prefab_TorreAethelgard.
    /// </summary>
    public class CityGeneratorPrototipo3 : MonoBehaviour
    {
        public static CityGeneratorPrototipo3 Instance { get; private set; }

        [Header("Motor Matematico (PRNG)")]
        public int seed = 12345;

        [Header("Dimensiones de la Ciudad")]
        public int mapWidth = 40;
        public int mapLength = 40;
        public float cellSize = 10f;

        [Header("Anclas de Historia (PASO 2 - Distancia Manhattan)")]
        public GameObject prefabEscuelaBellavista;
        public GameObject prefabBosqueCeniza;
        public GameObject prefabHospitalSanJudas;
        public GameObject prefabTorreAethelgard;
        public GameObject prefabMarcoTejon;

        [Header("Listas de Edificios Adicionales")]
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

        // Celdas y Posiciones de Anclas Manhattan
        public Vector2Int BaseCell { get; private set; }
        public Vector2Int MarcoCell { get; private set; }
        public Vector2Int EscuelaCell { get; private set; }
        public Vector2Int BosqueCell { get; private set; }
        public Vector2Int HospitalCell { get; private set; }
        public Vector2Int TorreCell { get; private set; }

        public GameObject SpawnedEscuela { get; private set; }
        public GameObject SpawnedBosque { get; private set; }
        public GameObject SpawnedHospital { get; private set; }
        public GameObject SpawnedTorre { get; private set; }
        public GameObject SpawnedMarco { get; private set; }

        private readonly Dictionary<string, Vector3> landmarkPositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);

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

        private static int? pendingSeed = null;
        private const string PendingSeedPrefKey = "Feral_PendingSeed";

        public static void SetPendingSeed(int newSeed)
        {
            pendingSeed = newSeed;
            PlayerPrefs.SetInt(PendingSeedPrefKey, newSeed);
            PlayerPrefs.Save();
            Debug.Log($"[CityGeneratorPrototipo3] Semilla pendiente configurada: {newSeed}");
        }

        public static bool TryConsumePendingSeed(out int consumedSeed)
        {
            if (pendingSeed.HasValue)
            {
                consumedSeed = pendingSeed.Value;
                pendingSeed = null;
                if (PlayerPrefs.HasKey(PendingSeedPrefKey))
                {
                    PlayerPrefs.DeleteKey(PendingSeedPrefKey);
                    PlayerPrefs.Save();
                }
                return true;
            }

            if (PlayerPrefs.HasKey(PendingSeedPrefKey))
            {
                consumedSeed = PlayerPrefs.GetInt(PendingSeedPrefKey);
                PlayerPrefs.DeleteKey(PendingSeedPrefKey);
                PlayerPrefs.Save();
                return true;
            }

            consumedSeed = 0;
            return false;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void Start()
        {
            if (TryConsumePendingSeed(out int incomingSeed))
            {
                seed = incomingSeed;
                Debug.Log($"[CityGeneratorPrototipo3] Semilla recibida aplicada: {seed}");
            }
            GenerateCity();
        }

        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Regenera la ciudad en tiempo de ejecución (ej. al presionar [G]), horneando el NavMesh
        /// y conservando el progreso del StoryManager.
        /// </summary>
        public void RegenerateCityRuntime(int? customSeed = null)
        {
            seed = customSeed ?? UnityEngine.Random.Range(10000, 99999);
            Debug.Log($"[CityGeneratorPrototipo3] Regenerando ciudad procedural con semilla {seed}...");
            GenerateCity();
        }

        [ContextMenu("Generar Ciudad Completa en Editor")]
        public void GenerateCity()
        {
            ClearCity();

            mapWidth = Mathf.Max(26, mapWidth);
            mapLength = Mathf.Max(26, mapLength);

            LootSpawnPoints.Clear();
            EnemySpawnPoints.Clear();
            IntersectionPoints.Clear();
            DeadEndPoints.Clear();
            LoopCenterPoints.Clear();
            landmarkPositions.Clear();

            var rng = new CityRandom(new MathPRNG(seed));
            var grid = new CityGrid(mapWidth, mapLength);
            var network = new RoadNetwork(grid, settings);
            var spawner = new BuildingSpawner(transform, cellSize, grid, rng);

            // 1. Red de caminos principal
            new RoadNetworkBuilder(settings, rng, network).Build();

            // 2. Seleccionar Celda Base (Punto 0) en una posición que permita los 3 anillos Manhattan en todas direcciones
            SelectBaseAndMarcoCells(grid, network);

            // 3. Colocar los 4 Edificios Importantes por Anclas de Distancia Manhattan desde BaseCell
            PlaceStoryLandmarkAnchors(grid, network, spawner, rng);

            // 4. Especiales secundarios opcionales
            if (specialBuildings != null && specialBuildings.Count > 0)
            {
                new SpecialBuildingPlacer(settings, rng, network, spawner).PlaceAll(specialBuildings);
            }

            // 5. Relleno con edificios normales
            new NormalBuildingPlacer(settings, rng, grid, spawner).PlaceAll(normalBuildings);

            // 6. Instanciar calles y pavimento
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

            // 7. Configurar colliders/NavMeshObstacles y hornear NavMesh
            ConfigurarNavegacionYColisionesCiudad();

            // 8. Analizar topología vial
            AnalizarTopologiaVial(grid, network);

            // 9. Ubicar Refugio Inicial (Base de Kilo + Mesa de Fabricación) y a Marco en la calle adyacente
            UbicarRefugioInicialYMarco(grid);

            // 10. Poblar el interior/entorno de los 4 edificios clave (NPCs, Jefes Duales y Mecanismos)
            // Si StoryManagerPrototipo3 existe, este poblará los artefactos mediante OnCityGenerated -> BindGraphToWorld
            StoryWorldBinder binder = FindAnyObjectByType<StoryWorldBinder>();
            if (binder != null && StoryManagerPrototipo3.Instance == null)
            {
                binder.PopulateLandmarkActorsAndMechanisms(this);
            }

            // 11. Generar Loot usando Sockets de edificios y callejones
            GenerarLootEnSockets(grid, rng);

            // 12. Generar Enemigos Ferales multiespecie en loops, intersecciones y dead-ends
            GenerarEnemigosFerales(rng);

            // 13. Re-muestrear todos los NavMeshAgents sobre el NavMesh recién horneado
            SnapAllAgentsToNavMesh();

            Debug.Log(
                $"[CityGeneratorPrototipo3] Ciudad generada (Semilla {seed}). Anclas Manhattan desde Base {BaseCell}:\n" +
                $"• Marco (Adyacente): {MarcoCell} (dM = {ManhattanDistance(BaseCell, MarcoCell)})\n" +
                $"• Escuela Bellavista (Anillo Cercano 3-5): {EscuelaCell} (dM = {ManhattanDistance(BaseCell, EscuelaCell)})\n" +
                $"• Bosque Ceniza (Anillo Medio 6-9): {BosqueCell} (dM = {ManhattanDistance(BaseCell, BosqueCell)})\n" +
                $"• Hospital San Judas (Anillo Medio 6-9): {HospitalCell} (dM = {ManhattanDistance(BaseCell, HospitalCell)})\n" +
                $"• Torre Aethelgard (Anillo Lejano 10+): {TorreCell} (dM = {ManhattanDistance(BaseCell, TorreCell)})"
            );

            OnCityGenerated?.Invoke();
        }

        private void SelectBaseAndMarcoCells(CityGrid grid, RoadNetwork network)
        {
            // Buscamos una celda de carretera alrededor de (Width/3, Length/3) para tener espacio amplio en los 4 cuadrantes y para distancia 10+
            Vector2Int idealCenter = new Vector2Int(Mathf.Clamp(grid.Width / 3, 8, grid.Width - 12), Mathf.Clamp(grid.Length / 3, 8, grid.Length - 12));
            Vector2Int bestCell = new Vector2Int(-1, -1);
            float bestScore = float.MaxValue;

            for (int x = 6; x < grid.Width - 10; x++)
            {
                for (int y = 6; y < grid.Length - 10; y++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if (grid.IsRoad(p))
                    {
                        float d = Vector2Int.Distance(p, idealCenter);
                        if (d < bestScore)
                        {
                            bestScore = d;
                            bestCell = p;
                        }
                    }
                }
            }

            if (bestCell.x < 0)
            {
                // Si no había carretera cerca, buscar cualquier carretera o crearla en idealCenter
                for (int x = 2; x < grid.Width - 2; x++)
                {
                    for (int y = 2; y < grid.Length - 2; y++)
                    {
                        if (grid.IsRoad(x, y))
                        {
                            bestCell = new Vector2Int(x, y);
                            break;
                        }
                    }
                    if (bestCell.x >= 0) break;
                }
            }

            if (bestCell.x < 0)
            {
                bestCell = idealCenter;
                network.PlaceRoad(bestCell);
            }

            BaseCell = bestCell;

            // Buscar celda de calle adyacente (dM = 1 o 2) para Marco el Tejón Conserje
            MarcoCell = BaseCell + new Vector2Int(1, 0);
            for (int d = 0; d < 4; d++)
            {
                Vector2Int adj = BaseCell + CityGrid.Directions[d];
                if (grid.InBounds(adj) && grid.IsRoad(adj))
                {
                    MarcoCell = adj;
                    break;
                }
            }
            if (!grid.IsRoad(MarcoCell) && grid.InBounds(MarcoCell))
            {
                network.PlaceRoad(MarcoCell);
            }
        }

        private void PlaceStoryLandmarkAnchors(CityGrid grid, RoadNetwork network, BuildingSpawner spawner, CityRandom rng)
        {
            // Cargar prefabs desde Assets/Prototipo3/Prefabs/Story/ o Resources si aún no están asignados en el Inspector
            EnsureStoryBuildingPrefabsLoaded();

            // 1. Anillo Cercano (Distancia Manhattan 3 a 5): Escuela Secundaria Bellavista
            SpawnedEscuela = PlaceLandmarkInManhattanRing(
                "Escuela",
                prefabEscuelaBellavista,
                minManhattan: 3,
                maxManhattan: 5,
                preferredQuadrant: -1,
                forbiddenQuadrant: -1,
                preferFarthest: false,
                grid, network, rng,
                out Vector2Int escCell);
            EscuelaCell = escCell;

            // 2. Anillo Medio (Distancia Manhattan 6 a 9, Cuadrante A): Bosque Ceniza
            SpawnedBosque = PlaceLandmarkInManhattanRing(
                "Bosque",
                prefabBosqueCeniza,
                minManhattan: 6,
                maxManhattan: 9,
                preferredQuadrant: -1,
                forbiddenQuadrant: -1,
                preferFarthest: false,
                grid, network, rng,
                out Vector2Int bosCell);
            BosqueCell = bosCell;
            int bosqueQuadrant = GetQuadrantRelativeToBase(BosqueCell);

            // 3. Anillo Medio (Distancia Manhattan 6 a 9, Cuadrante Distinto al Bosque): Hospital General San Judas
            SpawnedHospital = PlaceLandmarkInManhattanRing(
                "Hospital",
                prefabHospitalSanJudas,
                minManhattan: 6,
                maxManhattan: 9,
                preferredQuadrant: (bosqueQuadrant + 2) % 4,
                forbiddenQuadrant: bosqueQuadrant,
                preferFarthest: false,
                grid, network, rng,
                out Vector2Int hospCell);
            HospitalCell = hospCell;

            // 4. Anillo Lejano (Distancia Manhattan 10+ o esquina más alejada): Torre Corporativa Aethelgard
            SpawnedTorre = PlaceLandmarkInManhattanRing(
                "Torre",
                prefabTorreAethelgard,
                minManhattan: 10,
                maxManhattan: 999,
                preferredQuadrant: -1,
                forbiddenQuadrant: -1,
                preferFarthest: true,
                grid, network, rng,
                out Vector2Int torreCell);
            TorreCell = torreCell;
        }

        private int GetQuadrantRelativeToBase(Vector2Int cell)
        {
            int dx = cell.x - BaseCell.x;
            int dy = cell.y - BaseCell.y;
            if (dx >= 0 && dy >= 0) return 0; // Noreste (+X, +Y)
            if (dx < 0 && dy >= 0) return 1;  // Noroeste (-X, +Y)
            if (dx < 0 && dy < 0) return 2;   // Suroeste (-X, -Y)
            return 3;                         // Sureste (+X, -Y)
        }

        private GameObject PlaceLandmarkInManhattanRing(
            string landmarkKey,
            GameObject prefab,
            int minManhattan,
            int maxManhattan,
            int preferredQuadrant,
            int forbiddenQuadrant,
            bool preferFarthest,
            CityGrid grid,
            RoadNetwork network,
            CityRandom rng,
            out Vector2Int chosenCell)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();

            for (int x = 2; x < grid.Width - 2; x++)
            {
                for (int y = 2; y < grid.Length - 2; y++)
                {
                    Vector2Int c = new Vector2Int(x, y);
                    if (c == BaseCell || c == MarcoCell || grid.Get(c) == CellType.SpecialBuilding) continue;

                    int dM = ManhattanDistance(BaseCell, c);
                    if (dM < minManhattan || dM > maxManhattan) continue;

                    int quad = GetQuadrantRelativeToBase(c);
                    if (forbiddenQuadrant >= 0 && quad == forbiddenQuadrant) continue;

                    candidates.Add(c);
                }
            }

            // Si por tamaño de mapa no hubiera en otro cuadrante, relajar restricción de cuadrante manteniendo el anillo Manhattan
            if (candidates.Count == 0)
            {
                for (int x = 1; x < grid.Width - 1; x++)
                {
                    for (int y = 1; y < grid.Length - 1; y++)
                    {
                        Vector2Int c = new Vector2Int(x, y);
                        int dM = ManhattanDistance(BaseCell, c);
                        if (dM >= minManhattan && dM <= maxManhattan && c != BaseCell && c != MarcoCell && grid.Get(c) != CellType.SpecialBuilding)
                        {
                            candidates.Add(c);
                        }
                    }
                }
            }

            // Ordenar candidatos priorizando celdas Empty junto a una calle (Road) y el cuadrante/distancia deseados
            candidates.Sort((a, b) =>
            {
                int scoreA = ScoreLandmarkCandidate(a, grid, preferredQuadrant, preferFarthest);
                int scoreB = ScoreLandmarkCandidate(b, grid, preferredQuadrant, preferFarthest);
                return scoreB.CompareTo(scoreA);
            });

            chosenCell = candidates.Count > 0 ? candidates[0] : (BaseCell + new Vector2Int(minManhattan, 0));

            // Marcar la celda como SpecialBuilding y garantizar que tenga una celda de carretera adyacente conectada
            grid.Set(chosenCell, CellType.SpecialBuilding);
            EnsureAdjacentRoadConnection(chosenCell, grid, network);

            Vector3 worldPos = CellToWorldCenter(chosenCell);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                instance.name = $"Landmark_{landmarkKey}_{prefab.name}";
            }
            else
            {
                instance = CreateProceduralLandmarkBuilding(landmarkKey, worldPos);
            }

            // Orientar hacia la calle adyacente
            for (int d = 0; d < 4; d++)
            {
                Vector2Int n = chosenCell + CityGrid.Directions[d];
                if (grid.IsRoad(n))
                {
                    Vector3 roadPos = CellToWorldCenter(n);
                    Vector3 look = roadPos - worldPos;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.01f)
                    {
                        instance.transform.rotation = Quaternion.LookRotation(look.normalized);
                    }
                    break;
                }
            }

            landmarkPositions[landmarkKey] = worldPos;
            return instance;
        }

        private int ScoreLandmarkCandidate(Vector2Int c, CityGrid grid, int preferredQuadrant, bool preferFarthest)
        {
            int score = 0;
            if (grid.Get(c) == CellType.Empty) score += 500;
            if (grid.RoadNeighborCount(c) > 0) score += 300;
            if (preferredQuadrant >= 0 && GetQuadrantRelativeToBase(c) == preferredQuadrant) score += 150;

            int dM = ManhattanDistance(BaseCell, c);
            if (preferFarthest)
            {
                score += Mathf.Min(dM, 28) * 10;
            }
            return score;
        }

        private void EnsureAdjacentRoadConnection(Vector2Int buildingCell, CityGrid grid, RoadNetwork network)
        {
            for (int d = 0; d < 4; d++)
            {
                Vector2Int n = buildingCell + CityGrid.Directions[d];
                if (grid.IsRoad(n)) return;
            }

            // Conectar con la carretera más cercana mediante un pasaje corto
            Vector2Int entrance = buildingCell + CityGrid.Directions[0];
            for (int d = 0; d < 4; d++)
            {
                Vector2Int cand = buildingCell + CityGrid.Directions[d];
                if (grid.InBounds(cand) && grid.Get(cand) == CellType.Empty)
                {
                    entrance = cand;
                    break;
                }
            }

            if (grid.InBounds(entrance))
            {
                if (!network.TryConnectToMain(entrance, 16))
                {
                    network.PlaceRoad(entrance);
                }
            }
        }

        private GameObject CreateProceduralLandmarkBuilding(string landmarkKey, Vector3 worldPos)
        {
            GameObject root = new GameObject($"Prefab_{landmarkKey}_Landmark");
            root.transform.SetParent(transform, false);
            root.transform.position = worldPos;

            BuildingData bData = root.AddComponent<BuildingData>();
            bData.gridWidth = 1;
            bData.gridLength = 1;

            CitySocket socket = root.AddComponent<CitySocket>();
            GameObject front = new GameObject("FrontSocket");
            front.transform.SetParent(root.transform, false);
            front.transform.localPosition = new Vector3(0f, 0f, 4.2f);
            socket.frontSocket = front.transform;

            // Estructura arquitectónica abierta (3 muros y patio frontal accesible para que Kilo entre caminando)
            Color wallColor = landmarkKey == "Escuela" ? new Color(0.75f, 0.52f, 0.28f)
                            : landmarkKey == "Bosque" ? new Color(0.22f, 0.45f, 0.26f)
                            : landmarkKey == "Hospital" ? new Color(0.82f, 0.88f, 0.92f)
                            : new Color(0.22f, 0.25f, 0.35f);

            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(sh) { color = wallColor };

            // Pared Trasera
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackStructure";
            backWall.transform.SetParent(root.transform, false);
            backWall.transform.localPosition = new Vector3(0f, 2.2f, -3.5f);
            backWall.transform.localScale = new Vector3(7.6f, 4.4f, 1.2f);
            backWall.GetComponent<Renderer>().material = mat;

            // Pared Izquierda
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWing";
            leftWall.transform.SetParent(root.transform, false);
            leftWall.transform.localPosition = new Vector3(-3.5f, 1.8f, 0f);
            leftWall.transform.localScale = new Vector3(1.0f, 3.6f, 7.0f);
            leftWall.GetComponent<Renderer>().material = mat;

            // Pared Derecha
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWing";
            rightWall.transform.SetParent(root.transform, false);
            rightWall.transform.localPosition = new Vector3(3.5f, 1.8f, 0f);
            rightWall.transform.localScale = new Vector3(1.0f, 3.6f, 7.0f);
            rightWall.GetComponent<Renderer>().material = mat;

            // Piso/Patio Caminable ("Pavement" en el nombre para que el NavMesh lo considere suelo transitable)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Pavement_Courtyard";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            floor.transform.localScale = new Vector3(8.8f, 0.1f, 8.8f);
            Material floorMat = new Material(sh) { color = wallColor * 0.65f };
            floor.GetComponent<Renderer>().material = floorMat;

            return root;
        }

        private void EnsureStoryBuildingPrefabsLoaded()
        {
#if UNITY_EDITOR
            if (prefabEscuelaBellavista == null)
                prefabEscuelaBellavista = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Story/Prefab_EscuelaBellavista.prefab");
            if (prefabBosqueCeniza == null)
                prefabBosqueCeniza = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Story/Prefab_BosqueCeniza.prefab");
            if (prefabHospitalSanJudas == null)
                prefabHospitalSanJudas = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Story/Prefab_HospitalSanJudas.prefab");
            if (prefabTorreAethelgard == null)
                prefabTorreAethelgard = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Story/Prefab_TorreAethelgard.prefab");
            if (prefabMarcoTejon == null)
            {
                prefabMarcoTejon = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Story/Boss_Marco_Tejon.prefab")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Story/NPC_Marco_Tejon.prefab");
            }
#endif
        }

        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            return new Vector3(cell.x * cellSize + cellSize * 0.5f, roadHeightY, cell.y * cellSize + cellSize * 0.5f);
        }

        public Vector3 GetLandmarkPosition(string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return PlayerSpawnPoint;
            if (landmarkPositions.TryGetValue(locationId, out Vector3 pos)) return pos;

            if (locationId.IndexOf("Escuela", StringComparison.OrdinalIgnoreCase) >= 0 && landmarkPositions.TryGetValue("Escuela", out pos)) return pos;
            if (locationId.IndexOf("Bosque", StringComparison.OrdinalIgnoreCase) >= 0 && landmarkPositions.TryGetValue("Bosque", out pos)) return pos;
            if (locationId.IndexOf("Hospital", StringComparison.OrdinalIgnoreCase) >= 0 && landmarkPositions.TryGetValue("Hospital", out pos)) return pos;
            if (locationId.IndexOf("Torre", StringComparison.OrdinalIgnoreCase) >= 0 && landmarkPositions.TryGetValue("Torre", out pos)) return pos;
            if (locationId.IndexOf("Marco", StringComparison.OrdinalIgnoreCase) >= 0 && landmarkPositions.TryGetValue("Marco", out pos)) return pos;
            return PlayerSpawnPoint;
        }

        /// <summary>
        /// PASO 2.5: Calcula en tiempo real si la Escuela, el Bosque, el Hospital o la Torre quedaron al
        /// Norte, Noreste, Este, Sureste, Sur, Suroeste, Oeste o Noroeste respecto al jugador.
        /// </summary>
        public string GetCardinalDirectionFromPlayer(string locationId)
        {
            Vector3 targetPos = GetLandmarkPosition(locationId);
            Transform playerRef = hamsterTransform;
            if (playerRef == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerRef = hc.transform;
            }

            Vector3 origin = playerRef != null ? playerRef.position : PlayerSpawnPoint;
            Vector3 delta = targetPos - origin;
            delta.y = 0f;

            if (delta.sqrMagnitude < 2.25f)
            {
                return "Aquí mismo";
            }

            // En Unity: +Z = Norte (0°), +X = Este (90°), -Z = Sur (180°), -X = Oeste (270°)
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            string[] cardinalNames =
            {
                "Norte",
                "Noreste",
                "Este",
                "Sureste",
                "Sur",
                "Suroeste",
                "Oeste",
                "Noroeste"
            };

            int idx = Mathf.RoundToInt(angle / 45f) % 8;
            return cardinalNames[idx];
        }


        private void ConfigurarNavegacionYColisionesCiudad()
        {
            BuildingData[] buildings = GetComponentsInChildren<BuildingData>(true);
            foreach (BuildingData b in buildings)
            {
                if (b == null) continue;
                GameObject bObj = b.gameObject;

                // Los 4 edificios clave tienen interiores/patios transitables con sus propios muros hijos
                if (bObj == SpawnedEscuela || bObj == SpawnedBosque || bObj == SpawnedHospital || bObj == SpawnedTorre ||
                    bObj.name.StartsWith("Landmark_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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

        private void AnalizarTopologiaVial(CityGrid grid, RoadNetwork network)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Length; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!grid.IsRoad(cell)) continue;

                    Vector3 worldPos = CellToWorldCenter(cell);
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

        private void UbicarRefugioInicialYMarco(CityGrid grid)
        {
            Vector3 rawSpawnPoint = CellToWorldCenter(BaseCell);
            if (NavMesh.SamplePosition(rawSpawnPoint, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                PlayerSpawnPoint = navHit.position + Vector3.up * 0.05f;
            }
            else
            {
                PlayerSpawnPoint = rawSpawnPoint + Vector3.up * 0.1f;
            }

            landmarkPositions["Base"] = PlayerSpawnPoint;

            CraftTableSpawnPoint = PlayerSpawnPoint + new Vector3(2.2f, 0f, 1.6f);
            if (NavMesh.SamplePosition(CraftTableSpawnPoint, out NavMeshHit tableHit, 3f, NavMesh.AllAreas))
            {
                CraftTableSpawnPoint = tableHit.position;
            }

            GameObject shelterMarker = new GameObject("SPAWN_HAMSTER_BASE_PUNTO_0");
            shelterMarker.transform.SetParent(transform, false);
            shelterMarker.transform.position = PlayerSpawnPoint;

            // Mover a Kilo a la Base
            if (hamsterTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null)
                {
                    hc.enabled = true;
                    hamsterTransform = hc.transform;
                }
            }
            else
            {
                HamsterController hc = hamsterTransform.GetComponent<HamsterController>();
                if (hc != null) hc.enabled = true;
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

            // Instanciar o posicionar Mesa de Fabricación en la Base
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
                GameObject tableObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableObj.name = "CraftTable_Prototipo3";
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

            // Registrar posición de Marco en la calle adyacente (Punto 0)
            Vector3 marcoWorldPos = CellToWorldCenter(MarcoCell);
            if (NavMesh.SamplePosition(marcoWorldPos, out NavMeshHit marcoNavHit, 5f, NavMesh.AllAreas))
            {
                marcoWorldPos = marcoNavHit.position;
            }
            landmarkPositions["Marco"] = marcoWorldPos;
        }

        private void GenerarLootEnSockets(CityGrid grid, CityRandom rng)
        {
            CityLootSpawner spawner = FindAnyObjectByType<CityLootSpawner>() ?? GetComponent<CityLootSpawner>();
            if (spawner == null)
            {
                GameObject popMgr = GameObject.Find("CityPopulationManager");
                if (popMgr == null) popMgr = new GameObject("CityPopulationManager");
                spawner = popMgr.AddComponent<CityLootSpawner>();
            }
            spawner.SpawnLoot(this, grid, rng);
        }

        public GameObject CreateLootContainerInstance(Transform parent, string containerName, Vector3 pos, List<ItemType> guaranteed)
        {
            CityLootSpawner spawner = FindAnyObjectByType<CityLootSpawner>() ?? GetComponent<CityLootSpawner>();
            if (spawner != null)
            {
                return spawner.CreateLootContainerInstance(parent, containerName, pos, guaranteed, lootPrefab);
            }
            return null;
        }

        private void GenerarEnemigosFerales(CityRandom rng)
        {
            CityEnemyPopulationSpawner spawner = FindAnyObjectByType<CityEnemyPopulationSpawner>() ?? GetComponent<CityEnemyPopulationSpawner>();
            if (spawner == null)
            {
                GameObject popMgr = GameObject.Find("CityPopulationManager");
                if (popMgr == null) popMgr = new GameObject("CityPopulationManager");
                spawner = popMgr.AddComponent<CityEnemyPopulationSpawner>();
            }
            spawner.SpawnEnemies(this, rng);
        }

        [ContextMenu("Limpiar Ciudad")]
        public void ClearCity()
        {
            LootSpawnPoints.Clear();
            EnemySpawnPoints.Clear();
            IntersectionPoints.Clear();
            DeadEndPoints.Clear();
            LoopCenterPoints.Clear();
            landmarkPositions.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == hamsterTransform) continue;
                if (Application.isPlaying)
                {
                    child.SetParent(null);
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            StoryWorldBinder binder = FindAnyObjectByType<StoryWorldBinder>();
            if (binder != null)
            {
                binder.LimpiarArtefactos();
            }

            CityLootSpawner lootSpawner = FindAnyObjectByType<CityLootSpawner>();
            if (lootSpawner != null)
            {
                lootSpawner.ClearLoot();
            }

            CityEnemyPopulationSpawner enemySpawner = FindAnyObjectByType<CityEnemyPopulationSpawner>();
            if (enemySpawner != null)
            {
                enemySpawner.ClearEnemies();
            }

            if (!Application.isPlaying)
            {
                NavMeshSurface navSurface = GetComponent<NavMeshSurface>();
                if (navSurface != null)
                {
                    navSurface.RemoveData();
                }
            }
        }
    }
}
