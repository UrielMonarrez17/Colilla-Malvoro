using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-200)]
public class CinematicaManager : MonoBehaviour
{
    public VideoPlayer reproductorVideo;
    public GameObject objetoTutorialManager;
    public HamsterController hamster;
    private bool cinematicaFinalizada;
    private Canvas[] canvases;
    private bool[] canvasStates;

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
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(false);
        canvases = FindObjectsByType<Canvas>();
        canvasStates = new bool[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            canvasStates[i] = canvases[i].enabled;
            canvases[i].enabled = false;
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
            IniciarGameplay(reproductorVideo);
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
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(true);
        if (hamster != null) hamster.enabled = true;
        gameObject.SetActive(false);
    }
}
