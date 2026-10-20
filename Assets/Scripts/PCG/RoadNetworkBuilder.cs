using System.Collections.Generic;
using UnityEngine;

// Traza la red: caminantes que nacen en los bordes y alargamiento de callejones sin salida.
public class RoadNetworkBuilder
{
    private readonly CityGenerationSettings settings;
    private readonly CityRandom rng;
    private readonly RoadNetwork network;
    private readonly CityGrid grid;

    public RoadNetworkBuilder(CityGenerationSettings settings, CityRandom rng, RoadNetwork network)
    {
        this.settings = settings;
        this.rng = rng;
        this.network = network;
        grid = network.Grid;
    }

    public void Build()
    {
        TraceWalkers();
        TrimOrConnectDeadEnds();
    }

    private void TraceWalkers()
    {
        var walkers = new List<RoadCell>();
        int count = rng.Range(settings.minWalkers, settings.maxWalkers + 1);

        for (int i = 0; i < count; i++)
        {
            bool fromLeft = rng.Chance(0.5f);
            var start = new Vector2Int(fromLeft ? 0 : grid.Width - 1, rng.Range(grid.Length));
            if (grid.Get(start) != CellType.Empty || grid.RoadNeighborCount(start) > 0) continue;

            network.PlaceRoad(start);
            walkers.Add(new RoadCell(start, fromLeft ? 1 : 3, settings));
        }

        int stepsLeft = settings.maxWalkerSteps;
        while (walkers.Count > 0 && stepsLeft-- > 0)
        {
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                RoadCell walker = walkers[i];
                RoadStepResult result = walker.Move(rng.Value(), network);

                if (result == RoadStepResult.Branched)
                    walkers.Add(new RoadCell(walker.Position, walker.BranchDirection, settings));
                else if (result == RoadStepResult.Connected || result == RoadStepResult.Died)
                    walkers.RemoveAt(i);
            }
        }
    }
// Intenta conectar la punta muerta, si no puede, borra solo esa celda
    private void TrimOrConnectDeadEnds()
    {
        var deadEnds = new List<Vector2Int>();

        // 1. Recopilamos todos los callejones sin salida actuales
        for (int x = 1; x < grid.Width - 1; x++)
        {
            for (int y = 1; y < grid.Length - 1; y++)
            {
                var cell = new Vector2Int(x, y);
                if (grid.IsRoad(cell) && grid.RoadNeighborCount(cell) == 1)
                {
                    deadEnds.Add(cell);
                }
            }
        }

        // 2. Evaluamos cada uno sin reacción en cadena
        foreach (var cell in deadEnds)
        {
            int mask = grid.NeighborMask(cell);
            int dirIndex = BitIndex(mask);
            
            // Calculamos la dirección opuesta a su única conexión (hacia donde apunta el callejón)
            // Directions: 0=+Y, 1=+X, 2=-Y, 3=-X. Sumar 2 nos da el lado opuesto.
            Vector2Int forwardDir = CityGrid.Directions[(dirIndex + 2) % 4];
            Vector2Int forwardCell = cell + forwardDir;

            bool connected = false;

            // Intentamos extenderlo/conectarlo hacia adelante
            if (grid.InBounds(forwardCell))
            {
                PlacementVerdict verdict = network.Evaluate(cell, forwardCell);
                if (verdict == PlacementVerdict.Empty || verdict == PlacementVerdict.Join || verdict == PlacementVerdict.Loop)
                {
                    network.Commit(cell, forwardCell, verdict);
                    connected = true;
                }
            }

            // Si no sirvió para conectar a nada útil, desaparecemos esa única celda
            if (!connected)
            {
                grid.Set(cell, CellType.Empty);
            }
        }
    }

    private static bool IsSingleBit(int mask) => mask != 0 && (mask & (mask - 1)) == 0;

    private static int BitIndex(int mask)
    {
        int index = 0;
        while (mask > 1)
        {
            mask >>= 1;
            index++;
        }
        return index;
    }
}
