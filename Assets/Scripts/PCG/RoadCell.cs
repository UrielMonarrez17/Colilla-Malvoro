using System.Collections.Generic;
using UnityEngine;

// Celda caminante: avanza por el grid construyendo carretera.
// Move() recibe un porcentaje (0..1) y con el decide que hacer en ese paso.
public class RoadCell
{
    public Vector2Int Position { get; private set; }
    public int Direction { get; private set; }        // 0:+Y  1:+X  2:-Y  3:-X
    public int BranchDirection { get; private set; }  // direccion de la rama tras un Branch
    public WalkerState State { get; private set; } = WalkerState.Walking;

    // Historial del recorrido (para depurar, por ejemplo con Gizmos)
    public readonly List<Vector2Int> Path = new List<Vector2Int>();

    private readonly CityGenerationSettings settings;
    private int pendingBranchSide;   // -1 izquierda, +1 derecha

    public RoadCell(Vector2Int start, int direction, CityGenerationSettings settings)
    {
        Position = start;
        Direction = direction;
        this.settings = settings;
        Path.Add(start);
    }

    // Uso: cell.Move(prng.NextProbability(2), network)
    public RoadStepResult Move(float percent, RoadNetwork network)
    {
        if (State != WalkerState.Walking) return RoadStepResult.Died;

        RoadAction action = Decide(percent);
        if (action == RoadAction.TurnLeft) Direction = (Direction + 3) % 4;
        else if (action == RoadAction.TurnRight) Direction = (Direction + 1) % 4;

        // Si el paso esta bloqueado, gira a la derecha y reintenta (maximo 4 intentos)
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Vector2Int next = Position + CityGrid.Directions[Direction];
            PlacementVerdict verdict = network.Evaluate(Position, next);

            switch (verdict)
            {
                case PlacementVerdict.Outside:
                    return Finish(RoadStepResult.Died);

                case PlacementVerdict.Blocked:
                    Direction = (Direction + 1) % 4;
                    continue;

                case PlacementVerdict.Join:
                case PlacementVerdict.Loop:
                    // Se conecta con la red o cierra una rotonda: la celda muere
                    network.Commit(Position, next, verdict);
                    return Finish(RoadStepResult.Connected);

                case PlacementVerdict.Empty:
                    network.Commit(Position, next, verdict);
                    Position = next;
                    Path.Add(next);
                    if (action == RoadAction.Branch)
                    {
                        BranchDirection = (Direction + (pendingBranchSide < 0 ? 3 : 1)) % 4;
                        return RoadStepResult.Branched;
                    }
                    return RoadStepResult.Advanced;
            }
        }

        return Finish(RoadStepResult.Died);
    }

    // Un solo porcentaje reparte las decisiones en rangos consecutivos
    private RoadAction Decide(float p)
    {
        float left = settings.turnChancePerSide;
        float right = left + settings.turnChancePerSide;
        float branch = right + settings.branchChance;

        if (p < left) return RoadAction.TurnLeft;
        if (p < right) return RoadAction.TurnRight;
        if (p < branch)
        {
            pendingBranchSide = (p - right) < settings.branchChance * 0.5f ? -1 : 1;
            return RoadAction.Branch;
        }
        return RoadAction.Forward;
    }

    private RoadStepResult Finish(RoadStepResult result)
    {
        State = WalkerState.Finished;
        return result;
    }
}