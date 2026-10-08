using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Cinemática entre la historia y el cuarto 1: Luis camina por el pasillo hasta
    /// la puerta del cuarto prohibido, la abre (rechinido), entra a la oscuridad y
    /// la puerta se cierra de golpe detrás de él (portazo + sacudida). Después
    /// fundido a negro y carga el juego.
    ///
    /// Va sola en EntradaCuarto.unity; arma su propio Canvas en runtime. Click,
    /// Espacio o Enter la saltan. Arte placeholder en Assets/Recursos/Cinematica/
    /// hasta que llegue el definitivo: se reemplaza asignando otros sprites acá.
    /// </summary>
    public class CinematicaPuerta : MonoBehaviour
    {
        [Header("Escena siguiente")]
        [SerializeField] private string escenaSiguiente = "JUEGO";

        [Header("Arte")]
        [SerializeField] private Sprite fondoPasillo;
        [SerializeField] private Sprite hojaPuerta;
        [SerializeField] private Sprite brilloFosforo;
        [SerializeField] private Sprite luisQuieto;
        [SerializeField] private Sprite luisPasoA;
        [SerializeField] private Sprite luisPasoB;

        [Header("Sonido")]
        [SerializeField] private AudioClip sonidoAbrir;
        [SerializeField] private AudioClip sonidoPortazo;
        [SerializeField] private AudioClip sonidoPasos;

        [Header("Distribución (Canvas 1920x1080, origen al centro)")]
        [Tooltip("Rect del hueco de la puerta en el fondo: centro x, y del piso, ancho, alto.")]
        [SerializeField] private Vector4 puerta = new Vector4(330f, -330f, 300f, 640f);
        [SerializeField] private Vector2 tamanoLuis = new Vector2(280f, 520f);
        [SerializeField] private float xInicioLuis = -1150f;
        [SerializeField] private float xFrenteAPuerta = 120f;

        [Header("Tiempos")]
        [SerializeField] private float duracionCaminata = 2.8f;
        [SerializeField] private float segundosPorPaso = 0.22f;
        [SerializeField] private float duracionAbrir = 1.6f;
        [SerializeField] private float duracionEntrar = 1.3f;
        [SerializeField] private float silencioAntesDelPortazo = 0.7f;
        [SerializeField] private float duracionPortazo = 0.09f;
        [SerializeField] private float duracionSacudida = 0.4f;
        [SerializeField] private float fuerzaSacudida = 22f;
        [SerializeField] private float esperaFinal = 0.9f;
        [SerializeField] private float duracionFundido = 0.6f;

        private const float AperturaMinima = 0.12f; // la hoja abierta se ve "de canto"

        private RectTransform escenario;
        private RectTransform hoja;
        private Image luis;
        private Image brillo;
        private Image negro;
        private AudioSource fuente;
        private bool terminando;

        private void Start()
        {
            Time.timeScale = 1f;
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            Construir();
            StartCoroutine(SilenciarMusicaDelMenu());
            StartCoroutine(Reproducir());
        }

        private void Update()
        {
            bool saltar = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame
                                                 || Keyboard.current.enterKey.wasPressedThisFrame));
            if (saltar && !terminando)
            {
                StopAllCoroutines();
                StartCoroutine(Terminar(0.25f));
            }
        }

        // ---------- Secuencia ----------

        private IEnumerator Reproducir()
        {
            yield return Fundir(negro, 1f, 0f, 0.8f);

            // 1) Luis camina hasta la puerta.
            if (sonidoPasos != null) fuente.PlayOneShot(sonidoPasos, 0.6f);
            yield return Caminar(xInicioLuis, xFrenteAPuerta, duracionCaminata);
            luis.sprite = luisQuieto;
            yield return Esperar(0.5f);

            // 2) Abre la puerta, despacio, con rechinido.
            if (sonidoAbrir != null) fuente.PlayOneShot(sonidoAbrir);
            yield return Animar(duracionAbrir, p =>
            {
                // Arranca lento (duda) y termina de abrirse.
                float e = p * p * (3f - 2f * p);
                SetApertura(Mathf.Lerp(1f, AperturaMinima, e));
            });
            yield return Esperar(0.4f);

            // 3) Entra: avanza hacia el hueco y se lo traga la oscuridad.
            fuente.Stop();
            Vector2 desde = luis.rectTransform.anchoredPosition;
            Vector2 hasta = new Vector2(puerta.x, desde.y + 20f);
            float t = 0f;
            yield return Animar(duracionEntrar, p =>
            {
                t += Time.unscaledDeltaTime;
                luis.sprite = ((int)(t / segundosPorPaso) % 2 == 0) ? luisPasoA : luisPasoB;
                luis.rectTransform.anchoredPosition = Vector2.Lerp(desde, hasta, p);
                float escala = Mathf.Lerp(1f, 0.85f, p);
                luis.rectTransform.localScale = new Vector3(escala, escala, 1f);
                brillo.rectTransform.anchoredPosition = luis.rectTransform.anchoredPosition + new Vector2(70f, 150f) * escala;
                float a = 1f - Mathf.Clamp01((p - 0.3f) / 0.7f);
                SetAlpha(luis, a);
                SetAlpha(brillo, a * 0.55f);
            });

            // 4) Silencio... y PORTAZO.
            yield return Esperar(silencioAntesDelPortazo);
            if (sonidoPortazo != null) fuente.PlayOneShot(sonidoPortazo, 1f);
            yield return Animar(duracionPortazo, p => SetApertura(Mathf.Lerp(AperturaMinima, 1f, p * p)));
            yield return Sacudir();

            yield return Esperar(esperaFinal);
            yield return Terminar(duracionFundido);
        }

        private IEnumerator Caminar(float x0, float x1, float duracion)
        {
            float t = 0f;
            Vector2 pos = luis.rectTransform.anchoredPosition;
            yield return Animar(duracion, p =>
            {
                t += Time.unscaledDeltaTime;
                luis.sprite = ((int)(t / segundosPorPaso) % 2 == 0) ? luisPasoA : luisPasoB;
                pos.x = Mathf.Lerp(x0, x1, p);
                // Leve rebote al caminar.
                luis.rectTransform.anchoredPosition = new Vector2(pos.x, pos.y + Mathf.Abs(Mathf.Sin(t * Mathf.PI / segundosPorPaso)) * 6f);
                brillo.rectTransform.anchoredPosition = luis.rectTransform.anchoredPosition + new Vector2(70f, 150f);
                SetAlpha(brillo, 0.5f + Mathf.PerlinNoise(t * 6f, 0f) * 0.15f);
            });
        }

        private IEnumerator Sacudir()
        {
            yield return Animar(duracionSacudida, p =>
            {
                float fuerza = fuerzaSacudida * (1f - p);
                escenario.anchoredPosition = new Vector2(Random.Range(-fuerza, fuerza), Random.Range(-fuerza, fuerza));
            });
            escenario.anchoredPosition = Vector2.zero;
        }

        private IEnumerator Terminar(float fundido)
        {
            terminando = true;
            AsyncOperation carga = SceneManager.LoadSceneAsync(escenaSiguiente);
            if (carga != null) carga.allowSceneActivation = false;

            yield return Fundir(negro, negro.color.a, 1f, fundido);

            if (carga != null) carga.allowSceneActivation = true;
            else SceneManager.LoadScene(escenaSiguiente);
        }

        // La música del menú (PersistentAudio) sigue sonando hasta JUEGO: acá molesta.
        private IEnumerator SilenciarMusicaDelMenu()
        {
            PersistentAudio musica = FindFirstObjectByType<PersistentAudio>();
            AudioSource src = musica != null ? musica.GetComponent<AudioSource>() : null;
            if (src == null) yield break;

            float v0 = src.volume;
            yield return Animar(1f, p => src.volume = Mathf.Lerp(v0, 0f, p));
            Destroy(musica.gameObject);
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            var goCanvas = new GameObject("CanvasCinematica", typeof(Canvas), typeof(CanvasScaler));
            var canvas = goCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = goCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image fondoNegro = CrearImagen("Negro", goCanvas.transform, null, Color.black);
            Estirar(fondoNegro.rectTransform);

            // Todo lo que tiembla con el portazo cuelga de 'escenario'.
            escenario = CrearRect("Escenario", goCanvas.transform);
            escenario.sizeDelta = new Vector2(1920f, 1080f);

            Image fondo = CrearImagen("Pasillo", escenario, fondoPasillo, Color.white);
            fondo.rectTransform.sizeDelta = new Vector2(1960f, 1110f); // un poco más grande: la sacudida no deja ver bordes

            Image imgHoja = CrearImagen("PuertaHoja", escenario, hojaPuerta, Color.white);
            hoja = imgHoja.rectTransform;
            hoja.pivot = new Vector2(0f, 0f); // bisagra a la izquierda
            hoja.sizeDelta = new Vector2(puerta.z, puerta.w);
            hoja.anchoredPosition = new Vector2(puerta.x - puerta.z / 2f, puerta.y);

            brillo = CrearImagen("Brillo", escenario, brilloFosforo, new Color(1f, 1f, 1f, 0.5f));
            brillo.rectTransform.sizeDelta = new Vector2(420f, 420f);

            luis = CrearImagen("Luis", escenario, luisPasoA, Color.white);
            luis.preserveAspect = true;
            luis.rectTransform.pivot = new Vector2(0.5f, 0f); // pies en el piso
            luis.rectTransform.sizeDelta = tamanoLuis;
            luis.rectTransform.anchoredPosition = new Vector2(xInicioLuis, puerta.y - 10f);
            brillo.rectTransform.anchoredPosition = luis.rectTransform.anchoredPosition + new Vector2(70f, 150f);

            negro = CrearImagen("Fundido", goCanvas.transform, null, Color.black);
            Estirar(negro.rectTransform);
        }

        private void SetApertura(float apertura)
        {
            hoja.localScale = new Vector3(apertura, 1f, 1f);
            // De canto se ve más oscura.
            float luz = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(AperturaMinima, 1f, apertura));
            hoja.GetComponent<Image>().color = new Color(luz, luz, luz, 1f);
        }

        // ---------- Ayudantes ----------

        private static IEnumerator Animar(float duracion, System.Action<float> paso)
        {
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                paso(Mathf.Clamp01(t / duracion));
                yield return null;
            }
            paso(1f);
        }

        private static IEnumerator Esperar(float segundos)
        {
            yield return new WaitForSecondsRealtime(segundos);
        }

        private static IEnumerator Fundir(Image img, float desde, float hasta, float duracion)
        {
            yield return Animar(duracion, p => SetAlpha(img, Mathf.Lerp(desde, hasta, p)));
        }

        private static RectTransform CrearRect(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(padre, false);
            return r;
        }

        private static void Estirar(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static Image CrearImagen(string nombre, Transform padre, Sprite sprite, Color color)
        {
            Image img = CrearRect(nombre, padre).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetAlpha(Graphic g, float a)
        {
            Color c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
