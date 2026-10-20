using System;
using UnityEngine;

// Estado de cada celda del grid
public enum CellType : byte { Empty, Road, SpecialBuilding, NormalBuilding }

// Accion que decide un caminante en cada paso
public enum RoadAction : byte { Forward, TurnLeft, TurnRight, Branch }

// Estado de vida de un caminante
public enum WalkerState : byte { Walking, Finished }

// Resultado de un paso de un caminante
public enum RoadStepResult : byte { Advanced, Branched, Connected, Died }

// Veredicto de evaluar pasar de una celda a otra
public enum PlacementVerdict : byte
{
    Outside,   // fuera del mapa
    Blocked,   // ocupada, pegada a una rotonda, 2x2, o ciclo no permitido
    Empty,     // celda libre: se coloca carretera
    Join,      // el destino ya es carretera: solo se conecta
    Loop       // cierra un ciclo aceptable (rotonda)
}

[Serializable]
public class CityGenerationSettings
{
    [Header("Caminantes (red de caminos)")]
    public int minWalkers = 3;
    public int maxWalkers = 8;
    public int maxWalkerSteps = 4000;
    [Range(0f, 0.5f)] public float turnChancePerSide = 0.10f;
    [Range(0f, 0.5f)] public float branchChance = 0.10f;

    [Header("Rotondas")]
    [Tooltip("8 = anillo 3x3 con isla de 1 celda")]
    public int maxRoundaboutCells = 8;
    [Tooltip("Distancia minima en celdas entre dos rotondas")]
    public int roundaboutSpacing = 3;

    [Header("Edificios especiales")]
    public int specialAttempts = 50;
    public int minCellDistanceBetweenSpecials = 4;
    public int maxAccessPathLength = 12;

    [Header("Edificios normales")]
    [Range(0f, 1f)] public float normalBuildingDensity = 0.85f;
    [Tooltip("Permite edificios sin calle al lado (solo prefabs sin CitySocket)")]
    public bool allowInteriorBuildings = false;
}

// Prefabs de carretera agrupados para pasarlos al instanciador
public struct RoadPrefabs
{
    public GameObject forward, angle, t, x, deadEnd, pavement;
}
