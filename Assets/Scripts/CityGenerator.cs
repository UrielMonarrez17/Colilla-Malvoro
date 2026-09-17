using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Configuración del Mapa")]
    public int mapWidth = 20;
    public int mapLength = 20;
    public float cellSize = 5f; // Tamaño real en Unity de cada "cuadro" de la malla

    [Header("Listas de Edificios")]
    public List<GameObject> specialBuildings; // Se irá desgastando (removiendo)
    public List<GameObject> normalBuildings;  // Se reutilizará infinitamente

    [Header("Conectores de Caminos (Calles)")]
    public GameObject roadStraight;
    public GameObject roadCorner;
    public GameObject shapeTIntersectionConnector; // Conector en forma de T
    public GameObject shapeXIntersectionConnector; // Conector en forma de X (Cruz)
    public GameObject roadDeadEnd; // Callejón sin salida

    [Header("Reglas de Generación")]
    public float minDistanceBetweenSpecials = 5f; // Distancia en celdas para que no estén juntos

    // Representación del mapa: 0 = Vacío, 1 = Especial, 2 = Normal, 3 = Camino
    private int[,] grid;
    private List<Vector2Int> specialBuildingPositions = new List<Vector2Int>();

    void Start()
    {
        grid = new int[mapWidth, mapLength];
        GenerateMap();
    }

    void GenerateMap()
    {
        PlaceSpecialBuildings();
        PlaceNormalBuildings();
        GenerateRoads();
    }

    // --- 1. COLOCAR EDIFICIOS ESPECIALES ---
    void PlaceSpecialBuildings()
    {
        // Copiamos la lista para poder "desgastarla" sin perder los datos originales en el Inspector
        List<GameObject> availableSpecials = new List<GameObject>(specialBuildings);

        // Mientras haya edificios especiales en la lista
        while (availableSpecials.Count > 0)
        {
            // Tomamos el primero y lo eliminamos de la lista ("se desgasta")
            GameObject buildingToPlace = availableSpecials[0];
            availableSpecials.RemoveAt(0);

            // Calculamos cuánto espacio físico ocupa en la malla
            Vector2Int size = GetBuildingGridSize(buildingToPlace);
            bool placed = false;
            int attempts = 0;

            // Intentamos encontrar un lugar válido (max 100 intentos para evitar bucles infinitos)
            while (!placed && attempts < 100)
            {
                int randomX = Random.Range(0, mapWidth - size.x);
                int randomY = Random.Range(0, mapLength - size.y);

                if (CanPlaceBuilding(randomX, randomY, size.x, size.y) && 
                    CheckDistanceToSpecials(randomX, randomY))
                {
                    // Lo colocamos y marcamos la malla
                    Instantiate(buildingToPlace, new Vector3(randomX * cellSize, 0, randomY * cellSize), Quaternion.identity, this.transform);
                    MarkGrid(randomX, randomY, size.x, size.y, 1); // 1 = Especial
                    specialBuildingPositions.Add(new Vector2Int(randomX, randomY));
                    placed = true;
                }
                attempts++;
            }
        }
    }

    // --- 2. COLOCAR EDIFICIOS NORMALES ---
    void PlaceNormalBuildings()
    {
        // Recorremos toda la malla buscando espacios libres
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                // Dejamos un margen probabilístico para que queden espacios libres para las calles
                if (grid[x, y] == 0 && Random.value > 0.4f) 
                {
                    // Tomamos un edificio normal al azar (NO se borra de la lista, se reutiliza)
                    GameObject normalPrefab = normalBuildings[Random.Range(0, normalBuildings.Count)];
                    Vector2Int size = GetBuildingGridSize(normalPrefab);

                    if (CanPlaceBuilding(x, y, size.x, size.y))
                    {
                        Instantiate(normalPrefab, new Vector3(x * cellSize, 0, y * cellSize), Quaternion.identity, this.transform);
                        MarkGrid(x, y, size.x, size.y, 2); // 2 = Normal
                    }
                }
            }
        }
    }

    // --- 3. CONECTAR CON CAMINOS ---
    void GenerateRoads()
    {
        // Todo lo que quedó vacío (0) será calle (3)
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 0)
                {
                    grid[x, y] = 3; // Marcamos como camino
                }
            }
        }

        // Instanciamos los caminos correctos leyendo sus vecinos
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapLength; y++)
            {
                if (grid[x, y] == 3)
                {
                    InstantiateRoad(x, y);
                }
            }
        }
    }

    void InstantiateRoad(int x, int y)
    {
        int mask = GetRoadMask(x, y);
        GameObject roadPrefab = roadStraight; // Por defecto
        float rotation = 0f;

        // Lógica de Bitmasking: 1=Norte, 2=Este, 4=Sur, 8=Oeste. Sumados dan combinaciones únicas.
        switch (mask)
        {
            // Rectas y callejones (simplificado)
            case 1: case 4: case 5: 
                roadPrefab = roadStraight; rotation = 90f; break; // Vertical
            case 2: case 8: case 10: 
                roadPrefab = roadStraight; rotation = 0f; break; // Horizontal
            
            // Intersecciones en T
            case 7: roadPrefab = shapeTIntersectionConnector; rotation = 0f; break; // N, E, S
            case 11: roadPrefab = shapeTIntersectionConnector; rotation = -90f; break; // N, E, W
            case 13: roadPrefab = shapeTIntersectionConnector; rotation = 180f; break; // N, S, W
            case 14: roadPrefab = shapeTIntersectionConnector; rotation = 90f; break; // E, S, W
            
            // Intersecciones en X
            case 15: roadPrefab = shapeXIntersectionConnector; break; // Todos los lados
        }

        GameObject road = Instantiate(roadPrefab, new Vector3(x * cellSize, 0, y * cellSize), Quaternion.Euler(0, rotation, 0), this.transform);
    }

    // ===================================================================================
    // FUNCIONES AUXILIARES (HELPERS) AÑADIDAS PARA QUE LA LÓGICA FUNCIONE
    // ===================================================================================

    // Función auxiliar 1: Convierte el tamaño real del modelo a celdas de la malla
    Vector2Int GetBuildingGridSize(GameObject prefab)
    {
        // Busca el tamaño del modelo 3D sin importar su escala base
        Renderer rend = prefab.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            int widthInCells = Mathf.CeilToInt(rend.bounds.size.x / cellSize);
            int lengthInCells = Mathf.CeilToInt(rend.bounds.size.z / cellSize);
            return new Vector2Int(Mathf.Max(1, widthInCells), Mathf.Max(1, lengthInCells));
        }
        return new Vector2Int(1, 1); // Tamaño mínimo por defecto
    }

    // Función auxiliar 2: Verifica que haya suficiente separación entre edificios especiales
    bool CheckDistanceToSpecials(int x, int y)
    {
        foreach (Vector2Int pos in specialBuildingPositions)
        {
            float dist = Vector2Int.Distance(new Vector2Int(x, y), pos);
            if (dist < minDistanceBetweenSpecials)
                return false; // Está demasiado cerca de otro edificio especial
        }
        return true;
    }

    // Función auxiliar 3: Máscara de bits para determinar qué vecinos (N, S, E, O) están ocupados
    int GetRoadMask(int x, int y)
    {
        int mask = 0;
        // Asumimos que los caminos conectan tanto con otras calles (3) como con las entradas de los edificios (1, 2)
        if (y + 1 < mapLength && grid[x, y + 1] != 0) mask += 1; // Norte
        if (x + 1 < mapWidth && grid[x + 1, y] != 0)  mask += 2; // Este
        if (y - 1 >= 0 && grid[x, y - 1] != 0)        mask += 4; // Sur
        if (x - 1 >= 0 && grid[x - 1, y] != 0)        mask += 8; // Oeste
        return mask;
    }

    // Función de rutina: Revisa si hay espacio en la malla para un edificio
    bool CanPlaceBuilding(int startX, int startY, int sizeX, int sizeY)
    {
        if (startX + sizeX > mapWidth || startY + sizeY > mapLength) return false;

        for (int x = startX; x < startX + sizeX; x++)
        {
            for (int y = startY; y < startY + sizeY; y++)
            {
                if (grid[x, y] != 0) return false; // Ya hay algo aquí
            }
        }
        return true;
    }

    // Función de rutina: Marca las celdas de la malla como ocupadas
    void MarkGrid(int startX, int startY, int sizeX, int sizeY, int type)
    {
        for (int x = startX; x < startX + sizeX; x++)
        {
            for (int y = startY; y < startY + sizeY; y++)
            {
                grid[x, y] = type;
            }
        }
    }
}