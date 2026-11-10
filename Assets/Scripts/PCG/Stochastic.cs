using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Rule
{
    public string variable; // Qué letra busca (ej. "F")
    public string expansion; // En qué se convierte (ej. "F[+F]F")
    [Range(0, 1)] public float probability;
}

[Serializable]
public class StochasticData
{
    public MeshFilter meshFilter; // Para asignar el mesh generado
    public char axiom = 'F';
    public uint maxIterations = 3;
    public List<Rule> rules = new List<Rule>();
    public float stepSize = 0.5f;
    public float angle = 25f;
}

// Estructura para guardar la posición y rotación de la "tortuga" en la pila
public struct TurtleState
{
    public Vector3 position;
    public Quaternion rotation;
}

public class Stochastic : MonoBehaviour
{
    public MathPRNG mathPrng = new MathPRNG(12345);
    public List<StochasticData> stochastics = new List<StochasticData>();

    private void Start()
    {
        foreach (var data in stochastics)
        {
            if (data.rules.Count == 0 || data.meshFilter == null) continue;

            // 1. Generar la cadena de texto L-System
            string finalString = GenerateLSystemString(data);

            // 2. Construir el Mesh 3D a partir de la cadena
            Mesh proceduralMesh = CreateMeshFromLString(finalString, data);
            data.meshFilter.mesh = proceduralMesh;
        }
    }

    // Fase 1: Expansión de la cadena con reglas estocásticas
    private string GenerateLSystemString(StochasticData data)
    {
        string currentString = data.axiom.ToString();

        for (int i = 0; i < data.maxIterations; i++)
        {
            System.Text.StringBuilder nextString = new System.Text.StringBuilder();

            foreach (char c in currentString)
            {
                bool replaced = false;
                
                // Buscar reglas que apliquen para este carácter
                List<Rule> matchingRules = data.rules.FindAll(r => r.variable == c.ToString());

                if (matchingRules.Count > 0)
                {
                    // Elegir regla estocástica basada en probabilidad
                    double roll = mathPrng.NextProbability(2);
                    float cumulativeProbability = 0f;

                    foreach (var rule in matchingRules)
                    {
                        cumulativeProbability += rule.probability;
                        if (roll <= cumulativeProbability)
                        {
                            nextString.Append(rule.expansion);
                            replaced = true;
                            break;
                        }
                    }
                }

                // Si no hubo regla o no coincidió, se queda el carácter original
                if (!replaced)
                {
                    nextString.Append(c);
                }
            }

            currentString = nextString.ToString();
        }

        return currentString;
    }

    // Fase 2: Interpretación y Creación del Mesh 3D
    private Mesh CreateMeshFromLString(string lString, StochasticData data)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Stack<TurtleState> stack = new Stack<TurtleState>();

        Vector3 currentPos = Vector3.zero;
        Quaternion currentRot = Quaternion.identity;

        // Para simplificar, dibujaremos pequeños segmentos de línea como quads delgados o cilindros planos
        float thickness = 0.05f;

        foreach (char c in lString)
        {
            switch (c)
            {
                case 'F':
                case 'G':
                    Vector3 startPos = currentPos;
                    // Avanzar en la dirección actual de la tortuga
                    currentPos += currentRot * Vector3.up * data.stepSize;

                    // Crear un segmento simple (quad) entre startPos y currentPos
                    AddSegment(startPos, currentPos, thickness, vertices, triangles, uvs, currentRot);
                    break;

                case '+': // Rotar Yaw (Derecha)
                    currentRot *= Quaternion.Euler(0, data.angle, 0);
                    break;
                case '-': // Rotar Yaw (Izquierda)
                    currentRot *= Quaternion.Euler(0, -data.angle, 0);
                    break;
                case '&': // Rotar Pitch (Abajo)
                    currentRot *= Quaternion.Euler(data.angle, 0, 0);
                    break;
                case '^': // Rotar Pitch (Arriba)
                    currentRot *= Quaternion.Euler(-data.angle, 0, 0);
                    break;
                case '[': // Guardar estado
                    stack.Push(new TurtleState { position = currentPos, rotation = currentRot });
                    break;
                case ']': // Restaurar estado
                    if (stack.Count > 0)
                    {
                        TurtleState state = stack.Pop();
                        currentPos = state.position;
                        currentRot = state.rotation;
                    }
                    break;
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Función auxiliar para agregar un segmento geométrico (un quad plano) al mesh
    private void AddSegment(Vector3 start, Vector3 end, float width, List<Vector3> verts, List<int> tris, List<Vector2> uvs, Quaternion rot)
    {
        int startIndex = verts.Count;

        Vector3 dir = (end - start).normalized;
        Vector3 right = rot * Vector3.right * (width * 0.5f);

        // Definir 4 vértices para formar un pequeño rectángulo (segmento de tallo/pasto)
        
        verts.Add(start - right);
        verts.Add(start + right);
        verts.Add(end + right);
        verts.Add(end - right);

        // UVs básicos
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));

        // Dos triángulos que forman el quad
        tris.Add(startIndex + 0);
        tris.Add(startIndex + 2);
        tris.Add(startIndex + 1);

        tris.Add(startIndex + 0);
        tris.Add(startIndex + 1);
        tris.Add(startIndex + 2);
        
        tris.Add(startIndex + 0);
        tris.Add(startIndex + 3);
        tris.Add(startIndex + 2);
    }
}