using UnityEngine;

public class CitySocket : MonoBehaviour
{
    [Tooltip("El objeto Empty que representa la entrada o fachada principal del modelo.")]
    public Transform frontSocket;

    void OnDrawGizmosSelected()
    {
        // Esto dibujará una línea roja en el editor para que veas fácilmente hacia dónde mira el edificio
        if (frontSocket != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, frontSocket.position);
            Gizmos.DrawSphere(frontSocket.position, 0.5f);
        }
    }
}