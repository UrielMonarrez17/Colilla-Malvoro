using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [Tooltip("Arrastra aquí el Transform de tu hámster")]
    public Transform target;

    [Header("Ajustes de Cámara")]
    [Tooltip("Distancia entre la cámara y el hámster (X, Y, Z)")]
    public Vector3 offset = new Vector3(0f, 10f, -8f);

    [Tooltip("Qué tan suave sigue la cámara al jugador. Un valor más alto es más rígido.")]
    public float smoothSpeed = 8f;

    void LateUpdate()
    {
        // Si no hay objetivo asignado, no hacemos nada
        if (target == null) return;

        // Calculamos la posición a la que debe ir la cámara
        Vector3 desiredPosition = target.position + offset;

        // Movemos la cámara suavemente hacia la posición deseada usando Lerp
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Nota: NO usamos transform.rotation ni transform.LookAt aquí. 
        // De esta forma, la rotación de la cámara se queda exactamente como la dejaste en Unity, 
        // sin importar hacia dónde voltee o corra el hámster.
    }
} 