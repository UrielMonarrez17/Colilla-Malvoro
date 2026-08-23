using UnityEngine;

// Definimos todos los materiales base que el hámster puede recolectar
public enum ItemType
{
    Madera,
    Metal,
    Desperdicios,
    Semillas,
    Goma
}

// Esta estructura (Struct) nos permite emparejar un ítem con su probabilidad de salir
[System.Serializable]
public struct LootDrop
{
    public ItemType item;
    
    [Range(0f, 100f)]
    [Tooltip("Probabilidad de que salga este objeto (Ej. 75 para 75%)")]
    public float dropChance; 
}