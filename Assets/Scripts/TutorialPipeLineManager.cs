using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Video;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TutorialPipelineManager : MonoBehaviour
{
    public static TutorialPipelineManager Instance { get; private set; }

    public enum TutorialState
    {
        // --- FASE 1: DÍA 1 — MOVIMIENTO Y RECOLECCIÓN (LOOTING) ---
        AprenderControles,          // Base del Hámster (madriguera/zona segura) + WASD
        AprenderEstamina,           // Salida del callejón + Shift Izquierdo (Correr) + Resaltado de Estamina
        SaquearContenedorControl,   // Puerta de control bloqueada + Contenedor resplandeciente (Madera + Metal) -> gris + abre puerta
        BuscarLulu,                 // Exploración diurna en ciudad con ciudadanos pacíficos (hablar con Lulú)
        RecolectarLoot,             // Recolectar 3 Desperdicios en la ciudad
        RegresarLulu,               // Entregar a Lulú
        AnuncioAnochecer,           // Cambio gradual de iluminación + aviso UI + ciudadanos pacíficos se retiran/transforman

        // --- FASE 2: NOCHE 1 — TENSIÓN, EVASIÓN Y MECÁNICAS FÍSICAS ---
        CinematicaAparicionFeral,   // Iluminación decae drásticamente + audio amenazante + Animal Feral bloquea vía principal
        EscapeDash,                 // Persecución directa + Prompt Espacio (Dash) + Respuesta de Adrenalina si recibe 1 golpe
        SigiloEsquina,              // Doblar esquina para romper línea de visión + Mantener C (Agacharse) + Cono visual se reduce
        LlegadaRefugioAmanecer,     // Entrar al túnel/zona segura -> Ciclo avanza al Amanecer y enemigos vuelven a ser inofensivos

        // --- FASE 3: DÍA 2 — FABRICACIÓN (CRAFTING) Y TRAMPAS ---
        UsarMesaFabricacion,        // Guía a la mesa de trabajo de la base + Presionar F para abrir interfaz
        CrearPrimeraTrampa,         // Usar Madera y Metal del Día 1 para craftear Trampa 1 (Púas)
        ColocacionTacticaTrampa,    // Seleccionar (1) y colocar Trampa 1 con F en la entrada/pasillo estrecho del refugio
        DemostracionTrampaActiva,   // Demostración guiada de cómo la trampa frena a un intruso en el pasillo

        // --- FINAL ---
        FinTutorial
    }

    [Header("Estado Actual del Tutorial")]
    public TutorialState currentState = TutorialState.AprenderControles;

    [Header("UI Referencias (Existentes en Escena)")]
    public GameObject panelControlesWASD; 
    public TextMeshProUGUI textoMisionUI; 
    public PunteroMision punteroFlecha; 

    [Header("Cinemática Tigre (MP4)")]
    [Tooltip("Video del tigre amenazante con la leyenda ¡CORRE!. Si se deja vacío se carga Resources/Videos/TigreCorre.")]
    public VideoClip videoCinematicaTigre;

    [Header("Prefabs de Animales y Objetos (Auto-cargados o Asignables)")]
    public GameObject prefabEnemigoTigre;
    public GameObject prefabEnemigoPerro;
    public GameObject prefabCiudadanoGato;
    public GameObject prefabCiudadanoCiervo;
    public GameObject prefabPuertaControl;
    public GameObject prefabContenedorLoot;
    public VideoClip cinematicaAnochecer;

    [Header("Referencias de Gameplay (Auto-detectadas o Asignables)")]
    public HamsterController hamster;
    public TimeManager timeManager;
    public DayNightCycle dayNightCycle;
    public CraftingTable mesaCrafteoRefugio;
    public PuertaTrampa puertaTrampaRefugio;
    public LuluNPC luluNPC;

    private Camera mainCam;
    private CameraFollow cameraFollow;
    private LootContainer contenedorControlBrillante;
    private GameObject puertaControlCallejon;
    private Transform puntoRefugioSeguro;
    private Transform puntoEsquinaSigilo;
    private Transform puntoSalidaCallejon;

    // --- Actores del Tutorial ---
    private EnemyAI enemigoFeralPrincipal;
    private EnemyAI enemigoPatrullaSigilo;
    private List<GameObject> ciudadanosPacificos = new List<GameObject>();
    private List<LootContainer> contenedoresCiudad = new List<LootContainer>();

    // --- Progreso de Controles y Fases ---
    private bool pulsoW, pulsoA, pulsoS, pulsoD;
    private float tiempoCorriendoConShift = 0f;
    private bool contenedorControlSaqueado = false;
    private bool dashEjecutadoEnPersecucion = false;
    private float tiempoAgachadoEnSigilo = 0f;
    private bool menuCrafteoAbierto = false;
    private bool trampaInstaladaEnRefugio = false;
    private bool enCinematica = false;

    // --- Barreras y Candados de Secuencia Obligatoria ---
    private GameObject barreraSalidaMadriguera;
    private GameObject barreraSprintCallejon;
    private GameObject barreraEsquinaSigilo;
    private GameObject barreraEntradaRefugio;
    private List<Button> botonesRecetasSecundarias = new List<Button>();

    // --- Checkpoints para Respawn Seguro ---
    private Vector3 checkpointPosicion = new Vector3(225.0f, 0.85f, 246.0f);

    // --- UI Dinámica de Apoyo (HUD, Prompts, Resaltados, Banner de Fase) ---
    private Canvas tutorialHudCanvas;
    private TextMeshProUGUI textoFaseBanner;
    private TextMeshProUGUI textoSubtituloGuia;
    private TextMeshProUGUI textoPromptContextual;
    private TextMeshProUGUI textoRelojCiclo;
    private TextMeshProUGUI textoInventarioRapido;
    private TextMeshProUGUI textoEstadoSaludAdrenalina;
    private Image overlayPantalla;
    private TextMeshProUGUI textoCinematicaCorre;
    private Image marcoResaltadoEstamina;

    // --- Audio Procedural Atmosférico y SFX ---
    private AudioSource audioAmbienteSource;
    private AudioSource audioSfxSource;
    private AudioClip clipAmbienteDia;
    private AudioClip clipAmbienteNoche;
    private AudioClip clipSfxLoot;
    private AudioClip clipSfxAlertaFeral;
    private AudioClip clipSfxAdrenalina;
    private AudioClip clipSfxCrafteo;
    private AudioClip clipSfxAmanecer;

    // --- Coordenadas Clave dentro de la Ciudad Principal (cityManagerMath en Tutorial.unity) ---
    public static readonly Vector3 POS_MADRIGUERA_INICIO = new Vector3(225.0f, 0.85f, 246.0f);
    public static readonly Vector3 POS_MESA_REFUGIO      = new Vector3(222.3f, 0.55f, 244.5f);
    public static readonly Vector3 POS_PUERTA_TRAMPA_REF = new Vector3(225.0f, 0.10f, 237.5f);
    public static readonly Vector3 POS_SALIDA_CALLEJON   = new Vector3(225.0f, 0.45f, 235.0f);
    public static readonly Vector3 POS_PUERTA_CONTROL    = new Vector3(251.0f, 0.10f, 235.0f);
    public static readonly Vector3 POS_CONTENEDOR_BRILLO = new Vector3(246.34f, 0.35f, 237.60f);
    public static readonly Vector3 POS_LULU_CIUDAD       = new Vector3(265.0f, 0.15f, 248.0f);
    public static readonly Vector3 POS_VIA_FERAL_SPAWN   = new Vector3(255.0f, 0.15f, 242.0f);
    public static readonly Vector3 POS_ESQUINA_SIGILO    = new Vector3(275.0f, 0.35f, 226.0f);
    public static readonly Vector3 POS_PATRULLA_SIGILO   = new Vector3(262.0f, 0.15f, 226.5f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        CargarPrefabsSiFaltan();
        InicializarAudioProcedural();
    }

    void OnEnable()
    {
        LootContainer.OnContainerLooted += HandleContainerLooted;
        CraftingTable.OnMenuToggled += HandleCraftingMenuToggled;
        CraftingTable.OnTrapCrafted += HandleTrapCrafted;
        PuertaTrampa.OnTrapInstalledOnDoor += HandleTrapInstalledOnDoor;
    }

    void OnDisable()
    {
        LootContainer.OnContainerLooted -= HandleContainerLooted;
        CraftingTable.OnMenuToggled -= HandleCraftingMenuToggled;
        CraftingTable.OnTrapCrafted -= HandleTrapCrafted;
        PuertaTrampa.OnTrapInstalledOnDoor -= HandleTrapInstalledOnDoor;

        if (hamster != null)
        {
            hamster.OnDashUsed -= HandlePlayerDash;
            hamster.OnAdrenalineTriggered -= HandlePlayerAdrenaline;
            hamster.OnPlayerDied -= HandlePlayerDied;
        }
    }

    void Start()
    {
        PrepararEscenaYReferencias();
        ConstruirHudTutorial();

        // Si hay una cinemática de video inicial activa, sincronizar y ocultar toda la UI del tutorial
        CinematicaManager cinManager = FindAnyObjectByType<CinematicaManager>();
        if (cinManager != null && !cinManager.CinematicaTerminada)
        {
            enCinematica = true;
        }

        IniciarFase1Dia1();

        if (enCinematica)
        {
            EstablecerVisibilidadUIEnCinematica(true);
        }
    }

    // ===================================================================================
    //  PREPARACIÓN INTEGRAL DE LA ESCENA TUTORIAL
    // ===================================================================================

    private void CargarPrefabsSiFaltan()
    {
#if UNITY_EDITOR
        if (prefabEnemigoTigre == null)
            prefabEnemigoTigre = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Enemies/Tiger_001.prefab");
        if (prefabEnemigoPerro == null)
            prefabEnemigoPerro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Enemies/Dog_001.prefab");
        if (prefabCiudadanoGato == null)
        {
            prefabCiudadanoGato = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/Animals_FREE/Prefabs/Kitty_001.prefab");
            if (prefabCiudadanoGato == null)
                prefabCiudadanoGato = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab");
        }
        if (prefabCiudadanoCiervo == null)
        {
            prefabCiudadanoCiervo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/Animals_FREE/Prefabs/Deer_001.prefab");
            if (prefabCiudadanoCiervo == null)
                prefabCiudadanoCiervo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/ithappy/Animals_FREE/Prefabs/Deer_001.prefab");
        }
        if (prefabPuertaControl == null)
        {
            prefabPuertaControl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Doors/Door_1_Yellow Variant.prefab");
            if (prefabPuertaControl == null)
                prefabPuertaControl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Door.prefab");
        }
        if (prefabContenedorLoot == null)
        {
            prefabContenedorLoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/LootContainers/GarbageCollector.prefab");
            if (prefabContenedorLoot == null)
                prefabContenedorLoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Basura2.prefab");
        }
#endif
    }

    private void PrepararEscenaYReferencias()
    {
        // 1. Asegurar TimeManager y DayNightCycle (ausentes originalmente en Tutorial.unity)
        if (TimeManager.Instance == null)
        {
            GameObject tmObj = new GameObject("TimeManager_Tutorial");
            TimeManager tm = tmObj.AddComponent<TimeManager>();
            tm.pauseAutoAdvance = true;
            tm.SetExactHour(8.5f); // Día 1 por la mañana
            timeManager = tm;
        }
        else
        {
            TimeManager.Instance.pauseAutoAdvance = true;
            TimeManager.Instance.SetExactHour(8.5f);
            timeManager = TimeManager.Instance;
        }

        Light dirLight = FindDirectionalLight();
        if (dirLight != null)
        {
            dayNightCycle = dirLight.GetComponent<DayNightCycle>();
            if (dayNightCycle == null)
            {
                dayNightCycle = dirLight.gameObject.AddComponent<DayNightCycle>();
            }
        }

        // 2. Asegurar InventoryManager
        if (InventoryManager.Instance == null)
        {
            new GameObject("InventoryManager_Tutorial").AddComponent<InventoryManager>();
        }

        // 3. Desactivar generador procedural lejano si interfiere o consume recursos innecesarios
        CityGeneratorMathVersion pcg = FindAnyObjectByType<CityGeneratorMathVersion>();
        if (pcg != null)
        {
            // Desactivamos los cubos Enemy primitivos que estaban a cientos de metros o apuntando al hámster inactivo
            EnemyAI[] enemigosSueltos = FindObjectsByType<EnemyAI>();
            for (int i = 0; i < enemigosSueltos.Length; i++)
            {
                if (enemigosSueltos[i] != null && enemigosSueltos[i].name.StartsWith("Enemy"))
                {
                    enemigosSueltos[i].gameObject.SetActive(false);
                }
            }
        }

        // 4. Localizar al Hámster activo (Mesh_Hamster)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && playerObj.activeInHierarchy)
        {
            hamster = playerObj.GetComponent<HamsterController>();
        }
        if (hamster == null)
        {
            HamsterController[] allHamsters = FindObjectsByType<HamsterController>();
            for (int i = 0; i < allHamsters.Length; i++)
            {
                if (allHamsters[i].gameObject.activeInHierarchy)
                {
                    hamster = allHamsters[i];
                    break;
                }
            }
        }

        if (hamster != null)
        {
            hamster.OnDashUsed -= HandlePlayerDash;
            hamster.OnDashUsed += HandlePlayerDash;
            hamster.OnAdrenalineTriggered -= HandlePlayerAdrenaline;
            hamster.OnAdrenalineTriggered += HandlePlayerAdrenaline;
            hamster.OnPlayerDied -= HandlePlayerDied;
            hamster.OnPlayerDied += HandlePlayerDied;
            hamster.RespawnOrHeal(POS_MADRIGUERA_INICIO);
            hamster.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraFollow = mainCam.GetComponent<CameraFollow>();
            Vector3 offsetCam = cameraFollow != null ? cameraFollow.offset : new Vector3(0f, 11f, -9f);
            mainCam.transform.position = POS_MADRIGUERA_INICIO + offsetCam;
        }

        // 5. Construir la Madriguera / Zona Segura (Refugio del Hámster) y el Callejón Inicial dentro de la ciudad
        ConstruirArquitecturaRefugioYCallejon();

        // 6. Reubicar y configurar la Mesa de Fabricación (CraftTable) dentro del Refugio
        if (mesaCrafteoRefugio == null)
            mesaCrafteoRefugio = FindAnyObjectByType<CraftingTable>();
        if (mesaCrafteoRefugio != null)
        {
            mesaCrafteoRefugio.transform.position = POS_MESA_REFUGIO;
            mesaCrafteoRefugio.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            mesaCrafteoRefugio.gameObject.tag = "Interactable";
            DecorarMesaDeTrabajo(mesaCrafteoRefugio.transform);
            CachearBotonesSecundariosMesa();
        }

        // 7. Reubicar y configurar la PuertaTrampa en el pasillo estrecho de entrada al Refugio
        if (puertaTrampaRefugio == null)
            puertaTrampaRefugio = FindAnyObjectByType<PuertaTrampa>();
        if (puertaTrampaRefugio != null)
        {
            Transform rootDoor = puertaTrampaRefugio.transform.root;
            rootDoor.position = POS_PUERTA_TRAMPA_REF;
            rootDoor.rotation = Quaternion.Euler(0f, 0f, 0f);
            puertaTrampaRefugio.gameObject.tag = "Door";
            // Asegurarnos de que empiece SIN trampa instalada para que el jugador la coloque en la Fase 3
            puertaTrampaRefugio.LimpiarTrampa();
        }

        if (luluNPC == null)
            luluNPC = FindAnyObjectByType<LuluNPC>();
        if (luluNPC != null)
        {
            luluNPC.transform.position = POS_LULU_CIUDAD;
            luluNPC.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            luluNPC.gameObject.tag = "Interactable";
        }

        // 8. Crear la Puerta de Control del Callejón y el Contenedor Resplandeciente (Fase 1)
        CrearZonaControlYContenedorResplandeciente();

        // 9. Acomodar Contenedores de Basura en la Ciudad y Ciudadanos Pacíficos (Día 1)
        CrearContenedoresYCiudadanosDia1();
    }

    private Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional) return lights[i];
        }
        return null;
    }

    public void ConstruirArquitecturaRefugioYCallejon()
    {
        GameObject existingEnv = GameObject.Find("Tutorial_EntornoGuiado");
        if (existingEnv != null)
        {
            puntoRefugioSeguro = existingEnv.transform.Find("Waypoint_RefugioSeguro");
            puntoSalidaCallejon = existingEnv.transform.Find("Waypoint_SalidaCallejon");
            puntoEsquinaSigilo = existingEnv.transform.Find("Waypoint_EsquinaSigilo");
            return;
        }

        GameObject rootEnv = new GameObject("Tutorial_EntornoGuiado");
        GameObject refugioWp = new GameObject("Waypoint_RefugioSeguro");
        refugioWp.transform.SetParent(rootEnv.transform, false);
        refugioWp.transform.position = new Vector3(225.0f, 0.5f, 244.5f);
        puntoRefugioSeguro = refugioWp.transform;

        GameObject salidaCallejonWp = new GameObject("Waypoint_SalidaCallejon");
        salidaCallejonWp.transform.SetParent(rootEnv.transform, false);
        salidaCallejonWp.transform.position = POS_SALIDA_CALLEJON;
        puntoSalidaCallejon = salidaCallejonWp.transform;

        GameObject esquinaWp = new GameObject("Waypoint_EsquinaSigilo");
        esquinaWp.transform.SetParent(rootEnv.transform, false);
        esquinaWp.transform.position = POS_ESQUINA_SIGILO;
        puntoEsquinaSigilo = esquinaWp.transform;
    }

    private void ConfigurarParedCafeEscena(string wallName, Vector3 pos, Vector3 scale, Material matCafe, Transform fallbackParent)
    {
        GameObject w = GameObject.Find(wallName);
        if (w == null)
        {
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                if (allTransforms[i] != null && allTransforms[i].name == wallName && allTransforms[i].gameObject.scene.IsValid())
                {
                    w = allTransforms[i].gameObject;
                    break;
                }
            }
        }

        if (w != null)
        {
            w.SetActive(true);
            w.transform.position = pos;
            w.transform.rotation = Quaternion.identity;
            w.transform.localScale = scale;
            Renderer r = w.GetComponent<Renderer>();
            if (r != null) r.material = matCafe;
        }
        else
        {
            CrearBloque3D(wallName, fallbackParent, pos, scale, matCafe, true);
        }
    }

    private GameObject CrearBloque3D(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat, bool conCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        go.transform.localScale = escala;
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.material = mat;
        if (!conCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
        return go;
    }

    private void DecorarMesaDeTrabajo(Transform mesa)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        // Añadimos una lámpara y herramientas sobre la mesa para que luzca como mesa de trabajo del refugio
        GameObject luzMesa = new GameObject("Luz_MesaTrabajo");
        luzMesa.transform.SetParent(mesa, false);
        luzMesa.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        Light l = luzMesa.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.4f, 0.85f, 1f);
        l.range = 5f;
        l.intensity = 2f;
    }

    private void CrearZonaControlYContenedorResplandeciente()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        // 1. Puerta / Barrera de Control que bloquea temporalmente la salida hacia la ciudad
        puertaControlCallejon = GameObject.Find("PuertaControl_Ciudad");
        if (puertaControlCallejon == null)
        {
            puertaControlCallejon = new GameObject("PuertaControl_Ciudad");
            puertaControlCallejon.transform.position = POS_PUERTA_CONTROL;
            puertaControlCallejon.transform.rotation = Quaternion.identity;
        }

        // Si ya está configurada visualmente en la escena, no sobreescribir ni duplicar hijos
        Transform existingReja = puertaControlCallejon.transform.Find("RejaControl");
        if (existingReja == null)
        {
            GameObject barrierPrefab = null;
#if UNITY_EDITOR
            barrierPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Control Barrier Fence.prefab");
#endif
            if (barrierPrefab != null)
            {
                GameObject b1 = Instantiate(barrierPrefab, puertaControlCallejon.transform);
                b1.name = "RejaControl";
                b1.transform.localPosition = Vector3.zero;
                b1.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                BoxCollider bc = b1.GetComponent<BoxCollider>();
                if (bc == null) bc = b1.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.2f, 0f);
                bc.size = new Vector3(8.5f, 2.5f, 0.6f);
            }
        }

        // 2. Contenedor Resplandeciente al lado de la puerta de control (reutiliza "GarbageCollector" de la escena si existe)
        GameObject existingCont = GameObject.Find("GarbageCollector");
        if (existingCont != null && existingCont.GetComponent<LootContainer>() != null)
        {
            existingCont.transform.position = POS_CONTENEDOR_BRILLO;
            existingCont.transform.rotation = Quaternion.identity;
            existingCont.tag = "Interactable";
            contenedorControlBrillante = existingCont.GetComponent<LootContainer>();
        }
        else if (prefabContenedorLoot != null)
        {
            GameObject contObj = Instantiate(prefabContenedorLoot, POS_CONTENEDOR_BRILLO, Quaternion.identity);
            contObj.name = "ContenedorResplandeciente_Tutorial";
            contObj.tag = "Interactable";
            contObj.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            contenedorControlBrillante = contObj.GetComponent<LootContainer>();
        }
        else
        {
            GameObject contObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            contObj.name = "ContenedorResplandeciente_Tutorial";
            contObj.tag = "Interactable";
            contObj.transform.position = POS_CONTENEDOR_BRILLO;
            contObj.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            BoxCollider bc = contObj.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(4f, 4f, 4f);
            contenedorControlBrillante = contObj.AddComponent<LootContainer>();
        }

        // Configurar para que otorgue Madera y Metal garantizados, brille y desbloquee la puerta al saquearse
        contenedorControlBrillante.itemsGarantizados = new List<ItemType> { ItemType.Madera, ItemType.Metal };
        contenedorControlBrillante.puertaDesbloqueable = puertaControlCallejon;
        contenedorControlBrillante.ConfigurarEfectoResplandor();
    }

    private void CrearContenedoresYCiudadanosDia1()
    {
        // Posiciones en el circuito de calles de la ciudad hacia Lulú (Lulú está en X=45.0, Z=148.0)
        Vector3[] posicionesLootCiudad = new Vector3[]
        {
            new Vector3(255.0f, 0.35f, 235.0f),
            new Vector3(265.0f, 0.35f, 222.2f),
            new Vector3(275.0f, 0.35f, 235.0f),
            new Vector3(272.5f, 0.35f, 245.0f)
        };

        string[] nombresExistentes = new string[]
        {
            "GarbageCollector (4)",
            "GarbageCollector (5)",
            "GarbageCollector (8)",
            "GarbageCollector (2)"
        };

        for (int i = 0; i < posicionesLootCiudad.Length; i++)
        {
            GameObject cObj = GameObject.Find(nombresExistentes[i]);
            if (cObj != null && cObj.GetComponent<LootContainer>() != null)
            {
                cObj.transform.position = posicionesLootCiudad[i];
                cObj.transform.rotation = Quaternion.identity;
            }
            else if (prefabContenedorLoot != null)
            {
                cObj = Instantiate(prefabContenedorLoot, posicionesLootCiudad[i], Quaternion.identity);
            }
            else
            {
                cObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cObj.transform.position = posicionesLootCiudad[i];
                cObj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                BoxCollider bc = cObj.GetComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(4f, 4f, 4f);
                cObj.AddComponent<LootContainer>();
            }

            cObj.name = $"ContenedorCiudad_{i + 1}";
            cObj.tag = "Interactable";
            LootContainer lc = cObj.GetComponent<LootContainer>();
            if (i < 3)
            {
                lc.itemsGarantizados = new List<ItemType> { ItemType.Desperdicios };
            }
            else
            {
                lc.itemsGarantizados = new List<ItemType> { ItemType.Desperdicios, ItemType.Goma };
            }
            contenedoresCiudad.Add(lc);
        }

        // Ciudadanos pacíficos en la ciudad que pasean de día y se retiran/transforman al anochecer
        Vector3[] posCiudadanos = new Vector3[]
        {
            new Vector3(255.0f, 0.15f, 228.0f),
            new Vector3(261.5f, 0.15f, 245.0f),
            new Vector3(275.0f, 0.15f, 228.0f)
        };

        for (int i = 0; i < posCiudadanos.Length; i++)
        {
            GameObject prefabToUse = (i % 2 == 0 && prefabCiudadanoCiervo != null) ? prefabCiudadanoCiervo : prefabCiudadanoGato;
            if (prefabToUse != null)
            {
                GameObject cit = Instantiate(prefabToUse, posCiudadanos[i], Quaternion.Euler(0f, 90f + i * 45f, 0f));
                cit.name = $"CiudadanoPacifico_{i + 1}";
                // Desactivar scripts de input del paquete Animals_FREE
                MonoBehaviour[] comps = cit.GetComponents<MonoBehaviour>();
                for (int c = 0; c < comps.Length; c++)
                {
                    if (comps[c] != null) comps[c].enabled = false;
                }
                CharacterController cc = cit.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                ciudadanosPacificos.Add(cit);
            }
        }
    }

    // ===================================================================================
    //  CONSTRUCCIÓN DEL HUD DEL TUTORIAL Y RETROALIMENTACIÓN VISUAL (GDD)
    // ===================================================================================

    private void ConstruirHudTutorial()
    {
        Canvas existingCanvas = textoMisionUI != null ? textoMisionUI.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            tutorialHudCanvas = existingCanvas;
        }
        else
        {
            GameObject cObj = new GameObject("TutorialHUD_Canvas");
            tutorialHudCanvas = cObj.AddComponent<Canvas>();
            tutorialHudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cObj.AddComponent<CanvasScaler>();
            cObj.AddComponent<GraphicRaycaster>();
        }

        TMP_FontAsset font = textoMisionUI != null ? textoMisionUI.font : null;

        // 1. Overlay de pantalla completa (para parpadeo de Adrenalina, Anochecer, Amanecer)
        GameObject ovObj = new GameObject("Overlay_EfectosPantalla", typeof(RectTransform), typeof(Image));
        ovObj.transform.SetParent(tutorialHudCanvas.transform, false);
        RectTransform ovRect = ovObj.GetComponent<RectTransform>();
        ovRect.anchorMin = Vector2.zero;
        ovRect.anchorMax = Vector2.one;
        ovRect.offsetMin = Vector2.zero;
        ovRect.offsetMax = Vector2.zero;
        overlayPantalla = ovObj.GetComponent<Image>();
        overlayPantalla.color = new Color(0f, 0f, 0f, 0f);
        overlayPantalla.raycastTarget = false;

        // 2. Banner Superior de Fase y Subtítulo Pedagógico
        GameObject bannerPanel = new GameObject("Panel_FaseTutorial", typeof(RectTransform), typeof(Image));
        bannerPanel.transform.SetParent(tutorialHudCanvas.transform, false);
        RectTransform bpRect = bannerPanel.GetComponent<RectTransform>();
        bpRect.anchorMin = new Vector2(0.5f, 1f);
        bpRect.anchorMax = new Vector2(0.5f, 1f);
        bpRect.pivot = new Vector2(0.5f, 1f);
        bpRect.anchoredPosition = new Vector2(0f, -12f);
        bpRect.sizeDelta = new Vector2(760f, 88f);
        Image bpImg = bannerPanel.GetComponent<Image>();
        bpImg.color = new Color(0.08f, 0.10f, 0.14f, 0.86f);
        bpImg.raycastTarget = false;

        textoFaseBanner = CrearTextoTMP("Texto_FaseBanner", bannerPanel.transform, font, 20, FontStyles.Bold, new Color(1f, 0.84f, 0.28f), new Vector2(0f, 22f), new Vector2(730f, 34f));
        textoSubtituloGuia = CrearTextoTMP("Texto_SubtituloGuia", bannerPanel.transform, font, 15, FontStyles.Normal, new Color(0.92f, 0.95f, 0.98f), new Vector2(0f, -16f), new Vector2(730f, 44f));

        // 3. Prompt Contextual de Interacción / Acción en la parte inferior central (ej. "Presiona [F] para saquear")
        GameObject promptPanel = new GameObject("Panel_PromptContextual", typeof(RectTransform), typeof(Image));
        promptPanel.transform.SetParent(tutorialHudCanvas.transform, false);
        RectTransform ppRect = promptPanel.GetComponent<RectTransform>();
        ppRect.anchorMin = new Vector2(0.5f, 0f);
        ppRect.anchorMax = new Vector2(0.5f, 0f);
        ppRect.pivot = new Vector2(0.5f, 0f);
        ppRect.anchoredPosition = new Vector2(0f, 32f);
        ppRect.sizeDelta = new Vector2(580f, 46f);
        Image ppImg = promptPanel.GetComponent<Image>();
        ppImg.color = new Color(0.10f, 0.14f, 0.18f, 0.90f);
        ppImg.raycastTarget = false;

        textoPromptContextual = CrearTextoTMP("Texto_PromptContextual", promptPanel.transform, font, 18, FontStyles.Bold, new Color(0.45f, 1f, 0.65f), Vector2.zero, new Vector2(560f, 40f));
        promptPanel.SetActive(false);

        // 4. Panel Superior Derecho: Reloj del Ciclo + Mochila Rápida + Estado Físico/Adrenalina
        GameObject statusPanel = new GameObject("Panel_EstadoGDD", typeof(RectTransform), typeof(Image));
        statusPanel.transform.SetParent(tutorialHudCanvas.transform, false);
        RectTransform spRect = statusPanel.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(1f, 1f);
        spRect.anchorMax = new Vector2(1f, 1f);
        spRect.pivot = new Vector2(1f, 1f);
        spRect.anchoredPosition = new Vector2(-16f, -12f);
        spRect.sizeDelta = new Vector2(310f, 128f);
        Image spImg = statusPanel.GetComponent<Image>();
        spImg.color = new Color(0.08f, 0.10f, 0.14f, 0.84f);
        spImg.raycastTarget = false;

        textoRelojCiclo = CrearTextoTMP("Texto_RelojCiclo", statusPanel.transform, font, 16, FontStyles.Bold, new Color(1f, 0.9f, 0.4f), new Vector2(0f, 38f), new Vector2(290f, 28f));
        textoInventarioRapido = CrearTextoTMP("Texto_InventarioRapido", statusPanel.transform, font, 14, FontStyles.Normal, Color.white, new Vector2(0f, 2f), new Vector2(290f, 42f));
        textoEstadoSaludAdrenalina = CrearTextoTMP("Texto_SaludAdrenalina", statusPanel.transform, font, 14, FontStyles.Bold, new Color(0.5f, 0.95f, 0.55f), new Vector2(0f, -38f), new Vector2(290f, 28f));

        // 5. Marco de Resaltado para la Barra de Estamina (usado en Fase 1: Uso de la Estamina)
        if (hamster != null && hamster.staminaSlider != null)
        {
            GameObject highlightObj = new GameObject("Resaltado_BarraEstamina", typeof(RectTransform), typeof(Image));
            highlightObj.transform.SetParent(hamster.staminaSlider.transform, false);
            highlightObj.transform.SetAsFirstSibling();
            RectTransform hlRect = highlightObj.GetComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.offsetMin = new Vector2(-8f, -8f);
            hlRect.offsetMax = new Vector2(8f, 8f);
            marcoResaltadoEstamina = highlightObj.GetComponent<Image>();
            marcoResaltadoEstamina.color = new Color(1f, 0.85f, 0.1f, 0f);
            marcoResaltadoEstamina.raycastTarget = false;
        }

        // 6. Leyenda Cinemática Intimidante: "¡CORRE!"
        textoCinematicaCorre = CrearTextoTMP("Texto_Cinematica_CORRE", tutorialHudCanvas.transform, font, 86, FontStyles.Bold, new Color(1f, 0.12f, 0.12f, 1f), new Vector2(0f, 30f), new Vector2(900f, 160f));
        textoCinematicaCorre.text = "¡ C O R R E !";
        textoCinematicaCorre.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CrearTextoTMP(string nombre, Transform padre, TMP_FontAsset font, float size, FontStyles style, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ===================================================================================
    //  FLUJO DE ESTADOS DE LAS 3 FASES DEL TUTORIAL
    // ===================================================================================

    private void IniciarFase1Dia1()
    {
        checkpointPosicion = POS_MADRIGUERA_INICIO;

        if (panelControlesWASD != null) panelControlesWASD.SetActive(true);
        if (punteroFlecha != null) punteroFlecha.gameObject.SetActive(false);

        ReproducirAmbiente(clipAmbienteDia);

        ConfigurarTextosEstado(
            TutorialState.AprenderControles,
            "FASE 1: DÍA 1 — MOVIMIENTO Y RECOLECCIÓN",
            "Misión: Desplázate por la madriguera usando [W] [A] [S] [D]",
            "Estás en la Base del Hámster (zona segura). Practica moverte en las 4 direcciones para salir al callejón.",
            null
        );
    }

    void Update()
    {
        // Rescate físico de emergencia ante caídas accidentales fuera del escenario
        if (hamster != null && hamster.transform.position.y < -0.6f)
        {
            hamster.RespawnOrHeal(checkpointPosicion);
        }

        AplicarRestriccionesSecuencialesEstrictas();
        ActualizarHudGDD();
        ActualizarPromptContextual();

        if (enCinematica) return;

        switch (currentState)
        {
            case TutorialState.AprenderControles:
                ActualizarPasoControlesWASD();
                break;

            case TutorialState.AprenderEstamina:
                ActualizarPasoEstamina();
                break;

            case TutorialState.SaquearContenedorControl:
                ActualizarPasoContenedorControl();
                break;

            case TutorialState.RecolectarLoot:
                ActualizarContadorLootCiudad();
                break;

            case TutorialState.EscapeDash:
                ActualizarPasoEscapeDash();
                break;

            case TutorialState.SigiloEsquina:
                ActualizarPasoSigiloEsquina();
                break;

            case TutorialState.LlegadaRefugioAmanecer:
                ActualizarPasoLlegadaRefugio();
                break;

            case TutorialState.ColocacionTacticaTrampa:
                ActualizarPasoColocacionTrampa();
                break;
        }
    }

    // --- FASE 1: LÓGICA DE PASOS ---

    private void ActualizarPasoControlesWASD()
    {
        if (FeralInput.Held(KeyCode.W) || FeralInput.Pressed(KeyCode.W)) pulsoW = true;
        if (FeralInput.Held(KeyCode.A) || FeralInput.Pressed(KeyCode.A)) pulsoA = true;
        if (FeralInput.Held(KeyCode.S) || FeralInput.Pressed(KeyCode.S)) pulsoS = true;
        if (FeralInput.Held(KeyCode.D) || FeralInput.Pressed(KeyCode.D)) pulsoD = true;

        string checkW = pulsoW ? "<color=#55FF55>[W]</color>" : "[W]";
        string checkA = pulsoA ? "<color=#55FF55>[A]</color>" : "[A]";
        string checkS = pulsoS ? "<color=#55FF55>[S]</color>" : "[S]";
        string checkD = pulsoD ? "<color=#55FF55>[D]</color>" : "[D]";

        if (textoMisionUI != null)
        {
            textoMisionUI.text = $"Misión: Muévete por la madriguera {checkW} {checkA} {checkS} {checkD}";
        }

        if (pulsoW && pulsoA && pulsoS && pulsoD)
        {
            IniciarPasoEstamina();
        }
    }

    private void IniciarPasoEstamina()
    {
        tiempoCorriendoConShift = 0f;
        ConfigurarTextosEstado(
            TutorialState.AprenderEstamina,
            "FASE 1: DÍA 1 — USO DE LA ESTAMINA (CARRERA)",
            "Misión: Mantén [Shift Izquierdo] mientras te mueves para correr por el callejón",
            "Observa tu barra de Estamina resaltada: correr consume energía y si llega a 0 quedarás agotado temporalmente.",
            puntoSalidaCallejon
        );
    }

    private void ActualizarPasoEstamina()
    {
        // Resaltar visualmente la barra de estamina con un pulso amarillo/naranja
        if (marcoResaltadoEstamina != null)
        {
            float alpha = 0.45f + 0.45f * Mathf.Sin(Time.time * 6f);
            marcoResaltadoEstamina.color = new Color(1f, 0.85f, 0.1f, alpha);
        }

        if (hamster != null && hamster.IsSprinting)
        {
            tiempoCorriendoConShift += Time.deltaTime;
        }

        float progreso = Mathf.Clamp01(tiempoCorriendoConShift / 1.2f) * 100f;
        if (textoMisionUI != null)
        {
            textoMisionUI.text = $"Misión: Mantén [Shift Izquierdo] para correr hacia la salida ({progreso:0}%)";
        }

        if (tiempoCorriendoConShift >= 1.2f)
        {
            if (marcoResaltadoEstamina != null)
            {
                marcoResaltadoEstamina.color = new Color(1f, 0.85f, 0.1f, 0f);
            }
            if (panelControlesWASD != null) panelControlesWASD.SetActive(false);

            IniciarPasoSaquearContenedorControl();
        }
    }

    private void IniciarPasoSaquearContenedorControl()
    {
        checkpointPosicion = POS_SALIDA_CALLEJON;
        Transform targetCont = contenedorControlBrillante != null ? contenedorControlBrillante.transform : null;

        ConfigurarTextosEstado(
            TutorialState.SaquearContenedorControl,
            "FASE 1: DÍA 1 — MECÁNICA DE LOOTING",
            "Misión: Acércate al contenedor resplandeciente y presiona [F] (Interactuar)",
            "El paso hacia la ciudad está bloqueado por una barrera de control. Saquea el contenedor brillante a su lado para obtener Madera y Metal.",
            targetCont
        );
    }

    private void ActualizarPasoContenedorControl()
    {
        if (contenedorControlSaqueado || (contenedorControlBrillante != null && contenedorControlBrillante.YaFueSaqueado))
        {
            AvanzarABuscarLulu();
        }
    }

    private void HandleContainerLooted(LootContainer container, List<ItemType> items)
    {
        ReproducirSfx(clipSfxLoot);

        if (container == contenedorControlBrillante)
        {
            contenedorControlSaqueado = true;
            if (currentState == TutorialState.SaquearContenedorControl)
            {
                AvanzarABuscarLulu();
            }
        }
    }

    private void AvanzarABuscarLulu()
    {
        LuluNPC luluEnEscena = FindAnyObjectByType<LuluNPC>();
        Transform objetivoLulu = luluEnEscena != null ? luluEnEscena.transform : null;

        if (objetivoLulu != null)
        {
            ConfigurarTextosEstado(
                TutorialState.BuscarLulu,
                "FASE 1: DÍA 1 — EXPLORACIÓN EN LA CIUDAD",
                "Misión: Puerta desbloqueada (+1 Madera, +1 Metal). Ve a hablar con Lulú [F]",
                "El contenedor cambió a gris indicando que ya fue saqueado. Aprovecha la luz del día para hablar con los ciudadanos pacíficos.",
                objetivoLulu
            );
        }
        else
        {
            StartCoroutine(SecuenciaAnuncioAnochecer());
        }
    }

    public void NotificarDialogoInicialLulu()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SetExactHour(14.5f); // Avanza la tarde
        }

        Transform primerContenedor = null;
        for (int i = 0; i < contenedoresCiudad.Count; i++)
        {
            if (contenedoresCiudad[i] != null && !contenedoresCiudad[i].YaFueSaqueado)
            {
                primerContenedor = contenedoresCiudad[i].transform;
                break;
            }
        }

        ConfigurarTextosEstado(
            TutorialState.RecolectarLoot,
            "FASE 1: DÍA 1 — RECOLECCIÓN URBANA",
            "Misión: Recolecta 3 Desperdicios en los basureros con [F] (0/3)",
            "Busca los contenedores en la calle principal y presiona [F] para saquearlos antes de que caiga la tarde.",
            primerContenedor
        );
    }

    private void ActualizarContadorLootCiudad()
    {
        if (InventoryManager.Instance == null) return;

        int desperdiciosActuales = InventoryManager.Instance.GetItemCount(ItemType.Desperdicios);
        int desperdiciosMostrados = Mathf.Clamp(desperdiciosActuales, 0, 3);

        if (textoMisionUI != null)
        {
            textoMisionUI.text = $"Misión: Recolecta 3 Desperdicios con [F] ({desperdiciosMostrados}/3)";
        }

        // Actualizar la flecha al contenedor no saqueado más cercano
        if (punteroFlecha != null && desperdiciosActuales < 3)
        {
            Transform masCercano = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < contenedoresCiudad.Count; i++)
            {
                if (contenedoresCiudad[i] != null && !contenedoresCiudad[i].YaFueSaqueado)
                {
                    float d = hamster != null ? Vector3.Distance(hamster.transform.position, contenedoresCiudad[i].transform.position) : 0f;
                    if (d < minDist)
                    {
                        minDist = d;
                        masCercano = contenedoresCiudad[i].transform;
                    }
                }
            }
            if (masCercano != null) punteroFlecha.objetivo = masCercano;
        }

        if (desperdiciosActuales >= 3)
        {
            LuluNPC lulu = FindAnyObjectByType<LuluNPC>();
            Transform objetivoLulu = lulu != null ? lulu.transform : null;

            ConfigurarTextosEstado(
                TutorialState.RegresarLulu,
                "FASE 1: DÍA 1 — EL SOL COMIENZA A CAER",
                "Misión: ¡Completado! Regresa con Lulú y presiona [F] para entregar el loot",
                "La tarde está avanzando rápidamente. Entrégale los Desperdicios a Lulú antes del anochecer.",
                objetivoLulu
            );

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetExactHour(16.8f); // Inicio del atardecer
            }
        }
    }

    public void NotificarEntregaLuluCompletada()
    {
        StartCoroutine(SecuenciaAnuncioAnochecer());
    }

    public void NotificarFinCinematicaInicial()
    {
        enCinematica = false;
        EstablecerVisibilidadUIEnCinematica(false);
    }

    private void EstablecerVisibilidadUIEnCinematica(bool enCursoCinematica)
    {
        bool mostrarUI = !enCursoCinematica;

        if (textoFaseBanner != null)
        {
            textoFaseBanner.gameObject.SetActive(mostrarUI);
            if (textoFaseBanner.transform.parent != null)
                textoFaseBanner.transform.parent.gameObject.SetActive(mostrarUI);
        }

        if (textoSubtituloGuia != null)
        {
            textoSubtituloGuia.gameObject.SetActive(mostrarUI);
        }

        if (textoRelojCiclo != null && textoRelojCiclo.transform.parent != null)
            textoRelojCiclo.transform.parent.gameObject.SetActive(mostrarUI);

        if (textoPromptContextual != null && textoPromptContextual.transform.parent != null && enCursoCinematica)
            textoPromptContextual.transform.parent.gameObject.SetActive(false);

        if (textoMisionUI != null)
            textoMisionUI.gameObject.SetActive(mostrarUI);

        if (hamster != null && hamster.staminaSlider != null)
            hamster.staminaSlider.gameObject.SetActive(mostrarUI);

        if (panelControlesWASD != null)
            panelControlesWASD.SetActive(mostrarUI && (currentState == TutorialState.AprenderControles || currentState == TutorialState.AprenderEstamina));

        if (punteroFlecha != null && enCursoCinematica)
            punteroFlecha.gameObject.SetActive(false);
    }

    private IEnumerator SecuenciaAnuncioAnochecer()
    {
        enCinematica = true;
        ConfigurarTextosEstado(
            TutorialState.AnuncioAnochecer,
            "FASE 1: ANUNCIO DEL ANOCHECER",
            "¡ADVERTENCIA! El sol se oculta. Los ciudadanos se retiran o se vuelven ferales...",
            "Observa el cambio gradual de iluminación: los habitantes pacíficos huyen a esconderse y la ciudad se vuelve hostil.",
            puntoRefugioSeguro
        );
        EstablecerVisibilidadUIEnCinematica(true);

        // Retirar a Lulú y a los ciudadanos pacíficos mientras la luz se torna rojiza (16.8h -> 18.0h)
        LuluNPC lulu = FindAnyObjectByType<LuluNPC>();
        float duracionAtardecer = 3.2f;
        float elapsed = 0f;

        while (elapsed < duracionAtardecer)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracionAtardecer;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetExactHour(Mathf.Lerp(16.8f, 18.05f, t));
            }

            if (overlayPantalla != null)
            {
                overlayPantalla.color = new Color(0.65f, 0.18f, 0.08f, Mathf.Sin(t * Mathf.PI) * 0.25f);
            }

            // Hacer que los ciudadanos pacíficos y Lulú corran a retirarse
            for (int i = 0; i < ciudadanosPacificos.Count; i++)
            {
                if (ciudadanosPacificos[i] != null)
                {
                    ciudadanosPacificos[i].transform.position += new Vector3(0f, 0f, 6f * Time.deltaTime);
                }
            }
            if (lulu != null)
            {
                lulu.transform.position += new Vector3(5f * Time.deltaTime, 0f, 3f * Time.deltaTime);
            }

            yield return null;
        }

        for (int i = 0; i < ciudadanosPacificos.Count; i++)
        {
            if (ciudadanosPacificos[i] != null) ciudadanosPacificos[i].SetActive(false);
        }
        if (lulu != null) lulu.gameObject.SetActive(false);
        if (overlayPantalla != null) overlayPantalla.color = Color.clear;

        // Pasar directamente a la Fase 2: Noche 1 — Cinemática de Aparición Feral
        yield return StartCoroutine(SecuenciaFase2CinematicaNoche());
    }

    // ===================================================================================
    //  FASE 2: NOCHE 1 — TENSIÓN, EVASIÓN, DASH, ADRENALINA Y SIGILO
    // ===================================================================================

    private IEnumerator SecuenciaFase2CinematicaNoche()
    {
        enCinematica = true;
        EstablecerVisibilidadUIEnCinematica(true);

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SetExactHour(21.0f); // Noche cerrada (decaimiento drástico de iluminación)
        }

        ReproducirAmbiente(clipAmbienteNoche);

        // Colocar al jugador en la vía norte
        if (hamster != null)
        {
            Vector3 posJugadorNoche = new Vector3(268.0f, 0.85f, 245.0f);
            hamster.RespawnOrHeal(posJugadorNoche);
            hamster.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            checkpointPosicion = posJugadorNoche;
            hamster.enabled = false;
        }

        // Instanciar al Depredador Feral (Tigre) en la vía principal bloqueando el retorno
        SpawnearEnemigosFase2();

        // --- CINEMÁTICA MP4: TIGRE AMENAZANTE MIRANDO A CÁMARA + LEYENDA "¡CORRE!" ---
        // Congelar al depredador mientras corre el video para que no ataque antes de la persecución
        NavMeshAgent agFeral = enemigoFeralPrincipal != null ? enemigoFeralPrincipal.GetComponent<NavMeshAgent>() : null;
        if (enemigoFeralPrincipal != null) enemigoFeralPrincipal.enabled = false;
        if (agFeral != null && agFeral.enabled && agFeral.isOnNavMesh) agFeral.isStopped = true;

        bool videoReproducido = false;
        yield return StartCoroutine(ReproducirVideoCinematicaTigre(ok => videoReproducido = ok));
        if (!videoReproducido && cinematicaAnochecer != null)
        {
            yield return StartCoroutine(ReproducirVideoAnochecer(ok => videoReproducido = ok));
        }

        // Respaldo si el clip no está disponible: destello rojo + leyenda "¡CORRE!" en HUD
        if (!videoReproducido)
        {
            ReproducirSfx(clipSfxAlertaFeral);
            if (textoCinematicaCorre != null) textoCinematicaCorre.gameObject.SetActive(true);
            float hold = 0f;
            while (hold < 2.2f)
            {
                hold += Time.deltaTime;
                if (overlayPantalla != null) overlayPantalla.color = new Color(0.85f, 0.05f, 0.05f, 0.22f + 0.15f * Mathf.Sin(hold * 8f));
                yield return null;
            }
            if (textoCinematicaCorre != null) textoCinematicaCorre.gameObject.SetActive(false);
            if (overlayPantalla != null) overlayPantalla.color = Color.clear;
        }

        // Reubicar la cámara detrás del hámster para arrancar la persecución
        if (mainCam != null && hamster != null)
        {
            mainCam.transform.position = hamster.transform.position + (cameraFollow != null ? cameraFollow.offset : new Vector3(0f, 11f, -9f));
            if (cameraFollow != null) cameraFollow.enabled = true;
        }

        if (enemigoFeralPrincipal != null) enemigoFeralPrincipal.enabled = true;
        if (agFeral != null && agFeral.enabled && agFeral.isOnNavMesh) agFeral.isStopped = false;
        yield return null; // Dejar que EnemyAI.Start() (que fija Wander) se ejecute antes de ForceChasePlayer

        if (hamster != null) hamster.enabled = true;
        enCinematica = false;
        EstablecerVisibilidadUIEnCinematica(false);

        IniciarPasoEscapeDash();
    }

    private IEnumerator ReproducirVideoAnochecer(Action<bool> onDone)
    {
        var host = new GameObject("Cinematica_Anochecer");
        var video = host.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.clip = cinematicaAnochecer;
        video.isLooping = false;
        video.renderMode = VideoRenderMode.CameraNearPlane;
        video.targetCamera = mainCam;
        video.aspectRatio = VideoAspectRatio.FitInside;
        video.audioOutputMode = VideoAudioOutputMode.Direct;
        bool finished = false;
        bool error = false;
        video.loopPointReached += _ => finished = true;
        video.errorReceived += (_, message) =>
        {
            Debug.LogWarning(message);
            error = true;
            finished = true;
        };
        if (tutorialHudCanvas != null) tutorialHudCanvas.enabled = false;
        if (enemigoFeralPrincipal != null) enemigoFeralPrincipal.gameObject.SetActive(false);
        video.Prepare();
        float deadline = Time.realtimeSinceStartup + 8f;
        while (!video.isPrepared && !finished && Time.realtimeSinceStartup < deadline) yield return null;
        bool played = video.isPrepared && !error;
        if (video.isPrepared && !finished)
        {
            video.Play();
            deadline = Time.realtimeSinceStartup + (float)video.length + 3f;
            while (!finished && Time.realtimeSinceStartup < deadline)
            {
                if (FeralInput.Pressed(KeyCode.Escape) || FeralInput.Pressed(KeyCode.Return)) break;
                yield return null;
            }
        }
        video.Stop();
        Destroy(host);
        if (tutorialHudCanvas != null) tutorialHudCanvas.enabled = true;
        if (enemigoFeralPrincipal != null) enemigoFeralPrincipal.gameObject.SetActive(true);
        // Allow EnemyAI.Start to initialize before ForceChasePlayer.
        yield return null;
        onDone(played && !error);
    }

    private IEnumerator ReproducirVideoCinematicaTigre(Action<bool> onDone)
    {
        if (videoCinematicaTigre == null) videoCinematicaTigre = Resources.Load<VideoClip>("Videos/TigreCorre");
        if (videoCinematicaTigre == null || mainCam == null)
        {
            onDone(false);
            yield break;
        }

        GameObject vpObj = new GameObject("VideoPlayer_CinematicaTigre");
        VideoPlayer vp = vpObj.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.clip = videoCinematicaTigre;
        vp.renderMode = VideoRenderMode.CameraNearPlane;
        vp.targetCamera = mainCam;
        vp.targetCameraAlpha = 1f;
        vp.aspectRatio = VideoAspectRatio.FitOutside;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.skipOnDrop = true;

        bool terminado = false;
        bool error = false;
        vp.loopPointReached += _ => terminado = true;
        vp.errorReceived += (_, msg) => { Debug.LogWarning($"[Tutorial] Video tigre: {msg}"); error = true; };

        vp.Prepare();
        float espera = 5f;
        while (!vp.isPrepared && !error && espera > 0f)
        {
            espera -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPrepared || error)
        {
            Destroy(vpObj);
            onDone(false);
            yield break;
        }

        vp.Play();
        ReproducirSfx(clipSfxAlertaFeral);

        // Esperar al final natural del clip (con margen de seguridad por si no llega loopPointReached)
        float limite = (float)videoCinematicaTigre.length + 1.5f;
        float t = 0f;
        while (!terminado && !error && t < limite)
        {
            if (FeralInput.Pressed(KeyCode.Escape) || FeralInput.Pressed(KeyCode.Return))
                break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        vp.Stop();
        vp.targetCamera = null;
        Destroy(vpObj);
        onDone(!error);
    }

    private void SpawnearEnemigosFase2()
    {
        if (enemigoFeralPrincipal != null) Destroy(enemigoFeralPrincipal.gameObject);
        if (enemigoPatrullaSigilo != null) Destroy(enemigoPatrullaSigilo.gameObject);

        GameObject prefabPrincipal = prefabEnemigoTigre != null ? prefabEnemigoTigre : prefabEnemigoPerro;
        if (prefabPrincipal != null)
        {
            Vector3 spawnPos = AjustarANavMesh(POS_VIA_FERAL_SPAWN);
            GameObject feralObj = Instantiate(prefabPrincipal, spawnPos, Quaternion.Euler(0f, 90f, 0f));
            feralObj.name = "DepredadorFeral_VíaPrincipal";
            feralObj.tag = "Enemy";

            if (feralObj.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = feralObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            enemigoFeralPrincipal = feralObj.GetComponent<EnemyAI>();
            if (enemigoFeralPrincipal == null) enemigoFeralPrincipal = feralObj.AddComponent<EnemyAI>();

            enemigoFeralPrincipal.player = hamster != null ? hamster.transform : null;
            enemigoFeralPrincipal.visionRadiusNormal = 22f;
            enemigoFeralPrincipal.visionRadiusCrouched = 6.5f;
            enemigoFeralPrincipal.visionAngle = 105f;
            enemigoFeralPrincipal.wanderSpeed = 3.5f;
            enemigoFeralPrincipal.chaseSpeed = 7.2f;
            enemigoFeralPrincipal.showVisionCone = true;

            // Luz roja amenazante sobre el depredador feral
            GameObject redLightObj = new GameObject("LuzAmenazaFeral");
            redLightObj.transform.SetParent(feralObj.transform, false);
            redLightObj.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);
            Light rl = redLightObj.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(1f, 0.15f, 0.1f);
            rl.range = 8f;
            rl.intensity = 2.5f;
        }
    }

    private Vector3 AjustarANavMesh(Vector3 pos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return pos;
    }

    private void IniciarPasoEscapeDash()
    {
        dashEjecutadoEnPersecucion = false;

        if (enemigoFeralPrincipal != null)
        {
            enemigoFeralPrincipal.ForceChasePlayer();
        }

        ConfigurarTextosEstado(
            TutorialState.EscapeDash,
            "FASE 2: NOCHE 1 — INTRODUCCIÓN AL DASH (ESCAPE RÁPIDO)",
            "Misión: ¡Te persiguen! Presiona [Espacio] (Dash) en movimiento y corre hacia la esquina",
            "Usa [Espacio] para esquivar el zarpazo y ganar distancia. Si te golpea una vez, se activará tu Respuesta de Adrenalina (repone estamina + velocidad).",
            puntoEsquinaSigilo
        );
    }

    private void HandlePlayerDash()
    {
        if (currentState == TutorialState.EscapeDash)
        {
            dashEjecutadoEnPersecucion = true;
            if (barreraEsquinaSigilo != null)
            {
                barreraEsquinaSigilo.SetActive(false);
            }
        }
    }

    private void HandlePlayerAdrenaline()
    {
        ReproducirSfx(clipSfxAdrenalina);
        StartCoroutine(EfectoVisualAdrenalina());
    }

    private IEnumerator EfectoVisualAdrenalina()
    {
        if (textoSubtituloGuia != null)
        {
            textoSubtituloGuia.text = " ¡RESPUESTA DE ADRENALINA ACTIVADA! Estamina restaurada al 100% y +30% de velocidad temporal para escapar.";
        }

        if (overlayPantalla != null)
        {
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                float a = Mathf.Sin(t * 10f) * 0.18f + 0.18f;
                overlayPantalla.color = new Color(1f, 0.35f, 0.1f, a);
                yield return null;
            }
            overlayPantalla.color = Color.clear;
        }
    }

    private void ActualizarPasoEscapeDash()
    {
        if (hamster == null) return;

        float distEsquina = puntoEsquinaSigilo != null
            ? Vector3.Distance(hamster.transform.position, puntoEsquinaSigilo.position)
            : 99f;

        if (textoMisionUI != null)
        {
            string estadoDash = dashEjecutadoEnPersecucion ? "<color=#55FF55>[Dash ]</color>" : "Presiona [Espacio] (Dash)";
            textoMisionUI.text = $"Misión: {estadoDash} y dobla la esquina sur ({distEsquina:0.0}m)";
        }

        if (dashEjecutadoEnPersecucion && distEsquina < 4f)
        {
            IniciarPasoSigiloEsquina();
        }
    }

    private void IniciarPasoSigiloEsquina()
    {
        checkpointPosicion = POS_ESQUINA_SIGILO;
        tiempoAgachadoEnSigilo = 0f;

        // Reubicar al enemigo en patrulla en la calle sur (Z=125) para que el jugador vea claramente su cono visual y radio
        if (enemigoFeralPrincipal != null)
        {
            NavMeshAgent ag = enemigoFeralPrincipal.GetComponent<NavMeshAgent>();
            Vector3 patrolPos = AjustarANavMesh(POS_PATRULLA_SIGILO);
            if (ag != null && ag.enabled) ag.Warp(patrolPos);
            else enemigoFeralPrincipal.transform.position = patrolPos;

            enemigoFeralPrincipal.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            enemigoFeralPrincipal.visionRadiusNormal = 15f;
            enemigoFeralPrincipal.visionRadiusCrouched = 4.5f;
            enemigoFeralPrincipal.showVisionCone = true;
            enemigoFeralPrincipal.SetPatrolWaypoints(new Vector3[]
            {
                AjustarANavMesh(new Vector3(264.0f, 0.15f, 226.5f)),
                AjustarANavMesh(new Vector3(255.5f, 0.15f, 226.5f))
            });
        }

        ConfigurarTextosEstado(
            TutorialState.SigiloEsquina,
            "FASE 2: NOCHE 1 — INTRODUCCIÓN AL SIGILO (AGACHARSE)",
            "Misión: Mantén [C] (Agacharse) detrás de los obstáculos para reducir el radio de detección",
            "Observa el círculo y cono visual del enemigo en el suelo: al mantener [C], su radio de detección se reduce drásticamente (verde).",
            puntoRefugioSeguro
        );
    }

    private void ActualizarPasoSigiloEsquina()
    {
        if (hamster == null) return;

        if (hamster.isCrouching && hamster.IsMoving && hamster.transform.position.z < 125f &&
            (enemigoFeralPrincipal == null || enemigoFeralPrincipal.CurrentState == EnemyAI.EnemyState.Wander))
        {
            tiempoAgachadoEnSigilo += Time.deltaTime;
        }

        float progresoSigilo = Mathf.Clamp01(tiempoAgachadoEnSigilo / 2.0f) * 100f;
        float radioActual = enemigoFeralPrincipal != null ? enemigoFeralPrincipal.GetCurrentEffectiveVisionRadius() : (hamster.isCrouching ? 4.5f : 15f);

        if (textoMisionUI != null)
        {
            textoMisionUI.text = $"Misión: Avanza manteniendo [C] (Agachado: {progresoSigilo:0}% | Radio detección enemigo: {radioActual:0.0}m)";
        }

        bool cercaDeEntradaRefugio = hamster.transform.position.x <= 26.0f;
        if (tiempoAgachadoEnSigilo >= 2.0f && cercaDeEntradaRefugio)
        {
            IniciarPasoLlegadaRefugio();
        }
    }

    private void IniciarPasoLlegadaRefugio()
    {
        ConfigurarTextosEstado(
            TutorialState.LlegadaRefugioAmanecer,
            "FASE 2: NOCHE 1 — LLEGADA AL REFUGIO Y AMANECER",
            "Misión: ¡Entra al túnel de tu refugio seguro al oeste para sobrevivir la noche!",
            "Cruza la entrada del túnel hacia tu madriguera antes de que los enemigos te alcancen.",
            puntoRefugioSeguro
        );
    }

    private void ActualizarPasoLlegadaRefugio()
    {
        if (hamster == null) return;

        float distRefugio = puntoRefugioSeguro != null
            ? Vector3.Distance(hamster.transform.position, puntoRefugioSeguro.position)
            : 99f;

        if ((hamster.transform.position.x <= 9.5f && hamster.transform.position.z >= 137.5f) || distRefugio <= 5.5f)
        {
            StartCoroutine(SecuenciaAmanecerDia2());
        }
    }

    private IEnumerator SecuenciaAmanecerDia2()
    {
        enCinematica = true;
        ReproducirSfx(clipSfxAmanecer);
        ReproducirAmbiente(clipAmbienteDia);

        // Curar al hámster al entrar al refugio y avanzar el ciclo al Amanecer (Día 2)
        if (hamster != null)
        {
            hamster.RespawnOrHeal();
        }

        if (enemigoFeralPrincipal != null)
        {
            enemigoFeralPrincipal.showVisionCone = false;
        }

        ConfigurarTextosEstado(
            TutorialState.LlegadaRefugioAmanecer,
            "¡LLEGASTE AL REFUGIO! — AMANECER DEL DÍA 2",
            "El sol sale de nuevo. Los enemigos vuelven a ser inofensivos...",
            "Has sobrevivido a la Noche 1. Al amanecer, tu salud y estamina se restauran en el refugio.",
            null
        );
        EstablecerVisibilidadUIEnCinematica(true);

        float t = 0f;
        while (t < 2.2f)
        {
            t += Time.deltaTime;
            float norm = t / 2.2f;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetExactHour(Mathf.Lerp(5.5f, 7.5f, norm));
            }
            if (overlayPantalla != null)
            {
                overlayPantalla.color = new Color(1f, 0.88f, 0.45f, Mathf.Sin(norm * Mathf.PI) * 0.28f);
            }
            yield return null;
        }

        if (overlayPantalla != null) overlayPantalla.color = Color.clear;
        enCinematica = false;
        EstablecerVisibilidadUIEnCinematica(false);

        IniciarFase3Crafting();
    }

    // ===================================================================================
    //  FASE 3: DÍA 2 — FABRICACIÓN (CRAFTING) Y COLOCACIÓN TÁCTICA DE TRAMPAS
    // ===================================================================================

    private void IniciarFase3Crafting()
    {
        checkpointPosicion = POS_MADRIGUERA_INICIO;

        // Asegurar que el jugador tenga al menos 1 Madera y 1 Metal recolectados en el Día 1
        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasEnoughItem(ItemType.Madera, 1))
                InventoryManager.Instance.AddItem(ItemType.Madera, 1);
            if (!InventoryManager.Instance.HasEnoughItem(ItemType.Metal, 1))
                InventoryManager.Instance.AddItem(ItemType.Metal, 1);
        }

        Transform targetMesa = mesaCrafteoRefugio != null ? mesaCrafteoRefugio.transform : null;

        ConfigurarTextosEstado(
            TutorialState.UsarMesaFabricacion,
            "FASE 3: DÍA 2 — USO DE LA MESA DE FABRICACIÓN",
            "Misión: Acércate a la Mesa de Trabajo del refugio y presiona [F] (Interactuar)",
            "Utiliza la mesa de trabajo de tu base para abrir la interfaz de preparación antes de la siguiente noche.",
            targetMesa
        );
    }

    private void HandleCraftingMenuToggled(CraftingTable table, bool isOpen)
    {
        if (isOpen && currentState != TutorialState.UsarMesaFabricacion && currentState != TutorialState.CrearPrimeraTrampa && currentState != TutorialState.FinTutorial)
        {
            table.CloseMenu();
            return;
        }

        menuCrafteoAbierto = isOpen;
        if (isOpen && table == mesaCrafteoRefugio && currentState == TutorialState.UsarMesaFabricacion)
        {
            IniciarPasoCrearPrimeraTrampa();
        }
        else if (!isOpen && currentState == TutorialState.CrearPrimeraTrampa)
        {
            bool yaCrafteoPuas = InventoryManager.Instance != null && InventoryManager.Instance.HasTrampa(TipoTrampa.Puas);
            if (!yaCrafteoPuas && table != null && hamster != null && Vector3.Distance(hamster.transform.position, table.transform.position) < 4.0f)
            {
                // Mantener el flujo en la creación obligatoria de la primera trampa
                textoSubtituloGuia.text = "Debes fabricar la 'Trampa 1 (Púas)' antes de continuar. Presiona [F] en la mesa y haz clic en el botón.";
            }
        }
    }

    private void IniciarPasoCrearPrimeraTrampa()
    {
        ConfigurarTextosEstado(
            TutorialState.CrearPrimeraTrampa,
            "FASE 3: DÍA 2 — CREACIÓN DE LA PRIMERA TRAMPA",
            "Misión: Haz clic en 'Trampa 1 (Púas)' usando 1 Madera y 1 Metal recolectados en el Día 1",
            "La Trampa de Púas cuesta 1 Madera + 1 Metal y reduce un 45% la velocidad de cualquier enemigo que cruce la entrada.",
            mesaCrafteoRefugio != null ? mesaCrafteoRefugio.transform : null
        );
    }

    private void HandleTrapCrafted(TipoTrampa tipo)
    {
        if (tipo != TipoTrampa.Puas) return;
        ReproducirSfx(clipSfxCrafteo);

        if (mesaCrafteoRefugio != null)
        {
            mesaCrafteoRefugio.CloseMenu();
        }

        if (currentState == TutorialState.CrearPrimeraTrampa || currentState == TutorialState.UsarMesaFabricacion)
        {
            IniciarPasoColocacionTactica();
        }
    }

    private void IniciarPasoColocacionTactica()
    {
        if (puertaTrampaRefugio != null)
        {
            puertaTrampaRefugio.LimpiarTrampa();
        }

        Transform targetPuerta = puertaTrampaRefugio != null ? puertaTrampaRefugio.transform : null;

        ConfigurarTextosEstado(
            TutorialState.ColocacionTacticaTrampa,
            "FASE 3: DÍA 2 — COLOCACIÓN TÁCTICA EN EL REFUGIO",
            "Misión: Presiona [1] para equipar la Trampa de Púas, acércate a la puerta del pasillo y presiona [F]",
            "Coloca la trampa en el punto de entrada estrecho del refugio para preparar el entorno antes de la siguiente noche.",
            targetPuerta
        );
    }

    private void ActualizarPasoColocacionTrampa()
    {
        if (puertaTrampaRefugio != null && puertaTrampaRefugio.trampaActual != TipoTrampa.Ninguna)
        {
            HandleTrapInstalledOnDoor(puertaTrampaRefugio, puertaTrampaRefugio.trampaActual);
        }
    }

    private void HandleTrapInstalledOnDoor(PuertaTrampa puerta, TipoTrampa tipo)
    {
        if (currentState != TutorialState.ColocacionTacticaTrampa ||
            puerta != puertaTrampaRefugio || tipo != TipoTrampa.Puas) return;
        if (trampaInstaladaEnRefugio) return;
        trampaInstaladaEnRefugio = true;
        ReproducirSfx(clipSfxCrafteo);

        StartCoroutine(SecuenciaDemostracionTrampaYFinal(puerta, tipo));
    }

    private IEnumerator SecuenciaDemostracionTrampaYFinal(PuertaTrampa puerta, TipoTrampa tipo)
    {
        ConfigurarTextosEstado(
            TutorialState.DemostracionTrampaActiva,
            "FASE 3: ¡TRAMPA INSTALADA EN LA ENTRADA!",
            $"¡Excelente! Has colocado la trampa de {tipo} en el pasillo del refugio. La entrada está preparada para la siguiente noche.",
            "La trampa se activará cuando un enemigo cruce el pasillo.",
            puerta != null ? puerta.transform : null
        );

        yield return new WaitForSeconds(3.5f);

        ReproducirSfx(clipSfxAmanecer);
        ConfigurarTextosEstado(
            TutorialState.FinTutorial,
            " ¡TUTORIAL COMPLETADO CON ÉXITO!",
            "Misión: ¡Estás listo para sobrevivir en FERAL! Explora, recolecta de día y defiende tu refugio de noche.",
            "Has dominado Movimiento (WASD), Carrera y Estamina (Shift), Looting (F), Ciclo Día/Noche, Dash (Espacio), Adrenalina, Sigilo (C) y Crafteo/Trampas (1-3 + F).",
            null
        );

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.pauseAutoAdvance = false;
            TimeManager.Instance.SetDayDuration(6f);
        }
    }

    // ===================================================================================
    //  MANEJO DE MUERTE Y RESPAWN EN CHECKPOINT DURANTE EL TUTORIAL
    // ===================================================================================

    private void HandlePlayerDied()
    {
        StartCoroutine(RespawnEnCheckpointRoutine());
    }

    private IEnumerator RespawnEnCheckpointRoutine()
    {
        if (textoSubtituloGuia != null)
        {
            textoSubtituloGuia.text = " Has recibido demasiados golpes. Reapareciendo en el punto de control seguro...";
        }

        if (overlayPantalla != null)
        {
            overlayPantalla.color = new Color(0.6f, 0f, 0f, 0.45f);
        }

        yield return new WaitForSeconds(1.5f);

        if (overlayPantalla != null)
        {
            overlayPantalla.color = Color.clear;
        }

        if (hamster != null)
        {
            hamster.RespawnOrHeal(checkpointPosicion);
        }

        if (currentState == TutorialState.EscapeDash && enemigoFeralPrincipal != null)
        {
            NavMeshAgent ag = enemigoFeralPrincipal.GetComponent<NavMeshAgent>();
            Vector3 resetEnemyPos = AjustarANavMesh(POS_VIA_FERAL_SPAWN);
            if (ag != null && ag.enabled) ag.Warp(resetEnemyPos);
            enemigoFeralPrincipal.ForceChasePlayer();
        }
    }

    // ===================================================================================
    //  ACTUALIZACIÓN DEL HUD Y PROMPTS CONTEXTUALES (GDD)
    // ===================================================================================

    private void ActualizarHudGDD()
    {
        if (TimeManager.Instance != null && textoRelojCiclo != null)
        {
            int h = TimeManager.Instance.CurrentHour;
            int m = TimeManager.Instance.CurrentMinute;
            bool esDia = TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Day;
            string icono = esDia ? " DÍA" : " NOCHE FERAL";
            if (esDia && h >= 16) icono = " ATARDECER";
            textoRelojCiclo.text = $"{icono} — {h:D2}:{m:D2}";
            textoRelojCiclo.color = esDia ? new Color(1f, 0.88f, 0.35f) : new Color(1f, 0.38f, 0.38f);
        }

        if (InventoryManager.Instance != null && textoInventarioRapido != null)
        {
            int mad = InventoryManager.Instance.GetItemCount(ItemType.Madera);
            int met = InventoryManager.Instance.GetItemCount(ItemType.Metal);
            int desp = InventoryManager.Instance.GetItemCount(ItemType.Desperdicios);
            int trPuas = InventoryManager.Instance.trampasInventory.ContainsKey(TipoTrampa.Puas) ? InventoryManager.Instance.trampasInventory[TipoTrampa.Puas] : 0;
            textoInventarioRapido.text = $" Madera: {mad} |  Metal: {met} |  Desp: {desp}\n Trampa Púas [1]: {trPuas}";
        }

        if (hamster != null && textoEstadoSaludAdrenalina != null)
        {
            int vidasRestantes = Mathf.Max(0, hamster.maxHitsAllowed - hamster.HitsReceived);
            if (hamster.IsAdrenalineActive)
            {
                textoEstadoSaludAdrenalina.text = " ¡ADRENALINA ACTIVA! (+30% Vel)";
                textoEstadoSaludAdrenalina.color = new Color(1f, 0.65f, 0.15f);
            }
            else if (hamster.isCrouching)
            {
                textoEstadoSaludAdrenalina.text = $" MODO SIGILO (C) |  Salud: {vidasRestantes}/{hamster.maxHitsAllowed}";
                textoEstadoSaludAdrenalina.color = new Color(0.35f, 0.95f, 0.7f);
            }
            else
            {
                textoEstadoSaludAdrenalina.text = $" Salud: {vidasRestantes}/{hamster.maxHitsAllowed} | Vel: {(hamster.CurrentSpeedModifier * 100f):0}%";
                textoEstadoSaludAdrenalina.color = vidasRestantes >= 2 ? new Color(0.5f, 0.95f, 0.55f) : new Color(1f, 0.4f, 0.4f);
            }
        }
    }

    private void ActualizarPromptContextual()
    {
        if (textoPromptContextual == null || textoPromptContextual.transform.parent == null) return;
        GameObject panelPrompt = textoPromptContextual.transform.parent.gameObject;

        if (hamster == null || hamster.CurrentInteractable == null)
        {
            if (panelPrompt.activeSelf) panelPrompt.SetActive(false);
            return;
        }

        GameObject inter = hamster.CurrentInteractable;
        if (!inter.CompareTag("Interactable") && !inter.CompareTag("Door"))
        {
            if (panelPrompt.activeSelf) panelPrompt.SetActive(false);
            return;
        }
        string mensaje = "";

        if (inter.TryGetComponent<LootContainer>(out LootContainer lc))
        {
            mensaje = lc.YaFueSaqueado
                ? "Contenedor vacío (Ya fue saqueado)"
                : "Presiona [F] para saquear el contenedor";
        }
        else if (inter.TryGetComponent<LuluNPC>(out LuluNPC _))
        {
            mensaje = "Presiona [F] para hablar con Lulú";
        }
        else if (inter.TryGetComponent<CraftingTable>(out CraftingTable ct))
        {
            mensaje = ct.IsMenuOpen
                ? "Selecciona una receta en pantalla o presiona [F] para cerrar"
                : "Presiona [F] para usar la Mesa de Fabricación";
        }
        else if (inter.TryGetComponent<PuertaTrampa>(out PuertaTrampa pt))
        {
            if (pt.trampaActual != TipoTrampa.Ninguna)
                mensaje = $"Puerta protegida con Trampa de {pt.trampaActual}";
            else
                mensaje = $"Presiona [1/2/3] para elegir trampa ({hamster.trampaSeleccionada}) y [F] para colocarla";
        }

        if (!string.IsNullOrEmpty(mensaje))
        {
            if (!panelPrompt.activeSelf) panelPrompt.SetActive(true);
            textoPromptContextual.text = mensaje;
        }
        else if (panelPrompt.activeSelf)
        {
            panelPrompt.SetActive(false);
        }
    }

    // ===================================================================================
    //  UTILIDADES DE ESTADO Y AUDIO PROCEDURAL
    // ===================================================================================

    private void CachearBotonesSecundariosMesa()
    {
        botonesRecetasSecundarias.Clear();
        if (mesaCrafteoRefugio == null || mesaCrafteoRefugio.craftingPanelUI == null) return;
        Button[] btns = mesaCrafteoRefugio.craftingPanelUI.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < btns.Length; i++)
        {
            if (btns[i] != null && btns[i].name != "Button")
            {
                botonesRecetasSecundarias.Add(btns[i]);
            }
        }
    }

    private void SincronizarPermisosDeEstado(TutorialState estado)
    {
        // 1. Barreras físicas para evitar que el jugador avance a zonas antes de completar el requisito del paso actual
        if (barreraSalidaMadriguera != null)
            barreraSalidaMadriguera.SetActive(false);

        if (barreraSprintCallejon != null)
            barreraSprintCallejon.SetActive(false);

        if (barreraEsquinaSigilo != null)
            barreraEsquinaSigilo.SetActive(estado == TutorialState.CinematicaAparicionFeral || (estado == TutorialState.EscapeDash && !dashEjecutadoEnPersecucion));

        if (barreraEntradaRefugio != null)
            barreraEntradaRefugio.SetActive(estado == TutorialState.CinematicaAparicionFeral || estado == TutorialState.EscapeDash || estado == TutorialState.SigiloEsquina);

        // 2. Permisos de interacción ("Interactable" / "Door") estrictos por paso
        if (contenedorControlBrillante != null)
        {
            bool habilitarContControl = (estado == TutorialState.SaquearContenedorControl || estado == TutorialState.AprenderEstamina || estado == TutorialState.FinTutorial);
            contenedorControlBrillante.gameObject.tag = habilitarContControl ? "Interactable" : "Untagged";
        }

        if (luluNPC != null)
        {
            bool habilitarLulu = (estado == TutorialState.BuscarLulu || estado == TutorialState.RegresarLulu);
            luluNPC.gameObject.tag = habilitarLulu ? "Interactable" : "Untagged";
        }

        for (int i = 0; i < contenedoresCiudad.Count; i++)
        {
            if (contenedoresCiudad[i] != null)
            {
                bool habilitarContCiudad = (estado == TutorialState.RecolectarLoot || estado == TutorialState.FinTutorial);
                contenedoresCiudad[i].gameObject.tag = habilitarContCiudad ? "Interactable" : "Untagged";
            }
        }

        if (mesaCrafteoRefugio != null)
        {
            bool habilitarMesa = (estado == TutorialState.UsarMesaFabricacion || estado == TutorialState.CrearPrimeraTrampa || estado == TutorialState.FinTutorial);
            mesaCrafteoRefugio.gameObject.tag = habilitarMesa ? "Interactable" : "Untagged";
            if (!habilitarMesa && mesaCrafteoRefugio.IsMenuOpen)
            {
                mesaCrafteoRefugio.CloseMenu();
            }
        }

        for (int i = 0; i < botonesRecetasSecundarias.Count; i++)
        {
            if (botonesRecetasSecundarias[i] != null)
            {
                botonesRecetasSecundarias[i].interactable = (estado == TutorialState.FinTutorial);
            }
        }

        if (puertaTrampaRefugio != null)
        {
            bool habilitarPuertaTrampa = (estado == TutorialState.ColocacionTacticaTrampa || estado == TutorialState.FinTutorial);
            puertaTrampaRefugio.gameObject.tag = habilitarPuertaTrampa ? "Door" : "Untagged";
        }
    }

    private void AplicarRestriccionesSecuencialesEstrictas()
    {
        if (hamster == null) return;

        // En ColocacionTacticaTrampa obligar a tener seleccionada la Trampa de Púas [1]
        if (currentState == TutorialState.ColocacionTacticaTrampa)
        {
            hamster.trampaSeleccionada = TipoTrampa.Puas;
        }

        // Si el objeto en CurrentInteractable perdió su tag por no corresponder al paso actual, impedir que muestre prompt
        if (hamster.CurrentInteractable != null)
        {
            GameObject cur = hamster.CurrentInteractable;
            if (!cur.CompareTag("Interactable") && !cur.CompareTag("Door"))
            {
                // El jugador no puede activarlo fuera de orden
                if (cur.TryGetComponent<CraftingTable>(out CraftingTable ct) && ct.IsMenuOpen)
                {
                    ct.CloseMenu();
                }
            }
        }
    }

    private void ConfigurarTextosEstado(TutorialState nuevoEstado, string tituloFase, string textoMision, string subtituloGuia, Transform nuevoObjetivo)
    {
        currentState = nuevoEstado;
        SincronizarPermisosDeEstado(nuevoEstado);

        if (textoFaseBanner != null) textoFaseBanner.text = tituloFase;
        if (textoSubtituloGuia != null) textoSubtituloGuia.text = subtituloGuia;
        if (textoMisionUI != null)
        {
            textoMisionUI.gameObject.SetActive(!enCinematica);
            textoMisionUI.text = textoMision;
        }

        if (punteroFlecha != null)
        {
            if (nuevoObjetivo != null && !enCinematica)
            {
                punteroFlecha.objetivo = nuevoObjetivo;
                punteroFlecha.gameObject.SetActive(true);
            }
            else
            {
                punteroFlecha.gameObject.SetActive(false);
            }
        }

        if (enCinematica)
        {
            EstablecerVisibilidadUIEnCinematica(true);
        }
    }

    public void AvanzarEstado(TutorialState nuevoEstado, string textoMision, Transform nuevoObjetivo = null)
    {
        ConfigurarTextosEstado(nuevoEstado, textoFaseBanner != null ? textoFaseBanner.text : "TUTORIAL FERAL", "Misión: " + textoMision, textoSubtituloGuia != null ? textoSubtituloGuia.text : "", nuevoObjetivo);
    }

    private void InicializarAudioProcedural()
    {
        audioAmbienteSource = gameObject.AddComponent<AudioSource>();
        audioAmbienteSource.loop = true;
        audioAmbienteSource.volume = 0.18f;
        audioAmbienteSource.spatialBlend = 0f;

        audioSfxSource = gameObject.AddComponent<AudioSource>();
        audioSfxSource.loop = false;
        audioSfxSource.volume = 0.45f;
        audioSfxSource.spatialBlend = 0f;

#if UNITY_EDITOR
        clipAmbienteDia = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musica_Tutorial_Dia.mp3");
        clipAmbienteNoche = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musica_Tutorial_Noche.mp3");
#endif
        if (clipAmbienteDia == null)
            clipAmbienteDia = Resources.Load<AudioClip>("Audio/Musica_Tutorial_Dia");
        if (clipAmbienteNoche == null)
            clipAmbienteNoche = Resources.Load<AudioClip>("Audio/Musica_Tutorial_Noche");

        if (clipAmbienteDia == null)
            clipAmbienteDia = GenerarClipProcedural("Amb_Dia", 4.0f, (t) =>
                Mathf.Sin(t * 2f * Mathf.PI * 130f) * 0.04f + Mathf.Sin(t * 2f * Mathf.PI * 196f) * 0.03f);

        if (clipAmbienteNoche == null)
            clipAmbienteNoche = GenerarClipProcedural("Amb_NocheAmenazante", 4.0f, (t) =>
                Mathf.Sin(t * 2f * Mathf.PI * 55f) * 0.06f + Mathf.Sin(t * 2f * Mathf.PI * 82f) * 0.04f);

        clipSfxLoot = GenerarClipProcedural("Sfx_Loot", 0.35f, (t) =>
            Mathf.Sin(t * 2f * Mathf.PI * (520f + t * 600f)) * (1f - t / 0.35f) * 0.5f);

        clipSfxAlertaFeral = GenerarClipProcedural("Sfx_AlertaFeral", 0.9f, (t) =>
            Mathf.Sin(t * 2f * Mathf.PI * (140f - t * 60f)) * (1f - t / 0.9f) * 0.7f);

        clipSfxAdrenalina = GenerarClipProcedural("Sfx_Adrenalina", 0.5f, (t) =>
            Mathf.Sin(t * 2f * Mathf.PI * (300f + t * 900f)) * (1f - t / 0.5f) * 0.6f);

        clipSfxCrafteo = GenerarClipProcedural("Sfx_Crafteo", 0.45f, (t) =>
            Mathf.Sin(t * 2f * Mathf.PI * (440f + Mathf.Floor(t * 8f) * 110f)) * (1f - t / 0.45f) * 0.55f);

        clipSfxAmanecer = GenerarClipProcedural("Sfx_Amanecer", 0.8f, (t) =>
            (Mathf.Sin(t * 2f * Mathf.PI * 440f) + Mathf.Sin(t * 2f * Mathf.PI * 660f)) * 0.3f * (1f - t / 0.8f));
    }

    private AudioClip GenerarClipProcedural(string nombre, float duracionSegundos, Func<float, float> generadorMuestra)
    {
        int sampleRate = 22050;
        int totalSamples = Mathf.Max(1, Mathf.RoundToInt(duracionSegundos * sampleRate));
        float[] data = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            data[i] = Mathf.Clamp(generadorMuestra(t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(nombre, totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void ReproducirAmbiente(AudioClip clip)
    {
        if (audioAmbienteSource == null || clip == null) return;
        if (audioAmbienteSource.clip == clip && audioAmbienteSource.isPlaying) return;
        audioAmbienteSource.clip = clip;
        audioAmbienteSource.Play();
    }

    private void ReproducirSfx(AudioClip clip)
    {
        if (audioSfxSource == null || clip == null) return;
        audioSfxSource.PlayOneShot(clip);
    }
}
