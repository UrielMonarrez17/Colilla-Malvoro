using System.Collections.Generic;
using UnityEngine;

// Unico responsable de colocar carreteras. Usa Union-Find para saber
// cuando un camino cierra un ciclo, aunque no se coloque una celda nueva.
public class RoadNetwork
{
    public readonly CityGrid Grid;
    public readonly RoadLoopRegistry Loops;

    private readonly CityGenerationSettings settings;
    private readonly int[] parent;

    public RoadNetwork(CityGrid grid, CityGenerationSettings settings)
    {
        Grid = grid;
        Loops = new RoadLoopRegistry(grid);
        this.settings = settings;
        parent = new int[grid.Width * grid.Length];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;
    }

    // ---------- Evaluar (no modifica nada) ----------

    public PlacementVerdict Evaluate(Vector2Int from, Vector2Int to)
    {
        if (!Grid.InBounds(to)) return PlacementVerdict.Outside;

        if (Grid.Get(to) == CellType.Road)
        {
            if (!SameComponent(from, to)) return PlacementVerdict.Join;
            return FindAcceptableCycle(from, to, null) != null ? PlacementVerdict.Loop : PlacementVerdict.Blocked;
        }

        if (Grid.Get(to) != CellType.Empty) return PlacementVerdict.Blocked;
        if (Loops.IsNextToLoop(to) || Grid.Creates2x2Road(to)) return PlacementVerdict.Blocked;

        if (FindRedundantPair(to, out Vector2Int a, out Vector2Int b))
            return FindAcceptableCycle(a, b, to) != null ? PlacementVerdict.Loop : PlacementVerdict.Blocked;

        return PlacementVerdict.Empty;
    }

    // ---------- Ejecutar (si modifica) ----------

    public void Commit(Vector2Int from, Vector2Int to, PlacementVerdict verdict)
    {
        switch (verdict)
        {
            case PlacementVerdict.Empty:
                PlaceRoad(to);
                break;

            case PlacementVerdict.Join:
                Union(from, to);
                break;

            case PlacementVerdict.Loop:
                if (Grid.IsRoad(to))
                {
                    List<Vector2Int> ringA = FindAcceptableCycle(from, to, null);
                    Union(from, to);
                    if (ringA != null) Loops.Register(ringA);
                }
                else
                {
                    FindRedundantPair(to, out Vector2Int a, out Vector2Int b);
                    List<Vector2Int> ringB = FindAcceptableCycle(a, b, to);
                    PlaceRoad(to);
                    if (ringB != null) Loops.Register(ringB);
                }
                break;
        }
    }

    public void PlaceRoad(Vector2Int p)
    {
        int i = Grid.Index(p);
        Grid.Set(p, CellType.Road);
        parent[i] = i;
        for (int d = 0; d < 4; d++)
        {
            Vector2Int n = p + CityGrid.Directions[d];
            if (Grid.IsRoad(n)) Union(p, n);
        }
    }

    // Conecta la entrada de un edificio especial con la red principal (o confirma que ya lo esta)
    public bool TryConnectToMain(Vector2Int entrance, int maxLength)
    {
        if (!Grid.InBounds(entrance)) return false;

        int mainRoot = FindMainRoot();
        if (mainRoot < 0) return false;

        if (Grid.Get(entrance) == CellType.Road)
            return Find(Grid.Index(entrance)) == mainRoot;

        List<Vector2Int> path = FindAccessPath(entrance, mainRoot, maxLength);
        if (path == null) return false;

        foreach (Vector2Int p in path) PlaceRoad(p);
        return true;
    }

    // ---------- Ciclos ----------

    // Ciclo = camino a->b (+ celda nueva si la hay). Solo se acepta si es una rotonda pequena y separada.
    private List<Vector2Int> FindAcceptableCycle(Vector2Int a, Vector2Int b, Vector2Int? newCell)
    {
        List<Vector2Int> cycle = FindPath(a, b);
        if (cycle == null) return null;

        if (newCell.HasValue) cycle.Add(newCell.Value);
        if (cycle.Count > settings.maxRoundaboutCells) return null;
        if (Loops.IsTooCloseToLoops(cycle, settings.roundaboutSpacing)) return null;
        return cycle;
    }

    // Camino por carreteras de start a goal, ignorando la arista directa start-goal
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        int maxDepth = settings.maxRoundaboutCells;
        var prev = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var depth = new Dictionary<Vector2Int, int> { [start] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (depth[cur] >= maxDepth) continue;

            for (int d = 0; d < 4; d++)
            {
                Vector2Int nxt = cur + CityGrid.Directions[d];
                if (prev.ContainsKey(nxt) || !Grid.IsRoad(nxt)) continue;
                if (cur == start && nxt == goal) continue;

                prev[nxt] = cur;
                depth[nxt] = depth[cur] + 1;
                if (nxt == goal) return Rebuild(prev, start, goal);
                queue.Enqueue(nxt);
            }
        }
        return null;
    }

    // Dos vecinos de 'cell' que ya estan conectados entre si: unir 'cell' cerraria un ciclo
    private bool FindRedundantPair(Vector2Int cell, out Vector2Int a, out Vector2Int b)
    {
        var roads = new List<Vector2Int>(4);
        for (int d = 0; d < 4; d++)
        {
            Vector2Int n = cell + CityGrid.Directions[d];
            if (Grid.IsRoad(n)) roads.Add(n);
        }

        for (int i = 0; i < roads.Count; i++)
            for (int j = i + 1; j < roads.Count; j++)
                if (SameComponent(roads[i], roads[j]))
                {
                    a = roads[i];
                    b = roads[j];
                    return true;
                }

        a = default;
        b = default;
        return false;
    }

    // ---------- Accesos a edificios especiales ----------

    private bool IsPassable(Vector2Int p) =>
        Grid.InBounds(p) && Grid.Get(p) == CellType.Empty
        && !Loops.IsNextToLoop(p) && Grid.RoadNeighborCount(p) == 0;

    private bool IsGoal(Vector2Int p, int mainRoot) =>
        Grid.InBounds(p) && Grid.Get(p) == CellType.Empty
        && !Loops.IsNextToLoop(p) && Grid.RoadNeighborCount(p) == 1
        && TouchesRoot(p, mainRoot);

    private bool TouchesRoot(Vector2Int p, int root)
    {
        for (int d = 0; d < 4; d++)
        {
            Vector2Int n = p + CityGrid.Directions[d];
            if (Grid.IsRoad(n) && Find(Grid.Index(n)) == root) return true;
        }
        return false;
    }

    // BFS por celdas libres que no tocan carreteras, hasta una celda que se pegue a la red principal
    private List<Vector2Int> FindAccessPath(Vector2Int start, int mainRoot, int maxLength)
    {
        if (IsGoal(start, mainRoot)) return new List<Vector2Int> { start };
        if (!IsPassable(start)) return null;

        var prev = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var depth = new Dictionary<Vector2Int, int> { [start] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (depth[cur] >= maxLength) continue;

            for (int d = 0; d < 4; d++)
            {
                Vector2Int nxt = cur + CityGrid.Directions[d];
                if (prev.ContainsKey(nxt)) continue;

                if (IsGoal(nxt, mainRoot))
                {
                    prev[nxt] = cur;
                    return Rebuild(prev, start, nxt);
                }
                if (!IsPassable(nxt)) continue;

                prev[nxt] = cur;
                depth[nxt] = depth[cur] + 1;
                queue.Enqueue(nxt);
            }
        }
        return null;
    }

    // Raiz del componente con mas carreteras (la red principal)
    private int FindMainRoot()
    {
        var sizes = new Dictionary<int, int>();
        for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Length; y++)
            {
                var p = new Vector2Int(x, y);
                if (!Grid.IsRoad(p)) continue;
                int root = Find(Grid.Index(p));
                sizes[root] = sizes.TryGetValue(root, out int n) ? n + 1 : 1;
            }

        int best = -1, bestSize = 0;
        foreach (KeyValuePair<int, int> kv in sizes)
            if (kv.Value > bestSize)
            {
                best = kv.Key;
                bestSize = kv.Value;
            }
        return best;
    }

    // ---------- Union-Find ----------

    private int Find(int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }
        return i;
    }

    private void Union(Vector2Int a, Vector2Int b)
    {
        int ra = Find(Grid.Index(a));
        int rb = Find(Grid.Index(b));
        if (ra != rb) parent[rb] = ra;
    }

    private bool SameComponent(Vector2Int a, Vector2Int b) =>
        Find(Grid.Index(a)) == Find(Grid.Index(b));

    private static List<Vector2Int> Rebuild(Dictionary<Vector2Int, Vector2Int> prev, Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int> { goal };
        Vector2Int cur = goal;
        while (cur != start)
        {
            cur = prev[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }
}