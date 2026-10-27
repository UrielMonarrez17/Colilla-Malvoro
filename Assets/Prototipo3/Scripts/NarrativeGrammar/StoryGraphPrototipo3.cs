using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    public enum StoryNodeTypePrototipo3
    {
        Start,
        Encounter,
        Quest,
        SideQuest,
        MiniBoss,
        Key,
        Lock,
        FinalBoss,
        Exit
    }

    public enum StoryNodeStatePrototipo3
    {
        Locked,
        Available,
        Active,
        Completed
    }

    [Serializable]
    public class StoryNodePrototipo3
    {
        public string id;
        public StoryNodeTypePrototipo3 type;
        public string title;
        public string description;
        public string hint;
        public StoryNodeStatePrototipo3 state = StoryNodeStatePrototipo3.Locked;
        public Vector3 worldPosition;

        public List<string> prerequisiteNodeIds = new List<string>();
        public List<string> nextNodeIds = new List<string>();

        // Metadatos contextuales para gameplay e interacciones
        public string targetActorName = "";
        public string requiredItemName = "";
        public int requiredItemCount = 1;
        public int currentItemProgress = 0;
        public string dialogueText = "";
        public bool isOptional = false;
        public string cinematicId = "";
    }

    [Serializable]
    public class StoryGraphPrototipo3
    {
        public List<StoryNodePrototipo3> nodes = new List<StoryNodePrototipo3>();
        public string startNodeId;
        public string exitNodeId;

        public StoryNodePrototipo3 GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }

        public StoryNodePrototipo3 GetActiveNode()
        {
            // Busca primero el nodo principal activo
            StoryNodePrototipo3 active = nodes.Find(n => n.state == StoryNodeStatePrototipo3.Active && !n.isOptional);
            if (active != null) return active;
            return nodes.Find(n => n.state == StoryNodeStatePrototipo3.Active);
        }

        public List<StoryNodePrototipo3> GetAvailableNodes()
        {
            return nodes.FindAll(n => n.state == StoryNodeStatePrototipo3.Available);
        }

        public List<StoryNodePrototipo3> GetActiveNodes()
        {
            return nodes.FindAll(n => n.state == StoryNodeStatePrototipo3.Active);
        }

        public bool ArePrerequisitesMet(StoryNodePrototipo3 node)
        {
            if (node == null || node.prerequisiteNodeIds == null || node.prerequisiteNodeIds.Count == 0)
                return true;

            foreach (var preId in node.prerequisiteNodeIds)
            {
                var preNode = GetNode(preId);
                if (preNode != null && preNode.state != StoryNodeStatePrototipo3.Completed)
                    return false;
            }
            return true;
        }

        public void ActivateAvailableNodes()
        {
            foreach (var node in nodes)
            {
                if (node.state == StoryNodeStatePrototipo3.Locked && ArePrerequisitesMet(node))
                {
                    node.state = StoryNodeStatePrototipo3.Available;
                }
            }
        }

        public bool CompleteNode(string nodeId)
        {
            var node = GetNode(nodeId);
            if (node == null) return false;

            node.state = StoryNodeStatePrototipo3.Completed;

            // Desbloquea nodos subsecuentes
            foreach (var nextId in node.nextNodeIds)
            {
                var nextNode = GetNode(nextId);
                if (nextNode != null && nextNode.state == StoryNodeStatePrototipo3.Locked && ArePrerequisitesMet(nextNode))
                {
                    nextNode.state = StoryNodeStatePrototipo3.Available;
                }
            }
            return true;
        }

        public bool ValidateDAG()
        {
            // Verificación de ciclos mediante algoritmo de Kahn o DFS
            var inDegree = new Dictionary<string, int>();
            foreach (var n in nodes) inDegree[n.id] = 0;

            foreach (var n in nodes)
            {
                foreach (var next in n.nextNodeIds)
                {
                    if (inDegree.ContainsKey(next))
                        inDegree[next]++;
                }
            }

            var queue = new Queue<string>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0) queue.Enqueue(kvp.Key);
            }

            int visitedCount = 0;
            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                visitedCount++;
                var uNode = GetNode(u);
                if (uNode != null)
                {
                    foreach (var next in uNode.nextNodeIds)
                    {
                        if (inDegree.ContainsKey(next))
                        {
                            inDegree[next]--;
                            if (inDegree[next] == 0) queue.Enqueue(next);
                        }
                    }
                }
            }

            return visitedCount == nodes.Count;
        }
    }
}
