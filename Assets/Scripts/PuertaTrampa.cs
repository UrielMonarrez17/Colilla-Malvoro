using UnityEngine;

public class PuertaTrampa : MonoBehaviour
{
    public TipoTrampa trampaActual = TipoTrampa.Ninguna;
    private Renderer rend;
    private Color colorOriginal;

    void Start()
    {
        // 1. Intenta buscar el Renderer en el propio objeto
        rend = GetComponent<Renderer>();
        
        // 2. Si no lo encuentra, lo busca en los objetos hijos (muy común en modelos importados)
        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }

        // 3. Si lo encontró, guarda el color. Si no, lanza un aviso (pero no un error fatal).
        if (rend != null)
        {
            colorOriginal = rend.material.color;
        }
        else
        {
            Debug.LogWarning($"El objeto {gameObject.name} no tiene un Renderer. Las trampas funcionarán, pero no cambiará de color.");
        }
    }

    // Llamado por el hámster para instalar la trampa
    public void InstalarTrampa(TipoTrampa nuevaTrampa)
    {
        if (trampaActual != TipoTrampa.Ninguna) return; // Ya hay una trampa

        trampaActual = nuevaTrampa;

        // Solo intentamos cambiar el color si existe el Renderer
        if (rend != null)
        {
            if (trampaActual == TipoTrampa.Puas) rend.material.color = Color.red;
            else if (trampaActual == TipoTrampa.Red) rend.material.color = Color.blue;
            else if (trampaActual == TipoTrampa.Madera) rend.material.color = new Color(0.6f, 0.3f, 0f); // Café
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si hay una trampa instalada y el que entra es un Enemigo
        if (trampaActual != TipoTrampa.Ninguna && other.CompareTag("Enemy"))
        {
            EnemyAI enemigo = other.GetComponent<EnemyAI>();
            if (enemigo != null)
            {
                // Aplicamos el efecto al enemigo
                enemigo.RecibirTrampa(trampaActual, transform.position);
                
                // Consumir la trampa
                trampaActual = TipoTrampa.Ninguna;
                
                // Restaurar la puerta a su estado normal (si tiene Renderer)
                if (rend != null)
                {
                    rend.material.color = colorOriginal;
                }
            }
        }
    }
}