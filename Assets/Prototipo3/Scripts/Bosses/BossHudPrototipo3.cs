using UnityEngine;
using UnityEngine.UI;

namespace Prototipo3
{
    /// <summary>
    /// Barra superior estilizada de Jefe / Amenaza Feral para Marco Feral, Perro Guardián y Búho Rector.
    /// </summary>
    public class BossHudPrototipo3 : MonoBehaviour
    {
        public static BossHudPrototipo3 Instance { get; private set; }

        private Canvas hudCanvas;
        private GameObject panelRoot;
        private Text bossTitleText;
        private Text bossSubText;
        private RectTransform fillRect;
        private Image fillImage;
        private Image flashImage;

        private float targetNormalized = 1f;
        private float displayedNormalized = 1f;
        private float flashAlpha = 0f;
        private Color flashColor = Color.white;
        private const float BarMaxWidth = 760f;

        public static BossHudPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("BossHud_Prototipo3");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<BossHudPrototipo3>();
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
            BuildHud();
        }

        private void BuildHud()
        {
            GameObject cGo = new GameObject("BossHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            cGo.transform.SetParent(transform, false);
            hudCanvas = cGo.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 180;

            CanvasScaler sc = cGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;

            // Flash sutil de pantalla completa para impactos de red o rugidos ferales
            GameObject fGo = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
            fGo.transform.SetParent(cGo.transform, false);
            RectTransform fRt = fGo.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            flashImage = fGo.GetComponent<Image>();
            flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashImage.raycastTarget = false;

            panelRoot = new GameObject("BossBarRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(cGo.transform, false);
            RectTransform rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, -24f);
            rootRt.sizeDelta = new Vector2(820f, 118f);

            CreateSkewBox("ShadowBg", panelRoot.transform, new Vector2(6f, -5f), new Vector2(824f, 114f), new Color(0.84f, 0.16f, 0.12f, 0.90f));
            CreateSkewBox("InkBg", panelRoot.transform, Vector2.zero, new Vector2(812f, 106f), new Color(0.06f, 0.05f, 0.07f, 0.94f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject titleGo = new GameObject("BossTitle", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelRoot.transform, false);
            RectTransform tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -10f);
            tRt.sizeDelta = new Vector2(760f, 34f);
            bossTitleText = titleGo.GetComponent<Text>();
            bossTitleText.font = font;
            bossTitleText.fontSize = 24;
            bossTitleText.fontStyle = FontStyle.Bold;
            bossTitleText.color = new Color(0.98f, 0.94f, 0.86f, 1f);
            bossTitleText.alignment = TextAnchor.MiddleCenter;
            bossTitleText.raycastTarget = false;

            GameObject trackGo = new GameObject("BarTrack", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(panelRoot.transform, false);
            RectTransform trRt = trackGo.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0.5f, 1f);
            trRt.anchorMax = new Vector2(0.5f, 1f);
            trRt.pivot = new Vector2(0.5f, 1f);
            trRt.anchoredPosition = new Vector2(0f, -48f);
            trRt.sizeDelta = new Vector2(BarMaxWidth, 22f);
            trackGo.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.16f, 1f);
            trackGo.GetComponent<Image>().raycastTarget = false;

            GameObject fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(BarMaxWidth, 0f);
            fillImage = fillGo.GetComponent<Image>();
            fillImage.color = new Color(0.92f, 0.24f, 0.14f, 1f);
            fillImage.raycastTarget = false;

            GameObject subGo = new GameObject("BossSubtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(panelRoot.transform, false);
            RectTransform sRt = subGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 1f);
            sRt.anchorMax = new Vector2(0.5f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.anchoredPosition = new Vector2(0f, -74f);
            sRt.sizeDelta = new Vector2(770f, 26f);
            bossSubText = subGo.GetComponent<Text>();
            bossSubText.font = font;
            bossSubText.fontSize = 16;
            bossSubText.color = new Color(0.95f, 0.76f, 0.36f, 1f);
            bossSubText.alignment = TextAnchor.MiddleCenter;
            bossSubText.raycastTarget = false;

            panelRoot.SetActive(false);
        }

        private static void CreateSkewBox(string name, Transform parent, Vector2 offset, Vector2 size, Color col)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AngularComicBoxGraphic));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            AngularComicBoxGraphic g = go.GetComponent<AngularComicBoxGraphic>();
            g.color = col;
            g.topLeftSkew = new Vector2(16f, 0f);
            g.topRightSkew = new Vector2(-10f, 0f);
            g.bottomRightSkew = new Vector2(-16f, 0f);
            g.bottomLeftSkew = new Vector2(10f, 0f);
            g.raycastTarget = false;
        }

        public void ShowBoss(string title, string subtitle, float normalizedValue, Color? barColor = null)
        {
            if (panelRoot != null && !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
                displayedNormalized = Mathf.Clamp01(normalizedValue);
            }
            if (bossTitleText != null) bossTitleText.text = title;
            if (bossSubText != null) bossSubText.text = subtitle;
            targetNormalized = Mathf.Clamp01(normalizedValue);
            if (barColor.HasValue && fillImage != null) fillImage.color = barColor.Value;
        }

        public void HideBoss()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void TriggerFlash(Color color, float alpha = 0.28f)
        {
            flashColor = color;
            flashAlpha = Mathf.Clamp01(alpha);
        }

        private void Update()
        {
            if (hudCanvas != null)
            {
                hudCanvas.enabled = !PortraitDialogueUIPrototipo3.IsConversationActive;
            }

            if (panelRoot != null && panelRoot.activeSelf && fillRect != null)
            {
                displayedNormalized = Mathf.Lerp(displayedNormalized, targetNormalized, Time.unscaledDeltaTime * 8f);
                fillRect.sizeDelta = new Vector2(BarMaxWidth * displayedNormalized, 0f);
            }

            if (flashAlpha > 0.001f && flashImage != null)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 1.4f);
                flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
            }
        }
    }
}
