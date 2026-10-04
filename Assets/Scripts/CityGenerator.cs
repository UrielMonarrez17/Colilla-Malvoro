using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Generación Sembrada (Seed)")]
    public bool useSeed = true;
    public int seed = 12345;

    [Header("Dimensiones de la Ciudad")]
    public int mapWidth = 30;
    public int mapLength = 30;
    public float cellSize = 5f;

    [Header("Configuración de Calles (Manzanas)")]
    [Tooltip("Cada cuántas casillas se trazará una calle vehicular. Debe ser mayor al tamaño de tus edificios.")]
    public int blockSize = 6; 

    [Header("Listas de Edificios")]
    public List<GameObject> specialBuildings = new List<GameObject>();
    public List<GameObject> normalBuildings = new List<GameObject>();

    [Header("Prefabs de Caminos y Suelos")]
    public GameObject roadForward;
    public GameObject roadAngle;
    public GameObject roadT; 
    public GameObject roadX; 
    public GameObject roadDeadEnd;
    
    [Tooltip("Asset de pavimento/suelo peatonal para NPCs")]
    public GameObject pedestrianPavement;

    [Header("Ajustes de Altura y Distribución")]
    public float roadHeightY = 0f;
    public int minCellDistanceBetweenSpecials = 4;
    [Range(0f, 1f)]
    public float normalBuildingDensity = 0.6f;

    // 0 = Vacío, 1 = Especial, 2 = Normal, 3 = Calle, 4 = Pavimento Peatonal
    private int[,] grid;
    private List<Vector2Int> placedSpecialPositions = new List<Vector2Int>();

    void Start()
    {
        GenerateCity();
    }

    public void GenerateCity()
    {
        if (useSeed) Random.InitState(seed);

        grid = new int[mapWidth, mapLength];
        placedSpecialPositions.Clear();

        GenerateRoadNetwork();      // 1. Trazamos las calles (líneas exactas)
        PlaceSpecialBuildings();    // 2. Colocamos edificios de historia
        PlaceNormalBuildings();     // 3. Colocamos edificios normales
        FillRemainingWithPavement();// 4. Convertimos el sobrante en plazas peatonales
        InstantiateCity();          // 5. Instanciamos los modelos 3D
    }

    // ==========================================
    // 1. RED DE CALLES (MANZANAS)
    // ==========================================
    void GenerateRoadNetwork()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                // Crea una cuadrícula perfecta de calles de 1 celda de grosor
                if (x % blockSize == 0 || y % blockSize == 0)
                {
                    grid[x, y] = 3;
                }
            }
        }
    }

    // ==========================================
    // 2. Y 3. COLOCACIÓN DE EDIFICIOS Y PAVIMENTO
    // ==========================================
    void PlaceSpecialBuildings()
    {
        List<GameObject> availableSpecials = new List<GameObject>(specialBuildings);

        while (availableSpecials.Count > 0)
        {
            GameObject specialPrefab = availableSpecials[0];
            availableSpecials.RemoveAt(0);
            BuildingData bData = GetBuildingData(specialPrefab);

            bool placed = false;
            int maxAttempts = 200;
            
            for (int attempt = 0; attempt < maxAttempts && !placed; attempt++)
            {
                int randomX = Random.Range(0, mapWidth - bData.gridWidth + 1);
                int randomY = Random.Range(0, mapLength - bData.gridLength + 1);

                if (CanPlaceBuilding(randomX, randomY, bData.gridWidth, bData.gridLength) &&
                    IsFarFromOtherSpecials(randomX, randomY))
                {
                    MarkGridCellsAndPavement(randomX, randomY, bData.gridWidth, bData.gridLength, 1, specialPrefab);
                    placedSpecialPositions.Add(new Vector2Int(randomX, randomY));
                    placed = true;
                }
            }
        }
    }

    void PlaceNormalBuildings()
    {
        if (normalBuildings.Count == 0) return;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (Random.value < normalBuildingDensity)
                {
                    GameObject normalPrefab = normalBuildings[Random.Range(0, normalBuildings.Count)];
                    BuildingData bData = GetBuildingData(normalPrefab);

                    if (CanPlaceBuilding(x, y, bData.gridWidth, bData.gridLength))
                    {
                        MarkGridCellsAndPavement(x, y, bData.gridWidth, bData.gridLength, 2, normalPrefab);
                    }
                }
            }
        }
    }

    bool CanPlaceBuilding(int startX, int startY, int width, int length)
    {
        if (startX + width > mapWidth || startY + length > mapLength) return false;

        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + length; y++)
            {
                // REGLA CLAVE: Solo puede construirse si es Vacío (0) o Pavimento (4).
                // NUNCA sobre Calles (3) ni otros Edificios (1, 2).
                if (grid[x, y] == 1 || grid[x, y] == 2 || grid[x, y] == 3) return false;
            }
        }
        return true;
    }

    void MarkGridCellsAndPavement(int startX, int startY, int width, int length, int type, GameObject prefab)
    {
        // 1. Marca las casillas ocupadas por el edificio
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + length; y++)
            {
                grid[x, y] = type;
            }
        }

        // 2. Coloca pavimento (Banqueta) en los 4 bordes (Norte, Sur, Este, Oeste)
        for (int x = startX; x < startX + width; x++)
        {
            SetPavementIfEmpty(x, startY - 1);         // Sur
            SetPavementIfEmpty(x, startY + length);    // Norte
        }
        for (int y = startY; y < startY + length; y++)
        {
            SetPavementIfEmpty(startX - 1, y);         // Oeste
            SetPavementIfEmpty(startX + width, y);     // Este
        }

        // 3. Instancia el modelo 3D
        Vector3 spawnPos = prefab.GetComponent<BuildingData>().GetCalculatedWorldPosition(startX, startY, cellSize, prefab.transform.position.y);
        Instantiate(prefab, spawnPos, Quaternion.identity, transform);
    }

    void SetPavementIfEmpty(int x, int y)
    {
        if (x >= 0 && x < mapWidth && y >= 0 && y < mapLength)
        {
            // Solo pone banqueta si la celda está totalmente vacía
            if (grid[x, y] == 0) grid[x, y] = 4;
        }
    }

    // ==========================================
    // 4. RELLENO PEATONAL
    // ==========================================
    void FillRemainingWithPavement()
    {
        // Todo lo que quedó como "0" dentro de las manzanas se vuelve zona peatonal
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 0) grid[x, y] = 4;
            }
        }
    }

    // ==========================================
    // 5. INSTANCIACIÓN DE CALLES Y SUELOS
    // ==========================================
    void InstantiateCity()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 3) SpawnRoadTile(x, y);
                else if (grid[x, y] == 4 && pedestrianPavement != null)
                {
                    Vector3 pos = new Vector3((x * cellSize) + (cellSize / 2f), pedestrianPavement.transform.position.y + roadHeightY, (y * cellSize) + (cellSize / 2f));
                    Instantiate(pedestrianPavement, pos, Quaternion.identity, transform);
                }
            }
        }
    }

    void SpawnRoadTile(int x, int y)
    {
        int mask = CalculateNeighborMask(x, y);
        GameObject selectedRoadPrefab = roadForward;
        float yRotation = 0f;

        switch (mask)
        {
            case 1: selectedRoadPrefab = roadDeadEnd; yRotation = 0f; break;
            case 2: selectedRoadPrefab = roadDeadEnd; yRotation = 90f; break;
            case 4: selectedRoadPrefab = roadDeadEnd; yRotation = 180f; break;
            case 8: selectedRoadPrefab = roadDeadEnd; yRotation = 270f; break;
            case 5: selectedRoadPrefab = roadForward; yRotation = 0f; break;
            case 10: selectedRoadPrefab = roadForward; yRotation = 90f; break;
            case 3: selectedRoadPrefab = roadAngle; yRotation = 0f; break;
            case 6: selectedRoadPrefab = roadAngle; yRotation = 90f; break;
            case 12: selectedRoadPrefab = roadAngle; yRotation = 180f; break;
            case 9: selectedRoadPrefab = roadAngle; yRotation = 270f; break;
            case 7: selectedRoadPrefab = roadT; yRotation = 0f; break;
            case 14: selectedRoadPrefab = roadT; yRotation = 90f; break;
            case 13: selectedRoadPrefab = roadT; yRotation = 180f; break;
            case 11: selectedRoadPrefab = roadT; yRotation = 270f; break;
            case 15: selectedRoadPrefab = roadX; yRotation = 0f; break;
            default: selectedRoadPrefab = roadForward; yRotation = 0f; break;
        }

        Vector3 roadPos = new Vector3((x * cellSize) + (cellSize / 2f), selectedRoadPrefab.transform.position.y + roadHeightY, (y * cellSize) + (cellSize / 2f));
        float offset = selectedRoadPrefab.GetComponent<RoadData>()?.rotationOffset ?? 0f;
        Instantiate(selectedRoadPrefab, roadPos, selectedRoadPrefab.transform.rotation * Quaternion.Euler(0f, yRotation + offset, 0f), transform);
    }

    int CalculateNeighborMask(int x, int y)
    {
        int mask = 0;
        // CORRECCIÓN VITAL: Las calles SOLO detectan otras calles (3). Esto arregla los falsos cruces.
        if (y + 1 < mapLength && grid[x, y + 1] == 3) mask += 1; // Norte
        if (x + 1 < mapWidth && grid[x + 1, y] == 3)  mask += 2; // Este
        if (y - 1 >= 0 && grid[x, y - 1] == 3)        mask += 4; // Sur
        if (x - 1 >= 0 && grid[x - 1, y] == 3)        mask += 8; // Oeste
        return mask;
    }

    bool IsFarFromOtherSpecials(int gridX, int gridY)
    {
        Vector2Int currentPos = new Vector2Int(gridX, gridY);
        foreach (Vector2Int pos in placedSpecialPositions)
        {
            if (Vector2Int.Distance(currentPos, pos) < minCellDistanceBetweenSpecials) return false;
        }
        return true;
    }

    BuildingData GetBuildingData(GameObject prefab)
    {
        BuildingData bData = prefab.GetComponent<BuildingData>();
        return bData != null ? bData : prefab.AddComponent<BuildingData>();
    }
}