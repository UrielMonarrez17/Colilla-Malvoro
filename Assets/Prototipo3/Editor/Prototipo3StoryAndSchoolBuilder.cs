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
    /// Constructor integral en Editor para la Historia Lineal, Jefes 3D, Armas y los 2 Escenarios de la Escuela en Prototipo 3.
    /// Se ejecuta automáticamente una vez al compilar (si aún no existe el sello de construcción) o desde el menú Feral/Prototipo 3.
    /// </summary>
    [InitializeOnLoad]
    public static class Prototipo3StoryAndSchoolBuilder
    {
        private const string BuildStampAssetPath = "Assets/Prototipo3/Resources/StorySchoolBuildStamp_v1.txt";

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

            Debug.Log("[Prototipo3StoryAndSchoolBuilder] Detectados nuevos assets 3D/Historia sin ensamblar. Ejecutando construcción automática...");
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

            string previousOpenScene = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.GetActiveScene().isDirty && !string.IsNullOrEmpty(previousOpenScene))
            {
                EditorSceneManager.SaveOpenScenes();
            }

            ConfigurePortraitTextures();
            ConfigurePolyHavenNormalMaps();

            Dictionary<string, Material> allMats = CreateOrUpdateAllStoryAndSchoolMaterials();

            // Configurar importadores FBX (materiales + clips con loop)
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
                $"{SchoolModelsDir}/SM_Gym_BasketballCourt.fbx",
                $"{SchoolModelsDir}/SM_Gym_BasketballHoop.fbx",
                $"{SchoolModelsDir}/SM_Gym_Bleachers.fbx",
                $"{SchoolModelsDir}/SM_Gym_RafterTruss.fbx",
                $"{SchoolModelsDir}/SM_Gym_FloodlightSwitch.fbx",
                $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx",
                $"{SchoolModelsDir}/PH_WoodenBookshelf.fbx",
                $"{SchoolModelsDir}/PH_FluorescentLights.fbx",
                $"{SchoolModelsDir}/PH_CagedGymLight.fbx",
            };
            foreach (string fbx in staticFbxs) ConfigureStaticFbx(fbx, allMats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Crear AnimatorControllers conectados a los clips de Blender
            RuntimeAnimatorController acMarcoDay = BuildGroundCharacterController("AC_Marco_Day", $"{CharModelsDir}/Marco_Tejon_Day.fbx");
            RuntimeAnimatorController acMarcoFeral = BuildGroundCharacterController("AC_Marco_Feral", $"{CharModelsDir}/Marco_Tejon_Feral.fbx");
            RuntimeAnimatorController acPerro = BuildGroundCharacterController("AC_PerroGuardian", $"{CharModelsDir}/Boss_PerroGuardian.fbx");
            RuntimeAnimatorController acToby = BuildGroundCharacterController("AC_Toby_Mapache", $"{CharModelsDir}/NPC_Toby_Mapache.fbx");
            RuntimeAnimatorController acBuho = BuildOwlBossController("AC_BuhoRector", $"{CharModelsDir}/Boss_BuhoRector.fbx");

            // Crear Prefabs de Armas y Proyectiles en Resources/Weapons
            GameObject netGunPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_NetLauncherGun.fbx", $"{ResWeapsDir}/Prefab_NetLauncherGun.prefab", allMats);
            GameObject netProjPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_NetProjectile.fbx", $"{ResWeapsDir}/Prefab_NetProjectile.prefab", allMats);
            GameObject owlFeatherPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_OwlFeatherProjectile.fbx", $"{ResWeapsDir}/Prefab_OwlFeatherProjectile.prefab", allMats);
            GameObject debrisPrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_BadgerDebrisProjectile.fbx", $"{ResWeapsDir}/Prefab_BadgerDebrisProjectile.prefab", allMats);
            GameObject sonicWavePrefab = BuildStaticVisualPrefab($"{WeapModelsDir}/SM_SonicBarkWave.fbx", $"{ResWeapsDir}/Prefab_SonicBarkWave.prefab", allMats);
            BuildStaticVisualPrefab($"{WeapModelsDir}/SM_SchoolHoleEntrance.fbx", $"{ResWeapsDir}/Prefab_SchoolHoleEntrance.prefab", allMats);
            BuildStaticVisualPrefab($"{WeapModelsDir}/SM_TobyWorkshopFacade.fbx", $"{ResWeapsDir}/Prefab_TobyWorkshopFacade.prefab", allMats);

            // Copiar flecha de misión a Resources/Props para el puntero de historia
            BuildStaticVisualPrefab("Assets/Prototipo3/Models/Props/SM_MissionArrow.fbx", $"{ResPropsDir}/SM_MissionArrow.prefab", allMats);

            // Crear Prefabs de NPCs y de los 3 Jefes en Resources/Characters
            BuildNpcVisualPrefab($"{CharModelsDir}/Marco_Tejon_Day.fbx", $"{ResCharsDir}/Prefab_Marco_Day.prefab", acMarcoDay, allMats);
            BuildNpcVisualPrefab($"{CharModelsDir}/NPC_Toby_Mapache.fbx", $"{ResCharsDir}/Prefab_Toby_Mapache.prefab", acToby, allMats);
            BuildMarcoFeralBossPrefab($"{CharModelsDir}/Marco_Tejon_Feral.fbx", $"{ResCharsDir}/Prefab_Marco_Feral.prefab", acMarcoFeral, debrisPrefab, allMats);
            BuildGuardDogBossPrefab($"{CharModelsDir}/Boss_PerroGuardian.fbx", $"{ResCharsDir}/Prefab_Boss_PerroGuardian.prefab", acPerro, sonicWavePrefab, allMats);
            GameObject owlBossPrefab = BuildOwlRectorBossPrefab($"{CharModelsDir}/Boss_BuhoRector.fbx", $"{ResCharsDir}/Prefab_Boss_BuhoRector.prefab", acBuho, owlFeatherPrefab, allMats);

            // Construir las 2 nuevas escenas de la Escuela y actualizar Prototipo3.unity + Build Settings
            BuildSchoolInfiltrationScene(owlBossPrefab, allMats);
            BuildSchoolBasketballFinalScene(owlBossPrefab, netGunPrefab, netProjPrefab, allMats);
            EnsureStoryDirectorInPrototipo3CityScene();
            RegisterScenesInBuildSettings();

            File.WriteAllText(BuildStampAssetPath, $"Built at {DateTime.UtcNow:O}\n");
            AssetDatabase.ImportAsset(BuildStampAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Dejar abierta Prototipo3.unity lista para dar Play
            if (File.Exists(StoryDirectorPrototipo3.CityScenePath))
            {
                EditorSceneManager.OpenScene(StoryDirectorPrototipo3.CityScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[Prototipo3StoryAndSchoolBuilder] ¡CONSTRUCCIÓN COMPLETA! Jefes 3D, animaciones, retratos, Pistola Lanza Redes, Escuela Infiltración y Cancha de Básquetbol listos.");
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static void ConfigurePortraitTextures()
        {
            const string dir = "Assets/Prototipo3/Resources/Portraits";
            if (!Directory.Exists(dir)) return;
            foreach (string file in Directory.GetFiles(dir, "*.png"))
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Default;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
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

                // Materiales de Escuela y Gimnasio
                ("M_School_LockerTeal", new Color(0.17f, 0.38f, 0.46f), 0.68f, 0.56f, null),
                ("M_School_LockerDark", new Color(0.13f, 0.17f, 0.21f), 0.72f, 0.50f, null),
                ("M_School_CourtLineWhite", new Color(0.95f, 0.95f, 0.93f), 0f, 0.65f, null),
                ("M_School_Burgundy", new Color(0.40f, 0.09f, 0.13f), 0f, 0.58f, null),
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

            // Materiales PBR de PolyHaven (Duela de Cancha, Ladrillo Roto, Yeso Escolar, Piso Escolar y Modelos PH)
            dict["M_School_WoodCourt"] = BuildPolyHavenUrpMat("M_School_WoodCourt", "laminate_floor_02", new Color(0.92f, 0.76f, 0.56f), 0.72f, new Vector2(5f, 5f));
            dict["M_PH_BrickWall"] = BuildPolyHavenUrpMat("M_PH_BrickWall", "broken_brick_wall", Color.white, 0.28f, new Vector2(4f, 2f));
            dict["M_PH_PlasterWall"] = BuildPolyHavenUrpMat("M_PH_PlasterWall", "blue_plaster_weathered", Color.white, 0.34f, new Vector2(4f, 2f));
            dict["M_PH_HallwayFloor"] = BuildPolyHavenUrpMat("M_PH_HallwayFloor", "concrete_floor_painted", new Color(0.85f, 0.87f, 0.90f), 0.42f, new Vector2(7f, 6f));

            dict["M_PH_metal_office_desk"] = BuildPolyHavenModelMat("M_PH_metal_office_desk", "metal_office_desk", new Color(0.45f, 0.48f, 0.50f), 0.55f, 0.52f);
            dict["M_PH_wooden_bookshelf_worn"] = BuildPolyHavenModelMat("M_PH_wooden_bookshelf_worn", "wooden_bookshelf_worn", new Color(0.52f, 0.34f, 0.20f), 0f, 0.38f);
            dict["M_PH_mounted_fluorescent_lights"] = CreateOrUpdateUrpMat($"{SchoolMatsDir}/M_PH_mounted_fluorescent_lights.mat", urpLit, new Color(0.92f, 0.95f, 0.98f), 0.3f, 0.7f, new Color(0.95f, 0.92f, 0.82f) * 1.4f);
            dict["M_PH_caged_hanging_light"] = CreateOrUpdateUrpMat($"{SchoolMatsDir}/M_PH_caged_hanging_light.mat", urpLit, new Color(0.85f, 0.80f, 0.70f), 0.5f, 0.6f, new Color(1f, 0.82f, 0.55f) * 1.5f);

            // Incluir también materiales de Props existentes para SM_MissionArrow
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
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            }
            if (imp == null) return;

            imp.animationType = ModelImporterAnimationType.Generic;
            imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var kv in mats)
            {
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            }
            imp.SaveAndReimport();

            // Segunda pasada para activar loopTime en Idle, Walk, Run, Fly, Swoop, Stunned, Talk
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
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            }
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

        private static GameObject BuildStaticVisualPrefab(string fbxPath, string prefabOutPath, Dictionary<string, Material> mats)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (src == null) return null;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabOutPath));
            GameObject vis = PrefabUtility.InstantiatePrefab(src) as GameObject ?? UnityEngine.Object.Instantiate(src);
            vis.name = "Model";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.identity;
            ApplyMappedMaterials(vis, mats);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabOutPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
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

            Animator anim = vis.GetComponentInChildren<Animator>(true);
            if (anim == null) anim = vis.AddComponent<Animator>();
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

            Animator anim = vis.GetComponentInChildren<Animator>(true) ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ac;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        // ==========================================================================================
        //  CONSTRUCCIÓN BASE COMÚN PARA LAS 2 ESCENAS DE LA ESCUELA (ISOMÉTRICAS)
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
            dirLight.color = new Color(0.76f, 0.82f, 0.96f, 1f);
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

            // Cámara Isométrica (45° pitch, 45° yaw)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 220f;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            UniversalAdditionalCameraData urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;

            IsometricCameraFollow camFollow = camGo.AddComponent<IsometricCameraFollow>();
            camFollow.pitchAngle = 45f;
            camFollow.yawAngle = 45f;
            camFollow.distance = 21.5f;
            camFollow.fieldOfView = 32f;

            // Jugador Hámster Kilo
            GameObject playerGo = new GameObject("Hamster_Player");
            playerGo.tag = "Player";
            playerGo.transform.position = playerSpawnPos;

            CapsuleCollider cap = playerGo.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.35f, 0f);
            cap.radius = 0.25f;
            cap.height = 0.7f;

            Rigidbody rb = playerGo.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

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
                if (minY < float.MaxValue) vis.transform.localPosition = new Vector3(0f, playerGo.transform.position.y - minY, 0f);
            }

            camFollow.target = playerGo.transform;
            camGo.transform.position = playerSpawnPos + new Vector3(-11f, 15f, -11f);
            camGo.transform.LookAt(playerSpawnPos + Vector3.up * 0.4f);
            return playerGo;
        }

        private static GameObject PlaceSchoolModel(Transform parent, string fbxPath, string name, Vector3 pos, float rotY, Vector3 scale, bool addSolidBoxCollider, Dictionary<string, Material> mats)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) return null;

            GameObject inst = PrefabUtility.InstantiatePrefab(fbx, parent) as GameObject ?? UnityEngine.Object.Instantiate(fbx, parent);
            inst.name = name;
            inst.transform.localPosition = pos;
            inst.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            inst.transform.localScale = scale;
            ApplyMappedMaterials(inst, mats);

            if (addSolidBoxCollider)
            {
                Renderer[] rens = inst.GetComponentsInChildren<Renderer>(true);
                if (rens.Length > 0)
                {
                    Bounds b = rens[0].bounds;
                    for (int i = 1; i < rens.Length; i++) b.Encapsulate(rens[i].bounds);
                    BoxCollider bc = inst.AddComponent<BoxCollider>();
                    bc.center = inst.transform.InverseTransformPoint(b.center);
                    Vector3 invScale = new Vector3(
                        1f / Mathf.Max(0.01f, Mathf.Abs(inst.transform.lossyScale.x)),
                        1f / Mathf.Max(0.01f, Mathf.Abs(inst.transform.lossyScale.y)),
                        1f / Mathf.Max(0.01f, Mathf.Abs(inst.transform.lossyScale.z)));
                    bc.size = Vector3.Scale(b.size, invScale);
                }
            }
            return inst;
        }

        private static GameObject CreateWallBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(parent, false);
            w.transform.localPosition = pos;
            w.transform.localScale = scale;
            if (mat != null) w.GetComponent<Renderer>().sharedMaterial = mat;
            return w;
        }

        // ==========================================================================================
        //  ESCENA 1: ESCUELA INFILTRACIÓN Y HUIDA DEL BÚHO RECTOR
        // ==========================================================================================

        private static void BuildSchoolInfiltrationScene(GameObject owlBossPrefab, Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCommonIsometricSchoolRig(new Vector3(-12.5f, 0.08f, -8.5f), new Color(0.26f, 0.28f, 0.36f, 1f), 0.65f);

            GameObject envRoot = new GameObject("HighSchool_Infiltration_Environment");
            NavMeshSurface navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Material floorMat = mats.TryGetValue("M_PH_HallwayFloor", out var fm) ? fm : null;
            Material woodMat = mats.TryGetValue("M_School_WoodCourt", out var wm) ? wm : floorMat;
            Material plasterMat = mats.TryGetValue("M_PH_PlasterWall", out var pm) ? pm : null;
            Material brickMat = mats.TryGetValue("M_PH_BrickWall", out var bm) ? bm : plasterMat;

            // Pisos principales (Pasillo + Laboratorio de Ciencias)
            CreateWallBlock(envRoot.transform, "HallwayFloor", new Vector3(0f, -0.15f, 0f), new Vector3(34f, 0.30f, 24f), floorMat);
            CreateWallBlock(envRoot.transform, "ScienceLabFloor", new Vector3(9.5f, -0.13f, 5.5f), new Vector3(14f, 0.30f, 12f), woodMat);

            // Muros Norte (+Z) y Oeste (-X) altos; Sur (-Z) y Este (+X) de corte isométrico bajo para máxima visibilidad
            CreateWallBlock(envRoot.transform, "Wall_North_High", new Vector3(0f, 2.0f, 12.0f), new Vector3(34.4f, 4.0f, 0.45f), plasterMat);
            CreateWallBlock(envRoot.transform, "Wall_West_High", new Vector3(-17.0f, 2.0f, 0f), new Vector3(0.45f, 4.0f, 24.4f), brickMat);
            CreateWallBlock(envRoot.transform, "Wall_South_Cutaway", new Vector3(0f, 0.55f, -12.0f), new Vector3(34.4f, 1.1f, 0.45f), brickMat);
            CreateWallBlock(envRoot.transform, "Wall_East_Cutaway", new Vector3(17.0f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 24.4f), brickMat);

            // Tabique interior divisorio entre Pasillo y Laboratorio (con puerta ancha abierta)
            CreateWallBlock(envRoot.transform, "LabPartition_Top", new Vector3(2.2f, 1.8f, 8.8f), new Vector3(0.4f, 3.6f, 6.4f), plasterMat);
            CreateWallBlock(envRoot.transform, "LabPartition_Bottom", new Vector3(2.2f, 1.8f, -3.5f), new Vector3(0.4f, 3.6f, 9.0f), plasterMat);

            // Agujero de entrada/salida en el muro Sur-Oeste
            GameObject holeObj = PlaceSchoolModel(envRoot.transform, $"{WeapModelsDir}/SM_SchoolHoleEntrance.fbx", "Agujero_Salida_Escuela",
                new Vector3(-14.6f, 0f, -9.2f), 90f, Vector3.one, false, mats);
            if (holeObj != null)
            {
                holeObj.tag = "Interactable";
                BoxCollider trig = holeObj.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.center = new Vector3(0f, 1.0f, 0.6f);
                trig.size = new Vector3(3.8f, 2.4f, 3.6f);
                StoryInteractablePrototipo3 escInt = holeObj.AddComponent<StoryInteractablePrototipo3>();
                escInt.kind = StoryInteractableKind.SchoolEscapeHoleToCity;
            }

            // Bancos de casilleros creando pasillos y coberturas para la huida
            (Vector3 p, float ry)[] lockerSpots =
            {
                (new Vector3(-11.5f, 0f, 11.4f), 180f),
                (new Vector3(-5.5f, 0f, 11.4f), 180f),
                (new Vector3(-10.5f, 0f, 2.5f), 0f),
                (new Vector3(-4.2f, 0f, -2.2f), 180f),
                (new Vector3(-11.0f, 0f, -4.5f), 90f),
                (new Vector3(-1.8f, 0f, 5.2f), -90f),
            };
            for (int i = 0; i < lockerSpots.Length; i++)
            {
                PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_LockerBank.fbx", $"Casilleros_{i + 1}",
                    lockerSpots[i].p, lockerSpots[i].ry, Vector3.one, true, mats);
            }

            // Props de PolyHaven: Escritorios, Libreros y Lámparas Fluorescentes
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_WoodenBookshelf.fbx", "PH_Bookshelf_1", new Vector3(-16.1f, 0f, 5.0f), 90f, Vector3.one * 1.25f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "PH_Desk_Hall", new Vector3(-4.5f, 0f, 7.2f), -25f, Vector3.one * 1.35f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "PH_Desk_Lab1", new Vector3(7.2f, 0f, 2.2f), 0f, Vector3.one * 1.35f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "PH_Desk_Lab2", new Vector3(13.2f, 0f, 2.2f), 180f, Vector3.one * 1.35f, true, mats);
            PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_ChalkboardWall.fbx", "Lab_Chalkboard", new Vector3(9.8f, 0f, 11.65f), 180f, Vector3.one, false, mats);

            Vector3[] lightSpots = { new Vector3(-10f, 3.6f, -3f), new Vector3(-6f, 3.6f, 6f), new Vector3(9.5f, 3.6f, 6f) };
            for (int i = 0; i < lightSpots.Length; i++)
            {
                PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/PH_FluorescentLights.fbx", $"PH_FluorLight_{i + 1}", lightSpots[i], 0f, Vector3.one, false, mats);
                GameObject pt = new GameObject($"SchoolLampPoint_{i + 1}");
                pt.transform.SetParent(envRoot.transform, false);
                pt.transform.localPosition = lightSpots[i] + Vector3.down * 0.4f;
                Light l = pt.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 14f;
                l.intensity = 2.6f;
                l.color = i == 2 ? new Color(1f, 0.72f, 0.35f) : new Color(0.75f, 0.88f, 1f);
            }

            // Estación de Laboratorio con Foco de Helios-9 (Objetivo de investigación)
            GameObject labTarget = PlaceSchoolModel(envRoot.transform, $"{SchoolModelsDir}/SM_School_LabStation.fbx", "Foco_Pandemia_Helios9_Lab",
                new Vector3(10.2f, 0f, 7.4f), 180f, Vector3.one * 1.15f, true, mats);
            if (labTarget != null)
            {
                labTarget.tag = "Interactable";
                BoxCollider trig = labTarget.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.center = new Vector3(0f, 1.0f, 0f);
                trig.size = new Vector3(3.6f, 2.4f, 2.8f);
                StoryInteractablePrototipo3 clueInt = labTarget.AddComponent<StoryInteractablePrototipo3>();
                clueInt.kind = StoryInteractableKind.SchoolPandemicClueLab;
            }

            // Contenedores de recursos opcionales en el pasillo
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototipo3/Prefabs/Props/Prefab_Loot_ScrapCrate.prefab");
            if (cratePrefab != null)
            {
                PrefabUtility.InstantiatePrefab(cratePrefab, envRoot.transform);
                GameObject c2 = PrefabUtility.InstantiatePrefab(cratePrefab, envRoot.transform) as GameObject;
                if (c2 != null) c2.transform.localPosition = new Vector3(-7.5f, 0f, -8.2f);
            }

            // Búho Rector listo para la emboscada (StoryDirector lo activa al inspeccionar el laboratorio)
            if (owlBossPrefab != null)
            {
                GameObject owl = PrefabUtility.InstantiatePrefab(owlBossPrefab) as GameObject ?? UnityEngine.Object.Instantiate(owlBossPrefab);
                owl.name = "Boss_BuhoRector_FirstEncounter";
                owl.transform.position = new Vector3(13.2f, 1.6f, 9.2f);
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
        //  ESCENA 2: CANCHA DE BÁSQUETBOL (PELEA FINAL CONTRA EL BÚHO RECTOR)
        // ==========================================================================================

        private static void BuildSchoolBasketballFinalScene(
            GameObject owlBossPrefab,
            GameObject netGunPrefab,
            GameObject netProjPrefab,
            Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupCommonIsometricSchoolRig(new Vector3(-9.5f, 0.08f, -2.5f), new Color(0.32f, 0.30f, 0.36f, 1f), 0.95f);

            // Vincular prefabs del Lanza Redes en un objeto de soporte de escena
            NetLauncherWeaponPrototipo3 weap = new GameObject("NetLauncherWeapon_Prototipo3").AddComponent<NetLauncherWeaponPrototipo3>();
            weap.gunVisualPrefab = netGunPrefab;
            weap.netProjectilePrefab = netProjPrefab;
            weap.isUnlocked = true;

            GameObject gymRoot = new GameObject("HighSchool_BasketballArena_Environment");
            NavMeshSurface navSurface = gymRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Material brickMat = mats.TryGetValue("M_PH_BrickWall", out var bm) ? bm : null;
            Material plasterMat = mats.TryGetValue("M_PH_PlasterWall", out var pm) ? pm : brickMat;

            // 1. Duela completa de la Cancha de Básquetbol con líneas y textura PBR PolyHaven
            GameObject court = PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BasketballCourt.fbx", "Cancha_Basquetbol_Duela",
                Vector3.zero, 0f, Vector3.one, false, mats);
            if (court != null)
            {
                BoxCollider floorCol = court.AddComponent<BoxCollider>();
                floorCol.center = new Vector3(0f, -0.05f, 0f);
                floorCol.size = new Vector3(38f, 0.2f, 26f);
            }

            // 2. Dos Tableros / Canastas reglamentarias (Oeste y Este)
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BasketballHoop.fbx", "Canasta_Oeste", new Vector3(-14.6f, 0f, 0f), 90f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_BasketballHoop.fbx", "Canasta_Este", new Vector3(14.6f, 0f, 0f), -90f, Vector3.one, true, mats);

            // 3. Muros perimetrales (Norte y Oeste altos; Sur y Este bajos para vista isométrica limpia)
            CreateWallBlock(gymRoot.transform, "GymWall_North", new Vector3(0f, 3.2f, 13.0f), new Vector3(38.4f, 6.4f, 0.5f), plasterMat);
            CreateWallBlock(gymRoot.transform, "GymWall_West", new Vector3(-19.0f, 3.2f, 0f), new Vector3(0.5f, 6.4f, 26.4f), brickMat);
            CreateWallBlock(gymRoot.transform, "GymWall_South_Low", new Vector3(0f, 0.6f, -13.0f), new Vector3(38.4f, 1.2f, 0.5f), brickMat);
            CreateWallBlock(gymRoot.transform, "GymWall_East_Low", new Vector3(19.0f, 0.6f, 0f), new Vector3(0.5f, 1.2f, 26.4f), brickMat);

            // 4. Gradas de madera al Norte y vigas superiores con lámparas enjauladas de PolyHaven
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_Bleachers.fbx", "Gradas_Izq", new Vector3(-9.2f, 0f, 10.6f), 180f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_Bleachers.fbx", "Gradas_Der", new Vector3(9.2f, 0f, 10.6f), 180f, Vector3.one, true, mats);
            PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/PH_MetalOfficeDesk.fbx", "Mesa_Anotadores", new Vector3(0f, 0f, 9.8f), 180f, Vector3.one * 1.4f, true, mats);

            float[] trussX = { -10f, 0f, 10f };
            for (int i = 0; i < trussX.Length; i++)
            {
                PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/SM_Gym_RafterTruss.fbx", $"Viga_Gimnasio_{i + 1}", new Vector3(trussX[i], 6.4f, 0f), 0f, Vector3.one, false, mats);
                PlaceSchoolModel(gymRoot.transform, $"{SchoolModelsDir}/PH_CagedGymLight.fbx", $"PH_GymLight_{i + 1}", new Vector3(trussX[i], 6.0f, 0f), 0f, Vector3.one * 1.3f, false, mats);

                GameObject spotGo = new GameObject($"GymOverheadLight_{i + 1}");
                spotGo.transform.SetParent(gymRoot.transform, false);
                spotGo.transform.localPosition = new Vector3(trussX[i], 5.8f, 0f);
                spotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Light sl = spotGo.AddComponent<Light>();
                sl.type = LightType.Spot;
                sl.spotAngle = 92f;
                sl.range = 16f;
                sl.intensity = 6.5f;
                sl.color = new Color(1f, 0.88f, 0.68f);
            }

            // 5. Cuatro Consolas de Reflectores UV en las esquinas de la duela para aturdir al Búho Rector con [F]
            (Vector3 pos, float rotY)[] switchCorners =
            {
                (new Vector3(-10.5f, 0f, -6.8f), 35f),
                (new Vector3(10.5f, 0f, -6.8f), -35f),
                (new Vector3(-10.5f, 0f, 6.8f), 145f),
                (new Vector3(10.5f, 0f, 6.8f), -145f),
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
                    trig.size = new Vector3(2.8f, 2.2f, 2.8f);

                    GameObject uvLight = new GameObject("UV_BeamLight");
                    uvLight.transform.SetParent(sw.transform, false);
                    uvLight.transform.localPosition = new Vector3(0f, 2.35f, 0.2f);
                    Light l = uvLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 8f;
                    l.intensity = 1.4f;
                    l.color = new Color(0.2f, 0.88f, 1f);

                    StoryInteractablePrototipo3 inter = sw.AddComponent<StoryInteractablePrototipo3>();
                    inter.kind = StoryInteractableKind.GymFloodlightSwitch;
                }
            }

            // 6. Jefe Final: EL BÚHO RECTOR en medio de la cancha de básquetbol
            if (owlBossPrefab != null)
            {
                GameObject owl = PrefabUtility.InstantiatePrefab(owlBossPrefab) as GameObject ?? UnityEngine.Object.Instantiate(owlBossPrefab);
                owl.name = "Boss_BuhoRector_FinalArena";
                owl.transform.position = new Vector3(6.5f, 3.2f, 1.5f);
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

        private static void EnsureStoryDirectorInPrototipo3CityScene()
        {
            if (!File.Exists(StoryDirectorPrototipo3.CityScenePath)) return;
            var scene = EditorSceneManager.OpenScene(StoryDirectorPrototipo3.CityScenePath, OpenSceneMode.Single);
            if (UnityEngine.Object.FindAnyObjectByType<StoryDirectorPrototipo3>() == null)
            {
                new GameObject("StoryDirector_Prototipo3").AddComponent<StoryDirectorPrototipo3>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, StoryDirectorPrototipo3.CityScenePath);
            }
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
