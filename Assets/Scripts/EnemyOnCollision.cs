using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyOnCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Comprobar si chocamos con el hámster (usando Tag o GetComponent)
        HamsterController hámster = collision.gameObject.GetComponent<HamsterController>();
        if (hámster != null)
        {
            // Activamos la lógica de daño en el hámster.
            hámster.ReceiveDamage();
            
            // Opcional: Aquí podrías reproducir un sonido de ataque del enemigo
            // o desactivar al enemigo por un segundo para que no te dé 3 golpes instantáneos.
        }
    }
}