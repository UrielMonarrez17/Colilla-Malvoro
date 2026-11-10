using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Registro global estático y singleton para acceso desacoplado a entidades,
    /// edificios clave (Landmarks), NPCs, contenedores y mecanismos de la ciudad procedural.
    /// </summary>
    public class WorldRegistry : MonoBehaviour
    {
        public static WorldRegistry Instance { get; private set; }

        private static readonly Dictionary<string, Transform> registry = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, Transform> AllEntries => registry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static void Register(string key, Transform target)
        {
            if (string.IsNullOrEmpty(key) || target == null) return;
            registry[key] = target;
        }

        public static bool Unregister(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return registry.Remove(key);
        }

        public static bool TryGet(string key, out Transform target)
        {
            if (string.IsNullOrEmpty(key))
            {
                target = null;
                return false;
            }
            return registry.TryGetValue(key, out target);
        }

        public static Transform Get(string key)
        {
            TryGet(key, out Transform target);
            return target;
        }

        public static bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return registry.ContainsKey(key);
        }

        public static void Clear()
        {
            registry.Clear();
        }

        public static WorldRegistry EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("WorldRegistry");
            return go.AddComponent<WorldRegistry>();
        }
    }
}
