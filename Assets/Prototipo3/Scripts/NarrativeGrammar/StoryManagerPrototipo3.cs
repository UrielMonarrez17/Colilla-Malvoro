using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prototipo3.Narrative;

namespace Prototipo3
{
    /// <summary>
    /// Gestor de la Historia No Lineal por Grafo (DAG) de Prototipo 3 sin límite de días.
    /// Coordina el Nodo 0 (Apertura), Rama A (Escuela), Rama B (Bosque Ceniza),
    /// Nodo C (Convergencia Hospital) y Nodo D (Torre Aethelgard), además de
    /// las transmisiones de radio de la Dra. Elena Vargas, rumores cardinales y cinemáticas MP4.
    /// </summary>
    public class StoryManagerPrototipo3 : MonoBehaviour
    {
        public static StoryManagerPrototipo3 Instance { get; private set; }

        [Header("Generación Narrativa")]
        public int storySeed = 54321;
        public StoryWorldBinder worldBinder;

        [Header("Referencias del Jugador y Misión")]
        public Transform player;
        public MissionPointerPrototipo3 missionPointer;

        [Header("Estado del Grafo Narrativo (DAG)")]
        [SerializeField] private StoryGraph currentGraph;
        public StoryGraph CurrentGraph => currentGraph;

        private StoryNode currentActiveNode;
        public StoryNode CurrentActiveNode => currentActiveNode;

        [Header("Objetos Clave de Historia")]
        [SerializeField] private List<string> debugKeyItemsList = new List<string>();
        private readonly HashSet<string> keyItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyCollection<string> KeyItems => keyItems;

        [Header("Progreso de Misiones y Jefes")]
        public int containersLootedCount = 0;
        public bool hasSpokenToMarcoDay = false;
        public bool hasCraftedInitialTool = false;
        public bool hasTriggeredMarcoFeralCutscene = false;

        // Rama A: Escuela Bellavista
        public bool hasReachedSchool = false;
        public bool hasSpokenToToby = false;
        public bool hasSpiedMoralesOffice = false;
        public bool hasTriggeredSchoolBlackout = false;

        // Rama B: Bosque Ceniza
        public bool hasReachedForest = false;
        public bool hasSpokenToMoka = false;
        public bool hasSpokenToSultanDay = false;
        public bool hasSultanLostControlAtNight = false;
        public int repairedReflectorPolesCount = 0;
        public bool hasPacifiedSultan = false;

        // Nodo C: Hospital San Judas
        public bool hasSpokenToSalgado = false;
        public bool hasVisitedSofiaRoom304 = false;
        public bool hasSpokenToClaraDay = false;
        public bool hasPacifiedClara = false;

        // Nodo D: Torre Aethelgard
        public bool hasSpokenToVane = false;
        public bool hasSpokenToElenaTower = false;
        public int overloadedCapacitorsCount = 0;
        public bool hasCompletedGame = false;

        private bool introCutscenePlayed = false;

        public string LastRadioMessage { get; private set; } = "";
        public string LastCardinalRumor { get; private set; } = "";

        public event Action<StoryNode> OnActiveNodeChanged;
        public event Action<string, string> OnDialogueTriggered; // (Speaker, Text)
        public event Action<string, string> OnRadioTransmission; // (Frequency/Speaker, Message)
        public event Action<string> OnKeyItemObtained;
        public event Action OnDemoCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }

            if (worldBinder == null)
            {
                worldBinder = GetComponent<StoryWorldBinder>() ?? gameObject.AddComponent<StoryWorldBinder>();
            }

            if (GetComponent<CutsceneManager>() == null && CutsceneManager.Instance == null)
            {
                gameObject.AddComponent<CutsceneManager>();
            }

            if (GetComponent<RespawnManagerPrototipo3>() == null && RespawnManagerPrototipo3.Instance == null)
            {
                gameObject.AddComponent<RespawnManagerPrototipo3>();
            }
        }

        private void OnEnable()
        {
            LootContainer.OnContainerLooted += HandleContainerLooted;
            CraftingTable.OnTrapCrafted += HandleLegacyTrapCrafted;
            TrapSystemPrototipo3.OnProto3TrapCrafted += HandlePrototipo3TrapCrafted;
        }

        private void OnDisable()
        {
            LootContainer.OnContainerLooted -= HandleContainerLooted;
            CraftingTable.OnTrapCrafted -= HandleLegacyTrapCrafted;
            TrapSystemPrototipo3.OnProto3TrapCrafted -= HandlePrototipo3TrapCrafted;

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnCycleStateChanged -= HandleCycleStateChanged;
            }

            CityGeneratorPrototipo3 cityGen = FindAnyObjectByType<CityGeneratorPrototipo3>();
            if (cityGen != null)
            {
                cityGen.OnCityGenerated -= InicializarHistoria;
            }
        }

        private void Start()
        {
            EnsurePlayerReference();

            if (missionPointer == null)
            {
                missionPointer = FindAnyObjectByType<MissionPointerPrototipo3>();
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnCycleStateChanged -= HandleCycleStateChanged;
                TimeManager.Instance.OnCycleStateChanged += HandleCycleStateChanged;
            }

            CityGeneratorPrototipo3 cityGen = FindAnyObjectByType<CityGeneratorPrototipo3>();
            if (cityGen != null)
            {
                cityGen.OnCityGenerated -= InicializarHistoria;
                cityGen.OnCityGenerated += InicializarHistoria;
                if (cityGen.Grid != null)
                {
                    InicializarHistoria();
                }
            }
            else
            {
                InicializarHistoria();
            }
        }

        private void EnsurePlayerReference()
        {
            if (player == null)
            {
                HamsterControllerPrototipo3 hc3 = FindAnyObjectByType<HamsterControllerPrototipo3>();
                if (hc3 != null)
                {
                    hc3.enabled = true;
                    player = hc3.transform;
                    return;
                }

                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null)
                {
                    hc.enabled = true;
                    player = hc.transform;
                }
            }
        }

        public void InicializarHistoria()
        {
            CityGeneratorPrototipo3 cityGen = FindAnyObjectByType<CityGeneratorPrototipo3>();

            if (currentGraph == null || currentGraph.nodes == null || currentGraph.nodes.Count == 0)
            {
                currentGraph = StoryGrammarEngine.GenerateStory(storySeed);
                SetNodeState("node_0_apertura", StoryNodeState.Active, setFocus: true);
            }

            if (cityGen != null && worldBinder != null)
            {
                worldBinder.BindGraphToWorld(currentGraph, cityGen);
            }

            RefreshPointerAndUI();

            if (!introCutscenePlayed)
            {
                introCutscenePlayed = true;
                StartCoroutine(PlayOpeningSequenceRoutine());
            }
        }

        private IEnumerator PlayOpeningSequenceRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            PlayCutsceneSafe(
                "01_Intro_Sofia.mp4",
                "CAPÍTULO 1: EL HUMO DE SOLIS",
                "Sofía tose bajo la fiebre del Gas Solis mientras Kilo promete volver con la cura. En las calles, los vecinos murmuran sobre la Torre Aethelgard.",
                worldBinder != null && worldBinder.SpawnedMarco != null ? worldBinder.SpawnedMarco.transform : player,
                () =>
                {
                    EmitRadioTransmission("Dra. Elena Vargas [Radio 94.7 MHz]",
                        "¿Me copias, pequeño? Soy la Dra. Elena Vargas. Para sintetizar el Suero Alba-Puro y salvar a Sofía necesitamos dos precursores: la Enzima Base en la Escuela Bellavista y el Filtro Biológico en el Bosque Ceniza. Habla primero con tu vecino Marco y prepara tu mesa de trabajo.");
                });
        }

        private void Update()
        {
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;
            EnsurePlayerReference();
            if (currentGraph == null || player == null) return;

            CheckProximityObjectives();
        }

        private void CheckProximityObjectives()
        {
            CityGeneratorPrototipo3 cityGen = CityGeneratorPrototipo3.Instance;
            if (cityGen == null) return;

            // Verificar llegada a la Escuela Secundaria Bellavista (Misión A1)
            StoryNode nodeA1 = currentGraph.GetNode("node_a1_escuela");
            if (nodeA1 != null && nodeA1.state == StoryNodeState.Active)
            {
                Vector3 schoolPos = cityGen.GetLandmarkPosition("Escuela");
                if (Vector3.Distance(player.position, schoolPos) <= 13.5f)
                {
                    hasReachedSchool = true;
                    CompleteNodeAndUnlockChildren("node_a1_escuela");
                    EmitRadioTransmission("Dra. Elena Vargas [Radio]",
                        "Estás frente a la Escuela Secundaria Bellavista. Busca a Toby el Mapache en el taller de robótica; él sabe cómo distraer los oídos del Rector Morales.");
                }
            }

            // Verificar llegada al Bosque Ceniza (Misión B1)
            StoryNode nodeB1 = currentGraph.GetNode("node_b1_bosque");
            if (nodeB1 != null && nodeB1.state == StoryNodeState.Active)
            {
                Vector3 forestPos = cityGen.GetLandmarkPosition("Bosque");
                if (Vector3.Distance(player.position, forestPos) <= 15f && hasSpokenToMoka)
                {
                    CompleteNodeAndUnlockChildren("node_b1_bosque");
                }
            }

            // En Nodo 0: si ya es de noche y el jugador sale de la base tras haber hablado con Marco o fabricado una herramienta
            StoryNode node0 = currentGraph.GetNode("node_0_apertura");
            if (node0 != null && node0.state == StoryNodeState.Active && TimeManager.Instance != null)
            {
                if (TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night && !hasTriggeredMarcoFeralCutscene)
                {
                    float distFromBase = Vector3.Distance(player.position, cityGen.GetLandmarkPosition("Base"));
                    if (distFromBase > 4.0f || hasSpokenToMarcoDay || hasCraftedInitialTool)
                    {
                        TriggerMarcoNightfallEvent();
                    }
                }
            }
        }

        private void HandleCycleStateChanged(TimeManager.CycleState newState)
        {
            if (currentGraph == null) return;

            if (newState == TimeManager.CycleState.Night)
            {
                StoryNode node0 = currentGraph.GetNode("node_0_apertura");
                if (node0 != null && node0.state == StoryNodeState.Active && !hasTriggeredMarcoFeralCutscene)
                {
                    TriggerMarcoNightfallEvent();
                }

                // Si cae la noche durante la misión de reparar los postes de Sultán sin haberlo pacificado
                StoryNode nodeB2 = currentGraph.GetNode("node_b2_postes");
                if (nodeB2 != null && nodeB2.state == StoryNodeState.Active && !hasPacifiedSultan)
                {
                    hasSultanLostControlAtNight = true;
                    EmitRadioTransmission("Dra. Elena Vargas [Radio]",
                        "¡Kilo, cayó la noche en el Bosque Ceniza! El olfato de Sultán es letal en la oscuridad. Si aún no terminaste de reparar los 3 postes, puedes escapar y continuar mañana al amanecer.");
                }
            }
            else if (newState == TimeManager.CycleState.Day)
            {
                StoryNode node0 = currentGraph.GetNode("node_0_apertura");
                if (node0 != null && node0.state == StoryNodeState.Active && hasTriggeredMarcoFeralCutscene)
                {
                    UnlockBranchesAfterOpening();
                }
            }
        }

        public void NotifyCharacterTurnedFeral(DualRoleCharacter character)
        {
            if (character == null || currentGraph == null) return;
            if (string.Equals(character.characterId, "Marco", StringComparison.OrdinalIgnoreCase))
            {
                StoryNode node0 = currentGraph.GetNode("node_0_apertura");
                if (node0 != null && node0.state == StoryNodeState.Active && !hasTriggeredMarcoFeralCutscene)
                {
                    TriggerMarcoNightfallEvent();
                }
            }
        }

        private void TriggerMarcoNightfallEvent()
        {
            if (hasTriggeredMarcoFeralCutscene) return;
            hasTriggeredMarcoFeralCutscene = true;

            Transform marcoTarget = worldBinder != null && worldBinder.SpawnedMarco != null
                ? worldBinder.SpawnedMarco.transform
                : player;

            PlayCutsceneSafe(
                "02_Marco_Feral.mp4",
                "LA PRIMERA NOCHE: EL RASTRO DEL TEJÓN",
                "Las sirenas de toque de queda resuenan. Los ojos del buen conserje Marco se tiñen de ámbar salvaje bajo la niebla de Solis. ¡Escapa o refúgiate!",
                marcoTarget,
                () =>
                {
                    UnlockBranchesAfterOpening();
                });
        }

        private void UnlockBranchesAfterOpening()
        {
            StoryNode node0 = currentGraph.GetNode("node_0_apertura");
            if (node0 != null && node0.state != StoryNodeState.Completed)
            {
                node0.state = StoryNodeState.Completed;
            }

            SetNodeState("node_a1_escuela", StoryNodeState.Active, setFocus: true);
            SetNodeState("node_b1_bosque", StoryNodeState.Active, setFocus: false);

            CityGeneratorPrototipo3 cityGen = CityGeneratorPrototipo3.Instance;
            string dirEscuela = cityGen != null ? cityGen.GetCardinalDirectionFromPlayer("Escuela") : "Norte";
            string dirBosque = cityGen != null ? cityGen.GetCardinalDirectionFromPlayer("Bosque") : "Oeste";

            EmitRadioTransmission("Dra. Elena Vargas [Radio 94.7 MHz]",
                $"¡Sobreviviste a la transformación de Marco! Se abren dos rutas en paralelo: la Escuela Bellavista está hacia el {dirEscuela} y el Bosque Ceniza hacia el {dirBosque}. Revisa los contenedores con [F] para orientarte.");

            RefreshPointerAndUI();
        }

        // =========================================================================================
        // INTERACCIÓN CON PERSONAJES (DualRoleCharacter)
        // =========================================================================================

        public void HandleCharacterInteraction(DualRoleCharacter character)
        {
            if (character == null || currentGraph == null) return;
            string id = character.characterId;

            switch (id)
            {
                case "Marco":
                    HandleMarcoInteraction(character);
                    break;

                case "Toby":
                    HandleTobyInteraction(character);
                    break;

                case "Morales":
                    OnDialogueTriggered?.Invoke(character.displayName,
                        "\"Silencio en los pasillos. Las clases vespertinas quedan suspendidas hasta nuevo aviso. Nadie debe acercarse al laboratorio de química.\"");
                    break;

                case "Moka":
                    HandleMokaInteraction(character);
                    break;

                case "Sultan":
                    HandleSultanInteraction(character);
                    break;

                case "Salgado":
                    HandleSalgadoInteraction(character);
                    break;

                case "Sofia":
                    HandleSofiaInteraction(character);
                    break;

                case "Clara":
                    HandleClaraInteraction(character);
                    break;

                case "Vane":
                    HandleVaneInteraction(character);
                    break;

                case "Elena":
                    HandleElenaInteraction(character);
                    break;

                default:
                    string line = !string.IsNullOrEmpty(character.dayDialogue)
                        ? character.dayDialogue
                        : "Ten cuidado cuando baje el sol, pequeñín.";
                    OnDialogueTriggered?.Invoke(character.displayName, line);
                    break;
            }
        }

        private void HandleMarcoInteraction(DualRoleCharacter marco)
        {
            hasSpokenToMarcoDay = true;
            StoryNode node0 = currentGraph.GetNode("node_0_apertura");

            if (node0 != null && node0.state == StoryNodeState.Active)
            {
                node0.currentItemProgress = Mathf.Min(node0.requiredItemCount, node0.currentItemProgress + 1);
                OnDialogueTriggered?.Invoke(marco.displayName,
                    "\"¡Hola, Kilo! Qué mala cara trae la pequeña Sofía... Escucha, recogí mucha chatarra en estos contenedores. Llévate madera y metal a tu mesa de trabajo para armar una trampa defensiva antes del anochecer. Últimamente me duele la cabeza al caer el sol...\"");

                if (hasCraftedInitialTool)
                {
                    EmitRadioTransmission("Dra. Elena Vargas [Radio]",
                        "Ya hablaste con Marco y preparaste tu primera herramienta. Ten cuidado: la neblina nocturna está a punto de bajar. Si quieres abrir las rutas de la ciudad ahora mismo, explora más allá de tu calle o espera al anochecer.");
                    UnlockBranchesAfterOpening();
                }
                else
                {
                    RefreshPointerAndUI();
                }
            }
            else
            {
                OnDialogueTriggered?.Invoke(marco.displayName,
                    "\"Perdona si anoche hice ruido cerca de tu refugio, Kilo... No recuerdo nada desde que sonó la sirena. Revisa los callejones si necesitas más materiales.\"");
            }
        }

        private void HandleTobyInteraction(DualRoleCharacter toby)
        {
            StoryNode nodeA1 = currentGraph.GetNode("node_a1_escuela");
            if (nodeA1 != null && nodeA1.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_a1_escuela");
            }

            if (!hasSpokenToToby)
            {
                hasSpokenToToby = true;
                PlayCutsceneSafe(
                    "03_Toby_Escuela.mp4",
                    "EL TALLER DE ROBÓTICA: TOBY EL MAPACHE",
                    "Toby ajusta un resonador sónico hecho con latas y engranajes. \"El Rector Morales tiene un oído hipersensible y visión de casi 300 grados. Si corres con [Shift], irá directo por ti.\"",
                    toby.transform,
                    () =>
                    {
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.AddTrampa(TipoTrampa.Puas);
                        }
                        OnDialogueTriggered?.Invoke(toby.displayName,
                            "\"Toma este Señuelo Acústico [Tecla 1]. Ahora espía la oficina del Rector Morales de día [F] y luego entra al laboratorio de química por la Llave Maestra y el Vial de Enzima Base.\"");
                        RefreshPointerAndUI();
                    });
            }
            else
            {
                OnDialogueTriggered?.Invoke(toby.displayName,
                    hasSpiedMoralesOffice
                        ? "\"¡Ya tienes el código de la oficina! Entra al Laboratorio de Química y toma el Vial de Enzima Base. Ten listo el Señuelo Acústico [1] y agáchate con [C] por si cortan la luz.\""
                        : "\"Asómate primero a la Oficina del Rector Morales y pulsa [F] para averiguar dónde guarda la Llave Maestra del Conserje.\"");
            }
        }

        private void HandleMokaInteraction(DualRoleCharacter moka)
        {
            hasSpokenToMoka = true;
            hasReachedForest = true;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(ItemType.Semillas, 2);
                InventoryManager.Instance.AddItem(ItemType.Madera, 1);
            }

            StoryNode nodeB1 = currentGraph.GetNode("node_b1_bosque");
            if (nodeB1 != null && nodeB1.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_b1_bosque");
            }

            OnDialogueTriggered?.Invoke(moka.displayName,
                "\"¡Chst, aquí arriba, hámster! Toma estas Semillas de polen y Madera (+2 Semillas, +1 Madera). En la mesa de fabricación puedes crear la Bomba de Polen [Trampa 2] para anular el olfato de Sultán. Ve a hablar con él a su cabaña ahora que es de día.\"");
            RefreshPointerAndUI();
        }

        private void HandleSultanInteraction(DualRoleCharacter sultan)
        {
            StoryNode nodeB1 = currentGraph.GetNode("node_b1_bosque");
            if (nodeB1 != null && nodeB1.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_b1_bosque");
            }

            if (hasPacifiedSultan || sultan.isPacified)
            {
                OnDialogueTriggered?.Invoke(sultan.displayName,
                    "\"Gracias a la luz fría de los reflectores mi mente está despejada, amigo mío. Usa el Filtro de Carbón Biológico y la Llave del Hospital para salvar a la pequeña Sofía.\"");
                return;
            }

            hasSpokenToSultanDay = true;

            if (hasSultanLostControlAtNight && repairedReflectorPolesCount < 3)
            {
                OnDialogueTriggered?.Invoke(sultan.displayName,
                    $"\"Kilo... perdóname por haber perdido el control anoche. Mis garras aún tiemblan. Por favor, termina de reparar los 3 postes reflectores del arroyo ({repairedReflectorPolesCount}/3 reparados) antes de que vuelva a oscurecer.\"");
            }
            else if (repairedReflectorPolesCount < 3)
            {
                OnDialogueTriggered?.Invoke(sultan.displayName,
                    $"\"Qué bueno que viniste de día, pequeño. Cuando cae la noche el Gas Solis me convierte en una bestia. Ayúdame a reparar los 3 postes reflectores del arroyo ({repairedReflectorPolesCount}/3) con Madera, Goma y Metal. Luego, de noche, usa una Bomba de Polen [2] y enciende el interruptor de luz fría.\"");
            }
            else
            {
                OnDialogueTriggered?.Invoke(sultan.displayName,
                    "\"¡Los 3 postes reflectores ya están listos! Cuando caiga la noche, usa la Bomba de Polen [Tecla 2] para cegar mi olfato, atráeme hacia el arroyo y activa el Interruptor de la Subestación [F].\"");
            }

            RefreshPointerAndUI();
        }

        private void HandleSalgadoInteraction(DualRoleCharacter salgado)
        {
            hasSpokenToSalgado = true;

            if (HasKeyItem("Cilindro de Oxígeno Puro"))
            {
                StoryNode nodeC2 = currentGraph != null ? currentGraph.GetNode("node_c2_clara") : null;
                if (nodeC2 != null && nodeC2.state != StoryNodeState.Completed)
                {
                    OnDialogueTriggered?.Invoke(salgado.displayName,
                        "\"¡Lograste traer el Cilindro de Oxígeno Puro, Kilo! Conectemos el respirador de Sofía de inmediato para estabilizarla... ¡Espera, los monitores del hospital están siendo intervenidos desde la Torre Aethelgard!\"");
                    DeliverOxygenCylinderAndTriggerKrossCutscene(salgado.transform);
                    return;
                }
            }

            OnDialogueTriggered?.Invoke(salgado.displayName,
                "\"Gracias al cielo trajiste la Enzima Base y el Filtro Biológico, Kilo. Visita a Sofía en la Habitación 304 y habla con la enfermera Clara. Para purificar la fórmula necesitamos el Cilindro de Oxígeno Puro del quirófano, pero de noche Clara se transforma en La Dama de Gas. Tendrás que encerrarla en la Cámara Hiperbárica sobrecargando su válvula con la Trampa 3 [Descargador Estático].\"");

            CheckHospitalC1Completion();
        }

        private void HandleSofiaInteraction(DualRoleCharacter sofia)
        {
            hasVisitedSofiaRoom304 = true;

            if (HasKeyItem("Cilindro de Oxígeno Puro"))
            {
                StoryNode nodeC2 = currentGraph != null ? currentGraph.GetNode("node_c2_clara") : null;
                if (nodeC2 != null && nodeC2.state != StoryNodeState.Completed)
                {
                    OnDialogueTriggered?.Invoke(sofia.displayName,
                        "\"El oxígeno puro me hace respirar mucho mejor, Kilo... ¡Mira la pantalla del monitor!\"");
                    DeliverOxygenCylinderAndTriggerKrossCutscene(sofia.transform);
                    return;
                }
            }

            OnDialogueTriggered?.Invoke(sofia.displayName,
                "\"¿Kilo...? Sabía que no me dejarías sola... El Dr. Salgado dice que falta muy poco para la medicina. Ten cuidado con la enfermera Clara cuando se apagan las luces...\"");

            CheckHospitalC1Completion();
        }

        private void HandleClaraInteraction(DualRoleCharacter clara)
        {
            if (hasPacifiedClara || clara.isPacified)
            {
                OnDialogueTriggered?.Invoke(clara.displayName,
                    "\"La presión de oxígeno puro de la cámara hiperbárica limpió mis pulmones de las esporas de Kross. Toma el Cilindro de Oxígeno Puro del quirófano y entrégaselo al Dr. Salgado para estabilizar a Sofía.\"");
                return;
            }

            hasSpokenToClaraDay = true;
            OnDialogueTriggered?.Invoke(clara.displayName,
                "\"La pequeña Sofía de la habitación 304 es muy valiente... Yo intento cuidar el ala de pediatría, pero las válvulas de gas anestésico tienen fugas cada noche y siento que pierdo la razón.\"");

            CheckHospitalC1Completion();
        }

        private void CheckHospitalC1Completion()
        {
            StoryNode nodeC1 = currentGraph.GetNode("node_c1_hospital");
            if (nodeC1 != null && nodeC1.state == StoryNodeState.Active)
            {
                bool isNightNow = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
                bool claraStepDone = hasSpokenToClaraDay || hasPacifiedClara || isNightNow;

                int progress = (hasSpokenToSalgado ? 1 : 0) + (hasVisitedSofiaRoom304 ? 1 : 0) + (claraStepDone ? 1 : 0);
                nodeC1.currentItemProgress = progress;

                if (hasSpokenToSalgado && hasVisitedSofiaRoom304 && claraStepDone)
                {
                    nodeC1.currentItemProgress = nodeC1.requiredItemCount;
                    CompleteNodeAndUnlockChildren("node_c1_hospital");
                    EmitRadioTransmission("Dra. Elena Vargas [Radio]",
                        "Ya viste a Sofía, al Dr. Salgado y a la enfermera Clara. Dirígete al quirófano del Hospital. Cuando Clara Valdés entre en estado feral, atráela a la Cámara Hiperbárica y usa tu Descargador Estático [Tecla 3] o [F] en la válvula para pacificarla, tomar el Cilindro de Oxígeno Puro y entregárselo al Dr. Salgado.");
                }
            }
            RefreshPointerAndUI();
        }

        private void HandleVaneInteraction(DualRoleCharacter vane)
        {
            hasSpokenToVane = true;
            if (HasKeyItem("Cilindro de Oxígeno Puro"))
            {
                CompleteNodeIfNotDone("node_c2_clara");
            }
            StoryNode nodeD1 = currentGraph.GetNode("node_d1_torre");
            if (nodeD1 != null && nodeD1.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_d1_torre");
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddTrampa(TipoTrampa.Madera);
            }

            OnDialogueTriggered?.Invoke(vane.displayName,
                "\"Deserté del escuadrón ECLIPSE porque Kross planea liberar una sobrecarga solar permanente sobre toda la ciudad. Desactivé las cerraduras hacia la celda de la Dra. Elena Vargas. Toma esta carga extra para tu Descargador Estático [Tecla 3]: la necesitarás contra los 3 Condensadores Fotónicos de Kross.\"");

            RefreshPointerAndUI();
        }

        private void HandleElenaInteraction(DualRoleCharacter elena)
        {
            if (HasKeyItem("Cilindro de Oxígeno Puro"))
            {
                CompleteNodeIfNotDone("node_c2_clara");
            }
            StoryNode nodeD1 = currentGraph.GetNode("node_d1_torre");
            if (nodeD1 != null && nodeD1.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_d1_torre");
            }

            hasSpokenToElenaTower = true;

            if (!HasKeyItem("Suero Alba-Puro"))
            {
                GrantKeyItem("Suero Alba-Puro", "Cura definitiva sintetizada por la Dra. Elena Vargas con la Enzima Base, el Filtro Biológico y el Oxígeno Puro.");
                OnDialogueTriggered?.Invoke(elena.displayName,
                    "\"¡Lo lograste, Kilo! Con la Enzima Base, el Filtro de Carbón Biológico y el Oxígeno Puro he sintetizado el SUERO ALBA-PURO. Ahora sube al núcleo: sobrecarga los 3 Condensadores Fotónicos UV [F o Trampa 3] para apagar el exoesqueleto del Dr. Kross e inserta el suero en el Inyector Central.\"");
            }
            else
            {
                OnDialogueTriggered?.Invoke(elena.displayName,
                    $"\"Ya tienes el Suero Alba-Puro. Condensadores Fotónicos desactivados: ({overloadedCapacitorsCount}/3). Cuando apagues los 3, inserta el suero en el Inyector Central.\"");
            }

            RefreshPointerAndUI();
        }

        // =========================================================================================
        // INTERACCIÓN CON MECANISMOS DE MISIÓN (StoryMechanismPrototipo3)
        // =========================================================================================

        public void HandleMechanismInteraction(StoryMechanismPrototipo3 mechanism, bool fromStaticDischarge)
        {
            if (mechanism == null || currentGraph == null) return;

            switch (mechanism.mechanismType)
            {
                case StoryMechanismType.MoralesOfficeDesk:
                    HandleMoralesOfficeSpy(mechanism);
                    break;

                case StoryMechanismType.SchoolChemistryLab:
                    HandleChemistryLabBlackout(mechanism);
                    break;

                case StoryMechanismType.ReflectorPole:
                    HandleReflectorPoleRepair(mechanism);
                    break;

                case StoryMechanismType.StreamSubstationSwitch:
                    HandleReflectorSubstationSwitch(mechanism);
                    break;

                case StoryMechanismType.HyperbaricChamberValve:
                    HandleHyperbaricValve(mechanism, fromStaticDischarge);
                    break;

                case StoryMechanismType.PureOxygenCylinder:
                    HandleOxygenCylinderPickup(mechanism);
                    break;

                case StoryMechanismType.ElenaReactorCell:
                    mechanism.MarkCompleted();
                    if (!HasKeyItem("Suero Alba-Puro"))
                    {
                        GrantKeyItem("Suero Alba-Puro", "Cura definitiva sintetizada por la Dra. Elena Vargas.");
                    }
                    CompleteNodeIfNotDone("node_d1_torre");
                    SetNodeState("node_d2_kross", StoryNodeState.Active, setFocus: true);
                    OnDialogueTriggered?.Invoke("Dra. Elena Vargas",
                        "\"¡Gracias por abrir la celda de contención, Kilo! Aquí tienes el Suero Alba-Puro. Sobrecarga los 3 Condensadores Fotónicos UV y colócalo en el Inyector Central.\"");
                    RefreshPointerAndUI();
                    break;

                case StoryMechanismType.PhotonicUVCapacitor:
                    HandleUVCapacitorOverload(mechanism, fromStaticDischarge);
                    break;

                case StoryMechanismType.CentralSerumInjector:
                    HandleCentralInjectorFinal(mechanism);
                    break;
            }
        }

        private void HandleMoralesOfficeSpy(StoryMechanismPrototipo3 mechanism)
        {
            StoryNode nodeA1 = currentGraph.GetNode("node_a1_escuela");
            if (nodeA1 != null && nodeA1.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_a1_escuela");
            }

            hasSpiedMoralesOffice = true;
            mechanism.MarkCompleted();

            StoryNode nodeA2 = currentGraph.GetNode("node_a2_toby");
            if (nodeA2 != null && nodeA2.state == StoryNodeState.Active)
            {
                CompleteNodeAndUnlockChildren("node_a2_toby");
            }

            OnDialogueTriggered?.Invoke("Bitácora del Rector Morales",
                "\"He guardado la Llave Maestra del Conserje y el último Vial de Enzima Base bajo llave en el Laboratorio de Química. Si algún intruso toca el estante, el sistema cortará la energía del ala académica.\"");
            RefreshPointerAndUI();
        }

        private void HandleChemistryLabBlackout(StoryMechanismPrototipo3 mechanism)
        {
            if (HasKeyItem("Vial de Enzima Base"))
            {
                OnDialogueTriggered?.Invoke("Laboratorio de Química", "Ya recuperaste la Llave Maestra del Conserje y el Vial de Enzima Base.");
                return;
            }

            CompleteNodeIfNotDone("node_a1_escuela");
            CompleteNodeIfNotDone("node_a2_toby");
            SetNodeState("node_a3_apagon", StoryNodeState.Active, setFocus: true);

            hasTriggeredSchoolBlackout = true;
            mechanism.MarkCompleted();

            GrantKeyItem("Llave Maestra del Conserje", "Abre los accesos de mantenimiento de la Escuela Secundaria Bellavista.");
            GrantKeyItem("Vial de Enzima Base", "Precursor bioquímico (Rama A) requerido para sintetizar el Suero Alba-Puro.");

            // Forzar al Rector H. Morales a entrar en modo feral por el apagón
            if (worldBinder != null && worldBinder.SpawnedMorales != null)
            {
                worldBinder.SpawnedMorales.ForceFeralState(true);
            }

            Transform moralesTarget = worldBinder != null && worldBinder.SpawnedMorales != null
                ? worldBinder.SpawnedMorales.transform
                : mechanism.transform;

            PlayCutsceneSafe(
                "04_Buho_Apagon.mp4",
                "APAGÓN EN EL LABORATORIO: EL SILENCIADOR",
                "Al tomar el Vial de Enzima Base, los fusibles estallan. En la penumbra, los ojos dorados del Rector H. Morales giran 300 grados buscando el menor ruido.",
                moralesTarget,
                () =>
                {
                    CompleteNodeAndUnlockChildren("node_a3_apagon");
                    CheckHospitalConvergenceUnlock();
                    EmitRadioTransmission("Dra. Elena Vargas [Radio]",
                        "¡Tienes el Vial de Enzima Base! ¡Agáchate con [C] y lanza un Señuelo Acústico [1] para distraer al Búho Morales y salir de la escuela!");
                    RefreshPointerAndUI();
                });
        }

        private void HandleReflectorPoleRepair(StoryMechanismPrototipo3 mechanism)
        {
            if (mechanism.isCompleted)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName, "Este poste reflector de luz fría ya está reparado y conectado al arroyo.");
                return;
            }

            InventoryManager inv = InventoryManager.Instance;
            if (inv != null)
            {
                bool hasMaterials = inv.HasEnoughItem(ItemType.Madera, 1) &&
                                    inv.HasEnoughItem(ItemType.Goma, 1) &&
                                    inv.HasEnoughItem(ItemType.Metal, 1);

                if (hasMaterials)
                {
                    inv.RemoveItem(ItemType.Madera, 1);
                    inv.RemoveItem(ItemType.Goma, 1);
                    inv.RemoveItem(ItemType.Metal, 1);
                }
                else
                {
                    OnDialogueTriggered?.Invoke(mechanism.displayName,
                        "Necesitas 1 Madera, 1 Goma y 1 Metal para aislar el cableado de este poste reflector (saquea contenedores cercanos con [F]).");
                    return;
                }
            }

            CompleteNodeIfNotDone("node_b1_bosque");
            mechanism.MarkCompleted();
            repairedReflectorPolesCount = Mathf.Clamp(repairedReflectorPolesCount + 1, 0, 3);

            StoryNode nodeB2 = currentGraph.GetNode("node_b2_postes");
            if (nodeB2 != null)
            {
                nodeB2.state = StoryNodeState.Active;
                nodeB2.currentItemProgress = repairedReflectorPolesCount;
            }

            if (repairedReflectorPolesCount >= 3)
            {
                CompleteNodeAndUnlockChildren("node_b2_postes");
                OnDialogueTriggered?.Invoke("Sistema del Arroyo",
                    "¡Los 3 Postes Reflectores están reparados (3/3)! Espera o actúa de noche, usa la Bomba de Polen [Tecla 2] cerca de Sultán y acciona el Interruptor de la Subestación del Arroyo.");
            }
            else
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName,
                    $"Poste reflector reparado ({repairedReflectorPolesCount}/3). Continúa con los demás postes del arroyo.");
            }

            RefreshPointerAndUI();
        }

        private void HandleReflectorSubstationSwitch(StoryMechanismPrototipo3 mechanism)
        {
            if (hasPacifiedSultan)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName, "Los reflectores de luz fría ya brillan sobre el arroyo y Sultán ha recobrado la cordura.");
                return;
            }

            if (repairedReflectorPolesCount < 3)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName,
                    $"Aún faltan postes por reparar ({repairedReflectorPolesCount}/3). Repara los 3 postes reflectores con Madera, Goma y Metal antes de encender la subestación.");
                return;
            }

            bool isNight = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            if (!isNight)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName,
                    "Los reflectores están listos, pero Sultán sólo entra en estado feral de noche. Espera al anochecer para atraerlo a la luz fría y curar su frenesí permanentemente.");
                return;
            }

            hasPacifiedSultan = true;
            mechanism.MarkCompleted();

            if (worldBinder != null && worldBinder.SpawnedSultan != null)
            {
                worldBinder.SpawnedSultan.PacifyBoss();
            }

            GrantKeyItem("Filtro de Carbón Biológico", "Precursor de filtración (Rama B) entregado por el guardabosques Sultán.");
            GrantKeyItem("Llave del Hospital", "Llave de acceso al ala pediátrica y quirófano del Hospital General San Judas.");

            Transform sultanTarget = worldBinder != null && worldBinder.SpawnedSultan != null
                ? worldBinder.SpawnedSultan.transform
                : mechanism.transform;

            PlayCutsceneSafe(
                "05_Sultan_LuzFria.mp4",
                "LUZ FRÍA EN EL ARROYO: LA REDENCIÓN DE SULTÁN",
                "Los tres reflectores inundan el arroyo con un destello azulado. El humo carmesí se disipa de los ojos del lobo Sultán, quien cae de rodillas agradecido.",
                sultanTarget,
                () =>
                {
                    CompleteNodeAndUnlockChildren("node_b3_sultan");
                    CheckHospitalConvergenceUnlock();
                    OnDialogueTriggered?.Invoke("Sultán (Guardabosques)",
                        "\"Me devolviste el alma, pequeño Kilo. Toma el Filtro de Carbón Biológico y la Llave del Hospital San Judas. Nunca olvidaré lo que hiciste por mí.\"");
                    RefreshPointerAndUI();
                });
        }

        private void CheckHospitalConvergenceUnlock()
        {
            if (HasKeyItem("Vial de Enzima Base") && HasKeyItem("Filtro de Carbón Biológico"))
            {
                StoryNode nodeC1 = currentGraph.GetNode("node_c1_hospital");
                if (nodeC1 != null && nodeC1.state != StoryNodeState.Completed && nodeC1.state != StoryNodeState.Active)
                {
                    SetNodeState("node_c1_hospital", StoryNodeState.Active, setFocus: true);
                    CityGeneratorPrototipo3 cityGen = CityGeneratorPrototipo3.Instance;
                    string dirHosp = cityGen != null ? cityGen.GetCardinalDirectionFromPlayer("Hospital") : "Sur";
                    EmitRadioTransmission("Dra. Elena Vargas [Radio 94.7 MHz]",
                        $"¡Excelente trabajo, Kilo! Ya tienes el Vial de Enzima Base y el Filtro de Carbón Biológico. Dirígete al Hospital General San Judas hacia el {dirHosp} para ver a Sofía y conseguir el Cilindro de Oxígeno Puro.");
                }
            }
        }

        private void HandleHyperbaricValve(StoryMechanismPrototipo3 mechanism, bool fromStaticDischarge)
        {
            if (hasPacifiedClara)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName, "La Cámara Hiperbárica ya está presurizada y Clara Valdés se encuentra fuera de peligro.");
                return;
            }

            CompleteNodeIfNotDone("node_c1_hospital");
            SetNodeState("node_c2_clara", StoryNodeState.Active, setFocus: true);

            hasPacifiedClara = true;
            mechanism.MarkCompleted();

            if (worldBinder != null && worldBinder.SpawnedClara != null)
            {
                worldBinder.SpawnedClara.PacifyBoss();
            }

            OnDialogueTriggered?.Invoke("Cámara Hiperbárica",
                fromStaticDischarge
                    ? "¡El Descargador Estático [Trampa 3] sobrecargó la válvula electromagnética! La Cámara Hiperbárica sella sus compuertas y purifica el gas anestésico de Clara Valdés. ¡Ahora toma el Cilindro de Oxígeno Puro!"
                    : "¡Válvula de la Cámara Hiperbárica activada! El oxígeno a presión disipa las nubes anestésicas y pacifica a Clara Valdés. ¡Recoge el Cilindro de Oxígeno Puro en el quirófano!");

            RefreshPointerAndUI();
        }

        private void HandleOxygenCylinderPickup(StoryMechanismPrototipo3 mechanism)
        {
            if (HasKeyItem("Cilindro de Oxígeno Puro"))
            {
                StoryNode nodeC2Check = currentGraph != null ? currentGraph.GetNode("node_c2_clara") : null;
                if (nodeC2Check != null && nodeC2Check.state != StoryNodeState.Completed)
                {
                    DeliverOxygenCylinderAndTriggerKrossCutscene(mechanism.transform);
                }
                else
                {
                    OnDialogueTriggered?.Invoke(mechanism.displayName, "Ya recogiste el Cilindro de Oxígeno Puro y estabilizaste a Sofía.");
                }
                return;
            }

            if (!hasPacifiedClara)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName,
                    "Clara Valdés custodia el quirófano. Activa primero la Válvula de la Cámara Hiperbárica (con [F] o con tu Descargador Estático [Tecla 3]) para asegurar el área.");
                return;
            }

            mechanism.MarkCompleted();
            GrantKeyItem("Cilindro de Oxígeno Puro", "Tercer componente médico obtenido en el quirófano del Hospital General San Judas.");

            StoryNode nodeC2 = currentGraph != null ? currentGraph.GetNode("node_c2_clara") : null;
            if (nodeC2 != null)
            {
                nodeC2.currentItemProgress = 1;
                if (worldBinder != null && worldBinder.SpawnedSalgado != null)
                {
                    nodeC2.worldPosition = worldBinder.SpawnedSalgado.transform.position;
                }
            }

            OnDialogueTriggered?.Invoke(mechanism.displayName,
                "¡Recuperaste el Cilindro de Oxígeno Puro! Entrégaselo al Dr. Arturo Salgado [F] en el Hospital para estabilizar a Sofía (o presiona [F] nuevamente aquí para conectarlo al sistema central).");
            RefreshPointerAndUI();
        }

        private void DeliverOxygenCylinderAndTriggerKrossCutscene(Transform focusTransform)
        {
            StoryNode nodeC2 = currentGraph != null ? currentGraph.GetNode("node_c2_clara") : null;
            if (nodeC2 != null)
            {
                nodeC2.currentItemProgress = nodeC2.requiredItemCount;
            }

            PlayCutsceneSafe(
                "06_Hospital_Kross.mp4",
                "TRANSMISIÓN INTERCEPTADA: AMENAZA EN LA TORRE",
                "Al estabilizar a Sofía con el oxígeno puro, en los monitores del hospital aparece el Dr. Julián Kross desde la Torre Aethelgard: \"Al amanecer, los Condensadores Fotónicos harán irreversible la fase feral\".",
                focusTransform,
                () =>
                {
                    CompleteNodeAndUnlockChildren("node_c2_clara");
                    SetNodeState("node_d1_torre", StoryNodeState.Active, setFocus: true);
                    CityGeneratorPrototipo3 cityGen = CityGeneratorPrototipo3.Instance;
                    string dirTorre = cityGen != null ? cityGen.GetCardinalDirectionFromPlayer("Torre") : "Noreste";
                    EmitRadioTransmission("Agente Vane [Frecuencia Cifrada]",
                        $"Kilo, soy el Agente Vane. Tengo a la Dra. Elena Vargas a salvo en el nivel inferior de la Torre Aethelgard, al {dirTorre} de tu posición. Trae los tres ingredientes rápido.");
                    RefreshPointerAndUI();
                });
        }

        private void HandleUVCapacitorOverload(StoryMechanismPrototipo3 mechanism, bool fromStaticDischarge)
        {
            if (mechanism.isCompleted)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName, "Este Condensador Fotónico UV ya fue sobrecargado y apagado.");
                return;
            }

            CompleteNodeIfNotDone("node_d1_torre");
            SetNodeState("node_d2_kross", StoryNodeState.Active, setFocus: true);

            if (!HasKeyItem("Suero Alba-Puro"))
            {
                GrantKeyItem("Suero Alba-Puro", "Cura definitiva sintetizada por la Dra. Elena Vargas.");
            }

            mechanism.MarkCompleted();
            overloadedCapacitorsCount = Mathf.Clamp(overloadedCapacitorsCount + 1, 0, 3);

            StoryNode nodeD2 = currentGraph.GetNode("node_d2_kross");
            if (nodeD2 != null)
            {
                nodeD2.currentItemProgress = overloadedCapacitorsCount;
            }

            if (overloadedCapacitorsCount >= 3)
            {
                if (worldBinder != null && worldBinder.SpawnedKross != null)
                {
                    worldBinder.SpawnedKross.PacifyBoss();
                }

                OnDialogueTriggered?.Invoke("Núcleo Aethelgard",
                    "¡Los 3 Condensadores Fotónicos UV han sido destruidos (3/3)! El exoesqueleto del Dr. Julián Kross ha colapsado. ¡Corre al Inyector Central [F] e inserta el Suero Alba-Puro!");
            }
            else
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName,
                    $"¡Condensador Fotónico UV sobrecargado ({overloadedCapacitorsCount}/3)! Esquiva al Dr. Kross y apaga los condensadores restantes.");
            }

            RefreshPointerAndUI();
        }

        private void HandleCentralInjectorFinal(StoryMechanismPrototipo3 mechanism)
        {
            if (hasCompletedGame) return;

            if (overloadedCapacitorsCount < 3)
            {
                OnDialogueTriggered?.Invoke(mechanism.displayName,
                    $"El escudo fotónico de Kross bloquea el Inyector Central. Debes sobrecargar primero los 3 Condensadores Fotónicos UV ({overloadedCapacitorsCount}/3) usando [F] o el Descargador Estático [Tecla 3].");
                return;
            }

            mechanism.MarkCompleted();
            hasCompletedGame = true;

            if (worldBinder != null && worldBinder.SpawnedKross != null)
            {
                worldBinder.SpawnedKross.PacifyBoss();
            }

            PlayCutsceneSafe(
                "07_Final_Amanecer.mp4",
                "EL AMANECER DE SOLIS: SUERO ALBA-PURO",
                "El Inyector Central dispersa una nube dorada sobre toda la ciudad. La niebla tóxica se disipa, los ciudadanos despiertan en paz y Sofía abraza a Kilo bajo la luz del nuevo día.",
                mechanism.transform,
                () =>
                {
                    CompleteNodeIfNotDone("node_d2_kross");
                    if (TimeManager.Instance != null)
                    {
                        TimeManager.Instance.SetExactHour(6.0f);
                    }
                    OnDemoCompleted?.Invoke();
                    RefreshPointerAndUI();
                });
        }

        // =========================================================================================
        // EVENTOS DE BOTÍN, TRAMPAS, RADIO Y RUMORES
        // =========================================================================================

        private void HandleContainerLooted(LootContainer container, List<ItemType> items)
        {
            containersLootedCount++;

            StoryNode node0 = currentGraph != null ? currentGraph.GetNode("node_0_apertura") : null;
            if (node0 != null && node0.state == StoryNodeState.Active)
            {
                node0.currentItemProgress = Mathf.Min(node0.requiredItemCount, node0.currentItemProgress + 1);
                OnActiveNodeChanged?.Invoke(node0);
            }
        }

        private void HandleLegacyTrapCrafted(TipoTrampa tipo)
        {
            OnAnyTrapCrafted(tipo);
        }

        private void HandlePrototipo3TrapCrafted(TipoTrampa tipo)
        {
            OnAnyTrapCrafted(tipo);
        }

        private void OnAnyTrapCrafted(TipoTrampa tipo)
        {
            hasCraftedInitialTool = true;
            StoryNode node0 = currentGraph != null ? currentGraph.GetNode("node_0_apertura") : null;
            if (node0 != null && node0.state == StoryNodeState.Active)
            {
                node0.currentItemProgress = node0.requiredItemCount;
                if (hasSpokenToMarcoDay)
                {
                    EmitRadioTransmission("Dra. Elena Vargas [Radio]",
                        $"¡Excelente! Ya fabricaste {TrapSystemPrototipo3.GetTrapDisplayName(tipo)} y hablaste con Marco. Cuando caiga la noche o salgas a explorar, se abrirán las rutas hacia la Escuela y el Bosque.");
                    UnlockBranchesAfterOpening();
                }
                else
                {
                    OnActiveNodeChanged?.Invoke(node0);
                }
            }
        }

        public void NotifyRumorFromContainer(string rumorText)
        {
            LastCardinalRumor = rumorText;
            OnDialogueTriggered?.Invoke("Nota de Contenedor [Brújula]", rumorText);
        }

        public void NotifyRumorFromContainer(LootContainer container, string rumorText)
        {
            NotifyRumorFromContainer(rumorText);
        }

        public void EmitRadioTransmission(string frequencySpeaker, string message)
        {
            LastRadioMessage = $"{frequencySpeaker}: {message}";
            OnRadioTransmission?.Invoke(frequencySpeaker, message);
            OnDialogueTriggered?.Invoke(frequencySpeaker, message);
        }

        public bool HasKeyItem(string itemName)
        {
            return !string.IsNullOrEmpty(itemName) && keyItems.Contains(itemName);
        }

        public void GrantKeyItem(string itemName, string description = "")
        {
            if (string.IsNullOrEmpty(itemName)) return;
            if (keyItems.Add(itemName))
            {
                debugKeyItemsList.Add(itemName);
                Debug.Log($"[StoryManagerPrototipo3] Objeto Clave Obtenido: {itemName} - {description}");
                OnKeyItemObtained?.Invoke(itemName);
            }
        }

        public void InteractuarConLulu(LuluNPCPrototipo3 luluNpc = null)
        {
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;
            CityGeneratorPrototipo3 cityGen = CityGeneratorPrototipo3.Instance;
            if (cityGen != null)
            {
                string dirEsc = cityGen.GetCardinalDirectionFromPlayer("Escuela");
                string dirBos = cityGen.GetCardinalDirectionFromPlayer("Bosque");
                string dirHos = cityGen.GetCardinalDirectionFromPlayer("Hospital");
                string dirTor = cityGen.GetCardinalDirectionFromPlayer("Torre");
                OnDialogueTriggered?.Invoke("Lulú (Informante)",
                    $"\"¡Hola, Kilo! Según mis notas: la Escuela Bellavista queda al {dirEsc}, el Bosque Ceniza al {dirBos}, el Hospital San Judas al {dirHos} y la Torre Aethelgard al {dirTor}.\"");
            }
            else
            {
                OnDialogueTriggered?.Invoke("Lulú (Informante)",
                    "¡Hola, Kilo! Recuerda usar [1] Señuelo Acústico contra el Búho Morales, [2] Bomba de Polen contra el Lobo Sultán y [3] Descargador Estático en el Hospital y la Torre.");
            }
        }

        // =========================================================================================
        // UTILIDADES DEL GRAFO NARRATIVO (DAG)
        // =========================================================================================

        private void CompleteNodeIfNotDone(string nodeId)
        {
            if (currentGraph == null) return;
            StoryNode node = currentGraph.GetNode(nodeId);
            if (node != null && node.state != StoryNodeState.Completed)
            {
                node.state = StoryNodeState.Completed;
            }
        }

        private void CompleteNodeAndUnlockChildren(string nodeId)
        {
            if (currentGraph == null) return;
            StoryNode node = currentGraph.GetNode(nodeId);
            if (node == null) return;

            node.state = StoryNodeState.Completed;
            Debug.Log($"[StoryManagerPrototipo3] ¡Nodo Completado!: {node.title} ({node.id})");

            foreach (string childId in node.nextNodeIds)
            {
                StoryNode child = currentGraph.GetNode(childId);
                if (child == null || child.state == StoryNodeState.Completed) continue;

                if (currentGraph.ArePrerequisitesCompleted(child))
                {
                    child.state = StoryNodeState.Active;
                    currentActiveNode = child;
                    Debug.Log($"[StoryManagerPrototipo3] Nuevo Nodo Desbloqueado en DAG: {child.title} ({child.id})");
                }
            }

            UpdatePrimaryActiveNodeFocus();
            RefreshPointerAndUI();
        }

        private void SetNodeState(string nodeId, StoryNodeState state, bool setFocus)
        {
            if (currentGraph == null) return;
            StoryNode node = currentGraph.GetNode(nodeId);
            if (node == null) return;

            node.state = state;
            if (setFocus && state == StoryNodeState.Active)
            {
                currentActiveNode = node;
            }
            else if (currentActiveNode == null || currentActiveNode.state != StoryNodeState.Active)
            {
                UpdatePrimaryActiveNodeFocus();
            }
        }

        private void UpdatePrimaryActiveNodeFocus()
        {
            if (currentGraph == null) return;
            List<StoryNode> activeList = currentGraph.GetActiveNodes();
            if (activeList.Count == 0) return;

            if (currentActiveNode != null && currentActiveNode.state == StoryNodeState.Active)
            {
                return;
            }

            if (player != null)
            {
                StoryNode closest = activeList[0];
                float bestDist = Vector3.Distance(player.position, closest.worldPosition);
                for (int i = 1; i < activeList.Count; i++)
                {
                    float d = Vector3.Distance(player.position, activeList[i].worldPosition);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        closest = activeList[i];
                    }
                }
                currentActiveNode = closest;
            }
            else
            {
                currentActiveNode = activeList[0];
            }
        }

        public void RefreshPointerAndUI()
        {
            UpdatePrimaryActiveNodeFocus();
            if (missionPointer != null && currentActiveNode != null)
            {
                missionPointer.ActualizarObjetivoPorNodo(currentActiveNode);
            }
            if (currentActiveNode != null)
            {
                OnActiveNodeChanged?.Invoke(currentActiveNode);
            }
        }

        public string GetActiveBranchesSummaryText()
        {
            if (currentGraph == null) return "Inicializando historia...";
            List<StoryNode> activeNodes = currentGraph.GetActiveNodes();
            if (activeNodes.Count == 0)
            {
                return hasCompletedGame
                    ? "¡Solis ha sido liberada! El Suero Alba-Puro curó a Sofía y a la ciudad."
                    : "Explora la ciudad de Solis.";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < activeNodes.Count; i++)
            {
                StoryNode n = activeNodes[i];
                if (i > 0) sb.Append("\n\n");
                sb.Append($"• {n.title}: {n.description}");
                if (n.requiredItemCount > 1)
                {
                    sb.Append($" ({n.currentItemProgress}/{n.requiredItemCount})");
                }
            }
            return sb.ToString();
        }

        private void PlayCutsceneSafe(string fileName, string title, string subtitle, Transform focusTarget, Action onFinished)
        {
            if (CutsceneManager.Instance != null)
            {
                CutsceneManager.Instance.PlayCutscene(fileName, focusTarget, $"{title}\n{subtitle}", onFinished);
            }
            else
            {
                onFinished?.Invoke();
            }
        }
    }
}
