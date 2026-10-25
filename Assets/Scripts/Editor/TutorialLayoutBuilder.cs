using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class TutorialLayoutBuilder
{
    const string Folder = "Assets/Scenes/Tutorial/GuidedLayout";
    static Material ground, marking;

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
        ground = MaterialAsset("Piso", new Color(.18f, .24f, .23f));
        marking = MaterialAsset("Ruta", new Color(.92f, .67f, .22f));

        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Fence.prefab");
        var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Control Barrier Fence.prefab");

        // 1. Suelo base continuo
        Block(root, "SueloContinuo", new Vector3(250, -.22f, 245), new Vector3(65, .2f, 50), ground);

        // 2. Limpieza de muros cafés viejos si existían
        string[] oldCubes = new string[] {
            "Cierre_Norte_Base", "Cierre_Oeste_Base", "Cierre_Este_Base",
            "Cierre_EntreCasas_1", "Cierre_EntreCasas_2",
            "Ciudad_LimiteSur", "Ciudad_LimiteNorte", "Ciudad_LimiteEste", "Ciudad_LimiteOeste",
            "Muro_Perimetro_SurCallejon", "Muro_Perimetro_NorteCallejon",
            "Muro_Perimetro_SurEste", "Muro_Perimetro_NorteEste"
        };
        foreach (var cName in oldCubes)
        {
            var tr = root.Find(cName);
            if (tr != null) Object.DestroyImmediate(tr.gameObject);
        }

        // 3. Delimitación Estética con Vallas (Props_Fence) en lugar de muros cafés
        // Cierres de la Madriguera y callejón
        ColocarFilaVallas(root, fencePrefab, "Valla_Base_Norte", new Vector3(220.5f, 0f, 252f), 3, new Vector3(3.2f, 0f, 0f), Quaternion.identity);
        ColocarFilaVallas(root, fencePrefab, "Valla_Base_Oeste", new Vector3(219.5f, 0f, 237f), 4, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));
        ColocarFilaVallas(root, fencePrefab, "Valla_Base_Este",  new Vector3(230.5f, 0f, 239f), 4, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));

        // Cierre de pasadizos entre casas
        ColocarFilaVallas(root, fencePrefab, "Valla_EntreCasas_1", new Vector3(239.5f, 0f, 241f), 3, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));
        ColocarFilaVallas(root, fencePrefab, "Valla_EntreCasas_2", new Vector3(249.5f, 0f, 241f), 3, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));

        // Límites del circuito (Sur, Norte, Este) con vallas de tráfico y cercas
        ColocarFilaVallas(root, barrierPrefab, "Barrera_LimiteSur", new Vector3(225f, 0f, 222f), 15, new Vector3(3.5f, 0f, 0f), Quaternion.identity);
        ColocarFilaVallas(root, barrierPrefab, "Barrera_LimiteNorte", new Vector3(230f, 0f, 272f), 14, new Vector3(3.5f, 0f, 0f), Quaternion.identity);
        ColocarFilaVallas(root, fencePrefab, "Valla_LimiteEste", new Vector3(281.5f, 0f, 224f), 15, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));
        ColocarFilaVallas(root, fencePrefab, "Valla_LimiteOeste", new Vector3(219f, 0f, 223f), 4, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 90f, 0f));

        // 4. Coberturas de sigilo en el circuito
        root.Find("Cobertura_Sigilo_1").position = new Vector3(268.5f, 1, 224.3f);
        root.Find("Cobertura_Sigilo_2").position = new Vector3(260.5f, 1, 224.3f);

        // 5. Guías visuales amarillas por el callejón desde la madriguera hacia el cruce
        for (int i = 0; i < 6; i++)
            Block(root, "Guia_Callejon_" + i, new Vector3(225f, .035f, 242f - i * 2.0f), new Vector3(.35f, .03f, 1.0f), marking, false);

        // 6. Configurar puerta de control (PuertaControl_Ciudad) con Props_Traffic Control Barrier Fence
        var gate = GameObject.Find("PuertaControl_Ciudad");
        if (gate != null)
        {
            gate.transform.position = TutorialPipelineManager.POS_PUERTA_CONTROL;
            gate.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var oldReja = gate.transform.Find("RejaControl");
            if (oldReja != null) Object.DestroyImmediate(oldReja.gameObject);
            var oldReja2 = gate.transform.Find("RejaControl_2");
            if (oldReja2 != null) Object.DestroyImmediate(oldReja2.gameObject);

            if (barrierPrefab != null)
            {
                ColocarPropLocal(gate.transform, barrierPrefab, "RejaControl", new Vector3(0f, 0f, -1.8f), Quaternion.Euler(0f, 90f, 0f));
                ColocarPropLocal(gate.transform, barrierPrefab, "RejaControl_2", new Vector3(0f, 0f, 1.8f), Quaternion.Euler(0f, 90f, 0f));
            }
        }

        // 7. Configurar contenedor resplandeciente en la esquina antes del cruce
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

        // 10. Hornear NavMesh del Tutorial para los enemigos
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
        surface.size = new Vector3(72, 10, 60);
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

    static void ColocarFilaVallas(Transform parent, GameObject prefab, string baseName, Vector3 startPos, int count, Vector3 step, Quaternion rot)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = startPos + step * i;
            ColocarProp(parent, prefab, $"{baseName}_{i}", pos, rot);
        }
    }

    static void ColocarPropLocal(Transform parent, GameObject prefab, string name, Vector3 localPos, Quaternion localRot)
    {
        var existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = name;
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;
        if (obj.GetComponent<Collider>() == null && obj.GetComponentInChildren<Collider>() == null)
        {
            var bc = obj.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.6f, 2.05f, 0.4f);
            bc.center = new Vector3(0f, 1f, 0f);
        }
    }

    static void ColocarProp(Transform parent, GameObject prefab, string name, Vector3 pos, Quaternion rot)
    {
        var existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        // Asegurar que tenga BoxCollider para que impida físicamente el paso
        if (obj.GetComponent<Collider>() == null && obj.GetComponentInChildren<Collider>() == null)
        {
            var bc = obj.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.2f, 2f, 0.4f);
            bc.center = new Vector3(0f, 1f, 0f);
        }
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
