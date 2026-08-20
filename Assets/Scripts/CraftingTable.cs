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

    // --- LÓGICA DE CRAFTEO (TESTEO) ---

    public void CraftTrap1()
    {
        // Requiere: 1 Madera, 1 Metal
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 1) && 
            InventoryManager.Instance.HasEnoughItem(ItemType.Metal, 1))
        {
            // Descontamos los materiales
            InventoryManager.Instance.RemoveItem(ItemType.Madera, 1);
            InventoryManager.Instance.RemoveItem(ItemType.Metal, 1);
            Debug.Log("✅ ¡Trampa 1 crafteada con éxito!");
        }
        else
        {
            Debug.Log("❌ Faltan materiales para la Trampa 1. Necesitas: 1 Madera, 1 Metal.");
        }
    }

    public void CraftTrap2()
    {
        // Requiere: 1 Goma, 1 Desperdicios
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Goma, 1) && 
            InventoryManager.Instance.HasEnoughItem(ItemType.Desperdicios, 1))
        {
            InventoryManager.Instance.RemoveItem(ItemType.Goma, 1);
            InventoryManager.Instance.RemoveItem(ItemType.Desperdicios, 1);
            Debug.Log("✅ ¡Trampa 2 crafteada con éxito!");
        }
        else
        {
            Debug.Log("❌ Faltan materiales para la Trampa 2. Necesitas: 1 Goma, 1 Desperdicio.");
        }
    }

    public void CraftTrap3()
    {
        // Requiere: 2 Madera
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 2))
        {
            InventoryManager.Instance.RemoveItem(ItemType.Madera, 2);
            Debug.Log("✅ ¡Trampa 3 crafteada con éxito!");
        }
        else
        {
            Debug.Log("❌ Faltan materiales para la Trampa 3. Necesitas: 2 Madera.");
        }
    }
}