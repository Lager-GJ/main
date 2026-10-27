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
    /// La caja de fósforos "La Chispa" en primer plano (pedido de David, 2026-10-27).
    /// Al tocarla en el cajón del inicio, la caja se ve en grande:
    ///  - se puede voltear (A/D, flechas o el botón) para leer el reverso: la advertencia
    ///    y "Lea las instrucciones antes de usar";
    ///  - en el frente, un clic la abre: sale la bandeja con los fósforos. Ese es el
    ///    momento del portazo (lo hace InicioCuarto) y del tutorial "Pulsa E", que es la
    ///    hoja de instrucciones de la caja;
    ///  - la bandeja muestra los fósforos que quedan: al prender uno, sale de la caja.
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
            [Tooltip("La bandeja que sale de la funda al abrir.")]
            public Sprite bandeja;
            [Tooltip("Un fósforo (se repite por cada fósforo que queda).")]
            public Sprite fosforo;
            [Tooltip("Halo del fósforo encendido. Vacío = sin halo.")]
            public Sprite brillo;
            [Tooltip("Tamaño de la funda en pantalla.")]
            public Vector2 tamano = new Vector2(380f, 545f);
            public Vector2 posicion = new Vector2(0f, 0f);
            [Tooltip("Adónde se corre la caja al abrirla, para dejar lugar a la hoja de instrucciones.")]
            public Vector2 posicionAbierta = new Vector2(-560f, 0f);
            [Tooltip("Cuánto sube la bandeja al abrir (fracción del alto de la funda).")]
            [Range(0f, 0.6f)] public float subidaBandeja = 0.38f;
            [Tooltip("Oscurece el cuarto detrás de la caja.")]
            [Range(0f, 1f)] public float fondo = 0.6f;
            [TextArea(1, 2)] public string ayudaCerrada = "Clic en la caja para abrirla   ·   A / D para voltearla";
            [TextArea(1, 2)] public string ayudaReverso = "A / D para voltearla";
            [TextArea(1, 2)] public string ayudaAbierta = "A / D para voltearla\nEsc o clic afuera para guardarla";
            public string textoVoltear = "Voltear";
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
        private TextMeshProUGUI ayuda;
        private GameObject botonVoltear;

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
            girador.localScale = Vector3.one;
            funda.sprite = cfg.frente;
            caja.anchoredPosition = Abierta ? cfg.posicionAbierta : cfg.posicion;
            PonerTinte(tinteInicial);
            MostrarFosforos(fosforosQueQuedan);
            ActualizarAyuda();
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
            if (k.aKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame ||
                k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame)
                Voltear();
        }

        private static bool Pausado() => PauseManager.Instance != null && PauseManager.Instance.EstaPausado;

        private void ClicEnCaja()
        {
            if (Pausado() || girando || abriendo) return;
            if (reverso) { Voltear(); return; }
            if (!Abierta && (puedeAbrir == null || puedeAbrir())) StartCoroutine(Abrir());
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
            funda.sprite = reverso ? cfg.reverso : cfg.frente;
            ActualizarAyuda();
            yield return Animar(0.16f, p => girador.localScale = new Vector3(Mathf.Sin(p * Mathf.PI * 0.5f), 1.04f - 0.04f * p, 1f));
            girador.localScale = Vector3.one;
            girando = false;
        }

        private IEnumerator Abrir()
        {
            abriendo = true;
            Sonar(cfg.sonidoAbrir);
            RectTransform b = bandeja.rectTransform;
            yield return Animar(0.45f, p => b.anchoredPosition = new Vector2(0f, Mathf.Lerp(yCerrada, yAbierta, 1f - Mathf.Pow(1f - p, 3f))));
            Abierta = true;
            abriendo = false;
            ActualizarAyuda();
            alAbrir?.Invoke();

            // Se corre a un costado para dejar lugar a la hoja de instrucciones.
            Vector2 desde = caja.anchoredPosition;
            yield return EsperarSinPausa(0.35f);
            yield return Animar(0.5f, p => caja.anchoredPosition = Vector2.Lerp(desde, cfg.posicionAbierta, p * p * (3f - 2f * p)));
        }

        /// <summary>El portazo: la caja tiembla en la mano.</summary>
        public void Sacudir(float amplitud)
        {
            if (Visible) StartCoroutine(UIRuntime.Sacudir(girador, amplitud));
        }

        private void ActualizarAyuda()
        {
            if (ayuda == null) return;
            ayuda.text = reverso ? cfg.ayudaReverso : Abierta ? cfg.ayudaAbierta : cfg.ayudaCerrada;
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
            if (cfg.bandeja == null) bandeja.color = new Color(0.39f, 0.32f, 0.6f, 1f);
            BotonPlano(bandeja.gameObject, ClicEnCaja);

            float k = tam.x / 607f;
            fosforos = new Image[4];
            for (int i = 0; i < fosforos.Length; i++)
            {
                fosforos[i] = NuevaImagen("Fosforo" + (i + 1), bandeja.transform, PosicionFosforo(i), new Vector2(74f, 430f) * k, cfg.fosforo);
                fosforos[i].raycastTarget = false;
            }

            funda = NuevaImagen("Funda", girador, Vector2.zero, tam, cfg.frente);
            BotonPlano(funda.gameObject, ClicEnCaja);

            float yAbajo = cfg.posicion.y - tam.y * 0.5f;
            ayuda = NuevoTexto("Ayuda", transform, new Vector2(cfg.posicion.x, yAbajo - 50f), new Vector2(1400f, 80f), 28f, FontStyles.Italic, fuente);
            ayuda.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);

            // Botón Voltear, con el mismo borde a mano que el resto.
            var marco = NuevaImagen("BotonVoltear", transform, new Vector2(cfg.posicion.x, yAbajo - 125f), new Vector2(220f, 64f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            marco.type = Image.Type.Sliced;
            marco.color = new Color(1f, 1f, 1f, 0.85f);
            BotonPlano(marco.gameObject, Voltear);
            NuevoTexto("Texto", marco.transform, Vector2.zero, new Vector2(220f, 64f), 32f, FontStyles.Normal, fuente).text = cfg.textoVoltear;
            botonVoltear = marco.gameObject;
        }

        private void LateUpdate()
        {
            // La ayuda y el botón siguen a la caja cuando se corre a un costado.
            if (ayuda == null) return;
            float x = caja.anchoredPosition.x;
            float yAbajo = caja.anchoredPosition.y - cfg.tamano.y * 0.5f;
            ayuda.rectTransform.anchoredPosition = new Vector2(Abierta ? x : cfg.posicion.x, yAbajo - 50f);
            ayuda.rectTransform.sizeDelta = new Vector2(Abierta ? 700f : 1400f, 80f);
            ((RectTransform)botonVoltear.transform).anchoredPosition = new Vector2(x, yAbajo - 125f);
        }
    }
}
