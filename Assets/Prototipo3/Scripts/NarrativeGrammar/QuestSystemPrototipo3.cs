using System;
using UnityEngine;

namespace Prototipo3.Narrative
{
    public class QuestSystemPrototipo3 : MonoBehaviour
    {
        public static QuestSystemPrototipo3 Instance { get; private set; }

        public event Action<StoryNodePrototipo3> OnQuestUpdated;
        public event Action<string> OnQuestCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void NotifyQuestUpdated(StoryNodePrototipo3 node)
        {
            OnQuestUpdated?.Invoke(node);
        }

        public void NotifyQuestCompleted(string nodeId)
        {
            OnQuestCompleted?.Invoke(nodeId);
        }
    }
}
