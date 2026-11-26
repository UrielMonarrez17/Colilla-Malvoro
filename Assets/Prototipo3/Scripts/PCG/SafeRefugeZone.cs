using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Zona segura del Refugio del Hámster (basado en InteriorBuilding).
    /// Bloquea e impele a los enemigos fuera del interior e indica al jugador que está a salvo.
    /// </summary>
    public class SafeRefugeZone : MonoBehaviour
    {
        public static SafeRefugeZone ActiveZone { get; private set; }
        public static bool IsPlayerInsideSafeRefuge { get; private set; }

        [Header("Referencias del Refugio")]
        public BoxCollider safeTrigger;
        public Transform doorwayOutsidePoint;
        public bool showSafeIndicatorHud = true;

        private HamsterController cachedPlayer;
        private GUIStyle pillBgStyle;
        private GUIStyle pillTextStyle;
        private Texture2D pillTex;
        private bool floorNormalized = false;

        private void Awake()
        {
            NormalizeRefugeFloorAndAnchors();
            EnsureSafeTrigger();
        }

        private void OnEnable()
        {
            ActiveZone = this;
            NormalizeRefugeFloorAndAnchors();
            EnsureSafeTrigger();
        }

        private void Start()
        {
            NormalizeRefugeFloorAndAnchors();
            EnsureSafeTrigger();
            EvaluatePlayerContainment();
        }

        /// <summary>
        /// Corrige en caliente cualquier desfase vertical heredado de InteriorBuilding (ej. InteriorModel en y=0.77m)
        /// para que el suelo de la casita quede exactamente al ras de la calle (y=+0.02m), con BoxCollider sólido
        /// debajo y sin objetos decorativos espurios (Pavement / Mesh_Hamster duplicado), evitando que Kilo quede bajo el piso.
        /// </summary>
        public void NormalizeRefugeFloorAndAnchors()
        {
            if (floorNormalized) return;
            floorNormalized = true;

            Transform pav = transform.Find("Pavement");
            if (pav != null) pav.gameObject.SetActive(false);

            Transform interior = transform.Find("InteriorModel");
            if (interior != null)
            {
                interior.localPosition = Vector3.zero;
                interior.localRotation = Quaternion.identity;

                // Ocultar cualquier hámster estático decorativo heredado del prefab antiguo
                foreach (Transform t in interior.GetComponentsInChildren<Transform>(true))
                {
                    if (t != null && t.name.IndexOf("Mesh_Hamster", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        t.gameObject.SetActive(false);
                    }
                }

                Transform layout = interior.Find("Layout");
                if (layout != null)
                {
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
                                float targetWorldY = transform.position.y + 0.02f;
                                layout.position += new Vector3(0f, targetWorldY - maxWorldY, 0f);
                            }
                        }

                        MeshCollider planeMc = floorTr.GetComponent<MeshCollider>();
                        if (planeMc != null) planeMc.enabled = false;
                    }
                }
            }

            // Losa sólida del piso del Refugio (cara superior exacta en local y = +0.02f)
            Transform solidSlab = transform.Find("RefugeSolidFloorCollider");
            if (solidSlab == null)
            {
                GameObject slabGo = new GameObject("RefugeSolidFloorCollider");
                slabGo.transform.SetParent(transform, false);
                slabGo.transform.localPosition = Vector3.zero;
                slabGo.transform.localRotation = Quaternion.identity;
                BoxCollider floorBox = slabGo.AddComponent<BoxCollider>();
                floorBox.isTrigger = false;
                floorBox.center = new Vector3(0f, -0.23f, 0.05f);
                floorBox.size = new Vector3(8.8f, 0.50f, 8.9f);
            }

            Transform frontWalls = transform.Find("FrontFacadeWalls");
            if (frontWalls != null) frontWalls.localPosition = Vector3.zero;

            Transform craftTr = transform.Find("Refuge_CraftingTable");
            if (craftTr != null) craftTr.localPosition = new Vector3(craftTr.localPosition.x, 0.03f, craftTr.localPosition.z);

            Transform spawnAnchor = transform.Find("PlayerSpawnAnchor");
            if (spawnAnchor != null) spawnAnchor.localPosition = new Vector3(0f, 0.08f, 0.1f);
        }

        private void OnDisable()
        {
            if (ActiveZone == this)
            {
                ActiveZone = null;
                IsPlayerInsideSafeRefuge = false;
            }
        }

        private void OnDestroy()
        {
            if (ActiveZone == this)
            {
                ActiveZone = null;
                IsPlayerInsideSafeRefuge = false;
            }
            if (pillTex != null)
            {
                Destroy(pillTex);
                pillTex = null;
            }
        }

        private void EnsureSafeTrigger()
        {
            if (safeTrigger == null)
            {
                safeTrigger = GetComponent<BoxCollider>();
            }
            if (safeTrigger == null)
            {
                safeTrigger = gameObject.AddComponent<BoxCollider>();
            }
            // Cubrir íntegramente todo el interior y el umbral de la puerta (z = -4.85 hasta +4.55)
            safeTrigger.center = new Vector3(0f, 1.4f, -0.15f);
            safeTrigger.size = new Vector3(9.2f, 3.6f, 9.4f);
            safeTrigger.enabled = true;
            safeTrigger.isTrigger = true;
        }

        /// <summary>
        /// Detección infalible (por Trigger + Bounds locales + distancia geométrica al centro/umbral del Refugio)
        /// para que misiones de huida como Marco Feral jamás fallen al cruzar la puerta.
        /// </summary>
        public static bool IsPointOrPlayerInRefuge(Vector3 worldPos)
        {
            if (IsPlayerInsideSafeRefuge) return true;

            if (ActiveZone != null)
            {
                Vector3 local = ActiveZone.transform.InverseTransformPoint(worldPos);
                if (Mathf.Abs(local.x) <= 4.75f && local.z >= -5.05f && local.z <= 4.65f)
                {
                    return true;
                }
                if (ActiveZone.ContainsPoint(worldPos, 0.55f))
                {
                    return true;
                }
            }

            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            if (city != null && city.SafeRefugeCenter != Vector3.zero)
            {
                float dx = worldPos.x - city.SafeRefugeCenter.x;
                float dz = worldPos.z - city.SafeRefugeCenter.z;
                if ((dx * dx + dz * dz) <= 5.1f * 5.1f)
                {
                    return true;
                }
                if (city.SafeRefugeEntrancePoint != Vector3.zero)
                {
                    float ex = worldPos.x - city.SafeRefugeEntrancePoint.x;
                    float ez = worldPos.z - city.SafeRefugeEntrancePoint.z;
                    if ((ex * ex + ez * ez) <= 2.5f * 2.5f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Update()
        {
            EvaluatePlayerContainment();
        }

        private void FixedUpdate()
        {
            EvaluatePlayerContainment();
        }

        private void EvaluatePlayerContainment()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<HamsterController>();
            }
            if (cachedPlayer == null || !cachedPlayer.gameObject.activeInHierarchy || safeTrigger == null)
            {
                if (ActiveZone == this) IsPlayerInsideSafeRefuge = false;
                return;
            }

            Vector3 checkPos = cachedPlayer.transform.position + Vector3.up * 0.35f;
            Vector3 local = transform.InverseTransformPoint(checkPos);
            bool inside = (Mathf.Abs(local.x) <= 4.65f && local.z >= -4.85f && local.z <= 4.55f) || ContainsPoint(checkPos, 0.25f);
            if (ActiveZone == this)
            {
                IsPlayerInsideSafeRefuge = inside;
            }
        }

        public bool ContainsPoint(Vector3 worldPoint, float horizontalPadding = 0f)
        {
            if (safeTrigger == null) return false;
            Bounds b = safeTrigger.bounds;
            return worldPoint.x >= b.min.x - horizontalPadding &&
                   worldPoint.x <= b.max.x + horizontalPadding &&
                   worldPoint.z >= b.min.z - horizontalPadding &&
                   worldPoint.z <= b.max.z + horizontalPadding;
        }

        public Vector3 GetSafeOutsidePosition()
        {
            if (doorwayOutsidePoint != null) return doorwayOutsidePoint.position;
            return transform.position - transform.forward * 6.0f;
        }

        public Vector3 GetRepelOrigin()
        {
            return safeTrigger != null ? safeTrigger.bounds.center : transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleColliderPresence(other, true);
        }

        private void OnTriggerStay(Collider other)
        {
            HandleColliderPresence(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            if (other.CompareTag("Player") || other.GetComponentInParent<HamsterController>() != null)
            {
                EvaluatePlayerContainment();
            }
        }

        private void HandleColliderPresence(Collider other, bool isInside)
        {
            if (other == null) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<HamsterController>() != null)
            {
                if (ActiveZone == this) IsPlayerInsideSafeRefuge = isInside;
                return;
            }

            EnemyAIPrototipo3 enemy = other.GetComponentInParent<EnemyAIPrototipo3>();
            if (enemy != null)
            {
                enemy.RepelFromSafeRefuge(GetRepelOrigin());
            }
        }

        private void OnGUI()
        {
            if (!showSafeIndicatorHud || ActiveZone != this || !IsPlayerInsideSafeRefuge) return;
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;

            if (pillTex == null)
            {
                pillTex = new Texture2D(1, 1);
                pillTex.SetPixel(0, 0, new Color(0.08f, 0.34f, 0.20f, 0.88f));
                pillTex.Apply();
            }

            if (pillBgStyle == null)
            {
                pillBgStyle = new GUIStyle(GUI.skin.box);
                pillBgStyle.normal.background = pillTex;
                pillBgStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (pillTextStyle == null)
            {
                pillTextStyle = new GUIStyle(GUI.skin.label);
                pillTextStyle.alignment = TextAnchor.MiddleCenter;
                pillTextStyle.fontSize = 15;
                pillTextStyle.fontStyle = FontStyle.Bold;
                pillTextStyle.normal.textColor = new Color(0.84f, 1f, 0.88f, 1f);
            }

            float width = 420f;
            float height = 34f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, 18f, width, height);
            GUI.Box(rect, GUIContent.none, pillBgStyle);
            GUI.Label(rect, "REFUGIO SEGURO — Los enemigos no pueden entrar", pillTextStyle);
        }
    }
}
