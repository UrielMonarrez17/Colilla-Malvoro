using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Prototipo3
{
    public enum DialoguePortraitId
    {
        None,
        KiloNormal,
        KiloScared,
        MarcoDay,
        MarcoFeral,
        ElenaRadio,
        TobyNormal,
        BuhoFeral,
        PerroFeral
    }

    public enum SpeakerSide
    {
        Left,
        Right
    }

    [Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        public string text;
        public SpeakerSide activeSide;
        public DialoguePortraitId leftPortrait;
        public DialoguePortraitId rightPortrait;
        public Color accentColor;
        public bool glitchEffect;

        public DialogueLine(
            string speakerName,
            string text,
            SpeakerSide activeSide,
            DialoguePortraitId leftPortrait,
            DialoguePortraitId rightPortrait,
            Color? accent = null,
            bool glitch = false)
        {
            this.speakerName = speakerName;
            this.text = text;
            this.activeSide = activeSide;
            this.leftPortrait = leftPortrait;
            this.rightPortrait = rightPortrait;
            this.accentColor = accent ?? new Color(0.86f, 0.18f, 0.14f, 1f);
            this.glitchEffect = glitch;
        }
    }

    /// <summary>
    /// Gráfico vectorial angular estilo Persona 5 (paralelogramos y polígonos estilizados sin depender de sprites externos).
    /// </summary>
    public class AngularComicBoxGraphic : MaskableGraphic
    {
        public Vector2 topLeftSkew = new Vector2(22f, -8f);
        public Vector2 topRightSkew = new Vector2(-12f, 6f);
        public Vector2 bottomRightSkew = new Vector2(-26f, 0f);
        public Vector2 bottomLeftSkew = new Vector2(8f, 4f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2 bl = new Vector2(r.xMin + bottomLeftSkew.x, r.yMin + bottomLeftSkew.y);
            Vector2 tl = new Vector2(r.xMin + topLeftSkew.x, r.yMax + topLeftSkew.y);
            Vector2 tr = new Vector2(r.xMax + topRightSkew.x, r.yMax + topRightSkew.y);
            Vector2 br = new Vector2(r.xMax + bottomRightSkew.x, r.yMin + bottomRightSkew.y);

            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = bl; vh.AddVert(v);
            v.position = tl; vh.AddVert(v);
            v.position = tr; vh.AddVert(v);
            v.position = br; vh.AddVert(v);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    /// <summary>
    /// Sistema de Diálogo fusión Persona 5 + CrossCode para Prototipo 3:
    /// - Muestra simultáneamente los retratos 2D de Kilo (izquierda) e interlocutor/jefe (derecha) sobre el gameplay isométrico en vivo.
    /// - Cajas angulares de alto contraste (borde blanco/crema + acento carmesí/cian + fondo tinta) y etiqueta de nombre inclinada.
    /// - Efecto máquina de escribir, resaltado dinámico del hablante activo y vibración/interferencia de radio.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class PortraitDialogueUIPrototipo3 : MonoBehaviour
    {
        public static PortraitDialogueUIPrototipo3 Instance { get; private set; }
        public static bool IsConversationActive => Instance != null && Instance.isOpen;

        private Canvas dialogueCanvas;
        private GameObject rootContainer;
        private RawImage leftPortraitImg;
        private RawImage rightPortraitImg;
        private RectTransform leftPortraitRt;
        private RectTransform rightPortraitRt;

        private RectTransform boxRootRt;
        private AngularComicBoxGraphic outerBorderGraphic;
        private AngularComicBoxGraphic accentShadowGraphic;
        private AngularComicBoxGraphic innerInkGraphic;

        private RectTransform nameBannerRt;
        private AngularComicBoxGraphic nameOuterGraphic;
        private AngularComicBoxGraphic nameInnerGraphic;
        private Text speakerNameText;
        private Text bodyText;
        private Text promptText;
        private Image radioScanlineOverlay;

        private readonly Dictionary<DialoguePortraitId, Texture2D> portraitCache = new Dictionary<DialoguePortraitId, Texture2D>();
        private readonly Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
        private Action onSequenceFinished;

        private bool isOpen;
        private bool isTyping;
        private string currentFullText = "";
        private Coroutine typingRoutine;
        private DialogueLine activeLine;
        private float inputUnlockUnscaledTime;
        private HamsterController cachedPlayer;
        private bool previousPlayerEnabled = true;
        private float prevTimeScale = 1f;
        private bool pausedTimeForDialogue;

        public static PortraitDialogueUIPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("PortraitDialogueUI_Prototipo3");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PortraitDialogueUIPrototipo3>();
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
            BuildUI();
        }

        private void BuildUI()
        {
            GameObject canvasGo = new GameObject("PersonaCrossDialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            dialogueCanvas = canvasGo.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 320;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            rootContainer = new GameObject("DialogueRoot", typeof(RectTransform));
            rootContainer.transform.SetParent(canvasGo.transform, false);
            RectTransform rootRt = rootContainer.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Sutil viñeta inferior semitransparente (permite ver toda la escena isométrica detrás como en CrossCode y Persona 5)
            GameObject bottomShade = new GameObject("BottomVignette", typeof(RectTransform), typeof(Image));
            bottomShade.transform.SetParent(rootContainer.transform, false);
            RectTransform shadeRt = bottomShade.GetComponent<RectTransform>();
            shadeRt.anchorMin = new Vector2(0f, 0f);
            shadeRt.anchorMax = new Vector2(1f, 0.36f);
            shadeRt.offsetMin = Vector2.zero;
            shadeRt.offsetMax = Vector2.zero;
            Image shadeImg = bottomShade.GetComponent<Image>();
            shadeImg.color = new Color(0.02f, 0.02f, 0.03f, 0.48f);
            shadeImg.raycastTarget = false;

            // Retrato Izquierdo (Kilo)
            leftPortraitRt = CreatePortraitSlot("LeftPortrait_Kilo", rootContainer.transform, true, out leftPortraitImg);

            // Retrato Derecho (NPC / Jefe / Radio)
            rightPortraitRt = CreatePortraitSlot("RightPortrait_Speaker", rootContainer.transform, false, out rightPortraitImg);

            // Overlay sutil de interferencia sobre retrato derecho cuando es radio cortada
            GameObject radioFxGo = new GameObject("RadioInterferenceOverlay", typeof(RectTransform), typeof(Image));
            radioFxGo.transform.SetParent(rightPortraitRt, false);
            RectTransform rfxRt = radioFxGo.GetComponent<RectTransform>();
            rfxRt.anchorMin = Vector2.zero;
            rfxRt.anchorMax = Vector2.one;
            rfxRt.offsetMin = Vector2.zero;
            rfxRt.offsetMax = Vector2.zero;
            radioScanlineOverlay = radioFxGo.GetComponent<Image>();
            radioScanlineOverlay.color = new Color(0.2f, 0.9f, 1f, 0.0f);
            radioScanlineOverlay.raycastTarget = false;

            // Caja Central Angular Estilo Persona 5
            GameObject boxGo = new GameObject("SpeechBoxContainer", typeof(RectTransform));
            boxGo.transform.SetParent(rootContainer.transform, false);
            boxRootRt = boxGo.GetComponent<RectTransform>();
            boxRootRt.anchorMin = new Vector2(0.5f, 0f);
            boxRootRt.anchorMax = new Vector2(0.5f, 0f);
            boxRootRt.pivot = new Vector2(0.5f, 0f);
            boxRootRt.anchoredPosition = new Vector2(0f, 34f);
            boxRootRt.sizeDelta = new Vector2(1120f, 238f);

            accentShadowGraphic = CreateAngularLayer("P5_AccentShadow", boxRootRt, new Vector2(12f, -10f), new Vector2(1134f, 244f),
                new Color(0.85f, 0.16f, 0.12f, 0.96f),
                new Vector2(28f, -10f), new Vector2(-10f, 8f), new Vector2(-30f, -4f), new Vector2(10f, 2f));

            outerBorderGraphic = CreateAngularLayer("P5_WhiteFrame", boxRootRt, Vector2.zero, new Vector2(1124f, 240f),
                new Color(0.96f, 0.93f, 0.87f, 1f),
                new Vector2(24f, -6f), new Vector2(-14f, 8f), new Vector2(-28f, 0f), new Vector2(8f, 4f));

            innerInkGraphic = CreateAngularLayer("P5_InnerInk", boxRootRt, Vector2.zero, new Vector2(1100f, 218f),
                new Color(0.06f, 0.05f, 0.07f, 0.96f),
                new Vector2(22f, -6f), new Vector2(-14f, 6f), new Vector2(-25f, 0f), new Vector2(8f, 4f));

            // Etiqueta de Nombre Inclinada Estilo Persona 5
            GameObject bannerGo = new GameObject("SpeakerBanner", typeof(RectTransform));
            bannerGo.transform.SetParent(boxRootRt, false);
            nameBannerRt = bannerGo.GetComponent<RectTransform>();
            nameBannerRt.anchorMin = new Vector2(0f, 1f);
            nameBannerRt.anchorMax = new Vector2(0f, 1f);
            nameBannerRt.pivot = new Vector2(0f, 0.5f);
            nameBannerRt.anchoredPosition = new Vector2(46f, 4f);
            nameBannerRt.sizeDelta = new Vector2(410f, 62f);
            nameBannerRt.localRotation = Quaternion.Euler(0f, 0f, 2.5f);

            nameOuterGraphic = CreateAngularLayer("BannerOuter", nameBannerRt, Vector2.zero, new Vector2(418f, 66f),
                new Color(0.97f, 0.94f, 0.88f, 1f),
                new Vector2(16f, 0f), new Vector2(-10f, 4f), new Vector2(-18f, -2f), new Vector2(4f, 0f));

            nameInnerGraphic = CreateAngularLayer("BannerInner", nameBannerRt, Vector2.zero, new Vector2(400f, 50f),
                new Color(0.82f, 0.16f, 0.12f, 1f),
                new Vector2(14f, 0f), new Vector2(-10f, 3f), new Vector2(-16f, -2f), new Vector2(4f, 0f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject nameTxtGo = new GameObject("SpeakerNameText", typeof(RectTransform), typeof(Text));
            nameTxtGo.transform.SetParent(nameBannerRt, false);
            RectTransform nameTxtRt = nameTxtGo.GetComponent<RectTransform>();
            nameTxtRt.anchorMin = Vector2.zero;
            nameTxtRt.anchorMax = Vector2.one;
            nameTxtRt.offsetMin = new Vector2(26f, 2f);
            nameTxtRt.offsetMax = new Vector2(-20f, -2f);
            speakerNameText = nameTxtGo.GetComponent<Text>();
            speakerNameText.font = font;
            speakerNameText.fontSize = 27;
            speakerNameText.fontStyle = FontStyle.Bold;
            speakerNameText.color = new Color(0.99f, 0.97f, 0.93f, 1f);
            speakerNameText.alignment = TextAnchor.MiddleLeft;
            speakerNameText.raycastTarget = false;

            // Texto Principal del Diálogo
            GameObject bodyGo = new GameObject("DialogueBodyText", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(boxRootRt, false);
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(58f, 46f);
            bodyRt.offsetMax = new Vector2(-58f, -38f);
            bodyText = bodyGo.GetComponent<Text>();
            bodyText.font = font;
            bodyText.fontSize = 27;
            bodyText.lineSpacing = 1.16f;
            bodyText.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.raycastTarget = false;

            // Indicador de Avance
            GameObject promptGo = new GameObject("AdvancePrompt", typeof(RectTransform), typeof(Text));
            promptGo.transform.SetParent(boxRootRt, false);
            RectTransform promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(1f, 0f);
            promptRt.anchorMax = new Vector2(1f, 0f);
            promptRt.pivot = new Vector2(1f, 0f);
            promptRt.anchoredPosition = new Vector2(-46f, 14f);
            promptRt.sizeDelta = new Vector2(420f, 32f);
            promptText = promptGo.GetComponent<Text>();
            promptText.font = font;
            promptText.fontSize = 19;
            promptText.fontStyle = FontStyle.Bold;
            promptText.color = new Color(0.95f, 0.76f, 0.36f, 1f);
            promptText.alignment = TextAnchor.MiddleRight;
            promptText.text = "▶  CONTINUAR  [F / ESPACIO]";
            promptText.raycastTarget = false;

            rootContainer.SetActive(false);
        }

        private static RectTransform CreatePortraitSlot(string name, Transform parent, bool isLeft, out RawImage rawImg)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            if (isLeft)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-12f, 0f);
            }
            rt.sizeDelta = new Vector2(540f, 540f);
            rawImg = go.GetComponent<RawImage>();
            rawImg.raycastTarget = false;
            return rt;
        }

        private static AngularComicBoxGraphic CreateAngularLayer(
            string name,
            Transform parent,
            Vector2 anchoredOffset,
            Vector2 size,
            Color color,
            Vector2 tl,
            Vector2 tr,
            Vector2 br,
            Vector2 bl)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AngularComicBoxGraphic));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredOffset;
            rt.sizeDelta = size;

            AngularComicBoxGraphic g = go.GetComponent<AngularComicBoxGraphic>();
            g.color = color;
            g.topLeftSkew = tl;
            g.topRightSkew = tr;
            g.bottomRightSkew = br;
            g.bottomLeftSkew = bl;
            g.raycastTarget = false;
            return g;
        }

        private Texture2D LoadPortraitTexture(DialoguePortraitId id)
        {
            if (id == DialoguePortraitId.None) return null;
            if (portraitCache.TryGetValue(id, out Texture2D cached) && cached != null) return cached;

            string resName;
            switch (id)
            {
                case DialoguePortraitId.KiloNormal: resName = "Portraits/Portrait_Kilo_Normal"; break;
                case DialoguePortraitId.KiloScared: resName = "Portraits/Portrait_Kilo_Scared"; break;
                case DialoguePortraitId.MarcoDay: resName = "Portraits/Portrait_Marco_Day"; break;
                case DialoguePortraitId.MarcoFeral: resName = "Portraits/Portrait_Marco_Feral"; break;
                case DialoguePortraitId.ElenaRadio: resName = "Portraits/Portrait_Elena_Radio"; break;
                case DialoguePortraitId.TobyNormal: resName = "Portraits/Portrait_Toby_Normal"; break;
                case DialoguePortraitId.BuhoFeral: resName = "Portraits/Portrait_Buho_Feral"; break;
                case DialoguePortraitId.PerroFeral: resName = "Portraits/Portrait_Perro_Feral"; break;
                default: return null;
            }

            Texture2D tex = Resources.Load<Texture2D>(resName);
            if (tex != null)
            {
                portraitCache[id] = tex;
            }
            return tex;
        }

        public void PlaySequence(IEnumerable<DialogueLine> lines, Action onComplete = null, bool pauseWorldTime = true)
        {
            lineQueue.Clear();
            foreach (DialogueLine l in lines)
            {
                lineQueue.Enqueue(l);
            }
            if (lineQueue.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            onSequenceFinished = onComplete;
            if (!isOpen)
            {
                isOpen = true;
                rootContainer.SetActive(true);
                cachedPlayer = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (cachedPlayer != null)
                {
                    previousPlayerEnabled = cachedPlayer.enabled;
                    cachedPlayer.enabled = false;
                    Rigidbody rb = cachedPlayer.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
                }

                if (pauseWorldTime && Time.timeScale > 0f)
                {
                    prevTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                    pausedTimeForDialogue = true;
                }
            }

            ShowNextLine();
        }

        private void ShowNextLine()
        {
            if (lineQueue.Count == 0)
            {
                CloseSequence();
                return;
            }

            activeLine = lineQueue.Dequeue();
            currentFullText = activeLine.text ?? "";
            speakerNameText.text = (activeLine.speakerName ?? "").ToUpperInvariant();

            accentShadowGraphic.color = activeLine.accentColor;
            nameInnerGraphic.color = activeLine.accentColor;

            // Posicionar la etiqueta del hablante a la izquierda o derecha según quién hable (estilo Persona 5 + CrossCode)
            if (activeLine.activeSide == SpeakerSide.Left)
            {
                nameBannerRt.anchorMin = nameBannerRt.anchorMax = new Vector2(0f, 1f);
                nameBannerRt.pivot = new Vector2(0f, 0.5f);
                nameBannerRt.anchoredPosition = new Vector2(44f, 6f);
                nameBannerRt.localRotation = Quaternion.Euler(0f, 0f, 2.4f);
            }
            else
            {
                nameBannerRt.anchorMin = nameBannerRt.anchorMax = new Vector2(1f, 1f);
                nameBannerRt.pivot = new Vector2(1f, 0.5f);
                nameBannerRt.anchoredPosition = new Vector2(-44f, 6f);
                nameBannerRt.localRotation = Quaternion.Euler(0f, 0f, -2.4f);
            }

            // Actualizar Retratos Simultáneos Izquierda / Derecha
            Texture2D leftTex = LoadPortraitTexture(activeLine.leftPortrait);
            leftPortraitImg.texture = leftTex;
            leftPortraitImg.gameObject.SetActive(leftTex != null);

            Texture2D rightTex = LoadPortraitTexture(activeLine.rightPortrait);
            rightPortraitImg.texture = rightTex;
            rightPortraitImg.gameObject.SetActive(rightTex != null);

            bool leftActive = activeLine.activeSide == SpeakerSide.Left;
            leftPortraitImg.color = leftActive ? Color.white : new Color(0.46f, 0.44f, 0.50f, 0.90f);
            rightPortraitImg.color = !leftActive ? Color.white : new Color(0.46f, 0.44f, 0.50f, 0.90f);

            leftPortraitRt.localScale = Vector3.one * (leftActive ? 1.05f : 0.92f);
            rightPortraitRt.localScale = Vector3.one * (!leftActive ? 1.05f : 0.92f);

            inputUnlockUnscaledTime = Time.unscaledTime + 0.18f;

            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = StartCoroutine(TypewriterRoutine(currentFullText));
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            isTyping = true;
            bodyText.text = "";
            const float charsPerSec = 58f;
            float elapsed = 0f;

            while (elapsed * charsPerSec < fullText.Length)
            {
                elapsed += Time.unscaledDeltaTime;
                int count = Mathf.Clamp(Mathf.FloorToInt(elapsed * charsPerSec), 0, fullText.Length);
                bodyText.text = fullText.Substring(0, count);
                yield return null;
            }

            bodyText.text = fullText;
            isTyping = false;
            typingRoutine = null;
        }

        private void Update()
        {
            if (!isOpen) return;

            // Animación sutil de respiración / pulso e interferencia si la línea tiene glitchEffect (llamada cortada)
            float t = Time.unscaledTime;
            if (promptText != null)
            {
                float a = isTyping ? 0.45f : (0.65f + 0.35f * Mathf.Sin(t * 6.5f));
                promptText.color = new Color(0.96f, 0.78f, 0.36f, a);
                promptText.text = isTyping ? "▶  SALTAR  [F / ESPACIO]" : (lineQueue.Count > 0 ? "▶  SIGUIENTE  [F / ESPACIO]" : "▶  CERRAR  [F / ESPACIO]");
            }

            if (activeLine.glitchEffect)
            {
                float jitterX = Mathf.Sin(t * 58f) * 9f;
                float jitterY = Mathf.Cos(t * 43f) * 4f;
                rightPortraitRt.anchoredPosition = new Vector2(-12f + jitterX, jitterY);
                boxRootRt.anchoredPosition = new Vector2(jitterX * 0.45f, 34f + jitterY * 0.35f);
                if (radioScanlineOverlay != null)
                {
                    radioScanlineOverlay.color = new Color(0.25f, 0.95f, 1f, 0.14f + 0.16f * Mathf.Abs(Mathf.Sin(t * 25f)));
                }
            }
            else
            {
                leftPortraitRt.anchoredPosition = new Vector2(12f, Mathf.Sin(t * 2.8f) * 4f);
                rightPortraitRt.anchoredPosition = new Vector2(-12f, Mathf.Cos(t * 2.6f) * 4f);
                boxRootRt.anchoredPosition = new Vector2(0f, 34f);
                if (radioScanlineOverlay != null) radioScanlineOverlay.color = new Color(0.2f, 0.9f, 1f, 0f);
            }

            if (Time.unscaledTime < inputUnlockUnscaledTime) return;

            bool advancePressed =
                FeralInput.Pressed(KeyCode.F) ||
                FeralInput.Pressed(KeyCode.Space) ||
                FeralInput.Pressed(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.F) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetMouseButtonDown(0);

            if (advancePressed)
            {
                if (isTyping)
                {
                    if (typingRoutine != null) StopCoroutine(typingRoutine);
                    typingRoutine = null;
                    isTyping = false;
                    bodyText.text = currentFullText;
                    inputUnlockUnscaledTime = Time.unscaledTime + 0.12f;
                }
                else
                {
                    ShowNextLine();
                }
            }
        }

        public void CloseSequence()
        {
            if (!isOpen) return;
            isOpen = false;
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;
            isTyping = false;
            rootContainer.SetActive(false);

            if (pausedTimeForDialogue)
            {
                Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
                pausedTimeForDialogue = false;
            }

            if (cachedPlayer != null && !cachedPlayer.IsDead)
            {
                cachedPlayer.enabled = previousPlayerEnabled;
            }

            Action cb = onSequenceFinished;
            onSequenceFinished = null;
            cb?.Invoke();
        }
    }
}
