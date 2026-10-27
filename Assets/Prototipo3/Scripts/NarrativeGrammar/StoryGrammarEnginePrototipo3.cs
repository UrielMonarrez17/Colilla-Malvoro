using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    public class StoryGrammarEnginePrototipo3 : MonoBehaviour
    {
        [Header("Configuración Procedural")]
        public int storySeed = 2026;
        public bool randomizeSeed = false;

        [Header("Grafo Generado")]
        public StoryGraphPrototipo3 currentGraph;

        private void Awake()
        {
            if (randomizeSeed)
            {
                storySeed = UnityEngine.Random.Range(1000, 99999);
            }
            GenerateStoryGraph(storySeed);
        }

        public StoryGraphPrototipo3 GenerateStoryGraph(int seed)
        {
            var rng = new System.Random(seed);
            var graph = new StoryGraphPrototipo3();

            // Variaciones procedurales de diálogos según semilla
            string[] luluGreetings = new string[]
            {
                "Si te quedas ahí oliendo el veneno, Barnaby, tus pulmones se convertirán en brasas. Las cloacas están hirviendo.",
                "Barnaby... el Aditivo Cero de Malvoro ya no sólo alimenta a las ratas; ha corrompido a las bestias del bulevar.",
                "Guarda esa púa, Barnaby. Los filtros carmesí que caen de la torre no son colillas ordinarias: están vivos y laten."
            };

            string[] baronDemands = new string[]
            {
                "¿Un hámster con armadura de agujas? Ja. Si quieres la tarjeta de la Torre Norte, tráeme la Batería de Litio del generador caído.",
                "El alquitrán me abriga más que tu moral, pequeño roedor. Consígueme energía para mi fogata y te abriré el paso a la Torre.",
                "Malvoro cree que somos basura. Demuéstrame que sabes encender una chispa y la llave de la esclusa será tuya."
            };

            string luluDialogue = luluGreetings[rng.Next(luluGreetings.Length)];
            string baronDialogue = baronDemands[rng.Next(baronDemands.Length)];

            // 1. Start Node: Madriguera del Drenaje
            var nodeStart = new StoryNodePrototipo3
            {
                id = "node_start",
                type = StoryNodeTypePrototipo3.Start,
                title = "1. Despertar en el Drenaje",
                description = "Barnaby amarra su púa de erizo y revisa su percutor de pedernal. Sal de la tubería hacia el bulevar.",
                hint = "Camina con WASD / Dash con [Q] hacia la salida de la alcantarilla.",
                state = StoryNodeStatePrototipo3.Active,
                cinematicId = "Intro_FiltroCarmesi_Prototipo3"
            };

            // 2. Encounter Node: Lulú (NPC)
            var nodeLulu = new StoryNodePrototipo3
            {
                id = "node_lulu",
                type = StoryNodeTypePrototipo3.Encounter,
                title = "2. Contacto con Lulú",
                description = "Habla con la gata siamesa albina apostada sobre la caja de fusibles.",
                hint = "Acércate a Lulú y presiona [F] o [E] para dialogar.",
                targetActorName = "LuluNPC",
                dialogueText = luluDialogue,
                state = StoryNodeStatePrototipo3.Locked
            };

            // 3. Side Quest Opcional: Purga de la Chimenea Menor
            var nodeSideChimney = new StoryNodePrototipo3
            {
                id = "node_side_chimney",
                type = StoryNodeTypePrototipo3.SideQuest,
                title = "[Opcional] Apagar Chimenea de Purga",
                description = "Una chimenea auxiliar de Malvoro está envenenando el callejón. Desactiva la válvula de purga.",
                hint = "Interactúa con la válvula de vapor para obtener munición de fósforo adicional.",
                isOptional = true,
                state = StoryNodeStatePrototipo3.Locked
            };

            // 4. Quest Node: Recolección de Filtros Carmesí
            var nodeLootFilters = new StoryNodePrototipo3
            {
                id = "node_loot_filters",
                type = StoryNodeTypePrototipo3.Quest,
                title = "3. Rastro de Filtros Carmesí",
                description = "Saquea 3 contenedores de filtros palpitantes caídos de los ductos de Malvoro.",
                hint = "Localiza y recolecta 3 cilindros carmesí esparcidos en el asfalto.",
                requiredItemName = "Filtro Carmesí",
                requiredItemCount = 3,
                currentItemProgress = 0,
                state = StoryNodeStatePrototipo3.Locked
            };

            // 5. Mini-Boss Node: Ciervo Feral Alfa
            var nodeMiniBoss = new StoryNodePrototipo3
            {
                id = "node_miniboss",
                type = StoryNodeTypePrototipo3.MiniBoss,
                title = "4. Bestia Feral de la Rotonda",
                description = "Un ciervo infectado por el Aditivo Cero bloquea el cruce este. Usa dash para esquivar su embestida brutal.",
                hint = "Esquiva con [Q] cuando embista y haz que se estrelle contra farolas o coloca trampas.",
                targetActorName = "MiniBossCiervo",
                cinematicId = "Alert_CiervoFeral_Prototipo3",
                state = StoryNodeStatePrototipo3.Locked
            };

            // 6. Encounter Node: Barón de Alquitrán
            var nodeBaron = new StoryNodePrototipo3
            {
                id = "node_baron",
                type = StoryNodeTypePrototipo3.Encounter,
                title = "5. El Señor del Vertedero",
                description = "Negocia con el Barón de Alquitrán en el basurero oriental.",
                hint = "Habla con el jabalí junto a la fogata de desecho.",
                targetActorName = "BaronNPC",
                dialogueText = baronDialogue,
                state = StoryNodeStatePrototipo3.Locked
            };

            // 7. Key Node: Keycard Torre Norte
            var nodeKey = new StoryNodePrototipo3
            {
                id = "node_key",
                type = StoryNodeTypePrototipo3.Key,
                title = "6. Tarjeta de Acceso Malvoro",
                description = "Obtén la Keycard_TorreNorte del Barón para burlar el cerrojo biométrico.",
                hint = "Recoge la tarjeta que el Barón ha dejado sobre el contenedor.",
                requiredItemName = "Keycard_TorreNorte",
                requiredItemCount = 1,
                state = StoryNodeStatePrototipo3.Locked
            };

            // 8. Lock Node: Barricada Electrificada de la Torre
            var nodeLock = new StoryNodePrototipo3
            {
                id = "node_lock",
                type = StoryNodeTypePrototipo3.Lock,
                title = "7. Infiltración a la Torre Malvoro",
                description = "Usa la Keycard_TorreNorte en la terminal de la barricada electrificada.",
                hint = "Inserta la tarjeta en la consola frente a la torre corporativa.",
                requiredItemName = "Keycard_TorreNorte",
                targetActorName = "LockBarricada",
                state = StoryNodeStatePrototipo3.Locked
            };

            // 9. Final Boss: El Reactor Carmesí
            var nodeFinalBoss = new StoryNodePrototipo3
            {
                id = "node_final_boss",
                type = StoryNodeTypePrototipo3.FinalBoss,
                title = "8. Sabotaje del Reactor Carmesí",
                description = "Sobrevive a la traición de Lulú y sabotea los 3 núcleos del reactor de Aditivo Cero.",
                hint = "Acciona los 3 conmutadores de refrigeración antes de que el vapor tóxico te consuma.",
                targetActorName = "BossReactor",
                cinematicId = "Traicion_Lulu_Prototipo3",
                state = StoryNodeStatePrototipo3.Locked
            };

            // 10. Exit Node: Victoria y Amanecer
            var nodeExit = new StoryNodePrototipo3
            {
                id = "node_exit",
                type = StoryNodeTypePrototipo3.Exit,
                title = "9. Huida al Amanecer",
                description = "El reactor colapsa en una nube purificadora. Barnaby contempla el amanecer libre del humo.",
                hint = "Cruza el portal de evacuación hacia la cornisa exterior.",
                cinematicId = "Outro_Victoria_Prototipo3",
                state = StoryNodeStatePrototipo3.Locked
            };

            // Definición de enlaces DAG (Prerrequisitos y Siguientes)
            nodeStart.nextNodeIds.Add(nodeLulu.id);

            nodeLulu.prerequisiteNodeIds.Add(nodeStart.id);
            nodeLulu.nextNodeIds.Add(nodeSideChimney.id);
            nodeLulu.nextNodeIds.Add(nodeLootFilters.id);

            // Misión secundaria bifurcada
            nodeSideChimney.prerequisiteNodeIds.Add(nodeLulu.id);
            nodeSideChimney.nextNodeIds.Add(nodeLootFilters.id);

            nodeLootFilters.prerequisiteNodeIds.Add(nodeLulu.id);
            nodeLootFilters.nextNodeIds.Add(nodeMiniBoss.id);

            nodeMiniBoss.prerequisiteNodeIds.Add(nodeLootFilters.id);
            nodeMiniBoss.nextNodeIds.Add(nodeBaron.id);

            nodeBaron.prerequisiteNodeIds.Add(nodeMiniBoss.id);
            nodeBaron.nextNodeIds.Add(nodeKey.id);

            nodeKey.prerequisiteNodeIds.Add(nodeBaron.id);
            nodeKey.nextNodeIds.Add(nodeLock.id);

            nodeLock.prerequisiteNodeIds.Add(nodeKey.id);
            nodeLock.nextNodeIds.Add(nodeFinalBoss.id);

            nodeFinalBoss.prerequisiteNodeIds.Add(nodeLock.id);
            nodeFinalBoss.nextNodeIds.Add(nodeExit.id);

            nodeExit.prerequisiteNodeIds.Add(nodeFinalBoss.id);

            // Agregar nodos al grafo
            graph.nodes.Add(nodeStart);
            graph.nodes.Add(nodeLulu);
            graph.nodes.Add(nodeSideChimney);
            graph.nodes.Add(nodeLootFilters);
            graph.nodes.Add(nodeMiniBoss);
            graph.nodes.Add(nodeBaron);
            graph.nodes.Add(nodeKey);
            graph.nodes.Add(nodeLock);
            graph.nodes.Add(nodeFinalBoss);
            graph.nodes.Add(nodeExit);

            graph.startNodeId = nodeStart.id;
            graph.exitNodeId = nodeExit.id;

            currentGraph = graph;
            return graph;
        }
    }
}
