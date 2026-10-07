using System.Collections;
using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Transición de entrada/salida para las vistas que abre PanelInspeccion:
    /// fade del CanvasGroup y un pequeño "pop" de escala del contenido. Usa
    /// tiempo sin escala para no depender de Time.timeScale. PanelInspeccion la
    /// usa sola si la vista tiene este componente; si no, hace SetActive directo.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TransicionPanel : MonoBehaviour
    {
        [Tooltip("Lo que hace el 'pop' de escala (ej. la imagen del armario). Si es null, solo hay fade.")]
        [SerializeField] private RectTransform contenido;

        [SerializeField] private float duracionEntrada = 0.3f;
        [SerializeField] private float duracionSalida = 0.15f;
        [SerializeField, Range(0.5f, 1f)] private float escalaInicial = 0.85f;

        private CanvasGroup grupo;
        private Coroutine actual;

        private CanvasGroup Grupo => grupo != null ? grupo : (grupo = GetComponent<CanvasGroup>());

        public void Mostrar()
        {
            bool estabaActivo = gameObject.activeSelf;
            gameObject.SetActive(true);

            if (!estabaActivo)
            {
                Grupo.alpha = 0f;
                AplicarEscala(escalaInicial);
            }

            Grupo.blocksRaycasts = true;
            Reproducir(1f, duracionEntrada, false);
        }

        public void Ocultar()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            // Mientras se desvanece no debe atrapar clicks del juego.
            Grupo.blocksRaycasts = false;
            Reproducir(0f, duracionSalida, true);
        }

        private void OnDisable()
        {
            // Si algo apagó la vista a mitad de la animación, la dejamos en un
            // estado sano para la próxima vez (Mostrar() la reinicia igual).
            actual = null;
            Grupo.alpha = 1f;
            Grupo.blocksRaycasts = true;
            AplicarEscala(1f);
        }

        private void Reproducir(float alphaDestino, float duracion, bool desactivarAlFinal)
        {
            if (actual != null) StopCoroutine(actual);
            actual = StartCoroutine(Animar(alphaDestino, duracion, desactivarAlFinal));
        }

        private IEnumerator Animar(float alphaDestino, float duracion, bool desactivarAlFinal)
        {
            float alphaInicial = Grupo.alpha;
            float escalaDesde = contenido != null ? contenido.localScale.x : 1f;
            float escalaHasta = alphaDestino > 0f ? 1f : escalaInicial;

            float t = 0f;
            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duracion), 3f); // ease-out

                Grupo.alpha = Mathf.Lerp(alphaInicial, alphaDestino, k);
                AplicarEscala(Mathf.Lerp(escalaDesde, escalaHasta, k));
                yield return null;
            }

            Grupo.alpha = alphaDestino;
            AplicarEscala(escalaHasta);
            actual = null;

            if (desactivarAlFinal)
                gameObject.SetActive(false);
        }

        private void AplicarEscala(float s)
        {
            if (contenido != null)
                contenido.localScale = new Vector3(s, s, 1f);
        }
    }
}
