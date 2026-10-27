using System.Collections.Generic;
using UnityEngine;

// Memoria de un anillo cerrado (rotonda): guarda los indices planos del grid que lo forman.
public class RoadLoop
{
    public readonly int Id;
    public readonly int[] CellIndices;

    public RoadLoop(int id, int[] cellIndices)
    {
        Id = id;
        CellIndices = cellIndices;
    }
}

// Guarda todas las rotondas y responde preguntas sobre ellas.
public class RoadLoopRegistry
{
    private readonly CityGrid grid;
    private readonly bool[,] loopCell;
    private readonly List<RoadLoop> loops = new List<RoadLoop>();

    public IReadOnlyList<RoadLoop> All => loops;

    public RoadLoopRegistry(CityGrid grid)
    {
        this.grid = grid;
        loopCell = new bool[grid.Width, grid.Length];
    }

    public bool IsLoopCell(Vector2Int p) => grid.InBounds(p) && loopCell[p.x, p.y];

    // Zona prohibida: ninguna celda nueva puede quedar pegada a un anillo
    public bool IsNextToLoop(Vector2Int p)
    {
        for (int d = 0; d < 4; d++)
            if (IsLoopCell(p + CityGrid.Directions[d])) return true;
        return false;
    }

    // Alguna celda esta a menos de 'spacing' celdas de una rotonda existente
    public bool IsTooCloseToLoops(IList<Vector2Int> cells, int spacing)
    {
        int r = spacing - 1;
        foreach (Vector2Int c in cells)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    if (IsLoopCell(new Vector2Int(c.x + dx, c.y + dy))) return true;
        return false;
    }

    public void Register(IList<Vector2Int> cells)
    {
        int[] indices = new int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            indices[i] = grid.Index(cells[i]);
            loopCell[cells[i].x, cells[i].y] = true;
        }
        loops.Add(new RoadLoop(loops.Count, indices));
    }
}