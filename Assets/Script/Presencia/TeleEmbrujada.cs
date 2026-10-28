using System.Collections;
using TMPro;
using Terror.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror
{
    /// <summary>
    /// La tele desenchufada que se prende sola (pedido de David, 2026-10-28).
    /// La primera vez que se toca, con un fósforo encendido:
    ///  1. la cámara se acerca a la tele;
    ///  2. la pantalla se llena de estática, con su ruido y una luz azul que parpadea;
    ///  3. por un instante, en el reflejo negro de la pantalla se ve a Luis y, detrás
    ///     de él, a la Presencia;
    ///  4. la tele se apaga de golpe, Luis piensa "...¿Quién está ahí?" y la cámara vuelve.
    ///
    /// Es solo un susto: no gasta fósforos ni sube la Presencia (lo único que la hace
    /// avanzar son los fósforos). Mientras dura, el fósforo no se gasta y el tiempo del
    /// cuarto pasa lento, igual que en los primeros planos. Las veces siguientes solo
    /// se ve el mensaje de siempre de la tele (ObjetoNarrable) y el pensamiento de después.
    ///
    /// Va en la zona de la tele (Narrable_Television, en Interactuables), junto a su
    /// ObjetoNarrable. La pantalla se arma en runtime encima del dibujo del fondo.
    /// </summary>
    public class TeleEmbrujada : MonoBehaviour, IPointerClickHandler
    {
        [Header("Pantalla (unidades del Canvas Interactuables, desde el centro de esta zona)")]
        [SerializeField] private Vector2 posicionPantalla = new Vector2(18.5f, -3.4f);
        [SerializeField] private Vector2 tamanoPantalla = new Vector2(275.4f, 215.2f);
        [SerializeField] private Sprite[] estatica;
        [SerializeField] private Sprite reflejo;

        [Header("Ritmo (segundos reales)")]
        [Tooltip("Zoom de la cámara hacia la tele (0.35 = se ve un 35% de la altura de siempre).")]
        [SerializeField, Range(0.1f, 1f)] private float zoom = 0.35f;
        [SerializeField] private float duracionZoom = 0.6f;
        [SerializeField] private float duracionEstatica = 1.3f;
        [SerializeField] private float duracionReflejo = 0.4f;
        [SerializeField] private float sacudida = 18f;

        [Header("Luz de la pantalla")]
        [SerializeField] private bool conLuz = true;
        [SerializeField] private Color colorLuz = new Color(0.6f, 0.72f, 1f, 1f);
        [SerializeField] private float intensidadLuz = 1.6f;
        [SerializeField] private float radioLuz = 900f;

        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoEstatica;
        [SerializeField] private AudioClip sonidoGolpe;

        [Header("Textos de Luis")]
        [TextArea(1, 2)] [SerializeField] private string textoSusto = "...¿Quién está ahí?";
        [TextArea(1, 2)] [SerializeField] private string textoDespues = "No pienso volver a mirar esa tele.";
        [SerializeField] private TMP_FontAsset fuente;

        private Image pantalla;
        private Light2D luz;
        private AudioSource audioFuente;
        private bool asustado;
        private bool enCurso;

        public void OnPointerClick(PointerEventData e)
        {
            if (enCurso || !PuedeTocar()) return;

            if (asustado)
            {
                // Después del susto: el mensaje de la tele (ObjetoNarrable) y lo que piensa Luis.
                Pensar(textoDespues, 3f);
                return;
            }
            StartCoroutine(Susto());
        }

        private static bool PuedeTocar()
        {
            if (Time.timeScale == 0f) return false;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return false;
            var panel = PanelInspeccion.Instance;
            if (panel != null && (panel.EstaAbierto || panel.CierreBloqueado || panel.HayCapaAbierta)) return false;
            return FosforoManager.Instance == null || FosforoManager.Instance.PuedeInteractuar();
        }

        private IEnumerator Susto()
        {
            enCurso = true;
            asustado = true;
            Construir();

            FosforoManager.Instance?.PausarQuemado(this);
            TiempoLento.Pedir(this);
            var camara = CamaraController.Instance;
            Vector3 centro = pantalla.rectTransform.TransformPoint(Vector3.zero);
            camara?.Enfocar(centro, zoom, duracionZoom);
            yield return EsperarSinPausa(duracionZoom + 0.35f);

            // Se prende sola.
            pantalla.gameObject.SetActive(true);
            Sonar(sonidoEstatica, 0.7f);
            float t = 0f;
            int cuadro = 0;
            while (t < duracionEstatica)
            {
                if (estatica != null && estatica.Length > 0) pantalla.sprite = estatica[cuadro++ % estatica.Length];
                pantalla.color = Color.white;
                Luz(Random.Range(0.6f, 1f));
                yield return EsperarSinPausa(0.05f);
                t += 0.05f;
            }

            // El reflejo: Luis... y lo que tiene detrás.
            if (reflejo != null) pantalla.sprite = reflejo;
            Luz(0.25f);
            if (audioFuente != null) audioFuente.Stop();
            Sonar(sonidoGolpe, 1f);
            camara?.Sacudir(sacudida, 0.35f);
            yield return EsperarSinPausa(duracionReflejo);

            // Un último golpe de estática y se apaga de golpe.
            if (estatica != null && estatica.Length > 0) pantalla.sprite = estatica[0];
            Luz(1f);
            yield return EsperarSinPausa(0.08f);
            pantalla.gameObject.SetActive(false);
            Luz(0f);

            yield return EsperarSinPausa(0.5f);
            Pensar(textoSusto, 3f);
            yield return EsperarSinPausa(0.6f);

            camara?.Soltar(duracionZoom);
            TiempoLento.Soltar(this);
            FosforoManager.Instance?.ReanudarQuemado(this);
            enCurso = false;
        }

        private void OnDisable()
        {
            if (!enCurso) return;
            enCurso = false;
            if (pantalla != null) pantalla.gameObject.SetActive(false);
            Luz(0f);
            CamaraController.Instance?.Soltar(duracionZoom);
            TiempoLento.Soltar(this);
            FosforoManager.Instance?.ReanudarQuemado(this);
        }

        private void Pensar(string texto, float segundos)
        {
            if (string.IsNullOrEmpty(texto)) return;
            var cartel = CartelNarrativa.Instancia;
            if (cartel == null) return;
            cartel.Mostrar(this, texto, fuente, segundos);
            cartel.SoltarHover(this);
        }

        private void Luz(float k)
        {
            if (luz == null) return;
            luz.intensity = intensidadLuz * k;
            luz.enabled = k > 0f;
        }

        private void Sonar(AudioClip clip, float volumen)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip, volumen);
        }

        private void Construir()
        {
            if (pantalla != null) return;

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.spatialBlend = 0f;

            // La pantalla brilla sola: va sin luz del cuarto. Orden 0 (el del fondo) pero
            // un poco más cerca de la cámara: queda encima del dibujo y detrás de Luis (1).
            pantalla = NuevaImagen("PantallaTele", transform, posicionPantalla, tamanoPantalla, null);
            pantalla.raycastTarget = false;
            pantalla.preserveAspect = false;
            pantalla.rectTransform.localPosition += new Vector3(0f, 0f, -2f);
            CapaEncimaEnCuarto(pantalla.gameObject, 0);
            var raycaster = pantalla.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false; // no le quita los clics a la zona
            pantalla.gameObject.SetActive(false);

            if (conLuz)
            {
                var go = new GameObject("LuzTele");
                go.transform.SetParent(pantalla.transform.parent, false);
                go.transform.position = pantalla.rectTransform.TransformPoint(Vector3.zero);
                luz = go.AddComponent<Light2D>();
                luz.lightType = Light2D.LightType.Point;
                luz.color = colorLuz;
                luz.pointLightOuterRadius = radioLuz;
                luz.pointLightInnerRadius = radioLuz * 0.15f;
                luz.intensity = 0f;
                luz.enabled = false;
            }
        }
    }
}
