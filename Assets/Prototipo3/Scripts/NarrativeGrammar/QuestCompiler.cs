using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    /// <summary>
    /// Enlaza cada TerminalAction derivada por la CFG con entidades reales del mundo procedural
    /// (NPCs, contenedores de loot dispersos, mecanismos e interruptores en WorldRegistry).
    /// </summary>
    public class BindingManager
    {
        public static void BindActionsToWorld(List<TerminalAction> actions, string storyNodeId, CityGeneratorPrototipo3 cityGen)
        {
            if (actions == null || actions.Count == 0) return;

            Vector3 previousStepPos = cityGen != null ? cityGen.PlayerSpawnPoint : Vector3.zero;
            int lastQuadrant = 0;
            HashSet<LootContainer> usedContainers = new HashSet<LootContainer>();

            for (int i = 0; i < actions.Count; i++)
            {
                TerminalAction act = actions[i];
                act.targetStoryNodeID = storyNodeId;

                switch (act.actionType)
                {
                    case "hablar_npc":
                        BindHablarNpc(act, storyNodeId, ref previousStepPos, cityGen);
                        break;

                    case "saquear_contenedor":
                    case "leer_nota":
                        BindContenedorONota(act, storyNodeId, ref previousStepPos, ref lastQuadrant, cityGen, usedContainers);
                        break;

                    case "fabricar_trampa_madera":
                    case "fabricar_trampa_red":
                    case "fabricar_trampa_puas":
                        BindFabricarTrampa(act, cityGen);
                        break;

                    case "activar_interruptor":
                    case "colarse_ducto":
                        BindInterruptorODucto(act, storyNodeId, ref previousStepPos, cityGen);
                        break;

                    case "colocar_trampa_madera":
                    case "colocar_trampa_red":
                    case "colocar_trampa_puas":
                        BindColocarTrampa(act, storyNodeId, previousStepPos);
                        break;

                    case "agacharse_sigilo":
                    case "correr_estamina":
                    case "dash_escape":
                    case "romper_linea_vision":
                        BindAccionTactica(act, previousStepPos);
                        break;

                    case "inyectar_antidoto_vial":
                        BindInyectarAntidoto(act, storyNodeId, previousStepPos);
                        break;

                    case "espiar_rutina":
                        BindEspiarRutina(act, storyNodeId, previousStepPos);
                        break;

                    default:
                        act.boundPosition = previousStepPos;
                        act.readableDescription = $"{act.actionType} ({act.argument})";
                        break;
                }

                if (cityGen != null && act.boundPosition != Vector3.zero)
                {
                    act.cardinalHint = cityGen.GetCardinalDirectionHint(cityGen.PlayerSpawnPoint, act.boundPosition);
                }
            }
        }

        private static void BindHablarNpc(TerminalAction act, string storyNodeId, ref Vector3 previousPos, CityGeneratorPrototipo3 cityGen)
        {
            // Determinar si es un NPC clave del nodo o un informante de la calle disperso
            Transform targetNpc = null;

            if (storyNodeId.Contains("prologo") || storyNodeId.Contains("apertura") || act.argument.Contains("Marco"))
            {
                targetNpc = WorldRegistry.Get("Marco");
                act.readableDescription = "Habla con el tejón Marco [F] cerca de tu Base";
            }
            else if (storyNodeId.Contains("escuela") || act.argument.Contains("Toby"))
            {
                targetNpc = WorldRegistry.Get("Toby") ?? WorldRegistry.Get("Escuela");
                act.readableDescription = "Habla con Toby en su taller de robótica (Edificio Especial) [F]";
            }
            else if (storyNodeId.Contains("bosque") || act.argument.Contains("Moka") || act.argument.Contains("Sultan"))
            {
                targetNpc = WorldRegistry.Get("Bosque");
                act.readableDescription = "Habla con los aliados del Bosque Ceniza [F]";
            }
            else if (storyNodeId.Contains("hospital") || act.argument.Contains("Salgado") || act.argument.Contains("Sofia"))
            {
                targetNpc = WorldRegistry.Get("Hospital");
                act.readableDescription = "Habla con el Dr. Salgado en el Hospital San Judas [F]";
            }
            else if (storyNodeId.Contains("torre") || act.argument.Contains("Vane") || act.argument.Contains("Elena"))
            {
                targetNpc = WorldRegistry.Get("Torre");
                act.readableDescription = "Contacta al Agente Vane en la Torre Aethelgard [F]";
            }
            else
            {
                // Informante secundario en cuadrante opuesto/intermedio
                DualRoleCharacter[] citizens = GameObject.FindObjectsByType<DualRoleCharacter>(FindObjectsSortMode.None);
                for (int i = 0; i < citizens.Length; i++)
                {
                    if (citizens[i] != null && !citizens[i].IsCurrentlyFeral && citizens[i].roleType != CharacterRoleType.PacifiableBoss)
                    {
                        targetNpc = citizens[i].transform;
                        break;
                    }
                }
                act.readableDescription = "Consulta a un ciudadano en la calle [F] sobre el humo ámbar";
            }

            if (targetNpc != null)
            {
                act.boundTarget = targetNpc;
                act.boundPosition = targetNpc.position;
                previousPos = targetNpc.position;
            }
            else
            {
                act.boundPosition = previousPos;
            }
        }

        private static void BindContenedorONota(TerminalAction act, string storyNodeId, ref Vector3 previousPos, ref int lastQuadrant, CityGeneratorPrototipo3 cityGen, HashSet<LootContainer> usedContainers)
        {
            // Dispersión obligatoria: buscar contenedor a dM >= 4 celdas en cuadrante opuesto
            int nextQuadrant = (lastQuadrant + 2) % 4;
            lastQuadrant = nextQuadrant;

            LootContainer chosenContainer = null;
            if (cityGen != null)
            {
                List<LootContainer> candidates = cityGen.GetUnlootedContainersFarFrom(previousPos, minCellDistance: 4, preferredQuadrant: nextQuadrant);
                if (candidates != null && candidates.Count > 0)
                {
                    for (int c = 0; c < candidates.Count; c++)
                    {
                        if (!usedContainers.Contains(candidates[c]))
                        {
                            chosenContainer = candidates[c];
                            usedContainers.Add(chosenContainer);
                            break;
                        }
                    }
                    if (chosenContainer == null) chosenContainer = candidates[0];
                }
            }

            if (chosenContainer == null)
            {
                LootContainer[] all = GameObject.FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && !all[i].isLooted && !usedContainers.Contains(all[i]))
                    {
                        chosenContainer = all[i];
                        usedContainers.Add(chosenContainer);
                        break;
                    }
                }
            }

            if (chosenContainer != null)
            {
                act.boundTarget = chosenContainer.transform;
                act.boundPosition = chosenContainer.transform.position;
                previousPos = chosenContainer.transform.position;

                if (act.actionType == "leer_nota")
                {
                    Transform targetLandmark = null;
                    if (storyNodeId.Contains("escuela") || storyNodeId.Contains("node_a")) targetLandmark = WorldRegistry.Get("Escuela");
                    else if (storyNodeId.Contains("bosque") || storyNodeId.Contains("node_b")) targetLandmark = WorldRegistry.Get("Bosque");
                    else if (storyNodeId.Contains("hospital") || storyNodeId.Contains("node_c")) targetLandmark = WorldRegistry.Get("Hospital");
                    else if (storyNodeId.Contains("torre") || storyNodeId.Contains("node_d")) targetLandmark = WorldRegistry.Get("Torre");
                    else targetLandmark = WorldRegistry.Get("Marco") ?? WorldRegistry.Get("Base");

                    Vector3 targetPos = targetLandmark != null ? targetLandmark.position : (previousPos + Vector3.forward * 40f);
                    string hint = cityGen != null ? cityGen.GetCardinalDirectionHint(chosenContainer.transform.position, targetPos) : "al Norte";
                    chosenContainer.injectedNoteId = $"nota_{storyNodeId}_{act.argument}";
                    chosenContainer.injectedNoteText = $"[Nota Arrugada]: 'El catalizador se oculta {hint}. Ten cuidado de noche.'";
                    act.readableDescription = $"Inspecciona el contenedor para leer la nota oculta [F] ({chosenContainer.name})";
                }
                else
                {
                    chosenContainer.containerTagFilter = act.argument;
                    act.readableDescription = $"Registra el contenedor urbano [F] en busca de {act.argument}";
                }
            }
            else
            {
                act.boundPosition = previousPos + Vector3.forward * 15f;
                act.readableDescription = $"Busca suministros de {act.argument} en los contenedores de la zona";
            }
        }

        private static void BindFabricarTrampa(TerminalAction act, CityGeneratorPrototipo3 cityGen)
        {
            Transform tableTransform = WorldRegistry.Get("CraftTable") ?? WorldRegistry.Get("Base");
            if (tableTransform != null)
            {
                act.boundTarget = tableTransform;
                act.boundPosition = tableTransform.position;
            }

            string trapName = act.actionType.Contains("madera") ? "Trampa de Madera (Barricada)"
                            : act.actionType.Contains("red") ? "Trampa de Red (Inmovilizadora)"
                            : "Trampa de Púas (Sobrecarga/Ralentización)";

            act.readableDescription = $"Fabrica 1 {trapName} en la Mesa de la Base";
        }

        private static void BindInterruptorODucto(TerminalAction act, string storyNodeId, ref Vector3 previousPos, CityGeneratorPrototipo3 cityGen)
        {
            StoryMechanismPrototipo3[] mechanisms = GameObject.FindObjectsByType<StoryMechanismPrototipo3>(FindObjectsSortMode.None);
            StoryMechanismPrototipo3 targetMech = null;

            for (int i = 0; i < mechanisms.Length; i++)
            {
                if (mechanisms[i] != null && !mechanisms[i].isCompleted)
                {
                    targetMech = mechanisms[i];
                    break;
                }
            }

            if (targetMech != null)
            {
                act.boundTarget = targetMech.transform;
                act.boundPosition = targetMech.transform.position;
                previousPos = targetMech.transform.position;
                act.readableDescription = $"Acciona {targetMech.displayName} [F]";
            }
            else
            {
                act.boundPosition = previousPos;
                act.readableDescription = $"Activa el mecanismo de paso [F] ({act.argument})";
            }
        }

        private static void BindColocarTrampa(TerminalAction act, string storyNodeId, Vector3 previousPos)
        {
            act.boundPosition = previousPos;
            string tName = act.actionType.Contains("madera") ? "Trampa de Madera [Tecla 3]"
                         : act.actionType.Contains("red") ? "Trampa de Red [Tecla 2]"
                         : "Trampa de Púas [Tecla 1]";
            act.readableDescription = $"Despliega {tName} en la ruta de persecución";
        }

        private static void BindAccionTactica(TerminalAction act, Vector3 previousPos)
        {
            act.boundPosition = previousPos;
            switch (act.actionType)
            {
                case "agacharse_sigilo":
                    act.readableDescription = "Avanza agachado en sigilo con [C] para reducir la visibilidad";
                    break;
                case "correr_estamina":
                    act.readableDescription = "Esprinta con [Shift] administrando tu estamina";
                    break;
                case "dash_escape":
                    act.readableDescription = "Usa el Dash evasivo [Espacio / Q] para romper la persecución";
                    break;
                case "romper_linea_vision":
                    act.readableDescription = "Rompe la línea de visión ocultándote tras un muro o cobertura";
                    break;
            }
        }

        private static void BindInyectarAntidoto(TerminalAction act, string storyNodeId, Vector3 previousPos)
        {
            act.boundPosition = previousPos;
            act.readableDescription = "Aplica el catalizador / suero para pacificar permanentemente al adversario";
        }

        private static void BindEspiarRutina(TerminalAction act, string storyNodeId, Vector3 previousPos)
        {
            act.boundPosition = previousPos;
            act.readableDescription = "Observa en sigilo sin ser detectado la rutina del vigilante";
        }
    }

    /// <summary>
    /// Compilador y Gestor en tiempo de ejecución de las misiones derivadas por la CFG.
    /// </summary>
    public class QuestCompiler : MonoBehaviour
    {
        public static QuestCompiler Instance { get; private set; }

        [Header("Misión Compilada Activa")]
        [SerializeField] private string activeStoryNodeID = "";
        [SerializeField] private List<TerminalAction> compiledActions = new List<TerminalAction>();
        [SerializeField] private int currentActionIndex = 0;

        public IReadOnlyList<TerminalAction> CompiledActions => compiledActions;
        public TerminalAction CurrentAction => (compiledActions != null && currentActionIndex < compiledActions.Count) ? compiledActions[currentActionIndex] : null;
        public int CurrentActionIndex => currentActionIndex;
        public bool IsCurrentQuestComplete => compiledActions == null || compiledActions.Count == 0 || currentActionIndex >= compiledActions.Count;

        public event Action<TerminalAction> OnTerminalActionCompleted;
        public event Action<string> OnQuestCompiled;
        public event Action<string> OnQuestCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this && Application.isPlaying) Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        public static QuestCompiler EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("QuestCompiler");
            Instance = go.AddComponent<QuestCompiler>();
            return Instance;
        }

        /// <summary>
        /// Compila una misión para el nodo macro especificado usando la CFG formal y dispersando los pasos en la ciudad.
        /// </summary>
        public List<TerminalAction> CompileQuestForNode(string storyNodeId, string startSymbol, int seed)
        {
            activeStoryNodeID = storyNodeId;
            currentActionIndex = 0;

            CityGeneratorPrototipo3 cityGen = FindAnyObjectByType<CityGeneratorPrototipo3>();
            compiledActions = QuestGrammarParser.Derivate(startSymbol, seed, maxRecursionDepth: 2);
            BindingManager.BindActionsToWorld(compiledActions, storyNodeId, cityGen);

            Debug.Log($"[QuestCompiler] Misión compilada para '{storyNodeId}' con símbolo '{startSymbol}'. Acciones generadas: {compiledActions.Count}");
            for (int i = 0; i < compiledActions.Count; i++)
            {
                Debug.Log($"  [{i + 1}] {compiledActions[i]}");
            }

            OnQuestCompiled?.Invoke(storyNodeId);
            return compiledActions;
        }

        public bool AdvanceActionIfMatches(string actionType, string argument = "", Transform callerTransform = null)
        {
            if (IsCurrentQuestComplete) return false;

            TerminalAction curr = CurrentAction;
            if (curr == null) return false;

            // Si la acción actual es opcional y la acción realizada coincide con un objetivo obligatorio posterior,
            // permitimos avanzar directamente sin bloquear el avance principal
            if (curr.isOptional && !curr.actionType.Equals(actionType, StringComparison.OrdinalIgnoreCase))
            {
                // Buscar si coincide con alguna acción posterior obligatoria
                for (int i = currentActionIndex + 1; i < compiledActions.Count; i++)
                {
                    if (compiledActions[i].actionType.Equals(actionType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Omitir bloque opcional y avanzar a la obligatoria
                        Debug.Log($"[QuestCompiler] Sub-arco opcional saltado; avanzando a acción obligatoria: {compiledActions[i].readableDescription}");
                        currentActionIndex = i;
                        curr = compiledActions[i];
                        break;
                    }
                }
            }

            bool typeMatches = curr.actionType.Equals(actionType, StringComparison.OrdinalIgnoreCase);

            bool isGenericRoleArg = string.IsNullOrEmpty(curr.argument) ||
                                    curr.argument.Equals("informante_calle", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("testigo_barrio", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("ciudadano_en_apuros", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("objeto_perdido", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("material_reciclado", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("devolucion_objeto", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("pista_cardinal", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("bitacora_perdida", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("vigilancia_patrulla", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("camuflaje_sombras", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("trampa_tactica", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("trampa_tactica_jefe", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("rejilla_ventilacion", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("paso_estrecho", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("panel_puerta_seguridad", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("evasion_inmediata", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("obstaculo_visual", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("refugio_detras_cobertura", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("barricada_pasillo", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("red_suelo", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("puas_electrificadas", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("mecanismo_arena", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("infiltracion_perimetro", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("sombra_arena", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("mecanismo_central_arena", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("retirada_segura", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("suero_restaurador", StringComparison.OrdinalIgnoreCase) ||
                                    curr.argument.Equals("jefe_pacificado", StringComparison.OrdinalIgnoreCase);

            bool targetMatches = callerTransform != null && curr.boundTarget != null &&
                                 (curr.boundTarget == callerTransform || curr.boundTarget.IsChildOf(callerTransform) || callerTransform.IsChildOf(curr.boundTarget));

            bool argMatches = string.IsNullOrEmpty(curr.argument) ||
                              string.IsNullOrEmpty(argument) ||
                              isGenericRoleArg ||
                              targetMatches ||
                              curr.argument.Equals(argument, StringComparison.OrdinalIgnoreCase) ||
                              curr.argument.IndexOf(argument, StringComparison.OrdinalIgnoreCase) >= 0 ||
                              argument.IndexOf(curr.argument, StringComparison.OrdinalIgnoreCase) >= 0;

            if (typeMatches && argMatches)
            {
                curr.isCompleted = true;
                Debug.Log($"[QuestCompiler] ¡Acción completada: {curr.readableDescription}!");
                OnTerminalActionCompleted?.Invoke(curr);

                currentActionIndex++;
                if (IsCurrentQuestComplete)
                {
                    Debug.Log($"[QuestCompiler] ¡Todas las acciones de la misión para '{activeStoryNodeID}' han sido completadas!");
                    OnQuestCompleted?.Invoke(activeStoryNodeID);
                }
                return true;
            }

            return false;
        }

        public void SkipOptionalActions()
        {
            while (!IsCurrentQuestComplete && CurrentAction != null && CurrentAction.isOptional)
            {
                currentActionIndex++;
            }
        }
    }
}
