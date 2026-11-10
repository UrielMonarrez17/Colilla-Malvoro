using System;
using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("UI de la Mesa")]
    public GameObject craftingPanelUI;

    private bool isMenuOpen = false;
    public bool IsMenuOpen => isMenuOpen;

    public static event Action<CraftingTable, bool> OnMenuToggled;
    public static event Action<TipoTrampa> OnTrapCrafted;

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
        if (craftingPanelUI != null)
        {
            craftingPanelUI.SetActive(isMenuOpen);
        }
        OnMenuToggled?.Invoke(this, isMenuOpen);
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        if (craftingPanelUI != null)
        {
            craftingPanelUI.SetActive(false);
        }
        OnMenuToggled?.Invoke(this, false);
    }

    // --- LÓGICA DE CRAFTEO ---

    public void CraftTrap1()
    {
        if (InventoryManager.Instance == null) return;

        // Trampa de Púas: 2 Metal, 1 Madera
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Metal, 2) && 
            InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 1))
        {
            // Descontamos los materiales
            InventoryManager.Instance.RemoveItem(ItemType.Metal, 2);
            InventoryManager.Instance.RemoveItem(ItemType.Madera, 1);
            
            // Añadimos la trampa al inventario
            InventoryManager.Instance.AddTrampa(TipoTrampa.Puas);
            Debug.Log("✅ ¡Trampa de Púas crafteada con éxito!");
            OnTrapCrafted?.Invoke(TipoTrampa.Puas);
            if (Prototipo3.Narrative.QuestCompiler.Instance != null)
            {
                Prototipo3.Narrative.QuestCompiler.Instance.AdvanceActionIfMatches("fabricar_trampa_puas");
            }
        }
        else
        {
            Debug.Log("❌ Faltan materiales. Necesitas: 2 Metal, 1 Madera.");
        }
    }

    public void CraftTrap2()
    {
        if (InventoryManager.Instance == null) return;

        // Trampa de Red: 1 Goma, 1 Desperdicios
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Goma, 1) && 
            InventoryManager.Instance.HasEnoughItem(ItemType.Desperdicios, 1))
        {
            InventoryManager.Instance.RemoveItem(ItemType.Goma, 1);
            InventoryManager.Instance.RemoveItem(ItemType.Desperdicios, 1);
            
            InventoryManager.Instance.AddTrampa(TipoTrampa.Red);
            Debug.Log("✅ ¡Trampa de Red crafteada con éxito!");
            OnTrapCrafted?.Invoke(TipoTrampa.Red);
            if (Prototipo3.Narrative.QuestCompiler.Instance != null)
            {
                Prototipo3.Narrative.QuestCompiler.Instance.AdvanceActionIfMatches("fabricar_trampa_red");
            }
        }
        else
        {
            Debug.Log("❌ Faltan materiales. Necesitas: 1 Goma, 1 Desperdicio.");
        }
    }

    public void CraftTrap3()
    {
        if (InventoryManager.Instance == null) return;

        // Trampa de Madera: 2 Madera
        if (InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 2))
        {
            InventoryManager.Instance.RemoveItem(ItemType.Madera, 2);
            
            InventoryManager.Instance.AddTrampa(TipoTrampa.Madera);
            Debug.Log("✅ ¡Trampa de Madera crafteada con éxito!");
            OnTrapCrafted?.Invoke(TipoTrampa.Madera);
            if (Prototipo3.Narrative.QuestCompiler.Instance != null)
            {
                Prototipo3.Narrative.QuestCompiler.Instance.AdvanceActionIfMatches("fabricar_trampa_madera");
            }
        }
        else
        {
            Debug.Log("❌ Faltan materiales. Necesitas: 2 Madera.");
        }
    }
}
