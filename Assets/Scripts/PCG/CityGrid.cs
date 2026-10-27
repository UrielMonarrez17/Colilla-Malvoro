using UnityEngine;

// Solo datos y consultas del grid. No decide nada de caminos ni de edificios.
public class CityGrid
{
    // El orden coincide con los bits de la mascara: 1=+Y, 2=+X, 4=-Y, 8=-X
    public static readonly Vector2Int[] Directions =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    public readonly int Width;
    public readonly int Length;
    private readonly CellType[,] cells;

    public CityGrid(int width, int length)
    {
        Width = width;
        Length = length;
        cells = new CellType[width, length];
    }

    public bool InBounds(Vector2Int p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Length;
    public int Index(Vector2Int p) => p.x + p.y * Width;
    public CellType Get(Vector2Int p) => cells[p.x, p.y];
    public void Set(Vector2Int p, CellType type) => cells[p.x, p.y] = type;

    public bool IsRoad(Vector2Int p) => InBounds(p) && cells[p.x, p.y] == CellType.Road;
    public bool IsRoad(int x, int y) => IsRoad(new Vector2Int(x, y));

    public int NeighborMask(Vector2Int p)
    {
        int mask = 0;
        for (int d = 0; d < 4; d++)
            if (IsRoad(p + Directions[d])) mask |= 1 << d;
        return mask;
    }

    public int RoadNeighborCount(Vector2Int p)
    {
        int count = 0;
        for (int d = 0; d < 4; d++)
            if (IsRoad(p + Directions[d])) count++;
        return count;
    }

    // Cuadro 2x2 de carretera que contiene a p
    public bool Creates2x2Road(Vector2Int p)
    {
        int x = p.x, y = p.y;
        return (IsRoad(x + 1, y) && IsRoad(x, y + 1) && IsRoad(x + 1, y + 1))
            || (IsRoad(x - 1, y) && IsRoad(x, y + 1) && IsRoad(x - 1, y + 1))
            || (IsRoad(x + 1, y) && IsRoad(x, y - 1) && IsRoad(x + 1, y - 1))
            || (IsRoad(x - 1, y) && IsRoad(x, y - 1) && IsRoad(x - 1, y - 1));
    }

    public bool CanPlaceFootprint(int x, int y, int w, int l)
    {
        if (x < 0 || y < 0 || x + w > Width || y + l > Length) return false;
        for (int i = x; i < x + w; i++)
            for (int j = y; j < y + l; j++)
                if (cells[i, j] != CellType.Empty) return false;
        return true;
    }

    public void MarkFootprint(int x, int y, int w, int l, CellType type)
    {
        for (int i = x; i < x + w; i++)
            for (int j = y; j < y + l; j++)
                cells[i, j] = type;
    }
}