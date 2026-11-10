using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    // =========================================================================
    // ESTRUCTURAS DEL ÁRBOL SINTÁCTICO DE LA GRAMÁTICA FORMAL G = (VN, Σ, R, S)
    // =========================================================================

    [Serializable]
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
                case "S":
                    // S -> ARCO_CORTO | ARCO_MEDIO | ARCO_COMPLEJO
                    int sChoice = rng.Next(3);
                    if (sChoice == 0)
                    {
                        node.ruleApplied = "S -> ARCO_CORTO";
                        node.AddChild(DerivateSymbol("ARCO_CORTO", currentDepth + 1, isOptionalContext));
                    }
                    else if (sChoice == 1)
                    {
                        node.ruleApplied = "S -> ARCO_MEDIO";
                        node.AddChild(DerivateSymbol("ARCO_MEDIO", currentDepth + 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "S -> ARCO_COMPLEJO";
                        node.AddChild(DerivateSymbol("ARCO_COMPLEJO", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "ARCO_CORTO":
                    // ARCO_CORTO -> ACCESO + NEUTRALIZAR | ACCESO + SUBMISION + NEUTRALIZAR
                    int acChoice = rng.Next(2);
                    if (acChoice == 0)
                    {
                        node.ruleApplied = "ARCO_CORTO -> ACCESO + NEUTRALIZAR";
                        node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("NEUTRALIZAR", currentDepth + 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ARCO_CORTO -> ACCESO + SUBMISION + NEUTRALIZAR";
                        node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("NEUTRALIZAR", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "ARCO_MEDIO":
                    // ARCO_MEDIO -> INVESTIGAR + [ ARCO_CORTO_OPC ] + PREPARAR + ACCESO + NEUTRALIZAR 
                    //             | INVESTIGAR + SUBMISION + PREPARAR + ACCESO + NEUTRALIZAR
                    int amChoice = rng.Next(2);
                    if (amChoice == 0)
                    {
                        node.ruleApplied = "ARCO_MEDIO -> INVESTIGAR + [ARCO_CORTO_OPC] + PREPARAR + ACCESO + NEUTRALIZAR";
                        node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                        if (rng.Next(100) < 60)
                        {
                            node.AddChild(DerivateSymbol("ARCO_CORTO_OPC", currentDepth + 1, true));
                        }
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("NEUTRALIZAR", currentDepth + 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ARCO_MEDIO -> INVESTIGAR + SUBMISION + PREPARAR + ACCESO + NEUTRALIZAR";
                        node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("NEUTRALIZAR", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "ARCO_COMPLEJO":
                    // ARCO_COMPLEJO -> INVESTIGAR + [ ARCO_MEDIO_OPC ] + PREPARAR + ACCESO + [ ARCO_CORTO_OPC ] + NEUTRALIZAR
                    node.ruleApplied = "ARCO_COMPLEJO -> INVESTIGAR + [ARCO_MEDIO_OPC] + PREPARAR + ACCESO + [ARCO_CORTO_OPC] + NEUTRALIZAR";
                    node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                    if (rng.Next(100) < 60)
                    {
                        node.AddChild(DerivateSymbol("ARCO_MEDIO_OPC", currentDepth + 1, true));
                    }
                    node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                    if (rng.Next(100) < 60)
                    {
                        node.AddChild(DerivateSymbol("ARCO_CORTO_OPC", currentDepth + 1, true));
                    }
                    node.AddChild(DerivateSymbol("NEUTRALIZAR", currentDepth + 1, isOptionalContext));
                    break;

                case "ARCO_MEDIO_OPC":
                    // Sub-arcos opcionales anidados: ARCO_MEDIO_OPC -> SUBMISION + OBTENER(llave_atajo)
                    node.ruleApplied = "ARCO_MEDIO_OPC -> SUBMISION + OBTENER(llave_atajo)";
                    node.isOptional = true;
                    node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, true));
                    node.AddChild(DerivateSymbol("OBTENER(llave_atajo)", currentDepth + 1, true));
                    break;

                case "ARCO_CORTO_OPC":
                    // Sub-arcos opcionales anidados: ARCO_CORTO_OPC -> saquear_contenedor(almacen_oculto) + fabricar_trampa_madera()
                    node.ruleApplied = "ARCO_CORTO_OPC -> saquear_contenedor(almacen_oculto) + fabricar_trampa_madera()";
                    node.isOptional = true;
                    node.AddChild(new TerminalAction("saquear_contenedor", "almacen_oculto", 1, true));
                    node.AddChild(new TerminalAction("fabricar_trampa_madera", "", 1, true));
                    break;

                case "INVESTIGAR":
                    // INVESTIGAR -> hablar_npc(testigo)
                    // INVESTIGAR -> saquear_contenedor(archivo) + leer_nota(pista)
                    // INVESTIGAR -> hablar_npc(informante) + SUBMISION
                    int invChoice = rng.Next(3);
                    if (invChoice == 0)
                    {
                        node.ruleApplied = "INVESTIGAR -> hablar_npc(testigo)";
                        node.AddChild(new TerminalAction("hablar_npc", "testigo", 1, isOptionalContext));
                    }
                    else if (invChoice == 1)
                    {
                        node.ruleApplied = "INVESTIGAR -> saquear_contenedor(archivo) + leer_nota(pista)";
                        node.AddChild(new TerminalAction("saquear_contenedor", "archivo", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("leer_nota", "pista", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "INVESTIGAR -> hablar_npc(informante) + SUBMISION";
                        node.AddChild(new TerminalAction("hablar_npc", "informante", 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "OBTENER":
                    // OBTENER(llave) -> saquear_contenedor(caja_fuerte)
                    // OBTENER(llave) -> hablar_npc(propietario) + SUBMISION
                    // OBTENER(llave) -> agacharse_sigilo() + colarse_ducto(oficina)
                    int obtChoice = rng.Next(3);
                    if (param == "material_raro" || param == "objeto_perdido")
                    {
                        node.ruleApplied = $"OBTENER -> saquear_contenedor({param})";
                        node.AddChild(new TerminalAction("saquear_contenedor", param, 1, isOptionalContext));
                    }
                    else if (obtChoice == 0)
                    {
                        string safeTag = string.IsNullOrEmpty(param) ? "caja_fuerte" : param;
                        node.ruleApplied = $"OBTENER -> saquear_contenedor({safeTag})";
                        node.AddChild(new TerminalAction("saquear_contenedor", safeTag, 1, isOptionalContext));
                    }
                    else if (obtChoice == 1)
                    {
                        node.ruleApplied = "OBTENER -> hablar_npc(propietario) + SUBMISION";
                        node.AddChild(new TerminalAction("hablar_npc", "propietario", 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "OBTENER -> agacharse_sigilo() + colarse_ducto(oficina)";
                        node.AddChild(new TerminalAction("agacharse_sigilo", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colarse_ducto", "oficina", 1, isOptionalContext));
                    }
                    break;

                case "PREPARAR":
                    // PREPARAR -> saquear_contenedor(madera) + fabricar_trampa_madera()
                    // PREPARAR -> saquear_contenedor(goma) + saquear_contenedor(desperdicios) + fabricar_trampa_red()
                    // PREPARAR -> saquear_contenedor(metal) + saquear_contenedor(madera) + fabricar_trampa_puas()
                    // PREPARAR -> OBTENER(material_raro) + fabricar_trampa_madera()
                    int prepChoice = rng.Next(4);
                    if (prepChoice == 0)
                    {
                        node.ruleApplied = "PREPARAR -> saquear_contenedor(madera) + fabricar_trampa_madera()";
                        node.AddChild(new TerminalAction("saquear_contenedor", "madera", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("fabricar_trampa_madera", "", 1, isOptionalContext));
                    }
                    else if (prepChoice == 1)
                    {
                        node.ruleApplied = "PREPARAR -> saquear_contenedor(goma) + saquear_contenedor(desperdicios) + fabricar_trampa_red()";
                        node.AddChild(new TerminalAction("saquear_contenedor", "goma", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("saquear_contenedor", "desperdicios", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("fabricar_trampa_red", "", 1, isOptionalContext));
                    }
                    else if (prepChoice == 2)
                    {
                        node.ruleApplied = "PREPARAR -> saquear_contenedor(metal) + saquear_contenedor(madera) + fabricar_trampa_puas()";
                        node.AddChild(new TerminalAction("saquear_contenedor", "metal", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("saquear_contenedor", "madera", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("fabricar_trampa_puas", "", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "PREPARAR -> OBTENER(material_raro) + fabricar_trampa_madera()";
                        node.AddChild(DerivateSymbol("OBTENER(material_raro)", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("fabricar_trampa_madera", "", 1, isOptionalContext));
                    }
                    break;

                case "ACCESO":
                    // ACCESO -> colarse_ducto(rejilla) + dash_escape()
                    // ACCESO -> OBTENER(llave) + activar_interruptor(puerta)
                    // ACCESO -> PREPARAR + colocar_trampa_madera() + activar_interruptor(consola)
                    // ACCESO -> PREPARAR + colocar_trampa_red() + colarse_ducto(brecha)
                    int accChoice = rng.Next(4);
                    if (accChoice == 0)
                    {
                        node.ruleApplied = "ACCESO -> colarse_ducto(rejilla) + dash_escape()";
                        node.AddChild(new TerminalAction("colarse_ducto", "rejilla", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                    }
                    else if (accChoice == 1)
                    {
                        node.ruleApplied = "ACCESO -> OBTENER(llave) + activar_interruptor(puerta)";
                        node.AddChild(DerivateSymbol("OBTENER(llave)", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "puerta", 1, isOptionalContext));
                    }
                    else if (accChoice == 2)
                    {
                        node.ruleApplied = "ACCESO -> PREPARAR + colocar_trampa_madera() + activar_interruptor(consola)";
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_madera", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "consola", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ACCESO -> PREPARAR + colocar_trampa_red() + colarse_ducto(brecha)";
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_red", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colarse_ducto", "brecha", 1, isOptionalContext));
                    }
                    break;

                case "SUBMISION":
                    // Submisiones recursivas (Permite anidar favores sin fin)
                    // SUBMISION -> saquear_contenedor(recurso) + entregar_recurso(recurso, 2)
                    // SUBMISION -> INVESTIGAR + OBTENER(objeto_perdido) + hablar_npc(solicitante)
                    // SUBMISION -> ACCESO + saquear_contenedor(medicina)
                    // SUBMISION -> SUBMISION + SUBMISION
                    int subMax = currentDepth < maxRecursionDepth ? 4 : 3;
                    int subChoice = rng.Next(subMax);
                    if (subChoice == 0)
                    {
                        node.ruleApplied = "SUBMISION -> saquear_contenedor(recurso) + entregar_recurso(recurso, 2)";
                        node.AddChild(new TerminalAction("saquear_contenedor", "recurso", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("entregar_recurso", "recurso", 2, isOptionalContext));
                    }
                    else if (subChoice == 1)
                    {
                        node.ruleApplied = "SUBMISION -> INVESTIGAR + OBTENER(objeto_perdido) + hablar_npc(solicitante)";
                        node.AddChild(DerivateSymbol("INVESTIGAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("OBTENER(objeto_perdido)", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("hablar_npc", "solicitante", 1, isOptionalContext));
                    }
                    else if (subChoice == 2)
                    {
                        node.ruleApplied = "SUBMISION -> ACCESO + saquear_contenedor(medicina)";
                        node.AddChild(DerivateSymbol("ACCESO", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("saquear_contenedor", "medicina", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "SUBMISION -> SUBMISION + SUBMISION";
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("SUBMISION", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "NEUTRALIZAR":
                    // NEUTRALIZAR -> ENFRENTAMIENTO_MENOR | BOSS_ENCOUNTER
                    int neuChoice = rng.Next(2);
                    if (neuChoice == 0)
                    {
                        node.ruleApplied = "NEUTRALIZAR -> ENFRENTAMIENTO_MENOR";
                        node.AddChild(DerivateSymbol("ENFRENTAMIENTO_MENOR", currentDepth + 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "NEUTRALIZAR -> BOSS_ENCOUNTER";
                        node.AddChild(DerivateSymbol("BOSS_ENCOUNTER", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "ENFRENTAMIENTO_MENOR":
                    // ENFRENTAMIENTO_MENOR -> PREPARAR + colocar_trampa_red() + dash_escape()
                    //                      | PREPARAR + colocar_trampa_puas() + activar_interruptor(luz_fria)
                    //                      | PREPARAR + colocar_trampa_madera() + agacharse_sigilo()
                    int enfChoice = rng.Next(3);
                    if (enfChoice == 0)
                    {
                        node.ruleApplied = "ENFRENTAMIENTO_MENOR -> PREPARAR + colocar_trampa_red() + dash_escape()";
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_red", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                    }
                    else if (enfChoice == 1)
                    {
                        node.ruleApplied = "ENFRENTAMIENTO_MENOR -> PREPARAR + colocar_trampa_puas() + activar_interruptor(luz_fria)";
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_puas", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "luz_fria", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ENFRENTAMIENTO_MENOR -> PREPARAR + colocar_trampa_madera() + agacharse_sigilo()";
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_madera", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("agacharse_sigilo", "", 1, isOptionalContext));
                    }
                    break;

                case "BOSS_ENCOUNTER":
                    // BOSS_ENCOUNTER -> FASE_INICIO_BOSS + FASE_VULNERABILIDAD + FASE_FURIA_BOSS + CLIMAX_PACIFICACION
                    node.ruleApplied = "BOSS_ENCOUNTER -> FASE_INICIO_BOSS + FASE_VULNERABILIDAD + FASE_FURIA_BOSS + CLIMAX_PACIFICACION";
                    node.AddChild(DerivateSymbol("FASE_INICIO_BOSS", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("FASE_VULNERABILIDAD", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("FASE_FURIA_BOSS", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("CLIMAX_PACIFICACION", currentDepth + 1, isOptionalContext));
                    break;

                case "FASE_INICIO_BOSS":
                    // FASE_INICIO_BOSS -> agacharse_sigilo() + romper_linea_vision() + dash_escape()
                    //                   | espiar_rutina() + colarse_ducto(cobertura) + PREPARAR
                    int fibChoice = rng.Next(2);
                    if (fibChoice == 0)
                    {
                        node.ruleApplied = "FASE_INICIO_BOSS -> agacharse_sigilo() + romper_linea_vision() + dash_escape()";
                        node.AddChild(new TerminalAction("agacharse_sigilo", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("romper_linea_vision", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "FASE_INICIO_BOSS -> espiar_rutina() + colarse_ducto(cobertura) + PREPARAR";
                        node.AddChild(new TerminalAction("espiar_rutina", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colarse_ducto", "cobertura", 1, isOptionalContext));
                        node.AddChild(DerivateSymbol("PREPARAR", currentDepth + 1, isOptionalContext));
                    }
                    break;

                case "FASE_VULNERABILIDAD":
                    // FASE_VULNERABILIDAD -> ATRAER_A_TRAMPA + ACTIVAR_VULNERABILIDAD_ENTORNO
                    node.ruleApplied = "FASE_VULNERABILIDAD -> ATRAER_A_TRAMPA + ACTIVAR_VULNERABILIDAD_ENTORNO";
                    node.AddChild(DerivateSymbol("ATRAER_A_TRAMPA", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("ACTIVAR_VULNERABILIDAD_ENTORNO", currentDepth + 1, isOptionalContext));
                    break;

                case "ATRAER_A_TRAMPA":
                    // ATRAER_A_TRAMPA -> colocar_trampa_madera() + dash_escape()
                    //                  | colocar_trampa_red() + agacharse_sigilo()
                    //                  | colocar_trampa_puas()
                    int aatChoice = rng.Next(3);
                    if (aatChoice == 0)
                    {
                        node.ruleApplied = "ATRAER_A_TRAMPA -> colocar_trampa_madera() + dash_escape()";
                        node.AddChild(new TerminalAction("colocar_trampa_madera", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                    }
                    else if (aatChoice == 1)
                    {
                        node.ruleApplied = "ATRAER_A_TRAMPA -> colocar_trampa_red() + agacharse_sigilo()";
                        node.AddChild(new TerminalAction("colocar_trampa_red", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("agacharse_sigilo", "", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ATRAER_A_TRAMPA -> colocar_trampa_puas()";
                        node.AddChild(new TerminalAction("colocar_trampa_puas", "", 1, isOptionalContext));
                    }
                    break;

                case "ACTIVAR_VULNERABILIDAD_ENTORNO":
                    // ACTIVAR_VULNERABILIDAD_ENTORNO -> colarse_ducto(generador) + activar_interruptor(valvula_o_luz)
                    //                                | dash_escape() + activar_interruptor(panel_sobrecarga)
                    int aveChoice = rng.Next(2);
                    if (aveChoice == 0)
                    {
                        node.ruleApplied = "ACTIVAR_VULNERABILIDAD_ENTORNO -> colarse_ducto(generador) + activar_interruptor(valvula_o_luz)";
                        node.AddChild(new TerminalAction("colarse_ducto", "generador", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "valvula_o_luz", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "ACTIVAR_VULNERABILIDAD_ENTORNO -> dash_escape() + activar_interruptor(panel_sobrecarga)";
                        node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "panel_sobrecarga", 1, isOptionalContext));
                    }
                    break;

                case "FASE_FURIA_BOSS":
                    // FASE_FURIA_BOSS -> EVASION_FURIA + SOBRECARGA_TRAMPA_DUAL
                    node.ruleApplied = "FASE_FURIA_BOSS -> EVASION_FURIA + SOBRECARGA_TRAMPA_DUAL";
                    node.AddChild(DerivateSymbol("EVASION_FURIA", currentDepth + 1, isOptionalContext));
                    node.AddChild(DerivateSymbol("SOBRECARGA_TRAMPA_DUAL", currentDepth + 1, isOptionalContext));
                    break;

                case "EVASION_FURIA":
                    // EVASION_FURIA -> romper_linea_vision() + dash_escape() + agacharse_sigilo()
                    node.ruleApplied = "EVASION_FURIA -> romper_linea_vision() + dash_escape() + agacharse_sigilo()";
                    node.AddChild(new TerminalAction("romper_linea_vision", "", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                    node.AddChild(new TerminalAction("agacharse_sigilo", "", 1, isOptionalContext));
                    break;

                case "SOBRECARGA_TRAMPA_DUAL":
                    // SOBRECARGA_TRAMPA_DUAL -> colocar_trampa_madera() + colocar_trampa_red()
                    //                         | colocar_trampa_red() + colocar_trampa_puas()
                    int stdChoice = rng.Next(2);
                    if (stdChoice == 0)
                    {
                        node.ruleApplied = "SOBRECARGA_TRAMPA_DUAL -> colocar_trampa_madera() + colocar_trampa_red()";
                        node.AddChild(new TerminalAction("colocar_trampa_madera", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_red", "", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "SOBRECARGA_TRAMPA_DUAL -> colocar_trampa_red() + colocar_trampa_puas()";
                        node.AddChild(new TerminalAction("colocar_trampa_red", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("colocar_trampa_puas", "", 1, isOptionalContext));
                    }
                    break;

                case "CLIMAX_PACIFICACION":
                    // CLIMAX_PACIFICACION -> dash_escape() + activar_interruptor(luz_fria_definitiva)
                    //                      | colarse_ducto(control_maestro) + activar_interruptor(camara_aislamiento)
                    //                      | agacharse_sigilo() + inyectar_antidoto_vial()
                    int cpChoice = rng.Next(3);
                    if (cpChoice == 0)
                    {
                        node.ruleApplied = "CLIMAX_PACIFICACION -> dash_escape() + activar_interruptor(luz_fria_definitiva)";
                        node.AddChild(new TerminalAction("dash_escape", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "luz_fria_definitiva", 1, isOptionalContext));
                    }
                    else if (cpChoice == 1)
                    {
                        node.ruleApplied = "CLIMAX_PACIFICACION -> colarse_ducto(control_maestro) + activar_interruptor(camara_aislamiento)";
                        node.AddChild(new TerminalAction("colarse_ducto", "control_maestro", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("activar_interruptor", "camara_aislamiento", 1, isOptionalContext));
                    }
                    else
                    {
                        node.ruleApplied = "CLIMAX_PACIFICACION -> agacharse_sigilo() + inyectar_antidoto_vial()";
                        node.AddChild(new TerminalAction("agacharse_sigilo", "", 1, isOptionalContext));
                        node.AddChild(new TerminalAction("inyectar_antidoto_vial", "suero", 1, isOptionalContext));
                    }
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
