using UnityEngine;

public class BuildingData : MonoBehaviour
{
    [Header("Dimensiones en la Malla")]
    [Tooltip("Número de celdas a lo ancho (Eje X)")]
    public int gridWidth = 1;

    [Tooltip("Número de celdas a lo largo (Eje Z)")]
    public int gridLength = 1;

    [Header("Ajuste de Altura")]
    [Tooltip("Offset vertical adicional si deseas elevar o bajar manualmente este edificio")]
    public float heightOffset = 0f;

    // Calcula la posición centrada en el mundo 3D manteniendo la coordenada Y del Prefab
    public Vector3 GetCalculatedWorldPosition(int gridX, int gridY, float cellSize, float originalPrefabY)
    {
        // Calculamos el centro exacto del área ocupada por las celdas en X y Z
        float offsetX = (gridWidth * cellSize) / 2f;
        float offsetZ = (gridLength * cellSize) / 2f;

        float worldX = (gridX * cellSize) + offsetX;
        float worldZ = (gridY * cellSize) + offsetZ;

        // Mantenemos la Y original del Prefab + el Offset personalizado de este edificio
        float worldY = originalPrefabY + heightOffset;

        return new Vector3(worldX, worldY, worldZ);
    }
}