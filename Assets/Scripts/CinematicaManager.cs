using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-200)]
public class CinematicaManager : MonoBehaviour
{
    public VideoPlayer reproductorVideo;
    public GameObject objetoTutorialManager;
    public HamsterController hamster;
    public GameObject canvasUIRoot;

    private bool cinematicaFinalizada = false;
    public bool CinematicaTerminada => cinematicaFinalizada;
    private Canvas[] canvases;
    private bool[] canvasStates;
    private bool videoIniciado = false;
    private float tiempoEnCinematica = 0f;
    private bool canvasUIRootWasActive;
    private bool canvasUIRootStateCaptured;

    void Awake()
    {
        if (reproductorVideo == null) reproductorVideo = GetComponent<VideoPlayer>();
        if (reproductorVideo != null) reproductorVideo.playOnAwake = false;
    }

    IEnumerator Start()
    {
        if (reproductorVideo == null || reproductorVideo.clip == null)
        {
            IniciarGameplay(null);
            yield break;
        }
        if (hamster != null) hamster.enabled = false;
        if (canvasUIRoot == null)
        {
            Canvas mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas != null) canvasUIRoot = mainCanvas.gameObject;
        }
        canvases = FindObjectsByType<Canvas>();
        canvasStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            canvasStates[i] = canvases[i].enabled;
            canvases[i].enabled = false;
        }
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(false);
        if (canvasUIRoot != null)
        {
            canvasUIRootWasActive = canvasUIRoot.activeSelf;
            canvasUIRootStateCaptured = true;
            canvasUIRoot.SetActive(false);
        }
        reproductorVideo.isLooping = false;
        reproductorVideo.audioOutputMode = VideoAudioOutputMode.Direct;
        reproductorVideo.loopPointReached += IniciarGameplay;
        reproductorVideo.errorReceived += OnVideoError;
        reproductorVideo.Prepare();
        // Use a realtime deadline: the first editor frame can have a very large delta.
        float deadline = Time.realtimeSinceStartup + 15f;
        while (!reproductorVideo.isPrepared && !cinematicaFinalizada && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (cinematicaFinalizada) yield break;
        if (!reproductorVideo.isPrepared)
        {
            Debug.LogWarning("No se pudo preparar la cinematica inicial en 15 segundos.");
            IniciarGameplay(reproductorVideo);
            yield break;
        }
        reproductorVideo.Play();
        deadline = Time.realtimeSinceStartup + (float)reproductorVideo.length + 15f;
        while (!cinematicaFinalizada && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (!cinematicaFinalizada) IniciarGameplay(reproductorVideo);
    }

    void Update()
    {
        if (!cinematicaFinalizada && (FeralInput.Pressed(KeyCode.Space) ||
            FeralInput.Pressed(KeyCode.Escape) || FeralInput.Pressed(KeyCode.Return)))
        {
            IniciarGameplay(reproductorVideo);
            return;
        }

        if (cinematicaFinalizada || reproductorVideo == null) return;

        tiempoEnCinematica += Time.unscaledDeltaTime;
        if (reproductorVideo.isPlaying || reproductorVideo.frame > 0)
        {
            videoIniciado = true;
        }

        if (videoIniciado &&
            ((reproductorVideo.frameCount > 10 &&
              reproductorVideo.frame >= (long)reproductorVideo.frameCount - 1) ||
             (!reproductorVideo.isPlaying && tiempoEnCinematica > 2f &&
              reproductorVideo.time > 1.0)))
        {
            IniciarGameplay(reproductorVideo);
        }
    }

    void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning("Cinematica inicial: " + message);
        IniciarGameplay(player);
    }

    void OnDisable()
    {
        if (reproductorVideo == null) return;
        reproductorVideo.loopPointReached -= IniciarGameplay;
        reproductorVideo.errorReceived -= OnVideoError;
    }

    public void IniciarGameplay(VideoPlayer player)
    {
        if (cinematicaFinalizada) return;
        cinematicaFinalizada = true;
        if (reproductorVideo != null)
        {
            reproductorVideo.Stop();
            reproductorVideo.targetCameraAlpha = 0f;
            reproductorVideo.enabled = false;
        }
        if (canvases != null)
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i] != null) canvases[i].enabled = canvasStates[i];
        if (canvasUIRoot != null && canvasUIRootStateCaptured)
            canvasUIRoot.SetActive(canvasUIRootWasActive);
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(true);
        if (TutorialPipelineManager.Instance != null)
        {
            TutorialPipelineManager.Instance.NotificarFinCinematicaInicial();
        }
        if (hamster != null) hamster.enabled = true;
        gameObject.SetActive(false);
    }
}
