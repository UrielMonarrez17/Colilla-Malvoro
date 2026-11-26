using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Prototipo3
{
    /// <summary>
    /// Proyectil de Red Energética disparado por la Pistola Lanza Redes de Toby.
    /// Al impactar al Búho Rector lo envuelve en red, lo derriba y le causa daño de captura.
    /// </summary>
    public class FiredCaptureNetProjectile : MonoBehaviour
    {
        public Vector3 velocity;
        public float maxLifetime = 2.6f;
        public float hitRadius = 1.65f;
        private float elapsed;
        private bool hitProcessed;

        private void Update()
        {
            if (hitProcessed) return;
            if (PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay) return;

            elapsed += Time.deltaTime;
            if (elapsed >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += velocity * Time.deltaTime;
            float expand = Mathf.Lerp(0.45f, 1.35f, Mathf.Clamp01(elapsed * 3.5f));
            transform.localScale = Vector3.one * expand;
            transform.Rotate(0f, 320f * Time.deltaTime, 0f, Space.World);

            // 1. Prioridad máxima: impacto contra el Búho Rector (en vuelo o en suelo)
            OwlRectorBossAI owl = FindAnyObjectByType<OwlRectorBossAI>();
            if (owl != null && owl.isActiveAndEnabled && !owl.IsDefeated)
            {
                Vector3 owlCenter = owl.GetAimCenterWorld();
                float dist = Vector3.Distance(transform.position, owlCenter);
                float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(owlCenter.x, owlCenter.z));
                if (dist <= hitRadius + 1.05f || (distXZ <= hitRadius + 0.85f && Mathf.Abs(transform.position.y - owlCenter.y) <= 2.4f))
                {
                    hitProcessed = true;
                    owl.OnHitByNetLauncher(transform.position);
                    Destroy(gameObject);
                    return;
                }
            }

            // 2. Impacto secundario contra otros enemigos ferales cercanos
            EnemyAIPrototipo3[] enemies = FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAIPrototipo3 e = enemies[i];
                if (e == null || !e.isActiveAndEnabled) continue;
                if (Vector3.Distance(transform.position, e.transform.position + Vector3.up * 0.6f) <= hitRadius)
                {
                    hitProcessed = true;
                    if (e.TryGetComponent<MarcoFeralBossAI>(out var marco))
                    {
                        marco.ApplyBriefStagger(1.5f);
                    }
                    else
                    {
                        e.ApplyNetImmobilization(4.5f);
                    }
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Pistola Lanza Redes fabricada por Toby el Mapache para Kilo.
    /// Se acopla visualmente a Kilo, muestra su widget en el HUD y dispara proyectiles 3D de red con [Click Izquierdo] o [E].
    /// </summary>
    public class NetLauncherWeaponPrototipo3 : MonoBehaviour
    {
        public static NetLauncherWeaponPrototipo3 Instance { get; private set; }

        [Header("Prefabs / Modelos 3D Opcionales")]
        public GameObject gunVisualPrefab;
        public GameObject netProjectilePrefab;

        [Header("Parámetros del Lanza Redes")]
        public bool isUnlocked = false;
        public float fireCooldown = 1.55f;
        public float projectileSpeed = 24f;

        private float cooldownRemaining = 0f;
        private GameObject mountedGunVisual;
        private HamsterController boundHamster;

        // Widget HUD compacto para el Lanza Redes
        private Canvas weaponHudCanvas;
        private GameObject widgetRoot;
        private Text statusLabel;
        private RectTransform cooldownFillRt;

        public static NetLauncherWeaponPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("NetLauncherWeapon_Prototipo3");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<NetLauncherWeaponPrototipo3>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildWeaponHud();
        }

        private void BuildWeaponHud()
        {
            GameObject cGo = new GameObject("NetLauncherHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            cGo.transform.SetParent(transform, false);
            weaponHudCanvas = cGo.GetComponent<Canvas>();
            weaponHudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            weaponHudCanvas.sortingOrder = 155;

            CanvasScaler sc = cGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1600f, 900f);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            widgetRoot = new GameObject("NetLauncherWidget", typeof(RectTransform));
            widgetRoot.transform.SetParent(cGo.transform, false);
            RectTransform rt = widgetRoot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-32f, 28f);
            rt.sizeDelta = new Vector2(350f, 92f);

            GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(AngularComicBoxGraphic));
            bg.transform.SetParent(widgetRoot.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            AngularComicBoxGraphic g = bg.GetComponent<AngularComicBoxGraphic>();
            g.color = new Color(0.06f, 0.08f, 0.11f, 0.95f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject lblGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            lblGo.transform.SetParent(widgetRoot.transform, false);
            RectTransform lRt = lblGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.38f);
            lRt.anchorMax = new Vector2(1f, 0.94f);
            lRt.offsetMin = new Vector2(22f, 0f);
            lRt.offsetMax = new Vector2(-18f, 0f);
            statusLabel = lblGo.GetComponent<Text>();
            statusLabel.font = font;
            statusLabel.fontSize = 17;
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.color = new Color(0.35f, 0.92f, 1f, 1f);
            statusLabel.alignment = TextAnchor.MiddleLeft;
            statusLabel.text = "PISTOLA LANZA REDES\n[CLICK IZQ / E]  LISTA";

            GameObject track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(widgetRoot.transform, false);
            RectTransform tRt = track.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(0f, 0f);
            tRt.pivot = new Vector2(0f, 0f);
            tRt.anchoredPosition = new Vector2(22f, 14f);
            tRt.sizeDelta = new Vector2(304f, 14f);
            track.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.28f, 1f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            cooldownFillRt = fill.GetComponent<RectTransform>();
            cooldownFillRt.anchorMin = new Vector2(0f, 0f);
            cooldownFillRt.anchorMax = new Vector2(0f, 1f);
            cooldownFillRt.pivot = new Vector2(0f, 0.5f);
            cooldownFillRt.anchoredPosition = Vector2.zero;
            cooldownFillRt.sizeDelta = new Vector2(304f, 0f);
            fill.GetComponent<Image>().color = new Color(0.20f, 0.88f, 1f, 1f);

            widgetRoot.SetActive(false);
        }

        public void UnlockNetLauncher()
        {
            isUnlocked = true;
            if (widgetRoot != null) widgetRoot.SetActive(true);
            AttachVisualToPlayerIfNeeded();
        }

        private void AttachVisualToPlayerIfNeeded()
        {
            if (!isUnlocked) return;
            HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            if (hc == null) return;

            if (boundHamster == hc && mountedGunVisual != null) return;
            boundHamster = hc;

            if (mountedGunVisual != null) Destroy(mountedGunVisual);

            GameObject srcPrefab = gunVisualPrefab != null ? gunVisualPrefab : Resources.Load<GameObject>("Weapons/Prefab_NetLauncherGun");
            if (srcPrefab != null)
            {
                mountedGunVisual = Instantiate(srcPrefab, hc.transform, false);
            }
            else
            {
                mountedGunVisual = CreateFallbackGunVisual();
                mountedGunVisual.transform.SetParent(hc.transform, false);
            }
            mountedGunVisual.name = "Equipped_NetLauncherGun";

            // Compensar si el padre tiene escala pequeña o 1.0
            float parentScale = Mathf.Max(0.001f, hc.transform.lossyScale.x);
            mountedGunVisual.transform.localPosition = new Vector3(0.26f / parentScale, 0.42f / parentScale, 0.14f / parentScale);
            mountedGunVisual.transform.localRotation = Quaternion.identity;
            mountedGunVisual.transform.localScale = Vector3.one * (0.82f / parentScale);
        }

        private static GameObject CreateFallbackGunVisual()
        {
            GameObject root = new GameObject("FallbackNetGun");
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(barrel.GetComponent<Collider>());
            barrel.transform.SetParent(root.transform, false);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(sh);
            m.color = new Color(0.2f, 0.88f, 1f, 1f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);
            barrel.GetComponent<Renderer>().material = m;
            return root;
        }

        private void Update()
        {
            if (weaponHudCanvas != null)
            {
                weaponHudCanvas.enabled = isUnlocked && !PortraitDialogueUIPrototipo3.IsConversationActive && !UI.FeralGameUI.BlocksGameplay;
            }
            if (!isUnlocked) return;

            AttachVisualToPlayerIfNeeded();

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            }

            if (widgetRoot != null)
            {
                if (!widgetRoot.activeSelf) widgetRoot.SetActive(true);
                float readyRatio = cooldownRemaining <= 0f ? 1f : (1f - cooldownRemaining / Mathf.Max(0.1f, fireCooldown));
                if (cooldownFillRt != null) cooldownFillRt.sizeDelta = new Vector2(304f * readyRatio, 0f);
                if (statusLabel != null)
                {
                    statusLabel.text = cooldownRemaining <= 0f
                        ? "PISTOLA LANZA REDES\n[CLICK IZQ / E]  ¡LISTA!"
                        : $"PISTOLA LANZA REDES\nRECARGANDO CO2... {cooldownRemaining:F1}s";
                }
            }

            if (PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay) return;
            if (boundHamster == null || boundHamster.IsDead) return;

            bool firePressed = FeralInput.Pressed(KeyCode.E) || Input.GetKeyDown(KeyCode.E);
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) firePressed = true;
#else
            if (Input.GetMouseButtonDown(0)) firePressed = true;
#endif

            if (firePressed && cooldownRemaining <= 0f)
            {
                FireCaptureNet();
            }
        }

        private void FireCaptureNet()
        {
            cooldownRemaining = fireCooldown;
            Vector3 spawnOrigin = boundHamster.transform.position + Vector3.up * 0.55f + boundHamster.transform.forward * 0.55f;
            Vector3 targetDir = ComputeAimDirection(spawnOrigin);

            // Orientar suavemente a Kilo hacia el disparo
            Vector3 flatDir = new Vector3(targetDir.x, 0f, targetDir.z);
            if (flatDir.sqrMagnitude > 0.01f)
            {
                boundHamster.transform.rotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            }

            GameObject projSrc = netProjectilePrefab != null ? netProjectilePrefab : Resources.Load<GameObject>("Weapons/Prefab_NetProjectile");
            GameObject projObj;
            if (projSrc != null)
            {
                projObj = Instantiate(projSrc, spawnOrigin, Quaternion.LookRotation(targetDir, Vector3.up));
            }
            else
            {
                projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(projObj.GetComponent<Collider>());
                projObj.transform.position = spawnOrigin;
                projObj.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Material m = new Material(sh);
                m.color = new Color(0.2f, 0.92f, 1f, 0.85f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);
                projObj.GetComponent<Renderer>().material = m;
            }

            projObj.name = "ActiveCaptureNetProjectile";
            FiredCaptureNetProjectile netBeh = projObj.GetComponent<FiredCaptureNetProjectile>();
            if (netBeh == null) netBeh = projObj.AddComponent<FiredCaptureNetProjectile>();
            netBeh.velocity = targetDir * projectileSpeed;
        }

        private Vector3 ComputeAimDirection(Vector3 origin)
        {
            Camera cam = Camera.main;
            OwlRectorBossAI owl = FindAnyObjectByType<OwlRectorBossAI>();

            if (cam != null)
            {
                Vector3 mousePos = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) mousePos = Mouse.current.position.ReadValue();
#else
                mousePos = Input.mousePosition;
#endif
                Ray ray = cam.ScreenPointToRay(mousePos);
                Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
                if (aimPlane.Raycast(ray, out float enter))
                {
                    Vector3 cursorWorld = ray.GetPoint(enter);
                    Vector3 aimDir = (cursorWorld - origin);
                    aimDir.y = 0f;

                    // Asistencia de puntería vertical hacia el Búho Rector si el cursor apunta en su cono (vital en cámara isométrica cuando vuela!)
                    if (owl != null && owl.isActiveAndEnabled && !owl.IsDefeated)
                    {
                        Vector3 toOwl = owl.GetAimCenterWorld() - origin;
                        Vector3 toOwlFlat = new Vector3(toOwl.x, 0f, toOwl.z);
                        if (aimDir.sqrMagnitude > 0.05f && toOwlFlat.sqrMagnitude > 0.1f &&
                            Vector3.Angle(aimDir.normalized, toOwlFlat.normalized) <= 28f)
                        {
                            return toOwl.normalized;
                        }
                    }

                    if (aimDir.sqrMagnitude > 0.05f) return aimDir.normalized;
                }
            }

            if (owl != null && owl.isActiveAndEnabled && !owl.IsDefeated)
            {
                return (owl.GetAimCenterWorld() - origin).normalized;
            }

            return boundHamster != null ? boundHamster.transform.forward : Vector3.forward;
        }
    }
}
