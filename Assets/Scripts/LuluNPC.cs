using UnityEngine;
using TMPro; // NUEVO: Importamos TextMesh Pro
using System.Collections;

public class LuluNPC : MonoBehaviour
{
    [Header("UI de Diálogo")]
    public GameObject panelDialogo; 
    public TextMeshProUGUI textoDialogo; // CAMBIO: Usamos TextMeshProUGUI

    [Header("Misión")]
    public int desperdiciosRequeridos = 3;

    private bool hablando = false;

    void Start()
    {
        if (panelDialogo != null) panelDialogo.SetActive(false);
    }

    public void InteractuarConLulu()
    {
        if (hablando) return; 

        var estado = TutorialPipelineManager.Instance.currentState;

        if (estado == TutorialPipelineManager.TutorialState.BuscarLulu)
        {
            StartCoroutine(MostrarDialogo("¡Hola nuevecito! ¿Me ayudas? Necesito que busques en los basureros y me traigas 3 Desperdicios."));
            TutorialPipelineManager.Instance.AvanzarEstado(
                TutorialPipelineManager.TutorialState.RecolectarLoot, // En este paso ya le pedimos que regrese cuando los tenga
                "Recolecta 3 Desperdicios (0/3) y dáselos a Lulu"
            );
        }
        else if (estado == TutorialPipelineManager.TutorialState.RegresarLulu)
        {
            if (InventoryManager.Instance.HasEnoughItem(ItemType.Desperdicios, desperdiciosRequeridos))
            {
                InventoryManager.Instance.RemoveItem(ItemType.Desperdicios, desperdiciosRequeridos);
                StartCoroutine(MostrarDialogo("¡Muchas gracias! Ya casi anochece, ve a prepararte."));
                
                TutorialPipelineManager.Instance.AvanzarEstado(
                    TutorialPipelineManager.TutorialState.FinTutorial, 
                    "¡Sobrevive a la noche!"
                );
            }
            else
            {
                StartCoroutine(MostrarDialogo($"Aún te faltan desperdicios. Necesito {desperdiciosRequeridos}."));
            }
        }
    }

    private IEnumerator MostrarDialogo(string mensaje)
    {
        hablando = true;
        panelDialogo.SetActive(true);
        textoDialogo.text = mensaje;

        yield return new WaitForSeconds(0.5f); 
        
        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.F))
        {
            yield return null;
        }

        panelDialogo.SetActive(false);
        hablando = false;
    }
}