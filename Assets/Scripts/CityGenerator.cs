using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Dimensiones de la Ciudad")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float cellSize = 3f;

    [Header("Reglas de Generación")]
    public int importantHousesCount = 4;
    public float minDistanceBetweenKeyPoints = 5f;
    [Range(0f, 1f)]
    public float genericHouseDensity = 0.3f;

    // --- NUEVO: RANURAS PARA TUS PREFABS ---
    [Header("Prefabs de Edificios")]
    public GameObject prefabBaseHamster;
    public GameObject prefabCasaImportante;
    public GameObject prefabCasaGenerica;

    [Header("Prefabs de Calle (Deben medir cellSize x cellSize)")]
    [Tooltip("Prefab para calle recta (el modelo debe estar alineado N-S por defecto)")]
    public GameObject prefabCalleRecta;
    [Tooltip("Prefab para esquina (el modelo debe girar de N a E por defecto)")]
    public GameObject prefabCalleEsquina;
    [Tooltip("Prefab para cruce en T (el modelo debe tener salidas N, S, E por defecto)")]
    public GameObject prefabCalleT;
    [Tooltip("Prefab para cruce de 4 caminos")]
    public GameObject prefabCalleCruce;

    // 0 = Vacío, 1 = Hámster, 2 = Casa Importante, 3 = Casa Generica, 4 = Calle
    private int[,] grid;
    private List<Vector2Int> keyLocations = new List<Vector2Int>();

    void Start()
    {
        GenerateCity();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            foreach (Transform child in transform) { Destroy(child.gameObject); }
            GenerateCity();
        }
    }

    private void GenerateCity()
    {
        grid = new int[gridWidth, gridHeight];
        keyLocations.Clear();

        PlaceHamsterHouse();
        PlaceImportantHouses();
        GenerateStreets(); // Mantenemos Manhattan Pathing
        PlaceGenericHouses();
        
        // --- NUEVO: INSTANCIAR PREFABS ---
        InstantiateCity();
    }

    // --- MÉTODOS DE LÓGICA (SE MANTIENEN IGUAL) ---
    private void PlaceHamsterHouse()
    {
        int x = Random.Range(0, gridWidth);
        int y = Random.Range(0, gridHeight);
        grid[x, y] = 1;
        keyLocations.Add(new Vector2Int(x, y));
    }

    private void PlaceImportantHouses()
    {
        int housesPlaced = 0;
        int attempts = 0;
        while (housesPlaced < importantHousesCount && attempts < 1000)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            Vector2Int potentialPos = new Vector2Int(x, y);
            if (grid[x, y] == 0 && IsFarEnough(potentialPos))
            {
                grid[x, y] = 2;
                keyLocations.Add(potentialPos);
                housesPlaced++;
            }
            attempts++;
        }
    }

    private bool IsFarEnough(Vector2Int pos)
    {
        foreach (Vector2Int keyPos in keyLocations)
        {
            if (Vector2Int.Distance(pos, keyPos) < minDistanceBetweenKeyPoints) return false;
        }
        return true;
    }

    private void GenerateStreets()
    {
        for (int i = 0; i < keyLocations.Count; i++)
        {
            Vector2Int start = keyLocations[i];
            Vector2Int end = keyLocations[(i + 1) % keyLocations.Count]; 
            int currentX = start.x;
            int currentY = start.y;
            while (currentX != end.x)
            {
                currentX += (int)Mathf.Sign(end.x - currentX);
                if (grid[currentX, currentY] == 0) grid[currentX, currentY] = 4;
            }
            while (currentY != end.y)
            {
                currentY += (int)Mathf.Sign(end.y - currentY);
                if (grid[currentX, currentY] == 0) grid[currentX, currentY] = 4;
            }
        }
    }

    private void PlaceGenericHouses()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == 0 && Random.value < genericHouseDensity) grid[x, y] = 3;
            }
        }
    }

    // --- NUEVO: MÉTODOS DE VISUALIZACIÓN ---

    private void InstantiateCity()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = new Vector3(x * cellSize, 0f, y * cellSize);
                int cellType = grid[x, y];

                if (cellType == 0) continue; // Vacío

                // 1. Instanciar Edificios (simple)
                if (cellType == 1 && prefabBaseHamster != null) Instantiate(prefabBaseHamster, worldPos, Quaternion.identity, this.transform);
                else if (cellType == 2 && prefabCasaImportante != null) Instantiate(prefabCasaImportante, worldPos, Quaternion.identity, this.transform);
                else if (cellType == 3 && prefabCasaGenerica != null) Instantiate(prefabCasaGenerica, worldPos, Quaternion.identity, this.transform);
                
                // 2. Instanciar Calles con Lógica de Adyacencia (NUEVO)
                else if (cellType == 4)
                {
                    HandleRoadInstantiation(x, y, worldPos);
                }
            }
        }
    }

    private void HandleRoadInstantiation(int x, int y, Vector3 worldPos)
    {
        // Revisar vecinos (Arriba, Abajo, Derecha, Izquierda)
        // Usamos variables booleanas para mayor claridad.
        bool n = (y + 1 < gridHeight) && (grid[x, y + 1] == 4 || grid[x, y + 1] == 1 || grid[x, y + 1] == 2 || grid[x, y + 1] == 3);
        bool s = (y - 1 >= 0)         && (grid[x, y - 1] == 4 || grid[x, y - 1] == 1 || grid[x, y - 1] == 2 || grid[x, y - 1] == 3);
        bool e = (x + 1 < gridWidth)  && (grid[x + 1, y] == 4 || grid[x + 1, y] == 1 || grid[x + 1, y] == 2 || grid[x + 1, y] == 3);
        bool w = (x - 1 >= 0)         && (grid[x - 1, y] == 4 || grid[x - 1, y] == 1 || grid[x - 1, y] == 2 || grid[x - 1, y] == 3);
        
        // Asumimos que los edificios (1, 2, 3) también se conectan a la calle.

        GameObject roadObj = null;
        Quaternion rotation = Quaternion.identity;

        // Lógica de Bitmasking simplificada para elegir prefab y rotación
        
        // Cruce completo
        if (n && s && e && w && prefabCalleCruce != null) { roadObj = prefabCalleCruce; }
        
        // Rectas
        else if (n && s && prefabCalleRecta != null) { roadObj = prefabCalleRecta; } // Vertical (N-S)
        else if (e && w && prefabCalleRecta != null) { roadObj = prefabCalleRecta; rotation = Quaternion.Euler(0, 90, 0); } // Horizontal (E-W)
        
        // Esquinas
        else if (n && e && prefabCalleEsquina != null) { roadObj = prefabCalleEsquina; } // N a E
        else if (e && s && prefabCalleEsquina != null) { roadObj = prefabCalleEsquina; rotation = Quaternion.Euler(0, 90, 0); } // E a S
        else if (s && w && prefabCalleEsquina != null) { roadObj = prefabCalleEsquina; rotation = Quaternion.Euler(0, 180, 0); } // S a W
        else if (w && n && prefabCalleEsquina != null) { roadObj = prefabCalleEsquina; rotation = Quaternion.Euler(0, 270, 0); } // W a N
        
        // Cruces en T (Implementación básica)
        else if (n && e && s && prefabCalleT != null) { roadObj = prefabCalleT; } // Salidas N, E, S
        else if (e && s && w && prefabCalleT != null) { roadObj = prefabCalleT; rotation = Quaternion.Euler(0, 90, 0); } // Salidas E, S, W
        // ... (añadir más combinaciones de T si tu pack de assets las tiene)

        // Por defecto, si es un final de calle, usamos recta
        else if (prefabCalleRecta != null) { roadObj = prefabCalleRecta; if (e || w) rotation = Quaternion.Euler(0, 90, 0); }

        if (roadObj != null)
        {
            Instantiate(roadObj, worldPos, rotation, this.transform);
        }
    }
}