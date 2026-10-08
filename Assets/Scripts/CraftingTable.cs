using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("UI de la Mesa")]
    public GameObject craftingPanelUI;

    private bool isMenuOpen = false;

    void Start()
    {
        if (craftingPanelUI != null)
        {
            craftingPanelUI.SetActive(false);
        }
    }

    public void ToggleCraftingMenu()
    {
        isMenuOpen = !isMenuOpen;
        craftingPanelUI.SetActive(isMenuOpen);
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        if (craftingPanelUI != null)
        {
            craftingPanelUI.SetActive(false);
        }
    }

    // --- LÓGICA DE CRAFTEO ---

    public void CraftTrap1()
    {
        // Trampa de Púas: 1 Madera, 1 Metal
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 1) && 
            InventoryManager.Instance.HasEnoughItem(ItemType.Metal, 1))
        {
            // Descontamos los materiales
            InventoryManager.Instance.RemoveItem(ItemType.Madera, 1);
            InventoryManager.Instance.RemoveItem(ItemType.Metal, 1);
            
            // Añadimos la trampa al inventario
            InventoryManager.Instance.AddTrampa(TipoTrampa.Puas);
            Debug.Log("✅ ¡Trampa de Púas crafteada con éxito!");
        }
        else
        {
            Debug.Log("❌ Faltan materiales. Necesitas: 1 Madera, 1 Metal.");
        }
    }

    public void CraftTrap2()
    {
        // Trampa de Red: 1 Goma, 1 Desperdicios
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Goma, 1) && 
            InventoryManager.Instance.HasEnoughItem(ItemType.Desperdicios, 1))
        {
            InventoryManager.Instance.RemoveItem(ItemType.Goma, 1);
            InventoryManager.Instance.RemoveItem(ItemType.Desperdicios, 1);
            
            InventoryManager.Instance.AddTrampa(TipoTrampa.Red);
            Debug.Log("✅ ¡Trampa de Red crafteada con éxito!");
        }
        else
        {
            Debug.Log("❌ Faltan materiales. Necesitas: 1 Goma, 1 Desperdicio.");
        }
    }

    public void CraftTrap3()
    {
        // Trampa de Madera: 2 Madera
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 2))
        {
            InventoryManager.Instance.RemoveItem(ItemType.Madera, 2);
            
            InventoryManager.Instance.AddTrampa(TipoTrampa.Madera);
            Debug.Log("✅ ¡Trampa de Madera crafteada con éxito!");
        }
        else
        {
            Debug.Log("❌ Faltan materiales. Necesitas: 2 Madera.");
        }
    }
}