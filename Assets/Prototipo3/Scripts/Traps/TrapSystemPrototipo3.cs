using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Prototipo3
{
    /// <summary>
    /// Sistema de Fabricación y Uso Táctico de las 3 Trampas de Prototipo 3:
    /// 1 (TipoTrampa.Puas)   -> Señuelo Acústico (Costo: 2 Metal + 1 Desperdicios)
    /// 2 (TipoTrampa.Red)    -> Bomba de Polen de Helecho (Costo: 2 Semillas + 1 Madera)
    /// 3 (TipoTrampa.Madera) -> Descargador Estático de Goma (Costo: 2 Goma + 1 Metal + 1 Madera)
    /// </summary>
    public class TrapSystemPrototipo3 : MonoBehaviour
    {
        public static TrapSystemPrototipo3 Instance { get; private set; }

        [Header("Prefabs Opcionales de Trampas")]
        public GameObject acousticDecoyPrefab;
        public GameObject pollenCloudPrefab;
        public GameObject staticDischargePrefab;

        [Header("Modelos 3D de Trampas (Blender)")]
        public GameObject spikesTrapPrefab;
        public GameObject netTrapPrefab;
        public GameObject mouseTrapPrefab;

        private const string SpikesResourcePath = "Traps/SM_Trap_Spikes";
        private const string NetResourcePath = "Traps/SM_Trap_Net";
        private const string MouseTrapResourcePath = "Traps/SM_Trap_MouseTrap";

        [Header("Estado de Buffs Activos")]
        [SerializeField] private float pollenTimeRemaining = 0f;
        public static bool IsPollenCloudActive => Instance != null && Instance.pollenTimeRemaining > 0f;
        public static float PollenRemainingSeconds => Instance != null ? Mathf.Max(0f, Instance.pollenTimeRemaining) : 0f;

        public static event Action<TipoTrampa> OnProto3TrapCrafted;
        public static event Action<TipoTrampa> OnProto3TrapDeployed;

        private GameObject activePollenVisual;
        private Transform trackedPlayerTransform;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this && Application.isPlaying) Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        public static TrapSystemPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("TrapSystemPrototipo3");
            Instance = go.AddComponent<TrapSystemPrototipo3>();
            return Instance;
        }

        private void Update()
        {
            if (pollenTimeRemaining > 0f)
            {
                pollenTimeRemaining -= Time.deltaTime;
                if (activePollenVisual != null && trackedPlayerTransform != null)
                {
                    activePollenVisual.transform.position = trackedPlayerTransform.position + Vector3.up * 0.35f;
                    activePollenVisual.transform.Rotate(0f, 95f * Time.deltaTime, 0f);
                }

                if (pollenTimeRemaining <= 0f)
                {
                    pollenTimeRemaining = 0f;
                    if (activePollenVisual != null)
                    {
                        Destroy(activePollenVisual);
                    }
                    Debug.Log("[TrapSystemPrototipo3] La nube de la Bomba de Polen se ha disipado.");
                }
            }
        }

        // ===================================================================================
        //  RECETAS DE LA MESA DE FABRICACIÓN (PASO 5)
        // ===================================================================================

        public static string GetTrapDisplayName(TipoTrampa t)
        {
            switch (t)
            {
                case TipoTrampa.Puas: return "Trampa de Púas";
                case TipoTrampa.Red: return "Trampa de Red";
                case TipoTrampa.Madera: return "Trampa de Madera (Barricada)";
                default: return "Ninguna";
            }
        }

        public static string GetTrapRecipeText(TipoTrampa t)
        {
            switch (t)
            {
                case TipoTrampa.Puas: return "2 Metal + 1 Madera";
                case TipoTrampa.Red: return "1 Goma + 1 Desperdicios";
                case TipoTrampa.Madera: return "2 Madera";
                default: return "";
            }
        }

        public static string GetTrapDescription(TipoTrampa t)
        {
            switch (t)
            {
                case TipoTrampa.Puas:
                    return "Tecla [1]: Reduce al 50% la velocidad enemiga por 6 s con chispas eléctricas y sobrecarga paneles de arena.";
                case TipoTrampa.Red:
                    return "Tecla [2]: Red en el suelo que inmoviliza al enemigo por 5 s y anula su olfato / detección de proximidad.";
                case TipoTrampa.Madera:
                    return "Tecla [3]: Barricada física por 10 s con NavMeshObstacle que bloquea pasillos y fuerza el recálculo de ruta del enemigo.";
                default:
                    return "";
            }
        }

        public static bool CanCraftTrap(TipoTrampa t)
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return false;

            switch (t)
            {
                case TipoTrampa.Puas:
                    // Costo principal: 2 Metal + 1 Madera (con fallback a 1 Desperdicios si tiene receta antigua)
                    return (inv.HasEnoughItem(ItemType.Metal, 2) && inv.HasEnoughItem(ItemType.Madera, 1)) ||
                           (inv.HasEnoughItem(ItemType.Metal, 2) && inv.HasEnoughItem(ItemType.Desperdicios, 1));

                case TipoTrampa.Red:
                    // Costo principal: 1 Goma + 1 Desperdicios (con soporte adicional a 2 Semillas + 1 Madera)
                    return (inv.HasEnoughItem(ItemType.Goma, 1) && inv.HasEnoughItem(ItemType.Desperdicios, 1)) ||
                           (inv.HasEnoughItem(ItemType.Semillas, 2) && inv.HasEnoughItem(ItemType.Madera, 1));

                case TipoTrampa.Madera:
                    // Costo principal: 2 Madera
                    return inv.HasEnoughItem(ItemType.Madera, 2) ||
                           (inv.HasEnoughItem(ItemType.Goma, 2) && inv.HasEnoughItem(ItemType.Metal, 1) && inv.HasEnoughItem(ItemType.Madera, 1));

                default:
                    return false;
            }
        }

        public static bool CraftTrap(TipoTrampa t)
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null || !CanCraftTrap(t))
            {
                Debug.LogWarning($"[TrapSystemPrototipo3] Faltan materiales para fabricar {GetTrapDisplayName(t)} ({GetTrapRecipeText(t)}).");
                return false;
            }

            switch (t)
            {
                case TipoTrampa.Puas:
                    if (inv.HasEnoughItem(ItemType.Metal, 2) && inv.HasEnoughItem(ItemType.Madera, 1))
                    {
                        inv.RemoveItem(ItemType.Metal, 2);
                        inv.RemoveItem(ItemType.Madera, 1);
                    }
                    else
                    {
                        inv.RemoveItem(ItemType.Metal, 2);
                        inv.RemoveItem(ItemType.Desperdicios, 1);
                    }
                    inv.AddTrampa(TipoTrampa.Puas);
                    break;

                case TipoTrampa.Red:
                    if (inv.HasEnoughItem(ItemType.Goma, 1) && inv.HasEnoughItem(ItemType.Desperdicios, 1))
                    {
                        inv.RemoveItem(ItemType.Goma, 1);
                        inv.RemoveItem(ItemType.Desperdicios, 1);
                    }
                    else
                    {
                        inv.RemoveItem(ItemType.Semillas, 2);
                        inv.RemoveItem(ItemType.Madera, 1);
                    }
                    inv.AddTrampa(TipoTrampa.Red);
                    break;

                case TipoTrampa.Madera:
                    if (inv.HasEnoughItem(ItemType.Madera, 2))
                    {
                        inv.RemoveItem(ItemType.Madera, 2);
                    }
                    else
                    {
                        inv.RemoveItem(ItemType.Goma, 2);
                        inv.RemoveItem(ItemType.Metal, 1);
                        inv.RemoveItem(ItemType.Madera, 1);
                    }
                    inv.AddTrampa(TipoTrampa.Madera);
                    break;
            }

            Debug.Log($"[TrapSystemPrototipo3] ¡Fabricado con éxito: {GetTrapDisplayName(t)}!");
            OnProto3TrapCrafted?.Invoke(t);

            return true;
        }

        // ===================================================================================
        //  DESPLIEGUE ACTIVO CON TECLAS RÁPIDAS 1, 2 Y 3
        // ===================================================================================

        public bool TryDeployTrap(TipoTrampa tipo, Transform playerTransform)
        {
            if (tipo == TipoTrampa.Ninguna || playerTransform == null) return false;

            InventoryManager inv = InventoryManager.Instance;
            if (inv == null || !inv.HasTrampa(tipo))
            {
                Debug.Log($"[TrapSystemPrototipo3] Seleccionada {GetTrapDisplayName(tipo)}, pero tienes 0 unidades. Fabrícala en la mesa de la Base.");
                return false;
            }

            inv.RemoveTrampa(tipo);
            trackedPlayerTransform = playerTransform;

            switch (tipo)
            {
                case TipoTrampa.Puas:
                    DeploySpikesTrap(playerTransform);
                    break;

                case TipoTrampa.Red:
                    DeployNetTrap(playerTransform);
                    break;

                case TipoTrampa.Madera:
                    DeployWoodBarricade(playerTransform);
                    break;
            }

            OnProto3TrapDeployed?.Invoke(tipo);

            return true;
        }

        // ===================================================================================
        //  LÓGICA FÍSICA Y EFECTOS TÁCTICOS DE LAS 3 TRAMPAS
        // ===================================================================================

        private static GameObject ResolveTrapModel(GameObject assigned, string resPath)
        {
            return assigned != null ? assigned : Resources.Load<GameObject>(resPath);
        }

        private static Vector3 GetGroundPoint(Vector3 around, Transform ignoreRoot)
        {
            RaycastHit[] hits = Physics.RaycastAll(around + Vector3.up * 1.6f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore);
            bool found = false;
            float bestDist = float.MaxValue;
            Vector3 bestPoint = Vector3.zero;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (ignoreRoot != null && (hit.collider.transform == ignoreRoot || hit.collider.transform.IsChildOf(ignoreRoot))) continue;
                if (hit.collider.GetComponentInParent<EnemyAI>() != null) continue;
                if (hit.normal.y < 0.55f) continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestPoint = hit.point;
                    found = true;
                }
            }

            if (found) return bestPoint;

            float fallbackY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : around.y;
            return new Vector3(around.x, fallbackY, around.z);
        }

        private static GameObject SpawnTrapVisual(GameObject src, Vector3 groundPos, Quaternion worldYaw, string rootName)
        {
            GameObject root = new GameObject(rootName);
            root.transform.position = groundPos;
            root.transform.rotation = worldYaw;

            GameObject vis = Instantiate(src);
            vis.name = "Model";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = Vector3.zero;

            Collider[] embeddedCols = vis.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < embeddedCols.Length; i++)
            {
                if (embeddedCols[i] != null) Destroy(embeddedCols[i]);
            }

            Renderer[] rens = vis.GetComponentsInChildren<Renderer>(true);
            EnsureUrpLitMaterials(rens);

            if (rens.Length > 0)
            {
                Bounds combined = rens[0].bounds;
                for (int i = 1; i < rens.Length; i++)
                {
                    if (rens[i] != null) combined.Encapsulate(rens[i].bounds);
                }
                float deltaY = (root.transform.position.y + 0.01f) - combined.min.y;
                vis.transform.position += new Vector3(0f, deltaY, 0f);
            }

            return root;
        }

        private static void EnsureUrpLitMaterials(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return;
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLit == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null || mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name == "Standard")
                    {
                        Color col = (mat != null && mat.HasProperty("_Color")) ? mat.color : Color.gray;
                        Material replacement = new Material(urpLit);
                        replacement.color = col;
                        if (replacement.HasProperty("_BaseColor")) replacement.SetColor("_BaseColor", col);
                        mats[m] = replacement;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        public void DeployWoodBarricade(Transform playerTransform)
        {
            Vector3 approxPos = playerTransform.position + playerTransform.forward * 1.5f;
            Quaternion yaw = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);
            GameObject mouseModel = ResolveTrapModel(mouseTrapPrefab, MouseTrapResourcePath);
            Vector3 spawnPos;
            GameObject barricade;

            if (mouseModel != null)
            {
                spawnPos = GetGroundPoint(approxPos, playerTransform);
                barricade = SpawnTrapVisual(mouseModel, spawnPos, yaw, "Barricada_Madera_Activa_10s");

                BoxCollider bc = barricade.AddComponent<BoxCollider>();
                bc.size = new Vector3(2.4f, 1.0f, 1.3f);
                bc.center = new Vector3(0f, 0.5f, 0f);

                NavMeshObstacle obstacle = barricade.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(2.4f, 1.0f, 1.3f);
                obstacle.center = new Vector3(0f, 0.5f, 0f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
            }
            else
            {
                spawnPos = approxPos;
                if (CityGeneratorPrototipo3.Instance != null)
                {
                    spawnPos.y = CityGeneratorPrototipo3.Instance.roadHeightY + 0.5f;
                }

                barricade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barricade.name = "Barricada_Madera_Activa_10s";
                barricade.transform.position = spawnPos;
                barricade.transform.rotation = playerTransform.rotation;
                barricade.transform.localScale = new Vector3(2.4f, 1.2f, 0.45f);

                Renderer r = barricade.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                    if (sh != null)
                    {
                        Material m = new Material(sh);
                        Color c = new Color(0.48f, 0.28f, 0.12f, 1f);
                        m.color = c;
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                        r.material = m;
                    }
                }

                NavMeshObstacle obstacle = barricade.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(2.4f, 1.2f, 0.45f);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
            }

            // Recalcular ruta de todos los enemigos cercanos (radio 14m)
            EnemyAIPrototipo3[] enemies = FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAIPrototipo3 e = enemies[i];
                if (e != null && e.isActiveAndEnabled && Vector3.Distance(spawnPos, e.transform.position) <= 14f)
                {
                    e.ApplyWoodBarricadeRepath(spawnPos, 10f);
                }
            }
            NotifyBaseEnemiesInRadius(spawnPos, 14f, TipoTrampa.Madera);

            // Destruir la barricada tras 10 segundos
            Destroy(barricade, 10f);
            Debug.Log("[TrapSystemPrototipo3] ¡TRAMPA DE MADERA desplegada! Barricada física activa con NavMeshObstacle durante 10 s.");
        }

        public void DeployNetTrap(Transform playerTransform)
        {
            Vector3 center = playerTransform.position;
            float radius = 8f;

            // 1. Inmovilizar por 5s y apagar detección extrema a los enemigos cercanos
            EnemyAIPrototipo3[] enemies = FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAIPrototipo3 e = enemies[i];
                if (e != null && e.isActiveAndEnabled && Vector3.Distance(center, e.transform.position) <= radius)
                {
                    e.ApplyNetImmobilization(5f);
                    Debug.Log($"[TrapSystemPrototipo3] ¡TRAMPA DE RED inmovilizó a {e.name} por 5 segundos y anuló su proximidad extrema!");
                }
            }
            NotifyBaseEnemiesInRadius(center, radius, TipoTrampa.Red);

            // 1b. Desplegar el modelo 3D de la red en el suelo durante los 5 s de inmovilización
            GameObject netModel = ResolveTrapModel(netTrapPrefab, NetResourcePath);
            if (netModel != null)
            {
                Quaternion yaw = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);
                Vector3 groundPos = GetGroundPoint(center, playerTransform);
                GameObject netVis = SpawnTrapVisual(netModel, groundPos, yaw, "Trampa2_Red_Activa_5s");
                Destroy(netVis, 5f);
            }

            // 2. Activar la nube de polen protectora de 15s para Kilo
            DeployPollenBomb(playerTransform);
        }

        public void DeploySpikesTrap(Transform playerTransform)
        {
            Vector3 center = playerTransform.position;
            float radius = 8f;

            // 1. Reducir al 50% la velocidad de los enemigos en un radio de 8m por 6s con chispas
            EnemyAIPrototipo3[] enemies = FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAIPrototipo3 e = enemies[i];
                if (e != null && e.isActiveAndEnabled && Vector3.Distance(center, e.transform.position) <= radius)
                {
                    e.ApplySpikeSlowAndSparks(0.5f, 6f);
                    Debug.Log($"[TrapSystemPrototipo3] ¡TRAMPA DE PÚAS ralentizó a {e.name} al 50% por 6 s con chispas eléctricas!");
                }
            }
            NotifyBaseEnemiesInRadius(center, radius, TipoTrampa.Puas);

            // 2. Emitir el señuelo acústico de distracción por compatibilidad
            DeployAcousticDecoy(playerTransform);
        }

        private static void NotifyBaseEnemiesInRadius(Vector3 origin, float radius, TipoTrampa tipo)
        {
            EnemyAI[] genericEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            for (int i = 0; i < genericEnemies.Length; i++)
            {
                EnemyAI e = genericEnemies[i];
                if (e == null || !e.isActiveAndEnabled || e is EnemyAIPrototipo3) continue;
                if (Vector3.Distance(origin, e.transform.position) <= radius)
                {
                    e.RecibirTrampa(tipo, origin);
                }
            }
        }

        private void DeployAcousticDecoy(Transform playerTransform)
        {
            Vector3 approxPos = playerTransform.position + playerTransform.forward * 1.2f;
            Quaternion yaw = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);
            GameObject spikesModel = ResolveTrapModel(spikesTrapPrefab, SpikesResourcePath);
            GameObject decoyObj;

            if (spikesModel != null)
            {
                Vector3 groundPos = GetGroundPoint(approxPos, playerTransform);
                decoyObj = SpawnTrapVisual(spikesModel, groundPos, yaw, "Trampa1_Puas_Activa");
            }
            else if (acousticDecoyPrefab != null)
            {
                Vector3 spawnPos = approxPos;
                if (CityGeneratorPrototipo3.Instance != null) spawnPos.y = CityGeneratorPrototipo3.Instance.roadHeightY;
                decoyObj = Instantiate(acousticDecoyPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                Vector3 spawnPos = approxPos;
                if (CityGeneratorPrototipo3.Instance != null) spawnPos.y = CityGeneratorPrototipo3.Instance.roadHeightY;
                decoyObj = new GameObject("Trampa1_SenueloAcustico_Active");
                decoyObj.transform.position = spawnPos;
            }

            AcousticDecoyObject decoyComp = decoyObj.GetComponent<AcousticDecoyObject>();
            if (decoyComp == null)
            {
                decoyComp = decoyObj.AddComponent<AcousticDecoyObject>();
            }
            decoyComp.duration = 8f;
            decoyComp.attractionRadius = 18f;

            Debug.Log("[TrapSystemPrototipo3] ¡Señuelo Acústico activo durante 8 s!");
        }

        private void DeployPollenBomb(Transform playerTransform)
        {
            pollenTimeRemaining = 15f;

            if (activePollenVisual != null)
            {
                Destroy(activePollenVisual);
            }

            if (pollenCloudPrefab != null)
            {
                activePollenVisual = Instantiate(pollenCloudPrefab, playerTransform.position + Vector3.up * 0.35f, Quaternion.identity);
            }
            else
            {
                activePollenVisual = new GameObject("Trampa2_NubePolen_Active");
                activePollenVisual.transform.position = playerTransform.position + Vector3.up * 0.35f;

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "PollenAura";
                sphere.transform.SetParent(activePollenVisual.transform, false);
                sphere.transform.localScale = new Vector3(2.4f, 1.3f, 2.4f);
                Collider col = sphere.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer ren = sphere.GetComponent<Renderer>();
                if (ren != null)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    Material m = new Material(sh);
                    Color colPollen = new Color(0.2f, 0.95f, 0.35f, 0.32f);
                    m.color = colPollen;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", colPollen);
                    if (m.HasProperty("_Surface"))
                    {
                        m.SetFloat("_Surface", 1f);
                        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        m.SetInt("_ZWrite", 0);
                        m.renderQueue = 3600;
                    }
                    ren.material = m;
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            Destroy(activePollenVisual, 1f);

            Debug.Log("[TrapSystemPrototipo3] ¡Aura de Polen activada! Olfato desactivado y visión enemiga al 50% por 15 s.");
        }

        private void DeployStaticDischarger(Transform playerTransform)
        {
            Vector3 center = playerTransform.position;
            float radius = 8f;

            GameObject pulseObj;
            if (staticDischargePrefab != null)
            {
                pulseObj = Instantiate(staticDischargePrefab, center, Quaternion.identity);
            }
            else
            {
                pulseObj = new GameObject("Trampa3_DescargaEstatica_Active");
                pulseObj.transform.position = center;
            }

            StaticDischargeEffect effect = pulseObj.GetComponent<StaticDischargeEffect>();
            if (effect == null)
            {
                effect = pulseObj.AddComponent<StaticDischargeEffect>();
            }
            effect.radius = radius;

            // Paralizar por 5 segundos al enemigo más cercano en 8m
            EnemyAIPrototipo3[] enemies = FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None);
            EnemyAIPrototipo3 closestEnemy = null;
            float bestDist = radius;

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAIPrototipo3 e = enemies[i];
                if (e == null || !e.isActiveAndEnabled) continue;
                float d = Vector3.Distance(center, e.transform.position);
                if (d <= bestDist)
                {
                    bestDist = d;
                    closestEnemy = e;
                }
            }

            if (closestEnemy != null)
            {
                closestEnemy.ApplyStaticParalysis(5f);
            }
        }
    }
}

