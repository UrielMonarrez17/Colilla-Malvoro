using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Prototipo3.Editor
{
    /// <summary>
    /// Constructor integral V2 en Editor para Prototipo 3:
    /// - Fuerza importación síncrona de todos los FBX de Blender y PolyHaven antes de ensamblar escenas.
    /// - Configura personajes/jefes corregidos hacia +Z (sin correr hacia atrás) y colisión exacta de Kilo al ras de y=0 (sin flotar).
    /// - Construye Escena 1 de Escuela ampliada (54m x 38m: Vestíbulo, Pasillos, 2 Salones de clases, Laboratorio y Oficina con Papeles).
    /// - Reconstruye Escena 2 del Gimnasio de Básquetbol con losa sólida, duela PBR, líneas, canastas, marcador, gradas, bancas y reflectores UV.
    /// </summary>
    [InitializeOnLoad]
    public static class Prototipo3StoryAndSchoolBuilder
    {
        private const string BuildStampAssetPath = "Assets/Prototipo3/Resources/StorySchoolBuildStamp_v6.txt";

        private const string CharModelsDir = "Assets/Prototipo3/Models/Characters";
        private const string WeapModelsDir = "Assets/Prototipo3/Models/Weapons";
        private const string SchoolModelsDir = "Assets/Prototipo3/Models/School";

        private const string StoryMatsDir = "Assets/Prototipo3/Materials/Story";
        private const string SchoolMatsDir = "Assets/Prototipo3/Materials/School";
        private const string SchoolTexDir = "Assets/Prototipo3/Materials/School/Textures";

        private const string ResCharsDir = "Assets/Prototipo3/Resources/Characters";
        private const string ResWeapsDir = "Assets/Prototipo3/Resources/Weapons";
        private const string ResPropsDir = "Assets/Prototipo3/Resources/Props";
        private const string ScenesDir = "Assets/Prototipo3/Scenes";

        static Prototipo3StoryAndSchoolBuilder()
        {
            EditorApplication.delayCall += TryAutoBuildOnce;
        }

        private static void TryAutoBuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (File.Exists(BuildStampAssetPath) &&
                File.Exists(StoryDirectorPrototipo3.SchoolEscapeScenePath) &&
                File.Exists(StoryDirectorPrototipo3.SchoolBasketballScenePath))
            {
                return;
            }

            Debug.Log("[Prototipo3StoryAndSchoolBuilder] Ejecutando construcción integral V2 (Jefes +Z, Escuela Ampliada y Gimnasio Reconstruido)...");
            BuildAllStoryAssetsAndScenes();
        }

        [MenuItem("Feral/Prototipo 3/Construir Historia Completa, Jefes y Escenas de Escuela")]
        public static void BuildAllStoryAssetsAndScenes()
        {
            EnsureDir(StoryMatsDir);
            EnsureDir(SchoolMatsDir);
            EnsureDir(ResCharsDir);
            EnsureDir(ResWeapsDir);
            EnsureDir(ResPropsDir);
            EnsureDir(ScenesDir);

            // CRÍTICO: Forzar importación síncrona inicial para que ningún FBX devuelva null al construir escenas
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string previousOpenScene = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.GetActiveScene().isDirty && !string.IsNullOrEmpty(previousOpenScene))
            {
                EditorSceneManager.SaveOpenScenes();
            }

            ConfigurePortraitTextures();
            ConfigurePolyHavenNormalMaps();

            Dictionary<string, Material> allMats = CreateOrUpdateAllStoryAndSchoolMaterials();

            string[] charFbxs =
            {
                $"{CharModelsDir}/Marco_Tejon_Day.fbx",
                $"{CharModelsDir}/Marco_Tejon_Feral.fbx",
                $"{CharModelsDir}/Boss_PerroGuardian.fbx",
                $"{CharModelsDir}/NPC_Toby_Mapache.fbx",
                $"{CharModelsDir}/Boss_BuhoRector.fbx",
            };
            foreach (string fbx in charFbxs) ConfigureRiggedCharacterFbx(fbx, allMats);

            string[] staticFbxs =
            {
                $"{WeapModelsDir}/SM_NetLauncherGun.fbx",
                $"{WeapModelsDir}/SM_NetProjectile.fbx",
                $"{WeapModelsDir}/SM_OwlFeatherProjectile.fbx",
                $"{WeapModelsDir}/SM_BadgerDebrisProjectile.fbx",
                $"{WeapModelsDir}/SM_SonicBarkWave.fbx",
                $"{WeapModelsDir}/SM_SchoolHoleEntrance.fbx",
                $"{WeapModelsDir}/SM_TobyWorkshopFacade.fbx",
                $"{SchoolModelsDir}/SM_School_LockerBank.fbx",
                $"{SchoolModelsDir}/SM_School_LabStation.fbx",
                $"{SchoolModelsDir}/SM_School_ChalkboardWall.fbx",
                $"{SchoolModelsDir}/SM_School_StudentDeskRow.fbx",
                $"{SchoolModelsDir}/SM_School_ReceptionCounter.fbx",
                $"{SchoolModelsDir}/SM_School_PrincipalDeskDocs.fbx",
                $"{SchoolModelsDir}/SM_School_TrophyCase.fbx",
                $"{SchoolModelsDir}/SM_School_VendingMachine.fbx",
                $"{SchoolModelsDir}/SM_Gym_BasketballCourt.fbx",
                $"{SchoolModelsDir}/SM_Gym_BasketballHoop.fbx",
                $"{SchoolModelsDir}/SM_Gym_Bleachers.fbx",
                $"{SchoolModelsDir}/SM_Gym_RafterTruss.fbx",
                $"{SchoolModelsDir}/SM_Gym_FloodlightSwitch.fbx",
                $"{SchoolModelsDir}/SM_Gym_Scoreboard.fbx",
                $"{SchoolModelsDir}/SM_Gym_BallCart.fbx",
                $"{SchoolModelsDir}/SM_Gym_TeamBench.fbx",
                $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx",
                $"{SchoolModelsDir}/PH_WoodenBookshelf.fbx",
                $"{SchoolModelsDir}/PH_FluorescentLights.fbx",
                $"{SchoolModelsDir}/PH_CagedGymLight.fbx",
            };
            foreach (string fbx in staticFbxs) ConfigureStaticFbx(fbx, allMats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            RuntimeAnimatorController acMarcoDay = BuildGroundCharacterController("AC_Marco_Day", $"{CharModelsDir}/Marco_Tejon_Day.fbx");
            RuntimeAnimatorController acMarcoFeral = BuildGroundCharacterController("AC_Marco_Feral", $"{CharModelsDir}/Marco_Tejon_Feral.fbx");
            RuntimeAnimatorController acPerro = BuildGroundCharacterController("AC_PerroGuardian", $"{CharModelsDir}/Boss_PerroGuardian.fbx");
            RuntimeAnimatorController acToby = BuildGroundCharacterController("AC_Toby_Mapache", $"{CharModelsDir}/NPC_Toby_Mapache.fbx");
            RuntimeAnimatorController acBuho = BuildOwlBossController("AC_BuhoRector", $"{CharModelsDir}/Boss_BuhoRector.fbx");

            GameObject netGunPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_NetLauncherGun.fbx", $"{ResWeapsDir}/Prefab_NetLauncherGun.prefab", allMats);
            GameObject netProjPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_NetProjectile.fbx", $"{ResWeapsDir}/Prefab_NetProjectile.prefab", allMats);
            GameObject owlFeatherPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_OwlFeatherProjectile.fbx", $"{ResWeapsDir}/Prefab_OwlFeatherProjectile.prefab", allMats);
            GameObject debrisPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_BadgerDebrisProjectile.fbx", $"{ResWeapsDir}/Prefab_BadgerDebrisProjectile.prefab", allMats);
            GameObject sonicWavePrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_SonicBarkWave.fbx", $"{ResWeapsDir}/Prefab_SonicBarkWave.prefab", allMats);
            BuildStaticVisualPrefab($"{WeapModelsDir}/SM_SchoolHoleEntrance.fbx", $"{ResWeapsDir}/Prefab_SchoolHoleEntrance.prefab", allMats);
            BuildStaticVisualPrefab($"{WeapModelsDir}/SM_TobyWorkshopFacade.fbx", $"{ResWeapsDir}/Prefab_TobyWorkshopFacade.prefab", allMats);
            BuildStaticVisualPrefab("Assets/Prototipo3/Models/Props/SM_MissionArrow.fbx", $"{ResPropsDir}/SM_MissionArrow.prefab", allMats);

            BuildNpcVisualPrefab($"{CharModelsDir}/Marco_Tejon_Day.fbx", $"{ResCharsDir}/Prefab_Marco_Day.prefab", acMarcoDay, allMats);
            BuildNpcVisualPrefab($"{CharModelsDir}/NPC_Toby_Mapache.fbx", $"{ResCharsDir}/Prefab_Toby_Mapache.prefab", acToby, allMats);
            BuildMarcoFeralBossPrefab($"{CharModelsDir}/Marco_Tejon_Feral.fbx", $"{ResCharsDir}/Prefab_Marco_Feral.prefab", acMarcoFeral, debrisPrefab, allMats);
            BuildGuardDogBossPrefab($"{CharModelsDir}/Boss_PerroGuardian.fbx", $"{ResCharsDir}/Prefab_Boss_PerroGuardian.prefab", acPerro, sonicWavePrefab, allMats);
            GameObject owlBossPrefab = BuildOwlRectorBossPrefab($"{CharModelsDir}/Boss_BuhoRector.fbx", $"{ResCharsDir}/Prefab_Boss_BuhoRector.prefab", acBuho, owlFeatherPrefab, allMats);

            BuildSchoolInfiltrationScene(owlBossPrefab, allMats);
            BuildSchoolBasketballFinalScene(owlBossPrefab, netGunPrefab, netProjPrefab, allMats);
            RepairSafeRefugePrefabFloorAndTrigger();
            Dictionary<string, GameObject> proto3RoadPrefabs = BuildPrototipo3RoadPrefabsWithPoleSpotLights();
            UpgradePrototipo3CityScenePlayerAndLoot(proto3RoadPrefabs);
            RegisterScenesInBuildSettings();

            File.WriteAllText(BuildStampAssetPath, $"Built V6 at {DateTime.UtcNow:O}\n");
            AssetDatabase.ImportAsset(BuildStampAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (File.Exists(StoryDirectorPrototipo3.CityScenePath))
            {
                EditorSceneManager.OpenScene(StoryDirectorPrototipo3.CityScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[Prototipo3StoryAndSchoolBuilder] ¡RECONSTRUCCIÓN V2 LISTA! Orientaciones +Z, colisiones de Kilo, Escuela grande con salones/papeles y Gimnasio completo verificados.");
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static void ConfigurePortraitTextures()
        {
            string[] uiDirs =
            {
                "Assets/Prototipo3/Resources/Portraits",
                "Assets/Prototipo3/Resources/FeralUI",
                "Assets/Prototipo3/UI/Art"
            };

            foreach (string dir in uiDirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti == null) continue;
                    bool dirty = false;
                    if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }
                    if (ti.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        dirty = true;
                    }
                    if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; dirty = true; }
                    if (ti.maxTextureSize < 2048) { ti.maxTextureSize = 2048; dirty = true; }
                    if (ti.filterMode != FilterMode.Bilinear) { ti.filterMode = FilterMode.Bilinear; dirty = true; }
                    if (dirty) ti.SaveAndReimport();
                }
            }
        }

        private static void RepairSafeRefugePrefabFloorAndTrigger()
        {
            const string refugePrefabPath = "Assets/Prototipo3/Prefabs/Props/Prefab_RefugioSeguro.prefab";
            if (!File.Exists(refugePrefabPath)) return;

            GameObject root = PrefabUtility.LoadPrefabContents(refugePrefabPath);
            if (root == null) return;

            try
            {
                Transform pav = root.transform.Find("Pavement");
                if (pav != null) UnityEngine.Object.DestroyImmediate(pav.gameObject);

                Transform interior = root.transform.Find("InteriorModel");
                if (interior != null)
                {
                    interior.localPosition = Vector3.zero;
                    interior.localRotation = Quaternion.identity;

                    foreach (Transform t in interior.GetComponentsInChildren<Transform>(true))
                    {
                        if (t != null && t.name.IndexOf("Mesh_Hamster", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            UnityEngine.Object.DestroyImmediate(t.gameObject);
                        }
                    }

                    Transform layout = interior.Find("Layout");
                    if (layout != null)
                    {
                        layout.localPosition = Vector3.zero;
                        Transform floorTr = layout.Find("Floor");
                        if (floorTr != null)
                        {
                            Renderer[] floorRens = floorTr.GetComponentsInChildren<Renderer>(true);
                            if (floorRens.Length > 0)
                            {
                                float maxWorldY = float.MinValue;
                                for (int i = 0; i < floorRens.Length; i++)
                                {
                                    if (floorRens[i] != null) maxWorldY = Mathf.Max(maxWorldY, floorRens[i].bounds.max.y);
                                }
                                if (maxWorldY > float.MinValue)
                                {
                                    float targetWorldY = root.transform.position.y + 0.02f;
                                    layout.position += new Vector3(0f, targetWorldY - maxWorldY, 0f);
                                }
                            }
                            MeshCollider planeMc = floorTr.GetComponent<MeshCollider>();
                            if (planeMc != null) planeMc.enabled = false;
                        }
                    }
                }

                Transform solidFloor = root.transform.Find("RefugeSolidFloorCollider");
                if (solidFloor == null)
                {
                    GameObject slabGo = new GameObject("RefugeSolidFloorCollider");
                    slabGo.transform.SetParent(root.transform, false);
                    solidFloor = slabGo.transform;
                }
                solidFloor.localPosition = Vector3.zero;
                solidFloor.localRotation = Quaternion.identity;
                BoxCollider floorBox = solidFloor.GetComponent<BoxCollider>() ?? solidFloor.gameObject.AddComponent<BoxCollider>();
                floorBox.isTrigger = false;
                floorBox.center = new Vector3(0f, -0.23f, 0.05f);
                floorBox.size = new Vector3(8.8f, 0.50f, 8.9f);

                Transform frontWalls = root.transform.Find("FrontFacadeWalls");
                if (frontWalls != null) frontWalls.localPosition = Vector3.zero;

                Transform craftTr = root.transform.Find("Refuge_CraftingTable");
                if (craftTr != null) craftTr.localPosition = new Vector3(-2.3f, 0.03f, 1.4f);

                Transform spawnAnchor = root.transform.Find("PlayerSpawnAnchor");
                if (spawnAnchor != null) spawnAnchor.localPosition = new Vector3(0f, 0.08f, 0.1f);

                BoxCollider safeTrigger = root.GetComponent<BoxCollider>() ?? root.AddComponent<BoxCollider>();
                safeTrigger.isTrigger = true;
                safeTrigger.center = new Vector3(0f, 1.4f, -0.15f);
                safeTrigger.size = new Vector3(9.2f, 3.6f, 9.4f);

                PrefabUtility.SaveAsPrefabAsset(root, refugePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurePolyHavenNormalMaps()
        {
            if (!Directory.Exists(SchoolTexDir)) return;
            foreach (string file in Directory.GetFiles(SchoolTexDir, "*Normal*.*"))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                string assetPath = file.Replace('\\', '/');
                TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
            }
        }

        private static Dictionary<string, Material> CreateOrUpdateAllStoryAndSchoolMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var dict = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

            var colorSpecs = new (string name, Color col, float metal, float smooth, Color? emis)[]
            {
                ("M_Char_BadgerDark", new Color(0.12f, 0.12f, 0.14f), 0f, 0.30f, null),
                ("M_Char_BadgerMid", new Color(0.25f, 0.24f, 0.27f), 0f, 0.32f, null),
                ("M_Char_FurWhite", new Color(0.93f, 0.91f, 0.87f), 0f, 0.35f, null),
                ("M_Char_OverallsBlue", new Color(0.19f, 0.32f, 0.46f), 0f, 0.28f, null),
                ("M_Char_FlannelRed", new Color(0.70f, 0.15f, 0.14f), 0f, 0.30f, null),
                ("M_Char_Brass", new Color(0.86f, 0.65f, 0.22f), 0.82f, 0.68f, null),
                ("M_Char_Steel", new Color(0.50f, 0.54f, 0.59f), 0.80f, 0.64f, null),
                ("M_Char_EyesDark", new Color(0.04f, 0.04f, 0.05f), 0.1f, 0.85f, null),
                ("M_Boss_FeralEyeGlow", new Color(1f, 0.45f, 0.05f), 0f, 0.75f, new Color(1f, 0.42f, 0.04f) * 3.2f),
                ("M_Boss_FeralVeinGlow", new Color(1f, 0.32f, 0.03f), 0f, 0.65f, new Color(1f, 0.30f, 0.02f) * 2.4f),
                ("M_Char_DogBlack", new Color(0.10f, 0.10f, 0.12f), 0f, 0.36f, null),
                ("M_Char_DogTan", new Color(0.68f, 0.40f, 0.18f), 0f, 0.34f, null),
                ("M_Char_NavyVest", new Color(0.11f, 0.17f, 0.30f), 0.1f, 0.42f, null),
                ("M_Boss_HazardOrange", new Color(0.96f, 0.45f, 0.06f), 0.1f, 0.55f, new Color(0.95f, 0.38f, 0.04f) * 0.9f),
                ("M_Char_RaccoonGrey", new Color(0.40f, 0.39f, 0.42f), 0f, 0.32f, null),
                ("M_Char_JacketDark", new Color(0.22f, 0.17f, 0.15f), 0f, 0.35f, null),
                ("M_Char_CyanGlow", new Color(0.15f, 0.88f, 1f), 0.1f, 0.75f, new Color(0.15f, 0.88f, 1f) * 2.6f),
                ("M_Boss_OwlPlumageDark", new Color(0.20f, 0.13f, 0.11f), 0f, 0.32f, null),
                ("M_Boss_OwlChestTan", new Color(0.66f, 0.50f, 0.36f), 0f, 0.34f, null),
                ("M_Boss_SuitBurgundy", new Color(0.32f, 0.08f, 0.12f), 0f, 0.38f, null),
                ("M_Char_LeatherWarm", new Color(0.46f, 0.25f, 0.12f), 0f, 0.42f, null),
                ("M_Prop_BrickSchool", new Color(0.58f, 0.24f, 0.19f), 0f, 0.22f, null),
                ("M_Prop_ConcreteDark", new Color(0.32f, 0.33f, 0.35f), 0f, 0.20f, null),

                ("M_School_LockerTeal", new Color(0.17f, 0.38f, 0.46f), 0.68f, 0.56f, null),
                ("M_School_LockerDark", new Color(0.13f, 0.17f, 0.21f), 0.72f, 0.50f, null),
                ("M_School_CourtLineWhite", new Color(0.96f, 0.96f, 0.94f), 0f, 0.68f, null),
                ("M_School_Burgundy", new Color(0.42f, 0.10f, 0.14f), 0f, 0.58f, null),
                ("M_School_HoopOrange", new Color(0.95f, 0.32f, 0.05f), 0.45f, 0.62f, null),
                ("M_School_BackboardGlass", new Color(0.84f, 0.91f, 0.97f), 0.15f, 0.86f, null),
                ("M_School_ChalkboardGreen", new Color(0.11f, 0.28f, 0.18f), 0f, 0.28f, null),
                ("M_School_HeliosAmberGlow", new Color(1f, 0.50f, 0.06f), 0f, 0.78f, new Color(1f, 0.45f, 0.04f) * 3.0f),
                ("M_School_Steel", new Color(0.50f, 0.54f, 0.58f), 0.82f, 0.64f, null),
                ("M_School_BleacherWood", new Color(0.59f, 0.37f, 0.19f), 0f, 0.46f, null),
                ("M_School_CyanGlow", new Color(0.15f, 0.88f, 1f), 0.1f, 0.75f, new Color(0.15f, 0.88f, 1f) * 2.5f),
            };

            foreach (var s in colorSpecs)
            {
                string folder = s.name.StartsWith("M_School_") ? SchoolMatsDir : StoryMatsDir;
                Material m = CreateOrUpdateUrpMat($"{folder}/{s.name}.mat", urpLit, s.col, s.metal, s.smooth, s.emis);
                dict[s.name] = m;
            }

            dict["M_School_WoodCourt"] = BuildPolyHavenUrpMat("M_School_WoodCourt", "laminate_floor_02", new Color(0.90f, 0.72f, 0.48f), 0.74f, new Vector2(8f, 5f));
            dict["M_PH_BrickWall"] = BuildPolyHavenUrpMat("M_PH_BrickWall", "broken_brick_wall", Color.white, 0.28f, new Vector2(5f, 2f));
            dict["M_PH_PlasterWall"] = BuildPolyHavenUrpMat("M_PH_PlasterWall", "blue_plaster_weathered", Color.white, 0.34f, new Vector2(6f, 2f));
            dict["M_PH_HallwayFloor"] = BuildPolyHavenUrpMat("M_PH_HallwayFloor", "concrete_floor_painted", new Color(0.84f, 0.86f, 0.89f), 0.42f, new Vector2(10f, 8f));

            dict["M_PH_metal_office_desk"] = BuildPolyHavenModelMat("M_PH_metal_office_desk", "metal_office_desk", new Color(0.45f, 0.48f, 0.50f), 0.55f, 0.52f);
            dict["M_PH_wooden_bookshelf_worn"] = BuildPolyHavenModelMat("M_PH_wooden_bookshelf_worn", "wooden_bookshelf_worn", new Color(0.52f, 0.34f, 0.20f), 0f, 0.38f);
            dict["M_PH_mounted_fluorescent_lights"] = CreateOrUpdateUrpMat($"{SchoolMatsDir}/M_PH_mounted_fluorescent_lights.mat", urpLit, new Color(0.92f, 0.95f, 0.98f), 0.3f, 0.7f, new Color(0.95f, 0.92f, 0.82f) * 1.4f);
            dict["M_PH_caged_hanging_light"] = CreateOrUpdateUrpMat($"{SchoolMatsDir}/M_PH_caged_hanging_light.mat", urpLit, new Color(0.85f, 0.80f, 0.70f), 0.5f, 0.6f, new Color(1f, 0.82f, 0.55f) * 1.5f);

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Prototipo3/Materials/Props" }))
            {
                Material pm = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (pm != null && !dict.ContainsKey(pm.name)) dict[pm.name] = pm;
            }

            return dict;
        }

        private static Material CreateOrUpdateUrpMat(string path, Shader urpLit, Color col, float metallic, float smooth, Color? emission)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(urpLit);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != urpLit)
            {
                m.shader = urpLit;
            }

            m.SetColor("_BaseColor", col);
            m.color = col;
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);

            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            else
            {
                m.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material BuildPolyHavenUrpMat(string matName, string phId, Color tint, float smoothness, Vector2 tiling)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Material m = CreateOrUpdateUrpMat($"{SchoolMatsDir}/{matName}.mat", urpLit, tint, 0f, smoothness, null);

            Texture2D albedo = FindTextureWithPrefix($"PH_{phId}_Albedo");
            Texture2D normal = FindTextureWithPrefix($"PH_{phId}_Normal");

            if (albedo != null)
            {
                m.SetTexture("_BaseMap", albedo);
                m.mainTexture = albedo;
                m.SetTextureScale("_BaseMap", tiling);
            }
            if (normal != null)
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", tiling);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material BuildPolyHavenModelMat(string matName, string phId, Color fallbackCol, float metal, float smooth)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Material m = CreateOrUpdateUrpMat($"{SchoolMatsDir}/{matName}.mat", urpLit, fallbackCol, metal, smooth, null);

            if (Directory.Exists(SchoolTexDir))
            {
                foreach (string f in Directory.GetFiles(SchoolTexDir, $"PH_{phId}_*.*"))
                {
                    if (f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    string lower = f.ToLowerInvariant();
                    if (lower.Contains("diff") || lower.Contains("col") || lower.Contains("albedo"))
                    {
                        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/'));
                        if (tex != null)
                        {
                            m.SetColor("_BaseColor", Color.white);
                            m.SetTexture("_BaseMap", tex);
                            break;
                        }
                    }
                }
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D FindTextureWithPrefix(string prefix)
        {
            if (!Directory.Exists(SchoolTexDir)) return null;
            foreach (string f in Directory.GetFiles(SchoolTexDir, prefix + ".*"))
            {
                if (f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(f.Replace('\\', '/'));
                if (t != null) return t;
            }
            return null;
        }

        private static void ConfigureRiggedCharacterFbx(string fbxPath, Dictionary<string, Material> mats)
        {
            if (!File.Exists(fbxPath)) return;
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null) return;

            imp.animationType = ModelImporterAnimationType.Generic;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var kv in mats)
            {
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            }
            imp.SaveAndReimport();

            ModelImporterClipAnimation[] defaults = imp.defaultClipAnimations;
            if (defaults != null && defaults.Length > 0)
            {
                for (int i = 0; i < defaults.Length; i++)
                {
                    bool isOneShot = defaults[i].name.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0;
                    defaults[i].loopTime = !isOneShot;
                }
                imp.clipAnimations = defaults;
                imp.SaveAndReimport();
            }
        }

        private static void ConfigureStaticFbx(string fbxPath, Dictionary<string, Material> mats)
        {
            if (!File.Exists(fbxPath)) return;
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null) return;

            imp.importAnimation = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var kv in mats)
            {
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            }
            imp.SaveAndReimport();
        }

        private static Dictionary<string, AnimationClip> LoadClipsFromFbx(string fbxPath)
        {
            var dict = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var o in objs)
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    dict[clip.name] = clip;
                }
            }
            return dict;
        }

        private static AnimationClip FindClipMatching(Dictionary<string, AnimationClip> clips, string keyword)
        {
            foreach (var kv in clips)
            {
                if (kv.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return kv.Value;
            }
            return clips.Values.FirstOrDefault();
        }

        private static RuntimeAnimatorController BuildGroundCharacterController(string name, string fbxPath)
        {
            string ctrlPath = $"{ResCharsDir}/{name}.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var clips = LoadClipsFromFbx(fbxPath);
            AnimationClip idle = FindClipMatching(clips, "Idle");
            AnimationClip walk = FindClipMatching(clips, "Walk") ?? FindClipMatching(clips, "Talk") ?? idle;
            AnimationClip run = FindClipMatching(clips, "Run") ?? walk;
            AnimationClip attack = FindClipMatching(clips, "Attack");

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState sIdle = sm.AddState("Idle");
            sIdle.motion = idle;
            sm.defaultState = sIdle;

            AnimatorState sMove = sm.AddState("Locomotion");
            sMove.motion = run != null ? run : walk;

            var tToMove = sIdle.AddTransition(sMove);
            tToMove.hasExitTime = false;
            tToMove.duration = 0.12f;
            tToMove.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var tToIdle = sMove.AddTransition(sIdle);
            tToIdle.hasExitTime = false;
            tToIdle.duration = 0.15f;
            tToIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            if (attack != null)
            {
                AnimatorState sAtk = sm.AddState("Attack");
                sAtk.motion = attack;
                var tAnyAtk = sm.AddAnyStateTransition(sAtk);
                tAnyAtk.hasExitTime = false;
                tAnyAtk.duration = 0.08f;
                tAnyAtk.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

                var tBack = sAtk.AddTransition(sIdle);
                tBack.hasExitTime = true;
                tBack.exitTime = 0.90f;
                tBack.duration = 0.12f;
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static RuntimeAnimatorController BuildOwlBossController(string name, string fbxPath)
        {
            string ctrlPath = $"{ResCharsDir}/{name}.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsFlying", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsSwooping", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsStunned", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var clips = LoadClipsFromFbx(fbxPath);
            AnimationClip idle = FindClipMatching(clips, "Idle");
            AnimationClip fly = FindClipMatching(clips, "Fly") ?? idle;
            AnimationClip swoop = FindClipMatching(clips, "Swoop") ?? fly;
            AnimationClip stunned = FindClipMatching(clips, "Stunned") ?? idle;

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState sFly = sm.AddState("Fly");
            sFly.motion = fly;
            sm.defaultState = sFly;

            AnimatorState sIdle = sm.AddState("Perched");
            sIdle.motion = idle;

            AnimatorState sSwoop = sm.AddState("Swoop");
            sSwoop.motion = swoop;

            AnimatorState sStun = sm.AddState("Stunned");
            sStun.motion = stunned;

            var tFlyToSwoop = sFly.AddTransition(sSwoop);
            tFlyToSwoop.hasExitTime = false;
            tFlyToSwoop.duration = 0.1f;
            tFlyToSwoop.AddCondition(AnimatorConditionMode.If, 0f, "IsSwooping");

            var tSwoopToPerch = sSwoop.AddTransition(sIdle);
            tSwoopToPerch.hasExitTime = false;
            tSwoopToPerch.duration = 0.12f;
            tSwoopToPerch.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSwooping");
            tSwoopToPerch.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFlying");

            var tPerchToFly = sIdle.AddTransition(sFly);
            tPerchToFly.hasExitTime = false;
            tPerchToFly.duration = 0.15f;
            tPerchToFly.AddCondition(AnimatorConditionMode.If, 0f, "IsFlying");

            var tAnyStun = sm.AddAnyStateTransition(sStun);
            tAnyStun.hasExitTime = false;
            tAnyStun.canTransitionToSelf = false;
            tAnyStun.duration = 0.1f;
            tAnyStun.AddCondition(AnimatorConditionMode.If, 0f, "IsStunned");

            var tStunToFly = sStun.AddTransition(sFly);
            tStunToFly.hasExitTime = false;
            tStunToFly.duration = 0.15f;
            tStunToFly.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsStunned");
            tStunToFly.AddCondition(AnimatorConditionMode.If, 0f, "IsFlying");

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static void ApplyMappedMaterials(GameObject root, Dictionary<string, Material> mats)
        {
            if (root == null) return;
            Renderer[] rens = root.GetComponentsInChildren<Renderer>(true);
            Material fallback = mats.Values.FirstOrDefault();
            foreach (Renderer r in rens)
            {
                Material[] shared = r.sharedMaterials;
                bool mod = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null && mats.TryGetValue(shared[i].name, out Material mapped))
                    {
                        shared[i] = mapped;
                        mod = true;
                    }
                    else if (shared[i] == null && fallback != null)
                    {
                        shared[i] = fallback;
                        mod = true;
                    }
                }
                if (mod) r.sharedMaterials = shared;
            }
        }

        /// <summary>
        /// Verifica geométricamente que el hueso/cabeza del personaje mire hacia +Z en espacio local de Unity;
        /// si detecta que apunta a -Z, gira el hijo VisualModel 180° automáticamente.
        /// </summary>
        private static void EnsureCharacterFacesPlusZ(GameObject root, GameObject visualChild)
        {
            if (root == null || visualChild == null) return;
            Transform headBone = null;
            foreach (Transform t in visualChild.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("Head", StringComparison.OrdinalIgnoreCase))
                {
                    headBone = t;
                    break;
                }
            }
            if (headBone != null)
            {
                Vector3 localHead = root.transform.InverseTransformPoint(headBone.position);
                // En todos nuestros rigs Head.z (o la cola/hocico respecto a Hips) es mayor hacia el frente (+Z)
                Transform tailBone = visualChild.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name.Equals("Tail", StringComparison.OrdinalIgnoreCase));
                if (tailBone != null)
                {
                    Vector3 localTail = root.transform.InverseTransformPoint(tailBone.position);
                    if (localHead.z < localTail.z)
                    {
                        visualChild.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                }
                else if (localHead.z < -0.02f)
                {
                    visualChild.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }
        }

        private static GameObject BuildStaticVisualPrefab(string fbxPath, string prefabOutPath, Dictionary<string, Material> mats)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) return null;

            // Preservar vis.transform.localRotation (-90° en X de los FBX de Blender) dentro del contenedor raíz!
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabOutPath));
            GameObject vis = PrefabUtility.InstantiatePrefab(src, root.transform) as GameObject ?? UnityEngine.Object.Instantiate(src, root.transform);
            vis.name = "Model";
            vis.transform.localPosition = Vector3.zero;
            ApplyMappedMaterials(vis, mats);

            // Alinear la base inferior en y=0 para fachadas y entradas de suelo
            if (fbxPath.IndexOf("Entrance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fbxPath.IndexOf("Facade", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SnapChildrenBottomToRootY(root, vis);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabOutPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void SnapChildrenBottomToRootY(GameObject pivotRoot, GameObject visualChild)
        {
            Renderer[] rens = visualChild.GetComponentsInChildren<Renderer>(true);
            if (rens.Length == 0) return;
            Bounds b = rens[0].bounds;
            for (int i = 1; i < rens.Length; i++) b.Encapsulate(rens[i].bounds);
            float deltaY = pivotRoot.transform.position.y - b.min.y;
            visualChild.transform.position += new Vector3(0f, deltaY, 0f);
        }

        private static GameObject BuildNpcVisualPrefab(string fbxPath, string prefabOutPath, RuntimeAnimatorController ac, Dictionary<string, Material> mats)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) return null;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabOutPath));
            GameObject vis = PrefabUtility.InstantiatePrefab(src) as GameObject ?? UnityEngine.Object.Instantiate(src);
            vis.name = "RiggedVisual";
            vis.transform.SetParent(root.transform, false);
            ApplyMappedMaterials(vis, mats);
            EnsureCharacterFacesPlusZ(root, vis);

            Animator anim = vis.GetComponentInChildren<Animator>(true) ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ac;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabOutPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildMarcoFeralBossPrefab(string fbxPath, string outPath, RuntimeAnimatorController ac, GameObject debrisPrefab, Dictionary<string, Material> mats)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) return null;

            GameObject root = new GameObject("Prefab_Marco_Feral");
            root.tag = "Enemy";

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.05f, 0f);
            col.radius = 0.65f;
            col.height = 2.1f;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            NavMeshAgent ag = root.AddComponent<NavMeshAgent>();
            ag.radius = 0.62f;
            ag.height = 2.1f;
            ag.speed = 6.9f;
            ag.angularSpeed = 420f;
            ag.acceleration = 18f;

            MarcoFeralBossAI boss = root.AddComponent<MarcoFeralBossAI>();
            boss.debrisProjectilePrefab = debrisPrefab;

            GameObject vis = PrefabUtility.InstantiatePrefab(src) as GameObject ?? UnityEngine.Object.Instantiate(src);
            vis.name = "VisualModel";
            vis.transform.SetParent(root.transform, false);
            ApplyMappedMaterials(vis, mats);
            EnsureCharacterFacesPlusZ(root, vis);

            Animator anim = vis.GetComponentInChildren<Animator>(true) ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ac;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildGuardDogBossPrefab(string fbxPath, string outPath, RuntimeAnimatorController ac, GameObject sonicWavePrefab, Dictionary<string, Material> mats)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) return null;

            GameObject root = new GameObject("Prefab_Boss_PerroGuardian");
            root.tag = "Enemy";

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.72f, 0.1f);
            box.size = new Vector3(0.75f, 1.4f, 1.85f);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            NavMeshAgent ag = root.AddComponent<NavMeshAgent>();
            ag.radius = 0.55f;
            ag.height = 1.45f;
            ag.speed = 4.3f;
            ag.angularSpeed = 380f;
            ag.acceleration = 15f;

            EnemyAIPrototipo3 eai = root.AddComponent<EnemyAIPrototipo3>();
            eai.wanderSpeed = 4.3f;
            eai.chaseSpeed = 7.6f;
            eai.visionRadiusNormal = 18.5f;
            eai.visionRadiusCrouched = 7.5f;
            eai.baseSmellRadius = 6.8f;
            eai.attackRange = 2.0f;
            eai.showVisionCone = true;

            GuardDogBossAI dogBoss = root.AddComponent<GuardDogBossAI>();
            dogBoss.sonicBarkPrefab = sonicWavePrefab;
            root.AddComponent<EnemyOnCollision>();

            GameObject vis = PrefabUtility.InstantiatePrefab(src) as GameObject ?? UnityEngine.Object.Instantiate(src);
            vis.name = "VisualModel";
            vis.transform.SetParent(root.transform, false);
            ApplyMappedMaterials(vis, mats);
            EnsureCharacterFacesPlusZ(root, vis);

            Animator anim = vis.GetComponentInChildren<Animator>(true) ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ac;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildOwlRectorBossPrefab(string fbxPath, string outPath, RuntimeAnimatorController ac, GameObject featherPrefab, Dictionary<string, Material> mats)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) return null;

            GameObject root = new GameObject("Prefab_Boss_BuhoRector");
            root.tag = "Enemy";

            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.center = new Vector3(0f, 1.15f, 0f);
            sc.radius = 1.25f;

            OwlRectorBossAI owl = root.AddComponent<OwlRectorBossAI>();
            owl.featherProjectilePrefab = featherPrefab;

            GameObject vis = PrefabUtility.InstantiatePrefab(src) as GameObject ?? UnityEngine.Object.Instantiate(src);
            vis.name = "VisualModel";
            vis.transform.SetParent(root.transform, false);
            ApplyMappedMaterials(vis, mats);
            EnsureCharacterFacesPlusZ(root, vis);

            Animator anim = vis.GetComponentInChildren<Animator>(true) ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ac;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        // ==========================================================================================
        //  CONFIGURACIÓN BASE DE JUGADOR Y CÁMARA ISOMÉTRICA SIN FLOTACIÓN DE COLLIDER
        // ==========================================================================================

        private static GameObject SetupCommonIsometricSchoolRig(Vector3 playerSpawnPos, Color ambientCol, float sunIntensity)
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientCol;

            GameObject lightGo = new GameObject("School_DirectionalLight");
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = sunIntensity;
            dirLight.shadows = LightShadows.Soft;
            dirLight.color = new Color(0.84f, 0.88f, 0.98f, 1f);
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile != null)
            {
                GameObject volGo = new GameObject("Global Volume");
                Volume vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.sharedProfile = profile;
            }

            TimeManager tm = new GameObject("TimeManager").AddComponent<TimeManager>();
            tm.pauseAutoAdvance = true;
            new GameObject("InventoryManager").AddComponent<InventoryManager>();
            new GameObject("TrapSystemPrototipo3").AddComponent<TrapSystemPrototipo3>();
            new GameObject("RespawnManagerPrototipo3").AddComponent<RespawnManagerPrototipo3>();
            new GameObject("StoryDirector_Prototipo3").AddComponent<StoryDirectorPrototipo3>();

            GameObject respawnAnchor = new GameObject("SchoolSpawnAnchor");
            respawnAnchor.tag = "Respawn";
            respawnAnchor.transform.position = playerSpawnPos;

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 240f;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            UniversalAdditionalCameraData urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;

            IsometricCameraFollow camFollow = camGo.AddComponent<IsometricCameraFollow>();
            camFollow.pitchAngle = 45f;
            camFollow.yawAngle = 45f;
            camFollow.distance = 22f;
            camFollow.fieldOfView = 32f;

            // Jugador Kilo: un ÚNICO BoxCollider con base exacta en y=0 (sin CapsuleCollider duplicado ni BoxCollider 1x1x1 en y=-0.5!)
            GameObject playerGo = new GameObject("Hamster_Player");
            playerGo.tag = "Player";
            playerGo.transform.position = playerSpawnPos;
            playerGo.transform.localScale = Vector3.one;

            Rigidbody rb = playerGo.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            BoxCollider box = playerGo.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.size = new Vector3(0.44f, 0.56f, 0.52f);
            box.center = new Vector3(0f, 0.28f, 0f); // Base exacta en y = 0.00!

            HamsterControllerPrototipo3 hc = playerGo.AddComponent<HamsterControllerPrototipo3>();
            hc.movementCamera = camGo.transform;

            Material hamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototipo3/Materials/Mat_Hamster_Prototipo3.mat");
            GameObject hamMesh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/Hamster by Poly by Google - 3YtzEQ5TVUP/Mesh_Hamster.obj");
            if (hamMesh != null)
            {
                GameObject vis = PrefabUtility.InstantiatePrefab(hamMesh) as GameObject ?? UnityEngine.Object.Instantiate(hamMesh);
                vis.name = "VisualModel";
                vis.transform.SetParent(playerGo.transform, false);
                vis.transform.localScale = new Vector3(0.0042f, 0.0042f, 0.0042f);
                vis.transform.localRotation = Quaternion.identity;
                MeshRenderer[] mrs = vis.GetComponentsInChildren<MeshRenderer>(true);
                float minY = float.MaxValue;
                foreach (var mr in mrs)
                {
                    if (hamMat != null)
                    {
                        Material[] arr = new Material[mr.sharedMaterials.Length];
                        for (int m = 0; m < arr.Length; m++) arr[m] = hamMat;
                        mr.sharedMaterials = arr;
                    }
                    minY = Mathf.Min(minY, mr.bounds.min.y);
                }
                if (minY < float.MaxValue)
                {
                    vis.transform.position += new Vector3(0f, playerGo.transform.position.y - minY, 0f);
                }
            }

            camFollow.target = playerGo.transform;
            camGo.transform.position = playerSpawnPos + new Vector3(-11f, 15.5f, -11f);
            camGo.transform.LookAt(playerSpawnPos + Vector3.up * 0.4f);
            return playerGo;
        }

        private static GameObject PlaceSchoolModel(
            Transform parent,
            string fbxPath,
            string name,
            Vector3 pos,
            float rotY,
            Vector3 scale,
            bool addSolidBoxCollider,
            Dictionary<string, Material> mats,
            bool snapToGroundY = true)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogWarning($"[Prototipo3StoryAndSchoolBuilder] No se encontró modelo {fbxPath}");
                return null;
            }

            // Crear un objeto pivote limpio en rotación identidad primero para medir y ajustar bounds/collider sin distorsión de ejes X/Z,
            // preservando INTACTA la rotación local (-90° X) importada del FBX!
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pos;
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = scale;

            GameObject inst = PrefabUtility.InstantiatePrefab(fbx, pivot.transform) as GameObject ?? UnityEngine.Object.Instantiate(fbx, pivot.transform);
            inst.name = "ModelMesh";
            // NO sobrescribir inst.transform.localRotation: conserva (-90, 0, 0) en mallas únicas de Blender y (0, 0, 0) en jerarquías glTF!
            inst.transform.localPosition = Vector3.zero;
            ApplyMappedMaterials(inst, mats);

            Renderer[] rens = inst.GetComponentsInChildren<Renderer>(true);
            if (rens.Length > 0 && snapToGroundY)
            {
                Bounds b = rens[0].bounds;
                for (int i = 1; i < rens.Length; i++) b.Encapsulate(rens[i].bounds);
                // Asentar la base inferior exactamente en la altura Y del pivote (evita objetos flotando o hundidos!)
                float dy = pivot.transform.position.y - b.min.y;
                inst.transform.position += new Vector3(0f, dy, 0f);
            }

            if (addSolidBoxCollider && rens.Length > 0)
            {
                Bounds b = rens[0].bounds;
                for (int i = 1; i < rens.Length; i++) b.Encapsulate(rens[i].bounds);
                BoxCollider bc = pivot.AddComponent<BoxCollider>();
                bc.center = pivot.transform.InverseTransformPoint(b.center);
                Vector3 invScale = new Vector3(
                    1f / Mathf.Max(0.01f, Mathf.Abs(pivot.transform.lossyScale.x)),
                    1f / Mathf.Max(0.01f, Mathf.Abs(pivot.transform.lossyScale.y)),
                    1f / Mathf.Max(0.01f, Mathf.Abs(pivot.transform.lossyScale.z)));
                bc.size = Vector3.Scale(b.size, invScale);
            }

            // Aplicar la rotación Y final una vez que la malla y el BoxCollider están centrados en el espacio local del pivote
            pivot.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            return pivot;
        }

        private static GameObject CreateSolidSlabOrWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(parent, false);
            w.transform.localPosition = pos;
            w.transform.localScale = scale;
            if (mat != null) w.GetComponent<Renderer>().sharedMaterial = mat;
            BoxCollider bc = w.GetComponent<BoxCollider>();
            if (bc != null) bc.isTrigger = false;
            return w;
        }

        // ==========================================================================================
        //  ESCENA 1 AMPLIADA: HIGH SCHOOL BELLAVISTA (54m x 38m MULTI-SALÓN + PAPELES DE HELIOS-9)
        // ==========================================================================================

        private static void BuildSchoolInfiltrationScene(GameObject owlBossPrefab, Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCommonIsometricSchoolRig(new Vector3(-22.0f, 0.02f, -14.0f), new Color(0.30f, 0.32f, 0.40f, 1f), 0.80f);

            GameObject envRoot = new GameObject("HighSchool_Expanded_Environment");
            NavMeshSurface navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Material floorMat = mats.TryGetValue("M_PH_HallwayFloor", out var fm) ? fm : null;
            Material woodMat = mats.TryGetValue("M_School_WoodCourt", out var wm) ? wm : floorMat;
            Material burgundyMat = mats.TryGetValue("M_School_Burgundy", out var bgm) ? bgm : floorMat;
            Material plasterMat = mats.TryGetValue("M_PH_PlasterWall", out var pm) ? pm : null;
            Material brickMat = mats.TryGetValue("M_PH_BrickWall", out var bm) ? bm : plasterMat;

            // Losa principal continua anti-caídas (56m x 40m, cara superior exacta en y = 0.00f)
            CreateSolidSlabOrWall(envRoot.transform, "SchoolMainFoundationFloor", new Vector3(0f, -0.50f, 0f), new Vector3(56f, 1.0f, 40f), floorMat);
            // Pisos diferenciados por ala (Salones con madera, Dirección con alfombra vino)
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomsWoodFloor_NW", new Vector3(-7f, -0.48f, 11f), new Vector3(20f, 1.0f, 14f), woodMat);
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomsWoodFloor_S", new Vector3(1f, -0.48f, -11.5f), new Vector3(18f, 1.0f, 13f), woodMat);
            CreateSolidSlabOrWall(envRoot.transform, "PrincipalAndLabWingFloor_E", new Vector3(18.5f, -0.48f, 3f), new Vector3(16f, 1.0f, 28f), burgundyMat);

            // Muros perimetrales (Norte y Oeste altos; Sur y Este de corte isométrico bajo)
            CreateSolidSlabOrWall(envRoot.transform, "Perimeter_North_High", new Vector3(0f, 2.1f, 19.2f), new Vector3(55.4f, 4.2f, 0.5f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "Perimeter_West_High", new Vector3(-27.2f, 2.1f, 0f), new Vector3(0.5f, 4.2f, 39.0f), brickMat);
            CreateSolidSlabOrWall(envRoot.transform, "Perimeter_South_Cutaway", new Vector3(0f, 0.6f, -19.2f), new Vector3(55.4f, 1.2f, 0.5f), brickMat);
            CreateSolidSlabOrWall(envRoot.transform, "Perimeter_East_Cutaway", new Vector3(27.2f, 0.6f, 0f), new Vector3(0.5f, 1.2f, 39.0f), brickMat);

            // Tabiques interiores bajos/medios (1.7m para ver toda la planta isométrica sin estorbar la cámara pero creando cuartos reales y rutas de sigilo!)
            CreateSolidSlabOrWall(envRoot.transform, "Lobby_Divider_N", new Vector3(-17.5f, 0.95f, -4.0f), new Vector3(0.45f, 1.9f, 12.0f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomNW_Wall_S1", new Vector3(-13.0f, 0.95f, 4.2f), new Vector3(8.0f, 1.9f, 0.45f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomNW_Wall_S2", new Vector3(-1.5f, 0.95f, 4.2f), new Vector3(7.0f, 1.9f, 0.45f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomNW_Wall_E", new Vector3(2.8f, 0.95f, 11.5f), new Vector3(0.45f, 1.9f, 14.5f), plasterMat);

            CreateSolidSlabOrWall(envRoot.transform, "ClassroomS_Wall_N1", new Vector3(-4.5f, 0.95f, -5.2f), new Vector3(7.0f, 1.9f, 0.45f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomS_Wall_N2", new Vector3(6.8f, 0.95f, -5.2f), new Vector3(6.5f, 1.9f, 0.45f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "ClassroomS_Wall_E", new Vector3(10.0f, 0.95f, -12.0f), new Vector3(0.45f, 1.9f, 13.5f), plasterMat);

            // Muro del Ala Este (Laboratorio y Dirección) con 2 puertas anchas (Norte y Sur) para poder rodear al Búho Rector durante la huida!
            CreateSolidSlabOrWall(envRoot.transform, "EastWing_CenterWall", new Vector3(10.5f, 1.1f, 3.0f), new Vector3(0.45f, 2.2f, 10.5f), plasterMat);
            CreateSolidSlabOrWall(envRoot.transform, "EastWing_TopStub", new Vector3(10.5f, 1.1f, 15.8f), new Vector3(0.45f, 2.2f, 6.2f), plasterMat);

            // 1. ZONA SUR-OESTE: Alcantarilla de Entrada/Salida + Vestíbulo de Recepción
            GameObject holeObj = PlaceSchoolModel(envRoot.transform, $"{WeapModelsDir}/SM_SchoolHoleEntrance.fbx", "Alcantarilla_Salida_Escuela",
                new Vector3(-23.2f, 0f, -15.2f), 0f, Vector3.one, false, mats);
            if (holeObj != null)
            {
                holeObj.tag = "Interactable";
                BoxCollider trig = holeObj.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.center = new Vector3(0f, 0.9f, 0f);
                trig.size = new Vector3(3.8f, 2.4f, 3.8f);
                StoryInteractablePrototipo3 escInt = holeObj.AddComponent<StoryInteractablePrototipo3>();
                escInt.kind = StoryInteractableKind.SchoolEscapeHoleToCity;

                GameObject sewerLight = new GameObject("SchoolSewerGlow");
                sewerLight.transform.SetParent(holeObj.transform, false);
                sewerLight.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                Light sl = sewerLight.AddComponent<Light>();
                sl.type = LightType.Point;
                sl.range = 6.5f;
                sl.intensity = 2.4f;
                sl.color = new Color(0.20f, 0.88f, 1f, 1f);
            }

            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_ReceptionCounter.fbx", "Mostrador_Recepcion", new Vector3(-21.5f, 0f, -7.2f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_TrophyCase.fbx", "Vitrinas_Lobby_1", new Vector3(-26.3f, 0f, -5.5f), 90f, Vector3.one, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_VendingMachine.fbx", "Expendedora_Lobby", new Vector3(-26.2f, 0f, 1.5f), 90f, Vector3.one, true, mats);

            // 2. PASILLO CENTRAL DE CASILLEROS Y GALERÍA
            (Vector3 p, float ry)[] lockerBanks =
            {
                (new Vector3(-23.0f, 0f, 18.4f), 180f),
                (new Vector3(-18.5f, 0f, 18.4f), 180f),
                (new Vector3(-21.0f, 0f, 8.5f), 0f),
                (new Vector3(-14.0f, 0f, -0.5f), 0f),
                (new Vector3(-7.0f, 0f, 0.5f), 180f),
                (new Vector3(0.5f, 0f, -0.8f), 0f),
                (new Vector3(6.5f, 0f, 1.8f), 90f),
                (new Vector3(6.5f, 0f, 18.4f), 180f),
                (new Vector3(14.5f, 0f, -10.5f), 0f),
                (new Vector3(20.5f, 0f, -10.5f), 0f),
            };
            for (int i = 0; i < lockerBanks.Length; i++)
            {
                PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_LockerBank.fbx", $"Casilleros_{i + 1}",
                    lockerBanks[i].p, lockerBanks[i].ry, Vector3.one, true, mats);
            }

            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_VendingMachine.fbx", "Expendedora_Pasillo", new Vector3(2.8f, 0f, 2.2f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_TrophyCase.fbx", "Vitrinas_Pasillo", new Vector3(10.5f, 0f, 18.4f), 180f, Vector3.one, true, mats);

            // 3. SALÓN DE CLASES NOROESTE Y SALÓN DE CLASES SUR (Pupitres, Escritorios de Profesor, Libreros y Pizarrones en pared a altura 0.95m)
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_ChalkboardWall.fbx", "Pizarron_Salon_NO", new Vector3(-7.5f, 0.95f, 18.8f), 0f, Vector3.one, false, mats, snapToGroundY: true);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "Escritorio_Prof_NO", new Vector3(-11.5f, 0f, 15.6f), 0f, Vector3.one * 1.35f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_WoodenBookshelf.fbx", "Librero_Salon_NO", new Vector3(-16.2f, 0f, 12.0f), 90f, Vector3.one * 1.25f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_StudentDeskRow.fbx", "Pupitres_NO_Fila1", new Vector3(-7.0f, 0f, 12.5f), 180f, Vector3.one, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_StudentDeskRow.fbx", "Pupitres_NO_Fila2", new Vector3(-7.0f, 0f, 8.8f), 180f, Vector3.one, true, mats);

            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_ChalkboardWall.fbx", "Pizarron_Salon_Sur", new Vector3(1.2f, 0.75f, -5.55f), 180f, Vector3.one * 0.75f, false, mats, snapToGroundY: true);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "Escritorio_Prof_Sur", new Vector3(-3.2f, 0f, -8.5f), 180f, Vector3.one * 1.35f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_StudentDeskRow.fbx", "Pupitres_Sur_Fila1", new Vector3(1.8f, 0f, -11.0f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_StudentDeskRow.fbx", "Pupitres_Sur_Fila2", new Vector3(1.8f, 0f, -14.8f), 0f, Vector3.one, true, mats);

            // 4. ALA ESTE: LABORATORIO QUÍMICO + OFICINA DE DIRECCIÓN CON LOS PAPELES DE HELIOS-9
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_LabStation.fbx", "Mesa_Lab_1", new Vector3(16.0f, 0f, 2.5f), 90f, Vector3.one * 1.1f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_LabStation.fbx", "Mesa_Lab_2", new Vector3(16.0f, 0f, 9.5f), 90f, Vector3.one * 1.1f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_WoodenBookshelf.fbx", "Librero_Direccion_1", new Vector3(19.5f, 0f, 18.2f), 180f, Vector3.one * 1.3f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_WoodenBookshelf.fbx", "Librero_Direccion_2", new Vector3(23.5f, 0f, 18.2f), 180f, Vector3.one * 1.3f, true, mats);

            // Objetivo de Misión: Escritorio del Rector con los Papeles e Informes Confidenciales de Helios-9
            GameObject docsDesk = PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_PrincipalDeskDocs.fbx", "Escritorio_Papeles_Helios9",
                new Vector3(22.2f, 0f, 11.5f), -90f, Vector3.one * 1.2f, true, mats);
            if (docsDesk != null)
            {
                docsDesk.tag = "Interactable";
                BoxCollider trig = docsDesk.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.center = new Vector3(0f, 1.0f, 0.3f);
                trig.size = new Vector3(4.2f, 2.5f, 4.2f);
                StoryInteractablePrototipo3 clueInt = docsDesk.AddComponent<StoryInteractablePrototipo3>();
                clueInt.kind = StoryInteractableKind.SchoolPandemicClueLab;
            }

            // Iluminación interior atmosférica
            Vector3[] lampPositions =
            {
                new Vector3(-21f, 3.8f, -10f),
                new Vector3(-12f, 3.8f, 1f),
                new Vector3(-7f, 3.8f, 13f),
                new Vector3(2f, 3.8f, -12f),
                new Vector3(5f, 3.8f, 6f),
                new Vector3(17f, 3.8f, 4f),
                new Vector3(22f, 3.8f, 12f),
            };
            for (int i = 0; i < lampPositions.Length; i++)
            {
                PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_FluorescentLights.fbx", $"FluorLamp_{i + 1}", lampPositions[i], 0f, Vector3.one, false, mats, snapToGroundY: false);
                GameObject lp = new GameObject($"SchoolPointLight_{i + 1}");
                lp.transform.SetParent(envRoot.transform, false);
                lp.transform.localPosition = lampPositions[i] + Vector3.down * 0.4f;
                Light l = lp.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 16f;
                l.intensity = i == lampPositions.Length - 1 ? 3.8f : 2.8f;
                l.color = i == lampPositions.Length - 1 ? new Color(1f, 0.72f, 0.28f) : new Color(0.78f, 0.90f, 1f);
            }

            // Abundantes Contenedores Looteables dentro de la Escuela (8 puntos)
            string[] lootPrefabPaths =
            {
                "Assets/Prototipo3/Prefabs/Props/Prefab_Loot_TrashBin.prefab",
                "Assets/Prototipo3/Prefabs/Props/Prefab_Loot_ScrapCrate.prefab",
                "Assets/Prototipo3/Prefabs/Props/Prefab_Loot_WasteBag.prefab"
            };
            Vector3[] schoolLoots =
            {
                new Vector3(-19.0f, 0f, -2.0f),
                new Vector3(-15.0f, 0f, 16.5f),
                new Vector3(-2.5f, 0f, 2.5f),
                new Vector3(-6.0f, 0f, -16.0f),
                new Vector3(7.5f, 0f, -16.0f),
                new Vector3(8.2f, 0f, 10.5f),
                new Vector3(14.5f, 0f, -4.5f),
                new Vector3(24.0f, 0f, 2.0f),
            };
            for (int i = 0; i < schoolLoots.Length; i++)
            {
                GameObject lp = AssetDatabase.LoadAssetAtPath<GameObject>(lootPrefabPaths[i % lootPrefabPaths.Length]);
                if (lp != null)
                {
                    GameObject inst = PrefabUtility.InstantiatePrefab(lp, envRoot.transform) as GameObject;
                    if (inst != null) inst.transform.localPosition = schoolLoots[i];
                }
            }

            // Búho Rector listo en el ala Este para iniciar la persecución al tomar los papeles
            if (owlBossPrefab != null)
            {
                GameObject owl = PrefabUtility.InstantiatePrefab(owlBossPrefab) as GameObject ?? UnityEngine.Object.Instantiate(owlBossPrefab);
                owl.name = "Boss_BuhoRector_FirstEncounter";
                owl.transform.position = new Vector3(24.2f, 1.5f, 15.2f);
                owl.transform.rotation = Quaternion.Euler(0f, -135f, 0f);
                OwlRectorBossAI ai = owl.GetComponent<OwlRectorBossAI>();
                if (ai != null) ai.mode = OwlRectorBossAI.EncounterMode.SchoolFirstEscape;
                owl.SetActive(false);
            }

            navSurface.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StoryDirectorPrototipo3.SchoolEscapeScenePath);
        }

        // ==========================================================================================
        //  ESCENA 2 RECONSTRUIDA DESDE CERO: GIMNASIO DE BÁSQUETBOL (PELEA FINAL CONTRA EL BÚHO)
        // ==========================================================================================

        private static void BuildSchoolBasketballFinalScene(
            GameObject owlBossPrefab,
            GameObject netGunPrefab,
            GameObject netProjPrefab,
            Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCommonIsometricSchoolRig(new Vector3(-9.5f, 0.02f, -3.0f), new Color(0.38f, 0.36f, 0.42f, 1f), 1.15f);

            NetLauncherWeaponPrototipo3 weap = new GameObject("NetLauncherWeapon_Prototipo3").AddComponent<NetLauncherWeaponPrototipo3>();
            weap.gunVisualPrefab = netGunPrefab;
            weap.netProjectilePrefab = netProjPrefab;
            weap.isUnlocked = true;

            GameObject gymRoot = new GameObject("HighSchool_BasketballArena_Environment");
            NavMeshSurface navSurface = gymRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Material woodCourtMat = mats.TryGetValue("M_School_WoodCourt", out var wcm) ? wcm : null;
            Material burgundyMat = mats.TryGetValue("M_School_Burgundy", out var bgm) ? bgm : woodCourtMat;
            Material whiteLineMat = mats.TryGetValue("M_School_CourtLineWhite", out var wlm) ? wlm : null;
            Material brickMat = mats.TryGetValue("M_PH_BrickWall", out var bm) ? bm : null;
            Material plasterMat = mats.TryGetValue("M_PH_PlasterWall", out var pm) ? pm : brickMat;

            // 1. PISO SÓLIDO INQUEBRANTABLE DEL GIMNASIO + DUELA Y LÍNEAS DE BÁSQUETBOL HORIZONTALES
            CreateSolidSlabOrWall(gymRoot.transform, "GymOuterApronFloor", new Vector3(0f, -0.54f, 0f), new Vector3(42f, 1.0f, 30f), burgundyMat);
            CreateSolidSlabOrWall(gymRoot.transform, "GymHardwoodPlayingCourtFloor", new Vector3(0f, -0.50f, 0f), new Vector3(32.4f, 1.0f, 19.2f), woodCourtMat);

            // Malla completa de la cancha (con rotación -90° X preservada; base en -0.145f deja las líneas pintadas al ras en y = +0.010f)
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BasketballCourt.fbx", "CourtArcDetailsMesh",
                new Vector3(0f, -0.145f, 0f), 0f, Vector3.one, false, mats, snapToGroundY: true);

            // 2. Dos Tableros / Canastas enfrentadas hacia el centro de la cancha
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BasketballHoop.fbx", "Canasta_Oeste", new Vector3(-14.6f, 0f, 0f), 90f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BasketballHoop.fbx", "Canasta_Este", new Vector3(14.6f, 0f, 0f), -90f, Vector3.one, true, mats);

            // 3. Muros del Gimnasio (Norte y Oeste altos con Marcador Electrónico; Sur y Este bajos isométricos)
            CreateSolidSlabOrWall(gymRoot.transform, "GymWall_North", new Vector3(0f, 3.4f, 14.2f), new Vector3(41.4f, 6.8f, 0.6f), plasterMat);
            CreateSolidSlabOrWall(gymRoot.transform, "GymWall_West", new Vector3(-20.4f, 3.4f, 0f), new Vector3(0.6f, 6.8f, 29.0f), brickMat);
            CreateSolidSlabOrWall(gymRoot.transform, "GymWall_South_Low", new Vector3(0f, 0.65f, -14.2f), new Vector3(41.4f, 1.3f, 0.6f), brickMat);
            CreateSolidSlabOrWall(gymRoot.transform, "GymWall_East_Low", new Vector3(20.4f, 0.65f, 0f), new Vector3(0.6f, 1.3f, 29.0f), brickMat);

            // Marcador electrónico iluminado en la pared Norte (altura 3.8m)
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_Scoreboard.fbx", "Marcador_Gimnasio", new Vector3(0f, 3.8f, 13.75f), 0f, Vector3.one * 1.25f, false, mats, snapToGroundY: true);

            // 4. Gradas, Mesa de Anotadores, Bancas de Equipos y Carritos de Balones
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_Bleachers.fbx", "Gradas_Norte_Izq", new Vector3(-10.2f, 0f, 11.4f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_Bleachers.fbx", "Gradas_Norte_Der", new Vector3(10.2f, 0f, 11.4f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "Mesa_Anotadores", new Vector3(0f, 0f, 10.8f), 0f, Vector3.one * 1.4f, true, mats);

            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_TeamBench.fbx", "Banca_Local", new Vector3(-6.5f, 0f, -11.2f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_TeamBench.fbx", "Banca_Visitante", new Vector3(6.5f, 0f, -11.2f), 0f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BallCart.fbx", "Carrito_Balones_1", new Vector3(-17.2f, 0f, 7.5f), 25f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BallCart.fbx", "Carrito_Balones_2", new Vector3(17.2f, 0f, -7.5f), -35f, Vector3.one, true, mats);

            // 5. Lámparas colgadas de PolyHaven en el techo (con snapToGroundY=false para que permanezcan arriba en y=7.5m!)
            float[] trussX = { -11f, 0f, 11f };
            for (int i = 0; i < trussX.Length; i++)
            {
                PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/PH_CagedGymLight.fbx", $"PH_GymLight_{i + 1}", new Vector3(trussX[i], 7.5f, 0f), 0f, Vector3.one * 1.25f, false, mats, snapToGroundY: false);
                GameObject spotGo = new GameObject($"GymOverheadSpotlight_{i + 1}");
                spotGo.transform.SetParent(gymRoot.transform, false);
                spotGo.transform.localPosition = new Vector3(trussX[i], 7.2f, 0f);
                spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Light sl = spotGo.AddComponent<Light>();
                sl.type = LightType.Spot;
                sl.spotAngle = 105f;
                sl.range = 19f;
                sl.intensity = 8.5f;
                sl.color = new Color(1f, 0.90f, 0.74f);
            }

            // 6. Cuatro Consolas de Reflectores UV en las 4 esquinas de la cancha
            (Vector3 pos, float rotY)[] switchCorners =
            {
                (new Vector3(-11.2f, 0f, -7.2f), -135f),
                (new Vector3(11.2f, 0f, -7.2f), 135f),
                (new Vector3(-11.2f, 0f, 7.2f), -45f),
                (new Vector3(11.2f, 0f, 7.2f), 45f),
            };
            for (int i = 0; i < switchCorners.Length; i++)
            {
                GameObject sw = PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_FloodlightSwitch.fbx", $"Reflector_UV_{i + 1}",
                    switchCorners[i].pos, switchCorners[i].rotY, Vector3.one, true, mats);
                if (sw != null)
                {
                    sw.tag = "Interactable";
                    BoxCollider trig = sw.AddComponent<BoxCollider>();
                    trig.isTrigger = true;
                    trig.center = new Vector3(0f, 1.0f, 0f);
                    trig.size = new Vector3(3.0f, 2.2f, 3.0f);

                    GameObject uvLight = new GameObject("UV_BeamLight");
                    uvLight.transform.SetParent(sw.transform, false);
                    uvLight.transform.localPosition = new Vector3(0f, 2.35f, 0.2f);
                    Light l = uvLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 9f;
                    l.intensity = 2.0f;
                    l.color = new Color(0.2f, 0.88f, 1f);

                    StoryInteractablePrototipo3 inter = sw.AddComponent<StoryInteractablePrototipo3>();
                    inter.kind = StoryInteractableKind.GymFloodlightSwitch;
                }
            }

            // Contenedores de suministros laterales
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Props/Prefab_Loot_ScrapCrate.prefab");
            if (cratePrefab != null)
            {
                Vector3[] gymCrates = { new Vector3(-17.0f, 0f, -8.5f), new Vector3(17.0f, 0f, 8.5f) };
                for (int i = 0; i < gymCrates.Length; i++)
                {
                    GameObject c = PrefabUtility.InstantiatePrefab(cratePrefab, gymRoot.transform) as GameObject;
                    if (c != null) c.transform.localPosition = gymCrates[i];
                }
            }

            // 7. Jefe Final: EL BÚHO RECTOR
            if (owlBossPrefab != null)
            {
                GameObject owl = PrefabUtility.InstantiatePrefab(owlBossPrefab) as GameObject ?? UnityEngine.Object.Instantiate(owlBossPrefab);
                owl.name = "Boss_BuhoRector_FinalArena";
                owl.transform.position = new Vector3(6.5f, 3.35f, 1.5f);
                owl.transform.rotation = Quaternion.Euler(0f, -110f, 0f);
                OwlRectorBossAI ai = owl.GetComponent<OwlRectorBossAI>();
                if (ai != null)
                {
                    ai.mode = OwlRectorBossAI.EncounterMode.BasketballFinalBoss;
                    ai.maxCaptureHealth = 100f;
                    ai.currentCaptureHealth = 100f;
                    ai.arenaCenter = Vector3.zero;
                    ai.arenaHalfExtents = new Vector2(13.2f, 7.6f);
                }
            }

            navSurface.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StoryDirectorPrototipo3.SchoolBasketballScenePath);
        }

        // ==========================================================================================
        //  PREFABS DE CALLES CON SPOTLIGHT EN CADA POLE -> POLE_LIGHT + ACTUALIZACIÓN DE PROTOTIPO3.UNITY
        // ==========================================================================================

        private static Dictionary<string, GameObject> BuildPrototipo3RoadPrefabsWithPoleSpotLights()
        {
            const string outDir = "Assets/Prototipo3/Prefabs/Roads";
            EnsureDir(outDir);

            (string key, string srcPath, string outName)[] roadSpecs =
            {
                ("Forward", "Assets/ModularLowpolyStreetsFree/RoadForward.prefab", "RoadForward_Proto3.prefab"),
                ("Angle",   "Assets/ModularLowpolyStreetsFree/RoadAngle.prefab",   "RoadAngle_Proto3.prefab"),
                ("T",       "Assets/ModularLowpolyStreetsFree/RoadT.prefab",       "RoadT_Proto3.prefab"),
                ("X",       "Assets/ModularLowpolyStreetsFree/RoadX.prefab",       "RoadX_Proto3.prefab"),
            };

            var built = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            Color warmAmber = new Color(1.0f, 0.80f, 0.46f, 1f);

            foreach (var spec in roadSpecs)
            {
                GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(spec.srcPath);
                if (src == null) continue;

                GameObject tempRoot = UnityEngine.Object.Instantiate(src);
                tempRoot.name = Path.GetFileNameWithoutExtension(spec.outName);
                tempRoot.transform.position = Vector3.zero;
                tempRoot.transform.rotation = Quaternion.identity;

                Transform[] children = tempRoot.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    Transform tr = children[i];
                    if (tr == null || !tr.name.StartsWith("Pole_light", StringComparison.OrdinalIgnoreCase)) continue;
                    Renderer bulbRen = tr.GetComponent<Renderer>();
                    if (bulbRen == null) continue;

                    Light sl = CityStreetLampManagerPrototipo3.EnsureSpotLightOnPoleLight(
                        tr, bulbRen, warmAmber, 22f, 15.5f, 104f, 52f, out _);
                    if (sl != null)
                    {
                        // Apagada por defecto de día; CityStreetLampManagerPrototipo3 la enciende y hace parpadear de noche
                        sl.enabled = false;
                    }
                }

                string outPath = $"{outDir}/{spec.outName}";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(tempRoot, outPath);
                UnityEngine.Object.DestroyImmediate(tempRoot);
                if (saved != null) built[spec.key] = saved;
            }

            return built;
        }

        private static void UpgradePrototipo3CityScenePlayerAndLoot(Dictionary<string, GameObject> proto3RoadPrefabs = null)
        {
            if (!File.Exists(StoryDirectorPrototipo3.CityScenePath)) return;
            var scene = EditorSceneManager.OpenScene(StoryDirectorPrototipo3.CityScenePath, OpenSceneMode.Single);

            if (UnityEngine.Object.FindAnyObjectByType<StoryDirectorPrototipo3>() == null)
            {
                new GameObject("StoryDirector_Prototipo3").AddComponent<StoryDirectorPrototipo3>();
            }

            // Hacer el ciclo de día y noche más ágil por defecto en Prototipo3.unity (~2.2 minutos por ciclo completo)
            TimeManager tm = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
            if (tm != null)
            {
                tm.SetDayDuration(2.2f);
                EditorUtility.SetDirty(tm);
            }

            // Limpiar collider duplicado en Hamster_Player de Prototipo3.unity
            HamsterControllerPrototipo3 hc = UnityEngine.Object.FindAnyObjectByType<HamsterControllerPrototipo3>();
            if (hc != null)
            {
                CapsuleCollider cap = hc.GetComponent<CapsuleCollider>();
                if (cap != null) UnityEngine.Object.DestroyImmediate(cap);
                BoxCollider box = hc.GetComponent<BoxCollider>();
                if (box == null) box = hc.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = false;
                box.size = new Vector3(0.44f, 0.56f, 0.52f);
                box.center = new Vector3(0f, 0.28f, 0f);
                EditorUtility.SetDirty(hc.gameObject);
            }

            // Asignar los nuevos prefabs de calles con SpotLight en Pole_light + probabilidades de looteables
            CityGeneratorPrototipo3 cg = UnityEngine.Object.FindAnyObjectByType<CityGeneratorPrototipo3>();
            if (cg != null)
            {
                if (proto3RoadPrefabs != null)
                {
                    if (proto3RoadPrefabs.TryGetValue("Forward", out GameObject rf) && rf != null)
                    {
                        cg.roadForward = rf;
                        cg.roadDeadEnd = rf;
                    }
                    if (proto3RoadPrefabs.TryGetValue("Angle", out GameObject ra) && ra != null) cg.roadAngle = ra;
                    if (proto3RoadPrefabs.TryGetValue("T", out GameObject rt) && rt != null) cg.roadT = rt;
                    if (proto3RoadPrefabs.TryGetValue("X", out GameObject rx) && rx != null) cg.roadX = rx;
                }

                cg.lootProbability = 0.92f;
                if (cg.socketSpawns != null)
                {
                    for (int i = 0; i < cg.socketSpawns.Count; i++)
                    {
                        if (cg.socketSpawns[i] != null)
                        {
                            cg.socketSpawns[i].probability = Mathf.Max(cg.socketSpawns[i].probability, 0.72f);
                        }
                    }
                }
                EditorUtility.SetDirty(cg);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StoryDirectorPrototipo3.CityScenePath);
        }

        private static void RegisterScenesInBuildSettings()
        {
            string[] requiredScenes =
            {
                StoryDirectorPrototipo3.CityScenePath,
                StoryDirectorPrototipo3.SchoolEscapeScenePath,
                StoryDirectorPrototipo3.SchoolBasketballScenePath
            };

            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;

            foreach (string sp in requiredScenes)
            {
                if (!File.Exists(sp)) continue;
                int existing = list.FindIndex(s => string.Equals(s.path, sp, StringComparison.OrdinalIgnoreCase));
                if (existing < 0)
                {
                    list.Add(new EditorBuildSettingsScene(sp, true));
                    changed = true;
                }
                else if (!list[existing].enabled)
                {
                    list[existing].enabled = true;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorBuildSettings.scenes = list.ToArray();
            }
        }
    }
}
