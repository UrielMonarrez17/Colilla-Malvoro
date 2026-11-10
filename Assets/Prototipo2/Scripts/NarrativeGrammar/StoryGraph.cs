using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo2.Narrative
{
    public enum StoryNodeType
    {
        Start,
        Encounter,
        Quest,
        Key,
        Lock,
        Boss,
        Exit
    }

    public enum StoryNodeState
    {
        Locked,
        Available,
        Active,
        Completed
    }

    [Serializable]
    public class StoryNode
    {
        public string id;
        public StoryNodeType type;
        public string title;
        public string description;
        public string hint;
        public StoryNodeState state = StoryNodeState.Locked;
        public Vector3 worldPosition;

        public List<string> prerequisiteNodeIds = new List<string>();
        public List<string> nextNodeIds = new List<string>();

        // Metadatos contextuales para gameplay
        public string targetActorName = "";
        public string requiredItemName = "";
        public int requiredItemCount = 1;
        public int currentItemProgress = 0;
        public string dialogueText = "";
    }

    [Serializable]
    public class StoryGraph
    {
        public List<StoryNode> nodes = new List<StoryNode>();
        public string startNodeId;
        public string exitNodeId;

        public StoryNode GetNode(string id)
        {
            return nodes.Find(n => n.id == id);
        }

        public StoryNode GetActiveNode()
        {
            return nodes.Find(n => n.state == StoryNodeState.Active);
        }

        public bool IsValidDAG()
        {
            // Verificación formal de que no existen ciclos mediante recorrido topológico
            Dictionary<string, int> inDegree = new Dictionary<string, int>();
            foreach (var node in nodes)
            {
                inDegree[node.id] = node.prerequisiteNodeIds.Count;
            }

            Queue<string> queue = new Queue<string>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0) queue.Enqueue(kvp.Key);
            }

            int visitedCount = 0;
            while (queue.Count > 0)
            {
                string currId = queue.Dequeue();
                visitedCount++;
                StoryNode node = GetNode(currId);
                if (node != null)
                {
                    foreach (string nxt in node.nextNodeIds)
                    {
                        if (inDegree.ContainsKey(nxt))
                        {
                            inDegree[nxt]--;
                            if (inDegree[nxt] == 0)
                            {
                                queue.Enqueue(nxt);
                            }
                        }
                    }
                }
            }

            return visitedCount == nodes.Count;
        }
    }
}
