using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

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

        // 1. Suelo base continuo
        Block(root, "SueloContinuo", new Vector3(250, -.22f, 245), new Vector3(65, .2f, 50), ground);

        // 2. Muros perimetrales para sellar huecos entre casas y evitar que el jugador escape
        // Norte: detrás de la madriguera (Z=250..265) y a lo largo de Z=250 / Z=270
        Block(root, "Cierre_Norte_Base", new Vector3(225, 1.8f, 252), new Vector3(10, 3.6f, .6f), wall);
        Block(root, "Cierre_Oeste_Base", new Vector3(220, 1.8f, 245), new Vector3(.6f, 3.6f, 16), wall);
        Block(root, "Cierre_Este_Base", new Vector3(230, 1.8f, 246), new Vector3(.6f, 3.6f, 14), wall);

        // Bloqueo de callejones entre casas en Z=235..245
        Block(root, "Cierre_EntreCasas_1", new Vector3(240, 1.8f, 245), new Vector3(2, 3.6f, 10), wall);
        Block(root, "Cierre_EntreCasas_2", new Vector3(250, 1.8f, 245), new Vector3(2, 3.6f, 10), wall);

        // Límites exteriores del circuito del tutorial
        Block(root, "Ciudad_LimiteSur", new Vector3(250, 1.8f, 222), new Vector3(65, 3.6f, .8f), wall);
        Block(root, "Ciudad_LimiteNorte", new Vector3(255, 1.8f, 272), new Vector3(55, 3.6f, .8f), wall);
        Block(root, "Ciudad_LimiteEste", new Vector3(282, 1.8f, 245), new Vector3(.8f, 3.6f, 55), wall);
        Block(root, "Ciudad_LimiteOeste", new Vector3(218, 1.8f, 235), new Vector3(.8f, 3.6f, 30), wall);

        // 3. Decoración con vallas y accesorios urbanos para dar aspecto natural
        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Fence.prefab");
        var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Control Barrier Fence.prefab");
        if (fencePrefab != null)
        {
            ColocarProp(root, fencePrefab, "Valla_Decorativa_1", new Vector3(221f, 0f, 242f), Quaternion.Euler(0, 90, 0));
            ColocarProp(root, fencePrefab, "Valla_Decorativa_2", new Vector3(229f, 0f, 242f), Quaternion.Euler(0, 90, 0));
        }
        if (barrierPrefab != null)
        {
            ColocarProp(root, barrierPrefab, "Barrera_Vial_Control", new Vector3(227.5f, 0f, 230f), Quaternion.identity);
        }

        // 4. Coberturas de sigilo en el circuito
        root.Find("Cobertura_Sigilo_1").position = new Vector3(268.5f, 1, 224.3f);
        root.Find("Cobertura_Sigilo_2").position = new Vector3(260.5f, 1, 224.3f);
        Block(root, "Cobertura_Sigilo_3", new Vector3(254, 1, 224.3f), new Vector3(3.4f, 2, 1.1f), wall);

        // 5. Guías visuales amarillas por el callejón desde la madriguera hacia la puerta de control
        for (int i = 0; i < 6; i++)
            Block(root, "Guia_Callejon_" + i, new Vector3(225f, .035f, 242f - i * 2.0f), new Vector3(.35f, .03f, 1.0f), marking, false);

        // 6. Configurar puerta de control (RejaControl) en la escena
        var gate = GameObject.Find("PuertaControl_Ciudad");
        if (gate != null)
        {
            gate.transform.position = TutorialPipelineManager.POS_PUERTA_CONTROL;
            var reja = gate.transform.Find("RejaControl");
            if (reja != null)
            {
                reja.localPosition = new Vector3(0f, 1.3f, 0f);
                reja.localScale = new Vector3(5.5f, 2.6f, 0.45f);
            }
        }

        // 7. Configurar contenedor resplandeciente
        var gc = GameObject.Find("GarbageCollector");
        if (gc != null)
        {
            gc.transform.position = TutorialPipelineManager.POS_CONTENEDOR_BRILLO;
            var lc = gc.GetComponent<LootContainer>();
            if (lc != null)
            {
                lc.esContenedorResplandeciente = true;
                lc.ConfigurarEfectoResplandor();
            }
        }

        // 8. Asegurar que la puerta del refugio permita el paso al abrir su hoja y deshabilitar colisionador de bloqueo de Frame
        var door = manager.puertaTrampaRefugio;
        if (door != null)
        {
            door.transform.position = TutorialPipelineManager.POS_PUERTA_TRAMPA_REF;
            door.transform.rotation = Quaternion.identity;
            var frame = door.transform.Find("Frame");
            if (frame != null)
            {
                var mc = frame.GetComponent<MeshCollider>();
                if (mc != null) mc.enabled = false;
            }
            foreach (var hinge in door.GetComponentsInChildren<DoorScript.Door>())
            {
                hinge.open = true;
                hinge.transform.localRotation = Quaternion.Euler(0, -90, 0);
                EditorUtility.SetDirty(hinge);
            }
        }

        // 9. Guardar materiales de entorno
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var mat = renderer.sharedMaterial;
            if (mat != null && !AssetDatabase.Contains(mat))
                renderer.sharedMaterial = MaterialAsset("Entorno_" + ColorUtility.ToHtmlStringRGB(mat.color), mat.color);
        }

        // 10. Hornear NavMesh del Tutorial
        foreach (var old in Object.FindObjectsByType<NavMeshSurface>())
        {
            old.RemoveData();
            old.enabled = false;
        }
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.enabled = true;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(250, 3, 245);
        surface.size = new Vector3(68, 10, 56);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .1f;

        if (gate != null) gate.SetActive(false);
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (gate != null) gate.SetActive(true);

        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static void ColocarProp(Transform parent, GameObject prefab, string name, Vector3 pos, Quaternion rot)
    {
        var existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.rotation = rot;
    }

    static Material MaterialAsset(string name, Color color)
    {
        string path = $"{Folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
        }
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = collider;
        return go;
    }
}
