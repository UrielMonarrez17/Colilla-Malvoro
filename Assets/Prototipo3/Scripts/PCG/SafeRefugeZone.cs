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

        private void OnEnable()
        {
            ActiveZone = this;
            EnsureSafeTrigger();
        }

        private void Start()
        {
            EnsureSafeTrigger();
            EvaluatePlayerContainment();
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
                safeTrigger.center = new Vector3(0f, 1.5f, 0.1f);
                safeTrigger.size = new Vector3(8.4f, 3.0f, 8.2f);
            }
            safeTrigger.enabled = true;
            safeTrigger.isTrigger = true;
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
            bool inside = ContainsPoint(checkPos, 0f);
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
