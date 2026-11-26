using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Banco de pistas musicales de Prototipo 3 almacenado en Resources/Audio/Prototipo3MusicBank.
    /// </summary>
    [CreateAssetMenu(fileName = "Prototipo3MusicBank", menuName = "Feral/Prototipo 3/Music Bank")]
    public class Prototipo3MusicBank : ScriptableObject
    {
        [Header("Pistas Musicales de Prototipo 3")]
        [Tooltip("Música de MainMenu_Prototipo3 (principal_en_loop.wav)")]
        public AudioClip menuLoopClip;

        [Tooltip("Música diurna e infiltración inicial en la escuela (gameplay_en_loop.wav)")]
        public AudioClip dayGameplayLoopClip;

        [Tooltip("Música nocturna, persecuciones de Tejón/Perro Guardián y huida del Búho (noche_en_loop.wav)")]
        public AudioClip nightAndChaseLoopClip;

        [Tooltip("Música de batalla final en la cancha (batalla_final_en_loop.wav)")]
        public AudioClip finalBossLoopClip;

        [Tooltip("Música de victoria al completar la demo (Final.mp3 / Final.mpeg)")]
        public AudioClip demoVictoryClip;
    }
}
