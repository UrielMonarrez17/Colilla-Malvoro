using System.Collections.Generic;
using UnityEngine;

// Orquestador: solo coordina las fases en orden. La logica vive en las demas clases.
public class CityGeneratorMathVersion : MonoBehaviour
{
    [Header("Motor Matematico (PRNG)")]
    public int seed = 12345;

    [Header("Dimensiones de la Ciudad")]
    public int mapWidth = 30;
    public int mapLength = 30;
    public float cellSize = 10f;

    [Header("Listas de Edificios")]
    public List<GameObject> specialBuildings = new List<GameObject>();
    public List<GameObject> normalBuildings = new List<GameObject>();

    [Header("Loot")]
    public GameObject lootPrefab;
    [Range(0f, 1f)] public float lootProbability = 0.5f; // 5% de probabilidad por celda libre
    
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

    private RoadNetwork lastNetwork;

    void Start()
    {
        if (transform.childCount == 0)
            GenerateCity();
    }

    public void GenerateCity()
    {
        ClearCity();

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
        
        // 3.5 Generar Loot usando los Sockets de los edificios
        if (lootPrefab != null)
        {
            CitySocket[] spawnedBuildings = GetComponentsInChildren<CitySocket>();
            
            foreach (CitySocket socket in spawnedBuildings)
            {
                if (socket.frontSocket == null) continue;

                // Evaluamos la probabilidad primero
                if (!rng.Chance(lootProbability)) continue;

                float z = socket.frontSocket.localPosition.z;

                // Posiciones relativas: Este, Oeste y Sur
                Vector3[] localOffsets = {
                    new Vector3(z, 0, 0),   // Derecha (E)
                    new Vector3(-z, 0, 0),  // Izquierda (O)
                    new Vector3(0, 0, -z)   // Atrás (S)
                };

                // Desordenamos el arreglo para elegir caras al azar sin repetir (Fisher-Yates)
                for (int i = 0; i < localOffsets.Length; i++)
                {
                    int rand = rng.Range(i, localOffsets.Length);
                    (localOffsets[i], localOffsets[rand]) = (localOffsets[rand], localOffsets[i]);
                }

                // Buscamos la primera cara que esté vacía
                foreach (Vector3 offset in localOffsets)
                {
                    Vector3 worldPos = socket.transform.TransformPoint(offset);
                    worldPos.y = roadHeightY;

                    int gridX = Mathf.FloorToInt(worldPos.x / cellSize);
                    int gridY = Mathf.FloorToInt(worldPos.z / cellSize);
                    Vector2Int cell = new Vector2Int(gridX, gridY);

                    // Si la celda es válida y está vacía, instanciamos y pasamos al siguiente edificio
                    if (grid.InBounds(cell) && grid.Get(cell) == CellType.Empty)
                    {
                        Instantiate(lootPrefab, worldPos, Quaternion.identity, transform);
                        
                        // Opcional pero recomendado: marcar la celda para que otro edificio cercano no ponga su loot aquí mismo
                        // grid.Set(cell, CellType.Special); 
                        
                        break; // Ya colocamos el loot de este edificio, no revisamos más caras
                    }
                }
            }
        }
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
        lastNetwork = network;
    }

    // Destruye TODOS los hijos del objeto antes de regenerar
    public void ClearCity()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    // Debug: dibuja en amarillo las celdas de cada rotonda (con el objeto seleccionado)
    void OnDrawGizmosSelected()
    {
        if (lastNetwork == null) return;

        Gizmos.color = Color.yellow;
        foreach (RoadLoop loop in lastNetwork.Loops.All)
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
    }
}
