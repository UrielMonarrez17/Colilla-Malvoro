using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Prototipo3.UI;
using Object = UnityEngine.Object;

namespace Prototipo3.UI.Editor
{
    public static class MainMenuSceneBuilderPrototipo3
    {
        const string Menu = "Assets/Prototipo3/Scenes/MainMenu_Prototipo3.unity";
        const string Art = "Assets/Prototipo3/UI/Art/";
        static Transform ui;
        static T Find<T>(string name) where T : Component => ui.GetComponentsInChildren<T>(true).First(x => x.name == name);

        static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        [MenuItem("Feral/Prototipo 3/Reconstruir Menú Principal Prototipo 3")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(Menu)) return;

            var scene = EditorSceneManager.OpenScene(Menu);
            var ctrl = Object.FindAnyObjectByType<MainMenuControllerPrototipo3>();
            if (ctrl == null) return;
            ui = ctrl.transform;
            FeralTheme.ApplyMenu(ui);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MainMenuSceneBuilderPrototipo3] Menú Principal de Prototipo 3 actualizado.");
        }
    }
}
