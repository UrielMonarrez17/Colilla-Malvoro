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
    public bool Hablando => hablando;

    void Start()
    {
        if (panelDialogo != null) panelDialogo.SetActive(false);
    }

    public void InteractuarConLulu()
    {
        if (hablando || TutorialPipelineManager.Instance == null) return; 

        var estado = TutorialPipelineManager.Instance.currentState;

        if (estado == TutorialPipelineManager.TutorialState.BuscarLulu)
        {
            StartCoroutine(MostrarDialogo(
                "Lulú: ¡Hola nuevecito! Qué bueno verte fuera de la madriguera.\n" +
                "¿Me ayudas antes de que caiga el sol? Busca en los contenedores de la calle y tráeme 3 Desperdicios.\n" +
                "[Presiona ESPACIO o F para continuar]"
            ));
            TutorialPipelineManager.Instance.NotificarDialogoInicialLulu();
        }
        else if (estado == TutorialPipelineManager.TutorialState.RegresarLulu)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasEnoughItem(ItemType.Desperdicios, desperdiciosRequeridos))
            {
                InventoryManager.Instance.RemoveItem(ItemType.Desperdicios, desperdiciosRequeridos);
                StartCoroutine(MostrarDialogoYAnochecer(
                    "Lulú: ¡Muchas gracias! Ten cuidado: el sol ya está cayendo y los animales de la ciudad se volverán ferales.\n" +
                    "¡Yo me voy a esconder ya mismo, corre de vuelta a tu madriguera!\n" +
                    "[Presiona ESPACIO o F para continuar]"
                ));
            }
            else
            {
                StartCoroutine(MostrarDialogo(
                    $"Lulú: Aún te faltan desperdicios. Necesito {desperdiciosRequeridos} de los contenedores de la calle.\n" +
                    "[Presiona ESPACIO o F para continuar]"
                ));
            }
        }
        else
        {
            StartCoroutine(MostrarDialogo(
                "Lulú: ¡No pierdas tiempo en la calle cuando empiece a oscurecer! Tu refugio está al oeste.\n" +
                "[Presiona ESPACIO o F para continuar]"
            ));
        }
    }

    private IEnumerator MostrarDialogo(string mensaje)
    {
        hablando = true;
        if (panelDialogo != null) panelDialogo.SetActive(true);
        if (textoDialogo != null) textoDialogo.text = mensaje;

        yield return new WaitForSeconds(0.35f); 
        
        while (!FeralInput.Pressed(KeyCode.Space) && !FeralInput.Pressed(KeyCode.F) && !Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        if (panelDialogo != null) panelDialogo.SetActive(false);
        hablando = false;
    }

    private IEnumerator MostrarDialogoYAnochecer(string mensaje)
    {
        yield return StartCoroutine(MostrarDialogo(mensaje));
        if (TutorialPipelineManager.Instance != null)
        {
            TutorialPipelineManager.Instance.NotificarEntregaLuluCompletada();
        }
    }
}
