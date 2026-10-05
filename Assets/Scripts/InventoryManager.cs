using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Singleton: Instancia estática para acceder fácilmente desde cualquier script
    public static InventoryManager Instance { get; private set; }

    // --- INVENTARIOS ---
    // Diccionario para materiales básicos
    private Dictionary<ItemType, int> inventory = new Dictionary<ItemType, int>();
    
    // Diccionario para las trampas construidas
    public Dictionary<TipoTrampa, int> trampasInventory = new Dictionary<TipoTrampa, int>()
    {
        { TipoTrampa.Puas, 0 },
        { TipoTrampa.Red, 0 },
        { TipoTrampa.Madera, 0 }
    };

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

        // Inicializamos el inventario de materiales en 0 al empezar.
        foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
        {
            inventory.Add(item, 0);
        }
    }

    // --- MÉTODOS DE MATERIALES BÁSICOS ---

    public void AddItem(ItemType item, int amount = 1)
    {
        inventory[item] += amount;
        Debug.Log($"[Inventario] Añadido +{amount} {item}. Total: {inventory[item]}");
    }

    public bool HasEnoughItem(ItemType item, int amount)
    {
        return inventory[item] >= amount;
    }

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

    // --- MÉTODOS DE TRAMPAS ---

    public void AddTrampa(TipoTrampa tipo)
    {
        trampasInventory[tipo]++;
        Debug.Log($"[Inventario] Trampa añadida: {tipo}. Total: {trampasInventory[tipo]}");
    }

    public bool HasTrampa(TipoTrampa tipo)
    {
        return trampasInventory[tipo] > 0;
    }

    public void RemoveTrampa(TipoTrampa tipo)
    {
        if (HasTrampa(tipo))
        {
            trampasInventory[tipo]--;
            Debug.Log($"[Inventario] Trampa usada: {tipo}. Restantes: {trampasInventory[tipo]}");
        }
    }

    // --- HERRAMIENTAS DE PRUEBA ---

    void Update()
    {
        // Presiona 'I' para imprimir todo tu inventario en la consola
        if (Input.GetKeyDown(KeyCode.I))
        {
            DebugPrintInventory();
        }
    }

    public void DebugPrintInventory()
    {
        string output = "--- MOCHILA DEL HÁMSTER ---\n";
        
        output += "MATERIALES:\n";
        foreach (var kvp in inventory)
        {
            output += $"- {kvp.Key}: {kvp.Value}\n";
        }

        output += "TRAMPAS:\n";
        foreach (var kvp in trampasInventory)
        {
            output += $"- {kvp.Key}: {kvp.Value}\n";
        }
        
        Debug.Log(output);
    }
}