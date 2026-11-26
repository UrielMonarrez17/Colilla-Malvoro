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

    // --- LÓGICA DE CRAFTEO (Unificada con TrapSystemPrototipo3) ---

    public void CraftTrap1()
    {
        TryCraftViaProto3System(TipoTrampa.Puas);
    }

    public void CraftTrap2()
    {
        TryCraftViaProto3System(TipoTrampa.Red);
    }

    public void CraftTrap3()
    {
        TryCraftViaProto3System(TipoTrampa.Madera);
    }

    private void TryCraftViaProto3System(TipoTrampa tipo)
    {
        Prototipo3.TrapSystemPrototipo3.EnsureInstance();
        if (Prototipo3.TrapSystemPrototipo3.CraftTrap(tipo))
        {
            OnTrapCrafted?.Invoke(tipo);
        }
    }
}
