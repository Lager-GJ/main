using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Zona clicable dentro del armario en primer plano (una prenda, una puerta, el
    /// cajón...). Es un Image casi transparente sobre el dibujo: se ilumina al pasar
    /// el mouse y, al hacer click, dispara sonido, una línea de texto, una sacudida,
    /// opcionalmente un susto (o alivio) de miedo y opcionalmente revela un objeto
    /// escondido (ej. la lata dentro del cajón). Todo se configura en el Inspector.
    ///
    /// No pausa el fósforo: rebuscar en el armario cuesta luz, igual que el resto de
    /// la tensión (decisión de diseño 2026-09-15, ver ControladorArmario).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class HotspotArmario : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private VistaArmario vista;

        [Header("Texto (una línea por click, en orden; al final da la vuelta)")]
        [SerializeField, TextArea(1, 3)] private string[] textos;

        [Header("Sonido")]
        [SerializeField] private AudioClip sonido;
        [Tooltip("Para los clicks siguientes al primero. Si está vacío se repite 'sonido'.")]
        [SerializeField] private AudioClip sonidoSiguientes;

        [Header("Efectos")]
        [Tooltip("Píxeles de sacudida del armario (0 = ninguna).")]
        [SerializeField] private float sacudida = 4f;
        [Tooltip("Se suma al miedo en cada click. Positivo = susto, negativo = alivio.")]
        [SerializeField] private float miedo = 0f;

        [Header("Revelar al primer click (opcional)")]
        [Tooltip("Objeto que empieza apagado y aparece con un 'pop' (ej. la lata en el cajón).")]
        [SerializeField] private GameObject revelar;

        [Header("Resalte")]
        [SerializeField] private Color colorHover = new Color(1f, 0.93f, 0.75f, 0.14f);
        [SerializeField] private Color colorClick = new Color(1f, 0.93f, 0.75f, 0.38f);
        [SerializeField] private float duracionFlash = 0.18f;

        private Image imagen;
        private int clicks;
        private bool encima;
        private Coroutine flash;

        private void Awake()
        {
            imagen = GetComponent<Image>();
            imagen.color = Transparente();
        }

        private void OnDisable()
        {
            encima = false;
            flash = null;
            if (imagen != null) imagen.color = Transparente();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            encima = true;
            if (flash == null) imagen.color = colorHover;
        }

        public void OnPointerExit(PointerEventData e)
        {
            encima = false;
            if (flash == null) imagen.color = Transparente();
        }

        public void OnPointerClick(PointerEventData e)
        {
            // Durante la secuencia de victoria de la lata el armario no responde.
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.CierreBloqueado) return;

            // Misma regla que el resto del juego: sin fósforo encendido no se interactúa.
            if (FosforoManager.Instance != null && !FosforoManager.Instance.PuedeInteractuar()) return;

            bool primerClick = clicks == 0;

            if (vista != null)
            {
                vista.Sonar(!primerClick && sonidoSiguientes != null ? sonidoSiguientes : sonido);

                if (textos != null && textos.Length > 0)
                    vista.Decir(textos[clicks % textos.Length]);

                vista.Sacudir(sacudida);
            }

            if (!Mathf.Approximately(miedo, 0f) && FearManager.Instance != null)
                FearManager.Instance.SetMiedo(FearManager.Instance.miedoActual + miedo);

            if (revelar != null && !revelar.activeSelf)
            {
                revelar.SetActive(true);
                StartCoroutine(Pop(revelar.transform));
            }

            clicks++;

            if (flash != null) StopCoroutine(flash);
            flash = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            for (float t = 0f; t < duracionFlash; t += Time.unscaledDeltaTime)
            {
                imagen.color = Color.Lerp(colorClick, encima ? colorHover : Transparente(), t / duracionFlash);
                yield return null;
            }

            imagen.color = encima ? colorHover : Transparente();
            flash = null;
        }

        private static IEnumerator Pop(Transform objetivo)
        {
            const float duracion = 0.3f;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                if (objetivo == null) yield break;
                float k = Mathf.Clamp01(t / duracion);
                // ease-out-back: se pasa un poco de 1 y vuelve, se siente "salta".
                float s = 1f + 2.70158f * Mathf.Pow(k - 1f, 3f) + 1.70158f * Mathf.Pow(k - 1f, 2f);
                objetivo.localScale = Vector3.one * Mathf.Max(0.01f, s);
                yield return null;
            }
            if (objetivo != null) objetivo.localScale = Vector3.one;
        }

        private Color Transparente() => new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
    }
}
