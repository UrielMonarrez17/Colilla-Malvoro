using UnityEngine;
using UnityEngine.Video; // Necesario para los videos

public class CinematicaManager : MonoBehaviour
{
    [Header("Referencias")]
    public VideoPlayer reproductorVideo;
    public GameObject objetoTutorialManager; // Arrastra tu TutorialManager aquí
    public HamsterController hamster; // Arrastra a tu jugador aquí

    void Start()
    {
        // 1. Congelamos al hámster y ocultamos el tutorial
        if (hamster != null) hamster.enabled = false;
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(false);

        // 2. Le decimos al reproductor qué hacer cuando llegue al final del video
        reproductorVideo.loopPointReached += IniciarGameplay;
    }

    private void IniciarGameplay(VideoPlayer vp)
    {
        // 3. El video terminó: Apagamos el reproductor de video
        reproductorVideo.gameObject.SetActive(false);

        // 4. Encendemos el tutorial y los controles del hámster
        if (objetoTutorialManager != null) objetoTutorialManager.SetActive(true);
        if (hamster != null) hamster.enabled = true;

        Debug.Log("🎬 Cinemática terminada. ¡A jugar!");
    }
}