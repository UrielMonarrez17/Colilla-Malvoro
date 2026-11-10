using System.Collections.Generic;
using UnityEngine;

// Regla para colocar un objeto cualquiera junto a los edificios (usando sus sockets)
[System.Serializable]
public class SocketSpawnRule
{
    public string name = "Objeto";
    public GameObject prefab;
    [Range(0f, 1f)] public float probability = 0.5f; // probabilidad por edificio
    public float yOffset = 0f;                        // altura extra sobre roadHeightY
    public bool alignToBuilding = false;              // copiar la rotación del edificio
}

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
        // 3.5 Objetos junto a los edificios (loot, props, etc.)
        SpawnSocketObjects(rng);

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

    // Recorre cada regla y, por cada edificio, intenta colocar el objeto en una cara libre (E, O o S)
    // Recorre cada regla y, por cada edificio, coloca el objeto en una cara al azar (E, O o S)
    void SpawnSocketObjects(CityRandom rng)
    {
        if (socketSpawns.Count == 0) return;

        CitySocket[] sockets = GetComponentsInChildren<CitySocket>();

        foreach (SocketSpawnRule rule in socketSpawns)
        {
            if (rule.prefab == null) continue;

            foreach (CitySocket socket in sockets)
            {
                if (socket.frontSocket == null) continue;
                if (!rng.Chance(rule.probability)) continue;

                SpawnAtSocket(rule, socket, rng);
            }
        }
    }

    void SpawnAtSocket(SocketSpawnRule rule, CitySocket socket, CityRandom rng)
    {
        float z = socket.frontSocket.localPosition.z;

        // Posiciones relativas: Este, Oeste y Sur
        Vector3[] localOffsets = {
            new Vector3(z, 0, 0),   // Derecha (E)
            new Vector3(-z, 0, 0),  // Izquierda (O)
            new Vector3(0, 0, -z)   // Atrás (S)
        };

        // Una cara al azar con el PRNG (el máximo es exclusivo)
        Vector3 offset = localOffsets[rng.Range(0, localOffsets.Length)];

        Vector3 worldPos = socket.transform.TransformPoint(offset);
        worldPos.y = roadHeightY + rule.yOffset;

        Quaternion rot = rule.alignToBuilding ? socket.transform.rotation : Quaternion.identity;
        Instantiate(rule.prefab, worldPos, rot, transform);
    }

    // Destruye TODOS los hijos del objeto antes de regenerar
    public void ClearCity()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
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