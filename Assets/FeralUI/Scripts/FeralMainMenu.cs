using UnityEngine;
using UnityEngine.UI;

namespace Feral.UI
{
    public sealed class FeralMainMenu : MonoBehaviour
    {
        readonly string[] pageNames = { "Home", "Sessions", "Settings", "Controls", "Exit" };
        GameObject[] pages;
        int currentPage;
        Slider volume;
        Text volumeValue;
        Text audioState;
        Text status;
        bool loading;

        void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            FeralUICommon.EnsureEventSystem();
            FeralUICommon.ApplyAudio();
            pages = new GameObject[pageNames.Length];
            for (int i = 0; i < pages.Length; i++)
                pages[i] = FeralUICommon.Find<RectTransform>(transform, pageNames[i]).gameObject;
            FeralUICommon.Bind(transform, "Play", () => Show(1));
            FeralUICommon.Bind(transform, "SavedGames", () => Show(1));
            FeralUICommon.Bind(transform, "AudioSettings", () => Show(2));
            FeralUICommon.Bind(transform, "Help", () => Show(3));
            FeralUICommon.Bind(transform, "Quit", () => Show(4));
            FeralUICommon.Bind(transform, "NewGame", StartGame);
            FeralUICommon.Bind(transform, "ConfirmQuit", Quit);
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name.StartsWith("Back")) button.onClick.AddListener(() => Show(0));

            volume = FeralUICommon.Find<Slider>(transform, "MasterVolume");
            volumeValue = FeralUICommon.Find<Text>(transform, "VolumeValue");
            audioState = FeralUICommon.Find<Text>(transform, "AudioState");
            status = FeralUICommon.Find<Text>(transform, "SessionStatus");
            volume.SetValueWithoutNotify(PlayerPrefs.GetFloat("Feral.UI.MasterVolume", 0.8f));
            volume.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat("Feral.UI.MasterVolume", value);
                FeralUICommon.ApplyAudio();
                UpdateAudioLabels();
            });
            FeralUICommon.Bind(transform, "Mute", () =>
            {
                PlayerPrefs.SetInt("Feral.UI.Muted", 1 - PlayerPrefs.GetInt("Feral.UI.Muted", 0));
                FeralUICommon.ApplyAudio();
                UpdateAudioLabels();
                PlayerPrefs.Save();
            });
            FeralUICommon.Bind(transform, "ResetAudio", () =>
            {
                volume.value = 0.8f;
                PlayerPrefs.SetInt("Feral.UI.Muted", 0);
                FeralUICommon.ApplyAudio();
                UpdateAudioLabels();
                PlayerPrefs.Save();
            });
            UpdateAudioLabels();
            Show(0);
        }

        void Show(int index)
        {
            if (loading) return;
            if (currentPage == 2) PlayerPrefs.Save();
            currentPage = index;
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
            FeralUICommon.Focus(pages[index].transform);
            if (index == 1) FeralUICommon.Focus(FeralUICommon.Find<Button>(transform, "NewGame").transform);
        }

        void UpdateAudioLabels()
        {
            volumeValue.text = Mathf.RoundToInt(volume.value * 100f) + "%";
            audioState.text = PlayerPrefs.GetInt("Feral.UI.Muted", 0) == 1 ? "Sonido silenciado" : "Sonido activado";
        }

        void StartGame()
        {
            if (loading) return;
            if (!FeralUICommon.CanLoad(FeralUICommon.GameScene))
            {
                status.text = "No se encontró la escena de juego. Revisa las escenas del build.";
                return;
            }
            loading = true;
            if (InventoryManager.Instance != null) InventoryManager.Instance.Clear();
            FeralUICommon.EnteringFromMenu = true;
            PlayerPrefs.Save();
            FeralUICommon.Load(FeralUICommon.GameScene);
        }

        void Update()
        {
            if (pages != null && FeralUICommon.EscapePressed()) Show(currentPage == 0 ? 4 : 0);
        }

        void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
