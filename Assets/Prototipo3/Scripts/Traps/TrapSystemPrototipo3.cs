using System;
using System.Collections.Generic;
using UnityEngine;

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static TrapSystemPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("TrapSystemPrototipo3");
            return go.AddComponent<TrapSystemPrototipo3>();
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
                case TipoTrampa.Puas: return "Señuelo Acústico";
                case TipoTrampa.Red: return "Bomba de Polen";
                case TipoTrampa.Madera: return "Descargador Estático";
                default: return "Ninguna";
            }
        }

        public static string GetTrapRecipeText(TipoTrampa t)
        {
            switch (t)
            {
                case TipoTrampa.Puas: return "2 Metal + 1 Desperdicios";
                case TipoTrampa.Red: return "2 Semillas + 1 Madera";
                case TipoTrampa.Madera: return "2 Goma + 1 Metal + 1 Madera";
                default: return "";
            }
        }

        public static string GetTrapDescription(TipoTrampa t)
        {
            switch (t)
            {
                case TipoTrampa.Puas:
                    return "Tecla [1]: Emite ondas sonoras durante 8 s y obliga a los enemigos en un radio de 18 m a investigar ese punto.";
                case TipoTrampa.Red:
                    return "Tecla [2]: Nube de polen verde durante 15 s que anula el olfato enemigo y reduce a la mitad su distancia visual.";
                case TipoTrampa.Madera:
                    return "Tecla [3]: Paraliza por 5 s al enemigo más cercano en 8 m y sobrecarga mecanismos eléctricos críticos.";
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
                    // Costo: 2 Metal + 1 Desperdicios
                    return inv.HasEnoughItem(ItemType.Metal, 2) && inv.HasEnoughItem(ItemType.Desperdicios, 1);

                case TipoTrampa.Red:
                    // Costo: 2 Semillas + 1 Madera
                    return inv.HasEnoughItem(ItemType.Semillas, 2) && inv.HasEnoughItem(ItemType.Madera, 1);

                case TipoTrampa.Madera:
                    // Costo: 2 Goma + 1 Metal + 1 Madera
                    return inv.HasEnoughItem(ItemType.Goma, 2) && inv.HasEnoughItem(ItemType.Metal, 1) && inv.HasEnoughItem(ItemType.Madera, 1);

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
                    inv.RemoveItem(ItemType.Metal, 2);
                    inv.RemoveItem(ItemType.Desperdicios, 1);
                    inv.AddTrampa(TipoTrampa.Puas);
                    break;

                case TipoTrampa.Red:
                    inv.RemoveItem(ItemType.Semillas, 2);
                    inv.RemoveItem(ItemType.Madera, 1);
                    inv.AddTrampa(TipoTrampa.Red);
                    break;

                case TipoTrampa.Madera:
                    inv.RemoveItem(ItemType.Goma, 2);
                    inv.RemoveItem(ItemType.Metal, 1);
                    inv.RemoveItem(ItemType.Madera, 1);
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
                    DeployAcousticDecoy(playerTransform);
                    break;

                case TipoTrampa.Red:
                    DeployPollenBomb(playerTransform);
                    break;

                case TipoTrampa.Madera:
                    DeployStaticDischarger(playerTransform);
                    break;
            }

            OnProto3TrapDeployed?.Invoke(tipo);
            return true;
        }

        private void DeployAcousticDecoy(Transform playerTransform)
        {
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * 1.2f;
            if (CityGeneratorPrototipo3.Instance != null)
            {
                spawnPos.y = CityGeneratorPrototipo3.Instance.roadHeightY;
            }

            GameObject decoyObj;
            if (acousticDecoyPrefab != null)
            {
                decoyObj = Instantiate(acousticDecoyPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
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

            Debug.Log("[TrapSystemPrototipo3] ¡TRAMPA 1 (Señuelo Acústico) desplegada! Atrayendo enemigos en 18 m durante 8 s.");
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
                    Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                    Material m = new Material(sh);
                    m.color = new Color(0.2f, 0.95f, 0.35f, 0.32f);
                    ren.material = m;
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            Debug.Log("[TrapSystemPrototipo3] ¡TRAMPA 2 (Bomba de Polen de Helecho) activada! Olfato desactivado y visión enemiga al 50% por 15 s.");
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

            // 1. Paralizar por 5 segundos al enemigo más cercano en un radio de 8 metros
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
                Debug.Log($"[TrapSystemPrototipo3] ¡TRAMPA 3 (Descargador Estático) paralizó a {closestEnemy.name} por 5 segundos!");
            }

            // 2. Activar/sobrecargar mecanismos eléctricos críticos cercanos (Válvula Hiperbárica de Clara / Condensadores UV de Kross)
            StoryMechanismPrototipo3[] mechanisms = FindObjectsByType<StoryMechanismPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < mechanisms.Length; i++)
            {
                StoryMechanismPrototipo3 mech = mechanisms[i];
                if (mech == null || !mech.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(center, mech.transform.position);
                if (d <= radius)
                {
                    mech.OnStaticDischargeHit();
                }
            }
        }
    }
}

