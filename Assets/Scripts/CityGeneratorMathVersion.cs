using System.Collections.Generic;
using UnityEngine;

public class CityGeneratorMathVersion : MonoBehaviour
{
    [Header("Motor Matemático (PRNG)")]
    public int seed = 12345;

    [Header("Dimensiones de la Ciudad")]
    public int mapWidth = 30;
    public int mapLength = 30;
    public float cellSize = 10f;

    [Header("Listas de Edificios")]
    public List<GameObject> specialBuildings = new List<GameObject>();
    public List<GameObject> normalBuildings = new List<GameObject>();

    [Header("Prefabs de Caminos y Suelos")]
    public GameObject roadForward;
    public GameObject roadAngle;
    public GameObject roadT; 
    public GameObject roadX; 
    public GameObject roadDeadEnd;
    public GameObject pedestrianPavement;

    [Header("Ajustes")]
    public float roadHeightY = 0f;
    public int minCellDistanceBetweenSpecials = 4;
    [Range(0f, 1f)]
    public float normalBuildingDensity = 0.6f;

    private int[,] grid;
    private MathPRNG prng;
    private List<Vector2Int> placedSpecialPositions = new List<Vector2Int>();

    private class RoadAgent
    {
        public int x, y, dir; 
        public RoadAgent(int startX, int startY, int direction)
        {
            x = startX; y = startY; dir = direction;
        }
    }

    void Start()
    {
        GenerateCity();
    }

    public void GenerateCity()
    {
        prng = new MathPRNG(seed);
        grid = new int[mapWidth, mapLength];
        placedSpecialPositions.Clear();

        TraceRoadsWithAgents();
        ConnectUnfinishedRoads(); 
        PlaceSpecialBuildings();
        PlaceNormalBuildings();
        FillRemainingWithPavement();
        InstantiateCity();
    }

    int GetUniformDigit(int maxExcl)
    {
        if (maxExcl <= 0) return 0;
        
        // Si el rango es 10 o menor, usamos rechazo simple (0-9)
        if (maxExcl <= 10)
        {
            int validLimit = (10 / maxExcl) * maxExcl;
            int d = prng.ConsumeDigit();
            while (d >= validLimit) 
            {
                d = prng.ConsumeDigit();
            }
            return d % maxExcl;
        }
        else
        {
            // Si el rango es mayor a 10 (ej. mapWidth = 30), generamos un número entre 0 y 99
            // consumiendo dos dígitos, y rechazamos si es mayor o igual al límite.
            int d = (prng.ConsumeDigit() * 10) + prng.ConsumeDigit();
            while (d >= maxExcl)
            {
                d = (prng.ConsumeDigit() * 10) + prng.ConsumeDigit();
            }
            return d;
        }
    }

    void TraceRoadsWithAgents()
    {
        int numAgents = prng.ConsumeDigit() + 3; 
        List<RoadAgent> agents = new List<RoadAgent>();

        for (int i = 0; i < numAgents; i++)
        {
            int startX = (prng.ConsumeDigit() % 2 == 0) ? 0 : mapWidth - 1;
            int startY = GetUniformDigit(mapLength); 
            int dir = (startX == 0) ? 1 : 3; 
            
            if (grid[startX, startY] != 3) {
                agents.Add(new RoadAgent(startX, startY, dir));
                grid[startX, startY] = 3; 
            }
        }

        int stepsLimit = 4000; 
        while (agents.Count > 0 && stepsLimit > 0)
        {
            stepsLimit--;
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                RoadAgent agent = agents[i];
                
                int action = prng.ConsumeDigit();
                if (action == 6) agent.dir = (agent.dir + 3) % 4; 
                else if (action == 7) agent.dir = (agent.dir + 1) % 4; 

                bool agentResolvedTurn = false;

                for (int attempts = 0; attempts < 4; attempts++)
                {
                    int nx = agent.x; int ny = agent.y;
                    if (agent.dir == 0) ny++; else if (agent.dir == 1) nx++;
                    else if (agent.dir == 2) ny--; else if (agent.dir == 3) nx--;

                    if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapLength)
                    {
                        agents.RemoveAt(i);
                        agentResolvedTurn = true;
                        break;
                    }

                    if (grid[nx, ny] == 3)
                    {
                        int colDecision = prng.ConsumeDigit();
                        if (colDecision % 2 == 0) 
                        {
                            agents.RemoveAt(i); 
                        }
                        else 
                        {
                            int jumpX = nx; int jumpY = ny;
                            if (agent.dir == 0) jumpY++; else if (agent.dir == 1) jumpX++;
                            else if (agent.dir == 2) jumpY--; else if (agent.dir == 3) jumpX--;
                            
                            if (jumpX >= 0 && jumpX < mapWidth && jumpY >= 0 && jumpY < mapLength && grid[jumpX, jumpY] != 3)
                            {
                                agent.x = jumpX; agent.y = jumpY; 
                                grid[agent.x, agent.y] = 3; 
                            }
                            else
                            {
                                agents.RemoveAt(i); 
                            }
                        }
                        agentResolvedTurn = true;
                        break;
                    }

                    if (Creates2x2Road(nx, ny))
                    {
                        agent.dir = (agent.dir + 1) % 4;
                        continue; 
                    }

                    agent.x = nx; agent.y = ny;
                    grid[agent.x, agent.y] = 3;
                    agentResolvedTurn = true;

                    if (prng.ConsumeDigit() == 9) 
                    {
                        int newDir = (agent.dir + (prng.ConsumeDigit() % 2 == 0 ? 1 : 3)) % 4;
                        agents.Add(new RoadAgent(agent.x, agent.y, newDir));
                    }
                    break; 
                }

                if (!agentResolvedTurn)
                {
                    agents.RemoveAt(i);
                }
            }
        }
    }

    void ConnectUnfinishedRoads()
    {
        bool changed = true;
        int loopBreaker = 100; 
        
        while (changed && loopBreaker > 0)
        {
            changed = false;
            loopBreaker--;
            
            for (int x = 1; x < mapWidth - 1; x++)
            {
                for (int y = 1; y < mapLength - 1; y++)
                {
                    if (grid[x, y] == 3)
                    {
                        int mask = CalculateNeighborMask(x, y);
                        
                        if (mask == 1 || mask == 2 || mask == 4 || mask == 8)
                        {
                            int targetX = x; int targetY = y;
                            
                            if (mask == 1) targetY--; 
                            else if (mask == 2) targetX--; 
                            else if (mask == 4) targetY++; 
                            else if (mask == 8) targetX++; 
                            
                            if (targetX >= 0 && targetX < mapWidth && targetY >= 0 && targetY < mapLength)
                            {
                                if (grid[targetX, targetY] != 3) 
                                {
                                    grid[targetX, targetY] = 3; 
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    bool Creates2x2Road(int x, int y)
    {
        if (IsRoad(x+1, y) && IsRoad(x, y+1) && IsRoad(x+1, y+1)) return true;
        if (IsRoad(x-1, y) && IsRoad(x, y+1) && IsRoad(x-1, y+1)) return true;
        if (IsRoad(x+1, y) && IsRoad(x, y-1) && IsRoad(x+1, y-1)) return true;
        if (IsRoad(x-1, y) && IsRoad(x, y-1) && IsRoad(x-1, y-1)) return true;
        return false;
    }

    bool IsRoad(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapLength) return false;
        return grid[x, y] == 3;
    }

    void PlaceSpecialBuildings()
    {
        foreach (GameObject prefab in specialBuildings)
        {
            int attempts = 50;
            while (attempts > 0)
            {
                attempts--;
                
                int randomX = GetUniformDigit(mapWidth);
                int randomY = GetUniformDigit(mapLength);
                
                BuildingData bData = GetBuildingData(prefab);
                if (CanPlaceBuilding(randomX, randomY, bData.gridWidth, bData.gridLength) && IsFarFromOtherSpecials(randomX, randomY))
                {
                    if (SpawnAndOrientBuilding(prefab, randomX, randomY, bData))
                    {
                        MarkGridCellsAndPavement(randomX, randomY, bData.gridWidth, bData.gridLength, 1);
                        placedSpecialPositions.Add(new Vector2Int(randomX, randomY));
                        break;
                    }
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
                int spawnChance = prng.ConsumeDigit(); 
                if (spawnChance < (normalBuildingDensity * 10)) 
                {
                    int prefabIndex = GetUniformDigit(normalBuildings.Count); 
                    GameObject normalPrefab = normalBuildings[prefabIndex];
                    BuildingData bData = GetBuildingData(normalPrefab);

                    if (CanPlaceBuilding(x, y, bData.gridWidth, bData.gridLength))
                    {
                        if (SpawnAndOrientBuilding(normalPrefab, x, y, bData))
                        {
                            MarkGridCellsAndPavement(x, y, bData.gridWidth, bData.gridLength, 2);
                        }
                    }
                }
            }
        }
    }

    bool SpawnAndOrientBuilding(GameObject prefab, int startX, int startY, BuildingData bData)
    {
        Vector3 spawnPos = bData.GetCalculatedWorldPosition(startX, startY, cellSize, prefab.transform.position.y);
        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        
        CitySocket socket = spawned.GetComponent<CitySocket>();
        if (socket != null && socket.frontSocket != null)
        {
            int randomStartRot = GetUniformDigit(4);
            spawned.transform.Rotate(0, randomStartRot * 90, 0);

            bool oriented = false;
            for (int i = 0; i < 4; i++)
            {
                Vector3 checkPos = socket.frontSocket.position + socket.frontSocket.forward * (cellSize * 0.9f);
                
                int targetX = Mathf.FloorToInt(checkPos.x / cellSize);
                int targetY = Mathf.FloorToInt(checkPos.z / cellSize);

                if (targetX >= 0 && targetX < mapWidth && targetY >= 0 && targetY < mapLength)
                {
                    if (grid[targetX, targetY] == 3)
                    {
                        oriented = true;
                        break; 
                    }
                }
                spawned.transform.Rotate(0, 90, 0);
            }
            
            if (!oriented) 
            {
                Destroy(spawned); 
                return false; 
            }
        }
        return true; 
    }

    bool CanPlaceBuilding(int startX, int startY, int width, int length)
    {
        if (startX + width > mapWidth || startY + length > mapLength) return false;
        for (int x = startX; x < startX + width; x++)
            for (int y = startY; y < startY + length; y++)
                if (grid[x, y] != 0) return false; 
        return true;
    }

    void MarkGridCellsAndPavement(int startX, int startY, int width, int length, int type)
    {
        for (int x = startX; x < startX + width; x++)
            for (int y = startY; y < startY + length; y++)
                grid[x, y] = type;

        for (int x = startX; x < startX + width; x++)
        {
            SetPavementIfEmpty(x, startY - 1);
            SetPavementIfEmpty(x, startY + length);
        }
        for (int y = startY; y < startY + length; y++)
        {
            SetPavementIfEmpty(startX - 1, y);
            SetPavementIfEmpty(startX + width, y);
        }
    }

    void SetPavementIfEmpty(int x, int y)
    {
        if (x >= 0 && x < mapWidth && y >= 0 && y < mapLength)
            if (grid[x, y] == 0) grid[x, y] = 4;
    }

    void FillRemainingWithPavement()
    {
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapLength; y++)
                if (grid[x, y] == 0) grid[x, y] = 4;
    }

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
        float yRot = 0f;

        switch (mask)
        {
            case 0: selectedRoadPrefab = roadDeadEnd; yRot = 0f; break;
            case 1: selectedRoadPrefab = roadDeadEnd; yRot = 0f; break;
            case 2: selectedRoadPrefab = roadDeadEnd; yRot = 90f; break;
            case 4: selectedRoadPrefab = roadDeadEnd; yRot = 180f; break;
            case 8: selectedRoadPrefab = roadDeadEnd; yRot = 270f; break;
            case 5: selectedRoadPrefab = roadForward; yRot = 0f; break;
            case 10: selectedRoadPrefab = roadForward; yRot = 90f; break;
            case 3: selectedRoadPrefab = roadAngle; yRot = 0f; break;
            case 6: selectedRoadPrefab = roadAngle; yRot = 90f; break;
            case 12: selectedRoadPrefab = roadAngle; yRot = 180f; break;
            case 9: selectedRoadPrefab = roadAngle; yRot = 270f; break;
            case 7: selectedRoadPrefab = roadT; yRot = 0f; break;
            case 14: selectedRoadPrefab = roadT; yRot = 90f; break;
            case 13: selectedRoadPrefab = roadT; yRot = 180f; break;
            case 11: selectedRoadPrefab = roadT; yRot = 270f; break;
            case 15: selectedRoadPrefab = roadX; yRot = 0f; break;
        }

        Vector3 roadPos = new Vector3((x * cellSize) + (cellSize / 2f), selectedRoadPrefab.transform.position.y + roadHeightY, (y * cellSize) + (cellSize / 2f));
        float offset = selectedRoadPrefab.GetComponent<RoadData>()?.rotationOffset ?? 0f;
        Instantiate(selectedRoadPrefab, roadPos, selectedRoadPrefab.transform.rotation * Quaternion.Euler(0f, yRot + offset, 0f), transform);
    }

    int CalculateNeighborMask(int x, int y)
    {
        int mask = 0;
        if (y + 1 < mapLength && grid[x, y + 1] == 3) mask += 1;
        if (x + 1 < mapWidth && grid[x + 1, y] == 3)  mask += 2;
        if (y - 1 >= 0 && grid[x, y - 1] == 3)        mask += 4;
        if (x - 1 >= 0 && grid[x - 1, y] == 3)        mask += 8;
        return mask;
    }

    bool IsFarFromOtherSpecials(int gridX, int gridY)
    {
        Vector2Int currentPos = new Vector2Int(gridX, gridY);
        foreach (Vector2Int pos in placedSpecialPositions)
            if (Vector2Int.Distance(currentPos, pos) < minCellDistanceBetweenSpecials) return false;
        return true;
    }

    BuildingData GetBuildingData(GameObject prefab)
    {
        BuildingData bData = prefab.GetComponent<BuildingData>();
        return bData != null ? bData : prefab.AddComponent<BuildingData>();
    }
}