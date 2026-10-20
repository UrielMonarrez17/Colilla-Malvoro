using System.Collections.Generic;
using UnityEngine;

// Coloca edificios especiales y garantiza que cada uno tenga un camino hacia la red principal.
public class SpecialBuildingPlacer
{
    private readonly CityGenerationSettings settings;
    private readonly CityRandom rng;
    private readonly RoadNetwork network;
    private readonly BuildingSpawner spawner;
    private readonly List<Vector2Int> placedAnchors = new List<Vector2Int>();

    public SpecialBuildingPlacer(CityGenerationSettings settings, CityRandom rng,
                                 RoadNetwork network, BuildingSpawner spawner)
    {
        this.settings = settings;
        this.rng = rng;
        this.network = network;
        this.spawner = spawner;
    }

    public void PlaceAll(List<GameObject> prefabs)
    {
        if (prefabs == null) return;
        CityGrid grid = network.Grid;

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;
            if (BuildingSpawner.GetSocket(prefab) == null)
            {
                Debug.LogWarning(prefab.name + " no tiene CitySocket con frontSocket; se omite.");
                continue;
            }

            BuildingData data = BuildingSpawner.GetData(prefab);
            for (int attempt = 0; attempt < settings.specialAttempts; attempt++)
            {
                int x = rng.Range(grid.Width);
                int y = rng.Range(grid.Length);
                if (!grid.CanPlaceFootprint(x, y, data.gridWidth, data.gridLength)) continue;
                if (!IsFarFromOtherSpecials(x, y)) continue;

                if (TryPlace(prefab, x, y, data))
                {
                    placedAnchors.Add(new Vector2Int(x, y));
                    break;
                }
            }
        }
    }

    private bool TryPlace(GameObject prefab, int x, int y, BuildingData data)
    {
        CityGrid grid = network.Grid;
        GameObject go = spawner.Spawn(prefab, x, y, data);
        CitySocket socket = BuildingSpawner.GetSocket(go);

        // Se marca antes para que el camino de acceso nunca pase por encima del edificio
        grid.MarkFootprint(x, y, data.gridWidth, data.gridLength, CellType.SpecialBuilding);
        go.transform.Rotate(0f, rng.Range(4) * 90f, 0f);

        for (int r = 0; r < 4; r++)
        {
            Vector2Int entrance = spawner.GetFrontCell(socket);
            if (network.TryConnectToMain(entrance, settings.maxAccessPathLength)) return true;
            go.transform.Rotate(0f, 90f, 0f);
        }

        grid.MarkFootprint(x, y, data.gridWidth, data.gridLength, CellType.Empty);
        spawner.Discard(go);
        return false;
    }

    private bool IsFarFromOtherSpecials(int x, int y)
    {
        var p = new Vector2Int(x, y);
        foreach (Vector2Int anchor in placedAnchors)
            if (Vector2Int.Distance(p, anchor) < settings.minCellDistanceBetweenSpecials) return false;
        return true;
    }
}
