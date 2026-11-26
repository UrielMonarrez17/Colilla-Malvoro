using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Prototipo3
{
    /// <summary>
    /// Gestor musical persistente para Prototipo 3 (Menú Principal, Ciudad Procedural, Infiltración Escuela y Cancha Final).
    /// Respeta otras escenas como Tutorial.unity silenciando su reproducción al salir del ámbito de Prototipo 3.
    /// Usa Time.unscaledTime/unscaledDeltaTime para seguir funcionando correctamente incluso en pausa o pantalla de victoria (timeScale = 0).
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class MusicManagerPrototipo3 : MonoBehaviour
    {
        public enum MusicContext
        {
            None = 0,
            MainMenu = 1,
            DayGameplay = 2,
            NightOrBossChase = 3,
            FinalBossArena = 4,
            DemoVictory = 5
        }

        public static MusicManagerPrototipo3 Instance { get; private set; }

        [Header("Banco y Pistas Musicales")]
        public Prototipo3MusicBank musicBank;
        public AudioClip menuLoopClip;
        public AudioClip dayGameplayLoopClip;
        public AudioClip nightAndChaseLoopClip;
        public AudioClip finalBossLoopClip;
        public AudioClip demoVictoryClip;

        [Header("Ajustes de Reproducción")]
        [Range(0f, 1f)]
        public float musicVolume = 0.55f;
        public float crossfadeDuration = 0.7f;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private bool usingSourceA = true;
        private float fadeProgress = 1f;
        private bool victoryMusicActive = false;
        private float nextContextPollTime = 0f;
        private bool loadingFallbackVictoryClip = false;

        public MusicContext CurrentContext { get; private set; } = MusicContext.None;
        public AudioClip CurrentClip { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterFirstSceneLoad()
        {
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
            SceneManager.sceneLoaded += OnAnySceneLoaded;

            Scene active = SceneManager.GetActiveScene();
            if (IsMusicSupportedScene(active.name))
            {
                EnsureInstance().HandleSceneEntered(active.name);
            }
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsMusicSupportedScene(scene.name))
            {
                EnsureInstance().HandleSceneEntered(scene.name);
            }
            else if (Instance != null)
            {
                Instance.HandleSceneEntered(scene.name);
            }
        }

        public static bool IsMusicSupportedScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (sceneName.IndexOf("UIValidation", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (sceneName.Equals("Tutorial", StringComparison.OrdinalIgnoreCase)) return false;

            return sceneName.IndexOf("MainMenu_Prototipo3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sceneName.IndexOf("Prototipo3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sceneName.IndexOf("Escuela_Infiltracion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sceneName.IndexOf("Escuela_CanchaFinal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static MusicManagerPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;

            MusicManagerPrototipo3 existing = FindAnyObjectByType<MusicManagerPrototipo3>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            GameObject go = new GameObject("MusicManager_Prototipo3");
            Instance = go.AddComponent<MusicManagerPrototipo3>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Instance.AdoptMissingClipsFrom(this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
            ResolveMusicClips();
        }

        private void Start()
        {
            if (Instance != this) return;
            HandleSceneEntered(SceneManager.GetActiveScene().name);
        }

        private void AdoptMissingClipsFrom(MusicManagerPrototipo3 other)
        {
            if (other == null) return;
            if (musicBank == null && other.musicBank != null) musicBank = other.musicBank;
            if (menuLoopClip == null && other.menuLoopClip != null) menuLoopClip = other.menuLoopClip;
            if (dayGameplayLoopClip == null && other.dayGameplayLoopClip != null) dayGameplayLoopClip = other.dayGameplayLoopClip;
            if (nightAndChaseLoopClip == null && other.nightAndChaseLoopClip != null) nightAndChaseLoopClip = other.nightAndChaseLoopClip;
            if (finalBossLoopClip == null && other.finalBossLoopClip != null) finalBossLoopClip = other.finalBossLoopClip;
            if (demoVictoryClip == null && other.demoVictoryClip != null) demoVictoryClip = other.demoVictoryClip;
            ResolveMusicClips();
        }

        private void EnsureAudioSources()
        {
            if (sourceA == null)
            {
                GameObject goA = new GameObject("MusicChannel_A");
                goA.transform.SetParent(transform, false);
                sourceA = goA.AddComponent<AudioSource>();
                ConfigureMusicAudioSource(sourceA);
            }

            if (sourceB == null)
            {
                GameObject goB = new GameObject("MusicChannel_B");
                goB.transform.SetParent(transform, false);
                sourceB = goB.AddComponent<AudioSource>();
                ConfigureMusicAudioSource(sourceB);
            }
        }

        private static void ConfigureMusicAudioSource(AudioSource src)
        {
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.priority = 64;
        }

        public void ResolveMusicClips()
        {
            if (musicBank == null)
            {
                musicBank = Resources.Load<Prototipo3MusicBank>("Audio/Prototipo3MusicBank");
            }

            if (musicBank != null)
            {
                if (menuLoopClip == null) menuLoopClip = musicBank.menuLoopClip;
                if (dayGameplayLoopClip == null) dayGameplayLoopClip = musicBank.dayGameplayLoopClip;
                if (nightAndChaseLoopClip == null) nightAndChaseLoopClip = musicBank.nightAndChaseLoopClip;
                if (finalBossLoopClip == null) finalBossLoopClip = musicBank.finalBossLoopClip;
                if (demoVictoryClip == null) demoVictoryClip = musicBank.demoVictoryClip;
            }

#if UNITY_EDITOR
            if (menuLoopClip == null)
                menuLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototipo3/Audio/principal_en_loop.wav");
            if (dayGameplayLoopClip == null)
                dayGameplayLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototipo3/Audio/gameplay_en_loop.wav");
            if (nightAndChaseLoopClip == null)
                nightAndChaseLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototipo3/Audio/noche_en_loop.wav");
            if (finalBossLoopClip == null)
                finalBossLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototipo3/Audio/batalla_final_en_loop.wav");
            if (demoVictoryClip == null)
            {
                demoVictoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototipo3/Audio/Final.mp3");
                if (demoVictoryClip == null)
                    demoVictoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototipo3/Audio/Final.mpeg");
            }
#endif
        }

        private void HandleSceneEntered(string sceneName)
        {
            ResolveMusicClips();
            if (!IsMusicSupportedScene(sceneName) ||
                sceneName.IndexOf("MainMenu_Prototipo3", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                victoryMusicActive = false;
            }

            RefreshMusicImmediate();
        }

        public void ClearVictoryOverride()
        {
            victoryMusicActive = false;
        }

        public void PlayDemoVictoryMusicImmediate()
        {
            victoryMusicActive = true;
            ResolveMusicClips();
            ApplyContext(MusicContext.DemoVictory, immediate: true);
        }

        public void RefreshMusicImmediate()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            MusicContext desired = ResolveDesiredContext(sceneName);
            ApplyContext(desired, immediate: CurrentContext == MusicContext.None || desired == MusicContext.None);
        }

        public MusicContext ResolveDesiredContext(string sceneName)
        {
            if (!IsMusicSupportedScene(sceneName))
            {
                return MusicContext.None;
            }

            if (sceneName.IndexOf("MainMenu_Prototipo3", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MusicContext.MainMenu;
            }

            StoryDirectorPrototipo3 story = StoryDirectorPrototipo3.Instance;

            if (sceneName.IndexOf("Escuela_CanchaFinal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (victoryMusicActive ||
                    (story != null && story.storyModeEnabled && story.CurrentPhase == StoryPhase.DemoCompleted))
                {
                    return MusicContext.DemoVictory;
                }
                return MusicContext.FinalBossArena;
            }

            if (sceneName.IndexOf("Escuela_Infiltracion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bool owlChaseActive =
                    (story != null && story.storyModeEnabled && story.CurrentPhase == StoryPhase.FleeSchoolFirstTime) ||
                    FindAnyObjectByType<OwlRectorBossAI>() != null;

                return owlChaseActive ? MusicContext.NightOrBossChase : MusicContext.DayGameplay;
            }

            // Escena Prototipo3 (ciudad): prioridad a persecución de jefes (Tejón / Perro Guardián) o ciclo nocturno
            bool marcoChase = FindAnyObjectByType<MarcoFeralBossAI>() != null;
            bool dogChase = false;
            GuardDogBossAI[] dogs = FindObjectsByType<GuardDogBossAI>();
            for (int i = 0; i < dogs.Length; i++)
            {
                if (dogs[i] != null && dogs[i].IsActivelyChasingPlayer)
                {
                    dogChase = true;
                    break;
                }
            }

            if (marcoChase || dogChase)
            {
                return MusicContext.NightOrBossChase;
            }

            bool isNight = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            return isNight ? MusicContext.NightOrBossChase : MusicContext.DayGameplay;
        }

        public AudioClip GetClipForContext(MusicContext ctx)
        {
            switch (ctx)
            {
                case MusicContext.MainMenu: return menuLoopClip;
                case MusicContext.DayGameplay: return dayGameplayLoopClip;
                case MusicContext.NightOrBossChase: return nightAndChaseLoopClip;
                case MusicContext.FinalBossArena: return finalBossLoopClip;
                case MusicContext.DemoVictory: return demoVictoryClip;
                default: return null;
            }
        }

        private void ApplyContext(MusicContext desired, bool immediate)
        {
            AudioClip targetClip = GetClipForContext(desired);

            if (desired == MusicContext.DemoVictory && targetClip == null && !loadingFallbackVictoryClip)
            {
                StartCoroutine(LoadFallbackVictoryClipFromDiskRoutine());
            }

            if (desired == MusicContext.None || targetClip == null)
            {
                CurrentContext = desired;
                CurrentClip = null;
                if (immediate)
                {
                    if (sourceA != null) { sourceA.Stop(); sourceA.volume = 0f; }
                    if (sourceB != null) { sourceB.Stop(); sourceB.volume = 0f; }
                    fadeProgress = 1f;
                }
                else
                {
                    // Fade out manteniendo el canal actual
                    AudioSource active = usingSourceA ? sourceA : sourceB;
                    AudioSource inactive = usingSourceA ? sourceB : sourceA;
                    if (inactive != null) { inactive.Stop(); inactive.clip = null; inactive.volume = 0f; }
                    if (active != null && active.isPlaying)
                    {
                        usingSourceA = !usingSourceA;
                        fadeProgress = 0f;
                    }
                }
                return;
            }

            AudioSource currentActive = usingSourceA ? sourceA : sourceB;
            if (CurrentContext == desired && CurrentClip == targetClip && currentActive != null && currentActive.isPlaying)
            {
                return;
            }

            CurrentContext = desired;
            CurrentClip = targetClip;
            EnsureAudioSources();

            AudioSource nextActive = usingSourceA ? sourceB : sourceA;
            AudioSource prevActive = usingSourceA ? sourceA : sourceB;

            nextActive.clip = targetClip;
            nextActive.loop = true;

            if (immediate || crossfadeDuration <= 0.01f)
            {
                if (prevActive != null)
                {
                    prevActive.Stop();
                    prevActive.volume = 0f;
                }
                nextActive.volume = musicVolume;
                nextActive.Play();
                usingSourceA = !usingSourceA;
                fadeProgress = 1f;
            }
            else
            {
                nextActive.volume = 0f;
                nextActive.Play();
                usingSourceA = !usingSourceA;
                fadeProgress = 0f;
            }
        }

        private IEnumerator LoadFallbackVictoryClipFromDiskRoutine()
        {
            loadingFallbackVictoryClip = true;
            string[] candidates =
            {
                Path.Combine(Application.dataPath, "Prototipo3", "Audio", "Final.mp3"),
                Path.Combine(Application.dataPath, "Prototipo3", "Audio", "Final.mpeg")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (!File.Exists(path)) continue;

                using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.MPEG))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip loaded = DownloadHandlerAudioClip.GetContent(req);
                        if (loaded != null && loaded.length > 0.1f)
                        {
                            loaded.name = Path.GetFileNameWithoutExtension(path);
                            demoVictoryClip = loaded;
                            loadingFallbackVictoryClip = false;
                            if (victoryMusicActive || CurrentContext == MusicContext.DemoVictory)
                            {
                                ApplyContext(MusicContext.DemoVictory, immediate: true);
                            }
                            yield break;
                        }
                    }
                }
            }

            loadingFallbackVictoryClip = false;
        }

        private void Update()
        {
            if (Instance != this) return;

            if (Time.unscaledTime >= nextContextPollTime)
            {
                nextContextPollTime = Time.unscaledTime + 0.15f;
                string sceneName = SceneManager.GetActiveScene().name;
                MusicContext desired = ResolveDesiredContext(sceneName);
                if (desired != CurrentContext || (desired != MusicContext.None && CurrentClip == null))
                {
                    if (desired != MusicContext.None && CurrentClip == null) ResolveMusicClips();
                    ApplyContext(desired, immediate: false);
                }
            }

            AudioSource active = usingSourceA ? sourceA : sourceB;
            AudioSource prev = usingSourceA ? sourceB : sourceA;

            if (fadeProgress < 1f)
            {
                float step = crossfadeDuration > 0.01f ? Time.unscaledDeltaTime / crossfadeDuration : 1f;
                fadeProgress = Mathf.Clamp01(fadeProgress + step);

                if (active != null && active.clip != null && CurrentContext != MusicContext.None)
                {
                    active.volume = Mathf.Lerp(0f, musicVolume, fadeProgress);
                }
                if (prev != null && prev.isPlaying)
                {
                    prev.volume = Mathf.Lerp(musicVolume, 0f, fadeProgress);
                    if (fadeProgress >= 1f)
                    {
                        prev.Stop();
                    }
                }
            }
            else
            {
                if (active != null && active.isPlaying && CurrentContext != MusicContext.None)
                {
                    active.volume = musicVolume;
                }
                if (prev != null && prev.isPlaying)
                {
                    prev.Stop();
                    prev.volume = 0f;
                }
            }
        }
    }
}
