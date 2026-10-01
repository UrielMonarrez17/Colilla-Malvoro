using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Feral.UI
{
    // Shared by the two review scenes; no objects are injected into original scenes.
    public static class FeralUICommon
    {
        public const string MenuScene = "Assets/Scenes/UIReview/Feral_MainMenu.unity";
        public const string GameScene = "Assets/Scenes/UIReview/Game.unity";
        public static bool EnteringFromMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { EnteringFromMenu = false; }

        public static T Find<T>(Transform root, string name) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
                if (component.name == name) return component;
            throw new InvalidOperationException("Feral UI: falta " + name);
        }

        public static void Bind(Transform root, string name, Action action)
        {
            Find<Button>(root, name).onClick.AddListener(() => action());
        }

        public static void Focus(Transform root)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            foreach (var button in root.GetComponentsInChildren<Button>())
                if (button.IsInteractable())
                {
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                    break;
                }
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var system = new GameObject("Feral EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            system.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            system.AddComponent<StandaloneInputModule>();
#endif
        }

        public static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool InventoryPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        public static bool CanLoad(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) != null;
#else
            return Application.CanStreamedLevelBeLoaded(path);
#endif
        }

        public static void Load(string path)
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(path);
#endif
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = PlayerPrefs.GetInt("Feral.UI.Muted", 0) == 1
                ? 0f : PlayerPrefs.GetFloat("Feral.UI.MasterVolume", 0.8f);
        }
    }
}
