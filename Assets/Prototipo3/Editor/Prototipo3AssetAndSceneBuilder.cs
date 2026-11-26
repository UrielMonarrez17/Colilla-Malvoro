using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;

namespace Prototipo3.Editor
{
    public static class Prototipo3AssetAndSceneBuilder
    {
        private const string PropsModelsDir = "Assets/Prototipo3/Models/Props";
        private const string PropsMaterialsDir = "Assets/Prototipo3/Materials/Props";
        private const string PropsPrefabsDir = "Assets/Prototipo3/Prefabs/Props";
        private const string EnemyPrefabsDir = "Assets/Prototipo3/Prefabs";

        private static readonly (string name, Color color, float metallic, float smoothness, Color? emission)[] PropMaterialSpecs =
        {
            ("M_Craft_WoodWarm", new Color(0.52f, 0.30f, 0.14f, 1f), 0.0f, 0.28f, null),
            ("M_Craft_WoodDark", new Color(0.28f, 0.15f, 0.08f, 1f), 0.0f, 0.22f, null),
            ("M_Craft_Metal", new Color(0.36f, 0.40f, 0.46f, 1f), 0.72f, 0.56f, null),
            ("M_Craft_Blueprint", new Color(0.22f, 0.48f, 0.78f, 1f), 0.0f, 0.35f, null),
            ("M_Craft_Brass", new Color(0.88f, 0.66f, 0.22f, 1f), 0.80f, 0.68f, null),

            ("M_Loot_BinTeal", new Color(0.20f, 0.49f, 0.46f, 1f), 0.30f, 0.44f, null),
            ("M_Loot_BinMetalDark", new Color(0.24f, 0.27f, 0.31f, 1f), 0.65f, 0.52f, null),
            ("M_Loot_AccentRust", new Color(0.74f, 0.44f, 0.18f, 1f), 0.20f, 0.32f, null),

            ("M_Loot_CrateDark", new Color(0.34f, 0.20f, 0.09f, 1f), 0.0f, 0.22f, null),
            ("M_Loot_CratePlank", new Color(0.62f, 0.39f, 0.17f, 1f), 0.0f, 0.30f, null),
            ("M_Loot_CrateIron", new Color(0.28f, 0.31f, 0.36f, 1f), 0.75f, 0.58f, null),

            ("M_Loot_BagOlive", new Color(0.21f, 0.26f, 0.23f, 1f), 0.05f, 0.38f, null),
            ("M_Loot_RopeTan", new Color(0.78f, 0.64f, 0.34f, 1f), 0.0f, 0.18f, null),
            ("M_Loot_TinScrap", new Color(0.58f, 0.63f, 0.67f, 1f), 0.70f, 0.62f, null),

            ("M_Arrow_GoldCore", new Color(1.0f, 0.72f, 0.11f, 1f), 0.15f, 0.55f, new Color(1.0f, 0.58f, 0.05f, 1f) * 1.4f),
            ("M_Arrow_AccentTrim", new Color(0.85f, 0.34f, 0.06f, 1f), 0.35f, 0.50f, new Color(0.70f, 0.24f, 0.03f, 1f) * 0.65f),
        };

        private struct EnemySpeciesSpec
        {
            public string outputName;
            public string sourcePrefabName;
            public string controllerName;
            public float scale;
            public float agentRadius;
            public float agentHeight;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public float wanderSpeed;
            public float chaseSpeed;
            public float visionNormal;
            public float visionCrouched;
            public float backDetect;
            public float smellRadius;
            public float attackRange;
        }

        private static readonly EnemySpeciesSpec[] SpeciesSpecs =
        {
            new EnemySpeciesSpec { outputName = "Tiger_Prototipo3", sourcePrefabName = "Tiger_001", controllerName = "Tiger", scale = 0.90f, agentRadius = 0.55f, agentHeight = 1.2f, colliderCenter = new Vector3(0f, 0.58f, 0.1f), colliderSize = new Vector3(0.52f, 1.15f, 1.55f), wanderSpeed = 3.2f, chaseSpeed = 6.2f, visionNormal = 16.0f, visionCrouched = 5.5f, backDetect = 2.8f, smellRadius = 2.8f, attackRange = 1.9f },
            new EnemySpeciesSpec { outputName = "Dog_Prototipo3", sourcePrefabName = "Dog_001", controllerName = "Dog", scale = 0.90f, agentRadius = 0.40f, agentHeight = 0.8f, colliderCenter = new Vector3(0f, 0.32f, 0f), colliderSize = new Vector3(0.35f, 0.65f, 0.95f), wanderSpeed = 3.6f, chaseSpeed = 6.6f, visionNormal = 14.0f, visionCrouched = 6.0f, backDetect = 3.6f, smellRadius = 3.6f, attackRange = 1.7f },
            new EnemySpeciesSpec { outputName = "Deer_Prototipo3", sourcePrefabName = "Deer_001", controllerName = "Deer", scale = 0.85f, agentRadius = 0.48f, agentHeight = 1.4f, colliderCenter = new Vector3(0f, 0.68f, 0f), colliderSize = new Vector3(0.48f, 1.35f, 1.40f), wanderSpeed = 3.8f, chaseSpeed = 6.8f, visionNormal = 15.0f, visionCrouched = 5.0f, backDetect = 2.5f, smellRadius = 2.5f, attackRange = 1.8f },
            new EnemySpeciesSpec { outputName = "Horse_Prototipo3", sourcePrefabName = "Horse_001", controllerName = "Horse", scale = 0.80f, agentRadius = 0.58f, agentHeight = 1.6f, colliderCenter = new Vector3(0f, 0.78f, 0.05f), colliderSize = new Vector3(0.62f, 1.55f, 1.85f), wanderSpeed = 3.4f, chaseSpeed = 7.2f, visionNormal = 15.0f, visionCrouched = 4.5f, backDetect = 2.4f, smellRadius = 2.4f, attackRange = 2.1f },
            new EnemySpeciesSpec { outputName = "Kitty_Prototipo3", sourcePrefabName = "Kitty_001", controllerName = "Kitty", scale = 1.05f, agentRadius = 0.32f, agentHeight = 0.6f, colliderCenter = new Vector3(0f, 0.28f, 0f), colliderSize = new Vector3(0.32f, 0.55f, 0.80f), wanderSpeed = 3.5f, chaseSpeed = 6.4f, visionNormal = 17.0f, visionCrouched = 6.5f, backDetect = 3.0f, smellRadius = 3.0f, attackRange = 1.6f },
            new EnemySpeciesSpec { outputName = "Chicken_Prototipo3", sourcePrefabName = "Chicken_001", controllerName = "Chicken", scale = 1.20f, agentRadius = 0.30f, agentHeight = 0.6f, colliderCenter = new Vector3(0f, 0.30f, 0f), colliderSize = new Vector3(0.36f, 0.60f, 0.55f), wanderSpeed = 3.0f, chaseSpeed = 5.4f, visionNormal = 11.0f, visionCrouched = 4.0f, backDetect = 2.0f, smellRadius = 2.0f, attackRange = 1.5f },
            new EnemySpeciesSpec { outputName = "Pinguin_Prototipo3", sourcePrefabName = "Pinguin_001", controllerName = "Pinguin", scale = 1.10f, agentRadius = 0.34f, agentHeight = 0.8f, colliderCenter = new Vector3(0f, 0.40f, 0f), colliderSize = new Vector3(0.42f, 0.80f, 0.45f), wanderSpeed = 2.6f, chaseSpeed = 5.0f, visionNormal = 12.0f, visionCrouched = 4.5f, backDetect = 2.2f, smellRadius = 2.2f, attackRange = 1.5f },
        };

        [MenuItem("Feral/Prototipo 3/Reconstruir Assets, Prefabs y Escena Prototipo3")]
        public static void RunFullRebuild()
        {
            EnsureDir(PropsMaterialsDir);
            EnsureDir(PropsPrefabsDir);
            EnsureDir(EnemyPrefabsDir);

            Dictionary<string, Material> propMats = CreateOrUpdatePropMaterials();
            Material hamsterMat = EnsureHamsterMaterial();

            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_CraftingTable.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_Loot_TrashBin.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_Loot_ScrapCrate.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_Loot_WasteBag.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_MissionArrow.fbx"), propMats);

            AuditProjectMaterialsToUrp();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject fbxCraft = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_CraftingTable.fbx");
            GameObject fbxBin = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_Loot_TrashBin.fbx");
            GameObject fbxCrate = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_Loot_ScrapCrate.fbx");
            GameObject fbxBag = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_Loot_WasteBag.fbx");
            GameObject fbxArrow = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_MissionArrow.fbx");

            GameObject craftPrefab = BuildCraftingTablePrefab(fbxCraft, propMats);
            GameObject binPrefab = BuildLootPrefab("Prefab_Loot_TrashBin", fbxBin, propMats,
                new[] { (ItemType.Metal, 40f), (ItemType.Desperdicios, 35f), (ItemType.Goma, 25f) });
            GameObject cratePrefab = BuildLootPrefab("Prefab_Loot_ScrapCrate", fbxCrate, propMats,
                new[] { (ItemType.Madera, 45f), (ItemType.Metal, 30f), (ItemType.Goma, 25f) });
            GameObject bagPrefab = BuildLootPrefab("Prefab_Loot_WasteBag", fbxBag, propMats,
                new[] { (ItemType.Desperdicios, 40f), (ItemType.Semillas, 35f), (ItemType.Goma, 25f) });

            UpgradeGarbageCollectorPrefab(fbxBin, propMats);
            GameObject safeRefugePrefab = BuildSafeRefugePrefab(craftPrefab);
            List<GameObject> enemyPrefabs = BuildOrUpdateEnemyPrefabs();

            UpgradeTutorialScene(fbxCraft, new[] { fbxBin, fbxCrate, fbxBag }, fbxArrow, propMats);
            RebuildPrototipo3Scene(craftPrefab, binPrefab, cratePrefab, bagPrefab, safeRefugePrefab, enemyPrefabs, hamsterMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Prototipo3AssetAndSceneBuilder] Reconstrucción completa: 7 especies enemigas, refugio seguro, flecha Blender, loots exteriores, Tutorial.unity y Prototipo3.unity listos.");
        }

        [MenuItem("Feral/Tutorial/Reconstruir Todas las Mejoras del Tutorial")]
        public static void RunTutorialOnlyRebuild()
        {
            EnsureDir(PropsMaterialsDir);
            EnsureDir(PropsPrefabsDir);

            Dictionary<string, Material> propMats = CreateOrUpdatePropMaterials();
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_CraftingTable.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_Loot_TrashBin.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_Loot_ScrapCrate.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_Loot_WasteBag.fbx"), propMats);
            RemapFbxMaterials(Path.Combine(PropsModelsDir, "SM_MissionArrow.fbx"), propMats);

            AuditProjectMaterialsToUrp();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject fbxCraft = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_CraftingTable.fbx");
            GameObject fbxBin = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_Loot_TrashBin.fbx");
            GameObject fbxCrate = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_Loot_ScrapCrate.fbx");
            GameObject fbxBag = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_Loot_WasteBag.fbx");
            GameObject fbxArrow = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsModelsDir}/SM_MissionArrow.fbx");

            UpgradeGarbageCollectorPrefab(fbxBin, propMats);
            UpgradeTutorialScene(fbxCraft, new[] { fbxBin, fbxCrate, fbxBag }, fbxArrow, propMats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Prototipo3AssetAndSceneBuilder] Reconstrucción exclusiva de Tutorial.unity completada sin alterar Prototipo3.unity.");
        }

        private static void EnsureDir(string assetPath)
        {
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
            }
        }

        private static Dictionary<string, Material> CreateOrUpdatePropMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var dict = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in PropMaterialSpecs)
            {
                string matPath = $"{PropsMaterialsDir}/{spec.name}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(urpLit);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else if (mat.shader != urpLit)
                {
                    mat.shader = urpLit;
                }

                mat.SetColor("_BaseColor", spec.color);
                mat.color = spec.color;
                mat.SetFloat("_Metallic", spec.metallic);
                mat.SetFloat("_Smoothness", spec.smoothness);
                mat.SetFloat("_Surface", 0f);

                if (spec.emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", spec.emission.Value);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                }

                EditorUtility.SetDirty(mat);
                dict[spec.name] = mat;
            }
            return dict;
        }

        private static Material EnsureHamsterMaterial()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            const string matPath = "Assets/Prototipo3/Materials/Mat_Hamster_Prototipo3.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Animals/Hamster by Poly by Google - 3YtzEQ5TVUP/Tex_Hamster.png");
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void RemapFbxMaterials(string fbxPath, Dictionary<string, Material> mats)
        {
            if (!File.Exists(fbxPath))
            {
                Debug.LogWarning($"[Prototipo3AssetAndSceneBuilder] No existe FBX en {fbxPath}");
                return;
            }

            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            }
            if (importer == null)
            {
                Debug.LogWarning($"[Prototipo3AssetAndSceneBuilder] No ModelImporter para {fbxPath}");
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var kvp in mats)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kvp.Key), kvp.Value);
            }
            importer.SaveAndReimport();
        }

        private static void ApplyUrpMaterialsRecursively(GameObject root, Dictionary<string, Material> mats)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                Material[] current = r.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < current.Length; m++)
                {
                    if (current[m] != null && mats.TryGetValue(current[m].name, out Material mapped))
                    {
                        current[m] = mapped;
                        changed = true;
                    }
                    else if (current[m] == null && mats.TryGetValue("M_Loot_BinTeal", out Material fallbackMat))
                    {
                        current[m] = fallbackMat;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = current;
            }
        }

        private static GameObject BuildCraftingTablePrefab(GameObject fbxCraft, Dictionary<string, Material> mats)
        {
            string prefabPath = $"{PropsPrefabsDir}/Prefab_CraftingTable.prefab";
            GameObject root = new GameObject("Prefab_CraftingTable");
            root.tag = "Interactable";

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.85f, 0f);
            col.size = new Vector3(3.6f, 2.2f, 3.6f);

            root.AddComponent<CraftingTable>();

            if (fbxCraft != null)
            {
                GameObject visual = PrefabUtility.InstantiatePrefab(fbxCraft) as GameObject ?? UnityEngine.Object.Instantiate(fbxCraft);
                visual.name = "VisualModel";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                ApplyUrpMaterialsRecursively(visual, mats);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildLootPrefab(string prefabName, GameObject fbxModel, Dictionary<string, Material> mats, (ItemType item, float chance)[] drops)
        {
            string prefabPath = $"{PropsPrefabsDir}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);
            root.tag = "Interactable";

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.55f, 0f);
            col.size = new Vector3(2.8f, 2.2f, 2.8f);

            LootContainer lc = root.AddComponent<LootContainer>();
            lc.posiblesLoot = new List<LootDrop>();
            foreach (var d in drops)
            {
                lc.posiblesLoot.Add(new LootDrop { item = d.item, dropChance = d.chance });
            }

            if (fbxModel != null)
            {
                GameObject visual = PrefabUtility.InstantiatePrefab(fbxModel) as GameObject ?? UnityEngine.Object.Instantiate(fbxModel);
                visual.name = "VisualModel";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                ApplyUrpMaterialsRecursively(visual, mats);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildSafeRefugePrefab(GameObject craftPrefab)
        {
            const string sourcePath = "Assets/PreFabs/City/InteriorBuilding.prefab";
            const string outputPath = PropsPrefabsDir + "/Prefab_RefugioSeguro.prefab";

            GameObject interiorSrc = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (interiorSrc == null)
            {
                Debug.LogError($"[Prototipo3AssetAndSceneBuilder] No se encontró {sourcePath}");
                return null;
            }

            GameObject root = new GameObject("Prefab_RefugioSeguro");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            // Instanciar desempaquetado para limpiar offsets, escalar a lote 1x1 y despejar centro
            GameObject interior = UnityEngine.Object.Instantiate(interiorSrc, root.transform, false);
            interior.name = "InteriorModel";
            interior.transform.localPosition = Vector3.zero;
            interior.transform.localRotation = Quaternion.identity;
            interior.transform.localScale = Vector3.one;

            Transform pav = interior.transform.Find("Pavement");
            if (pav != null) UnityEngine.Object.DestroyImmediate(pav.gameObject);

            foreach (Transform t in interior.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.IndexOf("Mesh_Hamster", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            Transform layout = interior.transform.Find("Layout");
            Material wallsMat = null;
            if (layout != null)
            {
                layout.localScale = new Vector3(0.64f, 0.64f, 0.64f);
                layout.localPosition = Vector3.zero;

                // Alinear altura superior del Floor justo en y = 0.02f y reemplazar MeshCollider plano por BoxCollider sólido
                Transform floorTr = layout.Find("Floor");
                if (floorTr != null)
                {
                    Renderer floorRen = floorTr.GetComponentInChildren<Renderer>();
                    if (floorRen != null)
                    {
                        float floorTopY = floorRen.bounds.max.y;
                        layout.position += new Vector3(0f, 0.02f - floorTopY, 0f);
                    }
                    MeshCollider floorCol = floorTr.GetComponent<MeshCollider>();
                    if (floorCol != null) floorCol.enabled = false;
                }

                GameObject solidFloor = new GameObject("RefugeSolidFloorCollider");
                solidFloor.transform.SetParent(root.transform, false);
                BoxCollider floorBox = solidFloor.AddComponent<BoxCollider>();
                floorBox.isTrigger = false;
                floorBox.center = new Vector3(0f, -0.23f, 0.05f);
                floorBox.size = new Vector3(8.8f, 0.50f, 8.9f);

                Transform wallsTr = layout.Find("Walls");
                if (wallsTr != null)
                {
                    Renderer[] wallRens = wallsTr.GetComponentsInChildren<Renderer>(true);
                    if (wallRens.Length > 0) wallsMat = wallRens[0].sharedMaterial;

                    for (int i = 0; i < wallRens.Length; i++)
                    {
                        BoxCollider wb = wallRens[i].GetComponent<BoxCollider>();
                        if (wb == null) wb = wallRens[i].gameObject.AddComponent<BoxCollider>();
                        wb.enabled = true;
                        wb.isTrigger = false;
                    }
                }

                // Retirar únicamente mobiliario que obstruya el paso central o el rincón de la mesa de crafteo
                Bounds clearWalkway = new Bounds(new Vector3(0f, 1.0f, -1.0f), new Vector3(3.4f, 4.0f, 5.6f));
                Bounds clearTableArea = new Bounds(new Vector3(-2.3f, 1.0f, 1.4f), new Vector3(3.2f, 4.0f, 3.2f));
                string[] trimGroups = { "Chairs", "OfficePlace", "RelaxZone", "Trash", "ChairSet", "ChairSet (1)", "Props", "Bookshelf" };
                for (int g = 0; g < trimGroups.Length; g++)
                {
                    Transform grp = layout.Find(trimGroups[g]);
                    if (grp == null) continue;
                    Renderer[] itemRens = grp.GetComponentsInChildren<Renderer>(true);
                    for (int r = itemRens.Length - 1; r >= 0; r--)
                    {
                        if (itemRens[r] == null) continue;
                        Bounds ib = itemRens[r].bounds;
                        Bounds bXZ = new Bounds(new Vector3(ib.center.x, 1.0f, ib.center.z), new Vector3(ib.size.x, 2.0f, ib.size.z));
                        if (bXZ.Intersects(clearWalkway) || bXZ.Intersects(clearTableArea))
                        {
                            itemRens[r].gameObject.SetActive(false);
                        }
                    }
                }
            }

            if (wallsMat == null)
            {
                wallsMat = AssetDatabase.LoadAssetAtPath<Material>(PropsMaterialsDir + "/M_Craft_WoodWarm.mat");
            }

            // Cerrar la fachada frontal (-Z) dejando un acceso centrado (2.6m ancho x 2.65m alto)
            GameObject frontFacade = new GameObject("FrontFacadeWalls");
            frontFacade.transform.SetParent(root.transform, false);
            CreateRefugeWallBlock(frontFacade.transform, "FrontWall_Left", new Vector3(-3.06f, 2.05f, -4.36f), new Vector3(3.52f, 4.1f, 0.28f), wallsMat);
            CreateRefugeWallBlock(frontFacade.transform, "FrontWall_Right", new Vector3(3.06f, 2.05f, -4.36f), new Vector3(3.52f, 4.1f, 0.28f), wallsMat);
            CreateRefugeWallBlock(frontFacade.transform, "FrontWall_Lintel", new Vector3(0f, 3.38f, -4.36f), new Vector3(2.65f, 1.44f, 0.28f), wallsMat);

            // Mesa de crafteo dentro del refugio
            if (craftPrefab != null)
            {
                GameObject tableObj = PrefabUtility.InstantiatePrefab(craftPrefab, root.transform) as GameObject ?? UnityEngine.Object.Instantiate(craftPrefab, root.transform, false);
                tableObj.name = "Refuge_CraftingTable";
                tableObj.tag = "Interactable";
                tableObj.transform.localPosition = new Vector3(-2.3f, 0.04f, 1.4f);
                tableObj.transform.localRotation = Quaternion.Euler(0f, 135f, 0f);
            }

            // Marco + PuertaTrampa en el umbral (-Z)
            GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PreFabs/Doors/Door_1_Yellow Variant.prefab");
            GameObject doorGo;
            if (doorPrefab != null)
            {
                doorGo = UnityEngine.Object.Instantiate(doorPrefab, root.transform, false);
                doorGo.name = "Doorway_PuertaTrampa";
                doorGo.transform.localPosition = new Vector3(0f, 0.04f, -4.36f);
                doorGo.transform.localRotation = Quaternion.identity;
                Transform frame = doorGo.transform.Find("Frame");
                if (frame != null)
                {
                    MeshCollider mc = frame.GetComponent<MeshCollider>();
                    if (mc != null) mc.enabled = false;
                }
                DoorScript.Door[] hinges = doorGo.GetComponentsInChildren<DoorScript.Door>(true);
                for (int i = 0; i < hinges.Length; i++)
                {
                    hinges[i].open = true;
                    hinges[i].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                }
            }
            else
            {
                doorGo = new GameObject("Doorway_PuertaTrampa");
                doorGo.transform.SetParent(root.transform, false);
                doorGo.transform.localPosition = new Vector3(0f, 0.04f, -4.36f);
            }

            doorGo.tag = "Door";
            if (doorGo.GetComponent<PuertaTrampa>() == null) doorGo.AddComponent<PuertaTrampa>();
            BoxCollider doorTrigger = doorGo.GetComponent<BoxCollider>();
            if (doorTrigger == null) doorTrigger = doorGo.AddComponent<BoxCollider>();
            doorTrigger.isTrigger = true;
            doorTrigger.center = new Vector3(0f, 1.2f, 0f);
            doorTrigger.size = new Vector3(2.6f, 2.4f, 1.6f);

            // Puntos de anclaje para jugador y repulsión exterior
            GameObject spawnAnchor = new GameObject("PlayerSpawnAnchor");
            spawnAnchor.transform.SetParent(root.transform, false);
            spawnAnchor.transform.localPosition = new Vector3(0f, 0.12f, 0.1f);

            GameObject outsideAnchor = new GameObject("DoorwayOutsidePoint");
            outsideAnchor.transform.SetParent(root.transform, false);
            outsideAnchor.transform.localPosition = new Vector3(0f, 0.05f, -6.5f);

            // Zona segura (Trigger BoxCollider en el interior + umbral de la puerta + SafeRefugeZone)
            BoxCollider safeTrigger = root.AddComponent<BoxCollider>();
            safeTrigger.isTrigger = true;
            safeTrigger.center = new Vector3(0f, 1.4f, -0.15f);
            safeTrigger.size = new Vector3(9.2f, 3.6f, 9.4f);

            SafeRefugeZone safeZone = root.AddComponent<SafeRefugeZone>();
            safeZone.safeTrigger = safeTrigger;
            safeZone.doorwayOutsidePoint = outsideAnchor.transform;
            safeZone.showSafeIndicatorHud = true;

            // NavMeshObstacle que recorta el interior del NavMesh para impedir el paso de enemigos
            // dejando z=-4.36 (umbral PuertaTrampa) libre para que activen trampas instaladas en la puerta
            GameObject navBlocker = new GameObject("SafeRefugeNavBlocker");
            navBlocker.transform.SetParent(root.transform, false);
            NavMeshObstacle navObs = navBlocker.AddComponent<NavMeshObstacle>();
            navObs.shape = NavMeshObstacleShape.Box;
            navObs.center = new Vector3(0f, 1.2f, 0.65f);
            navObs.size = new Vector3(8.5f, 2.6f, 7.3f);
            navObs.carving = true;
            navObs.carveOnlyStationary = true;

            EnsureRenderersUseUrp(root);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateRefugeWallBlock(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = localScale;
            Renderer r = wall.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            BoxCollider bc = wall.GetComponent<BoxCollider>();
            if (bc != null) bc.isTrigger = false;
        }

        private static List<GameObject> BuildOrUpdateEnemyPrefabs()
        {
            var built = new List<GameObject>();
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

            for (int i = 0; i < SpeciesSpecs.Length; i++)
            {
                EnemySpeciesSpec spec = SpeciesSpecs[i];
                string srcPath = $"Assets/Animals/Animals_FREE/Prefabs/{spec.sourcePrefabName}.prefab";
                GameObject srcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
                if (srcPrefab == null)
                {
                    Debug.LogWarning($"[Prototipo3AssetAndSceneBuilder] No se encontró animal base: {srcPath}");
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(srcPrefab);
                instance.name = spec.outputName;
                instance.tag = "Enemy";
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one * spec.scale;

                // Eliminar primero dependientes (MovePlayerInput/AnimationShowcase) antes que CreatureMover/CharacterController
                MonoBehaviour[] monos = instance.GetComponentsInChildren<MonoBehaviour>(true);
                string[] firstPass = { "MovePlayerInput", "AnimationShowcase" };
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int m = monos.Length - 1; m >= 0; m--)
                    {
                        if (monos[m] == null) continue;
                        string tn = monos[m].GetType().Name;
                        bool match = pass == 0
                            ? Array.IndexOf(firstPass, tn) >= 0
                            : tn == "CreatureMover";
                        if (match)
                        {
                            UnityEngine.Object.DestroyImmediate(monos[m]);
                        }
                    }
                }
                CharacterController cc = instance.GetComponent<CharacterController>();
                if (cc != null) UnityEngine.Object.DestroyImmediate(cc);

                // Garantizar Animator + RuntimeAnimatorController
                Animator anim = instance.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = instance.AddComponent<Animator>();
                if (anim.runtimeAnimatorController == null)
                {
                    RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        $"Assets/Animals/Animals_FREE/Animations/Animation_Controllers/{spec.controllerName}.controller");
                    if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                }

                BoxCollider box = instance.GetComponent<BoxCollider>();
                if (box == null) box = instance.AddComponent<BoxCollider>();
                box.enabled = true;
                box.isTrigger = false;
                box.center = spec.colliderCenter;
                box.size = spec.colliderSize;

                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb == null) rb = instance.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
                if (agent == null) agent = instance.AddComponent<NavMeshAgent>();
                agent.radius = spec.agentRadius;
                agent.height = spec.agentHeight;
                agent.speed = spec.wanderSpeed;
                agent.angularSpeed = 360f;
                agent.acceleration = 12f;
                agent.baseOffset = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.autoRepath = true;

                EnemyAIPrototipo3 ai = instance.GetComponent<EnemyAIPrototipo3>();
                if (ai == null) ai = instance.AddComponent<EnemyAIPrototipo3>();
                ai.wanderRadius = 35f;
                ai.wanderWaitTime = 2.5f;
                ai.wanderSpeed = spec.wanderSpeed;
                ai.chaseSpeed = spec.chaseSpeed;
                ai.visionRadiusNormal = spec.visionNormal;
                ai.visionRadiusCrouched = spec.visionCrouched;
                ai.backDetectionRadius = spec.backDetect;
                ai.baseSmellRadius = spec.smellRadius;
                ai.visionAngle = 95f;
                ai.attackRange = spec.attackRange;
                ai.showVisionCone = true;

                if (instance.GetComponent<EnemyOnCollision>() == null)
                {
                    instance.AddComponent<EnemyOnCollision>();
                }

                EnsureRenderersUseUrp(instance, urpLit);

                string outPath = $"{EnemyPrefabsDir}/{spec.outputName}.prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, outPath);
                UnityEngine.Object.DestroyImmediate(instance);
                if (saved != null) built.Add(saved);
            }

            Debug.Log($"[Prototipo3AssetAndSceneBuilder] Prefabs de enemigos preparados ({built.Count}/{SpeciesSpecs.Length}).");
            return built;
        }

        private static void EnsureRenderersUseUrp(GameObject root, Shader urpLit = null)
        {
            if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (root == null || urpLit == null) return;

            Renderer[] rens = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rens.Length; i++)
            {
                if (rens[i] == null) continue;
                Material[] mats = rens[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null || mat.shader == null) continue;
                    string sn = mat.shader.name;
                    if (sn == "Standard" || sn == "Standard (Specular setup)" || sn.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) || sn == "Hidden/InternalErrorShader")
                    {
                        Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                        Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                        mat.shader = urpLit;
                        if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }

        private static void UpgradeGarbageCollectorPrefab(GameObject fbxBin, Dictionary<string, Material> mats)
        {
            const string path = "Assets/PreFabs/LootContainers/GarbageCollector.prefab";
            if (!File.Exists(path) || fbxBin == null) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                MeshFilter mf = contents.GetComponent<MeshFilter>();
                if (mf != null) UnityEngine.Object.DestroyImmediate(mf);
                MeshRenderer mr = contents.GetComponent<MeshRenderer>();
                if (mr != null) UnityEngine.Object.DestroyImmediate(mr);

                contents.transform.localPosition = Vector3.zero;
                contents.transform.localScale = Vector3.one;
                BoxCollider bc = contents.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.isTrigger = true;
                    bc.center = new Vector3(0f, 0.5f, 0f);
                    bc.size = new Vector3(2.8f, 2.2f, 2.8f);
                }

                Transform oldVis = contents.transform.Find("VisualModel");
                if (oldVis != null) UnityEngine.Object.DestroyImmediate(oldVis.gameObject);

                GameObject visual = PrefabUtility.InstantiatePrefab(fbxBin) as GameObject ?? UnityEngine.Object.Instantiate(fbxBin);
                visual.name = "VisualModel";
                visual.transform.SetParent(contents.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                ApplyUrpMaterialsRecursively(visual, mats);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void AuditProjectMaterialsToUrp()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int fixedCount = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;

                string sName = mat.shader.name;
                bool isBroken = sName == "Standard" ||
                                sName == "Standard (Specular setup)" ||
                                sName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) ||
                                sName == "Hidden/InternalErrorShader" ||
                                sName.IndexOf("PostProcessDataTests", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isBroken) continue;

                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = urpLit;
                if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }

            Debug.Log($"[Prototipo3AssetAndSceneBuilder] Auditoría de materiales URP completada ({fixedCount} actualizados).");
        }

        private static void UpgradeTutorialScene(GameObject fbxCraft, GameObject[] fbxLootOptions, GameObject fbxArrow, Dictionary<string, Material> mats)
        {
            const string tutorialPath = "Assets/Scenes/Tutorial.unity";
            if (!File.Exists(tutorialPath)) return;

            var scene = EditorSceneManager.OpenScene(tutorialPath, OpenSceneMode.Single);

            // 1. Reemplazar CraftTable por SM_CraftingTable.fbx
            CraftingTable[] tables = UnityEngine.Object.FindObjectsByType<CraftingTable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < tables.Length; i++)
            {
                CraftingTable table = tables[i];
                if (table == null) continue;
                GameObject go = table.gameObject;

                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null) UnityEngine.Object.DestroyImmediate(mf);
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null) UnityEngine.Object.DestroyImmediate(mr);

                Vector3 p = go.transform.position;
                if (p.y > 0.05f && p.y < 1.0f) p.y = 0f;
                go.transform.position = p;
                go.transform.localScale = Vector3.one;

                BoxCollider bc = go.GetComponent<BoxCollider>();
                if (bc == null) bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.center = new Vector3(0f, 0.85f, 0f);
                bc.size = new Vector3(3.6f, 2.2f, 3.6f);

                Transform oldVis = go.transform.Find("VisualModel");
                if (oldVis != null) UnityEngine.Object.DestroyImmediate(oldVis.gameObject);

                if (fbxCraft != null)
                {
                    GameObject visual = PrefabUtility.InstantiatePrefab(fbxCraft) as GameObject ?? UnityEngine.Object.Instantiate(fbxCraft);
                    visual.name = "VisualModel";
                    visual.transform.SetParent(go.transform, false);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;
                    ApplyUrpMaterialsRecursively(visual, mats);
                }
            }

            // 2. Reemplazar GarbageCollector(Clone) con assets 3D URP
            LootContainer[] containers = UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < containers.Length; i++)
            {
                LootContainer lc = containers[i];
                if (lc == null) continue;
                GameObject go = lc.gameObject;
                if (!go.name.StartsWith("GarbageCollector", StringComparison.OrdinalIgnoreCase)) continue;

                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null) UnityEngine.Object.DestroyImmediate(mf);
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null) UnityEngine.Object.DestroyImmediate(mr);

                Vector3 p = go.transform.position;
                if (p.y > 0.05f && p.y <= 0.6f) p.y = 0f;
                go.transform.position = p;
                go.transform.localScale = Vector3.one;

                BoxCollider bc = go.GetComponent<BoxCollider>();
                if (bc == null) bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.center = new Vector3(0f, 0.55f, 0f);
                bc.size = new Vector3(2.8f, 2.2f, 2.8f);

                Transform oldVis = go.transform.Find("VisualModel");
                if (oldVis != null) UnityEngine.Object.DestroyImmediate(oldVis.gameObject);

                int pick = Mathf.Abs(Mathf.RoundToInt(p.x * 3f + p.z * 7f)) % fbxLootOptions.Length;
                GameObject chosenFbx = fbxLootOptions[pick];
                if (chosenFbx != null)
                {
                    GameObject visual = PrefabUtility.InstantiatePrefab(chosenFbx) as GameObject ?? UnityEngine.Object.Instantiate(chosenFbx);
                    visual.name = "VisualModel";
                    visual.transform.SetParent(go.transform, false);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.Euler(0f, (Mathf.Abs(Mathf.RoundToInt(p.x + p.z)) % 4) * 90f, 0f);
                    visual.transform.localScale = Vector3.one;
                    ApplyUrpMaterialsRecursively(visual, mats);
                }
            }

            // 3. Reemplazar flecha indicadora de misiones (PunteroMision / Arrow) por modelo grande estilizado de Blender
            UpgradeTutorialMissionArrow(fbxArrow, mats);

            // 4. Construir recorrido guiado + cerrar todo el perímetro del tutorial + hornear NavMesh primero
            TutorialLayoutBuilder.Build();

            // 4b. Asegurar escala (1.3) y BoxCollider de Lulú + Smog atmosférico y cámara URP del Tutorial
            ConfigureTutorialLuluAndSmog();

            // 5. Auditar y reubicar cualquier LootContainer atascado dentro de edificios de Tutorial.unity
            RelocateOverlappingTutorialLoots();

            // 6. Verificar que no queden Renderers con material nulo o roto
            MeshRenderer[] allRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Material safeFallback = mats.TryGetValue("M_Loot_CrateDark", out Material dMat) ? dMat : null;
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < allRenderers.Length; i++)
            {
                MeshRenderer r = allRenderers[i];
                if (r == null) continue;
                Material[] ms = r.sharedMaterials;
                bool dirty = false;
                for (int m = 0; m < ms.Length; m++)
                {
                    if (ms[m] == null && safeFallback != null)
                    {
                        ms[m] = safeFallback;
                        dirty = true;
                    }
                    else if (ms[m] != null && ms[m].shader != null &&
                        (ms[m].shader.name == "Standard" || ms[m].shader.name == "Hidden/InternalErrorShader") &&
                        urpLit != null)
                    {
                        ms[m].shader = urpLit;
                        EditorUtility.SetDirty(ms[m]);
                    }
                }
                if (dirty) r.sharedMaterials = ms;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, tutorialPath);
            Debug.Log($"[Prototipo3AssetAndSceneBuilder] Tutorial.unity actualizado (Lulú 1.3, Smog URP, flecha Blender, perímetro cerrado y {containers.Length} contenedores auditados).");
        }

        private static void ConfigureTutorialLuluAndSmog()
        {
            LuluNPC lulu = UnityEngine.Object.FindAnyObjectByType<LuluNPC>(FindObjectsInactive.Include);
            if (lulu != null)
            {
                lulu.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                BoxCollider luluCol = lulu.GetComponent<BoxCollider>();
                if (luluCol == null) luluCol = lulu.gameObject.AddComponent<BoxCollider>();
                luluCol.enabled = true;
                luluCol.isTrigger = true;
                luluCol.center = new Vector3(0f, 0.6f, 0f);
                luluCol.size = new Vector3(2.2f, 1.4f, 2.2f);
                EditorUtility.SetDirty(lulu.gameObject);
                EditorUtility.SetDirty(luluCol);
            }

            RenderSettings.fog = false;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                UniversalAdditionalCameraData urpCam = mainCam.GetComponent<UniversalAdditionalCameraData>();
                if (urpCam != null)
                {
                    urpCam.renderPostProcessing = true;
                    urpCam.requiresDepthOption = CameraOverrideOption.On;
                    urpCam.requiresColorOption = CameraOverrideOption.On;
                    EditorUtility.SetDirty(urpCam);
                }
            }

            Prototipo2.DirectionalNightFog nightFog = UnityEngine.Object.FindAnyObjectByType<Prototipo2.DirectionalNightFog>(FindObjectsInactive.Include);
            if (nightFog != null)
            {
                Shader fogSh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Prototipo2/Shaders/CityDirectionalFog.shader");
                if (fogSh != null) nightFog.fogShader = fogSh;
                if (mainCam != null) nightFog.targetCamera = mainCam;
                HamsterController hc = UnityEngine.Object.FindAnyObjectByType<HamsterController>();
                if (hc != null) nightFog.playerTransform = hc.transform;
                nightFog.dayFogColor = new Color(0.70f, 0.76f, 0.82f, 0.24f);
                nightFog.nightFogColor = new Color(0.06f, 0.09f, 0.14f, 0.82f);
                nightFog.fogDensity = 0.022f;
                nightFog.directionalForwardBoost = 1.25f;
                nightFog.nearDistance = 11.5f;
                nightFog.farDistance = 32f;
                nightFog.noiseScale = 0.065f;
                nightFog.noiseSpeed = 0.35f;
                nightFog.maxSmogAlpha = 0.8f;
                nightFog.fogTransitionSpeed = 1.8f;
                nightFog.controlGlobalFog = false;
                EditorUtility.SetDirty(nightFog);
            }
        }

        private static void UpgradeTutorialMissionArrow(GameObject fbxArrow, Dictionary<string, Material> mats)
        {
            PunteroMision arrow = UnityEngine.Object.FindAnyObjectByType<PunteroMision>(FindObjectsInactive.Include);
            if (arrow == null || fbxArrow == null)
            {
                Debug.LogWarning("[Prototipo3AssetAndSceneBuilder] No se encontró PunteroMision o SM_MissionArrow.fbx en Tutorial.unity.");
                return;
            }

            float parentScale = arrow.transform.parent != null ? Mathf.Max(0.0001f, arrow.transform.parent.lossyScale.x) : 1f;

            for (int i = arrow.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(arrow.transform.GetChild(i).gameObject);
            }

            MeshFilter rootMf = arrow.GetComponent<MeshFilter>();
            if (rootMf != null) UnityEngine.Object.DestroyImmediate(rootMf);
            MeshRenderer rootMr = arrow.GetComponent<MeshRenderer>();
            if (rootMr != null) UnityEngine.Object.DestroyImmediate(rootMr);

            arrow.transform.localPosition = new Vector3(0f, 1.35f / parentScale, 0f);
            arrow.transform.localRotation = Quaternion.identity;
            arrow.transform.localScale = Vector3.one * (1.25f / parentScale);

            arrow.flotar = true;
            arrow.velocidadFlotacion = 3.2f;
            arrow.amplitudFlotacion = 0.10f / parentScale;

            GameObject visual = PrefabUtility.InstantiatePrefab(fbxArrow) as GameObject ?? UnityEngine.Object.Instantiate(fbxArrow);
            visual.name = "VisualModel";
            visual.transform.SetParent(arrow.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Collider[] cols = visual.GetComponentsInChildren<Collider>(true);
            for (int c = cols.Length - 1; c >= 0; c--)
            {
                if (cols[c] != null) UnityEngine.Object.DestroyImmediate(cols[c]);
            }

            MeshRenderer[] mrs = visual.GetComponentsInChildren<MeshRenderer>(true);
            for (int r = 0; r < mrs.Length; r++)
            {
                mrs[r].shadowCastingMode = ShadowCastingMode.Off;
            }

            ApplyUrpMaterialsRecursively(visual, mats);

            // Verificar geométricamente que la punta de la flecha (+alas anchas) apunte hacia +Z en arrow.transform
            MeshFilter[] mfs = visual.GetComponentsInChildren<MeshFilter>(true);
            float weightedZSum = 0f;
            float weightTotal = 0f;
            for (int f = 0; f < mfs.Length; f++)
            {
                Mesh mesh = mfs[f].sharedMesh;
                if (mesh == null) continue;
                Vector3[] verts = mesh.vertices;
                for (int v = 0; v < verts.Length; v++)
                {
                    Vector3 worldP = mfs[f].transform.TransformPoint(verts[v]);
                    Vector3 localP = arrow.transform.InverseTransformPoint(worldP);
                    float absX = Mathf.Abs(localP.x);
                    if (absX > 0.25f)
                    {
                        weightedZSum += localP.z * absX;
                        weightTotal += absX;
                    }
                }
            }
            if (weightTotal > 0f && (weightedZSum / weightTotal) < 0f)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            EditorUtility.SetDirty(arrow);
        }

        private static void RelocateOverlappingTutorialLoots()
        {
            var buildingBounds = new List<Bounds>();
            BuildingData[] bData = UnityEngine.Object.FindObjectsByType<BuildingData>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var seenRoots = new HashSet<Transform>();
            for (int i = 0; i < bData.Length; i++)
            {
                if (bData[i] != null && seenRoots.Add(bData[i].transform))
                {
                    buildingBounds.Add(ComputeTutorialObjectBounds(bData[i].gameObject));
                }
            }

            GameObject[] allGos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < allGos.Length; i++)
            {
                GameObject go = allGos[i];
                if (go == null || !seenRoots.Add(go.transform)) continue;
                string n = go.name;
                if (n.StartsWith("Building", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("special_temp", StringComparison.OrdinalIgnoreCase))
                {
                    // Evitar sub-partes hijas de otro edificio ya medido
                    if (go.transform.parent != null &&
                        (go.transform.parent.name.StartsWith("Building", StringComparison.OrdinalIgnoreCase) ||
                         go.transform.parent.name.StartsWith("special_temp", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    buildingBounds.Add(ComputeTutorialObjectBounds(go));
                }
            }

            LootContainer[] containers = UnityEngine.Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            int movedCount = 0;
            int removedCount = 0;

            for (int i = 0; i < containers.Length; i++)
            {
                LootContainer lc = containers[i];
                if (lc == null) continue;
                if (lc.gameObject.name == "GarbageCollector" || lc.esContenedorResplandeciente) continue;

                Vector3 pos = lc.transform.position;
                int overlapIdx = FindOverlappingBoundsIndex(pos, buildingBounds, 0.35f);
                if (overlapIdx < 0) continue;

                Bounds targetB = buildingBounds[overlapIdx];
                bool relocated = false;

                for (int d = 0; d < dirs.Length; d++)
                {
                    Vector3 dir = dirs[d];
                    float halfExt = Mathf.Abs(dir.x) > 0.5f ? targetB.extents.x : targetB.extents.z;
                    Vector3 edgePos = new Vector3(targetB.center.x, 0f, targetB.center.z) + dir * (halfExt + 0.90f);

                    if (FindOverlappingBoundsIndex(edgePos, buildingBounds, 0.35f) >= 0) continue;

                    if (NavMesh.SamplePosition(edgePos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                    {
                        Vector3 finalPos = new Vector3(hit.position.x, 0f, hit.position.z);
                        if (FindOverlappingBoundsIndex(finalPos, buildingBounds, 0.25f) >= 0) continue;

                        lc.transform.position = finalPos;
                        lc.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                        EditorUtility.SetDirty(lc.gameObject);
                        relocated = true;
                        movedCount++;
                        break;
                    }
                }

                if (!relocated)
                {
                    UnityEngine.Object.DestroyImmediate(lc.gameObject);
                    removedCount++;
                }
            }

            Debug.Log($"[Prototipo3AssetAndSceneBuilder] Auditoría loots Tutorial: reubicados={movedCount}, eliminados por estar totalmente encerrados={removedCount}.");
        }

        private static Bounds ComputeTutorialObjectBounds(GameObject obj)
        {
            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box != null && box.enabled) return box.bounds;
            Renderer[] rens = obj.GetComponentsInChildren<Renderer>(true);
            Bounds b = default;
            bool init = false;
            for (int i = 0; i < rens.Length; i++)
            {
                if (rens[i] == null) continue;
                if (!init) { b = rens[i].bounds; init = true; }
                else b.Encapsulate(rens[i].bounds);
            }
            return init ? b : new Bounds(obj.transform.position, new Vector3(6f, 4f, 6f));
        }

        private static int FindOverlappingBoundsIndex(Vector3 pos, List<Bounds> boundsList, float padding)
        {
            for (int i = 0; i < boundsList.Count; i++)
            {
                Bounds b = boundsList[i];
                if (pos.x >= b.min.x - padding && pos.x <= b.max.x + padding &&
                    pos.z >= b.min.z - padding && pos.z <= b.max.z + padding)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void RebuildPrototipo3Scene(
            GameObject craftPrefab,
            GameObject binPrefab,
            GameObject cratePrefab,
            GameObject bagPrefab,
            GameObject safeRefugePrefab,
            List<GameObject> enemyPrefabs,
            Material hamsterMat)
        {
            const string scenePath = "Assets/Prototipo3/Prototipo3.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f, 1f);

            // 1. Directional Light + DayNightCycle
            GameObject lightGo = new GameObject("Directional Light");
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.color = new Color(1f, 0.96f, 0.88f, 1f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightGo.AddComponent<DayNightCycle>();

            // 2. Global Volume URP
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (profile != null)
            {
                GameObject volGo = new GameObject("Global Volume");
                Volume vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.sharedProfile = profile;
            }

            // 3. Managers globales
            new GameObject("TimeManager").AddComponent<TimeManager>();
            new GameObject("InventoryManager").AddComponent<InventoryManager>();
            new GameObject("TrapSystemPrototipo3").AddComponent<TrapSystemPrototipo3>();
            new GameObject("RespawnManagerPrototipo3").AddComponent<RespawnManagerPrototipo3>();

            // 4. Main Camera + IsometricCameraFollow + DirectionalNightFog
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 260f;
            camGo.AddComponent<AudioListener>();

            UniversalAdditionalCameraData urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.requiresDepthOption = CameraOverrideOption.On;
            urpCam.requiresColorOption = CameraOverrideOption.On;

            IsometricCameraFollow camFollow = camGo.AddComponent<IsometricCameraFollow>();
            camFollow.pitchAngle = 45f;
            camFollow.yawAngle = 45f;
            camFollow.distance = 20f;
            camFollow.fieldOfView = 32f;

            DirectionalNightFog nightFog = camGo.AddComponent<DirectionalNightFog>();
            nightFog.targetCamera = cam;
            nightFog.fogShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Prototipo3/Shaders/CityDirectionalFog.shader");
            nightFog.dayFogColor = new Color(0.70f, 0.76f, 0.82f, 0.24f);
            nightFog.nightFogColor = new Color(0.06f, 0.09f, 0.14f, 0.82f);
            nightFog.directionalForwardBoost = 1.25f;
            nightFog.nearDistance = 11.5f;
            nightFog.farDistance = 32f;
            nightFog.controlGlobalFog = false;

            // 5. Hamster_Player (VisualModel orientado a +Z = 0 grados para que camine de frente)
            GameObject playerGo = new GameObject("Hamster_Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(150f, 0.05f, 150f);

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

            GameObject hamsterModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animals/Hamster by Poly by Google - 3YtzEQ5TVUP/Mesh_Hamster.obj");
            if (hamsterModel != null)
            {
                GameObject vis = PrefabUtility.InstantiatePrefab(hamsterModel) as GameObject ?? UnityEngine.Object.Instantiate(hamsterModel);
                vis.name = "VisualModel";
                vis.transform.SetParent(playerGo.transform, false);
                vis.transform.localScale = new Vector3(0.0042f, 0.0042f, 0.0042f);
                vis.transform.localRotation = Quaternion.identity;

                MeshRenderer[] mrs = vis.GetComponentsInChildren<MeshRenderer>(true);
                float minY = float.MaxValue;
                for (int i = 0; i < mrs.Length; i++)
                {
                    if (hamsterMat != null)
                    {
                        Material[] matsArr = new Material[mrs[i].sharedMaterials.Length];
                        for (int m = 0; m < matsArr.Length; m++) matsArr[m] = hamsterMat;
                        mrs[i].sharedMaterials = matsArr;
                    }
                    minY = Mathf.Min(minY, mrs[i].bounds.min.y);
                }
                if (minY < float.MaxValue)
                {
                    vis.transform.localPosition = new Vector3(0f, playerGo.transform.position.y - minY, 0f);
                }
            }

            camFollow.target = playerGo.transform;
            nightFog.playerTransform = playerGo.transform;
            camGo.transform.position = playerGo.transform.position + new Vector3(-10f, 14f, -10f);
            camGo.transform.LookAt(playerGo.transform.position + Vector3.up * 0.4f);

            // 6. CityManager_Prototipo3
            GameObject cityGo = new GameObject("CityManager_Prototipo3");
            CityGeneratorPrototipo3 cg = cityGo.AddComponent<CityGeneratorPrototipo3>();
            CityEnemyPopulationSpawner popSpawner = cityGo.AddComponent<CityEnemyPopulationSpawner>();
            NavMeshSurface navSurface = cityGo.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.Children;
            navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            cg.seed = 12345;
            cg.mapWidth = 30;
            cg.mapLength = 30;
            cg.cellSize = 10f;
            cg.roadHeightY = 0f;
            cg.hamsterTransform = playerGo.transform;
            cg.craftTablePrefab = craftPrefab;
            cg.lootPrefab = binPrefab;
            cg.safeRefugePrefab = safeRefugePrefab;
            cg.settings = new CityGenerationSettings
            {
                minWalkers = 3,
                maxWalkers = 8,
                maxWalkerSteps = 4000,
                turnChancePerSide = 0.1f,
                branchChance = 0.2f,
                maxRoundaboutCells = 8,
                roundaboutSpacing = 3,
                specialAttempts = 50,
                minCellDistanceBetweenSpecials = 4,
                maxAccessPathLength = 12,
                normalBuildingDensity = 0.65f,
                allowInteriorBuildings = false
            };

            GameObject specialBld = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/special_temp Variant.prefab");
            cg.specialBuildings = new List<GameObject>();
            if (specialBld != null)
            {
                for (int i = 0; i < 4; i++) cg.specialBuildings.Add(specialBld);
            }

            string[] normalPaths =
            {
                "Assets/SimplePoly City - Low Poly Assets/Building Sky_big_color01.prefab",
                "Assets/SimplePoly City - Low Poly Assets/Building Sky_small_color01.prefab",
                "Assets/SimplePoly City - Low Poly Assets/Building_House_01_color01.prefab",
                "Assets/SimplePoly City - Low Poly Assets/Building_House_02_color01.prefab",
                "Assets/SimplePoly City - Low Poly Assets/Building_House_03_color02.prefab",
                "Assets/SimplePoly City - Low Poly Assets/Building_Residential_color01.prefab",
            };
            cg.normalBuildings = new List<GameObject>();
            foreach (string np in normalPaths)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(np);
                if (p != null) cg.normalBuildings.Add(p);
            }

            cg.roadForward = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModularLowpolyStreetsFree/RoadForward.prefab");
            cg.roadAngle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModularLowpolyStreetsFree/RoadAngle.prefab");
            cg.roadT = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModularLowpolyStreetsFree/RoadT.prefab");
            cg.roadX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModularLowpolyStreetsFree/RoadX.prefab");
            cg.roadDeadEnd = cg.roadForward;
            cg.pedestrianPavement = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModularLowpolyStreetsFree/Pavement.prefab");

            cg.socketSpawns = new List<SocketSpawnRule>();
            if (binPrefab != null) cg.socketSpawns.Add(new SocketSpawnRule { name = "Basurero", prefab = binPrefab, probability = 0.28f, yOffset = 0f, alignToBuilding = true });
            if (cratePrefab != null) cg.socketSpawns.Add(new SocketSpawnRule { name = "CajaSuministros", prefab = cratePrefab, probability = 0.22f, yOffset = 0f, alignToBuilding = true });
            if (bagPrefab != null) cg.socketSpawns.Add(new SocketSpawnRule { name = "BolsaResiduos", prefab = bagPrefab, probability = 0.18f, yOffset = 0f, alignToBuilding = true });

            popSpawner.enemyPrefabs = new List<GameObject>(enemyPrefabs);
            popSpawner.maxEnemiesToSpawn = 35;
            popSpawner.minEnemyDistanceFromSpawn = 26f;
            popSpawner.minSpacingBetweenEnemies = 12f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[Prototipo3AssetAndSceneBuilder] Prototipo3.unity reconstruida con VisualModel 0°, Refugio Seguro y 35 enemigos (7 especies).");
        }
    }
}
