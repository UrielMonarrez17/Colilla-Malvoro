using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Prototipo3;
using Prototipo3.Narrative;
using Prototipo3.UI;

namespace Prototipo3.Editor
{
    public static class TobySpecialTempVerification
    {
        private const string ScenePath = "Assets/Prototipo3/Prototipo3.unity";

        [MenuItem("Feral/Verificar Encuentro Toby en Special Temp", priority = 20)]
        public static void RunVerificationFromMenu()
        {
            RunVerification(interactive: true);
        }

        public static void RunVerification(bool interactive)
        {
            int passed = 0;
            int total = 0;

            void Assert(bool condition, string testName, string details = "")
            {
                total++;
                if (condition)
                {
                    passed++;
                    Debug.Log($"<color=green>[VERIFICATION_TEST] PASS #{total}: {testName}</color>");
                }
                else
                {
                    Debug.LogError($"<color=red>[VERIFICATION_TEST] FAIL #{total}: {testName} - {details}</color>");
                }
            }

            Debug.Log("==========================================================================");
            Debug.Log("[TobySpecialTempVerification] INICIANDO PRUEBAS AUTOMATIZADAS DE TOBY SPECIAL TEMP");
            Debug.Log("==========================================================================");

            // Test 1: StoryGrammarEngine node_a2_toby landmarkId
            StoryGraph graph = StoryGrammarEngine.GenerateStory();
            StoryNode tobyNode = graph.GetNode("node_a2_toby");
            Assert(tobyNode != null, "StoryNode node_a2_toby existe en el grafo");
            Assert(tobyNode != null && tobyNode.landmarkId == "Toby", "node_a2_toby tiene landmarkId == 'Toby'", $"Actual: {tobyNode?.landmarkId}");
            Assert(tobyNode != null && tobyNode.description.Contains("Edificio Especial"), "node_a2_toby menciona Edificio Especial en la descripción");

            // Test 2: SpecialBuildingEntrance component API
            GameObject dummyBuilding = new GameObject("TestBuilding_Dummy");
            SpecialBuildingEntrance entranceComp = dummyBuilding.AddComponent<SpecialBuildingEntrance>();
            entranceComp.Configure("TallerToby", "Taller de Robótica de Toby", "TallerToby_Interior", dummyBuilding.transform);
            Assert(entranceComp.buildingId == "TallerToby", "SpecialBuildingEntrance.buildingId correcto");
            Assert(entranceComp.buildingDisplayName == "Taller de Robótica de Toby", "SpecialBuildingEntrance.buildingDisplayName correcto");
            Assert(entranceComp.targetSceneName == "TallerToby_Interior", "SpecialBuildingEntrance.targetSceneName configurado para futura escena interior");
            Assert(SpecialBuildingEntrance.ActiveEntrances.Contains(entranceComp), "SpecialBuildingEntrance.ActiveEntrances rastrea la instancia");
            UnityEngine.Object.DestroyImmediate(dummyBuilding);
            Assert(!SpecialBuildingEntrance.ActiveEntrances.Contains(entranceComp), "SpecialBuildingEntrance se desregistra al destruirse");

            // Test 3: Carga de Prefab special_temp Variant
            GameObject specialPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimplePoly City - Low Poly Assets/special_temp Variant.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/specialBuildingPrefab.prefab");
            Assert(specialPrefab != null, "Prefab special_temp Variant o specialBuildingPrefab existe y es cargable");
            if (specialPrefab != null)
            {
                CitySocket sock = BuildingSpawner.GetSocket(specialPrefab);
                Assert(sock != null && sock.frontSocket != null, "specialPrefab tiene CitySocket válido con frontSocket");
            }

            // Test 4: Cargar escena Prototipo3 y verificar configuración de CityGeneratorPrototipo3
            bool sceneOpened = false;
            UnityEngine.SceneManagement.Scene currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.path != ScenePath)
            {
                if (System.IO.File.Exists(ScenePath))
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    sceneOpened = true;
                }
            }
            else
            {
                sceneOpened = true;
            }

            Assert(sceneOpened, "Escena Prototipo3.unity abierta exitosamente");

            CityGeneratorPrototipo3 cityGen = UnityEngine.Object.FindAnyObjectByType<CityGeneratorPrototipo3>();
            Assert(cityGen != null, "CityGeneratorPrototipo3 existe en la escena Prototipo3");

            if (cityGen != null)
            {
                // Configurar escena mediante FeralMissionBuilderEditor para asegurar prefabs vinculados
                FeralMissionBuilderEditor.BuildAllStoryPrefabsAndConfigureScene(false);

                Assert(cityGen.specialBuildings != null && cityGen.specialBuildings.Count > 0,
                    "cityGen.specialBuildings tiene al menos un prefab configurado",
                    $"Count: {cityGen.specialBuildings?.Count ?? 0}");

                // Test 5: Ejecutar GenerateCity() en modo Editor
                try
                {
                    cityGen.GenerateCity();

                    Assert(cityGen.SpawnedSpecialBuildings != null && cityGen.SpawnedSpecialBuildings.Count > 0,
                        "SpawnedSpecialBuildings colocó edificios especiales",
                        $"Count: {cityGen.SpawnedSpecialBuildings?.Count ?? 0}");

                    Assert(cityGen.SpawnedTobyBuilding != null, "SpawnedTobyBuilding asignado exitosamente");
                    if (cityGen.SpawnedTobyBuilding != null)
                    {
                        Assert(cityGen.SpawnedTobyBuilding.name.Contains("TallerToby"),
                            "SpawnedTobyBuilding nombrado SpecialBuilding_TallerToby",
                            $"Name: {cityGen.SpawnedTobyBuilding.name}");

                        SpecialBuildingEntrance entranceOnToby = cityGen.SpawnedTobyBuilding.GetComponent<SpecialBuildingEntrance>();
                        Assert(entranceOnToby != null, "SpecialBuildingEntrance adjuntado en SpawnedTobyBuilding");

                        Vector3 tobyPos = cityGen.GetLandmarkPosition("Toby");
                        Vector3 tobyEntrancePos = cityGen.GetLandmarkEntrancePosition("Toby");
                        Assert(tobyPos != cityGen.PlayerSpawnPoint, "GetLandmarkPosition('Toby') devuelve posición válida del edificio");
                        Assert(tobyEntrancePos != Vector3.zero, "GetLandmarkEntrancePosition('Toby') devuelve posición válida de fachada");

                        // Comprobar registro en WorldRegistry
                        Transform tobyReg = WorldRegistry.Get("Toby");
                        Assert(tobyReg != null && tobyReg == cityGen.SpawnedTobyBuilding.transform,
                            "WorldRegistry.Get('Toby') apunta al edificio especial de Toby");
                    }

                    // Test 6: StoryWorldBinder poblamiento
                    StoryWorldBinder binder = UnityEngine.Object.FindAnyObjectByType<StoryWorldBinder>();
                    Assert(binder != null, "StoryWorldBinder_Prototipo3 existe en la escena");
                    if (binder != null)
                    {
                        binder.PopulateLandmarkActorsAndMechanisms(cityGen);

                        // Buscar actor Toby en la escena
                        DualRoleCharacter tobyChar = null;
                        foreach (var drc in DualRoleCharacter.ActiveCharacters)
                        {
                            if (drc != null && string.Equals(drc.characterId, "Toby", StringComparison.OrdinalIgnoreCase))
                            {
                                tobyChar = drc;
                                break;
                            }
                        }

                        Assert(tobyChar != null, "Actor Toby instanciado en el mundo");
                        if (tobyChar != null && cityGen.SpawnedTobyBuilding != null)
                        {
                            float distTobyToBuilding = Vector3.Distance(tobyChar.transform.position, cityGen.SpawnedTobyBuilding.transform.position);
                            float distTobyToEscuela = cityGen.SpawnedEscuela != null
                                ? Vector3.Distance(tobyChar.transform.position, cityGen.SpawnedEscuela.transform.position)
                                : 999f;

                            Assert(distTobyToBuilding < 25f,
                                "Toby está ubicado en el edificio especial (distancia <= 25m)",
                                $"Dist: {distTobyToBuilding:F2}m");

                            Assert(distTobyToEscuela > 5f,
                                "Toby está desacoplado de la Escuela Secundaria Bellavista",
                                $"Distancia a Escuela: {distTobyToEscuela:F2}m");

                            SpecialBuildingEntrance tobyEntrance = cityGen.SpawnedTobyBuilding.GetComponent<SpecialBuildingEntrance>();
                            Assert(tobyEntrance != null && tobyEntrance.residentNPC == tobyChar,
                                "SpecialBuildingEntrance.residentNPC vinculado a Toby");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Assert(false, "Excepción durante GenerateCity", ex.ToString());
                }
            }

            Debug.Log($"==========================================================================");
            Debug.Log($"[TobySpecialTempVerification] RESULTADO: {passed}/{total} PRUEBAS SUPERADAS");
            Debug.Log($"==========================================================================");

            if (interactive && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Verificación Toby Special Temp",
                    $"Se ejecutaron las pruebas automatizadas con éxito:\n\n{passed} de {total} pruebas pasadas.",
                    "OK");
            }
        }
    }
}
