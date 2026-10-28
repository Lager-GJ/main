using System.Collections;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Sonido de ambiente en loop: grillos, ambiente de Presencia, latidos/patas de
    /// cabra, y el búho ocasional. Avanza con el nivel de la Presencia (uno por
    /// fósforo consumido, GameEvents.OnNivelPresenciaCambiado): desde 2026-10-22 no
    /// hay barra de miedo y lo único que progresa son los fósforos.
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

        [Header("Nivel de Presencia (1 por fósforo consumido) en que entra cada sonido")]
        [SerializeField] private int nivelAmbientePresencia = 2;
        [SerializeField] private int nivelLatidos = 1;

        [Header("Latidos: se aceleran y suben con cada nivel")]
        [SerializeField] private float velocidadLatidosNivelInicial = 0.8f;
        [Tooltip("Cuánto se acelera el latido (pitch) por cada nivel extra de la Presencia.")]
        [SerializeField] private float aceleracionLatidosPorNivel = 0.25f;
        [SerializeField, Range(0f, 1f)] private float volumenLatidosInicial = 0.3f;
        [SerializeField, Range(0f, 1f)] private float volumenLatidosPorNivel = 0.2f;

        [Header("Búho (solo suena con el ambiente tranquilo)")]
        [SerializeField] private float buhoIntervaloMinimo = 8f;
        [SerializeField] private float buhoIntervaloMaximo = 20f;

        private Coroutine coroutineBuho;

        private void Start()
        {
            GameEvents.OnNivelPresenciaCambiado += ActualizarAmbiente;

            CambiarAmbiente(sonidoGrillos);
            IniciarBuhoSiCorresponde();
        }

        private void OnDestroy()
        {
            GameEvents.OnNivelPresenciaCambiado -= ActualizarAmbiente;
        }

        private void ActualizarAmbiente(int nivel)
        {
            AudioClip clipDeseado = nivel >= nivelAmbientePresencia ? sonidoAmbientePresencia : sonidoGrillos;
            CambiarAmbiente(clipDeseado);
            IniciarBuhoSiCorresponde();

            bool debeSonarLatidos = nivel >= nivelLatidos;
            if (fuenteLatidos == null || sonidoLatidos == null)
                return;

            if (debeSonarLatidos)
            {
                int pasos = Mathf.Max(0, nivel - nivelLatidos);
                fuenteLatidos.pitch = velocidadLatidosNivelInicial + aceleracionLatidosPorNivel * pasos;
                fuenteLatidos.volume = Mathf.Clamp01(volumenLatidosInicial + volumenLatidosPorNivel * pasos);
            }

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