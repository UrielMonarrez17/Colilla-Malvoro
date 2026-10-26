using System.Collections.Generic;
using UnityEngine;

namespace Prototipo2.Narrative
{
    public class StoryGrammarEngine
    {
        // Variantes de diálogo contextual de Lulú según la semilla procedural
        private static readonly string[] LuluEncounterDialogues =
        {
            "¡Psst! Hámster, qué alivio ver a alguien cuerdo. La niebla nocturna enloqueció a casi todos en la ciudad... Reúne suministros en los callejones y fabrica una trampa antes de cruzar al norte.",
            "¡Oye, pequeñín! Por aquí. Soy Lulú, conozco los atajos de estos bloques. Los edificios están llenos de chatarra en sus fachadas; junta 2 lotes y usa tu mesa de trabajo para abrir la barricada.",
            "¡Cuidado con las avenidas abiertas! Incluso los ciervos se han vuelto ferales al caer la noche. Busca recursos en las esquinas, arma una herramienta en tu refugio y no dejes que te acorralen."
        };

        // Pool de Animales Ferales Alfa para el Clímax (Universal feral lore)
        private static readonly (string title, string species, string description)[] AlphaBosses =
        {
            ("Tigre Alfa Feral", "Félido Gigante Infectado", "Un depredador masivo que patrulla la rotonda central cegado por la rabia nocturna."),
            ("Ciervo Cornamenta Sangrienta", "Herbívoro Feral Alfa", "Un ciervo enloquecido cuyas embestidas destrozan cualquier obstáculo a su paso."),
            ("Dóberman Feral Alfa", "Cánido Alfa Territorial", "El líder de la jauría que bloquea el acceso a la madriguera de evacuación.")
        };

        public static StoryGraph GenerateStory(int seed = 12345)
        {
            Random.State prevState = Random.state;
            Random.InitState(seed);

            StoryGraph graph = new StoryGraph();

            // Selección temática según semilla manteniendo siempre a Lulú como NPC aliada protagonista
            string luluBaseDialogue = LuluEncounterDialogues[Random.Range(0, LuluEncounterDialogues.Length)];
            var alphaBoss = AlphaBosses[Random.Range(0, AlphaBosses.Length)];
            string contextualDialogue = $"{luluBaseDialogue} Y ten mucho cuidado más adelante con el {alphaBoss.title}.";

            // Regla 1: [Story] -> Start -> [Exploration] -> [Conflict] -> [Climax] -> Exit

            // 1. Nodo Start (Refugio)
            StoryNode startNode = new StoryNode
            {
                id = "node_start",
                type = StoryNodeType.Start,
                title = "Despertar en la Ciudad Hostil",
                description = "Has llegado a las inmediaciones de la ciudad tras el tutorial. Establece tu base de operaciones en el refugio inicial.",
                hint = "Camina hacia la calle cercana para localizar a Lulú.",
                state = StoryNodeState.Active,
                nextNodeIds = new List<string> { "node_encounter" }
            };

            // 2. Nodo Encounter [Exploration] - Protagonizado por Lulú NPC
            StoryNode encounterNode = new StoryNode
            {
                id = "node_encounter",
                type = StoryNodeType.Encounter,
                title = "Contacto con Lulú",
                description = "Localiza a Lulú (Gata Callejera Cuerda), una aliada superviviente que patrulla las aceras cercanas al refugio.",
                hint = "Acércate a Lulú y presiona [F] para hablar con ella.",
                state = StoryNodeState.Locked,
                targetActorName = "Lulú",
                dialogueText = contextualDialogue,
                prerequisiteNodeIds = new List<string> { "node_start" },
                nextNodeIds = new List<string> { "node_quest" }
            };

            // 3. Nodo Quest [Exploration]
            StoryNode questNode = new StoryNode
            {
                id = "node_quest",
                type = StoryNodeType.Quest,
                title = "Recuperar Suministros Vitales",
                description = "Busca contenedores de botín en los sockets de los edificios para obtener restos de madera, metal y desperdicios.",
                hint = "Saquea al menos 2 contenedores de loot en las esquinas de los edificios con [F].",
                state = StoryNodeState.Locked,
                requiredItemName = "Desperdicios / Chatarra",
                requiredItemCount = 2,
                prerequisiteNodeIds = new List<string> { "node_encounter" },
                nextNodeIds = new List<string> { "node_key" }
            };

            // 4. Nodo Key [Conflict] (Garantizado ANTES de Lock)
            StoryNode keyNode = new StoryNode
            {
                id = "node_key",
                type = StoryNodeType.Key,
                title = "Fabricar Herramienta de Apertura",
                description = "Utiliza la mesa de crafteo para preparar el mecanismo que abrirá el pasaje de seguridad bloqueado.",
                hint = "Dirígete a la mesa de crafteo o encuentra la llave en el socket marcado.",
                state = StoryNodeState.Locked,
                requiredItemName = "Llave de Paso / Trampa Táctica",
                prerequisiteNodeIds = new List<string> { "node_quest" },
                nextNodeIds = new List<string> { "node_lock" }
            };

            // 5. Nodo Lock [Conflict]
            StoryNode lockNode = new StoryNode
            {
                id = "node_lock",
                type = StoryNodeType.Lock,
                title = "Desbloquear el Callejón Central",
                description = "El acceso a la zona norte está cerrado por una barrera de escombros y trampas abandonadas. Usa la llave o herramienta para despejar la vía.",
                hint = "Acércate a la barricada y desbloquea el pasaje con [F].",
                state = StoryNodeState.Locked,
                prerequisiteNodeIds = new List<string> { "node_key" },
                nextNodeIds = new List<string> { "node_boss" }
            };

            // 6. Nodo Boss [Climax]
            StoryNode bossNode = new StoryNode
            {
                id = "node_boss",
                type = StoryNodeType.Boss,
                title = $"Confrontación: {alphaBoss.title}",
                description = $"{alphaBoss.description} Supera su territorio en la gran rotonda usando sigilo, trampas o distracción.",
                hint = "Elude al enemigo alfa usando agacharse (C) o distráelo para alcanzar la madriguera.",
                state = StoryNodeState.Locked,
                targetActorName = alphaBoss.title,
                prerequisiteNodeIds = new List<string> { "node_lock" },
                nextNodeIds = new List<string> { "node_exit" }
            };

            // 7. Nodo Exit [Victoria]
            StoryNode exitNode = new StoryNode
            {
                id = "node_exit",
                type = StoryNodeType.Exit,
                title = "Extracción en la Madriguera Principal",
                description = "Has sorteado la amenaza feral. Entra a la madriguera subterránea para consolidar la colonia.",
                hint = "Alcanza el punto de extracción en el extremo de la ciudad.",
                state = StoryNodeState.Locked,
                prerequisiteNodeIds = new List<string> { "node_boss" }
            };

            graph.nodes.Add(startNode);
            graph.nodes.Add(encounterNode);
            graph.nodes.Add(questNode);
            graph.nodes.Add(keyNode);
            graph.nodes.Add(lockNode);
            graph.nodes.Add(bossNode);
            graph.nodes.Add(exitNode);

            graph.startNodeId = startNode.id;
            graph.exitNodeId = exitNode.id;

            Random.state = prevState;

            if (!graph.IsValidDAG())
            {
                Debug.LogError("[StoryGrammarEngine] Error crítico: El grafo generado no es un DAG válido.");
            }

            return graph;
        }
    }
}
