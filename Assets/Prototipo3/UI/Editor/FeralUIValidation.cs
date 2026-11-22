using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Prototipo3;
using Prototipo3.UI;
using Prototipo3.Narrative;
using Object = UnityEngine.Object;

namespace Prototipo3.UI.Editor
{
    /// <summary>Reproducible presentation/integration checks for Prototipo 3 UI.</summary>
    public static class FeralUIValidationPrototipo3
    {
        [MenuItem("Feral/Prototipo 3/Aplicar estilo a submenús del Menú Principal")]
        public static void StyleMenu()
        {
            const string menuScenePath = "Assets/Prototipo3/Scenes/MainMenu_Prototipo3.unity";
            if (!File.Exists(menuScenePath)) return;

            var menuScene = EditorSceneManager.OpenScene(menuScenePath, OpenSceneMode.Additive);
            try
            {
                var menu = menuScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MainMenuControllerPrototipo3>(true)).FirstOrDefault();
                if (menu != null)
                {
                    FeralTheme.ApplyMenu(menu.transform);
                    EditorSceneManager.SaveScene(menuScene);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(menuScene, true);
            }
            Debug.Log("[FeralUIValidationPrototipo3] Submenús de Prototipo 3 estilizados correctamente.");
        }
    }
}
