using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Objeto del cuarto que Luis "comenta" (una foto, la tele, un cuadro...). Es una
    /// zona invisible (Image transparente) sobre el dibujo del fondo:
    ///   - Al pasar el mouse se dibuja un contorno alrededor y aparece el mensaje en la
    ///     parte de abajo de la pantalla.
    ///   - Al hacer click se escucha la narración del mensaje, SOLO la primera vez; los
    ///     clicks siguientes vuelven a mostrar el texto, pero ya sin audio.
    /// Misma regla que el resto del juego: sin fósforo encendido no se ve ni se oye nada.
    /// El cartel de abajo y la fuente de audio son compartidos por todos los objetos
    /// (se arman solos en runtime), así nunca suenan dos narraciones a la vez.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ObjetoNarrable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Mensaje")]
        [SerializeField, TextArea(2, 4)] private string mensaje;
        [Tooltip("Narración del mensaje. Suena solo en el primer click.")]
        [SerializeField] private AudioClip narracion;
        [SerializeField, Range(0f, 1f)] private float volumen = 1f;
        [Tooltip("Fuente del cartel de abajo. Vacía = la fuente por defecto de TextMeshPro.")]
        [SerializeField] private TMP_FontAsset fuente;
        [Tooltip("Segundos que el mensaje sigue en pantalla tras el click aunque el mouse se vaya (si la narración dura más, espera a que termine).")]
        [SerializeField] private float segundosMensajeTrasClick = 3f;

        [Header("Contorno")]
        [SerializeField] private Color colorContorno = new Color(1f, 0.78f, 0.45f, 0.9f);
        [SerializeField] private float grosorContorno = 3f;
        [Tooltip("Separación entre el borde de la zona y el contorno.")]
        [SerializeField] private float margenContorno = 4f;

        private bool narrado;
        private bool encima;
        private CanvasGroup contorno;
        private Coroutine fadeContorno;

        private void Awake()
        {
            Image zona = GetComponent<Image>();
            zona.color = new Color(1f, 1f, 1f, 0f);   // invisible, pero sigue recibiendo el mouse
            contorno = CrearContorno();
        }

        private void OnDisable()
        {
            encima = false;
            fadeContorno = null;
            if (contorno != null) contorno.alpha = 0f;
        }

        private void Update()
        {
            // Si el fósforo se apaga (o se pausa, o se gana/pierde) con el mouse encima, se esconde todo.
            if (encima && !PuedeInteractuar())
                Salir();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!PuedeInteractuar()) return;

            encima = true;
            FundirContorno(1f);
            CartelNarrativa.Instancia.Mostrar(this, mensaje, fuente, 0f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (encima) Salir();
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!PuedeInteractuar()) return;

            AudioClip clip = narrado ? null : narracion;
            narrado = true;

            float duracion = Mathf.Max(segundosMensajeTrasClick, clip != null ? clip.length + 0.5f : 0f);
            CartelNarrativa.Instancia.Mostrar(this, mensaje, fuente, duracion);
            if (clip != null)
                CartelNarrativa.Instancia.Narrar(clip, volumen);

            // Latido del contorno para confirmar el click.
            if (contorno != null)
                StartCoroutine(LatidoContorno());
        }

        private void Salir()
        {
            encima = false;
            FundirContorno(0f);
            CartelNarrativa.Instancia.SoltarHover(this);
        }

        private static bool PuedeInteractuar()
        {
            if (Time.timeScale == 0f) return false;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return false;
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.EstaAbierto) return false;
            return FosforoManager.Instance == null || FosforoManager.Instance.PuedeInteractuar();
        }

        // Cuatro barras finas alrededor de la zona, agrupadas en un CanvasGroup para el fade.
        private CanvasGroup CrearContorno()
        {
            var go = new GameObject("Contorno", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-margenContorno, -margenContorno);
            rect.offsetMax = new Vector2(margenContorno, margenContorno);

            var grupo = go.GetComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
            grupo.interactable = false;

            CrearBarra(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -grosorContorno), Vector2.zero);   // arriba
            CrearBarra(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, grosorContorno));    // abajo
            CrearBarra(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(grosorContorno, 0f));    // izquierda
            CrearBarra(rect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-grosorContorno, 0f), Vector2.zero);   // derecha
            return grupo;
        }

        private void CrearBarra(RectTransform padre, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("Barra", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var imagen = go.GetComponent<Image>();
            imagen.color = colorContorno;
            imagen.raycastTarget = false;
        }

        private void FundirContorno(float destino)
        {
            if (contorno == null || !isActiveAndEnabled) return;
            if (fadeContorno != null) StopCoroutine(fadeContorno);
            fadeContorno = StartCoroutine(Fundir(destino));
        }

        private IEnumerator Fundir(float destino)
        {
            const float duracion = 0.15f;
            float inicio = contorno.alpha;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                contorno.alpha = Mathf.Lerp(inicio, destino, t / duracion);
                yield return null;
            }
            contorno.alpha = destino;
            fadeContorno = null;
        }

        private IEnumerator LatidoContorno()
        {
            const float duracion = 0.25f;
            Transform t0 = contorno.transform;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / duracion * Mathf.PI);
                t0.localScale = Vector3.one * (1f + 0.04f * k);
                yield return null;
            }
            t0.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Cartel de texto en la parte de abajo de la pantalla + fuente de audio de las
    /// narraciones. Lo comparten todos los ObjetoNarrable; se crea solo la primera vez
    /// que alguien lo pide y muere con la escena.
    /// </summary>
    public class CartelNarrativa : MonoBehaviour
    {
        private static CartelNarrativa instancia;

        public static CartelNarrativa Instancia
        {
            get
            {
                if (instancia == null)
                    instancia = new GameObject("CartelNarrativa").AddComponent<CartelNarrativa>();
                return instancia;
            }
        }

        private CanvasGroup grupo;
        private TextMeshProUGUI texto;
        private AudioSource fuenteAudio;
        private Object dueno;
        private float visibleHasta;     // tiempo (sin escala) hasta el que se mantiene tras un click
        private bool hoverActivo;
        private bool pausadoPorTiempo;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            var escalador = gameObject.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;

            grupo = gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = false;
            grupo.interactable = false;

            // Franja oscura abajo, para que el texto se lea sobre cualquier parte del cuarto.
            var franja = new GameObject("Franja", typeof(RectTransform), typeof(Image));
            var rf = (RectTransform)franja.transform;
            rf.SetParent(transform, false);
            rf.anchorMin = new Vector2(0f, 0f);
            rf.anchorMax = new Vector2(1f, 0f);
            rf.pivot = new Vector2(0.5f, 0f);
            rf.anchoredPosition = Vector2.zero;
            rf.sizeDelta = new Vector2(0f, 190f);
            var imagenFranja = franja.GetComponent<Image>();
            imagenFranja.color = new Color(0f, 0f, 0f, 0.65f);
            imagenFranja.raycastTarget = false;

            var goTexto = new GameObject("Texto", typeof(RectTransform));
            var rt = (RectTransform)goTexto.transform;
            rt.SetParent(rf, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(220f, 20f);
            rt.offsetMax = new Vector2(-220f, -20f);
            texto = goTexto.AddComponent<TextMeshProUGUI>();
            texto.alignment = TextAlignmentOptions.Center;
            texto.fontSize = 44f;
            texto.fontStyle = FontStyles.Italic;
            texto.color = new Color(0.96f, 0.9f, 0.78f);
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.raycastTarget = false;

            fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.playOnAwake = false;
            fuenteAudio.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            if (instancia == this) instancia = null;
        }

        private void Update()
        {
            // Con el juego en pausa (timeScale 0) la narración espera, igual que la Presencia.
            bool pausado = Time.timeScale == 0f;
            if (pausado != pausadoPorTiempo)
            {
                pausadoPorTiempo = pausado;
                if (pausado) fuenteAudio.Pause();
                else fuenteAudio.UnPause();
            }

            bool visible = !pausado && (hoverActivo || Time.unscaledTime < visibleHasta);
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime / 0.2f);
        }

        /// <param name="segundosMinimos">0 = solo mientras dure el hover; si no, se mantiene al menos ese tiempo.</param>
        public void Mostrar(Object quien, string mensaje, TMP_FontAsset fuente, float segundosMinimos)
        {
            if (dueno != quien && fuenteAudio.isPlaying)
                fuenteAudio.Stop();   // la narración de otro objeto no sigue debajo del texto nuevo

            dueno = quien;
            hoverActivo = true;
            texto.text = mensaje;
            if (fuente != null) texto.font = fuente;
            visibleHasta = segundosMinimos > 0f ? Time.unscaledTime + segundosMinimos : 0f;
        }

        public void SoltarHover(Object quien)
        {
            if (dueno == quien) hoverActivo = false;
        }

        public void Narrar(AudioClip clip, float volumen)
        {
            fuenteAudio.Stop();
            fuenteAudio.clip = clip;
            fuenteAudio.volume = volumen;
            fuenteAudio.Play();
        }
    }
}
