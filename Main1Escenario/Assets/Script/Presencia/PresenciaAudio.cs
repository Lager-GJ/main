using UnityEngine;

namespace Terror
{
    // Gancho de audio para el avance de la Presencia. El clip queda vacio
    // hasta que Diseñador C entregue el sonido final: sin clip asignado,
    // simplemente no suena nada (no da error).
    [RequireComponent(typeof(AudioSource))]
    public class PresenciaAudio : MonoBehaviour
    {
        [Tooltip("Sonido que se reproduce cada vez que la Presencia sube de nivel")]
        public AudioClip sonidoAcercamiento;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            GameEvents.OnCercaniaPresenciaCambiada += ReproducirSonido;
        }

        private void OnDisable()
        {
            GameEvents.OnCercaniaPresenciaCambiada -= ReproducirSonido;
        }

        private void ReproducirSonido(int nivel, float multiplicador)
        {
            if (sonidoAcercamiento != null)
            {
                audioSource.PlayOneShot(sonidoAcercamiento);
            }
        }
    }
}
