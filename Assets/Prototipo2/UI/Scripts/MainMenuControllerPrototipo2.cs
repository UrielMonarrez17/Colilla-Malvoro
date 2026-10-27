using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Prototipo2.UI
{
    public class MainMenuControllerPrototipo2 : MonoBehaviour
    {
        [Header("Escena Destino (Tutorial)")]
        [Tooltip("Ruta o nombre de la escena del Tutorial a cargar.")]
        public string tutorialSceneName = "Assets/Scenes/Tutorial.unity";

        [Header("Video Introductorio Opcional")]
        public VideoPlayer videoPlayer;
        public RawImage videoDisplay;
        public GameObject videoContainer;
        public Button toggleVideoButton;

        private readonly string[] pageNames = { "Home", "Sessions", "Settings", "Controls", "Exit" };
        private GameObject[] pages;
        private int currentPage = 0;
        private Slider volumeSlider;
        private Text volumeValue;
        private Text audioState;
        private Text status;
        private bool isLoading = false;

        private const string PREF_VOLUME = "Feral.UI.MasterVolume";
        private const string PREF_MUTED = "Feral.UI.Muted";

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EnsureEventSystem();
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var system = new GameObject("Prototipo2 EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            system.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
            system.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        private void Start()
        {
            ApplySavedAudio();
            InitializePages();
            InitializeButtons();
            InitializeAudioControls();
            InitializeVideoPlayer();

            ShowPage(0);
        }

        private void InitializePages()
        {
            pages = new GameObject[pageNames.Length];
            for (int i = 0; i < pageNames.Length; i++)
            {
                Transform t = FindChildRecursive(transform, pageNames[i]);
                if (t != null)
                {
                    pages[i] = t.gameObject;
                }
            }
        }

        private void InitializeButtons()
        {
            BindButton("Play", StartTutorial);
            BindButton("SavedGames", () => ShowPage(1));
            BindButton("AudioSettings", () => ShowPage(2));
            BindButton("Help", () => ShowPage(3));
            BindButton("Quit", () => ShowPage(4));

            BindButton("NewGame", StartTutorial);
            BindButton("ConfirmQuit", QuitGame);

            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.name.StartsWith("Back", StringComparison.OrdinalIgnoreCase))
                {
                    b.onClick.AddListener(() => ShowPage(0));
                }
            }

            status = FindComponentRecursive<Text>(transform, "SessionStatus");
        }

        private void InitializeAudioControls()
        {
            volumeSlider = FindComponentRecursive<Slider>(transform, "MasterVolume");
            volumeValue = FindComponentRecursive<Text>(transform, "VolumeValue");
            audioState = FindComponentRecursive<Text>(transform, "AudioState");

            if (volumeSlider != null)
            {
                float savedVol = PlayerPrefs.GetFloat(PREF_VOLUME, 0.8f);
                volumeSlider.SetValueWithoutNotify(savedVol);
                volumeSlider.onValueChanged.AddListener(val =>
                {
                    PlayerPrefs.SetFloat(PREF_VOLUME, val);
                    ApplySavedAudio();
                    UpdateAudioLabels();
                });
            }

            BindButton("Mute", () =>
            {
                int currentMuted = PlayerPrefs.GetInt(PREF_MUTED, 0);
                PlayerPrefs.SetInt(PREF_MUTED, 1 - currentMuted);
                PlayerPrefs.Save();
                ApplySavedAudio();
                UpdateAudioLabels();
            });

            BindButton("ResetAudio", () =>
            {
                if (volumeSlider != null) volumeSlider.value = 0.8f;
                PlayerPrefs.SetFloat(PREF_VOLUME, 0.8f);
                PlayerPrefs.SetInt(PREF_MUTED, 0);
                PlayerPrefs.Save();
                ApplySavedAudio();
                UpdateAudioLabels();
            });

            UpdateAudioLabels();
        }

        private void InitializeVideoPlayer()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            }

            if (toggleVideoButton != null)
            {
                toggleVideoButton.onClick.AddListener(ToggleVideoCinematic);
            }
            else
            {
                BindButton("ToggleVideo", ToggleVideoCinematic);
            }
        }

        public void ToggleVideoCinematic()
        {
            if (videoPlayer == null) return;

            if (videoContainer != null)
            {
                bool active = !videoContainer.activeSelf;
                videoContainer.SetActive(active);
                if (active) videoPlayer.Play();
                else videoPlayer.Pause();
            }
            else
            {
                if (videoPlayer.isPlaying) videoPlayer.Pause();
                else videoPlayer.Play();
            }
        }

        public void ShowPage(int index)
        {
            if (isLoading || pages == null) return;

            if (currentPage == 2)
            {
                PlayerPrefs.Save();
            }

            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                {
                    pages[i].SetActive(i == currentPage);
                }
            }
        }

        public void StartTutorial()
        {
            if (isLoading) return;
            isLoading = true;

            if (status != null)
            {
                status.text = "Iniciando Tutorial...";
            }

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.Clear();
            }

            PlayerPrefs.Save();

            // Cargar escena de Tutorial
            if (Application.CanStreamedLevelBeLoaded(tutorialSceneName))
            {
                SceneManager.LoadScene(tutorialSceneName);
            }
            else if (Application.CanStreamedLevelBeLoaded("Tutorial"))
            {
                SceneManager.LoadScene("Tutorial");
            }
            else
            {
                // Cargar por índice 1 (definido en EditorBuildSettings)
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    SceneManager.LoadScene(1);
                }
                else
                {
                    Debug.LogError("[MainMenuControllerPrototipo2] No se encontró la escena Tutorial en Build Settings.");
                    isLoading = false;
                }
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void UpdateAudioLabels()
        {
            float vol = volumeSlider != null ? volumeSlider.value : PlayerPrefs.GetFloat(PREF_VOLUME, 0.8f);
            bool isMuted = PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;

            if (volumeValue != null)
            {
                volumeValue.text = Mathf.RoundToInt(vol * 100f) + "%";
            }

            if (audioState != null)
            {
                audioState.text = isMuted ? "Sonido silenciado" : "Sonido activado";
            }
        }

        public static void ApplySavedAudio()
        {
            bool isMuted = PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;
            float vol = PlayerPrefs.GetFloat(PREF_VOLUME, 0.8f);
            AudioListener.volume = isMuted ? 0f : vol;
        }

        private void Update()
        {
            if (pages == null) return;

            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escapePressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape)) escapePressed = true;
#endif
            if (escapePressed)
            {
                ShowPage(currentPage == 0 ? 4 : 0);
            }
        }

        private void BindButton(string name, Action action)
        {
            Button btn = FindComponentRecursive<Button>(transform, name);
            if (btn != null)
            {
                btn.onClick.AddListener(() => action());
            }
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            return null;
        }

        private T FindComponentRecursive<T>(Transform parent, string objectName) where T : Component
        {
            foreach (T comp in parent.GetComponentsInChildren<T>(true))
            {
                if (comp.name.Equals(objectName, StringComparison.OrdinalIgnoreCase) ||
                    comp.name.Equals(objectName + "Label", StringComparison.OrdinalIgnoreCase) ||
                    comp.name.Equals(objectName + "Button", StringComparison.OrdinalIgnoreCase))
                    return comp;
            }
            return null;
        }
    }
}
