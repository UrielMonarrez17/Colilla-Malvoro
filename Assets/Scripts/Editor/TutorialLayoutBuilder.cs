#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

// Explicit, repeatable scene operation. Never runs when opening the editor.
public static class TutorialLayoutBuilder
{
    const string Folder = "Assets/Scenes/Tutorial/GuidedLayout";
    static Material wall, ground, marking;

    public static void BuildBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");
        Build();
    }

    [MenuItem("Feral/Tutorial/Construir recorrido guiado y hornear navegacion")]
    public static void Build()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Tutorial.unity") return;
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        TutorialSceneSetupEditor.ApplyTutorialSceneArrangement(false);
        var manager = Object.FindAnyObjectByType<TutorialPipelineManager>();
        manager.ConstruirArquitecturaRefugioYCallejon();
        var root = GameObject.Find("Tutorial_EntornoGuiado").transform;
        wall = MaterialAsset("Muro", new Color(.32f, .25f, .20f));
        ground = MaterialAsset("Piso", new Color(.18f, .24f, .23f));
        marking = MaterialAsset("Ruta", new Color(.92f, .67f, .22f));
        // Fill seams beneath the existing roads without replacing their surfaces.
        Block(root, "SueloContinuo", new Vector3(30, -.22f, 135), new Vector3(60, .2f, 32), ground);
        Block(root, "Cierre_Norte_Base", new Vector3(5, 1.8f, 150), new Vector3(10, 3.6f, .6f), wall);
        Block(root, "Cierre_Este_Base", new Vector3(10, 1.8f, 140), new Vector3(.6f, 3.6f, 20), wall);
        Block(root, "Callejon_BordeNorte", new Vector3(19.5f, 1.5f, 130), new Vector3(19, 3, .6f), wall);
        Block(root, "Callejon_BordeSur", new Vector3(19.5f, 1.5f, 120), new Vector3(29, 3, .6f), wall);
        Block(root, "Ciudad_LimiteSur", new Vector3(44.5f, 1.5f, 119.7f), new Vector3(31, 3, .6f), wall);
        Block(root, "Ciudad_LimiteNorte", new Vector3(45, 1.5f, 150), new Vector3(30, 3, .6f), wall);
        Block(root, "Ciudad_LimiteEste", new Vector3(60, 1.5f, 135), new Vector3(.6f, 3, 30), wall);
        // Cover sits BETWEEN the patrol and the crouched route, not behind the player.
        root.Find("Cobertura_Sigilo_1").position = new Vector3(48.5f, 1, 124.3f);
        root.Find("Cobertura_Sigilo_2").position = new Vector3(40.5f, 1, 124.3f);
        Block(root, "Cobertura_Sigilo_3", new Vector3(34, 1, 124.3f), new Vector3(3.4f, 2, 1.1f), wall);
        // Save the runtime-created materials so scene references survive a build/reload.
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var mat = renderer.sharedMaterial;
            if (mat != null && !AssetDatabase.Contains(mat))
                renderer.sharedMaterial = MaterialAsset("Entorno_" + ColorUtility.ToHtmlStringRGB(mat.color), mat.color);
        }
        for (int i = 0; i < 8; i++)
            Block(root, "Guia_Callejon_" + i, new Vector3(8 + i * 2.5f, .035f, 125), new Vector3(1, .03f, .15f), marking, false);

        // Preserve the door asset; open its leaf so it cannot trap the player in the base.
        var door = manager.puertaTrampaRefugio;
        foreach (var hinge in door.GetComponentsInChildren<DoorScript.Door>())
        {
            hinge.open = true;
            hinge.transform.localRotation = Quaternion.Euler(0, -90, 0);
            EditorUtility.SetDirty(hinge);
        }
        // Bake only the tutorial volume; do not overwrite the user's city NavMesh assets.
        foreach (var old in Object.FindObjectsByType<NavMeshSurface>())
        {
            old.RemoveData();
            old.enabled = false;
        }
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.enabled = true;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(30, 3, 135);
        surface.size = new Vector3(62, 10, 34);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .1f;
        var existingGate = GameObject.Find("PuertaControl_Ciudad");
        if (existingGate != null) existingGate.SetActive(false);
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (existingGate != null) existingGate.SetActive(true);
        string navPath = Folder + "/TutorialNavigation.asset";
        var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
        if (saved == null) AssetDatabase.CreateAsset(surface.navMeshData, navPath);
        else
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(surface.navMeshData, saved);
            surface.navMeshData = saved;
            surface.AddData();
        }
        // The gate is dynamic: bake the open route, then carve until the first loot.
        var gate = GameObject.Find("PuertaControl_Ciudad");
        if (gate == null) gate = new GameObject("PuertaControl_Ciudad");
        gate.transform.position = TutorialPipelineManager.POS_PUERTA_CONTROL;
        Block(gate.transform, "RejaControl", new Vector3(28.8f, 1.4f, 125), new Vector3(.45f, 2.6f, 4.2f), marking);
        var obstacle = gate.GetComponent<NavMeshObstacle>();
        if (obstacle == null) obstacle = gate.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = new Vector3(0, 1.3f, 0);
        obstacle.size = new Vector3(.45f, 2.6f, 4.2f);
        obstacle.carving = true;
        var loot = GameObject.Find("GarbageCollector").GetComponent<LootContainer>();
        loot.puertaDesbloqueable = gate;
        loot.esContenedorResplandeciente = true;
        loot.itemsGarantizados = new System.Collections.Generic.List<ItemType> { ItemType.Madera, ItemType.Metal };
        Physics.SyncTransforms();
        ValidatePaths();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveScene(manager.gameObject.scene);
    }

    static Material MaterialAsset(string name, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void Block(Transform root, string name, Vector3 position, Vector3 scale, Material mat, bool solid = true)
    {
        var child = root.Find(name);
        var go = child != null ? child.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!solid && go.GetComponent<Collider>() != null) Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static void ValidatePaths()
    {
        Vector3[] points = { new Vector3(5,0,146), new Vector3(5,0,132), new Vector3(22,0,125),
            new Vector3(35,0,125), new Vector3(35,0,145), new Vector3(48,0,145),
            new Vector3(55,0,126), new Vector3(50,0,122), new Vector3(32,0,122), new Vector3(22,0,125), new Vector3(5,0,146) };
        var report = new StringBuilder();
        for (int i = 1; i < points.Length; i++)
        {
            var path = new NavMeshPath();
            bool ok = NavMesh.SamplePosition(points[i-1], out var a, 2, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(points[i], out var b, 2, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
            report.AppendLine($"{points[i-1]} -> {points[i]}: {(ok ? "PASS" : "FAIL")}");
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/tutorial-navigation.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
#endif
