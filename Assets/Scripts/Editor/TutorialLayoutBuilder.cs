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
                    ColocarPropLocal(gate.transform, barrierPrefab, "RejaControl_" + (i + 1), new Vector3(0f, 0f, i * 3.3f), Quaternion.Euler(0f, 90f, 0f));
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

        // 7. Cerrar todo el perímetro del tutorial con vallas y bloqueos reales + colliders altos sólidos
        EnsureClosedTutorialPerimeter(root, fencePrefab, barrierPrefab);

        // 8. Guardar materiales de entorno
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var mat = renderer.sharedMaterial;
            if (mat != null && !AssetDatabase.Contains(mat))
                renderer.sharedMaterial = MaterialAsset("Entorno_" + ColorUtility.ToHtmlStringRGB(mat.color), mat.color);
        }

        // 9. Hornear NavMesh del Tutorial limpio sin artefactos
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
        if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
        {
            string navDataPath = $"{Folder}/NavMesh_Tutorial_EntornoGuiado.asset";
            NavMeshData builtData = Object.Instantiate(surface.navMeshData);
            surface.RemoveData();
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(navDataPath) != null)
            {
                AssetDatabase.DeleteAsset(navDataPath);
            }
            AssetDatabase.CreateAsset(builtData, navDataPath);
            AssetDatabase.SaveAssets();
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(navDataPath);
            surface.AddData();
            EditorUtility.SetDirty(surface);
        }
        if (gate != null) gate.SetActive(true);

        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static void EnsureClosedTutorialPerimeter(Transform root, GameObject fencePrefab, GameObject barrierPrefab)
    {
        // Orientaciones observadas en Tutorial.unity:
        // rotY = 0 -> Props_Fence y Props_Traffic Control Barrier Fence se extienden a lo largo de X (~3.2m - 3.5m)
        // rotY = 90 -> se extienden a lo largo de Z (~3.2m - 3.5m)
        Quaternion rotAlongX = Quaternion.identity;
        Quaternion rotAlongZ = Quaternion.Euler(0f, 90f, 0f);

        // Enderezar las 2 vallas torcidas del límite Este para evitar rendijas
        GameObject este8 = GameObject.Find("Valla_LimiteEste_8");
        if (este8 != null)
        {
            este8.transform.position = new Vector3(281.5f, 0f, 250.0f);
            este8.transform.rotation = rotAlongZ;
            EditorUtility.SetDirty(este8);
        }
        GameObject este12 = GameObject.Find("Valla_LimiteEste_12");
        if (este12 != null)
        {
            este12.transform.position = new Vector3(281.5f, 0f, 262.4f);
            este12.transform.rotation = rotAlongZ;
            EditorUtility.SetDirty(este12);
        }

        // Limpiar posible BoxCollider gigante en contenedor previo si existiera
        Transform legacyPerim = root.Find("Perimetro_Tutorial_Cierre");
        if (legacyPerim != null) Object.DestroyImmediate(legacyPerim.gameObject);
        Transform oldPerim = root.Find("GrupoPerimetro_Tutorial");
        if (oldPerim != null) Object.DestroyImmediate(oldPerim.gameObject);
        GameObject perimObj = new GameObject("GrupoPerimetro_Tutorial");
        perimObj.transform.SetParent(root, false);

        // Cierre Sur (Z = 222.0): esquinas SW y SE hasta el borde Este/Oeste
        if (barrierPrefab != null)
        {
            ColocarPropMundo(perimObj.transform, barrierPrefab, "Perimetro_Sur_SW", new Vector3(219.8f, 0f, 222.0f), rotAlongX);
            ColocarPropMundo(perimObj.transform, barrierPrefab, "Perimetro_Sur_SE_1", new Vector3(277.4f, 0f, 222.0f), rotAlongX);
            ColocarPropMundo(perimObj.transform, barrierPrefab, "Perimetro_Sur_SE_2", new Vector3(280.6f, 0f, 222.0f), rotAlongX);

            // Cierre Norte (Z = 272.0): esquinas NE y enlace NW hacia X = 229.8
            ColocarPropMundo(perimObj.transform, barrierPrefab, "Perimetro_Norte_NE_1", new Vector3(278.5f, 0f, 272.0f), rotAlongX);
            ColocarPropMundo(perimObj.transform, barrierPrefab, "Perimetro_Norte_NE_2", new Vector3(280.8f, 0f, 272.0f), rotAlongX);
            ColocarPropMundo(perimObj.transform, barrierPrefab, "Perimetro_Norte_NW", new Vector3(227.2f, 0f, 272.0f), rotAlongX);
        }

        // Cierres con Vallas (Props_Fence) a lo largo de E, W y tramo NW
        if (fencePrefab != null)
        {
            // Borde Este (X = 281.5): cubrir cualquier hueco en Z=222..272
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Este_Sur", new Vector3(281.5f, 0f, 222.4f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Este_Mid1", new Vector3(281.5f, 0f, 249.6f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Este_Mid2", new Vector3(281.5f, 0f, 262.4f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Este_Norte", new Vector3(281.5f, 0f, 270.8f), rotAlongZ);

            // Borde Oeste (X = 219.2): cerrar huecos entre Valla_LimiteOeste (Z<=232.6), Valla_Base_Oeste (240.2..246.6) y Valla_Base_Norte (Z=252)
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Oeste_Sur1", new Vector3(219.0f, 0f, 221.2f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Oeste_Mid1", new Vector3(219.2f, 0f, 235.5f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Oeste_Mid2", new Vector3(219.2f, 0f, 238.5f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Oeste_Norte1", new Vector3(219.5f, 0f, 249.6f), rotAlongZ);
            ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_Oeste_Norte2", new Vector3(219.5f, 0f, 251.8f), rotAlongZ);

            // Cierre tramo Noroeste (X = 230.2, Z = 252.5..271.5) conectando la base con Barrera_LimiteNorte_0
            float[] nwZ = { 253.5f, 256.7f, 259.9f, 263.1f, 266.3f, 269.5f };
            for (int i = 0; i < nwZ.Length; i++)
            {
                ColocarPropMundo(perimObj.transform, fencePrefab, "Perimetro_NW_" + i, new Vector3(230.2f, 0f, nwZ[i]), rotAlongZ);
            }
        }

        // Asegurar BoxCollider alto (>= 3.4m) y sólido en TODAS las vallas/barreras/rejas del tutorial
        Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            if (tr == null) continue;
            string n = tr.name;
            if (n.StartsWith("Valla_") || n.StartsWith("Barrera_") || n.StartsWith("RejaControl") || n.StartsWith("Perimetro_"))
            {
                EnsureTallSolidBarrierCollider(tr.gameObject);
            }
        }
    }

    static void ColocarPropMundo(Transform parent, GameObject prefab, string name, Vector3 worldPos, Quaternion worldRot)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = name;
        obj.transform.position = worldPos;
        obj.transform.rotation = worldRot;
        EnsureTallSolidBarrierCollider(obj);
    }

    static void ColocarPropLocal(Transform parent, GameObject prefab, string name, Vector3 localPos, Quaternion localRot)
    {
        var existing = parent.Find(name);
        GameObject obj = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = name;
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;
        EnsureTallSolidBarrierCollider(obj);
    }

    static void EnsureTallSolidBarrierCollider(GameObject obj)
    {
        if (obj == null) return;
        if (obj.name == "GrupoPerimetro_Tutorial" || obj.name == "Perimetro_Tutorial_Cierre") return;
        for (int c = 0; c < obj.transform.childCount; c++)
        {
            string cn = obj.transform.GetChild(c).name;
            if (cn.StartsWith("Perimetro_") || cn.StartsWith("Valla_") || cn.StartsWith("Barrera_") || cn.StartsWith("RejaControl"))
                return;
        }

        // Medir ancho/profundidad local a partir de los renderers hijos
        Renderer[] rens = obj.GetComponentsInChildren<Renderer>(true);
        Vector3 minLocal = new Vector3(-1.7f, 0f, -0.32f);
        Vector3 maxLocal = new Vector3(1.7f, 3.6f, 0.32f);
        if (rens.Length > 0)
        {
            minLocal = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            maxLocal = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int r = 0; r < rens.Length; r++)
            {
                if (rens[r] == null) continue;
                Bounds wb = rens[r].bounds;
                for (int cx = -1; cx <= 1; cx += 2)
                for (int cy = -1; cy <= 1; cy += 2)
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    Vector3 wp = wb.center + Vector3.Scale(wb.extents, new Vector3(cx, cy, cz));
                    Vector3 lp = obj.transform.InverseTransformPoint(wp);
                    minLocal = Vector3.Min(minLocal, lp);
                    maxLocal = Vector3.Max(maxLocal, lp);
                }
            }
        }

        float sizeX = Mathf.Max(0.65f, (maxLocal.x - minLocal.x) + 0.12f);
        float sizeZ = Mathf.Max(0.65f, (maxLocal.z - minLocal.z) + 0.12f);
        float sizeY = 3.6f; // Suficientemente alto para bloquear saltos y dash del hámster

        BoxCollider bc = obj.GetComponent<BoxCollider>();
        if (bc == null) bc = obj.AddComponent<BoxCollider>();
        bc.enabled = true;
        bc.isTrigger = false;
        bc.center = new Vector3((minLocal.x + maxLocal.x) * 0.5f, sizeY * 0.5f, (minLocal.z + maxLocal.z) * 0.5f);
        bc.size = new Vector3(sizeX, sizeY, sizeZ);
        EditorUtility.SetDirty(bc);
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
