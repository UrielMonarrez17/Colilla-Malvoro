using UnityEngine;
using TMPro; // NUEVO: Importamos TextMesh Pro

public class TutorialPipelineManager : MonoBehaviour
{
    public static TutorialPipelineManager Instance { get; private set; }

    public enum TutorialState
    {
        AprenderControles,
        BuscarLulu,
        RecolectarLoot,
        RegresarLulu,
        FinTutorial
    }

    [Header("Estado")]
    public TutorialState currentState = TutorialState.AprenderControles;

    [Header("UI Referencias")]
    public GameObject panelControlesWASD; 
    public TextMeshProUGUI textoMisionUI; // CAMBIO: Usamos TextMeshProUGUI
    public PunteroMision punteroFlecha; 

    [Header("Progreso de Controles")]
    private bool pulsoW, pulsoA, pulsoS, pulsoD, pulsoShift;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
void Start()
    {
        // Forzamos el encendido del panel de controles
        if (panelControlesWASD != null) 
        {
            panelControlesWASD.SetActive(true);
        }

        // Forzamos el encendido del texto de la misión
        if (textoMisionUI != null) 
        {
            textoMisionUI.gameObject.SetActive(true); 
            // Si el texto de misión tiene un panel de fondo, asegúrate de que esté encendido en Unity, 
            // o arrástralo también al script como una variable GameObject nueva.
            textoMisionUI.text = "Misión: Muévete y Corre";
        }

        // Apagamos la flecha al inicio
        if (punteroFlecha != null) 
        {
            punteroFlecha.gameObject.SetActive(false);
        }
    }

   void Update()
    {
        if (currentState == TutorialState.AprenderControles)
        {
            VerificarControles();
        }
        else if (currentState == TutorialState.RecolectarLoot)
        {
            ActualizarContadorLoot();
        }
    }


    private void ActualizarContadorLoot()
    {
        // 1. Leemos la cantidad exacta desde el inventario
        int desperdiciosActuales = InventoryManager.Instance.GetItemCount(ItemType.Desperdicios);
        
        // 2. Limitamos visualmente el número a 3 por si el jugador recoge basura extra
        int desperdiciosMostrados = Mathf.Clamp(desperdiciosActuales, 0, 3);
        
        // 3. Actualizamos el texto en pantalla en tiempo real
        textoMisionUI.text = $"Misión: Recolecta 3 Desperdicios ({desperdiciosMostrados}/3)";

        // 4. Transición automática: si ya juntó los 3, le indicamos que regrese
        if (desperdiciosActuales >= 3)
        {
            // Buscamos a Lulu (Asegúrate de que siga teniendo su Tag o busca por script)
            LuluNPC lulu = FindObjectOfType<LuluNPC>();
            Transform objetivoLulu = lulu != null ? lulu.transform : null;

            AvanzarEstado(
                TutorialState.RegresarLulu, 
                "¡Completado! Regresa a darle el loot a Lulu", 
                objetivoLulu
            );
        }
    }


    private void VerificarControles()
    {
        if (Input.GetKeyDown(KeyCode.W)) pulsoW = true;
        if (Input.GetKeyDown(KeyCode.A)) pulsoA = true;
        if (Input.GetKeyDown(KeyCode.S)) pulsoS = true;
        if (Input.GetKeyDown(KeyCode.D)) pulsoD = true;
        if (Input.GetKeyDown(KeyCode.LeftShift)) pulsoShift = true;

        if (pulsoW && pulsoA && pulsoS && pulsoD && pulsoShift)
        {
            AvanzarABuscarLulu();
        }
    }

    private void AvanzarABuscarLulu()
    {
        currentState = TutorialState.BuscarLulu;
        panelControlesWASD.SetActive(false); 
        textoMisionUI.text = "Misión: Encuentra a Lulu";
        
        if (punteroFlecha != null)
        {
            punteroFlecha.gameObject.SetActive(true);
            
            // CAMBIO: Buscamos a Lulu por su script en lugar de su Tag
            LuluNPC luluEnEscena = FindObjectOfType<LuluNPC>();
            if (luluEnEscena != null)
            {
                punteroFlecha.objetivo = luluEnEscena.transform;
            }
            else
            {
                Debug.LogWarning("No se encontró a LuluNPC en la escena.");
            }
        }
    }

    public void AvanzarEstado(TutorialState nuevoEstado, string textoMision, Transform nuevoObjetivo = null)
    {
        currentState = nuevoEstado;
        textoMisionUI.text = "Misión: " + textoMision;

        if (nuevoObjetivo != null && punteroFlecha != null)
        {
            punteroFlecha.objetivo = nuevoObjetivo;
            punteroFlecha.gameObject.SetActive(true);
        }
        else if (punteroFlecha != null)
        {
            punteroFlecha.gameObject.SetActive(false);
        }
    }
}