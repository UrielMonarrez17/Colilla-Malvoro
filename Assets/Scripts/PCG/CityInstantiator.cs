using UnityEngine;

// Convierte el grid final en GameObjects. Es el unico que conoce los prefabs de calles.
public class CityInstantiator
{
    private readonly CityGrid grid;
    private readonly Transform parent;
    private readonly float cellSize;
    private readonly float roadHeightY;
    private readonly RoadPrefabs prefabs;

    public CityInstantiator(CityGrid grid, Transform parent, float cellSize,
                            float roadHeightY, RoadPrefabs prefabs)
    {
        this.grid = grid;
        this.parent = parent;
        this.cellSize = cellSize;
        this.roadHeightY = roadHeightY;
        this.prefabs = prefabs;
    }

    public void Instantiate()
    {
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Length; y++)
            {
                var cell = new Vector2Int(x, y);
                CellType type = grid.Get(cell);

                if (type == CellType.Road) SpawnRoad(cell);
                else if (type == CellType.Empty) SpawnPavement(cell);
            }
        }
    }

    private Vector3 CellCenter(Vector2Int cell, float baseY) =>
        new Vector3(cell.x * cellSize + cellSize / 2f, baseY + roadHeightY, cell.y * cellSize + cellSize / 2f);

    private void SpawnPavement(Vector2Int cell)
    {
        if (prefabs.pavement == null) return;
        Vector3 pos = CellCenter(cell, prefabs.pavement.transform.position.y);
        UnityEngine.Object.Instantiate(prefabs.pavement, pos, Quaternion.identity, parent);
    }

    private void SpawnRoad(Vector2Int cell)
    {
        RoadTile tile = RoadTileMapper.Pick(grid.NeighborMask(cell), prefabs);
        if (tile.prefab == null) return;

        Vector3 pos = CellCenter(cell, tile.prefab.transform.position.y);
        float offset = tile.prefab.GetComponent<RoadData>()?.rotationOffset ?? 0f;
        Quaternion rot = tile.prefab.transform.rotation * Quaternion.Euler(0f, tile.yRot + offset, 0f);
        Object.Instantiate(tile.prefab, pos, rot, parent);
    }
}

public struct RoadTile
{
    public GameObject prefab;
    public float yRot;
}

// Mascara de vecinos (1=+Y, 2=+X, 4=-Y, 8=-X) -> prefab y rotacion. Misma tabla que antes.
public static class RoadTileMapper
{
    public static RoadTile Pick(int mask, RoadPrefabs p)
    {
        switch (mask)
        {
            case 0:
            case 1:  return Tile(p.deadEnd, 0f);
            case 2:  return Tile(p.deadEnd, 90f);
            case 4:  return Tile(p.deadEnd, 180f);
            case 8:  return Tile(p.deadEnd, 270f);
            case 5:  return Tile(p.forward, 0f);
            case 10: return Tile(p.forward, 90f);
            case 3:  return Tile(p.angle, 0f);
            case 6:  return Tile(p.angle, 90f);
            case 12: return Tile(p.angle, 180f);
            case 9:  return Tile(p.angle, 270f);
            case 7:  return Tile(p.t, 0f);
            case 14: return Tile(p.t, 90f);
            case 13: return Tile(p.t, 180f);
            case 11: return Tile(p.t, 270f);
            case 15: return Tile(p.x, 0f);
            default: return Tile(p.forward, 0f);
        }
    }

    private static RoadTile Tile(GameObject prefab, float yRot)
    {
        return new RoadTile { prefab = prefab, yRot = yRot };
    }
}
