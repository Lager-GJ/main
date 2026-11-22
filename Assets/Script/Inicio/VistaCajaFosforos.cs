using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror.UI
{
    /// <summary>
    /// La caja de fósforos ("El Cuco" desde 2026-11-12, antes "La Chispa") en primer plano (pedido de David, 2026-10-27).
    /// Al tocarla en el cajón del inicio, la caja se ve en grande:
    ///  - un clic en la caja (o Enter) la abre: sale la bandeja con los fósforos. Ese es
    ///    el momento del portazo (lo hace InicioCuarto) y del tutorial "Pulsa E", que es
    ///    la hoja de instrucciones de la caja. Ya abierta, el clic lee su descripción en
    ///    la franja de subtítulos;
    ///  - el botón "Voltear" de abajo (o A/D, flechas) la da vuelta para leer el reverso:
    ///    la advertencia y "Lea las instrucciones antes de usar";
    ///  - la bandeja muestra los fósforos que quedan: al prender uno, sale de la caja.
    /// Igual que el resto de los primeros planos desde 2026-11-17: clic = interactuar,
    /// botón = voltear, sin línea de ayuda y centrada en la pantalla.
    ///
    /// Se arma en runtime dentro del panel del cajón (Canvas de la UI). Es una "capa" de
    /// PanelInspeccion: Escape o un clic afuera la cierran y se vuelve al cajón.
    /// </summary>
    public class VistaCajaFosforos : MonoBehaviour, ICapaEscape
    {
        [Serializable]
        public class Config
        {
            [Tooltip("Frente de la caja cerrada (la funda).")]
            public Sprite frente;
            [Tooltip("Reverso de la caja: advertencia y \"Lea las instrucciones antes de usar\".")]
            public Sprite reverso;
            [Tooltip("La bandeja que sale de la funda al abrir (no se usa con Dibujo Completo).")]
            public Sprite bandeja;
            [Tooltip("Un fósforo (se repite por cada fósforo que queda).")]
            public Sprite fosforo;
            [Tooltip("Halo del fósforo encendido. Vacío = sin halo.")]
            public Sprite brillo;
            [Tooltip("Marcado: el dibujo del frente ya muestra la caja abierta con sus fósforos. No se usan la bandeja ni los fósforos sueltos: al abrir solo hay un pequeño golpe.")]
            public bool dibujoCompleto;
            [Tooltip("Tamaño de la funda en pantalla (frente y reverso se ajustan a su proporción dentro de este alto).")]
            public Vector2 tamano = new Vector2(380f, 545f);
            [Tooltip("Centro de la caja. Un poco arriba del centro de la pantalla, para que el botón Voltear quede arriba de los subtítulos.")]
            public Vector2 posicion = new Vector2(0f, 40f);
            [Tooltip("Cuánto sube la bandeja al abrir (fracción del alto de la funda).")]
            [Range(0f, 0.6f)] public float subidaBandeja = 0.38f;
            [Tooltip("Oscurece el cuarto detrás de la caja.")]
            [Range(0f, 1f)] public float fondo = 0.6f;
            [Tooltip("Lo que se lee al hacer clic en la caja ya abierta, una página por clic.")]
            [TextArea(1, 3)] public string[] descripcion =
            {
                "«El Cuco». Fósforos de cocina, de los de antes.",
                "Abajo, en letras chiquitas: «Cada llama tiene un precio».",
            };
            [Tooltip("Última página de la descripción. {0} = fósforos que quedan.")]
            [TextArea(1, 2)] public string descripcionQuedan = "Me quedan {0}. Tengo que cuidarlos.";
            public AudioClip sonidoVoltear;
            public AudioClip sonidoAbrir;
        }

        private Config cfg;
        private TMP_FontAsset fuente;
        private Action alAbrir;
        private Action<bool> alCerrar;
        private Func<bool> puedeAbrir;
        private AudioSource audioFuente;

        private CanvasGroup grupo;
        private Image fondo;
        private RectTransform caja;
        private RectTransform girador;
        private Image funda;
        private Image bandeja;
        private Image halo;
        private Image[] fosforos;
        private GameObject botonVoltear;
        private int pagina = -1;
        private const string TextoVoltear = "Voltear";

        private float yCerrada;
        private float yAbierta;
        private bool reverso;
        private bool girando;
        private bool abriendo;
        private Color tinte = Color.white;
        private Coroutine rutinaTinte;
        private Coroutine rutinaHalo;

        public bool Visible => gameObject.activeSelf;
        public bool Abierta { get; private set; }

        public bool CapaAbierta => gameObject.activeInHierarchy;
        public void CerrarCapa() => Cerrar();

        /// <summary>
        /// Crea la vista dentro de 'padre'. alAbrir: se abrió la caja (portazo).
        /// alCerrar(abierta): se volvió al cajón. puedeAbrir: si ahora se puede abrir.
        /// </summary>
        public static VistaCajaFosforos Crear(Transform padre, Config cfg, TMP_FontAsset fuente, AudioSource audio,
            Action alAbrir, Action<bool> alCerrar, Func<bool> puedeAbrir)
        {
            var go = new GameObject("VistaCajaFosforos", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(padre, false);
            go.transform.SetAsFirstSibling(); // debajo de los mensajes y las tarjetas del panel
            var v = go.AddComponent<VistaCajaFosforos>();
            v.cfg = cfg ?? new Config();
            v.fuente = fuente;
            v.audioFuente = audio;
            v.alAbrir = alAbrir;
            v.alCerrar = alCerrar;
            v.puedeAbrir = puedeAbrir;
            v.Construir();
            go.SetActive(false);
            return v;
        }

        // ---------- Mostrar / cerrar ----------

        public void Mostrar(Color tinteInicial, int fosforosQueQuedan)
        {
            if (Visible) return;
            gameObject.SetActive(true);
            reverso = false;
            girando = false;
            pagina = -1;
            girador.localScale = Vector3.one;
            PonerSpriteFunda(cfg.frente);
            caja.anchoredPosition = PosicionObjetivo;
            PonerTinte(tinteInicial);
            MostrarFosforos(fosforosQueQuedan);
            PanelInspeccion.Instance?.AbrirCapa(this);

            grupo.alpha = 0f;
            StartCoroutine(Animar(0.3f, p =>
            {
                grupo.alpha = p;
                fondo.color = new Color(0f, 0f, 0f, cfg.fondo * p);
                caja.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, 1f - Mathf.Pow(1f - p, 3f));
            }));
        }

        public void Cerrar()
        {
            if (!Visible || abriendo) return;
            StopAllCoroutines();
            rutinaTinte = rutinaHalo = null;
            girando = false;
            gameObject.SetActive(false);
            PanelInspeccion.Instance?.QuitarCapa(this);
            alCerrar?.Invoke(Abierta);
        }

        /// <summary>Se cerró el cajón entero (se apagó el fósforo, por ejemplo).</summary>
        public void CerrarSinAnimar()
        {
            abriendo = false;
            Cerrar();
        }

        // ---------- Luz ----------

        public void CambiarTinte(Color destino, float duracion)
        {
            if (!Visible) { PonerTinte(destino); return; }
            if (rutinaTinte != null) StopCoroutine(rutinaTinte);
            Color desde = tinte;
            rutinaTinte = StartCoroutine(Animar(duracion, p => PonerTinte(Color.Lerp(desde, destino, p))));
        }

        public void Halo(bool encendido)
        {
            if (halo == null) return;
            if (!Visible) { halo.color = new Color(1f, 0.85f, 0.6f, encendido ? 0.55f : 0f); return; }
            if (rutinaHalo != null) StopCoroutine(rutinaHalo);
            float desde = halo.color.a, hasta = encendido ? 0.55f : 0f;
            rutinaHalo = StartCoroutine(Animar(0.35f, p => halo.color = new Color(1f, 0.85f, 0.6f, Mathf.Lerp(desde, hasta, p))));
        }

        private void PonerTinte(Color c)
        {
            tinte = c;
            funda.color = c;
            bandeja.color = c;
            foreach (var f in fosforos) f.color = new Color(c.r, c.g, c.b, f.color.a);
        }

        // ---------- Fósforos ----------

        public void MostrarFosforos(int cuantos)
        {
            for (int i = 0; i < fosforos.Length; i++)
            {
                fosforos[i].gameObject.SetActive(i < cuantos);
                fosforos[i].rectTransform.anchoredPosition = PosicionFosforo(i);
                fosforos[i].color = new Color(tinte.r, tinte.g, tinte.b, 1f);
            }
        }

        /// <summary>Se prendió un fósforo: sale de la caja hacia arriba y desaparece.</summary>
        public void SacarFosforo(int quedan)
        {
            if (!Visible || quedan < 0 || quedan >= fosforos.Length) { MostrarFosforos(quedan); return; }
            Image f = fosforos[quedan];
            if (!f.gameObject.activeSelf) { MostrarFosforos(quedan); return; }
            Vector2 p0 = f.rectTransform.anchoredPosition;
            float alto = cfg.tamano.y;
            StartCoroutine(Animar(0.5f, p =>
            {
                f.rectTransform.anchoredPosition = p0 + new Vector2(0f, alto * 0.35f * p);
                f.color = new Color(tinte.r, tinte.g, tinte.b, 1f - p * p);
                if (p >= 1f) f.gameObject.SetActive(false);
            }));
        }

        private Vector2 PosicionFosforo(int i)
        {
            // Medidas del dibujo (bandeja 546x800, fósforo 74x430) pasadas a la pantalla.
            float k = cfg.tamano.x / 607f;
            return new Vector2((i - 1.5f) * 105f * k, (400f - 50f - 215f - (i % 2) * 14f) * k);
        }

        // ---------- Clics y teclas ----------

        private void Update()
        {
            if (Pausado()) return;
            var k = Keyboard.current;
            if (k == null) return;
            if (Controles.Pulsada(Accion.Izquierda) || Controles.Pulsada(Accion.Derecha))
                Voltear();
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
                Inspeccionar();
        }

        private static bool Pausado() => PauseManager.Instance != null && PauseManager.Instance.EstaPausado;

        /// <summary>Clic en la caja (o Enter): la abre; ya abierta, lee su descripción.</summary>
        public void Inspeccionar()
        {
            if (!Visible || Pausado() || girando || abriendo) return;

            if (!Abierta && (puedeAbrir == null || puedeAbrir()))
            {
                StartCoroutine(reverso ? VoltearYAbrir() : Abrir());
                return;
            }

            int quedan = 0;
            foreach (var f in fosforos) if (f.gameObject.activeSelf) quedan++;
            int total = (cfg.descripcion != null ? cfg.descripcion.Length : 0) + (string.IsNullOrEmpty(cfg.descripcionQuedan) ? 0 : 1);
            if (total == 0) return;
            pagina = (pagina + 1) % total;
            int nDescripcion = cfg.descripcion != null ? cfg.descripcion.Length : 0;
            Decir(pagina < nDescripcion ? cfg.descripcion[pagina] : string.Format(cfg.descripcionQuedan, quedan));
        }

        // La descripción sale en la franja de subtítulos, como los pensamientos de Luis.
        private void Decir(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            var cartel = CartelNarrativa.Instancia;
            cartel.Mostrar(this, texto, fuente, Mathf.Clamp(texto.Length * 0.06f, 2.5f, 5f));
            cartel.SoltarHover(this);
        }

        // Inspeccionar con el reverso a la vista: primero vuelve al frente y después la abre.
        private IEnumerator VoltearYAbrir()
        {
            yield return Girar();
            yield return Abrir();
        }

        public void Voltear()
        {
            if (!Visible || girando || abriendo) return;
            StartCoroutine(Girar());
        }

        private IEnumerator Girar()
        {
            girando = true;
            Sonar(cfg.sonidoVoltear);
            yield return Animar(0.16f, p => girador.localScale = new Vector3(Mathf.Cos(p * Mathf.PI * 0.5f), 1f + 0.04f * p, 1f));
            reverso = !reverso;
            PonerSpriteFunda(reverso ? cfg.reverso : cfg.frente);
            yield return Animar(0.16f, p => girador.localScale = new Vector3(Mathf.Sin(p * Mathf.PI * 0.5f), 1.04f - 0.04f * p, 1f));
            girador.localScale = Vector3.one;
            girando = false;
        }

        private IEnumerator Abrir()
        {
            abriendo = true;
            Sonar(cfg.sonidoAbrir);
            RectTransform b = bandeja.rectTransform;
            if (cfg.dibujoCompleto)
                yield return Animar(0.45f, p => girador.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(p * Mathf.PI)));
            else
                yield return Animar(0.45f, p => b.anchoredPosition = new Vector2(0f, Mathf.Lerp(yCerrada, yAbierta, 1f - Mathf.Pow(1f - p, 3f))));
            girador.localScale = Vector3.one;
            Abierta = true;
            abriendo = false;
            pagina = -1;
            alAbrir?.Invoke();
        }

        // La caja se queda en el centro: la hoja de instrucciones sale centrada encima
        // (2026-11-17). Antes se corría a un costado y quedaba encima de Luis.
        private Vector2 PosicionObjetivo => cfg.posicion;

        /// <summary>El portazo: la caja tiembla en la mano.</summary>
        public void Sacudir(float amplitud)
        {
            if (Visible) StartCoroutine(UIRuntime.Sacudir(girador, amplitud));
        }

        /// <summary>
        /// Cambia el dibujo de la funda y la ajusta a su proporción: el frente es vertical y el
        /// reverso horizontal, así que no se pueden estirar al mismo rectángulo.
        /// </summary>
        private void PonerSpriteFunda(Sprite sprite)
        {
            funda.sprite = sprite;
            if (sprite == null || sprite.rect.height <= 0f) { funda.rectTransform.sizeDelta = cfg.tamano; return; }
            float asp = sprite.rect.width / sprite.rect.height;
            Vector2 caja = asp >= 1f ? new Vector2(cfg.tamano.y * 0.95f, cfg.tamano.y * 0.8f) : cfg.tamano;
            funda.rectTransform.sizeDelta = asp > caja.x / caja.y ? new Vector2(caja.x, caja.x / asp) : new Vector2(caja.y * asp, caja.y);
        }

        private void Sonar(AudioClip clip)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip);
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            var rt = (RectTransform)transform;
            Estirar(rt);
            grupo = GetComponent<CanvasGroup>();

            // Fondo: oscurece el cuarto; un clic afuera de la caja vuelve al cajón.
            fondo = gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, cfg.fondo);
            BotonPlano(gameObject, Cerrar);

            Vector2 tam = cfg.tamano;
            caja = NuevoRect("Caja", transform, cfg.posicion, tam);
            girador = NuevoRect("Girador", caja, Vector2.zero, tam);

            halo = NuevaImagen("Halo", girador, new Vector2(0f, tam.y * 0.2f), tam * 2.2f, cfg.brillo);
            halo.raycastTarget = false;
            halo.color = new Color(1f, 0.85f, 0.6f, 0f);
            if (cfg.brillo == null) halo.enabled = false;

            // La bandeja va detrás de la funda: al abrir sube y asoman los fósforos.
            Vector2 tamBandeja = new Vector2(tam.x * 546f / 607f, tam.y * 800f / 870f);
            yCerrada = tam.y * 0.5f - tam.y * 30f / 870f - tamBandeja.y * 0.5f;
            yAbierta = yCerrada + tam.y * cfg.subidaBandeja;
            bandeja = NuevaImagen("Bandeja", girador, new Vector2(0f, yCerrada), tamBandeja, cfg.bandeja);
            BotonPlano(bandeja.gameObject, Inspeccionar);

            float k = tam.x / 607f;
            fosforos = new Image[4];
            for (int i = 0; i < fosforos.Length; i++)
            {
                fosforos[i] = NuevaImagen("Fosforo" + (i + 1), bandeja.transform, PosicionFosforo(i), new Vector2(74f, 430f) * k, cfg.fosforo);
                fosforos[i].raycastTarget = false;
            }

            funda = NuevaImagen("Funda", girador, Vector2.zero, tam, cfg.frente);
            PonerSpriteFunda(cfg.frente);
            if (cfg.dibujoCompleto) bandeja.gameObject.SetActive(false);
            BotonPlano(funda.gameObject, Inspeccionar);
            funda.gameObject.AddComponent<ResaltarAlPasar>();

            // Botón Voltear, con el mismo borde a mano que el resto.
            var marco = NuevaImagen("BotonVoltear", transform, PosicionBoton(cfg.posicion), new Vector2(AnchoBoton, AltoBoton), null);
            ArteUI.PonerPapel(marco); // botón de papel: hover y presionado vienen del arte
            BotonPlano(marco.gameObject, Voltear);
            NuevoTexto("Texto", marco.transform, Vector2.zero, new Vector2(AnchoBoton, AltoBoton), 32f, FontStyles.Normal, fuente).text = TextoVoltear;
            botonVoltear = marco.gameObject;
        }

        private const float AltoBoton = 80f;
        private const float AnchoBoton = 176f; // proporción del papel: 317 x 143

        // El botón va justo debajo de la caja.
        private Vector2 PosicionBoton(Vector2 centroCaja) =>
            new Vector2(centroCaja.x, centroCaja.y - cfg.tamano.y * 0.5f - 14f - AltoBoton * 0.5f);

        private void LateUpdate()
        {
            if (botonVoltear == null) return;

            Vector2 objetivo = PosicionObjetivo;
            if ((caja.anchoredPosition - objetivo).sqrMagnitude > 0.25f)
                caja.anchoredPosition = Vector2.Lerp(caja.anchoredPosition, objetivo, 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));
            else
                caja.anchoredPosition = objetivo;

            // El botón sigue a la caja y queda arriba de la franja de subtítulos.
            ((RectTransform)botonVoltear.transform).anchoredPosition = PosicionBoton(caja.anchoredPosition);
        }
    }
}
