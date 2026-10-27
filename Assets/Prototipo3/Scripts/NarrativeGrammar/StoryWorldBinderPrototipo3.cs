using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Prototipo3.PCG;
using Prototipo3.Combat;

namespace Prototipo3.Narrative
{
    public class StoryWorldBinderPrototipo3 : MonoBehaviour
    {
        [Header("Prefabs de Actores y Props")]
        public GameObject luluPrefab;
        public GameObject baronPrefab;
        public GameObject deerMiniBossPrefab;
        public GameObject reactorBossPrefab;
        public GameObject lockBarricadePrefab;
        public GameObject filtroCarmesiPrefab;

        public GameObject SpawnedLuluInstance { get; private set; }
        public LuluNPCPrototipo3 SpawnedLuluNPC { get; private set; }
        public GameObject SpawnedBaronInstance { get; private set; }
        public BaronAlquitranNPCPrototipo3 SpawnedBaronNPC { get; private set; }
        public GameObject SpawnedMiniBossInstance { get; private set; }
        public MiniBossCiervoPrototipo3 SpawnedMiniBoss { get; private set; }
        public GameObject SpawnedReactorInstance { get; private set; }
        public BossReactorPrototipo3 SpawnedReactor { get; private set; }
        public GameObject SpawnedLockBarricadeInstance { get; private set; }

        private readonly List<GameObject> spawnedWorldArtifacts = new List<GameObject>();

        public void BindGraphToWorld(StoryGraphPrototipo3 graph, CityGeneratorPrototipo3 cityGen)
        {
            if (graph == null || cityGen == null) return;

            LimpiarArtefactos();

            Vector3 spawnPt = cityGen.PlayerSpawnPoint;

            // 1. Start Node
            var startNode = graph.GetNode("node_start");
            if (startNode != null)
            {
                startNode.worldPosition = spawnPt;
            }

            // 2. Encounter Node: Lulú
            var luluNode = graph.GetNode("node_lulu");
            if (luluNode != null)
            {
                Vector3 luluPos = spawnPt + new Vector3(12f, 0f, 10f);
                luluPos.y = cityGen.roadHeightY;

                if (cityGen.IntersectionPoints.Count > 0)
                {
                    float bestDist = float.MaxValue;
                    foreach (var pt in cityGen.IntersectionPoints)
                    {
                        float d = Vector3.Distance(pt, spawnPt);
                        if (d > 8f && d < bestDist)
                        {
                            bestDist = d;
                            luluPos = pt;
                        }
                    }
                }

                luluNode.worldPosition = luluPos;
                SpawnearLulu(luluPos);
            }

            // 3. SideQuest Node: Chimenea Menor
            var sideNode = graph.GetNode("node_side_chimney");
            if (sideNode != null)
            {
                Vector3 chimneyPos = spawnPt + new Vector3(18f, 0f, -12f);
                if (cityGen.DeadEndPoints.Count > 0)
                {
                    chimneyPos = cityGen.DeadEndPoints[0];
                }
                chimneyPos.y = cityGen.roadHeightY;
                sideNode.worldPosition = chimneyPos;
                CrearMarcadorMundo(chimneyPos, new Color(0.9f, 0.4f, 0.1f), "Chimenea_Purga", "Válvula de Chimenea");
            }

            // 4. Quest Node: 3 Filtros Carmesí
            var filtersNode = graph.GetNode("node_loot_filters");
            if (filtersNode != null)
            {
                Vector3 filterBasePos = spawnPt + new Vector3(25f, 0f, 20f);
                if (cityGen.LootSpawnPoints.Count > 0)
                {
                    filterBasePos = cityGen.LootSpawnPoints[0];
                }
                filtersNode.worldPosition = filterBasePos;

                // Spawnear 3 cilindros interactivos de Filtro Carmesí
                for (int i = 0; i < 3; i++)
                {
                    Vector3 fPos = filterBasePos + new Vector3((i - 1) * 8f, 0f, ((i % 2) == 0 ? 4f : -4f));
                    fPos.y = cityGen.roadHeightY;
                    SpawnearFiltroCarmesi(fPos, i + 1);
                }
            }

            // 5. MiniBoss Node: Ciervo Feral Alfa
            var miniBossNode = graph.GetNode("node_miniboss");
            if (miniBossNode != null)
            {
                Vector3 mbPos = spawnPt + new Vector3(45f, 0f, 35f);
                if (cityGen.LoopCenterPoints.Count > 0)
                {
                    mbPos = cityGen.LoopCenterPoints[0];
                }
                else if (cityGen.IntersectionPoints.Count > 2)
                {
                    mbPos = cityGen.IntersectionPoints[cityGen.IntersectionPoints.Count / 3];
                }
                mbPos.y = cityGen.roadHeightY;
                miniBossNode.worldPosition = mbPos;
                SpawnearMiniBoss(mbPos);
            }

            // 6. Encounter Node: Barón de Alquitrán (Vertedero)
            var baronNode = graph.GetNode("node_baron");
            if (baronNode != null)
            {
                Vector3 baronPos = spawnPt + new Vector3(60f, 0f, 20f);
                if (cityGen.DeadEndPoints.Count > 1)
                {
                    baronPos = cityGen.DeadEndPoints[cityGen.DeadEndPoints.Count - 1];
                }
                baronPos.y = cityGen.roadHeightY;
                baronNode.worldPosition = baronPos;
                SpawnearBaron(baronPos);
            }

            // 7. Key Node: Tarjeta de Acceso
            var keyNode = graph.GetNode("node_key");
            if (keyNode != null && baronNode != null)
            {
                keyNode.worldPosition = baronNode.worldPosition + new Vector3(1.5f, 0f, 1.5f);
            }

            // 8. Lock Node: Barricada Electrificada Torre Malvoro
            var lockNode = graph.GetNode("node_lock");
            if (lockNode != null)
            {
                Vector3 lockPos = spawnPt + new Vector3(70f, 0f, 55f);
                if (cityGen.IntersectionPoints.Count > 3)
                {
                    lockPos = cityGen.IntersectionPoints[cityGen.IntersectionPoints.Count - 2];
                }
                lockPos.y = cityGen.roadHeightY;
                lockNode.worldPosition = lockPos;
                CrearBarricadaLock(lockPos);
            }

            // 9. Final Boss Node: Reactor Carmesí
            var finalBossNode = graph.GetNode("node_final_boss");
            if (finalBossNode != null)
            {
                Vector3 reactorPos = spawnPt + new Vector3(85f, 0f, 75f);
                if (cityGen.DeadEndPoints.Count > 2)
                {
                    reactorPos = cityGen.DeadEndPoints[cityGen.DeadEndPoints.Count - 2];
                }
                reactorPos.y = cityGen.roadHeightY;
                finalBossNode.worldPosition = reactorPos;
                SpawnearReactorBoss(reactorPos);
            }

            // 10. Exit Node
            var exitNode = graph.GetNode("node_exit");
            if (exitNode != null && finalBossNode != null)
            {
                exitNode.worldPosition = finalBossNode.worldPosition + new Vector3(8f, 0f, 8f);
            }
        }

        private void SpawnearLulu(Vector3 pos)
        {
            if (luluPrefab != null)
            {
                SpawnedLuluInstance = Instantiate(luluPrefab, pos, Quaternion.identity, transform);
            }
            else
            {
                // Crear objeto visual estilizado para Lulú
                SpawnedLuluInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                SpawnedLuluInstance.name = "LuluNPC_Visual";
                SpawnedLuluInstance.transform.SetParent(transform);
                SpawnedLuluInstance.transform.position = pos + Vector3.up * 0.45f;
                SpawnedLuluInstance.transform.localScale = new Vector3(0.5f, 0.45f, 0.7f);

                var ren = SpawnedLuluInstance.GetComponent<Renderer>();
                if (ren != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.95f, 0.90f, 0.85f); // Color siamés crema
                    ren.material = mat;
                }
            }

            SpawnedLuluNPC = SpawnedLuluInstance.GetComponent<LuluNPCPrototipo3>();
            if (SpawnedLuluNPC == null)
            {
                SpawnedLuluNPC = SpawnedLuluInstance.AddComponent<LuluNPCPrototipo3>();
            }
            spawnedWorldArtifacts.Add(SpawnedLuluInstance);
        }

        private void SpawnearBaron(Vector3 pos)
        {
            if (baronPrefab != null)
            {
                SpawnedBaronInstance = Instantiate(baronPrefab, pos, Quaternion.identity, transform);
            }
            else
            {
                SpawnedBaronInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SpawnedBaronInstance.name = "BaronNPC_Visual";
                SpawnedBaronInstance.transform.SetParent(transform);
                SpawnedBaronInstance.transform.position = pos + Vector3.up * 0.6f;
                SpawnedBaronInstance.transform.localScale = new Vector3(1.2f, 1.2f, 1.8f);

                var ren = SpawnedBaronInstance.GetComponent<Renderer>();
                if (ren != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.25f, 0.18f, 0.12f); // Marrón alquitrán
                    ren.material = mat;
                }
            }

            SpawnedBaronNPC = SpawnedBaronInstance.GetComponent<BaronAlquitranNPCPrototipo3>();
            if (SpawnedBaronNPC == null)
            {
                SpawnedBaronNPC = SpawnedBaronInstance.AddComponent<BaronAlquitranNPCPrototipo3>();
            }
            spawnedWorldArtifacts.Add(SpawnedBaronInstance);
        }

        private void SpawnearMiniBoss(Vector3 pos)
        {
            if (deerMiniBossPrefab != null)
            {
                SpawnedMiniBossInstance = Instantiate(deerMiniBossPrefab, pos, Quaternion.identity, transform);
            }
            else
            {
                SpawnedMiniBossInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SpawnedMiniBossInstance.name = "MiniBossCiervo_Visual";
                SpawnedMiniBossInstance.transform.SetParent(transform);
                SpawnedMiniBossInstance.transform.position = pos + Vector3.up * 1.0f;
                SpawnedMiniBossInstance.transform.localScale = new Vector3(1.4f, 2.0f, 2.2f);

                var ren = SpawnedMiniBossInstance.GetComponent<Renderer>();
                if (ren != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.65f, 0.20f, 0.15f); // Rojo feral
                    ren.material = mat;
                }

                var rb = SpawnedMiniBossInstance.GetComponent<Rigidbody>();
                if (rb == null) rb = SpawnedMiniBossInstance.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            SpawnedMiniBoss = SpawnedMiniBossInstance.GetComponent<MiniBossCiervoPrototipo3>();
            if (SpawnedMiniBoss == null)
            {
                SpawnedMiniBoss = SpawnedMiniBossInstance.AddComponent<MiniBossCiervoPrototipo3>();
            }
            spawnedWorldArtifacts.Add(SpawnedMiniBossInstance);
        }

        private void SpawnearReactorBoss(Vector3 pos)
        {
            if (reactorBossPrefab != null)
            {
                SpawnedReactorInstance = Instantiate(reactorBossPrefab, pos, Quaternion.identity, transform);
            }
            else
            {
                SpawnedReactorInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                SpawnedReactorInstance.name = "BossReactor_Visual";
                SpawnedReactorInstance.transform.SetParent(transform);
                SpawnedReactorInstance.transform.position = pos + Vector3.up * 2.0f;
                SpawnedReactorInstance.transform.localScale = new Vector3(3.5f, 4.0f, 3.5f);

                var ren = SpawnedReactorInstance.GetComponent<Renderer>();
                if (ren != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.85f, 0.12f, 0.12f); // Núcleo carmesí
                    ren.material = mat;
                }
            }

            SpawnedReactor = SpawnedReactorInstance.GetComponent<BossReactorPrototipo3>();
            if (SpawnedReactor == null)
            {
                SpawnedReactor = SpawnedReactorInstance.AddComponent<BossReactorPrototipo3>();
            }
            spawnedWorldArtifacts.Add(SpawnedReactorInstance);
        }

        private void SpawnearFiltroCarmesi(Vector3 pos, int index)
        {
            var filtro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            filtro.name = $"Drop_FiltroCarmesi_{index}";
            filtro.transform.SetParent(transform);
            filtro.transform.position = pos + Vector3.up * 0.25f;
            filtro.transform.localScale = new Vector3(0.6f, 0.35f, 0.6f);

            var ren = filtro.GetComponent<Renderer>();
            if (ren != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = new Color(1.0f, 0.15f, 0.2f);
                ren.material = mat;
            }

            var col = filtro.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var collector = filtro.AddComponent<FiltroCarmesiItemPrototipo3>();
            spawnedWorldArtifacts.Add(filtro);
        }

        private void CrearBarricadaLock(Vector3 pos)
        {
            SpawnedLockBarricadeInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SpawnedLockBarricadeInstance.name = "Lock_BarricadaElectrificada_Prototipo3";
            SpawnedLockBarricadeInstance.transform.SetParent(transform);
            SpawnedLockBarricadeInstance.transform.position = pos + Vector3.up * 1.5f;
            SpawnedLockBarricadeInstance.transform.localScale = new Vector3(8.0f, 3.0f, 1.2f);

            var ren = SpawnedLockBarricadeInstance.GetComponent<Renderer>();
            if (ren != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = new Color(0.9f, 0.75f, 0.1f);
                ren.material = mat;
            }

            var obs = SpawnedLockBarricadeInstance.AddComponent<NavMeshObstacle>();
            obs.carving = true;

            spawnedWorldArtifacts.Add(SpawnedLockBarricadeInstance);
        }

        public void DesactivarBarricadaLock()
        {
            if (SpawnedLockBarricadeInstance != null)
            {
                SpawnedLockBarricadeInstance.SetActive(false);
                Debug.Log("[StoryWorldBinderPrototipo3] ¡Barricada de Torre Malvoro desactivada!");
            }
        }

        private void CrearMarcadorMundo(Vector3 pos, Color col, string name, string label)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(transform);
            marker.transform.position = pos + Vector3.up * 0.1f;
            marker.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);

            var ren = marker.GetComponent<Renderer>();
            if (ren != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = col;
                ren.material = mat;
            }

            Destroy(marker.GetComponent<Collider>());
            spawnedWorldArtifacts.Add(marker);
        }

        public void LimpiarArtefactos()
        {
            foreach (var art in spawnedWorldArtifacts)
            {
                if (art != null)
                {
                    if (Application.isPlaying) Destroy(art);
                    else DestroyImmediate(art);
                }
            }
            spawnedWorldArtifacts.Clear();
        }
    }
}
