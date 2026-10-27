using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class TutorialLayoutBuilder
{
    const string Folder = "Assets/Scenes/Tutorial/GuidedLayout";
    static Material marking;

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
        marking = MaterialAsset("Ruta", new Color(.92f, .67f, .22f));

        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Fence.prefab");
        var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Control Barrier Fence.prefab");

        // 1. Limpieza de cubos/objetos invisibles obsoletos
        string[] oldObjects = new string[] {
            "SueloContinuo", "Piso_SueloTutorial_AntiCaidas",
            "Muro_Control_Norte", "Muro_Control_Sur", "Muro_Esquina_RomperVista",
            "BarreraSecuencia_SalidaMadriguera", "BarreraSecuencia_SprintCallejon",
            "BarreraSecuencia_EsquinaSigilo", "BarreraSecuencia_EntradaRefugio",
            "Cierre_Norte_Base", "Cierre_Oeste_Base", "Cierre_Este_Base",
            "Cierre_EntreCasas_1", "Cierre_EntreCasas_2",
            "Ciudad_LimiteSur", "Ciudad_LimiteNorte", "Ciudad_LimiteEste", "Ciudad_LimiteOeste"
        };
        foreach (var cName in oldObjects)
        {
            var tr = root.Find(cName);
            if (tr != null) Object.DestroyImmediate(tr.gameObject);
        }

        // 2. Coberturas de sigilo estéticas
        var cob1 = root.Find("Cobertura_Sigilo_1");
        if (cob1 != null) cob1.position = new Vector3(268.5f, 1f, 224.3f);
        var cob2 = root.Find("Cobertura_Sigilo_2");
        if (cob2 != null) cob2.position = new Vector3(260.5f, 1f, 224.3f);

        // 3. Guías visuales amarillas por el callejón desde la madriguera hacia la calle
        for (int i = 0; i < 6; i++)
            Block(root, "Guia_Callejon_" + i, new Vector3(225f, .035f, 242f - i * 2.0f), new Vector3(.35f, .03f, 1.0f), marking, false);

        // 4. Puerta de Control (PuertaControl_Ciudad) con Props_Traffic Control Barrier Fence
        // Bloqueo horizontal transversal de la calle en X = 251.0 (después del contenedor en X = 246.34)
        var gate = GameObject.Find("PuertaControl_Ciudad");
        if (gate != null)
        {
            gate.transform.position = TutorialPipelineManager.POS_PUERTA_CONTROL;
            gate.transform.rotation = Quaternion.identity;

            while (gate.transform.childCount > 0)
                Object.DestroyImmediate(gate.transform.GetChild(0).gameObject);

            if (barrierPrefab != null)
            {
                for (int i = -1; i <= 1; i++)
                {
                    ColocarPropLocal(gate.transform, barrierPrefab, "RejaControl_" + (i + 1), new Vector3(0f, 0f, i * 3.3f), Quaternion.identity);
                }
            }
        }

        // 5. Configurar contenedor resplandeciente en su posición asignada
        var gc = GameObject.Find("GarbageCollector");
        if (gc != null)
        {
            gc.transform.position = TutorialPipelineManager.POS_CONTENEDOR_BRILLO;
            var lc = gc.GetComponent<LootContainer>();
            if (lc != null)
            {
                lc.puertaDesbloqueable = gate;
                lc.esContenedorResplandeciente = true;
                lc.ConfigurarEfectoResplandor();
            }
        }

        // 6. Asegurar que la puerta del refugio permita el paso al abrir su hoja y deshabilitar colisionador de bloqueo de Frame
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

        // 7. Guardar materiales de entorno
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var mat = renderer.sharedMaterial;
            if (mat != null && !AssetDatabase.Contains(mat))
                renderer.sharedMaterial = MaterialAsset("Entorno_" + ColorUtility.ToHtmlStringRGB(mat.color), mat.color);
        }

        // 8. Hornear NavMesh del Tutorial limpio sin artefactos
        foreach (var old in Object.FindObjectsByType<NavMeshSurface>())
        {
            old.RemoveData();
            old.enabled = false;
        }
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.enabled = true;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(250f, 3f, 245f);
        surface.size = new Vector3(75f, 12f, 65f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .08f;

        if (gate != null) gate.SetActive(false);
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (gate != null) gate.SetActive(true);

        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
            bc.size = new Vector3(0.4f, 2.05f, 3.6f);
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
