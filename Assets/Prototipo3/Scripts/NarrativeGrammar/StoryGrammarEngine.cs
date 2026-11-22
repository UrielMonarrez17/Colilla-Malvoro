using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    /// <summary>
    /// PASO 4: Construye el Grafo Dirigido Acíclico (DAG) Lineal Clásico de la campaña completa de Kilo en Prototipo 3:
    /// Cadena estricta de 13 nodos:
    /// Acto 0 (Apertura) -> Acto 1 (Escuela: a1 -> a2 -> a3) -> Acto 2 (Bosque: b1 -> b2 -> b3) ->
    /// Acto 3 (Hospital: c1 -> c2 -> c3) -> Acto 4 (Torre: d1 -> d2 -> d3).
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
            return GenerateStory(0, true, true, true, true, true);
        }

        public static StoryGraph GenerateStory(int seed)
        {
            return GenerateStory(seed, true, true, true, true, true);
        }

        public static StoryGraph GenerateStory(
            int seed = 0,
            bool includeApertura = true,
            bool includeEscuela = true,
            bool includeBosque = true,
            bool includeHospital = true,
            bool includeTorre = true)
        {
            StoryGraph graph = new StoryGraph();

            // Lista secuencial de listas de nodos por acto para encadenamiento lineal estricto
            List<List<StoryNode>> orderedActs = new List<List<StoryNode>>();

            // =========================================================================
            // ACTO 0: APERTURA - EL HUMO DE SOLIS (BOSS 1: MARCO)
            // =========================================================================
            if (includeApertura)
            {
                StoryNode node0 = new StoryNode
                {
                    id = "node_0_apertura",
                    type = StoryNodeType.Start,
                    branchTag = "Acto 0 • Apertura",
                    landmarkId = "Marco",
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
                    prerequisiteNodeIds = new List<string>(),
                    nextNodeIds = new List<string>()
                };

                orderedActs.Add(new List<StoryNode> { node0 });
            }

            // =========================================================================
            // ACTO 1: ARCO DE LA ESCUELA SECUNDARIA BELLAVISTA (TOBY & BOSS 2: MORALES)
            // =========================================================================
            if (includeEscuela)
            {
                StoryNode nodeA1 = new StoryNode
                {
                    id = "node_a1_escuela",
                    type = StoryNodeType.Encounter,
                    branchTag = "Acto 1 • Escuela",
                    landmarkId = "Escuela",
                    title = "Rama A1: Localizar la Escuela Bellavista",
                    description = "Usa las pistas cardinales encontradas al saquear contenedores [F] para encontrar la Escuela Secundaria Bellavista (Anillo Cercano: 3 a 5 celdas de la Base).",
                    hint = "Saquea contenedores con [F] para leer rumores cardinales hacia la Escuela Bellavista.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Escuela Bellavista",
                    prerequisiteNodeIds = new List<string>(),
                    nextNodeIds = new List<string> { "node_a2_toby" }
                };

                StoryNode nodeA2 = new StoryNode
                {
                    id = "node_a2_toby",
                    type = StoryNodeType.Quest,
                    branchTag = "Acto 1 • Escuela",
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

                StoryNode nodeA3 = new StoryNode
                {
                    id = "node_a3_apagon",
                    type = StoryNodeType.Boss,
                    branchTag = "Acto 1 • Escuela",
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
                    nextNodeIds = new List<string>()
                };

                orderedActs.Add(new List<StoryNode> { nodeA1, nodeA2, nodeA3 });
            }

            // =========================================================================
            // ACTO 2: ARCO DEL BOSQUE CENIZA (MOKA & BOSS 3: SULTÁN)
            // =========================================================================
            if (includeBosque)
            {
                StoryNode nodeB1 = new StoryNode
                {
                    id = "node_b1_bosque",
                    type = StoryNodeType.Encounter,
                    branchTag = "Acto 2 • Bosque",
                    landmarkId = "Bosque",
                    title = "Rama B1: Moka en el Bosque Ceniza",
                    description = "Localiza el Bosque Ceniza (Anillo Medio: 6 a 9 celdas de la Base) y habla en las alturas con la zarigüeya Moka [F] para recibir pistas y semillas.",
                    hint = "Usa las pistas cardinales hacia el Bosque Ceniza y habla con Moka [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Moka",
                    dialogueText = "¡Hola, chiquitín! Desde aquí arriba veo a los Cuervos de Hollín y al pobre lobo Sultán. Toma estas semillas de helecho: con ellas puedes fabricar la Bomba de Polen [Trampa 2] para anular el olfato de Sultán cuando anochezca.",
                    prerequisiteNodeIds = new List<string>(),
                    nextNodeIds = new List<string> { "node_b2_postes" }
                };

                StoryNode nodeB2 = new StoryNode
                {
                    id = "node_b2_postes",
                    type = StoryNodeType.Quest,
                    branchTag = "Acto 2 • Bosque",
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

                StoryNode nodeB3 = new StoryNode
                {
                    id = "node_b3_sultan",
                    type = StoryNodeType.Boss,
                    branchTag = "Acto 2 • Bosque",
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
                    nextNodeIds = new List<string>()
                };

                orderedActs.Add(new List<StoryNode> { nodeB1, nodeB2, nodeB3 });
            }

            // =========================================================================
            // ACTO 3: ARCO DEL HOSPITAL GENERAL SAN JUDAS (BOSS 4: CLARA VALDÉS)
            // =========================================================================
            if (includeHospital)
            {
                StoryNode nodeC1 = new StoryNode
                {
                    id = "node_c1_hospital",
                    type = StoryNodeType.Encounter,
                    branchTag = "Acto 3 • Hospital",
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
                    prerequisiteNodeIds = new List<string>(),
                    nextNodeIds = new List<string> { "node_c2_clara" }
                };

                StoryNode nodeC2 = new StoryNode
                {
                    id = "node_c2_clara",
                    type = StoryNodeType.Boss,
                    branchTag = "Acto 3 • Hospital",
                    landmarkId = "Hospital",
                    title = "Momento Crítico C2: La Dama de Gas y el Quirófano",
                    description = "Evade el gas anestésico de Clara Valdés, usa el Descargador Estático [Trampa 3] en la válvula de la cámara hiperbárica para contenerla sin herirla.",
                    hint = "Usa Trampa 3 [3] o [F] en la Cámara Hiperbárica para contener a Clara Valdés.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Clara Valdés",
                    requiredItemName = "Cámara Hiperbárica Activada",
                    requiredItemCount = 1,
                    cutsceneFileName = "06_Hospital_Kross.mp4",
                    prerequisiteNodeIds = new List<string> { "node_c1_hospital" },
                    nextNodeIds = new List<string> { "node_c3_oxigeno" }
                };

                StoryNode nodeC3 = new StoryNode
                {
                    id = "node_c3_oxigeno",
                    type = StoryNodeType.Key,
                    branchTag = "Acto 3 • Hospital",
                    landmarkId = "Hospital",
                    title = "Misión C3: Cilindro de Oxígeno Puro",
                    description = "Recoge el Cilindro de Oxígeno Puro del quirófano y entrégalo al Dr. Arturo Salgado para estabilizar a la pequeña Sofía.",
                    hint = "Recoge el Cilindro de Oxígeno Puro [F] en el quirófano y llévaselo al Dr. Salgado [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Cilindro de Oxígeno Puro",
                    requiredItemName = "Cilindro de Oxígeno Puro",
                    requiredItemCount = 1,
                    dialogueText = "¡Sofía está respirando con normalidad gracias al oxígeno puro! Pero Kross ha tomado el control de los generadores en la Torre Aethelgard...",
                    prerequisiteNodeIds = new List<string> { "node_c2_clara" },
                    nextNodeIds = new List<string>()
                };

                orderedActs.Add(new List<StoryNode> { nodeC1, nodeC2, nodeC3 });
            }

            // =========================================================================
            // ACTO 4: ARCO DE LA TORRE CORPORATIVA AETHELGARD (BOSS 5: DR. KROSS)
            // =========================================================================
            if (includeTorre)
            {
                StoryNode nodeD1 = new StoryNode
                {
                    id = "node_d1_torre",
                    type = StoryNodeType.Encounter,
                    branchTag = "Acto 4 • Torre Aethelgard",
                    landmarkId = "Torre",
                    title = "Misión D1: Infiltración en la Torre Aethelgard",
                    description = "Prepárate fabricando trampas durante los días necesarios, habla con el zorro ártico Agente Vane y libera a la Dra. Elena Vargas en la Torre Aethelgard (Anillo Lejano: 10+ celdas).",
                    hint = "Habla con el Agente Vane [F] y libera a la Dra. Elena [F] en la Torre Aethelgard.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Agente Vane",
                    dialogueText = "Toby, Moka y Sultán están distrayendo a los escuadrones ECLIPSE en el perímetro. Te abrí el elevador de carga, Kilo. Sube al reactor, libera a la Dra. Elena Vargas y detén a Kross.",
                    prerequisiteNodeIds = new List<string>(),
                    nextNodeIds = new List<string> { "node_d2_kross" }
                };

                StoryNode nodeD2 = new StoryNode
                {
                    id = "node_d2_kross",
                    type = StoryNodeType.Boss,
                    branchTag = "Acto 4 • Torre Aethelgard",
                    landmarkId = "Torre",
                    title = "Momento Crítico Final D2: El Heraldo Solar",
                    description = "Libera a la nutria Dra. Elena Vargas para obtener el Suero Alba-Puro [F], inutiliza los 3 condensadores UV del exoesqueleto del Dr. Julián Kross usando sigilo [C], dash [Espacio] y Trampa 3 [3] o [F].",
                    hint = "Sobrecarga los 3 Condensadores UV [F / Tecla 3] para deshabilitar el exoesqueleto del Dr. Kross.",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Dr. Julián Kross",
                    requiredItemName = "Condensadores UV Inutilizados",
                    requiredItemCount = 3,
                    prerequisiteNodeIds = new List<string> { "node_d1_torre" },
                    nextNodeIds = new List<string> { "node_d3_final" }
                };

                StoryNode nodeD3 = new StoryNode
                {
                    id = "node_d3_final",
                    type = StoryNodeType.Exit,
                    branchTag = "Acto 4 • Torre Aethelgard",
                    landmarkId = "Torre",
                    title = "Misión Final D3: El Amanecer de Solis",
                    description = "Inserta el Suero Alba-Puro en el Inyector Central de la Torre Aethelgard para disipar la niebla ámbar y salvar a Solis.",
                    hint = "Inserta el Suero Alba-Puro en el Inyector Central [F].",
                    state = StoryNodeState.Locked,
                    isArcEnabled = true,
                    targetActorName = "Inyector Central",
                    requiredItemName = "Suero Alba-Puro Inyectado",
                    requiredItemCount = 1,
                    cutsceneFileName = "07_Final_Amanecer.mp4",
                    dialogueText = "El Inyector Central dispersa el Suero Alba-Puro por toda la ciudad. La pesadilla ha terminado.",
                    prerequisiteNodeIds = new List<string> { "node_d2_kross" },
                    nextNodeIds = new List<string>()
                };

                orderedActs.Add(new List<StoryNode> { nodeD1, nodeD2, nodeD3 });
            }

            // =========================================================================
            // ENCADENAMIENTO SECUENCIAL ESTRICTO ENTRE ACTOS (LINEAL: Acto i -> Acto i+1)
            // =========================================================================
            for (int actIdx = 0; actIdx < orderedActs.Count; actIdx++)
            {
                List<StoryNode> currentActNodes = orderedActs[actIdx];

                // Agregar los nodos al grafo
                for (int n = 0; n < currentActNodes.Count; n++)
                {
                    graph.nodes.Add(currentActNodes[n]);
                }

                // Si hay un siguiente acto habilitado, encadenar el último nodo del acto actual con el primero del siguiente
                if (actIdx + 1 < orderedActs.Count)
                {
                    StoryNode lastOfCurrent = currentActNodes[currentActNodes.Count - 1];
                    StoryNode firstOfNext = orderedActs[actIdx + 1][0];

                    if (!lastOfCurrent.nextNodeIds.Contains(firstOfNext.id))
                    {
                        lastOfCurrent.nextNodeIds.Add(firstOfNext.id);
                    }
                    if (!firstOfNext.prerequisiteNodeIds.Contains(lastOfCurrent.id))
                    {
                        firstOfNext.prerequisiteNodeIds.Add(lastOfCurrent.id);
                    }
                }
            }

            // Determinar Start y Exit de forma adaptativa
            if (graph.nodes.Count > 0)
            {
                graph.startNodeId = graph.nodes[0].id;
                graph.exitNodeId = graph.nodes[graph.nodes.Count - 1].id;
            }

            if (!graph.IsValidDAG())
            {
                Debug.LogError("[StoryGrammarEngine] Error crítico: El grafo generado no es un DAG válido.");
            }

            return graph;
        }
    }
}
