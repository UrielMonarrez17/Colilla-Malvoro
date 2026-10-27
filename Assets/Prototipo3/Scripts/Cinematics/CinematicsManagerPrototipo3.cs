using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Prototipo3.Cinematics
{
    public class CinematicsManagerPrototipo3 : MonoBehaviour
    {
        private static CinematicsManagerPrototipo3 _instance;
        public static CinematicsManagerPrototipo3 Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CinematicsManagerPrototipo3>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CinematicsManagerPrototipo3");
                        _instance = go.AddComponent<CinematicsManagerPrototipo3>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Referencias de Video")]
        public VideoPlayer videoPlayer;
        public CanvasGroup canvasGroup;
        public RawImage videoDisplay;
        public Text skipHintText;

        [Header("Estado")]
        public bool isPlaying = false;
        private Action currentOnComplete;
        private RenderTexture videoRenderTexture;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            SetupComponentsIfNeeded();
        }

        private void SetupComponentsIfNeeded()
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
                videoRenderTexture.Create();
            }

            videoPlayer.targetTexture = videoRenderTexture;

            if (canvasGroup == null || videoDisplay == null)
            {
                var canvasObj = new GameObject("CinematicsUI_Canvas");
                canvasObj.transform.SetParent(this.transform);
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
                canvasGroup = canvasObj.AddComponent<CanvasGroup>();

                var bgObj = new GameObject("VideoBackground");
                bgObj.transform.SetParent(canvasObj.transform, false);
                var bgImg = bgObj.AddComponent<Image>();
                bgImg.color = Color.black;
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                var rawObj = new GameObject("VideoRawImage");
                rawObj.transform.SetParent(canvasObj.transform, false);
                videoDisplay = rawObj.AddComponent<RawImage>();
                var rawRect = rawObj.GetComponent<RectTransform>();
                rawRect.anchorMin = Vector2.zero;
                rawRect.anchorMax = Vector2.one;
                rawRect.offsetMin = Vector2.zero;
                rawRect.offsetMax = Vector2.zero;
                videoDisplay.texture = videoRenderTexture;

                if (skipHintText == null)
                {
                    var textObj = new GameObject("SkipHintText");
                    textObj.transform.SetParent(canvasObj.transform, false);
                    skipHintText = textObj.AddComponent<Text>();
                    skipHintText.text = "[ESPACIO / ENTER] Saltar Cinemática";
                    skipHintText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    skipHintText.fontSize = 22;
                    skipHintText.alignment = TextAnchor.LowerRight;
                    skipHintText.color = new Color(1f, 1f, 1f, 0.7f);
                    var textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = new Vector2(0.5f, 0f);
                    textRect.anchorMax = new Vector2(1f, 0f);
                    textRect.pivot = new Vector2(1f, 0f);
                    textRect.anchoredPosition = new Vector2(-40f, 30f);
                    textRect.sizeDelta = new Vector2(600f, 50f);
                }
            }
            else if (videoDisplay != null)
            {
                videoDisplay.texture = videoRenderTexture;
            }

            videoPlayer.loopPointReached += OnVideoFinished;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void Update()
        {
            if (isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                {
                    SkipCinematic();
                }
            }
        }

        public void PlayCinematic(string videoFileName, Action onComplete = null)
        {
            if (!videoFileName.EndsWith(".mp4"))
            {
                videoFileName += ".mp4";
            }

            string fullPath = Path.Combine(Application.dataPath, "Prototipo3", "Videos", videoFileName);
            if (!File.Exists(fullPath))
            {
                string streaming = Path.Combine(Application.streamingAssetsPath, videoFileName);
                if (File.Exists(streaming))
                {
                    fullPath = streaming;
                }
                else
                {
                    Debug.LogWarning($"[CinematicsManagerPrototipo3] No se encontró el video en: {fullPath}");
                    onComplete?.Invoke();
                    return;
                }
            }

            currentOnComplete = onComplete;
            isPlaying = true;

            // Bloquear jugador
            Prototipo3.Player.HamsterControllerPrototipo3.SetInputLocked(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(true);
                skipHintText.text = "[ESPACIO / ENTER] Saltar Cinemática";
            }

            videoPlayer.url = fullPath;
            videoPlayer.Play();
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            EndCinematic();
        }

        public void SkipCinematic()
        {
            if (!isPlaying) return;
            videoPlayer.Stop();
            EndCinematic();
        }

        private void EndCinematic()
        {
            isPlaying = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(false);
            }

            // Desbloquear jugador
            Prototipo3.Player.HamsterControllerPrototipo3.SetInputLocked(false);

            var cb = currentOnComplete;
            currentOnComplete = null;
            cb?.Invoke();
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
            }
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
            }
        }
    }
}
