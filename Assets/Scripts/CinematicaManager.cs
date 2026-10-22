using UnityEngine;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CinematicaManager : MonoBehaviour
{
    [Header("Referencias")]
    public VideoPlayer reproductorVideo;
    public GameObject objetoTutorialManager; // Arrastra tu TutorialManager aquí
    public HamsterController hamster; // Arrastra a tu jugador aquí
    public GameObject canvasUIRoot;

    private bool cinematicaFinalizada = false;
    public bool CinematicaTerminada => cinematicaFinalizada;
    private bool videoIniciado = false;
    private float tiempoEnCinematica = 0f;
    private double ultimoTiempoVideo = -1.0;
    private float tiempoSinAvanzar = 0f;

    void Awake()
    {
        if (reproductorVideo == null)
        {
            reproductorVideo = GetComponent<VideoPlayer>();
        }

        if (reproductorVideo != null)
        {
            reproductorVideo.isLooping = false;

            // Evitar el bug "AudioSampleProvider buffer overflow" cuando AudioOutputMode está en AudioSource (2) sin AudioSource asignado,
            // lo cual bloquea el evento loopPointReached al final de videos H.264.
            if (reproductorVideo.audioOutputMode == VideoAudioOutputMode.AudioSource &&
                reproductorVideo.GetTargetAudioSource(0) == null)
            {
                reproductorVideo.audioOutputMode = VideoAudioOutputMode.Direct;
            }
        }
    }

    void Start()
    {
        if (reproductorVideo == null || reproductorVideo.clip == null)
        {
            IniciarGameplay(null);
            return;
        }

        if (canvasUIRoot == null)
        {
            Canvas mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas != null) canvasUIRoot = mainCanvas.gameObject;
        }

        // 1. Congelamos al hámster y ocultamos el tutorial y toda la UI mientras corre el video inicial
        if (hamster != null) hamster.enabled = false;
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(false);
        if (canvasUIRoot != null) canvasUIRoot.SetActive(false);

        // 2. Suscribir eventos de finalización, inicio y error del reproductor
        reproductorVideo.started += OnVideoStarted;
        reproductorVideo.loopPointReached += IniciarGameplay;
        reproductorVideo.errorReceived += OnVideoError;

        if (!reproductorVideo.isPlaying)
        {
            reproductorVideo.Play();
        }
    }

    private void OnVideoStarted(VideoPlayer vp)
    {
        videoIniciado = true;
    }

    void Update()
    {
        if (cinematicaFinalizada) return;

        tiempoEnCinematica += Time.unscaledDeltaTime;

        // Permitir saltar la cinemática inicial únicamente con Escape o Enter si el usuario lo decide conscientemente
        bool skipPressed = Input.GetKeyDown(KeyCode.Escape) ||
                           Input.GetKeyDown(KeyCode.Return);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            skipPressed |= Keyboard.current.escapeKey.wasPressedThisFrame ||
                           Keyboard.current.enterKey.wasPressedThisFrame;
        }
#endif
        if (skipPressed)
        {
            IniciarGameplay(reproductorVideo);
            return;
        }

        if (reproductorVideo == null)
        {
            IniciarGameplay(null);
            return;
        }

        if (reproductorVideo.isPlaying || reproductorVideo.frame > 0)
        {
            videoIniciado = true;
        }

        // Si el reproductor terminó naturalmente de reproducir el video
        if (videoIniciado)
        {
            // Si el video llegó al final por conteo de frames o dejó de reproducir habiendo avanzado
            if (reproductorVideo.frameCount > 10 && reproductorVideo.frame >= (long)reproductorVideo.frameCount - 1)
            {
                IniciarGameplay(reproductorVideo);
                return;
            }

            if (!reproductorVideo.isPlaying && tiempoEnCinematica > 2.0f && reproductorVideo.time > 1.0)
            {
                IniciarGameplay(reproductorVideo);
                return;
            }
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning($"[CinematicaManager] Aviso en VideoPlayer: {message}. Iniciando gameplay.");
        IniciarGameplay(vp);
    }

    public void IniciarGameplay(VideoPlayer vp)
    {
        if (cinematicaFinalizada) return;
        cinematicaFinalizada = true;

        // 3. El video terminó: Limpiamos por completo el plano de cámara (CameraNearPlane) y apagamos el reproductor
        if (reproductorVideo != null)
        {
            reproductorVideo.started -= OnVideoStarted;
            reproductorVideo.loopPointReached -= IniciarGameplay;
            reproductorVideo.errorReceived -= OnVideoError;

            reproductorVideo.Stop();
            reproductorVideo.targetCameraAlpha = 0f;
            reproductorVideo.targetCamera = null;

            if (reproductorVideo.targetTexture != null)
            {
                reproductorVideo.targetTexture.Release();
                reproductorVideo.targetTexture = null;
            }

            reproductorVideo.clip = null;
            reproductorVideo.enabled = false;
            reproductorVideo.gameObject.SetActive(false);
        }

        // 4. Encendemos la UI, el tutorial y los controles del hámster
        if (canvasUIRoot != null) canvasUIRoot.SetActive(true);
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(true);
        if (TutorialPipelineManager.Instance != null)
        {
            TutorialPipelineManager.Instance.NotificarFinCinematicaInicial();
        }
        if (hamster != null) hamster.enabled = true;

        Debug.Log("🎬 Cinemática terminada y ocultada correctamente. ¡A jugar!");
    }
}
