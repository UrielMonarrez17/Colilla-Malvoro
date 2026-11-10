using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    /// <summary>
    /// Componente de Bloqueo Argumental (Plot Gate) que previene el "sequence breaking"
    /// al toparse prematuramente con jefes, recintos o mecanismos clave de la historia.
    /// </summary>
    [SelectionBase]
    public class PlotGate : MonoBehaviour
    {
        [Header("Condición Argumental")]
        [Tooltip("ID del nodo de historia en StoryDirector requerido para abrir esta compuerta/mecanismo.")]
        public string targetStoryNodeID = "node_0_apertura";

        [Tooltip("Sub-nodos o IDs alternativos que también permiten la interacción.")]
        public List<string> allowedStoryNodeIDs = new List<string>();

        [Header("Respuestas ante Visitas Prematuras")]
        [TextArea(2, 4)]
        [Tooltip("Diálogo mundano diurno cuando el jugador interactúa prematuramente (sin spoilers).")]
        public string mundaneDayDialogue = "Hace buen día en el barrio, pequeño, pero no salgas cuando caiga la noche: las calles se vuelven peligrosas.";

        [TextArea(2, 4)]
        [Tooltip("Mensaje mostrado en pantalla cuando se intenta activar un mecanismo antes de tiempo.")]
        public string lockedMechanismMessage = "El mecanismo no responde o carece del catalizador adecuado";

        /// <summary>
        /// Comprueba si el nodo de historia requerido está activo en StoryDirector o StoryManagerPrototipo3.
        /// </summary>
        public bool IsGateOpen()
        {
            // 1. Consultar primero a StoryDirector (DAG de la campaña)
            if (StoryDirector.Instance != null)
            {
                if (StoryDirector.Instance.IsNodeActive(targetStoryNodeID) || StoryDirector.Instance.IsNodeCompleted(targetStoryNodeID))
                {
                    return true;
                }

                if (allowedStoryNodeIDs != null && allowedStoryNodeIDs.Count > 0)
                {
                    for (int i = 0; i < allowedStoryNodeIDs.Count; i++)
                    {
                        if (StoryDirector.Instance.IsNodeActive(allowedStoryNodeIDs[i]) || StoryDirector.Instance.IsNodeCompleted(allowedStoryNodeIDs[i]))
                        {
                            return true;
                        }
                    }
                }
            }

            // 2. Consulta de respaldo con StoryManagerPrototipo3
            if (StoryManagerPrototipo3.Instance != null && StoryManagerPrototipo3.Instance.CurrentGraph != null)
            {
                StoryNode node = StoryManagerPrototipo3.Instance.CurrentGraph.GetNode(targetStoryNodeID);
                if (node != null && (node.state == StoryNodeState.Active || node.state == StoryNodeState.Completed))
                {
                    return true;
                }

                if (allowedStoryNodeIDs != null && allowedStoryNodeIDs.Count > 0)
                {
                    for (int i = 0; i < allowedStoryNodeIDs.Count; i++)
                    {
                        StoryNode alt = StoryManagerPrototipo3.Instance.CurrentGraph.GetNode(allowedStoryNodeIDs[i]);
                        if (alt != null && (alt.state == StoryNodeState.Active || alt.state == StoryNodeState.Completed))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool CanPacifyBoss()
        {
            return IsGateOpen();
        }

        public bool CanActivateMechanism()
        {
            return IsGateOpen();
        }
    }
}
