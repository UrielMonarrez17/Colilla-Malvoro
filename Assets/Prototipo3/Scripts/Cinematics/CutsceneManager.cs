using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.AI;

namespace Prototipo3
{
    /// <summary>
    /// Reproductor de Cinemáticas MP4 con respaldo automático de Novela Gráfica / Viñeta de Cómic In-Engine.
    /// Soporta 7 eventos cinemáticos clave buscando en Assets/StreamingAssets/Cutscenes/
    /// y en caso de ausencia de archivo .mp4, despliega la cámara hacia el objetivo y proyecta
    /// un marco estilizado de cómic 2D con retrato, insignias y subtítulos avanzando con [ESPACIO] o [F].
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        public static CutsceneManager Instance { get; private set; }

        public static readonly string[] RequiredCutsceneFiles =
        {
            "01_Intro_Sofia.mp4",
            "02_Marco_Feral.mp4",
            "03_Toby_Escuela.mp4",
            "04_Buho_Apagon.mp4",
            "05_Sultan_LuzFria.mp4",
            "06_Hospital_Kross.mp4",
            "07_Final_Amanecer.mp4"
        };

        [Header("Configuración")]
        public float fallbackDurationSeconds = 6.0f;

        [Header("Estado (Solo Lectura)")]
        [SerializeField] private bool isPlayingCutscene = false;
        [SerializeField] private string currentCutsceneFile = "";
        public bool IsPlayingCutscene => isPlayingCutscene;

        private Canvas cutsceneCanvas;
        private GameObject rootPanel;
        private RawImage videoRawImage;
        private VideoPlayer videoPlayer;
        private RenderTexture videoRenderTexture;
        private Image fallbackBlackoutBg;
        private Text headerText;
        private Text subtitleText;
        private Text skipHintText;
        private Button skipButton;

        // Elementos de la Novela Gráfica / Cómic In-Engine
        private GameObject comicPanelRoot;
        private Image comicPortraitBox;
        private Text comicPortraitInitial;
        private Text comicSpeakerName;
        private Text comicSpeakerTitle;
        private Text comicDialogueText;
        private Text comicAdvanceHint;

        private Coroutine activeRoutine;
        private Action pendingOnCompleted;
        private readonly List<NavMeshAgent> pausedAgents = new List<NavMeshAgent>();
        private bool wasPlayerEnabled = true;

        private struct ComicSceneData
        {
            public string speaker;
            public string title;
            public Color color;
            public string initial;
            public string[] lines;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this && Application.isPlaying) Destroy(gameObject);
            BuildCutsceneUI();
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        public static CutsceneManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("CutsceneManager");
            Instance = go.AddComponent<CutsceneManager>();
            return Instance;
        }

        private void BuildCutsceneUI()
        {
            if (cutsceneCanvas != null) return;

            GameObject canvasGo = new GameObject("CutsceneCanvas_Prototipo3", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            cutsceneCanvas = canvasGo.GetComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvas.sortingOrder = 500;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            rootPanel = new GameObject("CutsceneRoot", typeof(RectTransform));
            rootPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform rtRoot = rootPanel.GetComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.offsetMin = Vector2.zero;
            rtRoot.offsetMax = Vector2.zero;

            // Fondo para video MP4
            GameObject rawImgGo = new GameObject("MP4_VideoScreen", typeof(RectTransform), typeof(RawImage));
            rawImgGo.transform.SetParent(rootPanel.transform, false);
            RectTransform rtVideo = rawImgGo.GetComponent<RectTransform>();
            rtVideo.anchorMin = Vector2.zero;
            rtVideo.anchorMax = Vector2.one;
            rtVideo.offsetMin = Vector2.zero;
            rtVideo.offsetMax = Vector2.zero;
            videoRawImage = rawImgGo.GetComponent<RawImage>();
            videoRawImage.color = Color.black;

            videoPlayer = GetComponent<VideoPlayer>() ?? gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.isLooping = false;

            // Barras cinematográficas (Letterbox)
            GameObject topBar = new GameObject("LetterboxTop", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(rootPanel.transform, false);
            RectTransform rtTop = topBar.GetComponent<RectTransform>();
            rtTop.anchorMin = new Vector2(0f, 0.88f);
            rtTop.anchorMax = new Vector2(1f, 1f);
            rtTop.offsetMin = Vector2.zero;
            rtTop.offsetMax = Vector2.zero;
            topBar.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.95f);

            GameObject bottomBar = new GameObject("LetterboxBottom", typeof(RectTransform), typeof(Image));
            bottomBar.transform.SetParent(rootPanel.transform, false);
            RectTransform rtBottom = bottomBar.GetComponent<RectTransform>();
            rtBottom.anchorMin = new Vector2(0f, 0f);
            rtBottom.anchorMax = new Vector2(1f, 0.12f);
            rtBottom.offsetMin = Vector2.zero;
            rtBottom.offsetMax = Vector2.zero;
            fallbackBlackoutBg = bottomBar.GetComponent<Image>();
            fallbackBlackoutBg.color = new Color(0.02f, 0.02f, 0.03f, 0.95f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Título de la cinemática
            GameObject headerGo = new GameObject("CutsceneHeaderText", typeof(RectTransform), typeof(Text));
            headerGo.transform.SetParent(topBar.transform, false);
            RectTransform rtHeader = headerGo.GetComponent<RectTransform>();
            rtHeader.anchorMin = new Vector2(0.04f, 0.1f);
            rtHeader.anchorMax = new Vector2(0.65f, 0.9f);
            rtHeader.offsetMin = Vector2.zero;
            rtHeader.offsetMax = Vector2.zero;
            headerText = headerGo.GetComponent<Text>();
            headerText.font = font;
            headerText.fontSize = 24;
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = new Color(0.95f, 0.78f, 0.35f, 1f);
            headerText.alignment = TextAnchor.MiddleLeft;

            // Subtítulos estándar (Modo MP4)
            GameObject subGo = new GameObject("CutsceneSubtitleText", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(bottomBar.transform, false);
            RectTransform rtSub = subGo.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.08f, 0.1f);
            rtSub.anchorMax = new Vector2(0.92f, 0.9f);
            rtSub.offsetMin = Vector2.zero;
            rtSub.offsetMax = Vector2.zero;
            subtitleText = subGo.GetComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 26;
            subtitleText.color = new Color(0.96f, 0.93f, 0.85f, 1f);
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Botón de saltar
            GameObject skipBtnGo = new GameObject("SkipCutsceneButton", typeof(RectTransform), typeof(Image), typeof(Button));
            skipBtnGo.transform.SetParent(topBar.transform, false);
            RectTransform rtSkip = skipBtnGo.GetComponent<RectTransform>();
            rtSkip.anchorMin = new Vector2(1f, 0.5f);
            rtSkip.anchorMax = new Vector2(1f, 0.5f);
            rtSkip.pivot = new Vector2(1f, 0.5f);
            rtSkip.anchoredPosition = new Vector2(-40f, 0f);
            rtSkip.sizeDelta = new Vector2(310f, 50f);
            skipBtnGo.GetComponent<Image>().color = new Color(0.55f, 0.16f, 0.12f, 0.95f);

            skipButton = skipBtnGo.GetComponent<Button>();
            skipButton.onClick.AddListener(SkipCurrentCutscene);

            GameObject skipTxtGo = new GameObject("SkipLabel", typeof(RectTransform), typeof(Text));
            skipTxtGo.transform.SetParent(skipBtnGo.transform, false);
            RectTransform rtSkipTxt = skipTxtGo.GetComponent<RectTransform>();
            rtSkipTxt.anchorMin = Vector2.zero;
            rtSkipTxt.anchorMax = Vector2.one;
            rtSkipTxt.offsetMin = Vector2.zero;
            rtSkipTxt.offsetMax = Vector2.zero;
            skipHintText = skipTxtGo.GetComponent<Text>();
            skipHintText.font = font;
            skipHintText.fontSize = 20;
            skipHintText.fontStyle = FontStyle.Bold;
            skipHintText.color = Color.white;
            skipHintText.alignment = TextAnchor.MiddleCenter;
            skipHintText.text = "Saltar Cinemática [ESC]";

            // =========================================================================
            // PANEL 2D ESTILIZADO DE NOVELA GRÁFICA / CÓMIC IN-ENGINE
            // =========================================================================
            comicPanelRoot = new GameObject("ComicVignettePanel", typeof(RectTransform), typeof(Image));
            comicPanelRoot.transform.SetParent(rootPanel.transform, false);
            RectTransform rtComic = comicPanelRoot.GetComponent<RectTransform>();
            rtComic.anchorMin = new Vector2(0.08f, 0.12f);
            rtComic.anchorMax = new Vector2(0.92f, 0.44f);
            rtComic.offsetMin = Vector2.zero;
            rtComic.offsetMax = Vector2.zero;
            // Marco exterior oscuro de viñeta
            comicPanelRoot.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.06f, 0.96f);

            // Borde doble dorado / estilizado interior
            GameObject innerBorder = new GameObject("ComicInnerBorder", typeof(RectTransform), typeof(Image));
            innerBorder.transform.SetParent(comicPanelRoot.transform, false);
            RectTransform rtInner = innerBorder.GetComponent<RectTransform>();
            rtInner.anchorMin = new Vector2(0.008f, 0.02f);
            rtInner.anchorMax = new Vector2(0.992f, 0.98f);
            rtInner.offsetMin = Vector2.zero;
            rtInner.offsetMax = Vector2.zero;
            innerBorder.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.13f, 0.98f);

            // Retrato del personaje (Izquierda)
            GameObject portraitGo = new GameObject("PortraitBox", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(innerBorder.transform, false);
            RectTransform rtPortrait = portraitGo.GetComponent<RectTransform>();
            rtPortrait.anchorMin = new Vector2(0.02f, 0.12f);
            rtPortrait.anchorMax = new Vector2(0.18f, 0.88f);
            rtPortrait.offsetMin = Vector2.zero;
            rtPortrait.offsetMax = Vector2.zero;
            comicPortraitBox = portraitGo.GetComponent<Image>();
            comicPortraitBox.color = new Color(0.6f, 0.35f, 0.2f);

            GameObject initialGo = new GameObject("PortraitInitial", typeof(RectTransform), typeof(Text));
            initialGo.transform.SetParent(portraitGo.transform, false);
            RectTransform rtInitial = initialGo.GetComponent<RectTransform>();
            rtInitial.anchorMin = Vector2.zero;
            rtInitial.anchorMax = Vector2.one;
            rtInitial.offsetMin = Vector2.zero;
            rtInitial.offsetMax = Vector2.zero;
            comicPortraitInitial = initialGo.GetComponent<Text>();
            comicPortraitInitial.font = font;
            comicPortraitInitial.fontSize = 68;
            comicPortraitInitial.fontStyle = FontStyle.Bold;
            comicPortraitInitial.alignment = TextAnchor.MiddleCenter;
            comicPortraitInitial.color = Color.white;

            // Nombre y título del personaje
            GameObject speakerGo = new GameObject("SpeakerName", typeof(RectTransform), typeof(Text));
            speakerGo.transform.SetParent(innerBorder.transform, false);
            RectTransform rtSpeaker = speakerGo.GetComponent<RectTransform>();
            rtSpeaker.anchorMin = new Vector2(0.20f, 0.72f);
            rtSpeaker.anchorMax = new Vector2(0.95f, 0.94f);
            rtSpeaker.offsetMin = Vector2.zero;
            rtSpeaker.offsetMax = Vector2.zero;
            comicSpeakerName = speakerGo.GetComponent<Text>();
            comicSpeakerName.font = font;
            comicSpeakerName.fontSize = 28;
            comicSpeakerName.fontStyle = FontStyle.Bold;
            comicSpeakerName.color = new Color(1f, 0.85f, 0.35f);
            comicSpeakerName.alignment = TextAnchor.MiddleLeft;

            GameObject titleGo = new GameObject("SpeakerTitle", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(innerBorder.transform, false);
            RectTransform rtTitle = titleGo.GetComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.20f, 0.56f);
            rtTitle.anchorMax = new Vector2(0.95f, 0.72f);
            rtTitle.offsetMin = Vector2.zero;
            rtTitle.offsetMax = Vector2.zero;
            comicSpeakerTitle = titleGo.GetComponent<Text>();
            comicSpeakerTitle.font = font;
            comicSpeakerTitle.fontSize = 18;
            comicSpeakerTitle.color = new Color(0.75f, 0.75f, 0.78f);
            comicSpeakerTitle.alignment = TextAnchor.MiddleLeft;

            // Bocadillo de diálogo / Viñeta narrativa (Derecha)
            GameObject dialogueGo = new GameObject("ComicDialogueText", typeof(RectTransform), typeof(Text));
            dialogueGo.transform.SetParent(innerBorder.transform, false);
            RectTransform rtDial = dialogueGo.GetComponent<RectTransform>();
            rtDial.anchorMin = new Vector2(0.20f, 0.18f);
            rtDial.anchorMax = new Vector2(0.96f, 0.54f);
            rtDial.offsetMin = Vector2.zero;
            rtDial.offsetMax = Vector2.zero;
            comicDialogueText = dialogueGo.GetComponent<Text>();
            comicDialogueText.font = font;
            comicDialogueText.fontSize = 26;
            comicDialogueText.color = new Color(0.98f, 0.96f, 0.92f);
            comicDialogueText.alignment = TextAnchor.UpperLeft;
            comicDialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Pista para avanzar viñeta
            GameObject hintGo = new GameObject("ComicAdvanceHint", typeof(RectTransform), typeof(Text));
            hintGo.transform.SetParent(innerBorder.transform, false);
            RectTransform rtHint = hintGo.GetComponent<RectTransform>();
            rtHint.anchorMin = new Vector2(0.5f, 0.03f);
            rtHint.anchorMax = new Vector2(0.98f, 0.16f);
            rtHint.offsetMin = Vector2.zero;
            rtHint.offsetMax = Vector2.zero;
            comicAdvanceHint = hintGo.GetComponent<Text>();
            comicAdvanceHint.font = font;
            comicAdvanceHint.fontSize = 18;
            comicAdvanceHint.fontStyle = FontStyle.Bold;
            comicAdvanceHint.color = new Color(0.95f, 0.65f, 0.25f);
            comicAdvanceHint.alignment = TextAnchor.MiddleRight;
            comicAdvanceHint.text = "Avanzar viñeta [ESPACIO / F] ▶";

            comicPanelRoot.SetActive(false);
            rootPanel.SetActive(false);
        }

        public static string ResolveCutscenePath(string fileName, out bool exists)
        {
            string[] candidates =
            {
                Path.Combine(Application.streamingAssetsPath, "Cutscenes", fileName),
                Path.Combine(Application.dataPath, "StreamingAssets", "Cutscenes", fileName),
                Path.Combine(Application.dataPath, "Prototipo3", "StreamingAssets", "Cutscenes", fileName),
                Path.Combine(Application.dataPath, "Prototipo3", "Scripts", "Cinematics", fileName),
                Path.Combine(Application.dataPath, "Prototipo3", "UI", "Videos", fileName)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    exists = true;
                    return candidates[i];
                }
            }

            exists = false;
            return candidates[0];
        }

        public void PlayCutscene(string fileName, Transform fallbackCameraTarget, string subtitleSummary, Action onCompleted = null)
        {
            BuildCutsceneUI();

            if (isPlayingCutscene && string.Equals(currentCutsceneFile, fileName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[CutsceneManager] Cinemática '{fileName}' ya se encuentra activa. Agregando callback.");
                if (onCompleted != null)
                {
                    Action previous = pendingOnCompleted;
                    pendingOnCompleted = () => { previous?.Invoke(); onCompleted.Invoke(); };
                }
                return;
            }

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                RestoreGameplayFromCutscene();
                Action prev = pendingOnCompleted;
                pendingOnCompleted = null;
                prev?.Invoke();
            }

            currentCutsceneFile = fileName;
            pendingOnCompleted = onCompleted;
            activeRoutine = StartCoroutine(CutsceneRoutine(fileName, fallbackCameraTarget, subtitleSummary));
        }

        public void SkipCurrentCutscene()
        {
            if (!isPlayingCutscene) return;
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            FinishCutscene();
        }

        private IEnumerator CutsceneRoutine(string fileName, Transform fallbackCameraTarget, string subtitleSummary)
        {
            isPlayingCutscene = true;
            PauseGameplayForCutscene();

            rootPanel.SetActive(true);
            string resolvedPath = ResolveCutscenePath(fileName, out bool mp4Exists);

            if (mp4Exists)
            {
                // MODO 1: Video MP4 a pantalla completa
                comicPanelRoot.SetActive(false);
                if (videoRenderTexture == null)
                {
                    videoRenderTexture = new RenderTexture(1920, 1080, 0);
                }

                videoRawImage.gameObject.SetActive(true);
                videoRawImage.texture = videoRenderTexture;
                videoRawImage.color = Color.white;

                headerText.text = $"CINEMÁTICA  •  {fileName}";
                subtitleText.text = subtitleSummary;

                videoPlayer.targetTexture = videoRenderTexture;
                videoPlayer.url = resolvedPath;
                videoPlayer.Prepare();

                float prepareTimeout = 3.0f;
                while (!videoPlayer.isPrepared && prepareTimeout > 0f)
                {
                    prepareTimeout -= Time.unscaledDeltaTime;
                    if (CheckSkipInput())
                    {
                        FinishCutscene();
                        yield break;
                    }
                    yield return null;
                }

                if (videoPlayer.isPrepared)
                {
                    videoPlayer.Play();
                    yield return null;
                    yield return null;

                    while (videoPlayer.isPlaying)
                    {
                        if (CheckSkipInput())
                        {
                            FinishCutscene();
                            yield break;
                        }
                        yield return null;
                    }
                }
                else
                {
                    yield return FallbackComicRoutine(fileName, fallbackCameraTarget, subtitleSummary);
                    yield break;
                }
            }
            else
            {
                // MODO 2: Respaldo de Novela Gráfica / Viñeta de Cómic In-Engine
                yield return FallbackComicRoutine(fileName, fallbackCameraTarget, subtitleSummary);
                yield break;
            }

            FinishCutscene();
        }

        private IEnumerator FallbackComicRoutine(string fileName, Transform fallbackCameraTarget, string subtitleSummary)
        {
            Debug.Log($"[CutsceneManager] Archivo '{fileName}' no presente. Ejecutando Novela Gráfica / Viñeta de Cómic In-Engine.");

            videoRawImage.gameObject.SetActive(false);
            headerText.text = $"HISTORIA DE SOLIS  •  {fileName.Replace(".mp4", "")}";
            subtitleText.text = subtitleSummary;

            IsometricCameraFollow isoCam = FindAnyObjectByType<IsometricCameraFollow>();
            if (isoCam != null && fallbackCameraTarget != null)
            {
                isoCam.SetCutsceneOverrideTarget(fallbackCameraTarget, 12.5f);
            }

            ComicSceneData sceneData = GetComicSceneData(fileName, subtitleSummary);

            comicPanelRoot.SetActive(true);
            comicPortraitBox.color = sceneData.color;
            comicPortraitInitial.text = sceneData.initial;
            comicSpeakerName.text = sceneData.speaker;
            comicSpeakerTitle.text = sceneData.title;

            // Recorrer cada viñeta / línea de diálogo avanzando con Espacio o F
            for (int i = 0; i < sceneData.lines.Length; i++)
            {
                comicDialogueText.text = sceneData.lines[i];
                comicAdvanceHint.text = i < sceneData.lines.Length - 1
                    ? $"[ESPACIO / F] Siguiente viñeta ({i + 1}/{sceneData.lines.Length}) ▶"
                    : "Continuar juego [ESPACIO / F] ▶";

                yield return new WaitForSecondsRealtime(0.25f);

                float lineElapsed = 0f;
                float autoAdvanceTimeout = 6.5f;

                while (lineElapsed < autoAdvanceTimeout)
                {
                    lineElapsed += Time.unscaledDeltaTime;

                    if (CheckAdvanceInput())
                    {
                        break;
                    }

                    if (CheckSkipInput())
                    {
                        if (isoCam != null) isoCam.ClearCutsceneOverrideTarget();
                        comicPanelRoot.SetActive(false);
                        FinishCutscene();
                        yield break;
                    }

                    yield return null;
                }
            }

            comicPanelRoot.SetActive(false);

            if (isoCam != null)
            {
                isoCam.ClearCutsceneOverrideTarget();
            }

            FinishCutscene();
        }

        private ComicSceneData GetComicSceneData(string fileName, string fallbackSummary)
        {
            string key = fileName.ToLowerInvariant();

            if (key.Contains("01_intro") || key.Contains("sofia"))
            {
                return new ComicSceneData
                {
                    speaker = "SOFÍA",
                    title = "Conejita Albina • Barrio Residencial",
                    color = new Color(0.92f, 0.88f, 0.84f),
                    initial = "S",
                    lines = new string[]
                    {
                        "¡Kilo, despierta! El humo ámbar de la Torre Aethelgard ha cubierto todo el horizonte de Solis...",
                        "Los vecinos del barrio actúan de forma extraña cuando cae la tarde. Habla con el tejón Marco cerca de tu Base antes del anochecer.",
                        "Recuerda: la noche transforma a quienes respiran la niebla. Fabrica tus defensas en la mesa de trabajo."
                    }
                };
            }

            if (key.Contains("02_marco") || key.Contains("feral"))
            {
                return new ComicSceneData
                {
                    speaker = "MARCO (FERAL)",
                    title = "Tejón Conserje • Transformación Feral",
                    color = new Color(0.85f, 0.28f, 0.22f),
                    initial = "M",
                    lines = new string[]
                    {
                        "¡Grrr... Kilo, aléjate de mí! ¡Ese dolor en las sienes... me nubla la vista!",
                        "Las venas de Marco se iluminan con un resplandor ámbar. Ha perdido el control y entra en fase feral.",
                        "Usa sigilo [C] y el Dash [Espacio] para escapar de su alcance y sobrevivir hasta el amanecer."
                    }
                };
            }

            if (key.Contains("03_toby") || key.Contains("escuela"))
            {
                return new ComicSceneData
                {
                    speaker = "TOBY",
                    title = "Mapache Ingeniero • Taller de Bellavista",
                    color = new Color(0.35f, 0.65f, 0.85f),
                    initial = "T",
                    lines = new string[]
                    {
                        "¡Por aquí, pequeño Kilo! Entra al taller antes de que los vigilantes de la escuela te descubran.",
                        "El Rector Morales cerró el laboratorio de química. Su oído de búho es implacable: detecta cualquier carrera sin sigilo.",
                        "Si me ayudas con un favor secundario te daré la llave del ducto; si no, fabrica el Señuelo Acústico [1] para burlar su patrulla."
                    }
                };
            }

            if (key.Contains("04_buho") || key.Contains("apagon") || key.Contains("morales"))
            {
                return new ComicSceneData
                {
                    speaker = "RECTOR H. MORALES",
                    title = "Búho Real • 'El Silenciador'",
                    color = new Color(0.85f, 0.55f, 0.20f),
                    initial = "B",
                    lines = new string[]
                    {
                        "¡Apagón en la Escuela Secundaria Bellavista! ¡Nadie saldrá con el Vial de Enzima Base!",
                        "Morales sobrevuela los pasillos en penumbra. Despliega una barricada de madera [3] para frenar su vuelo rasante.",
                        "Atrapa al rector con la trampa de red [2], activa la alarma del laboratorio y asegura el vial."
                    }
                };
            }

            if (key.Contains("05_sultan") || key.Contains("luzfria") || key.Contains("bosque"))
            {
                return new ComicSceneData
                {
                    speaker = "LOBO SULTÁN",
                    title = "Lobo Guardabosques • Luz Fría Restauradora",
                    color = new Color(0.35f, 0.75f, 0.45f),
                    initial = "L",
                    lines = new string[]
                    {
                        "¡Los 3 postes reflectores del arroyo destellan con luz fría! La radiación UV disipa la bruma feral.",
                        "Sultán cae de rodillas al recuperar la cordura. 'Gracias, Kilo... Mi mente vuelve a ser libre.'",
                        "Toma el Filtro de Carbón Biológico y la llave del drenaje. El Hospital San Judas es el siguiente paso."
                    }
                };
            }

            if (key.Contains("06_hospital") || key.Contains("salgado") || key.Contains("clara"))
            {
                return new ComicSceneData
                {
                    speaker = "DR. ARTURO SALGADO",
                    title = "Tortuga Pediatra • Hospital San Judas",
                    color = new Color(0.30f, 0.55f, 0.85f),
                    initial = "H",
                    lines = new string[]
                    {
                        "¡El Cilindro de Oxígeno Puro y el nebulizador estabilizaron a Sofía en la habitación 304! Ha superado la crisis.",
                        "Una transmisión estática de radio interrumpe la clínica: '¡Kilo... soy la Dra. Elena Vargas desde la Torre Aethelgard!'",
                        "'Kross va a liberar la toxina Helios-Omega sobre Solis. Sube al anillo periférico... ¡Eres nuestra última esperanza!'"
                    }
                };
            }

            if (key.Contains("07_final") || key.Contains("amanecer") || key.Contains("kross"))
            {
                return new ComicSceneData
                {
                    speaker = "DRA. ELENA VARGAS",
                    title = "Nutria Bioquímica • Victoria y Amanecer",
                    color = new Color(1.0f, 0.82f, 0.25f),
                    initial = "E",
                    lines = new string[]
                    {
                        "¡El Suero Alba-Puro ha entrado a la chimenea central de Aethelgard! Los condensadores UV de Kross han colapsado.",
                        "El gas ámbar se disipa sobre los techos de la ciudad. El primer amanecer puro ilumina las calles de Solis.",
                        "Kilo contempla el nuevo día desde la cúspide de la torre. Todos los habitantes han recuperado su libertad."
                    }
                };
            }

            return new ComicSceneData
            {
                speaker = "CRÓNICA DE SOLIS",
                title = "Secuencia Argumental In-Engine",
                color = new Color(0.6f, 0.6f, 0.65f),
                initial = "F",
                lines = new string[] { fallbackSummary }
            };
        }

        private bool CheckAdvanceInput()
        {
            return FeralInput.Pressed(KeyCode.Space) || FeralInput.Pressed(KeyCode.F) ||
                   Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F);
        }

        private bool CheckSkipInput()
        {
            return FeralInput.Pressed(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Escape);
        }

        private void PauseGameplayForCutscene()
        {
            HamsterController hc = FindAnyObjectByType<HamsterController>();
            if (hc != null)
            {
                wasPlayerEnabled = hc.enabled;
                Rigidbody rb = hc.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
                hc.enabled = false;
            }

            pausedAgents.Clear();
            NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            for (int i = 0; i < agents.Length; i++)
            {
                NavMeshAgent a = agents[i];
                if (a != null && a.enabled && a.isOnNavMesh && !a.isStopped)
                {
                    a.isStopped = true;
                    pausedAgents.Add(a);
                }
            }
        }

        private void RestoreGameplayFromCutscene()
        {
            for (int i = 0; i < pausedAgents.Count; i++)
            {
                NavMeshAgent a = pausedAgents[i];
                if (a != null && a.enabled && a.isOnNavMesh)
                {
                    a.isStopped = false;
                }
            }
            pausedAgents.Clear();

            IsometricCameraFollow isoCam = FindAnyObjectByType<IsometricCameraFollow>();
            if (isoCam != null)
            {
                isoCam.ClearCutsceneOverrideTarget();
            }

            HamsterController hc = FindAnyObjectByType<HamsterController>();
            if (hc != null && !hc.IsDead)
            {
                hc.enabled = wasPlayerEnabled;
            }
        }

        private void FinishCutscene()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }

            isPlayingCutscene = false;
            activeRoutine = null;
            RestoreGameplayFromCutscene();

            Action callback = pendingOnCompleted;
            pendingOnCompleted = null;
            callback?.Invoke();
        }

        private void OnDestroy()
        {
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }
            if (Instance == this) Instance = null;
        }
    }
}
