using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Singleton: Creamos una instancia estática para acceder fácilmente desde cualquier script
    public static InventoryManager Instance { get; private set; }

    // Nuestro inventario: asocia cada material con su cantidad
    private Dictionary<ItemType, int> inventory = new Dictionary<ItemType, int>();

    void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Opcional: Esto hace que el inventario no se destruya al cambiar de escena
            DontDestroyOnLoad(gameObject); 
        }

        // Inicializamos el inventario. Le ponemos 0 a todos los materiales al empezar.
        foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
        {
            inventory.Add(item, 0);
        }
    }

    // Método para añadir ítems (se llamará desde el LootContainer)
    public void AddItem(ItemType item, int amount = 1)
    {
        inventory[item] += amount;
        Debug.Log($"[Inventario] Añadido +{amount} {item}. Total: {inventory[item]}");
        
        // TODO: Aquí luego llamaremos a la UI para actualizar los números en pantalla
    }

    // Método para comprobar si tenemos suficientes materiales (se llamará desde la Mesa de Crafteo)
    public bool HasEnoughItem(ItemType item, int amount)
    {
        return inventory[item] >= amount;
    }

    // Método para gastar materiales al construir algo
    public void RemoveItem(ItemType item, int amount)
    {
        if (HasEnoughItem(item, amount))
        {
            inventory[item] -= amount;
            Debug.Log($"[Inventario] Consumido -{amount} {item}. Restante: {inventory[item]}");
        }
        else
        {
            Debug.LogWarning($"[Inventario] Intento fallido de gastar {item}. No hay suficiente.");
        }
    }

    // Método de prueba para imprimir todo el inventario en la consola
    public void DebugPrintInventory()
    {
        string output = "--- MOCHILA DEL HÁMSTER ---\n";
        foreach (var kvp in inventory)
        {
            output += $"{kvp.Key}: {kvp.Value}\n";
        }
        Debug.Log(output);
    }

    void Update()
    {
        // Presiona 'I' para imprimir todo tu inventario en la consola
        if (Input.GetKeyDown(KeyCode.I))
        {
            DebugPrintInventory();
        }
    }
}