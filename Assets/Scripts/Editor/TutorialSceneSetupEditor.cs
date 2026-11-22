#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

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
            manager.prefabEnemigoTigre = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo2/Prefabs/Tiger_Prototipo2.prefab");
        if (manager.prefabEnemigoPerro == null)
            manager.prefabEnemigoPerro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo2/Prefabs/Dog_Prototipo2.prefab");
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
            if (cf is Prototipo2.IsometricCameraFollow iso)
            {
                mainCam.fieldOfView = iso.fieldOfView;
                mainCam.transform.rotation = Quaternion.Euler(iso.pitchAngle, iso.yawAngle, 0f);
                Vector3 targetCenter = TutorialPipelineManager.POS_MADRIGUERA_INICIO + iso.targetOffset;
                iso.offset = mainCam.transform.rotation * (Vector3.back * iso.distance);
                mainCam.transform.position = targetCenter + iso.offset;
            }
            else
            {
                Vector3 offset = cf != null ? cf.offset : new Vector3(0f, 11f, -9f);
                mainCam.transform.position = TutorialPipelineManager.POS_MADRIGUERA_INICIO + offset;
                mainCam.transform.rotation = Quaternion.Euler(42.189f, 0f, 0f);
            }
        }

        // 4. Posicionar Mesa de Fabricación (CraftTable) en el Refugio (bloqueada hasta Fase 3)
        CraftingTable mesa = Object.FindAnyObjectByType<CraftingTable>();
        if (mesa != null)
        {
            Undo.RecordObject(mesa.transform, "Posicionar CraftTable");
            Undo.RecordObject(mesa.gameObject, "Bloquear CraftTable Inicial");
            mesa.transform.position = TutorialPipelineManager.POS_MESA_REFUGIO;
            mesa.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            mesa.gameObject.tag = "Untagged";
            manager.mesaCrafteoRefugio = mesa;
        }

        // 5. Posicionar PuertaTrampa (Door_1_Yellow Variant) en la entrada estrecha del Refugio (bloqueada hasta Fase 3)
        PuertaTrampa puerta = Object.FindAnyObjectByType<PuertaTrampa>();
        if (puerta != null)
        {
            Transform rootDoor = puerta.transform.root;
            Undo.RecordObject(rootDoor, "Posicionar PuertaTrampa");
            Undo.RecordObject(puerta, "Resetear PuertaTrampa");
            Undo.RecordObject(puerta.gameObject, "Bloquear PuertaTrampa Inicial");
            rootDoor.position = TutorialPipelineManager.POS_PUERTA_TRAMPA_REF;
            rootDoor.rotation = Quaternion.Euler(0f, 0f, 0f);
            puerta.trampaActual = TipoTrampa.Ninguna;
            puerta.gameObject.tag = "Untagged";
            manager.puertaTrampaRefugio = puerta;
        }

        // 6. Posicionar a Lulú en la plaza norte de la ciudad (bloqueada hasta paso BuscarLulu)
        LuluNPC lulu = Object.FindAnyObjectByType<LuluNPC>();
        if (lulu != null)
        {
            Undo.RecordObject(lulu.transform, "Posicionar Lulu");
            Undo.RecordObject(lulu.gameObject, "Bloquear Lulu Inicial");
            lulu.transform.position = TutorialPipelineManager.POS_LULU_CIUDAD;
            lulu.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            lulu.gameObject.tag = "Untagged";
            manager.luluNPC = lulu;
        }

        // 7. Posicionar Contenedores de Basura (bloqueados hasta su paso secuencial correspondiente)
        GameObject contBrillo = GameObject.Find("GarbageCollector");
        if (contBrillo != null)
        {
            Undo.RecordObject(contBrillo.transform, "Posicionar Contenedor Resplandeciente");
            Undo.RecordObject(contBrillo, "Bloquear Contenedor Resplandeciente Inicial");
            contBrillo.transform.position = TutorialPipelineManager.POS_CONTENEDOR_BRILLO;
            contBrillo.transform.rotation = Quaternion.identity;
            contBrillo.tag = "Untagged";
        }

        string[] cityLootNames = { "GarbageCollector (4)", "GarbageCollector (5)", "GarbageCollector (8)", "GarbageCollector (2)" };
        Vector3[] cityLootPositions =
        {
            new Vector3(255.0f, 0.35f, 235.0f),
            new Vector3(265.0f, 0.35f, 222.2f),
            new Vector3(275.0f, 0.35f, 235.0f),
            new Vector3(272.5f, 0.35f, 245.0f)
        };
        for (int i = 0; i < cityLootNames.Length; i++)
        {
            GameObject c = GameObject.Find(cityLootNames[i]);
            if (c != null)
            {
                Undo.RecordObject(c.transform, "Posicionar Contenedor Ciudad");
                Undo.RecordObject(c, "Bloquear Contenedor Ciudad Inicial");
                c.transform.position = cityLootPositions[i];
                c.transform.rotation = Quaternion.identity;
                c.tag = "Untagged";
            }
        }

        // 7b. Forzar que el tutorial inicie estrictamente en el primer paso obligatorio
        manager.currentState = TutorialPipelineManager.TutorialState.AprenderControles;
        AlignTutorialUIRectTransforms(manager);

        // 8. Corregir VideoPlayer de Cinematica y ocultar UI durante cinemáticas
        CinematicaManager cinematica = Object.FindAnyObjectByType<CinematicaManager>();
        if (cinematica != null)
        {
            Undo.RecordObject(cinematica.transform, "Posicionar Cinematica");
            Undo.RecordObject(cinematica, "Configurar CinematicaManager");
            cinematica.transform.position = new Vector3(225.0f, 0f, 246.0f);
            Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (mainCanvas != null)
            {
                cinematica.canvasUIRoot = mainCanvas.gameObject;
                Undo.RecordObject(mainCanvas.gameObject, "Ocultar UI Inicial Durante Cinematica");
                mainCanvas.gameObject.SetActive(false);
            }
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
            EditorUtility.SetDirty(cinematica);
        }

        manager.transform.position = new Vector3(225.0f, 0f, 246.0f);

        // 8b. Activar y posicionar las 5 Paredes Cafés del Tutorial (Wall a Wall (4)) para delimitar la ciudad
        ArrangePerimeterWalls();

        // 9. Desactivar objetos rezagados fuera del mapa (Z = -1320 / Z = -300) sin tocar las paredes Wall
        string[] strayObjects =
        {
            "Enemy",
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
    private static void ArrangePerimeterWalls()
    {
        (string name, Vector3 pos, Vector3 scale)[] wallConfigs = new[]
        {
            ("Wall (1)", new Vector3(225.0f, 1.8f, 250.2f),  new Vector3(10.5f, 3.6f, 0.9f)),
            ("Wall (4)", new Vector3(265.0f, 1.8f, 252.5f), new Vector3(11.5f, 3.6f, 0.9f)),
            ("Wall (3)", new Vector3(249.8f, 1.8f, 245.0f), new Vector3(0.9f, 3.6f, 11.5f)),
            ("Wall",     new Vector3(265.0f, 1.8f, 219.8f), new Vector3(11.5f, 3.6f, 0.9f)),
            ("Wall (2)", new Vector3(280.2f, 1.8f, 235.0f), new Vector3(0.9f, 3.6f, 32.0f))
        };

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var cfg in wallConfigs)
        {
            GameObject w = GameObject.Find(cfg.name);
            if (w == null)
            {
                for (int i = 0; i < allTransforms.Length; i++)
                {
                    if (allTransforms[i] != null && allTransforms[i].name == cfg.name && allTransforms[i].gameObject.scene.IsValid())
                    {
                        w = allTransforms[i].gameObject;
                        break;
                    }
                }
            }

            if (w != null)
            {
                Undo.RecordObject(w, "Activar Pared Tutorial");
                Undo.RecordObject(w.transform, "Posicionar Pared Tutorial");
                w.SetActive(true);
                w.transform.position = cfg.pos;
                w.transform.rotation = Quaternion.identity;
                w.transform.localScale = cfg.scale;
            }
        }
    }

    private static void AlignTutorialUIRectTransforms(TutorialPipelineManager manager)
    {
        Canvas canvasComp = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        GameObject uiRoot = canvasComp != null ? canvasComp.gameObject : GameObject.Find("UI");
        if (uiRoot == null) return;

        CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Alinear CanvasScaler UI");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Misions (Texto de misión superior centrado bajo el banner)
        if (manager.textoMisionUI != null)
        {
            RectTransform rtMision = manager.textoMisionUI.GetComponent<RectTransform>();
            if (rtMision != null)
            {
                Undo.RecordObject(rtMision, "Alinear Misions RectTransform");
                rtMision.localScale = Vector3.one;
                rtMision.anchorMin = new Vector2(0.5f, 1f);
                rtMision.anchorMax = new Vector2(0.5f, 1f);
                rtMision.pivot = new Vector2(0.5f, 1f);
                rtMision.anchoredPosition = new Vector2(0f, -108f);
                rtMision.sizeDelta = new Vector2(760f, 54f);
            }
        }

        // Controles WASD (Esquina superior izquierda limpia)
        if (manager.panelControlesWASD != null)
        {
            RectTransform rtControles = manager.panelControlesWASD.GetComponent<RectTransform>();
            if (rtControles != null)
            {
                Undo.RecordObject(rtControles, "Alinear Controles RectTransform");
                rtControles.localScale = Vector3.one;
                rtControles.anchorMin = new Vector2(0f, 1f);
                rtControles.anchorMax = new Vector2(0f, 1f);
                rtControles.pivot = new Vector2(0f, 1f);
                rtControles.anchoredPosition = new Vector2(16f, -12f);
                rtControles.sizeDelta = new Vector2(270f, 190f);

                float[] offsetsY = { 68f, 34f, 0f, -34f, -68f };
                int childIdx = 0;
                foreach (Transform child in rtControles)
                {
                    RectTransform rtChild = child as RectTransform;
                    if (rtChild != null && childIdx < offsetsY.Length)
                    {
                        Undo.RecordObject(rtChild, "Alinear Texto Controles");
                        rtChild.localScale = Vector3.one;
                        rtChild.anchorMin = new Vector2(0.5f, 0.5f);
                        rtChild.anchorMax = new Vector2(0.5f, 0.5f);
                        rtChild.pivot = new Vector2(0.5f, 0.5f);
                        rtChild.anchoredPosition = new Vector2(0f, offsetsY[childIdx]);
                        rtChild.sizeDelta = new Vector2(240f, 28f);
                        childIdx++;
                    }
                }
            }
        }

        // StaminaBar (Esquina inferior derecha limpia)
        if (manager.hamster != null && manager.hamster.staminaSlider != null)
        {
            RectTransform rtStamina = manager.hamster.staminaSlider.GetComponent<RectTransform>();
            if (rtStamina != null)
            {
                Undo.RecordObject(rtStamina, "Alinear StaminaBar RectTransform");
                rtStamina.localPosition = Vector3.zero;
                rtStamina.localScale = Vector3.one;
                rtStamina.anchorMin = new Vector2(1f, 0f);
                rtStamina.anchorMax = new Vector2(1f, 0f);
                rtStamina.pivot = new Vector2(1f, 0f);
                rtStamina.anchoredPosition = new Vector2(-24f, 28f);
                rtStamina.sizeDelta = new Vector2(240f, 26f);
            }
        }

        // PanelDialogue (Inferior central limpio)
        if (manager.luluNPC != null && manager.luluNPC.panelDialogo != null)
        {
            RectTransform rtDiag = manager.luluNPC.panelDialogo.GetComponent<RectTransform>();
            if (rtDiag != null)
            {
                Undo.RecordObject(rtDiag, "Alinear PanelDialogue RectTransform");
                rtDiag.localScale = Vector3.one;
                rtDiag.anchorMin = new Vector2(0.5f, 0f);
                rtDiag.anchorMax = new Vector2(0.5f, 0f);
                rtDiag.pivot = new Vector2(0.5f, 0f);
                rtDiag.anchoredPosition = new Vector2(0f, 92f);
                rtDiag.sizeDelta = new Vector2(780f, 150f);
            }
        }

        // CraftingTable UI Panel (Modal central con botones alineados verticalmente)
        if (manager.mesaCrafteoRefugio != null && manager.mesaCrafteoRefugio.craftingPanelUI != null)
        {
            RectTransform rtCraft = manager.mesaCrafteoRefugio.craftingPanelUI.GetComponent<RectTransform>();
            if (rtCraft != null)
            {
                Undo.RecordObject(rtCraft, "Alinear CraftingTable Panel RectTransform");
                rtCraft.localScale = Vector3.one;
                rtCraft.anchorMin = new Vector2(0.5f, 0.5f);
                rtCraft.anchorMax = new Vector2(0.5f, 0.5f);
                rtCraft.pivot = new Vector2(0.5f, 0.5f);
                rtCraft.anchoredPosition = new Vector2(0f, -20f);
                rtCraft.sizeDelta = new Vector2(680f, 360f);

                float[] btnY = { 55f, -15f, -85f };
                int bIdx = 0;
                foreach (Transform child in rtCraft)
                {
                    RectTransform rtChild = child as RectTransform;
                    if (rtChild == null) continue;
                    Undo.RecordObject(rtChild, "Alinear Hijo CraftingTable");
                    rtChild.localScale = Vector3.one;

                    if (child.GetComponent<Button>() != null && bIdx < btnY.Length)
                    {
                        rtChild.anchorMin = new Vector2(0.5f, 0.5f);
                        rtChild.anchorMax = new Vector2(0.5f, 0.5f);
                        rtChild.pivot = new Vector2(0.5f, 0.5f);
                        rtChild.anchoredPosition = new Vector2(0f, btnY[bIdx]);
                        rtChild.sizeDelta = new Vector2(420f, 52f);
                        bIdx++;

                        foreach (Transform grandChild in rtChild)
                        {
                            RectTransform rtGrand = grandChild as RectTransform;
                            if (rtGrand != null)
                            {
                                Undo.RecordObject(rtGrand, "Alinear Texto Boton Crafting");
                                rtGrand.localScale = Vector3.one;
                                rtGrand.anchorMin = Vector2.zero;
                                rtGrand.anchorMax = Vector2.one;
                                rtGrand.pivot = new Vector2(0.5f, 0.5f);
                                rtGrand.anchoredPosition = Vector2.zero;
                                rtGrand.sizeDelta = new Vector2(-16f, -8f);
                            }
                        }
                    }
                    else
                    {
                        rtChild.anchorMin = new Vector2(0.5f, 1f);
                        rtChild.anchorMax = new Vector2(0.5f, 1f);
                        rtChild.pivot = new Vector2(0.5f, 1f);
                        rtChild.anchoredPosition = new Vector2(0f, -32f);
                        rtChild.sizeDelta = new Vector2(560f, 48f);
                    }
                }
            }
        }
    }
}
#endif