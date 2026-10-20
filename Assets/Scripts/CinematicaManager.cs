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

    private bool cinematicaFinalizada = false;
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

        // 1. Congelamos al hámster y ocultamos el tutorial mientras corre el video inicial
        if (hamster != null) hamster.enabled = false;
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(false);

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

        // Permitir saltar la cinemática inicial con Espacio, Escape o Enter
        bool skipPressed = FeralInput.Pressed(KeyCode.Space) ||
                           Input.GetKeyDown(KeyCode.Escape) ||
                           Input.GetKeyDown(KeyCode.Return);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            skipPressed |= Keyboard.current.spaceKey.wasPressedThisFrame ||
                           Keyboard.current.escapeKey.wasPressedThisFrame ||
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

        if (videoIniciado)
        {
            // 1. Detección por duración del clip (corrige videos H.264 con timestamps sesgados donde loopPointReached no se dispara)
            if (reproductorVideo.length > 0.5 && reproductorVideo.time >= reproductorVideo.length - 0.12)
            {
                IniciarGameplay(reproductorVideo);
                return;
            }

            // 2. Detección por conteo de frames
            if (reproductorVideo.frameCount > 5 && reproductorVideo.frame >= (long)reproductorVideo.frameCount - 2)
            {
                IniciarGameplay(reproductorVideo);
                return;
            }

            // 3. Detección si el reproductor dejó de reproducir o se congeló en el último frame
            if (!reproductorVideo.isPlaying && tiempoEnCinematica > 1.0f)
            {
                IniciarGameplay(reproductorVideo);
                return;
            }

            double tActual = reproductorVideo.time;
            if (tiempoEnCinematica > 1.2f && Mathf.Abs((float)(tActual - ultimoTiempoVideo)) < 0.0005f)
            {
                tiempoSinAvanzar += Time.unscaledDeltaTime;
                if (tiempoSinAvanzar >= 0.55f)
                {
                    IniciarGameplay(reproductorVideo);
                    return;
                }
            }
            else
            {
                tiempoSinAvanzar = 0f;
            }
            ultimoTiempoVideo = tActual;
        }
        else if (tiempoEnCinematica > 4.0f)
        {
            // Si el video no pudo iniciar tras 4 segundos, iniciar gameplay para no bloquear al jugador
            IniciarGameplay(reproductorVideo);
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

        // 4. Encendemos el tutorial y los controles del hámster
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(true);
        if (hamster != null) hamster.enabled = true;

        Debug.Log("🎬 Cinemática terminada y ocultada correctamente. ¡A jugar!");
    }
}
