#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TutorialLayoutAudit
{
    public static void Inspect()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");
        var report = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects())
            report.AppendLine($"ROOT {root.name} active={root.activeSelf} position={root.transform.position}");
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var b = r.bounds;
            if (b.max.x >= 218 && b.min.x <= 282 && b.max.z >= 218 && b.min.z <= 255)
                report.AppendLine($"GEOMETRY {r.name} parent={r.transform.parent?.name} bounds={b} collider={r.GetComponent<Collider>() != null}");
        }
        foreach (var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b is HamsterController || b is CraftingTable || b is PuertaTrampa || b is CinematicaManager || b is TutorialPipelineManager)
                report.AppendLine($"GAMEPLAY {b.GetType().Name} {b.name} active={b.gameObject.activeInHierarchy} position={b.transform.position}");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/tutorial-layout-before.txt", report.ToString());
    }
}
#endif
