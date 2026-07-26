using System.Collections;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Sonido de ambiente en loop: grillos, ambiente de Presencia, latidos/patas de
    /// cabra (miedo nivel 3 de 4), y el búho ocasional. Es un script NUEVO, aparte del
    /// Core/AudioManager que ya existe — no lo reemplaza ni lo modifica, solo se
    /// suscribe a FearManager.OnMiedoCambiado (evento público que ya existe) para
    /// cubrir los sonidos continuos que ese AudioManager no maneja (el suyo son solo
    /// sfx puntuales de una sola vez).
    /// </summary>
    public class AmbienteManager : MonoBehaviour
    {
        [Header("Fuentes (Add Component > Audio Source x2)")]
        [SerializeField] private AudioSource fuenteAmbiente;
        [SerializeField] private AudioSource fuenteLatidos;

        [Header("Clips")]
        [SerializeField] private AudioClip sonidoGrillos;
        [SerializeField] private AudioClip sonidoAmbientePresencia;
        [SerializeField] private AudioClip sonidoLatidos;
        [SerializeField] private AudioClip sonidoBuho;

        [Header("Umbrales sobre FearManager.miedoActual (rango 0 a 100)")]
        [SerializeField] private float umbralAmbientePresencia = 50f;
        [Tooltip("Nivel 3 de 4 del miedo (75 de 100).")]
        [SerializeField] private float umbralLatidos = 75f;

        [Header("Búho (solo suena con el ambiente tranquilo)")]
        [SerializeField] private float buhoIntervaloMinimo = 8f;
        [SerializeField] private float buhoIntervaloMaximo = 20f;

        private Coroutine coroutineBuho;

        private void Start()
        {
            // Nos suscribimos en Start() (no OnEnable), igual que Core/AudioManager,
            // para dar tiempo a que FearManager ya haya corrido su Awake().
            if (FearManager.Instance != null)
                FearManager.Instance.OnMiedoCambiado += ActualizarAmbiente;

            CambiarAmbiente(sonidoGrillos);
            IniciarBuhoSiCorresponde();
        }

        private void OnDestroy()
        {
            if (FearManager.Instance != null)
                FearManager.Instance.OnMiedoCambiado -= ActualizarAmbiente;
        }

        private void ActualizarAmbiente(float miedo)
        {
            AudioClip clipDeseado = miedo >= umbralAmbientePresencia ? sonidoAmbientePresencia : sonidoGrillos;
            CambiarAmbiente(clipDeseado);
            IniciarBuhoSiCorresponde();

            bool debeSonarLatidos = miedo >= umbralLatidos;
            if (fuenteLatidos == null || sonidoLatidos == null)
                return;

            if (debeSonarLatidos && !fuenteLatidos.isPlaying)
            {
                fuenteLatidos.clip = sonidoLatidos;
                fuenteLatidos.loop = true;
                fuenteLatidos.Play();
            }
            else if (!debeSonarLatidos && fuenteLatidos.isPlaying)
            {
                fuenteLatidos.Stop();
            }
        }

        private void CambiarAmbiente(AudioClip clip)
        {
            if (fuenteAmbiente == null || clip == null || fuenteAmbiente.clip == clip)
                return;

            fuenteAmbiente.clip = clip;
            fuenteAmbiente.loop = true;
            fuenteAmbiente.Play();
        }

        private void IniciarBuhoSiCorresponde()
        {
            bool ambienteTranquilo = fuenteAmbiente != null && fuenteAmbiente.clip == sonidoGrillos;

            if (ambienteTranquilo && coroutineBuho == null)
                coroutineBuho = StartCoroutine(ReproducirBuhoPeriodicamente());
            else if (!ambienteTranquilo && coroutineBuho != null)
            {
                StopCoroutine(coroutineBuho);
                coroutineBuho = null;
            }
        }

        private IEnumerator ReproducirBuhoPeriodicamente()
        {
            while (true)
            {
                float espera = Random.Range(buhoIntervaloMinimo, buhoIntervaloMaximo);
                yield return new WaitForSeconds(espera);

                if (sonidoBuho != null && fuenteAmbiente != null)
                {
                    fuenteAmbiente.PlayOneShot(sonidoBuho);
                }
            }
        }
    }
}