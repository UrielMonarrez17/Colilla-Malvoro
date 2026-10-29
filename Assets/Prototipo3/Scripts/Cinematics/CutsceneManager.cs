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
    /// PASO 6: Reproductor de Cinemáticas MP4 con Fallback In-Engine automático de 6 segundos.
    /// Verifica Application.streamingAssetsPath + "/Cutscenes/" + fileName (y Assets/Prototipo3/StreamingAssets/Cutscenes/).
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

        private Coroutine activeRoutine;
        private Action pendingOnCompleted;
        private readonly List<NavMeshAgent> pausedAgents = new List<NavMeshAgent>();
        private bool wasPlayerEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildCutsceneUI();
        }

        public static CutsceneManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("CutsceneManager");
            return go.AddComponent<CutsceneManager>();
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

            // Fondo completo para video MP4
            GameObject rawImgGo = new GameObject("MP4_VideoScreen", typeof(RectTransform), typeof(RawImage));
            rawImgGo.transform.SetParent(rootPanel.transform, false);
            RectTransform rtVideo = rawImgGo.GetComponent<RectTransform>();
            rtVideo.anchorMin = Vector2.zero;
            rtVideo.anchorMax = Vector2.one;
            rtVideo.offsetMin = Vector2.zero;
            rtVideo.offsetMax = Vector2.zero;
            videoRawImage = rawImgGo.GetComponent<RawImage>();
            videoRawImage.color = Color.black;

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.isLooping = false;

            // Barras cinematográficas (Letterbox Superior e Inferior)
            GameObject topBar = new GameObject("LetterboxTop", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(rootPanel.transform, false);
            RectTransform rtTop = topBar.GetComponent<RectTransform>();
            rtTop.anchorMin = new Vector2(0f, 0.86f);
            rtTop.anchorMax = new Vector2(1f, 1f);
            rtTop.offsetMin = Vector2.zero;
            rtTop.offsetMax = Vector2.zero;
            topBar.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.94f);

            GameObject bottomBar = new GameObject("LetterboxBottom", typeof(RectTransform), typeof(Image));
            bottomBar.transform.SetParent(rootPanel.transform, false);
            RectTransform rtBottom = bottomBar.GetComponent<RectTransform>();
            rtBottom.anchorMin = new Vector2(0f, 0f);
            rtBottom.anchorMax = new Vector2(1f, 0.22f);
            rtBottom.offsetMin = Vector2.zero;
            rtBottom.offsetMax = Vector2.zero;
            fallbackBlackoutBg = bottomBar.GetComponent<Image>();
            fallbackBlackoutBg.color = new Color(0.02f, 0.02f, 0.03f, 0.94f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Título de Cinemática en barra superior
            GameObject headerGo = new GameObject("CutsceneHeaderText", typeof(RectTransform), typeof(Text));
            headerGo.transform.SetParent(topBar.transform, false);
            RectTransform rtHeader = headerGo.GetComponent<RectTransform>();
            rtHeader.anchorMin = new Vector2(0.04f, 0.1f);
            rtHeader.anchorMax = new Vector2(0.7f, 0.9f);
            rtHeader.offsetMin = Vector2.zero;
            rtHeader.offsetMax = Vector2.zero;
            headerText = headerGo.GetComponent<Text>();
            headerText.font = font;
            headerText.fontSize = 24;
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = new Color(0.95f, 0.78f, 0.35f, 1f);
            headerText.alignment = TextAnchor.MiddleLeft;

            // Subtítulos en barra inferior
            GameObject subGo = new GameObject("CutsceneSubtitleText", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(bottomBar.transform, false);
            RectTransform rtSub = subGo.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.08f, 0.15f);
            rtSub.anchorMax = new Vector2(0.92f, 0.88f);
            rtSub.offsetMin = Vector2.zero;
            rtSub.offsetMax = Vector2.zero;
            subtitleText = subGo.GetComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 28;
            subtitleText.color = new Color(0.96f, 0.93f, 0.85f, 1f);
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Botón de Saltar Cinemática (Espacio / Esc)
            GameObject skipBtnGo = new GameObject("SkipCutsceneButton", typeof(RectTransform), typeof(Image), typeof(Button));
            skipBtnGo.transform.SetParent(topBar.transform, false);
            RectTransform rtSkip = skipBtnGo.GetComponent<RectTransform>();
            rtSkip.anchorMin = new Vector2(1f, 0.5f);
            rtSkip.anchorMax = new Vector2(1f, 0.5f);
            rtSkip.pivot = new Vector2(1f, 0.5f);
            rtSkip.anchoredPosition = new Vector2(-40f, 0f);
            rtSkip.sizeDelta = new Vector2(310f, 54f);
            Image skipImg = skipBtnGo.GetComponent<Image>();
            skipImg.color = new Color(0.55f, 0.16f, 0.12f, 0.95f);

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
            skipHintText.text = "Saltar Cinemática [ESPACIO / ESC]";

            rootPanel.SetActive(false);
        }

        /// <summary>
        /// Resuelve la ruta del archivo MP4 buscando tanto en StreamingAssets estándar como en Assets/Prototipo3/StreamingAssets/Cutscenes/.
        /// </summary>
        public static string ResolveCutscenePath(string fileName, out bool exists)
        {
            string[] candidates =
            {
                Path.Combine(Application.streamingAssetsPath, "Cutscenes", fileName),
                Path.Combine(Application.dataPath, "Prototipo3", "StreamingAssets", "Cutscenes", fileName),
                Path.Combine(Application.dataPath, "StreamingAssets", "Cutscenes", fileName),
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

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                RestoreGameplayFromCutscene();
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
                // MODO 1: Reproducir archivo .mp4 real con VideoPlayer
                Debug.Log($"[CutsceneManager] Reproduciendo video MP4: {resolvedPath}");
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
                    // Esperar un par de frames para que isPlaying sea true
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
                    // Si el codec falla, ejecutar fallback de 6s
                    yield return FallbackInEngineRoutine(fileName, fallbackCameraTarget, subtitleSummary);
                    yield break;
                }
            }
            else
            {
                // MODO 2: Fallback In-Engine automático de 6 segundos moviendo la cámara hacia fallbackCameraTarget
                yield return FallbackInEngineRoutine(fileName, fallbackCameraTarget, subtitleSummary);
                yield break;
            }

            FinishCutscene();
        }

        private IEnumerator FallbackInEngineRoutine(string fileName, Transform fallbackCameraTarget, string subtitleSummary)
        {
            Debug.Log($"[CutsceneManager] Archivo '{fileName}' aún no copiado en Cutscenes/. Ejecutando cinemática In-Engine de respaldo (6 s).");

            videoRawImage.gameObject.SetActive(false);
            headerText.text = $"SECUENCIA CINEMÁTICA IN-ENGINE  •  {fileName.Replace(".mp4", "")}";
            subtitleText.text = subtitleSummary;

            IsometricCameraFollow isoCam = FindAnyObjectByType<IsometricCameraFollow>();
            if (isoCam != null && fallbackCameraTarget != null)
            {
                isoCam.SetCutsceneOverrideTarget(fallbackCameraTarget, 13.5f);
            }

            float elapsed = 0f;
            while (elapsed < fallbackDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                int remaining = Mathf.CeilToInt(fallbackDurationSeconds - elapsed);
                if (skipHintText != null)
                {
                    skipHintText.text = $"Saltar [{remaining}s]  [ESPACIO / ESC]";
                }

                // Permitir saltar tras 0.25s para evitar que la misma pulsación de interacción la cierre
                if (elapsed > 0.25f && CheckSkipInput())
                {
                    break;
                }
                yield return null;
            }

            if (isoCam != null)
            {
                isoCam.ClearCutsceneOverrideTarget();
            }

            FinishCutscene();
        }

        private bool CheckSkipInput()
        {
            return FeralInput.Pressed(KeyCode.Space) || FeralInput.Pressed(KeyCode.Escape) ||
                   Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape);
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
