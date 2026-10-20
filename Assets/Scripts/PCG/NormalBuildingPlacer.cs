using System.Collections.Generic;
using UnityEngine;

// Llena el mapa con edificios normales, priorizando celdas pegadas a una calle.
// Ya no reserva pavimento alrededor de cada edificio: asi pueden formar manzanas densas.
public class NormalBuildingPlacer
{
    private readonly CityGenerationSettings settings;
    private readonly CityRandom rng;
    private readonly CityGrid grid;
    private readonly BuildingSpawner spawner;

    public NormalBuildingPlacer(CityGenerationSettings settings, CityRandom rng,
                                CityGrid grid, BuildingSpawner spawner)
    {
        this.settings = settings;
        this.rng = rng;
        this.grid = grid;
        this.spawner = spawner;
    }

    public void PlaceAll(List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0) return;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Length; y++)
            {
                TryPlaceAt(x, y, prefabs);
            }
        }
    }

    private void TryPlaceAt(int x, int y, List<GameObject> prefabs)
    {
        var cell = new Vector2Int(x, y);
        if (grid.Get(cell) != CellType.Empty) return;

        bool nearRoad = grid.RoadNeighborCount(cell) > 0;
        if (!nearRoad && !settings.allowInteriorBuildings) return;
        if (!rng.Chance(settings.normalBuildingDensity)) return;

        GameObject prefab = prefabs[rng.Range(prefabs.Count)];
        if (prefab == null) return;

        BuildingData data = BuildingSpawner.GetData(prefab);
        if (!grid.CanPlaceFootprint(x, y, data.gridWidth, data.gridLength)) return;

        if (spawner.SpawnFacingRoad(prefab, x, y, data) != null)
            grid.MarkFootprint(x, y, data.gridWidth, data.gridLength, CellType.NormalBuilding);
    }
}
