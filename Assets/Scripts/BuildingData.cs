using UnityEngine;

public class BuildingData : MonoBehaviour
{
    [Header("Dimensiones en la Malla")]
    [Tooltip("Número de celdas a lo ancho (Eje X)")]
    public int gridWidth = 1;

    [Tooltip("Número de celdas a lo largo (Eje Z)")]
    public int gridLength = 1;

    // Calcula la posición centrada en el mundo 3D según el tamaño de celda
    public Vector3 GetCalculatedWorldPosition(int gridX, int gridY, float cellSize)
    {
        // Calculamos el centro exacto del área ocupada por las celdas
        float offsetX = (gridWidth * cellSize) / 2f;
        float offsetZ = (gridLength * cellSize) / 2f;

        float worldX = (gridX * cellSize) + offsetX;
        float worldZ = (gridY * cellSize) + offsetZ;

        return new Vector3(worldX, 0f, worldZ);
    }
}