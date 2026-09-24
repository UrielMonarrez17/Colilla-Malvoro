using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Dimensiones de la Ciudad")]
    [Tooltip("Número de columnas de la rejilla")]
    public int mapWidth = 20;

    [Tooltip("Número de filas de la rejilla")]
    public int mapLength = 20;

    [Tooltip("Distancia real en metros/unidades de Unity entre cada celda")]
    public float cellSize = 5f;

    [Header("Listas de Edificios")]
    [Tooltip("Lista de edificios de historia. Se 'desgastará' al colocar cada uno.")]
    public List<GameObject> specialBuildings = new List<GameObject>();

    [Tooltip("Lista de edificios decorativos. Se pueden repetir libremente.")]
    public List<GameObject> normalBuildings = new List<GameObject>();

    [Header("Prefabs de Calles / Caminos")]
    public GameObject roadStraight;
    public GameObject roadCorner;
    public GameObject shapeTIntersectionConnector; // Intersección en T
    public GameObject shapeXIntersectionConnector; // Intersección en Cruz / X
    public GameObject roadDeadEnd;                  // Callejón / Fin de vía

    [Header("Reglas de Distribución")]
    [Tooltip("Distancia mínima en celdas entre edificios especiales para evitar que queden adyacentes")]
    public int minCellDistanceBetweenSpecials = 4;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de intentar colocar un edificio normal en un espacio vacío")]
    public float normalBuildingDensity = 0.6f;

    // Matriz del Mapa: 0 = Vacío, 1 = Especial, 2 = Normal, 3 = Camino
    private int[,] grid;
    private List<Vector2Int> placedSpecialPositions = new List<Vector2Int>();

    void Start()
    {
        grid = new int[mapWidth, mapLength];
        GenerateCity();
    }

    public void GenerateCity()
    {
        ClearGrid();
        PlaceSpecialBuildings();
        PlaceNormalBuildings();
        GenerateRoads();
    }

    void ClearGrid()
    {
        grid = new int[mapWidth, mapLength];
        placedSpecialPositions.Clear();
    }

    // ===================================================================================
    // 1. COLOCAR EDIFICIOS ESPECIALES (Lista desgastable)
    // ===================================================================================
    void PlaceSpecialBuildings()
    {
        // Copiamos la lista para no alterar la lista original expuesta en el Inspector
        List<GameObject> availableSpecials = new List<GameObject>(specialBuildings);

        while (availableSpecials.Count > 0)
        {
            // Tomamos el primer edificio y lo Removemos de la lista ("Se desgasta/consume")
            GameObject specialPrefab = availableSpecials[0];
            availableSpecials.RemoveAt(0);

            BuildingData bData = GetBuildingData(specialPrefab);
            bool placed = false;
            int maxAttempts = 150;
            int attempt = 0;

            while (!placed && attempt < maxAttempts)
            {
                attempt++;
                int randomX = Random.Range(0, mapWidth - bData.gridWidth + 1);
                int randomY = Random.Range(0, mapLength - bData.gridLength + 1);

                // Comprobamos disponibilidad de celdas y distancia mnima a otros especiales
                if (CanPlaceBuilding(randomX, randomY, bData.gridWidth, bData.gridLength) &&
                    IsFarFromOtherSpecials(randomX, randomY))
                {
                    Vector3 spawnPos = bData.GetCalculatedWorldPosition(randomX, randomY, cellSize);
                    Instantiate(specialPrefab, spawnPos, Quaternion.identity, transform);

                    MarkGridCells(randomX, randomY, bData.gridWidth, bData.gridLength, 1);
                    placedSpecialPositions.Add(new Vector2Int(randomX, randomY));
                    placed = true;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"No se encontró un espacio espaciado para el edificio especial: {specialPrefab.name}");
            }
        }
    }

    // ===================================================================================
    // 2. COLOCAR EDIFICIOS NORMALES (Reutilizables)
    // ===================================================================================
    void PlaceNormalBuildings()
    {
        if (normalBuildings.Count == 0) return;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 0 && Random.value < normalBuildingDensity)
                {
                    // Seleccionamos un edificio al azar (NO se destruye ni remueve de la lista)
                    GameObject normalPrefab = normalBuildings[Random.Range(0, normalBuildings.Count)];
                    BuildingData bData = GetBuildingData(normalPrefab);

                    if (CanPlaceBuilding(x, y, bData.gridWidth, bData.gridLength))
                    {
                        Vector3 spawnPos = bData.GetCalculatedWorldPosition(x, y, cellSize);
                        Instantiate(normalPrefab, spawnPos, Quaternion.identity, transform);

                        MarkGridCells(x, y, bData.gridWidth, bData.gridLength, 2);
                    }
                }
            }
        }
    }

    // ===================================================================================
    // 3. GENERAR Y CONECTAR CAMINOS (Bitmasking para T, X, Rectas, Esquinas)
    // ===================================================================================
    void GenerateRoads()
    {
        // 1. Todo lo que quedó libre (0) se marca como Camino (3)
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 0)
                {
                    grid[x, y] = 3;
                }
            }
        }

        // 2. Instanciamos las piezas de calle correctas calculando la máscara de vecinos
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 3)
                {
                    SpawnRoadTile(x, y);
                }
            }
        }
    }

    void SpawnRoadTile(int x, int y)
    {
        int mask = CalculateNeighborMask(x, y);
        GameObject selectedRoadPrefab = roadStraight;
        float yRotation = 0f;

        // Máscara de Bits: 1 = Norte, 2 = Este, 4 = Sur, 8 = Oeste
        switch (mask)
        {
            // --- Callejones sin salida / Conexión única ---
            case 1: selectedRoadPrefab = roadDeadEnd; yRotation = 0f; break;   // N
            case 2: selectedRoadPrefab = roadDeadEnd; yRotation = 90f; break;  // E
            case 4: selectedRoadPrefab = roadDeadEnd; yRotation = 180f; break; // S
            case 8: selectedRoadPrefab = roadDeadEnd; yRotation = 270f; break; // W

            // --- Líneas Rectas ---
            case 5:  // N + S
                selectedRoadPrefab = roadStraight; yRotation = 0f; break;
            case 10: // E + W
                selectedRoadPrefab = roadStraight; yRotation = 90f; break;

            // --- Esquinas ---
            case 3:  selectedRoadPrefab = roadCorner; yRotation = 0f; break;   // N + E
            case 6:  selectedRoadPrefab = roadCorner; yRotation = 90f; break;  // E + S
            case 12: selectedRoadPrefab = roadCorner; yRotation = 180f; break; // S + W
            case 9:  selectedRoadPrefab = roadCorner; yRotation = 270f; break; // W + N

            // --- Intersecciones en T ---
            case 7:  selectedRoadPrefab = shapeTIntersectionConnector; yRotation = 0f; break;   // N + E + S
            case 14: selectedRoadPrefab = shapeTIntersectionConnector; yRotation = 90f; break;  // E + S + W
            case 13: selectedRoadPrefab = shapeTIntersectionConnector; yRotation = 180f; break; // S + W + N
            case 11: selectedRoadPrefab = shapeTIntersectionConnector; yRotation = 270f; break; // W + N + E

            // --- Intersección en X / Cruz ---
            case 15: // N + E + S + W
                selectedRoadPrefab = shapeXIntersectionConnector; yRotation = 0f; break;

            default:
                selectedRoadPrefab = roadStraight; yRotation = 0f; break;
        }

        // Posición centrada en la celda del camino (1x1)
        Vector3 roadPos = new Vector3((x * cellSize) + (cellSize / 2f), 0f, (y * cellSize) + (cellSize / 2f));
        Instantiate(selectedRoadPrefab, roadPos, Quaternion.Euler(0f, yRotation, 0f), transform);
    }

    // ===================================================================================
    // FUNCIONES AUXILIARES DE DISTANCIA Y COMPROBACIÓN
    // ===================================================================================

    int CalculateNeighborMask(int x, int y)
    {
        int mask = 0;
        // Se conecta tanto a otros caminos (3) como a las entradas de edificios (1, 2)
        if (y + 1 < mapLength && grid[x, y + 1] != 0) mask += 1; // Norte
        if (x + 1 < mapWidth && grid[x + 1, y] != 0)  mask += 2; // Este
        if (y - 1 >= 0 && grid[x, y - 1] != 0)        mask += 4; // Sur
        if (x - 1 >= 0 && grid[x - 1, y] != 0)        mask += 8; // Oeste
        return mask;
    }

    bool IsFarFromOtherSpecials(int gridX, int gridY)
    {
        Vector2Int currentPos = new Vector2Int(gridX, gridY);
        foreach (Vector2Int pos in placedSpecialPositions)
        {
            float distance = Vector2Int.Distance(currentPos, pos);
            if (distance < minCellDistanceBetweenSpecials)
            {
                return false; // Está demasiado cerca de otro edificio especial
            }
        }
        return true;
    }

    bool CanPlaceBuilding(int startX, int startY, int width, int length)
    {
        if (startX + width > mapWidth || startY + length > mapLength) return false;

        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + length; y++)
            {
                if (grid[x, y] != 0) return false;
            }
        }
        return true;
    }

    void MarkGridCells(int startX, int startY, int width, int length, int type)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + length; y++)
            {
                grid[x, y] = type;
            }
        }
    }

    BuildingData GetBuildingData(GameObject prefab)
    {
        BuildingData bData = prefab.GetComponent<BuildingData>();
        if (bData == null)
        {
            bData = prefab.AddComponent<BuildingData>();
        }
        return bData;
    }
}