using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyOnCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Durante el día, el enemigo es dócil y no ataca al jugador
        if (TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Day)
        {
            return;
        }

        // Comprobar si chocamos con el hámster (usando Tag o GetComponent)
        HamsterController hámster = collision.gameObject.GetComponent<HamsterController>();
        if (hámster != null)
        {
            // Si el objeto ya cuenta con EnemyAI, dicho componente maneja el ataque y su cooldown
            if (TryGetComponent<EnemyAI>(out var ai))
            {
                return;
            }

            // Activamos la lógica de daño en el hámster (fallback si no tiene EnemyAI)
            hámster.ReceiveDamage();
        }
    }
}