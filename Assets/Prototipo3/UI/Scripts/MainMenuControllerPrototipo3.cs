using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Prototipo3.UI
{
    public class MainMenuControllerPrototipo3 : MonoBehaviour
    {
        [Header("Escena Destino (Tutorial / Prototipo3)")]
        [Tooltip("Ruta o nombre de la escena de Sandbox Prototipo 3.")]
        public string prototypeSceneName = "Assets/Prototipo3/Prototipo3.unity";
        [Tooltip("Ruta o nombre de la escena del Tutorial.")]
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
        private GameObject cityModeModal;
        private InputField seedInputField;
        private InputField widthInputField;
        private InputField lengthInputField;
        private Text modeModalStatus;

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
            var system = new GameObject("Prototipo3 EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            system.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
            system.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        private void Start()
        {
            FeralTheme.ApplyMenu(transform);
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
            BindButton("Play", OpenPlayModeSelector);
            BindButton("SavedGames", () => ShowPage(1));
            BindButton("AudioSettings", () => ShowPage(2));
            BindButton("Help", () => ShowPage(3));
            BindButton("Quit", () => ShowPage(4));

            BindButton("NewGame", OpenPlayModeSelector);
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
            if (pages[currentPage] != null)
            {
                var first = pages[currentPage].GetComponentsInChildren<Button>().FirstOrDefault(b => b.interactable);
                if (first != null && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        public void StartTutorial()
        {
            OpenPlayModeSelector();
        }

        public void OpenPlayModeSelector()
        {
            if (isLoading) return;
            EnsurePlayModeSelector();
            if (seedInputField != null && string.IsNullOrWhiteSpace(seedInputField.text))
            {
                seedInputField.text = UnityEngine.Random.Range(100000, 999999).ToString();
            }
            if (cityModeModal != null)
            {
                cityModeModal.SetActive(true);
            }
        }

        public void ClosePlayModeSelector()
        {
            if (cityModeModal != null)
            {
                cityModeModal.SetActive(false);
            }
        }

        public void LaunchTutorialScene()
        {
            if (isLoading) return;
            isLoading = true;
            if (InventoryManager.Instance != null) InventoryManager.Instance.Clear();
            PlayerPrefs.Save();

            string sceneToLoad = ResolveLoadableScene(tutorialSceneName, "Tutorial", "Assets/Scenes/Tutorial.unity");
            LoadResolvedScene(sceneToLoad, "Tutorial");
        }

        public void LaunchRandomCityPrototipo3()
        {
            int randomSeed = UnityEngine.Random.Range(100000, 999999);
            LaunchCityWithConfig(randomSeed, 30, 30);
        }

        public void LaunchCustomCityFromInputs()
        {
            int parsedSeed = UnityEngine.Random.Range(100000, 999999);
            if (seedInputField != null && int.TryParse(seedInputField.text.Trim(), out int s))
            {
                parsedSeed = Mathf.Abs(s);
            }

            int w = 30;
            if (widthInputField != null && int.TryParse(widthInputField.text.Trim(), out int pw))
            {
                w = Mathf.Clamp(pw, 20, 60);
            }

            int l = 30;
            if (lengthInputField != null && int.TryParse(lengthInputField.text.Trim(), out int pl))
            {
                l = Mathf.Clamp(pl, 20, 60);
            }

            LaunchCityWithConfig(parsedSeed, w, l);
        }

        private void LaunchCityWithConfig(int targetSeed, int width, int length)
        {
            if (isLoading) return;
            isLoading = true;

            CityGeneratorPrototipo3.SetPendingCityConfig(targetSeed, width, length);
            if (status != null) status.text = $"Generando ciudad (Semilla {targetSeed}, {width}x{length})...";
            if (modeModalStatus != null) modeModalStatus.text = $"Cargando Prototipo 3 · Semilla {targetSeed}...";
            if (InventoryManager.Instance != null) InventoryManager.Instance.Clear();
            PlayerPrefs.Save();

            string sceneToLoad = ResolveLoadableScene(prototypeSceneName, "Prototipo3", "Assets/Prototipo3/Prototipo3.unity");
            LoadResolvedScene(sceneToLoad, "Prototipo3");
        }

        private static string ResolveLoadableScene(params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                string c = candidates[i];
                if (string.IsNullOrEmpty(c)) continue;
                if (Application.CanStreamedLevelBeLoaded(c) || SceneUtility.GetBuildIndexByScenePath(c) >= 0)
                {
                    return c;
                }
            }
            return null;
        }

        private void LoadResolvedScene(string sceneToLoad, string label)
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
                return;
            }

            isLoading = false;
            if (status != null) status.text = $"No se encontró la escena '{label}' en Build Settings.";
            if (modeModalStatus != null) modeModalStatus.text = $"No se encontró la escena '{label}' en Build Settings.";
            Debug.LogError($"[MainMenuControllerPrototipo3] No se encontró la escena '{label}' en Build Settings.");
        }

        private void EnsurePlayModeSelector()
        {
            if (cityModeModal != null) return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform host = parentCanvas != null ? parentCanvas.transform : transform;

            cityModeModal = new GameObject("CityModeSelectorModal", typeof(RectTransform), typeof(Image));
            cityModeModal.transform.SetParent(host, false);
            RectTransform rootRt = cityModeModal.GetComponent<RectTransform>();
            FeralTheme.Stretch(rootRt);
            cityModeModal.GetComponent<Image>().color = new Color(0.02f, 0.015f, 0.01f, 0.85f);

            var card = FeralTheme.Brush("SelectorCard", cityModeModal.transform, 0, 0, 760, 620, FeralTheme.Ink).rectTransform;
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;

            FeralTheme.Panel("AccentTop", card, 0, 0, 760, 5, FeralTheme.Red);
            FeralTheme.Label("Kicker", card, "FERAL  ·  SELECCIÓN DE PARTIDA", 36, 26, 688, 28, 16, FeralTheme.Gold);
            FeralTheme.Label("Title", card, "Elige cómo empezar", 36, 58, 688, 48, 36);
            FeralTheme.Label("Subtitle", card, "Practica primero en el tutorial o entra directo a una ciudad procedural.", 36, 110, 688, 32, 18, FeralTheme.Muted);

            FeralTheme.Button("BtnTutorial", card, "1. Jugar Tutorial Guiado", 36, 162, 688, 56, LaunchTutorialScene, false);
            FeralTheme.Button("BtnRandomCity", card, "2. Ciudad Aleatoria (Prototipo 3)", 36, 232, 688, 58, LaunchRandomCityPrototipo3, true);

            var customBox = FeralTheme.Panel("CustomCityBox", card, 36, 308, 688, 226, FeralTheme.Card).rectTransform;
            FeralTheme.Label("CustomTitle", customBox, "3. Personalizar Ciudad Procedural", 24, 16, 640, 32, 22, FeralTheme.Gold);

            FeralTheme.Label("SeedLbl", customBox, "Semilla numérica:", 24, 60, 190, 30, 17, FeralTheme.Cream);
            seedInputField = CreateMenuInputField(customBox, "SeedInput", UnityEngine.Random.Range(100000, 999999).ToString(), 220, 56, 230, 38);

            FeralTheme.Button("RandSeedBtn", customBox, "Azar", 464, 56, 196, 38, () =>
            {
                if (seedInputField != null) seedInputField.text = UnityEngine.Random.Range(100000, 999999).ToString();
            }, false);

            FeralTheme.Label("SizeLbl", customBox, "Tamaño (20 - 60):", 24, 108, 190, 30, 17, FeralTheme.Cream);
            widthInputField = CreateMenuInputField(customBox, "WidthInput", "30", 220, 104, 100, 38);
            FeralTheme.Label("SizeX", customBox, "x", 328, 108, 24, 30, 18, FeralTheme.Muted);
            lengthInputField = CreateMenuInputField(customBox, "LengthInput", "30", 350, 104, 100, 38);

            FeralTheme.Button("BtnCustomCity", customBox, "Generar y Jugar Ciudad Personalizada", 24, 158, 640, 50, LaunchCustomCityFromInputs, true);

            modeModalStatus = FeralTheme.Label("ModeStatus", card, "", 36, 544, 460, 44, 16, FeralTheme.Gold);
            FeralTheme.Button("BtnCloseModal", card, "Cerrar  [ESC]", 520, 548, 204, 48, ClosePlayModeSelector, false);

            cityModeModal.SetActive(false);
        }

        private static InputField CreateMenuInputField(Transform parent, string name, string initialValue, float x, float y, float w, float h)
        {
            var bg = FeralTheme.Panel(name, parent, x, y, w, h, FeralTheme.Ink);
            var text = FeralTheme.Label("Text", bg.transform, "", 10, 4, w - 20, h - 8, 18, FeralTheme.Cream);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            var input = bg.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.text = initialValue;
            return input;
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
                if (cityModeModal != null && cityModeModal.activeSelf)
                {
                    ClosePlayModeSelector();
                    return;
                }
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
