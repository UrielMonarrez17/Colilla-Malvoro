#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class TutorialSceneSetupEditor
{

    [MenuItem("Feral/Tutorial/Configurar y Validar Escena Tutorial")]
    public static void SetupTutorialScene()
    {
        ApplyTutorialSceneArrangement(showDialog: true);
    }

    public static void ApplyTutorialSceneArrangement(bool showDialog)
    {
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Tutorial.unity" || Application.isPlaying) return;
        TutorialPipelineManager manager = Object.FindAnyObjectByType<TutorialPipelineManager>();
        if (manager == null)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Feral - Tutorial Setup",
                    "No se encontró un TutorialPipelineManager en la escena abierta. Abre primero Assets/Scenes/Tutorial.unity.",
                    "Aceptar");
            }
            return;
        }

        Undo.RecordObject(manager, "Acomodar y Configurar Escena Tutorial");
        manager.cinematicaAnochecer = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/UI/Videos/NightfallFeral.mp4");

        // 1. Vincular Prefabs al TutorialPipelineManager
        if (manager.prefabEnemigoTigre == null)
            manager.prefabEnemigoTigre = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Enemies/Tiger_001.prefab");
        if (manager.prefabEnemigoPerro == null)
            manager.prefabEnemigoPerro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Enemies/Dog_001.prefab");
        if (manager.prefabCiudadanoGato == null)
        {
            manager.prefabCiudadanoGato = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/Animals_FREE/Prefabs/Kitty_001.prefab");
            if (manager.prefabCiudadanoGato == null)
                manager.prefabCiudadanoGato = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab");
        }
        if (manager.prefabCiudadanoCiervo == null)
        {
            manager.prefabCiudadanoCiervo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/Animals_FREE/Prefabs/Deer_001.prefab");
            if (manager.prefabCiudadanoCiervo == null)
                manager.prefabCiudadanoCiervo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/ithappy/Animals_FREE/Prefabs/Deer_001.prefab");
        }
        if (manager.prefabPuertaControl == null)
        {
            manager.prefabPuertaControl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Doors/Door_1_Yellow Variant.prefab");
            if (manager.prefabPuertaControl == null)
                manager.prefabPuertaControl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Door.prefab");
        }
        if (manager.prefabContenedorLoot == null)
        {
            manager.prefabContenedorLoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/LootContainers/GarbageCollector.prefab");
            if (manager.prefabContenedorLoot == null)
                manager.prefabContenedorLoot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Basura2.prefab");
        }

        // 2. Posicionar al Hámster (Mesh_Hamster) en la Madriguera / Zona Segura dentro de cityManagerMath
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            Undo.RecordObject(pObj.transform, "Posicionar Mesh_Hamster");
            pObj.transform.position = TutorialPipelineManager.POS_MADRIGUERA_INICIO;
            pObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            manager.hamster = pObj.GetComponent<HamsterController>();
        }

        // 3. Posicionar Main Camera (1) enfocando al Hámster
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Undo.RecordObject(mainCam.transform, "Posicionar Main Camera");
            CameraFollow cf = mainCam.GetComponent<CameraFollow>();
            if (cf != null && manager.hamster != null) cf.target = manager.hamster.transform;
            Vector3 offset = cf != null ? cf.offset : new Vector3(0f, 11f, -9f);
            mainCam.transform.position = TutorialPipelineManager.POS_MADRIGUERA_INICIO + offset;
            mainCam.transform.rotation = Quaternion.Euler(42.189f, 0f, 0f);
        }

        // 4. Posicionar Mesa de Fabricación (CraftTable) en el Refugio
        CraftingTable mesa = Object.FindAnyObjectByType<CraftingTable>();
        if (mesa != null)
        {
            Undo.RecordObject(mesa.transform, "Posicionar CraftTable");
            mesa.transform.position = TutorialPipelineManager.POS_MESA_REFUGIO;
            mesa.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            mesa.gameObject.tag = "Interactable";
            manager.mesaCrafteoRefugio = mesa;
        }

        // 5. Posicionar PuertaTrampa (Door_1_Yellow Variant) en la entrada estrecha del Refugio
        PuertaTrampa puerta = Object.FindAnyObjectByType<PuertaTrampa>();
        if (puerta != null)
        {
            Transform rootDoor = puerta.transform.root;
            Undo.RecordObject(rootDoor, "Posicionar PuertaTrampa");
            Undo.RecordObject(puerta, "Resetear PuertaTrampa");
            rootDoor.position = TutorialPipelineManager.POS_PUERTA_TRAMPA_REF;
            rootDoor.rotation = Quaternion.Euler(0f, 0f, 0f);
            puerta.trampaActual = TipoTrampa.Ninguna;
            puerta.gameObject.tag = "Door";
            manager.puertaTrampaRefugio = puerta;
        }

        // 6. Posicionar a Lulú en la plaza norte de la ciudad
        LuluNPC lulu = Object.FindAnyObjectByType<LuluNPC>();
        if (lulu != null)
        {
            Undo.RecordObject(lulu.transform, "Posicionar Lulu");
            lulu.transform.position = TutorialPipelineManager.POS_LULU_CIUDAD;
            lulu.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            lulu.gameObject.tag = "Interactable";
            manager.luluNPC = lulu;
        }

        // 7. Posicionar Contenedores de Basura (Contenedor Resplandeciente + 4 Contenedores en Ciudad)
        GameObject contBrillo = GameObject.Find("GarbageCollector");
        if (contBrillo != null)
        {
            Undo.RecordObject(contBrillo.transform, "Posicionar Contenedor Resplandeciente");
            contBrillo.transform.position = TutorialPipelineManager.POS_CONTENEDOR_BRILLO;
            contBrillo.transform.rotation = Quaternion.identity;
        }

        string[] cityLootNames = { "GarbageCollector (4)", "GarbageCollector (5)", "GarbageCollector (8)", "GarbageCollector (2)" };
        Vector3[] cityLootPositions =
        {
            new Vector3(35.0f, 0.35f, 135.0f),
            new Vector3(45.0f, 0.35f, 122.2f),
            new Vector3(55.0f, 0.35f, 135.0f),
            new Vector3(52.5f, 0.35f, 145.0f)
        };
        for (int i = 0; i < cityLootNames.Length; i++)
        {
            GameObject c = GameObject.Find(cityLootNames[i]);
            if (c != null)
            {
                Undo.RecordObject(c.transform, "Posicionar Contenedor Ciudad");
                c.transform.position = cityLootPositions[i];
                c.transform.rotation = Quaternion.identity;
            }
        }

        // 8. Corregir VideoPlayer de Cinematica (evitar AudioSampleProvider buffer overflow y bloqueo al terminar)
        CinematicaManager cinematica = Object.FindAnyObjectByType<CinematicaManager>();
        if (cinematica != null)
        {
            Undo.RecordObject(cinematica.transform, "Posicionar Cinematica");
            cinematica.transform.position = new Vector3(5.0f, 0f, 146.0f);
            VideoPlayer vp = cinematica.GetComponent<VideoPlayer>();
            if (vp != null)
            {
                Undo.RecordObject(vp, "Configurar VideoPlayer Cinematica");
                vp.isLooping = false;
                vp.clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/UI/Videos/TestInitialVideo.mp4");
                vp.targetCamera = mainCam;
                vp.renderMode = VideoRenderMode.CameraNearPlane;
                vp.targetCameraAlpha = 1f;
                vp.playOnAwake = false;
                cinematica.hamster = manager.hamster;
                cinematica.objetoTutorialManager = manager.gameObject;
                vp.audioOutputMode = VideoAudioOutputMode.Direct;
                EditorUtility.SetDirty(vp);
            }
        }

        manager.transform.position = new Vector3(5.0f, 0f, 146.0f);

        // 9. Desactivar objetos rezagados fuera del mapa (Z = -1320 / Z = -300) que causaban errores de NavMesh o scripts faltantes
        string[] strayObjects =
        {
            "Enemy", "Wall", "Wall (1)", "Wall (2)", "Wall (3)", "Wall (4)",
            "Crossroads_1_lines_walk", "Crossroads_1_lines_walk (1)", "Pavement",
            "Ciudad_Rodrigo", "CityGenerator"
        };
        for (int i = 0; i < strayObjects.Length; i++)
        {
            GameObject stray = GameObject.Find(strayObjects[i]);
            if (stray != null && stray.transform.parent == null && stray.activeSelf)
            {
                Undo.RecordObject(stray, "Desactivar Objeto Rezagado");
                stray.SetActive(false);
            }
        }

        EditorUtility.SetDirty(manager);
        Scene scene = manager.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Feral Tutorial] Escena Tutorial.unity acomodada, validada y guardada exitosamente.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Feral - Escena Tutorial Acomodada",
                "Todos los objetos de Tutorial.unity (Mesh_Hamster, Main Camera, CraftTable, PuertaTrampa, Lulu, Contenedores y Cinematica) han sido acomodados dentro de la ciudad y la escena se ha guardado.",
                "Excelente");
        }
    }
}
#endif
