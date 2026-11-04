using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    // =========================================================================
    // ESTRUCTURAS DEL ÁRBOL SINTÁCTICO DE LA GRAMÁTICA FORMAL G = (VN, Σ, R, S)
    // =========================================================================

    public abstract class GrammarSymbol
    {
        public string symbolName;
        public bool isTerminal;
        public bool isOptional;
        public string ruleApplied = "";
    }

    public class NonTerminalNode : GrammarSymbol
    {
        public List<GrammarSymbol> children = new List<GrammarSymbol>();

        public NonTerminalNode(string name, bool optional = false)
        {
            symbolName = name;
            isTerminal = false;
            isOptional = optional;
        }

        public void AddChild(GrammarSymbol child)
        {
            if (child != null)
            {
                if (isOptional) child.isOptional = true;
                children.Add(child);
            }
        }
    }

    [Serializable]
    public class TerminalAction : GrammarSymbol
    {
        public string actionType; // hablar_npc, saquear_contenedor, leer_nota, fabricar_trampa_*, etc.
        public string argument = ""; // npc_id, mechanism_id, tag, item_id
        public int quantity = 1;
        public bool isCompleted = false;
        public string rewardKeyID = "";
        public string unlockedShortcutId = "";

        // Metadatos espaciales calculados por BindingManager
        public Transform boundTarget;
        public Vector3 boundPosition;
        public string cardinalHint = "";
        public string readableDescription = "";
        public string targetStoryNodeID = "";
        public string bossProfileRequired = "";

        public TerminalAction(string type, string arg = "", int qty = 1, bool optional = false)
        {
            symbolName = type;
            actionType = type;
            argument = arg;
            quantity = qty;
            isTerminal = true;
            isOptional = optional;
        }

        public override string ToString()
        {
            string opt = isOptional ? "[OPCIONAL] " : "";
            string argStr = string.IsNullOrEmpty(argument) ? "" : $" ({argument})";
            return $"{opt}{actionType}{argStr} x{quantity} -> {readableDescription} ({cardinalHint})";
        }
    }

    /// <summary>
    /// Gramática Libre de Contexto Formal (CFG) para la derivación procedural de misiones,
    /// tareas intermedias, sub-arcos anidados y fases de jefes.
    /// G = (VN, Σ, R, S)
    /// </summary>
    public class QuestGrammarParser
    {
        public static readonly HashSet<string> TerminalVocabulary = new HashSet<string>
        {
            "hablar_npc",
            "entregar_recurso",
            "saquear_contenedor",
            "leer_nota",
            "fabricar_trampa_madera",
            "fabricar_trampa_red",
            "fabricar_trampa_puas",
            "agacharse_sigilo",
            "correr_estamina",
            "dash_escape",
            "romper_linea_vision",
            "colarse_ducto",
            "colocar_trampa_madera",
            "colocar_trampa_red",
            "colocar_trampa_puas",
            "activar_interruptor",
            "inyectar_antidoto_vial",
            "espiar_rutina"
        };

        private readonly System.Random rng;
        private readonly int maxRecursionDepth;

        public QuestGrammarParser(int seed = 12345, int maxDepth = 2)
        {
            rng = new System.Random(seed);
            maxRecursionDepth = maxDepth;
        }

        /// <summary>
        /// Deriva la gramática a partir del símbolo inicial y devuelve la lista ordenada de acciones atómicas (Σ).
        /// </summary>
        public static List<TerminalAction> Derivate(string startSymbol, int seed, int maxRecursionDepth = 2)
        {
            QuestGrammarParser parser = new QuestGrammarParser(seed, maxRecursionDepth);
            NonTerminalNode root = parser.DerivateSymbol(startSymbol, 0, false);
            List<TerminalAction> actions = new List<TerminalAction>();
            parser.FlattenActions(root, actions);
            return actions;
        }

        public NonTerminalNode ParseTree(string startSymbol)
        {
            return DerivateSymbol(startSymbol, 0, false);
        }

        private NonTerminalNode DerivateSymbol(string symbol, int currentDepth, bool isOptionalContext)
        {
            NonTerminalNode node = new NonTerminalNode(symbol, isOptionalContext);

            // Parsing con parámetros opcionales tipo OBTENER(recurso) o PREPARAR(trampa_1)
            string baseSymbol = symbol;
            string param = "";
            int parenOpen = symbol.IndexOf('(');
            int parenClose = symbol.IndexOf(')');
            if (parenOpen >= 0 && parenClose > parenOpen)
            {
                baseSymbol = symbol.Substring(0, parenOpen).Trim();
                param = symbol.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();
            }

            switch (baseSymbol.ToUpperInvariant())
            {
                case "ARCO_CORTO":
                    // R: ARCO_CORTO -> INVESTIGAR OBTENER(recurso_basico) PREPARAR(trampa_1)
                    node.ruleApplied = "ARCO_CORTO -> INVESTIGAR OBTENER PREPARAR";
                    node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("OBTENER(recurso_basico)", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("PREPARAR(trampa_madera)", currentDepth + 1, isOptionalContext));
                    break;

                case "ARCO_MEDIO":
                    // R: ARCO_MEDIO -> INVESTIGAR ACCESO OBTENER(recurso_avanzado) NEUTRALIZAR
                    node.ruleApplied = "ARCO_MEDIO -> INVESTIGAR ACCESO OBTENER NEUTRALIZAR";
                    node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("OBTENER(recurso_avanzado)", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("NEUTRALIZAR", currentDepth + 1, isOptionalContext));
                    break;

                case "ARCO_COMPLEJO":
                    // R: ARCO_COMPLEJO -> INVESTIGAR [ ARCO_CORTO_OPC ] ACCESO [ ARCO_MEDIO_OPC ] BOSS_ENCOUNTER
                    node.ruleApplied = "ARCO_COMPLEJO -> INVESTIGAR [ARCO_CORTO_OPC] ACCESO [ARCO_MEDIO_OPC] BOSS_ENCOUNTER";
                    node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                    // Sub-arco opcional corto (atajo o favor secundario)
                    node.AddChild(DerivateSymbol("ARCO_CORTO_OPC", currentDepth + 1, true));
                    node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                    // Sub-arco opcional medio (preparación de trampa avanzada o llave secundaria)
                    node.AddChild(DerivateSymbol("ARCO_MEDIO_OPC", currentDepth + 1, true));
                    node.AddChild(DerivateSymbol("BOSS_ENCOUNTER", currentDepth + 1, isOptionalContext));
                    break;

                case "ARCO_CORTO_OPC":
                    node.ruleApplied = "[ARCO_CORTO_OPC] -> INVESTIGAR OBTENER(atajo)";
                    node.isOptional = true;
                    var actInvest = DerivateSymbol("INVESTIGAR", currentDepth + 1, true);
                    var actObt = DerivateSymbol("OBTENER(suministro_extra)", currentDepth + 1, true);
                    node.AddChild(actInvest);
                    node.AddChild(actObt);
                    break;

                case "ARCO_MEDIO_OPC":
                    node.ruleApplied = "[ARCO_MEDIO_OPC] -> ACCESO OBTENER(llave_atajo_ducto)";
                    node.isOptional = true;
                    var actAcceso = DerivateSymbol("ACCESO", currentDepth + 1, true);
                    var actLlave = new TerminalAction("saquear_contenedor", "llave_ducto_secreto", 1, true)
                    {
                        rewardKeyID = "llave_ducto_secreto",
                        unlockedShortcutId = "ducto_mantenimiento"
                    };
                    node.AddChild(actAcceso);
                    node.AddChild(actLlave);
                    break;

                case "INVESTIGAR":
                    // R: INVESTIGAR -> hablar_npc | leer_nota saquear_contenedor | espiar_rutina agacharse_sigilo
                    int invChoice = rng.Next(3);
                    if (invChoice == 0)
                    {
                        node.ruleApplied = "INVESTIGAR -> hablar_npc";
                        node.AddChild(new TerminalAction("hablar_npc", "informante_calle", 1, isOptionalContext));
                    }
                    else if (invChoice == 1)
                    {
                        node.ruleApplied = "INVESTIGAR -> leer_nota saquear_contenedor";
                        node.AddChild(new TerminalAction("leer_nota", "bitacora_perdida", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("saquear_contenedor", "pista_cardinal", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "INVESTIGAR -> espiar_rutina agacharse_sigilo";
                        node.AddChild(new TerminalAction("espiar_rutina", "vigilancia_patrulla", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("agacharse_sigilo", "camuflaje_sombras", 1, isOptionalContext));
                    }
                    break;

                case "OBTENER":
                    // R: OBTENER(x) -> saquear_contenedor | hablar_npc entregar_recurso | SUBMISION
                    string resourceTag = string.IsNullOrEmpty(param) ? "material_reciclado" : param;
                    int obtChoice = rng.Next(3);
                    if (obtChoice == 0 || currentDepth >= maxRecursionDepth)
                    {
                        node.ruleApplied = "OBTENER -> saquear_contenedor";
                        node.AddChild(new TerminalAction("saquear_contenedor", resourceTag, 1, isOptionalContext));
                    }
                    else if (obtChoice == 1)
                    {
                        node.ruleApplied = "OBTENER -> hablar_npc entregar_recurso";
                        node.AddChild(new TerminalAction("hablar_npc", "testigo_barrio", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("entregar_recurso", resourceTag, 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "OBTENER -> SUBMISION";
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "PREPARAR":
                    // R: PREPARAR(t) -> fabricar_trampa_madera | fabricar_trampa_red | fabricar_trampa_puas
                    if (param.Contains("madera") || param.Contains("1"))
                    {
                        node.ruleApplied = "PREPARAR -> fabricar_trampa_madera";
                        node.AddChild(new TerminalAction("fabricar_trampa_madera", "TrampaMadera", 1, isOptionalContext));
                    }
                    else if (param.Contains("red") || param.Contains("2"))
                    {
                        node.ruleApplied = "PREPARAR -> fabricar_trampa_red";
                        node.AddChild(new TerminalAction("fabricar_trampa_red", "TrampaRed", 1, isOptionalContext));
                    }
                    else if (param.Contains("puas") || param.Contains("3"))
                    {
                        node.ruleApplied = "PREPARAR -> fabricar_trampa_puas";
                        node.AddChild(new TerminalAction("fabricar_trampa_puas", "TrampaPuas", 1, isOptionalContext));
                    }
                    else
                    {
                        int pChoice = rng.Next(3);
                        string tAction = pChoice == 0 ? "fabricar_trampa_madera" : (pChoice == 1 ? "fabricar_trampa_red" : "fabricar_trampa_puas");
                        node.ruleApplied = $"PREPARAR -> {tAction}";
                        node.AddChild(new TerminalAction(tAction, "trampa_tactica", 1, isOptionalContext));
                    }
                    break;

                case "ACCESO":
                    // R: ACCESO -> colarse_ducto agacharse_sigilo | activar_interruptor dash_escape | romper_linea_vision
                    int accChoice = rng.Next(3);
                    if (accChoice == 0)
                    {
                        node.ruleApplied = "ACCESO -> colarse_ducto agacharse_sigilo";
                        node.AddChild(new TerminalAction("colarse_ducto", "rejilla_ventilacion", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("agacharse_sigilo", "paso_estrecho", 1, isOptionalContext));
                    }
                    else if (accChoice == 1)
                    {
                        node.ruleApplied = "ACCESO -> activar_interruptor dash_escape";
                        node.AddChild(new TerminalAction("activar_interruptor", "panel_puerta_seguridad", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("dash_escape", "evasion_inmediata", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ACCESO -> romper_linea_vision";
                        node.AddChild(new TerminalAction("romper_linea_vision", "obstaculo_visual", 1, isOptionalContext));
                    }
                    break;

                case "SUBMISION":
                    // R: SUBMISION -> hablar_npc saquear_contenedor entregar_recurso
                    node.ruleApplied = "SUBMISION -> hablar_npc saquear_contenedor entregar_recurso";
                    node.AddChild(new TerminalAction("hablar_npc", "ciudadano_en_apuros", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("saquear_contenedor", "objeto_perdido", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("entregar_recurso", "devolucion_objeto", 1, isOptionalContext));
                    break;

                case "NEUTRALIZAR":
                    // R: NEUTRALIZAR -> colocar_trampa_madera | colocar_trampa_red romper_linea_vision | colocar_trampa_puas activar_interruptor
                    int nChoice = rng.Next(3);
                    if (nChoice == 0)
                    {
                        node.ruleApplied = "NEUTRALIZAR -> colocar_trampa_madera";
                        node.AddChild(new TerminalAction("colocar_trampa_madera", "barricada_pasillo", 1, isOptionalContext));
                    }
                    else if (nChoice == 1)
                    {
                        node.ruleApplied = "NEUTRALIZAR -> colocar_trampa_red romper_linea_vision";
                        node.AddChild(new TerminalAction("colocar_trampa_red", "red_suelo", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("romper_linea_vision", "refugio_detras_cobertura", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "NEUTRALIZAR -> colocar_trampa_puas activar_interruptor";
                        node.AddChild(new TerminalAction("colocar_trampa_puas", "puas_electrificadas", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "mecanismo_arena", 1, isOptionalContext));
                    }
                    break;

                case "BOSS_ENCOUNTER":
                    // BOSS_ENCOUNTER (Multifase obligatoria de 4 fases)
                    // FASE 1: Infiltración/Aproximación
                    // FASE 2: Preparación de Trampa Específica
                    // FASE 3: Ejecución del Mecanismo
                    // FASE 4: Resolución del Conflicto
                    node.ruleApplied = "BOSS_ENCOUNTER -> FASE1 FASE2 FASE3 FASE4";

                    // Fase 1: Aproximación y sigilo
                    node.AddChild(new TerminalAction("agacharse_sigilo", "infiltracion_perimetro", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("romper_linea_vision", "sombra_arena", 1, isOptionalContext));

                    // Fase 2: Trampa táctica
                    int trapChoice = rng.Next(3);
                    string trapAction = trapChoice == 0 ? "colocar_trampa_madera" : (trapChoice == 1 ? "colocar_trampa_red" : "colocar_trampa_puas");
                    node.AddChild(new TerminalAction(trapAction, "trampa_tactica_jefe", 1, isOptionalContext));

                    // Fase 3: Mecanismo de arena y evasión
                    node.AddChild(new TerminalAction("activar_interruptor", "mecanismo_central_arena", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("dash_escape", "retirada_segura", 1, isOptionalContext));

                    // Fase 4: Resolución y diálogo
                    node.AddChild(new TerminalAction("inyectar_antidoto_vial", "suero_restaurador", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("hablar_npc", "jefe_pacificado", 1, isOptionalContext));
                    break;

                default:
                    if (TerminalVocabulary.Contains(symbol.ToLowerInvariant()))
                    {
                        node.AddChild(new TerminalAction(symbol.ToLowerInvariant(), "", 1, isOptionalContext));
                    }
                    else
                    {
                        // Símbolo no reconocido, derivar como hablar_npc por seguridad
                        node.AddChild(new TerminalAction("hablar_npc", symbol, 1, isOptionalContext));
                    }
                    break;
            }

            return node;
        }

        private void FlattenActions(GrammarSymbol symbol, List<TerminalAction> list)
        {
            if (symbol == null) return;

            if (symbol is TerminalAction terminal)
            {
                list.Add(terminal);
            }
            else if (symbol is NonTerminalNode nonTerminal)
            {
                for (int i = 0; i < nonTerminal.children.Count; i++)
                {
                    FlattenActions(nonTerminal.children[i], list);
                }
            }
        }
    }
}
