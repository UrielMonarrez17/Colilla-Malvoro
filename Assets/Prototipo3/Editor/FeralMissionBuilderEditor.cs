using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Prototipo3;
using Prototipo3.UI;

namespace Prototipo3.Editor
{
    /// <summary>
    /// Auto-Builder de Editor para Prototipo 3 ("Feral").
    /// Menú: Feral > Configurar Historia, Prefabs y UI Completa
    /// Construye todos los Prefabs de Edificios Ancla (4), Personajes/Jefes (15) con VisualMesh_ReplaceMe,
    /// Trampas Funcionales (3), y vincula automáticamente la escena Assets/Prototipo3/Prototipo3.unity.
    /// </summary>
    [InitializeOnLoad]
    public static class FeralMissionBuilderEditor
    {
        private const string StoryPrefabsFolder = "Assets/Prototipo3/Prefabs/Story";
        private const string ScenePath = "Assets/Prototipo3/Prototipo3.unity";
        private const string CutscenesFolder = "Assets/Prototipo3/StreamingAssets/Cutscenes";

        static FeralMissionBuilderEditor()
        {
            EditorApplication.delayCall += AutoBuildIfMissing;
        }

        private static void AutoBuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            string checkPath = $"{StoryPrefabsFolder}/Prefab_EscuelaBellavista.prefab";
            if (!File.Exists(checkPath))
            {
                BuildAllStoryPrefabsAndConfigureScene(interactive: false);
            }
        }

        [MenuItem("Feral/Configurar Historia, Prefabs y UI Completa", priority = 1)]
        public static void RunFromMenu()
        {
            BuildAllStoryPrefabsAndConfigureScene(interactive: true);
        }

        public static void BuildAllStoryPrefabsAndConfigureScene(bool interactive)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FeralMissionBuilderEditor] Sal del modo Play antes de ejecutar el constructor.");
                return;
            }

            EnsureFoldersExist();

            // 1. Generar los 4 Prefabs de Edificios Importantes (Anclas de Distancia Manhattan)
            GameObject prefabEscuela = CreateLandmarkBuildingPrefab(
                "Prefab_EscuelaBellavista",
                "ESCUELA SECUNDARIA BELLAVISTA\n[Rama A · Rector H. Morales & Toby]",
                new Color(0.55f, 0.25f, 0.20f),
                new Vector3(9f, 5.5f, 9f));

            GameObject prefabBosque = CreateLandmarkBuildingPrefab(
                "Prefab_BosqueCeniza",
                "BOSQUE CENIZA - SUBESTACIÓN DEL ARROYO\n[Rama B · Sultán & Moka]",
                new Color(0.20f, 0.42f, 0.26f),
                new Vector3(9.5f, 4.2f, 9.5f));

            GameObject prefabHospital = CreateLandmarkBuildingPrefab(
                "Prefab_HospitalSanJudas",
                "HOSPITAL GENERAL SAN JUDAS\n[Convergencia C · Sofía, Dr. Salgado & Clara Valdés]",
                new Color(0.78f, 0.84f, 0.88f),
                new Vector3(9.5f, 6.5f, 9.5f));

            GameObject prefabTorre = CreateLandmarkBuildingPrefab(
                "Prefab_TorreAethelgard",
                "TORRE AETHELGARD - NÚCLEO SOLAR\n[Final D · Dra. Elena, Vane & Dr. Julián Kross]",
                new Color(0.18f, 0.22f, 0.34f),
                new Vector3(10f, 12f, 10f));

            // 2. Generar los 15 Prefabs de NPCs, Enemigos y Jefes con su contenedor hijo "VisualMesh_ReplaceMe"
            GameObject prefabElena = CreateCharacterPrefab(
                "NPC_DraElena_Nutria",
                "Elena",
                "Dra. Elena Vargas (Nutria)",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Kitty_001.prefab",
                new Color(0.25f, 0.85f, 0.95f),
                0.75f,
                "\"Con la Enzima Base, el Filtro Biológico y el Oxígeno Puro sintetizaremos el Suero Alba-Puro.\"");

            GameObject prefabSofia = CreateCharacterPrefab(
                "NPC_Sofia_Conejita",
                "Sofia",
                "Sofía (Conejita Albina)",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Kitty_001.prefab",
                new Color(0.96f, 0.96f, 1.0f),
                0.55f,
                "\"¿Kilo...? Sabía que vendrías por mí...\"");

            GameObject prefabMarco = CreateCharacterPrefab(
                "Boss_Marco_Tejon",
                "Marco",
                "Marco (Tejón Conserje)",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.Marco_Tejon,
                "Assets/Animals/Animals_FREE/Prefabs/Dog_001.prefab",
                new Color(0.45f, 0.38f, 0.32f),
                1.0f,
                "\"¡Hola, Kilo! Recoge madera y metal en los contenedores y fabrica una herramienta en tu mesa antes de que anochezca.\"");

            GameObject prefabToby = CreateCharacterPrefab(
                "NPC_Toby_Mapache",
                "Toby",
                "Toby (Mapache del Taller)",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Kitty_001.prefab",
                new Color(0.35f, 0.78f, 1.0f),
                0.75f,
                "\"Usa el Señuelo Acústico [1] para distraer los oídos del Rector Morales.\"");

            GameObject prefabPerroPatio = CreateCharacterPrefab(
                "Enemy_PerroGuardian_Patio",
                "PerroPatio",
                "Guardia del Patio (Perro)",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Dog_001.prefab",
                new Color(0.65f, 0.45f, 0.25f),
                0.95f,
                "\"El patio escolar cierra al atardecer. No hagas ruido cerca del edificio principal.\"");

            GameObject prefabMorales = CreateCharacterPrefab(
                "Boss_RectorMorales_Buho",
                "Morales",
                "Rector H. Morales (Búho Real - El Silenciador)",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.Morales_Buho,
                "Assets/Animals/Animals_FREE/Prefabs/Pinguin_001.prefab",
                new Color(0.58f, 0.42f, 0.24f),
                1.25f,
                "\"Silencio absoluto en los pasillos. Nadie tiene permiso para entrar al laboratorio de química.\"");

            GameObject prefabMoka = CreateCharacterPrefab(
                "NPC_Moka_Zarigueya",
                "Moka",
                "Moka (Zarigüeya Exploradora)",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Kitty_001.prefab",
                new Color(0.45f, 0.95f, 0.55f),
                0.72f,
                "\"¡Toma estas semillas de polen! Con una Bomba de Polen [2] el lobo Sultán no podrá olerte.\"");

            GameObject prefabCuervo = CreateCharacterPrefab(
                "Enemy_CuervoHollin",
                "CuervoHollin",
                "Cuervo de Hollín",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Chicken_001.prefab",
                new Color(0.22f, 0.22f, 0.26f),
                0.9f,
                "\"¡Craa! El arroyo huele a resina quemada y cables rotos...\"");

            GameObject prefabSultan = CreateCharacterPrefab(
                "Boss_Sultan_LoboGris",
                "Sultan",
                "Sultán (Lobo Gris - Colmillo de Brasa)",
                CharacterRoleType.PacifiableBoss,
                BossProfileType.Sultan_Lobo,
                "Assets/Animals/Animals_FREE/Prefabs/Tiger_001.prefab",
                new Color(0.52f, 0.56f, 0.62f),
                1.2f,
                "\"Ayúdame a reparar los 3 postes reflectores del arroyo antes de que caiga la noche.\"");

            GameObject prefabSalgado = CreateCharacterPrefab(
                "NPC_DrSalgado_Tortuga",
                "Salgado",
                "Dr. Arturo Salgado (Tortuga Pediatra)",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Deer_001.prefab",
                new Color(0.35f, 0.82f, 0.62f),
                0.85f,
                "\"Sofía resiste en la Habitación 304. Necesitamos el Cilindro de Oxígeno Puro del quirófano.\"");

            GameObject prefabCamillero = CreateCharacterPrefab(
                "Enemy_CamilleroJabali",
                "CamilleroJabali",
                "Camillero del Turno Nocturno (Jabalí)",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Horse_001.prefab",
                new Color(0.48f, 0.34f, 0.28f),
                0.85f,
                "\"Las tuberías del quirófano tienen fugas de gas anestésico cada noche.\"");

            GameObject prefabClara = CreateCharacterPrefab(
                "Boss_ClaraValdes_CisneNegro",
                "Clara",
                "Clara Valdés (Cisne Negro - La Dama de Gas)",
                CharacterRoleType.PacifiableBoss,
                BossProfileType.Clara_Cisne,
                "Assets/Animals/Animals_FREE/Prefabs/Pinguin_001.prefab",
                new Color(0.15f, 0.15f, 0.20f),
                1.22f,
                "\"La pequeña Sofía de la habitación 304 es muy valiente... Temo lo que me ocurre cuando se apagan las luces.\"");

            GameObject prefabVane = CreateCharacterPrefab(
                "NPC_AgenteVane_ZorroArtico",
                "Vane",
                "Agente Vane (Zorro Ártico Desertor)",
                CharacterRoleType.PureAlly,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Dog_001.prefab",
                new Color(0.85f, 0.92f, 1.0f),
                0.88f,
                "\"Desactivé las cerraduras hacia la celda de la Dra. Elena Vargas. Usa el Descargador Estático [3] contra los condensadores de Kross.\"");

            GameObject prefabHuron = CreateCharacterPrefab(
                "Enemy_Huron_ECLIPSE",
                "HuronEclipse",
                "Centinela Hurón ECLIPSE",
                CharacterRoleType.DualCitizenFeral,
                BossProfileType.None,
                "Assets/Animals/Animals_FREE/Prefabs/Dog_001.prefab",
                new Color(0.28f, 0.32f, 0.42f),
                0.82f,
                "\"Sector restringido de Aethelgard. Circule.\"");

            GameObject prefabKross = CreateCharacterPrefab(
                "Boss_DrJulianKross_Murcielago",
                "Kross",
                "Dr. Julián Kross (Murciélago Albino - El Heraldo Solar)",
                CharacterRoleType.PacifiableBoss,
                BossProfileType.Kross_Murcielago,
                "Assets/Animals/Animals_FREE/Prefabs/Tiger_001.prefab",
                new Color(0.92f, 0.85f, 0.98f),
                1.35f,
                "\"¡La evolución feral de Solis es inevitable! Mis Condensadores Fotónicos son invulnerables.\"");

            // 3. Generar los 3 Prefabs de Trampas Funcionales con su contenedor hijo "VisualMesh_ReplaceMe"
            GameObject prefabTrampa1 = CreateTrapPrefab(
                "Prefab_Trampa1_SenueloAcustico",
                typeof(AcousticDecoyObject),
                PrimitiveType.Cylinder,
                new Vector3(0.45f, 0.35f, 0.45f),
                new Color(0.25f, 0.9f, 1f));

            GameObject prefabTrampa2 = CreateTrapPrefab(
                "Prefab_Trampa2_BombaPolen",
                typeof(PollenCloudEffect),
                PrimitiveType.Sphere,
                new Vector3(0.55f, 0.55f, 0.55f),
                new Color(0.95f, 0.92f, 0.25f));

            GameObject prefabTrampa3 = CreateTrapPrefab(
                "Prefab_Trampa3_DescargadorEstatico",
                typeof(StaticDischargeEffect),
                PrimitiveType.Sphere,
                new Vector3(0.5f, 0.5f, 0.5f),
                new Color(0.35f, 0.65f, 1f));

            // 4. Configurar la escena Assets/Prototipo3/Prototipo3.unity
            ConfigurePrototipo3Scene(
                prefabEscuela, prefabBosque, prefabHospital, prefabTorre,
                prefabMarco,
                prefabTrampa1, prefabTrampa2, prefabTrampa3);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PrintDeliverableReport();

            if (interactive && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Feral - Prototipo 3 Configurado",
                    "Se han generado los 4 Prefabs de Edificios Ancla, los 15 Prefabs de Personajes/Jefes (con contenedor VisualMesh_ReplaceMe), los 3 Prefabs de Trampas Funcionales y se ha configurado la escena Prototipo3.unity.\n\nRevisa la Consola de Unity para ver la guía detallada de reemplazo de mallas 3D y cinemáticas MP4.",
                    "¡Entendido!");
            }
        }

        private static void EnsureFoldersExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prototipo3/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Prototipo3", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(StoryPrefabsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prototipo3/Prefabs", "Story");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prototipo3/StreamingAssets"))
            {
                AssetDatabase.CreateFolder("Assets/Prototipo3", "StreamingAssets");
            }
            if (!AssetDatabase.IsValidFolder(CutscenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prototipo3/StreamingAssets", "Cutscenes");
            }
        }

        private static GameObject CreateLandmarkBuildingPrefab(string prefabName, string signText, Color mainColor, Vector3 dimensions)
        {
            string path = $"{StoryPrefabsFolder}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);

            GameObject visualReplace = new GameObject("VisualMesh_ReplaceMe");
            visualReplace.transform.SetParent(root.transform, false);

            GameObject mainHall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainHall.name = "MainBuildingStructure";
            mainHall.transform.SetParent(visualReplace.transform, false);
            mainHall.transform.localPosition = new Vector3(0f, dimensions.y * 0.5f, -1.8f);
            mainHall.transform.localScale = new Vector3(dimensions.x, dimensions.y, dimensions.z * 0.55f);
            AssignColoredMaterial(mainHall.GetComponent<Renderer>(), mainColor, false);

            GameObject entrance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entrance.name = "EntranceCanopy";
            entrance.transform.SetParent(visualReplace.transform, false);
            entrance.transform.localPosition = new Vector3(0f, 2.6f, 1.4f);
            entrance.transform.localScale = new Vector3(4.2f, 0.35f, 2.4f);
            AssignColoredMaterial(entrance.GetComponent<Renderer>(), mainColor * 1.25f, true);

            GameObject beaconLightObj = new GameObject("LandmarkBeaconLight");
            beaconLightObj.transform.SetParent(root.transform, false);
            beaconLightObj.transform.localPosition = new Vector3(0f, 3.8f, 2.2f);
            Light beacon = beaconLightObj.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.range = 16f;
            beacon.intensity = 2.6f;
            beacon.color = new Color(1f, 0.92f, 0.72f);

            GameObject signObj = new GameObject("BuildingSign_TMP");
            signObj.transform.SetParent(root.transform, false);
            signObj.transform.localPosition = new Vector3(0f, dimensions.y + 1.1f, 0f);
            TMPro.TextMeshPro tmp = signObj.AddComponent<TMPro.TextMeshPro>();
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMPro.TMP_Settings.defaultFontAsset;
            }
            tmp.text = signText;
            tmp.fontSize = 3.8f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.22f;
            tmp.outlineColor = Color.black;

            MeshRenderer mr = signObj.GetComponent<MeshRenderer>();
            if (mr != null && TMPro.TMP_Settings.defaultFontAsset != null)
            {
                mr.sharedMaterial = TMPro.TMP_Settings.defaultFontAsset.material;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreateCharacterPrefab(
            string prefabName,
            string characterId,
            string displayName,
            CharacterRoleType roleType,
            BossProfileType bossProfile,
            string animalAssetPath,
            Color fallbackTint,
            float visualScale,
            string defaultDayDialogue)
        {
            string path = $"{StoryPrefabsFolder}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);
            root.tag = "Interactable";

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.65f, 0f);
            col.radius = 0.45f;
            col.height = 1.3f;

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 260f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.6f;
            agent.radius = 0.4f;
            agent.height = 1.3f;

            EnemyAIPrototipo3 enemyAI = root.AddComponent<EnemyAIPrototipo3>();
            enemyAI.enabled = false;

            DualRoleCharacter dual = root.AddComponent<DualRoleCharacter>();
            dual.characterId = characterId;
            dual.displayName = displayName;
            dual.roleType = roleType;
            dual.bossProfile = bossProfile;
            dual.dayDialogue = defaultDayDialogue;

            // Contenedor hijo requerido para reemplazo limpio de modelos 3D: "VisualMesh_ReplaceMe"
            GameObject visualContainer = new GameObject("VisualMesh_ReplaceMe");
            visualContainer.transform.SetParent(root.transform, false);
            visualContainer.transform.localPosition = Vector3.zero;
            visualContainer.transform.localScale = Vector3.one * visualScale;

            GameObject animalSource = AssetDatabase.LoadAssetAtPath<GameObject>(animalAssetPath);
            if (animalSource != null)
            {
                GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(animalSource, visualContainer.transform);
                modelInstance.name = "AnimalMesh";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;

                foreach (MonoBehaviour mb in modelInstance.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    string n = mb.GetType().Name;
                    if (n == "MovePlayerInput" || n == "CreatureMover" || n == "AnimationShowcase")
                    {
                        UnityEngine.Object.DestroyImmediate(mb);
                    }
                }
                foreach (CharacterController cc in modelInstance.GetComponentsInChildren<CharacterController>(true))
                {
                    UnityEngine.Object.DestroyImmediate(cc);
                }
                foreach (Collider childCol in modelInstance.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(childCol);
                }
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "PlaceholderMesh";
                capsule.transform.SetParent(visualContainer.transform, false);
                capsule.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                capsule.transform.localScale = new Vector3(0.65f, 0.7f, 0.65f);
                Collider c = capsule.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
                AssignColoredMaterial(capsule.GetComponent<Renderer>(), fallbackTint, false);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreateTrapPrefab(
            string prefabName,
            Type trapComponentType,
            PrimitiveType primitiveType,
            Vector3 visualScale,
            Color emissiveColor)
        {
            string path = $"{StoryPrefabsFolder}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);
            root.AddComponent(trapComponentType);

            GameObject visualContainer = new GameObject("VisualMesh_ReplaceMe");
            visualContainer.transform.SetParent(root.transform, false);

            GameObject mesh = GameObject.CreatePrimitive(primitiveType);
            mesh.name = "TrapPlaceholderMesh";
            mesh.transform.SetParent(visualContainer.transform, false);
            mesh.transform.localPosition = new Vector3(0f, visualScale.y * 0.5f, 0f);
            mesh.transform.localScale = visualScale;

            Collider col = mesh.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            AssignColoredMaterial(mesh.GetComponent<Renderer>(), emissiveColor, true);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void AssignColoredMaterial(Renderer renderer, Color color, bool emissive)
        {
            if (renderer == null) return;
            string materialsFolder = "Assets/Prototipo3/Materials";
            if (!AssetDatabase.IsValidFolder(materialsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prototipo3"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prototipo3");
                }
                AssetDatabase.CreateFolder("Assets/Prototipo3", "Materials");
            }

            string hexColor = ColorUtility.ToHtmlStringRGBA(color);
            string matName = $"Mat_{hexColor}_{(emissive ? "Emissive" : "Solid")}";
            string assetPath = $"{materialsFolder}/{matName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                mat = new Material(shader);
                mat.name = matName;
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (emissive && mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 1.8f);
                }
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            renderer.sharedMaterial = mat;
        }

        private static void ConfigurePrototipo3Scene(
            GameObject prefabEscuela,
            GameObject prefabBosque,
            GameObject prefabHospital,
            GameObject prefabTorre,
            GameObject prefabMarco,
            GameObject prefabTrampa1,
            GameObject prefabTrampa2,
            GameObject prefabTrampa3)
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogWarning($"[FeralMissionBuilderEditor] No se encontró la escena en {ScenePath}.");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            bool openedTarget = activeScene.path == ScenePath;
            var scene = openedTarget ? activeScene : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CityGeneratorPrototipo3 cityGen = UnityEngine.Object.FindAnyObjectByType<CityGeneratorPrototipo3>();
            if (cityGen != null)
            {
                cityGen.prefabEscuelaBellavista = prefabEscuela;
                cityGen.prefabBosqueCeniza = prefabBosque;
                cityGen.prefabHospitalSanJudas = prefabHospital;
                cityGen.prefabTorreAethelgard = prefabTorre;
                cityGen.prefabMarcoTejon = prefabMarco;

                // Limpiar hijos residuales guardados en modo edición dentro de cityManager
                for (int i = cityGen.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = cityGen.transform.GetChild(i);
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                EditorUtility.SetDirty(cityGen);
            }

            // Separar responsabilidades en GameObjects dedicados (SOLID)
            // 1. Spawners de Población y Loot
            GameObject popObj = GameObject.Find("CityPopulationManager");
            if (popObj == null) popObj = new GameObject("CityPopulationManager");
            for (int i = popObj.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(popObj.transform.GetChild(i).gameObject);
            }
            CityLootSpawner lootSpawner = popObj.GetComponent<CityLootSpawner>() ?? popObj.AddComponent<CityLootSpawner>();
            CityEnemyPopulationSpawner enemySpawner = popObj.GetComponent<CityEnemyPopulationSpawner>() ?? popObj.AddComponent<CityEnemyPopulationSpawner>();
            EditorUtility.SetDirty(popObj);

            // 2. StoryManager
            StoryManagerPrototipo3 storyMgr = UnityEngine.Object.FindAnyObjectByType<StoryManagerPrototipo3>();
            if (storyMgr == null)
            {
                GameObject storyObj = GameObject.Find("StoryManager_Prototipo3") ?? new GameObject("StoryManager_Prototipo3");
                storyMgr = storyObj.AddComponent<StoryManagerPrototipo3>();
            }

            // 3. StoryWorldBinder
            GameObject binderObj = GameObject.Find("StoryWorldBinder_Prototipo3");
            if (binderObj == null) binderObj = new GameObject("StoryWorldBinder_Prototipo3");
            StoryWorldBinder binder = binderObj.GetComponent<StoryWorldBinder>() ?? binderObj.AddComponent<StoryWorldBinder>();
            binder.luluPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Lulu_Prototipo3.prefab");
            storyMgr.worldBinder = binder;
            EditorUtility.SetDirty(binder);

            if (cityGen != null && cityGen.gameObject != binderObj)
            {
                var dupBinder = cityGen.GetComponent<StoryWorldBinder>();
                if (dupBinder != null) UnityEngine.Object.DestroyImmediate(dupBinder);
            }

            // 4. TrapSystem
            GameObject trapObj = GameObject.Find("TrapSystem_Prototipo3");
            if (trapObj == null) trapObj = new GameObject("TrapSystem_Prototipo3");
            TrapSystemPrototipo3 trapSys = trapObj.GetComponent<TrapSystemPrototipo3>() ?? trapObj.AddComponent<TrapSystemPrototipo3>();
            trapSys.acousticDecoyPrefab = prefabTrampa1;
            trapSys.pollenCloudPrefab = prefabTrampa2;
            trapSys.staticDischargePrefab = prefabTrampa3;
            EditorUtility.SetDirty(trapSys);
            if (cityGen != null && cityGen.gameObject != trapObj)
            {
                var dupTrap = cityGen.GetComponent<TrapSystemPrototipo3>();
                if (dupTrap != null) UnityEngine.Object.DestroyImmediate(dupTrap);
            }

            // 5. CutsceneManager
            GameObject cutsceneObj = GameObject.Find("CutsceneManager_Prototipo3");
            if (cutsceneObj == null) cutsceneObj = new GameObject("CutsceneManager_Prototipo3");
            CutsceneManager cutsceneMgr = cutsceneObj.GetComponent<CutsceneManager>() ?? cutsceneObj.AddComponent<CutsceneManager>();
            EditorUtility.SetDirty(cutsceneObj);
            if (cityGen != null && cityGen.gameObject != cutsceneObj)
            {
                var dupCut = cityGen.GetComponent<CutsceneManager>();
                if (dupCut != null) UnityEngine.Object.DestroyImmediate(dupCut);
                var vps = cityGen.GetComponents<UnityEngine.Video.VideoPlayer>();
                foreach (var vp in vps) UnityEngine.Object.DestroyImmediate(vp);
            }

            // 6. RespawnManager
            GameObject respawnObj = GameObject.Find("RespawnManager_Prototipo3");
            if (respawnObj == null) respawnObj = new GameObject("RespawnManager_Prototipo3");
            RespawnManagerPrototipo3 respawnMgr = respawnObj.GetComponent<RespawnManagerPrototipo3>() ?? respawnObj.AddComponent<RespawnManagerPrototipo3>();
            EditorUtility.SetDirty(respawnObj);
            if (cityGen != null && cityGen.gameObject != respawnObj)
            {
                var dupRespawn = cityGen.GetComponent<RespawnManagerPrototipo3>();
                if (dupRespawn != null) UnityEngine.Object.DestroyImmediate(dupRespawn);
            }

            HamsterControllerPrototipo3 hc3 = UnityEngine.Object.FindAnyObjectByType<HamsterControllerPrototipo3>();
            if (hc3 == null)
            {
                HamsterController legacyHc = UnityEngine.Object.FindAnyObjectByType<HamsterController>();
                if (legacyHc != null)
                {
                    GameObject playerGo = legacyHc.gameObject;
                    float wSpeed = legacyHc.walkSpeed;
                    float sSpeed = legacyHc.sprintSpeed;
                    float cSpeed = legacyHc.crouchSpeed;
                    UnityEngine.Object.DestroyImmediate(legacyHc);
                    hc3 = playerGo.AddComponent<HamsterControllerPrototipo3>();
                    hc3.walkSpeed = wSpeed;
                    hc3.sprintSpeed = sSpeed;
                    hc3.crouchSpeed = cSpeed;
                    EditorUtility.SetDirty(playerGo);
                }
            }

            if (hc3 != null)
            {
                storyMgr.player = hc3.transform;
            }

            MissionPointerPrototipo3 pointer = UnityEngine.Object.FindAnyObjectByType<MissionPointerPrototipo3>();
            if (pointer != null)
            {
                storyMgr.missionPointer = pointer;
                if (hc3 != null) pointer.playerTransform = hc3.transform;
                EditorUtility.SetDirty(pointer);
            }

            EditorUtility.SetDirty(storyMgr);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PrintDeliverableReport()
        {
            Debug.Log(
                "================================================================================\n" +
                "[FERAL - PROTOTIPO 3] REPORTE DE CONFIGURACIÓN AUTOMÁTICA COMPLETADA\n" +
                "================================================================================\n" +
                "1. PREFABS GENERADOS EN Assets/Prototipo3/Prefabs/Story/:\n" +
                "   • Edificios Ancla (4): Prefab_EscuelaBellavista, Prefab_BosqueCeniza, Prefab_HospitalSanJudas, Prefab_TorreAethelgard\n" +
                "   • Personajes y Jefes (15): NPC_DraElena_Nutria, NPC_Sofia_Conejita, Boss_Marco_Tejon, NPC_Toby_Mapache,\n" +
                "     Enemy_PerroGuardian_Patio, Boss_RectorMorales_Buho, NPC_Moka_Zarigueya, Enemy_CuervoHollin,\n" +
                "     Boss_Sultan_LoboGris, NPC_DrSalgado_Tortuga, Enemy_CamilleroJabali, Boss_ClaraValdes_CisneNegro,\n" +
                "     NPC_AgenteVane_ZorroArtico, Enemy_Huron_ECLIPSE, Boss_DrJulianKross_Murcielago\n" +
                "   • Trampas Funcionales (3): Prefab_Trampa1_SenueloAcustico, Prefab_Trampa2_BombaPolen, Prefab_Trampa3_DescargadorEstatico\n" +
                "2. CÓMO REEMPLAZAR MODELOS 3D (VisualMesh_ReplaceMe):\n" +
                "   • Abre cualquier Prefab en Assets/Prototipo3/Prefabs/Story/, expande el objeto hijo 'VisualMesh_ReplaceMe',\n" +
                "     arrastra tu FBX/Prefab definitivo dentro de 'VisualMesh_ReplaceMe' y desactiva la malla temporal.\n" +
                "3. CINEMÁTICAS MP4 EN Assets/Prototipo3/StreamingAssets/Cutscenes/:\n" +
                "   • 01_Intro_Sofia.mp4, 02_Marco_Feral.mp4, 03_Toby_Escuela.mp4, 04_Buho_Apagon.mp4,\n" +
                "     05_Sultan_LuzFria.mp4, 06_Hospital_Kross.mp4, 07_Final_Amanecer.mp4\n" +
                "================================================================================");
        }
    }
}
