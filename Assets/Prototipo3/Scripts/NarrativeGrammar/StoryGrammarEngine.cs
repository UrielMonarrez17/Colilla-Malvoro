using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    /// <summary>
    /// PASO 4: Construye el Grafo Dirigido Acíclico (DAG) No Lineal de la campaña completa de Kilo en Prototipo 3:
    /// Nodo 0 (Apertura) -> [Rama A: Escuela Secundaria Bellavista || Rama B: Bosque Ceniza] -> Convergencia: Hospital San Judas -> Final: Torre Aethelgard.
    /// Soporta configuración dinámica de arcos para habilitar/deshabilitar nodos en tiempo de diseño.
    /// </summary>
    public class StoryGrammarEngine
    {
        /// <summary>
        /// Deriva una secuencia de acciones atómicas directamente desde la gramática formal CFG:
        /// S -> ARCO_CORTO | ARCO_MEDIO | ARCO_COMPLEJO
        /// </summary>
        public static List<TerminalAction> GenerateArcActions(string arcSymbol = "S", int seed = 12345)
        {
            return QuestGrammarParser.Derivate(arcSymbol, seed, maxRecursionDepth: 2);
        }

        public static StoryGraph GenerateStory()
        {
            return GenerateStory(0, true, true, true, false, false);
        }

        public static StoryGraph GenerateStory(int seed)
        {
            return GenerateStory(seed, true, true, true, false, false);
        }

        public static StoryGraph GenerateStory(
            int seed,
            bool includeApertura,
            bool includeEscuela,
            bool includeBosque,
            bool includeHospital,
            bool includeTorre)
        {
            StoryGraph graph = new StoryGraph();

            // =========================================================================
            // NODO 0: APERTURA - EL HUMO DE SOLIS (BOSS 1: MARCO)
            // =========================================================================
            StoryNode node0 = null;
            if (includeApertura)
            {
                node0 = new StoryNode
                {
                    id = "node_0_apertura",
                    type = StoryNodeType.Start,
                    branchTag = "Apertura",
                    landmarkId = "Marco", // Apuntar directamente a Marco en vez de Base (spawn del jugador)
                    title = "El Humo de Solis",
                    description = "Habla con el tejón conserje Marco cerca de tu Base [F], recolecta materiales en contenedores, fabrica una herramienta en la mesa y sobrevive a la primera noche.",
                    hint = "Habla con Marco [F], saquea contenedores [F], fabrica en tu Mesa y sobrevive cuando Marco se vuelva feral de noche.",
                    state = StoryNodeState.Active,
                    isArcEnabled = true,
                    targetActorName = "Marco",
                    requiredItemName = "Objetivos de Apertura",
                    requiredItemCount = 3,
                    currentItemProgress = 0,
                    cutsceneFileName = "01_Intro_Sofia.mp4",
                    dialogueText = "Cof, cof... Hola, pequeño Kilo. Ese humo ámbar de la Torre Aethelgard me provoca jaquecas terribles cuando cae la tarde. Recoge chatarra de los contenedores y prepárate en tu mesa de trabajo antes de que anochezca.",
                    nextNodeIds = new List<string>()
                };

                if (includeEscuela) node0.nextNodeIds.Add("node_a1_escuela");
                if (includeBosque) node0.nextNodeIds.Add("node_b1_bosque");

                graph.nodes.Add(node0);
            }

            // =========================================================================
            // RAMA A: ARCO DE LA ESCUELA SECUNDARIA BELLAVISTA (TOBY/TONY & BOSS 2: MORALES)
            // =========================================================================
            StoryNode nodeA1 = null;
            StoryNode nodeA2 = null;
            StoryNode nodeA3 = null;

            if (includeEscuela)
            {
                nodeA1 = new StoryNode
                {
                    id = "node_a1_escuela",
                    type = StoryNodeType.Encounter,
                    branchTag = "Rama A • Escuela",
                    landmarkId = "Escuela",
                    title = "Rama A1: Localizar la Escuela Bellavista",
                    description = "Usa las pistas cardinales encontradas al saquear contenedores [F] para encontrar la Escuela Secundaria Bellavista (Anillo Cercano: 3 a 5 celdas de la Base).",
                    hint = "Saquea contenedores con [F] para leer rumores cardinales hacia la Escuela Bellavista.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Escuela Bellavista",
                    prerequisiteNodeIds = includeApertura ? new List<string> { "node_0_apertura" } : new List<string>(),
                    nextNodeIds = new List<string> { "node_a2_toby" }
                };

                nodeA2 = new StoryNode
                {
                    id = "node_a2_toby",
                    type = StoryNodeType.Quest,
                    branchTag = "Rama A • Escuela",
                    landmarkId = "Toby",
                    title = "Rama A2: Taller de Toby y Oficina del Rector",
                    description = "Habla con el mapache Toby en su taller de robótica (Edificio Especial) [F] para aprender el uso táctico del Señuelo Acústico (Trampa 1) y espía la oficina del búho Rector H. Morales de día en la Escuela Bellavista.",
                    hint = "Habla con Toby [F] en su taller de robótica (Edificio Especial) y luego inspecciona el escritorio de la Oficina del Rector Morales [F] en la Escuela.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Toby",
                    requiredItemName = "Pasos en la Escuela",
                    requiredItemCount = 2,
                    cutsceneFileName = "03_Toby_Escuela.mp4",
                    dialogueText = "¡Kilo! Bienvenido a mi taller de robótica. Menos mal que viniste. El Rector H. Morales instaló cerraduras magnéticas en la Escuela, pero su oído de búho es hipersensible: usa el Señuelo Acústico [Tecla 1] para distraerlo y no corras con Shift sin agacharte [C].",
                    prerequisiteNodeIds = new List<string> { "node_a1_escuela" },
                    nextNodeIds = new List<string> { "node_a3_apagon" }
                };

                nodeA3 = new StoryNode
                {
                    id = "node_a3_apagon",
                    type = StoryNodeType.Boss,
                    branchTag = "Rama A • Escuela",
                    landmarkId = "Escuela",
                    title = "Momento Crítico A3: El Silenciador en el Apagón",
                    description = "Entra al laboratorio de química por la Llave Maestra del Conserje y el Vial de Enzima Base [F]. Sobrevive al apagón del Rector Morales usando el Señuelo Acústico [1] y sigilo [C].",
                    hint = "Toma el Vial de Enzima Base en el Laboratorio [F] y escapa del perímetro de la Escuela usando [C] y Trampa 1.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Rector H. Morales",
                    requiredItemName = "Vial de Enzima Base",
                    requiredItemCount = 1,
                    cutsceneFileName = "04_Buho_Apagon.mp4",
                    prerequisiteNodeIds = new List<string> { "node_a2_toby" },
                    nextNodeIds = includeHospital ? new List<string> { "node_c1_hospital" } : new List<string>()
                };

                graph.nodes.Add(nodeA1);
                graph.nodes.Add(nodeA2);
                graph.nodes.Add(nodeA3);
            }

            // =========================================================================
            // RAMA B: ARCO DEL BOSQUE CENIZA (MOKA & BOSS 3: SULTÁN)
            // =========================================================================
            StoryNode nodeB1 = null;
            StoryNode nodeB2 = null;
            StoryNode nodeB3 = null;

            if (includeBosque)
            {
                nodeB1 = new StoryNode
                {
                    id = "node_b1_bosque",
                    type = StoryNodeType.Encounter,
                    branchTag = "Rama B • Bosque",
                    landmarkId = "Bosque",
                    title = "Rama B1: Moka en el Bosque Ceniza",
                    description = "Localiza el Bosque Ceniza (Anillo Medio: 6 a 9 celdas de la Base) y habla en las alturas con la zarigüeya Moka [F] para recibir pistas y semillas.",
                    hint = "Usa las pistas cardinales hacia el Bosque Ceniza y habla con Moka [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Moka",
                    dialogueText = "¡Hola, chiquitín! Desde aquí arriba veo a los Cuervos de Hollín y al pobre lobo Sultán. Toma estas semillas de helecho: con ellas puedes fabricar la Bomba de Polen [Trampa 2] para anular el olfato de Sultán cuando anochezca.",
                    prerequisiteNodeIds = includeApertura ? new List<string> { "node_0_apertura" } : new List<string>(),
                    nextNodeIds = new List<string> { "node_b2_postes" }
                };

                nodeB2 = new StoryNode
                {
                    id = "node_b2_postes",
                    type = StoryNodeType.Quest,
                    branchTag = "Rama B • Bosque",
                    landmarkId = "Bosque",
                    title = "Rama B2: Sultán y los 3 Postes Reflectores",
                    description = "Habla de día con el lobo guardabosques Sultán en su cabaña [F] y repara los 3 postes reflectores del arroyo usando Madera, Goma y Metal.",
                    hint = "Habla con Sultán de día [F] y repara los 3 Postes Reflectores del arroyo interactuando con [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Sultán",
                    requiredItemName = "Postes Reflectores Reparados",
                    requiredItemCount = 3,
                    dialogueText = "Kilo... aléjate de mí cuando caiga el sol. El gas me convierte en una bestia al anochecer. Por favor, repara los 3 postes de luz fría junto al arroyo; solo su destello UV puede devolverme la razón.",
                    prerequisiteNodeIds = new List<string> { "node_b1_bosque" },
                    nextNodeIds = new List<string> { "node_b3_sultan" }
                };

                nodeB3 = new StoryNode
                {
                    id = "node_b3_sultan",
                    type = StoryNodeType.Boss,
                    branchTag = "Rama B • Bosque",
                    landmarkId = "Bosque",
                    title = "Momento Crítico B3: Colmillo de Brasa y la Luz Fría",
                    description = "De noche, con los 3 postes reparados, usa la Bomba de Polen [Trampa 2] para burlar el olfato de Sultán feral, atráelo a la subestación y activa el interruptor de los reflectores [F].",
                    hint = "Espera o avanza a la noche, usa Bomba de Polen [2] y pulsa [F] en el Interruptor del Arroyo para pacificar a Sultán.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Sultán",
                    requiredItemName = "Filtro de Carbón Biológico",
                    requiredItemCount = 1,
                    cutsceneFileName = "05_Sultan_LuzFria.mp4",
                    prerequisiteNodeIds = new List<string> { "node_b2_postes" },
                    nextNodeIds = includeHospital ? new List<string> { "node_c1_hospital" } : new List<string>()
                };

                graph.nodes.Add(nodeB1);
                graph.nodes.Add(nodeB2);
                graph.nodes.Add(nodeB3);
            }

            // =========================================================================
            // NODO DE CONVERGENCIA: ARCO DEL HOSPITAL GENERAL SAN JUDAS (BOSS 4: CLARA VALDÉS)
            // =========================================================================
            StoryNode nodeC1 = null;
            StoryNode nodeC2 = null;

            if (includeHospital)
            {
                List<string> hospPrereqs = new List<string>();
                if (includeEscuela && nodeA3 != null) hospPrereqs.Add(nodeA3.id);
                if (includeBosque && nodeB3 != null) hospPrereqs.Add(nodeB3.id);
                if (hospPrereqs.Count == 0 && includeApertura && node0 != null) hospPrereqs.Add(node0.id);

                nodeC1 = new StoryNode
                {
                    id = "node_c1_hospital",
                    type = StoryNodeType.Lock,
                    branchTag = "Convergencia • Hospital",
                    landmarkId = "Hospital",
                    title = "Misión C1: Hospital General San Judas",
                    description = "Con el Vial de Enzima Base y el Filtro de Carbón Biológico reunidos, habla con el pediatra tortuga Dr. Arturo Salgado, visita a Sofía en la habitación 304 y habla de día con la enfermera cisne Clara Valdés.",
                    hint = "Dirígete al Hospital San Judas y habla con el Dr. Salgado, Sofía y Clara Valdés [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Dr. Arturo Salgado",
                    requiredItemName = "Visitas Médicas en el Hospital",
                    requiredItemCount = 3,
                    dialogueText = "¡Trajiste el Vial de Enzima y el Filtro de Carbón, Kilo! Sofía resiste en la habitación 304, pero necesitamos el Cilindro de Oxígeno Puro del quirófano... donde ronda Clara Valdés.",
                    prerequisiteNodeIds = hospPrereqs,
                    nextNodeIds = new List<string> { "node_c2_clara" }
                };

                nodeC2 = new StoryNode
                {
                    id = "node_c2_clara",
                    type = StoryNodeType.Boss,
                    branchTag = "Convergencia • Hospital",
                    landmarkId = "Hospital",
                    title = "Momento Crítico C2: La Dama de Gas y el Quirófano",
                    description = "Evade el gas anestésico de Clara Valdés, usa el Descargador Estático [Trampa 3] en la válvula de la cámara hiperbárica para contenerla sin herirla, recupera el Cilindro de Oxígeno Puro y entrégaselo al Dr. Salgado.",
                    hint = "Usa Trampa 3 [3] o [F] en la Cámara Hiperbárica, recoge el Cilindro de Oxígeno [F] y vuelve con el Dr. Salgado [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Clara Valdés",
                    requiredItemName = "Cilindro de Oxígeno Puro",
                    requiredItemCount = 2,
                    cutsceneFileName = "06_Hospital_Kross.mp4",
                    prerequisiteNodeIds = new List<string> { "node_c1_hospital" },
                    nextNodeIds = includeTorre ? new List<string> { "node_d1_torre" } : new List<string>()
                };

                graph.nodes.Add(nodeC1);
                graph.nodes.Add(nodeC2);
            }

            // =========================================================================
            // NODO FINAL: ARCO DE LA TORRE CORPORATIVA AETHELGARD (BOSS 5: DR. KROSS)
            // =========================================================================
            StoryNode nodeD1 = null;
            StoryNode nodeD2 = null;

            if (includeTorre)
            {
                List<string> torrePrereqs = new List<string>();
                if (includeHospital && nodeC2 != null) torrePrereqs.Add(nodeC2.id);
                else
                {
                    if (includeEscuela && nodeA3 != null) torrePrereqs.Add(nodeA3.id);
                    if (includeBosque && nodeB3 != null) torrePrereqs.Add(nodeB3.id);
                }
                if (torrePrereqs.Count == 0 && includeApertura && node0 != null) torrePrereqs.Add(node0.id);

                nodeD1 = new StoryNode
                {
                    id = "node_d1_torre",
                    type = StoryNodeType.Encounter,
                    branchTag = "Final • Torre Aethelgard",
                    landmarkId = "Torre",
                    title = "Misión D1: Infiltración en la Torre Aethelgard",
                    description = "Prepárate fabricando trampas durante los días necesarios y habla con el zorro ártico Agente Vane en la entrada de la Torre Aethelgard (Anillo Lejano: 10+ celdas).",
                    hint = "Fabrica trampas para el asalto final y habla con el Agente Vane [F] en la entrada de la Torre Aethelgard.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Agente Vane",
                    dialogueText = "Toby, Moka y Sultán están distrayendo a los escuadrones ECLIPSE en el perímetro. Te abrí el elevador de carga, Kilo. Sube al reactor, libera a la Dra. Elena Vargas y detén a Kross.",
                    prerequisiteNodeIds = torrePrereqs,
                    nextNodeIds = new List<string> { "node_d2_kross" }
                };

                nodeD2 = new StoryNode
                {
                    id = "node_d2_kross",
                    type = StoryNodeType.Exit,
                    branchTag = "Final • Torre Aethelgard",
                    landmarkId = "Torre",
                    title = "Momento Crítico Final D2: El Heraldo Solar",
                    description = "Libera a la nutria Dra. Elena Vargas para obtener el Suero Alba-Puro [F], inutiliza los 3 condensadores UV del exoesqueleto del Dr. Julián Kross usando sigilo [C], dash [Espacio] y Trampa 3 [3], e inserta el suero en el Inyector Central [F].",
                    hint = "Libera a la Dra. Elena [F], sobrecarga los 3 Condensadores UV [F / Tecla 3] e inserta el Suero Alba-Puro en el Inyector Central [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Dr. Julián Kross",
                    requiredItemName = "Condensadores UV Inutilizados",
                    requiredItemCount = 3,
                    cutsceneFileName = "07_Final_Amanecer.mp4",
                    prerequisiteNodeIds = new List<string> { "node_d1_torre" },
                    nextNodeIds = new List<string>()
                };

                graph.nodes.Add(nodeD1);
                graph.nodes.Add(nodeD2);
            }

            // Determinar Start y Exit de forma adaptativa
            if (graph.nodes.Count > 0)
            {
                if (includeApertura && node0 != null) graph.startNodeId = node0.id;
                else if (includeEscuela && nodeA1 != null) graph.startNodeId = nodeA1.id;
                else if (includeBosque && nodeB1 != null) graph.startNodeId = nodeB1.id;
                else graph.startNodeId = graph.nodes[0].id;

                if (includeTorre && nodeD2 != null) graph.exitNodeId = nodeD2.id;
                else if (includeHospital && nodeC2 != null) graph.exitNodeId = nodeC2.id;
                else if (includeBosque && nodeB3 != null) graph.exitNodeId = nodeB3.id;
                else if (includeEscuela && nodeA3 != null) graph.exitNodeId = nodeA3.id;
                else if (node0 != null) graph.exitNodeId = node0.id;
                else graph.exitNodeId = graph.nodes[graph.nodes.Count - 1].id;
            }

            if (!graph.IsValidDAG())
            {
                Debug.LogError("[StoryGrammarEngine] Error crítico: El grafo generado no es un DAG válido.");
            }

            return graph;
        }
    }
}
