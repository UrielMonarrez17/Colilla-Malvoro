using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Prototipo2.Narrative;

namespace Prototipo2
{
    public class StoryWorldBinder : MonoBehaviour
    {
        [Header("Prefabs y Marcadores Visuales")]
        public GameObject luluPrefab;
        public GameObject npcMarkerPrefab;
        public GameObject lockBarricadePrefab;
        public GameObject objectiveBeaconPrefab;

        public GameObject SpawnedLuluInstance { get; private set; }
        public LuluNPCPrototipo2 SpawnedLuluNPC { get; private set; }

        private readonly List<GameObject> spawnedWorldArtifacts = new List<GameObject>();

        public void BindGraphToWorld(StoryGraph graph, CityGeneratorPrototipo2 cityGen)
        {
            if (graph == null || cityGen == null) return;

            LimpiarArtefactos();

            Vector3 spawnPt = cityGen.PlayerSpawnPoint;
            Vector3 craftTablePt = cityGen.CraftTableSpawnPoint;

            // 1. Start Node
            StoryNode startNode = graph.GetNode("node_start");
            if (startNode != null)
            {
                startNode.worldPosition = spawnPt;
            }

            // 2. Encounter Node: Ubicar en una esquina o acera cercana al refugio (~10m a 22m) y spawnear el prefab de Lulú
            StoryNode encounterNode = graph.GetNode("node_encounter");
            if (encounterNode != null)
            {
                Vector3 encounterPos = spawnPt + new Vector3(10f, 0f, 8f);
                encounterPos.y = cityGen.roadHeightY;

                if (cityGen.IntersectionPoints.Count > 0)
                {
                    // Buscar la intersección más cercana al spawn pero no idéntica
                    float bestDist = float.MaxValue;
                    foreach (var pt in cityGen.IntersectionPoints)
                    {
                        float d = Vector3.Distance(pt, spawnPt);
                        if (d > 7f && d < bestDist)
                        {
                            bestDist = d;
                            encounterPos = pt;
                        }
                    }
                }

                encounterPos = AjustarPosicionSuperficieCiudad(encounterPos, cityGen.roadHeightY);
                encounterNode.worldPosition = encounterPos;

                SpawnearLuluEnMundo(encounterPos, spawnPt);
            }

            // 3. Quest Node: Asignar al grupo de loot sockets
            StoryNode questNode = graph.GetNode("node_quest");
            if (questNode != null)
            {
                if (cityGen.LootSpawnPoints.Count > 0)
                {
                    questNode.worldPosition = cityGen.LootSpawnPoints[0];
                }
                else
                {
                    questNode.worldPosition = spawnPt + new Vector3(20f, 0f, 20f);
                }
            }

            // 4. Key Node: Asignar a la mesa de crafteo o zona contigua
            StoryNode keyNode = graph.GetNode("node_key");
            if (keyNode != null)
            {
                keyNode.worldPosition = craftTablePt;
                CrearMarcadorMundo(craftTablePt, Color.yellow, "MesaCrafteo_Key", "Fabricar con [F]");
            }

            // 5. Lock Node: Ubicar a medio camino entre el refugio y el jefe
            StoryNode lockNode = graph.GetNode("node_lock");
            if (lockNode != null)
            {
                Vector3 lockPos = spawnPt + new Vector3(35f, 0f, 35f);
                if (cityGen.IntersectionPoints.Count > 1)
                {
                    lockPos = cityGen.IntersectionPoints[cityGen.IntersectionPoints.Count / 2];
                }
                lockPos = AjustarPosicionSuperficieCiudad(lockPos, cityGen.roadHeightY);
                lockNode.worldPosition = lockPos;
                CrearBarricadaLock(lockPos);
            }

            // 6. Boss Node: Ubicar en el centro de una rotonda (Loop) o la intersección más lejana
            StoryNode bossNode = graph.GetNode("node_boss");
            if (bossNode != null)
            {
                Vector3 bossPos = spawnPt + new Vector3(60f, 0f, 60f);
                if (cityGen.LoopCenterPoints.Count > 0)
                {
                    bossPos = cityGen.LoopCenterPoints[0];
                }
                else if (cityGen.IntersectionPoints.Count > 0)
                {
                    // La más lejana
                    float maxDist = -1f;
                    foreach (var pt in cityGen.IntersectionPoints)
                    {
                        float d = Vector3.Distance(pt, spawnPt);
                        if (d > maxDist)
                        {
                            maxDist = d;
                            bossPos = pt;
                        }
                    }
                }
                bossPos = AjustarPosicionSuperficieCiudad(bossPos, cityGen.roadHeightY);
                bossNode.worldPosition = bossPos;
                CrearMarcadorMundo(bossPos, Color.red, "Boss_Territory", "¡Territorio Feral Alfa!");
            }

            // 7. Exit Node: Ubicar en un callejón sin salida (DeadEnd) lejano o esquina del mapa
            StoryNode exitNode = graph.GetNode("node_exit");
            if (exitNode != null)
            {
                Vector3 exitPos = spawnPt + new Vector3(80f, 0f, 80f);
                if (cityGen.DeadEndPoints.Count > 0)
                {
                    float maxDist = -1f;
                    foreach (var pt in cityGen.DeadEndPoints)
                    {
                        float d = Vector3.Distance(pt, spawnPt);
                        if (d > maxDist)
                        {
                            maxDist = d;
                            exitPos = pt;
                        }
                    }
                }
                exitPos = AjustarPosicionSuperficieCiudad(exitPos, cityGen.roadHeightY);
                exitNode.worldPosition = exitPos;
                CrearMarcadorMundo(exitPos, Color.green, "Madriguera_Exit", "Madriguera Segura (Extracción)");
            }
        }

        private Vector3 AjustarPosicionSuperficieCiudad(Vector3 pos, float roadHeightY)
        {
            pos.y = roadHeightY;
            if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 10.0f, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            Vector3 rayOrigin = new Vector3(pos.x, roadHeightY + 5f, pos.z);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null) continue;
                if (hits[i].point.y >= roadHeightY - 0.5f && hits[i].point.y <= roadHeightY + 1.0f)
                {
                    pos.y = hits[i].point.y;
                    break;
                }
            }

            return pos;
        }

        private void SpawnearLuluEnMundo(Vector3 encounterPos, Vector3 playerSpawnPt)
        {
            GameObject prefabToUse = luluPrefab != null ? luluPrefab : npcMarkerPrefab;

            Vector3 lookDir = playerSpawnPt - encounterPos;
            lookDir.y = 0f;
            Quaternion spawnRot = lookDir.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(lookDir.normalized)
                : Quaternion.identity;

            if (prefabToUse != null)
            {
                SpawnedLuluInstance = Instantiate(prefabToUse, encounterPos, spawnRot, transform);
                SpawnedLuluInstance.name = "Lulu_NPC_Prototipo2";
                SpawnedLuluInstance.tag = "Interactable";
                SpawnedLuluInstance.transform.position = encounterPos;

                NavMeshAgent agent = SpawnedLuluInstance.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled)
                {
                    agent.Warp(encounterPos);
                }

                SpawnedLuluNPC = SpawnedLuluInstance.GetComponent<LuluNPCPrototipo2>();
                if (SpawnedLuluNPC == null)
                {
                    SpawnedLuluNPC = SpawnedLuluInstance.AddComponent<LuluNPCPrototipo2>();
                }
                SpawnedLuluNPC.SetHomeAnchor(encounterPos);

                spawnedWorldArtifacts.Add(SpawnedLuluInstance);
            }

            // Crear un anillo marcador sutil en el suelo debajo de Lulú para resaltar su zona de encuentro
            CrearMarcadorMundo(encounterPos, Color.cyan, "NPC_Lulu", "Hablar con Lulú [F]");
        }

        private void CrearMarcadorMundo(Vector3 pos, Color color, string nombre, string label)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"StoryMarker_{nombre}";
            marker.transform.SetParent(transform);
            marker.transform.position = pos + new Vector3(0, 0.05f, 0);
            marker.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);

            Collider col = marker.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Renderer ren = marker.GetComponent<Renderer>();
            if (ren != null)
            {
                Material m = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                m.color = new Color(color.r, color.g, color.b, 0.45f);
                ren.material = m;
            }

            spawnedWorldArtifacts.Add(marker);
        }

        private void CrearBarricadaLock(Vector3 pos)
        {
            GameObject barricada = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barricada.name = "StoryLock_Barricada";
            barricada.transform.SetParent(transform);
            barricada.transform.position = pos + new Vector3(0, 1.2f, 0);
            barricada.transform.localScale = new Vector3(6f, 2.4f, 1.5f);

            NavMeshObstacle obstacle = barricada.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;

            Renderer ren = barricada.GetComponent<Renderer>();
            if (ren != null)
            {
                Material m = new Material(Shader.Find("Standard"));
                m.color = new Color(0.7f, 0.25f, 0.15f);
                ren.material = m;
            }

            spawnedWorldArtifacts.Add(barricada);
        }

        public void DesactivarBarricadaLock()
        {
            for (int i = 0; i < spawnedWorldArtifacts.Count; i++)
            {
                if (spawnedWorldArtifacts[i] != null && spawnedWorldArtifacts[i].name == "StoryLock_Barricada")
                {
                    spawnedWorldArtifacts[i].SetActive(false);
                }
            }
        }

        public void LimpiarArtefactos()
        {
            for (int i = 0; i < spawnedWorldArtifacts.Count; i++)
            {
                if (spawnedWorldArtifacts[i] != null)
                {
                    Destroy(spawnedWorldArtifacts[i]);
                }
            }
            spawnedWorldArtifacts.Clear();
            SpawnedLuluInstance = null;
            SpawnedLuluNPC = null;
        }
    }
}
