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

    private string BuscarRutaVideo(string nombreArchivo)
    {
        if (string.IsNullOrEmpty(nombreArchivo)) return null;

        string[] carpetasBusqueda = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "UI", "Videos"),
            System.IO.Path.Combine(Application.dataPath, "Prototipo2", "UI", "Videos"),
            System.IO.Path.Combine(Application.dataPath, "Prototipo3", "UI", "Videos"),
            System.IO.Path.Combine(Application.dataPath, "StreamingAssets", "Cutscenes"),
            System.IO.Path.Combine(Application.dataPath, "Resources", "Videos"),
            System.IO.Path.Combine(Application.dataPath, "Prototipo3", "Scripts", "Cinematics")
        };

        foreach (string carpeta in carpetasBusqueda)
        {
            string ruta = System.IO.Path.Combine(carpeta, nombreArchivo);
            if (System.IO.File.Exists(ruta))
                return ruta;
        }

        return null;
    }

    void Awake()
    {
        if (reproductorVideo == null) reproductorVideo = GetComponent<VideoPlayer>();
        if (reproductorVideo != null)
        {
            reproductorVideo.playOnAwake = false;

            if (reproductorVideo.clip != null)
            {
                // Si hay un clip asignado (ej. TestInitialVideo), buscar si existe su archivo MP4 directo en disco para streaming óptimo
                string clipName = reproductorVideo.clip.name;
                string preferredFile = clipName.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase) ? clipName : (clipName + ".mp4");
                string ruta = BuscarRutaVideo(preferredFile);

                if (!string.IsNullOrEmpty(ruta))
                {
                    reproductorVideo.source = VideoSource.Url;
                    reproductorVideo.url = ruta;
                    Debug.Log($"[CinematicaManager] Reproduciendo video desde archivo MP4: {ruta}");
                }
                else
                {
                    reproductorVideo.source = VideoSource.VideoClip;
                    Debug.Log($"[CinematicaManager] Reproduciendo video desde VideoClip asignado: {reproductorVideo.clip.name}");
                }
            }
            else
            {
                // Si no hay clip asignado en el inspector, buscar por defecto TestInitialVideo.mp4 en las carpetas del proyecto
                string rutaDefault = BuscarRutaVideo("TestInitialVideo.mp4");
                if (!string.IsNullOrEmpty(rutaDefault))
                {
                    reproductorVideo.source = VideoSource.Url;
                    reproductorVideo.url = rutaDefault;
                    Debug.Log($"[CinematicaManager] Sin clip asignado: usando TestInitialVideo por defecto: {rutaDefault}");
                }
            }
        }
    }

    IEnumerator Start()
    {
        if (hamster == null) hamster = FindAnyObjectByType<HamsterController>();
        bool hasSource = reproductorVideo != null && (
            (reproductorVideo.source == VideoSource.Url && !string.IsNullOrEmpty(reproductorVideo.url)) ||
            (reproductorVideo.source == VideoSource.VideoClip && reproductorVideo.clip != null)
        );

        if (!hasSource)
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
        canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
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
        float videoDuration = reproductorVideo.length > 0 ? (float)reproductorVideo.length : 30f;
        deadline = Time.realtimeSinceStartup + videoDuration + 15f;
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
                if (canvases[i] != null) canvases[i].enabled = true;
        if (canvasUIRoot != null)
            canvasUIRoot.SetActive(true);
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(true);
        if (TutorialPipelineManager.Instance != null)
        {
            TutorialPipelineManager.Instance.NotificarFinCinematicaInicial();
        }
        if (hamster == null) hamster = FindAnyObjectByType<HamsterController>();
        if (hamster != null) hamster.enabled = true;
        gameObject.SetActive(false);
    }
}
