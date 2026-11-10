using UnityEngine;

// Materiales básicos de recolección
public enum ItemType
{
    Madera,
    Metal,
    Desperdicios,
    Semillas,
    Goma
}

// Objetos crafteables y usables
public enum TipoTrampa 
{ 
    Ninguna, 
    Puas, 
    Red, 
    Madera 
}

[System.Serializable]
public struct LootDrop
{
    public ItemType item;
    
    [Range(0f, 100f)]
    public float dropChance; 
}