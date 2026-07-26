using System.Collections;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Sonidos extra alrededor del final de la partida, SIN tocar Core/AudioManager
    /// (que ya reproduce sfxVictoria/sfxDerrota al cambiar de estado). Este script
    /// solo agrega lo que falta: la puerta de la casa antes de ganar, el good ending
    /// después, y el jingle de Game Over después del jumpscare al perder.
    /// </summary>
    public class SecuenciaFinalAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource fuenteEfectos;

        [Header("Victoria (puerta -> [aquí suena sfxVictoria de Core/AudioManager] -> good ending)")]
        [Tooltip("Puerta de la casa abriéndose porque la abuela llegó — suena ANTES del sfxVictoria.")]
        [SerializeField] private AudioClip sonidoPuertaVictoria;
        [SerializeField] private AudioClip sonidoGoodEnding;
        [SerializeField] private float esperaEntreSonidosVictoria = 1.2f;

        [Header("Derrota (jumpscare -> [aquí suena sfxDerrota de Core/AudioManager] -> game over)")]
        [Tooltip("Jumpscare + risa del demonio + grito del niño, todo en un solo clip.")]
        [SerializeField] private AudioClip sonidoJumpscare;
        [SerializeField] private AudioClip sonidoGameOver;
        [SerializeField] private float esperaAntesDeGameOver = 1.5f;

        private void Start()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += ManejarCambioDeEstado;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= ManejarCambioDeEstado;
        }

        private void ManejarCambioDeEstado(GameState estado)
        {
            if (estado == GameState.Victoria)
                StartCoroutine(SecuenciaVictoria());
            else if (estado == GameState.Derrota)
                StartCoroutine(SecuenciaDerrota());
        }

        private IEnumerator SecuenciaVictoria()
        {
            Reproducir(sonidoPuertaVictoria);
            yield return new WaitForSeconds(esperaEntreSonidosVictoria);
            // Core/AudioManager ya reproduce su sfxVictoria en este mismo cambio de estado.
            yield return new WaitForSeconds(esperaEntreSonidosVictoria);
            Reproducir(sonidoGoodEnding);
        }

        private IEnumerator SecuenciaDerrota()
        {
            Reproducir(sonidoJumpscare);
            yield return new WaitForSeconds(esperaAntesDeGameOver);
            Reproducir(sonidoGameOver);
        }

        private void Reproducir(AudioClip clip)
        {
            if (fuenteEfectos != null && clip != null)
                fuenteEfectos.PlayOneShot(clip);
        }
    }
}