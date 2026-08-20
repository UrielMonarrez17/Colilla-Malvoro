using System.Collections.Generic;
using UnityEngine;

public class LootContainer : MonoBehaviour
{
    [Header("Configuración del Loot")]
    [Tooltip("Añade aquí los ítems y qué porcentaje tienen de salir.")]
    public List<LootDrop> posiblesLoot;

    private bool yaFueSaqueado = false;

    // Esta función la llamaremos desde tu HamsterController cuando presiones la 'F'
    public void InteractuarConLoot()
    {
        if (yaFueSaqueado)
        {
            Debug.Log("Este contenedor ya está vacío.");
            return;
        }

        ItemType itemObtenido = CalcularDrop();
        
        // ¡Magia del Singleton! Llamamos al mánager directamente sin referencias
        InventoryManager.Instance.AddItem(itemObtenido, 1); 
        
        yaFueSaqueado = true;
        
        // Cambiamos el color para dar feedback visual de que ya está vacío
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.gray; 
        }
    }

    private ItemType CalcularDrop()
    {
        // 1. Sumamos todos los porcentajes (Idealmente deberían sumar 100, pero esto evita errores si suman 90 o 110)
        float totalPeso = 0f;
        foreach (LootDrop loot in posiblesLoot)
        {
            totalPeso += loot.dropChance;
        }

        // 2. Tiramos un dado virtual entre 0 y el peso total
        float numeroAleatorio = Random.Range(0f, totalPeso);

        // 3. Revisamos en qué "rango" cayó el dado
        foreach (LootDrop loot in posiblesLoot)
        {
            if (numeroAleatorio <= loot.dropChance)
            {
                return loot.item; // ¡Encontramos el ítem ganador!
            }
            // Si no fue este, le restamos su probabilidad al número y pasamos al siguiente
            numeroAleatorio -= loot.dropChance; 
        }

        // Por seguridad, si algo falla (que no debería), regresamos basura por defecto
        return ItemType.Desperdicios; 
    }
}