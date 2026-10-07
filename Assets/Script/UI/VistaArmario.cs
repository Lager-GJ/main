using System.Collections;
using TMPro;
using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Servicios compartidos de la vista de primer plano del armario: reproducir
    /// sonidos, mostrar una línea de texto ("pista") y sacudir el armario. Los
    /// HotspotArmario (ropa, puertas, cajón...) le piden todo esto a él, así cada
    /// zona clicable no necesita su propio AudioSource ni su propio texto.
    /// Va en el mismo objeto que la imagen del armario (es el que se sacude).
    /// Usa tiempo sin escala, igual que TransicionPanel.
    /// </summary>
    public class VistaArmario : MonoBehaviour
    {
        [SerializeField] private AudioSource fuente;

        [Header("Texto de pista (arriba de la pantalla, empieza apagado)")]
        [SerializeField] private TextMeshProUGUI textoPista;
        [SerializeField] private float duracionTexto = 2.5f;
        [SerializeField] private float duracionDesvanecer = 0.4f;

        [Header("Sacudida")]
        [SerializeField] private float duracionSacudida = 0.25f;
        [SerializeField] private float sacudidasPorSegundo = 30f;

        private RectTransform rect;
        private Vector2 posicionBase;
        private Coroutine rutinaTexto;
        private Coroutine rutinaSacudida;

        private void Awake() => rect = (RectTransform)transform;

        private void OnEnable() => posicionBase = rect.anchoredPosition;

        private void OnDisable()
        {
            // La vista se apaga al cerrar el armario: dejamos todo como estaba.
            rutinaTexto = null;
            rutinaSacudida = null;
            rect.anchoredPosition = posicionBase;
            if (textoPista != null) textoPista.gameObject.SetActive(false);
        }

        public void Sonar(AudioClip clip)
        {
            if (fuente != null && clip != null)
                fuente.PlayOneShot(clip);
        }

        public void Decir(string texto)
        {
            if (textoPista == null || string.IsNullOrEmpty(texto)) return;

            if (rutinaTexto != null) StopCoroutine(rutinaTexto);
            rutinaTexto = StartCoroutine(MostrarTexto(texto));
        }

        public void Sacudir(float amplitud)
        {
            if (amplitud <= 0f) return;

            if (rutinaSacudida != null)
            {
                StopCoroutine(rutinaSacudida);
                rect.anchoredPosition = posicionBase;
            }
            rutinaSacudida = StartCoroutine(HacerSacudida(amplitud));
        }

        private IEnumerator MostrarTexto(string texto)
        {
            textoPista.text = texto;
            textoPista.alpha = 1f;
            textoPista.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(duracionTexto);

            for (float t = 0f; t < duracionDesvanecer; t += Time.unscaledDeltaTime)
            {
                textoPista.alpha = 1f - t / duracionDesvanecer;
                yield return null;
            }

            textoPista.gameObject.SetActive(false);
            rutinaTexto = null;
        }

        private IEnumerator HacerSacudida(float amplitud)
        {
            for (float t = 0f; t < duracionSacudida; t += Time.unscaledDeltaTime)
            {
                float atenuacion = 1f - t / duracionSacudida;
                float x = Mathf.Sin(t * sacudidasPorSegundo * Mathf.PI * 2f) * amplitud * atenuacion;
                float y = Mathf.Cos(t * sacudidasPorSegundo * 1.3f * Mathf.PI * 2f) * amplitud * 0.5f * atenuacion;
                rect.anchoredPosition = posicionBase + new Vector2(x, y);
                yield return null;
            }

            rect.anchoredPosition = posicionBase;
            rutinaSacudida = null;
        }
    }
}
