using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Nuevo inicio del cuarto 1 (2026-10-19). Las tarjetas de Historia terminan con Luis
    /// abriendo el cajón de la mesita, y el juego arranca justo ahí:
    ///
    /// 1. Puerta abierta: entra un poco de luz de afuera (luzPuerta) y el cajón se ve en
    ///    primer plano. Todos sus objetos se pueden tocar y muestran su mensaje.
    /// 2. Al tocar la caja de fósforos ("Fuego maldito") la puerta se cierra de golpe,
    ///    se va la luz y el cajón queda casi a oscuras. Recién ahí se pueden prender
    ///    fósforos (aparece el mensaje "Pulsa E" del tutorial de Diego).
    /// 3. Con el primer fósforo encendido aparece la nota que estaba debajo de la caja.
    ///    Desde ese momento el cajón se puede cerrar, y se reabre desde el velador
    ///    (solo con fósforo encendido, como el resto del cuarto).
    ///
    /// El cajón se arma en runtime dentro del Canvas de la UI (igual que CinematicaPuerta
    /// y SelectorDeCuartos), así que el arte, las posiciones y los textos se cambian en
    /// el Inspector de este componente sin tocar la escena. Se muestra con PanelInspeccion,
    /// así Escape, el click afuera y el resto de los sistemas lo tratan como cualquier
    /// otro primer plano (el armario, por ejemplo).
    /// </summary>
    public class InicioCuarto : MonoBehaviour
    {
        [Serializable]
        public class ElementoCajon
        {
            [Tooltip("Se muestra al pasar el mouse.")]
            public string nombre;
            public Sprite sprite;
            [Tooltip("Centro del objeto dentro del cajón, en píxeles (0,0 = centro del cajón).")]
            public Vector2 posicion;
            public Vector2 tamano = new Vector2(200f, 200f);
            public float rotacion;
            [Tooltip("Arte de la tarjeta que se abre al tocarlo (como las de Historia). Vacío = el dibujo del objeto en grande sobre una tarjeta gris provisoria.")]
            public Sprite ilustracion;
            [Tooltip("Textos de su tarjeta, en orden: el botón > pasa al siguiente y el último cierra la tarjeta.")]
            [TextArea(1, 3)] public string[] textos;
            [Tooltip("Vacío = sonidoObjeto.")]
            public AudioClip sonido;
        }

        private enum Fase { PuertaAbierta, Oscuro, Iluminado }

        [Header("Escena")]
        [Tooltip("Canvas de la UI (ControladorGeneral). El cajón se crea adentro.")]
        [SerializeField] private RectTransform contenedorUI;
        [Tooltip("El cajón se pone justo después de este hijo (OSCURO), para que la viñeta de la Presencia no lo tape y los mensajes del tutorial queden arriba.")]
        [SerializeField] private RectTransform insertarDespuesDe;
        [Tooltip("Luz que entra por la puerta abierta. Se apaga con el portazo.")]
        [SerializeField] private Light2D luzPuerta;
        [Tooltip("ObjetoRecogible de los fósforos (el de la Mesita de Diego). Al tomar la caja se llama a su Recoger(): muestra el contador de fósforos y avanza el tutorial.")]
        [SerializeField] private ObjetoRecogible fosforosRecogibles;
        [SerializeField] private TMP_FontAsset fuente;
        [Tooltip("Desmarcado: el juego arranca sin la secuencia (útil para probar el resto del cuarto).")]
        [SerializeField] private bool empezarConCajonAbierto = true;
        [Tooltip("HUD que no se ve durante el inicio. Aparece con el primer fósforo.")]
        [SerializeField] private GameObject[] ocultarHastaPrimerFosforo;

        [Header("Cajón")]
        [SerializeField] private Sprite fondoCajon;
        [SerializeField] private Vector2 tamanoCajon = new Vector2(1260f, 720f);
        [SerializeField] private Vector2 posicionCajon = new Vector2(0f, -60f);
        [SerializeField] private Sprite brilloFosforo;

        [Header("Caja de fósforos")]
        [SerializeField] private string nombreFosforos = "Fuego maldito";
        [SerializeField] private Sprite spriteFosforos;
        [SerializeField] private Vector2 posicionFosforos = new Vector2(40f, -10f);
        [SerializeField] private Vector2 tamanoFosforos = new Vector2(306f, 198f);
        [SerializeField] private float rotacionFosforos = -3f;

        [Header("Nota (debajo de los fósforos)")]
        [SerializeField] private string nombreNota = "Una nota";
        [SerializeField] private Sprite spriteNota;
        [SerializeField] private Vector2 posicionNota = new Vector2(40f, -18f);
        [SerializeField] private Vector2 tamanoNota = new Vector2(270f, 198f);
        [SerializeField] private float rotacionNota = 4f;
        [Tooltip("Arte de la tarjeta de la nota. Vacío = el dibujo de la nota en grande.")]
        [SerializeField] private Sprite ilustracionNota;
        [TextArea(1, 3)] [SerializeField] private string[] textosNota =
        {
            "Es la letra de la abuelita... (próximamente)"
        };
        [Tooltip("Para conectar el contenido real de la nota cuando exista.")]
        [SerializeField] private UnityEvent alLeerNota;

        [Header("Tarjetas (mismo concepto que Historia)")]
        [Tooltip("Borde del cajón y de las tarjetas provisorias. Vacío = borde de tiza generado en código.")]
        [SerializeField] private Sprite marcoTarjeta;
        [Tooltip("Botón > (el mismo de Historia).")]
        [SerializeField] private Sprite spriteSiguiente;
        [SerializeField] private bool cajonConMarco = true;
        [SerializeField] private Vector2 tamanoTarjeta = new Vector2(1000f, 510f);
        [SerializeField] private Vector2 posicionTarjeta = new Vector2(0f, 70f);
        [SerializeField] private Color colorTarjetaProvisoria = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Otros objetos del cajón")]
        [SerializeField] private ElementoCajon[] elementos;

        [Header("Textos")]
        [TextArea(1, 3)] [SerializeField] private string textoAlAbrir = "Aquí guarda la abuelita sus cosas... y los fósforos.";
        [TextArea(1, 3)] [SerializeField] private string textoTomarFosforos = "«Fuego maldito»... qué nombre tan raro para unos fósforos.";
        [TextArea(1, 3)] [SerializeField] private string textoPortazo = "¡PUM! La puerta se cerró sola...";
        [TextArea(1, 3)] [SerializeField] private string textoSinLuz = "Está muy oscuro. No veo nada.";
        [TextArea(1, 3)] [SerializeField] private string textoNotaAparece = "Debajo de los fósforos había una nota...";
        [SerializeField] private string textoAyudaCerrar = "Esc o clic afuera para volver al cuarto";

        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoAbrirCajon;
        [SerializeField] private AudioClip sonidoTomarFosforos;
        [SerializeField] private AudioClip sonidoPortazo;
        [SerializeField] private AudioClip sonidoRevelarNota;
        [SerializeField] private AudioClip sonidoObjeto;

        [Header("Luz del cajón")]
        [Tooltip("Color del cajón mientras entra la luz de la puerta.")]
        [SerializeField] private Color tintePuerta = new Color(0.78f, 0.82f, 0.92f, 1f);
        [Tooltip("Color del cajón a oscuras (después del portazo, sin fósforo).")]
        [SerializeField] private Color tinteOscuro = new Color(0.17f, 0.17f, 0.19f, 1f);
        [Tooltip("Color del cajón con un fósforo encendido.")]
        [SerializeField] private Color tinteFosforo = new Color(1f, 0.93f, 0.82f, 1f);
        [SerializeField, Range(0f, 1f)] private float fondoConPuertaAbierta = 0.55f;
        [SerializeField, Range(0f, 1f)] private float fondoAOscuras = 0.9f;
        [Tooltip("Segundos entre tomar los fósforos y el portazo.")]
        [SerializeField] private float esperaAntesDelPortazo = 0.7f;
        [SerializeField] private float duracionApagarLuz = 0.25f;
        [SerializeField] private float sacudidaPortazo = 22f;

        /// <summary>La puerta se cerró de golpe (fin de la parte con luz de afuera).</summary>
        public static event Action OnPuertaCerrada;

        /// <summary>El jugador leyó la nota de debajo de los fósforos.</summary>
        public static event Action OnNotaLeida;

        private Fase fase = Fase.Iluminado;
        private bool construido;

        private GameObject panel;
        private CanvasGroup grupoPanel;
        private Image fondoOscuro;
        private RectTransform interior;
        private Image imagenInterior;
        private Image luzPuertaOverlay;
        private Image brillo;
        private ZonaCajon zonaFosforos;
        private ZonaCajon zonaNota;
        private ZonaCajon[] zonasElementos;
        private int[] clicksElementos;
        private TextMeshProUGUI textoMensaje;
        private TextMeshProUGUI textoEtiqueta;
        private AudioSource audioFuente;
        private NinoController nino;

        private Color tinteActual;
        private float intensidadBasePuerta;
        private Vector2 posicionBaseInterior;
        private Coroutine rutinaTinte;
        private Coroutine rutinaMensaje;
        private Coroutine rutinaSacudida;
        private ZonaCajon zonaEncima;

        private GameObject tarjeta;
        private CanvasGroup grupoTarjeta;
        private RectTransform rectTarjeta;
        private Image fondoTarjeta;
        private Image ilustracionTarjeta;
        private Image objetoTarjeta;
        private Image bordeTarjeta;
        private TextMeshProUGUI nombreTarjeta;
        private TextMeshProUGUI textoTarjeta;
        private string[] textosTarjeta;
        private int indiceTarjeta;
        private Image marcoCajon;

        public Color Tinte => tinteActual;

        private bool PanelAbierto => panel != null && panel.activeSelf;

        private bool TarjetaAbierta => tarjeta != null && tarjeta.activeSelf;

        // ---------- Ciclo de vida ----------

        private void OnEnable()
        {
            FosforoManager.OnFosforoEncendido += AlEncenderFosforo;
            FosforoManager.OnFosforoApagado += AlApagarFosforo;
            PanelInspeccion.OnCerrado += AlCerrarseUnPrimerPlano;
        }

        private void OnDisable()
        {
            FosforoManager.OnFosforoEncendido -= AlEncenderFosforo;
            FosforoManager.OnFosforoApagado -= AlApagarFosforo;
            PanelInspeccion.OnCerrado -= AlCerrarseUnPrimerPlano;

            // Nunca dejamos el HUD oculto ni al niño quieto si esto se apaga a mitad de camino.
            AlfaHudOculto(1f);
            if (nino != null) nino.enabled = true;
        }

        private void Start()
        {
            nino = FindFirstObjectByType<NinoController>();

            if (luzPuerta != null)
            {
                intensidadBasePuerta = luzPuerta.intensity;
                luzPuerta.gameObject.SetActive(empezarConCajonAbierto);
            }

            if (empezarConCajonAbierto)
                StartCoroutine(Intro());
        }

        private IEnumerator Intro()
        {
            // Un frame: que PanelInspeccion, FosforoManager y el tutorial terminen su Start
            // (el tutorial bloquea la E hasta que se recojan los fósforos).
            yield return null;

            Construir();
            StartCoroutine(SilenciarMusicaDelMenu());
            AlfaHudOculto(0f);

            FosforoManager.Instance?.HabilitarEncendido(false);

            fase = Fase.PuertaAbierta;
            PonerTinte(tintePuerta);
            fondoOscuro.color = new Color(0f, 0f, 0f, fondoConPuertaAbierta);
            luzPuertaOverlay.color = Color.white;
            brillo.color = new Color(1f, 1f, 1f, 0f);

            Mostrar();
            if (PanelInspeccion.Instance != null)
                PanelInspeccion.Instance.CierreBloqueado = true; // hasta prender el primer fósforo

            Sonar(sonidoAbrirCajon);
            Decir(textoAlAbrir);
        }

        // ---------- API pública ----------

        /// <summary>
        /// Reabre el cajón. Conectado al botón VeladorB. Solo con un fósforo encendido,
        /// igual que el resto de los objetos del cuarto.
        /// </summary>
        public void AbrirCajon()
        {
            if (fase != Fase.Iluminado) return; // el inicio todavía no terminó
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return;
            if (FosforoManager.Instance != null && !FosforoManager.Instance.PuedeInteractuar()) return;
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.CierreBloqueado) return;

            Construir();
            PonerTinte(tinteFosforo);
            fondoOscuro.color = new Color(0f, 0f, 0f, fondoAOscuras);
            luzPuertaOverlay.color = new Color(1f, 1f, 1f, 0f);
            brillo.color = new Color(1f, 1f, 1f, 0.55f);

            Mostrar();
            Sonar(sonidoAbrirCajon);
        }

        // ---------- Clicks en el cajón ----------

        internal void ClicEnZona(ZonaCajon zona)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return;
            if (TarjetaAbierta) return;

            if (zona == zonaFosforos)
            {
                if (fase == Fase.PuertaAbierta) StartCoroutine(TomarFosforos());
                return;
            }

            if (fase == Fase.Oscuro)
            {
                Decir(textoSinLuz);
                return;
            }

            if (fase == Fase.Iluminado && FosforoManager.Instance != null && !FosforoManager.Instance.PuedeInteractuar())
                return;

            if (zona == zonaNota)
            {
                AbrirTarjeta(nombreNota, spriteNota, ilustracionNota, textosNota, sonidoObjeto);
                alLeerNota?.Invoke();
                OnNotaLeida?.Invoke();
                return;
            }

            int i = Array.IndexOf(zonasElementos, zona);
            if (i < 0) return;

            ElementoCajon e = elementos[i];
            clicksElementos[i]++;
            AbrirTarjeta(e.nombre, e.sprite, e.ilustracion, e.textos, e.sonido != null ? e.sonido : sonidoObjeto);
        }

        // ---------- Tarjeta de un objeto ----------

        private void AbrirTarjeta(string nombre, Sprite dibujo, Sprite ilustracion, string[] textos, AudioClip sonido)
        {
            if (tarjeta == null) return;

            textosTarjeta = textos != null ? textos : Array.Empty<string>();
            indiceTarjeta = 0;

            // Con arte final la ilustración es la tarjeta entera (trae su propio borde,
            // como las de Historia). Sin arte: el dibujo del objeto en grande sobre gris.
            bool conArte = ilustracion != null;
            ilustracionTarjeta.sprite = ilustracion;
            ilustracionTarjeta.enabled = conArte;
            fondoTarjeta.color = conArte ? new Color(0f, 0f, 0f, 0f) : colorTarjetaProvisoria;
            bordeTarjeta.enabled = !conArte;
            objetoTarjeta.enabled = !conArte;
            objetoTarjeta.sprite = dibujo;
            objetoTarjeta.color = dibujo != null ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
            objetoTarjeta.rectTransform.sizeDelta = dibujo != null ? tamanoTarjeta * 0.7f : new Vector2(tamanoTarjeta.y * 0.75f, tamanoTarjeta.y * 0.55f);
            nombreTarjeta.gameObject.SetActive(!conArte);
            nombreTarjeta.text = nombre;

            MostrarTextoTarjeta();
            tarjeta.SetActive(true);
            StartCoroutine(Animar(0.25f, p =>
            {
                grupoTarjeta.alpha = p;
                rectTarjeta.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, p);
            }));
            Sonar(sonido);
            ActualizarEtiqueta();
        }

        private void MostrarTextoTarjeta()
        {
            textoTarjeta.text = indiceTarjeta < textosTarjeta.Length ? textosTarjeta[indiceTarjeta] : "";
        }

        /// <summary>Botón &gt; y click sobre la tarjeta: siguiente texto; después del último, cierra.</summary>
        private void AvanzarTarjeta()
        {
            if (!TarjetaAbierta) return;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return;

            indiceTarjeta++;
            if (indiceTarjeta >= textosTarjeta.Length)
                CerrarTarjeta();
            else
                MostrarTextoTarjeta();
        }

        private void CerrarTarjeta()
        {
            if (!TarjetaAbierta) return;
            tarjeta.SetActive(false);
            ActualizarEtiqueta();
        }

        internal void EncimaDeZona(ZonaCajon zona, bool encima)
        {
            if (encima) zonaEncima = zona;
            else if (zonaEncima == zona) zonaEncima = null;
            ActualizarEtiqueta();
        }

        private IEnumerator TomarFosforos()
        {
            fase = Fase.Oscuro;
            Sonar(sonidoTomarFosforos);
            Decir(textoTomarFosforos);
            StartCoroutine(Levantar(zonaFosforos));

            yield return EsperarSinPausa(esperaAntesDelPortazo);

            // El portazo: se va la luz de afuera y el cajón queda casi a oscuras.
            Sonar(sonidoPortazo);
            Sacudir(sacudidaPortazo);
            Decir(textoPortazo);
            StartCoroutine(Animar(duracionApagarLuz, p =>
            {
                if (luzPuerta != null) luzPuerta.intensity = Mathf.Lerp(intensidadBasePuerta, 0f, p);
                luzPuertaOverlay.color = new Color(1f, 1f, 1f, 1f - p);
                fondoOscuro.color = new Color(0f, 0f, 0f, Mathf.Lerp(fondoConPuertaAbierta, fondoAOscuras, p));
            }));
            CambiarTinte(tinteOscuro, duracionApagarLuz);

            yield return EsperarSinPausa(duracionApagarLuz);
            if (luzPuerta != null) luzPuerta.gameObject.SetActive(false);

            OnPuertaCerrada?.Invoke();

            // Recién ahora los fósforos son del jugador: aparece el contador y el tutorial
            // muestra "Pulsa E". Si se recogieran antes, se podría prender uno con la puerta abierta.
            if (fosforosRecogibles != null) fosforosRecogibles.Recoger();
            FosforoManager.Instance?.HabilitarEncendido(true);
        }

        // ---------- Fósforo ----------

        private void AlEncenderFosforo()
        {
            if (!construido) return;

            CambiarTinte(tinteFosforo, 0.35f);
            StartCoroutine(Animar(0.35f, p => brillo.color = new Color(1f, 1f, 1f, 0.55f * p)));

            if (fase != Fase.Oscuro) return;

            // Primer fósforo: con luz se ve la nota que estaba debajo de la caja.
            fase = Fase.Iluminado;
            zonaNota.gameObject.SetActive(true);
            StartCoroutine(Pop(zonaNota.transform));
            Sonar(sonidoRevelarNota);
            Decir(textoNotaAparece);
            StartCoroutine(Animar(0.8f, AlfaHudOculto));

            if (PanelInspeccion.Instance != null)
                PanelInspeccion.Instance.CierreBloqueado = false;
            ActualizarEtiqueta();
        }

        private void AlApagarFosforo()
        {
            if (!construido) return;

            CambiarTinte(tinteOscuro, 0.3f);
            brillo.color = new Color(1f, 1f, 1f, 0f);

            // Igual que el armario: sin luz no se sigue mirando el cajón.
            if (fase == Fase.Iluminado && PanelAbierto && PanelInspeccion.Instance != null)
                PanelInspeccion.Instance.Cerrar();
        }

        private void AlCerrarseUnPrimerPlano()
        {
            if (tarjeta != null && panel != null && !panel.activeSelf)
                tarjeta.SetActive(false);

            if (panel != null && !panel.activeSelf && nino != null)
                nino.enabled = true;
        }

        // ---------- Mostrar ----------

        private void Mostrar()
        {
            if (PanelInspeccion.Instance != null)
                PanelInspeccion.Instance.Mostrar(panel);
            else
                panel.SetActive(true);

            // Mientras se mira el cajón, el niño no camina por el cuarto de fondo.
            if (nino != null) nino.enabled = false;

            StartCoroutine(Animar(0.3f, p =>
            {
                grupoPanel.alpha = p;
                interior.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, p);
            }));
            ActualizarEtiqueta();
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            if (construido) return;
            construido = true;

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.spatialBlend = 0f;

            Transform padre = contenedorUI != null ? contenedorUI : transform;

            panel = new GameObject("PanelCajonPrimerPlano", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            panel.transform.SetParent(padre, false);
            Estirar((RectTransform)panel.transform);
            if (insertarDespuesDe != null && insertarDespuesDe.parent == padre)
                panel.transform.SetSiblingIndex(insertarDespuesDe.GetSiblingIndex() + 1);
            grupoPanel = panel.GetComponent<CanvasGroup>();
            fondoOscuro = panel.GetComponent<Image>();
            fondoOscuro.color = new Color(0f, 0f, 0f, fondoAOscuras);
            var botonFondo = panel.GetComponent<Button>();
            botonFondo.transition = Selectable.Transition.None;
            botonFondo.onClick.AddListener(() => PanelInspeccion.Instance?.Cerrar());

            // El cajón. El Button vacío evita que un click sobre el dibujo llegue al fondo y lo cierre.
            interior = NuevoRect("CajonInterior", panel.transform, posicionCajon, tamanoCajon);
            posicionBaseInterior = interior.anchoredPosition;
            imagenInterior = interior.gameObject.AddComponent<Image>();
            imagenInterior.sprite = fondoCajon;
            if (fondoCajon == null) imagenInterior.color = new Color(0.25f, 0.23f, 0.21f, 1f);
            var botonInterior = interior.gameObject.AddComponent<Button>();
            botonInterior.transition = Selectable.Transition.None;

            // La nota va antes que los fósforos: queda debajo y aparece cuando se los lleva.
            zonaNota = NuevaZona(nombreNota, spriteNota, posicionNota, tamanoNota, rotacionNota);
            zonaNota.gameObject.SetActive(false);

            zonasElementos = new ZonaCajon[elementos != null ? elementos.Length : 0];
            clicksElementos = new int[zonasElementos.Length];
            for (int i = 0; i < zonasElementos.Length; i++)
            {
                ElementoCajon e = elementos[i];
                zonasElementos[i] = NuevaZona(e.nombre, e.sprite, e.posicion, e.tamano, e.rotacion);
            }

            zonaFosforos = NuevaZona(nombreFosforos, spriteFosforos, posicionFosforos, tamanoFosforos, rotacionFosforos);

            // Luz fría que entra desde la puerta (a la izquierda) y se pierde hacia la derecha.
            luzPuertaOverlay = NuevaImagen("LuzDeLaPuerta", interior, Vector2.zero, tamanoCajon * 1.15f, CrearDegradeHorizontal());
            luzPuertaOverlay.raycastTarget = false;

            // Halo cálido del fósforo.
            brillo = NuevaImagen("BrilloFosforo", interior, new Vector2(0f, 30f), tamanoCajon * 1.3f, brilloFosforo);
            brillo.raycastTarget = false;
            brillo.color = new Color(1f, 1f, 1f, 0f);

            textoMensaje = NuevoTexto("TextoCajon", panel.transform, new Vector2(0f, -462f), new Vector2(1500f, 90f), 40f, FontStyles.Normal);
            textoMensaje.gameObject.SetActive(false);
            textoEtiqueta = NuevoTexto("EtiquetaCajon", panel.transform, new Vector2(0f, -515f), new Vector2(1500f, 44f), 26f, FontStyles.Italic);
            textoEtiqueta.color = new Color(0.8f, 0.8f, 0.8f, 0.85f);

            // El cajón se presenta como una tarjeta de Historia: con su borde blanco de tiza.
            if (cajonConMarco)
            {
                marcoCajon = NuevaImagen("MarcoTarjeta", interior, Vector2.zero, tamanoCajon + new Vector2(30f, 30f), SpriteMarco());
                marcoCajon.type = Image.Type.Sliced;
                marcoCajon.raycastTarget = false;
            }

            ConstruirTarjeta();

            if (PanelInspeccion.Instance != null)
            {
                PanelInspeccion.Instance.SubvistaAbierta = () => TarjetaAbierta && PanelAbierto;
                PanelInspeccion.Instance.CerrarSubvista = CerrarTarjeta;
            }

            panel.SetActive(false);
        }

        private Sprite SpriteMarco() => marcoTarjeta != null ? marcoTarjeta : MarcoDibujado.Obtener(128, 7f, 6f, 1f);

        private void ConstruirTarjeta()
        {
            tarjeta = new GameObject("TarjetaObjeto", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            tarjeta.transform.SetParent(panel.transform, false);
            Estirar((RectTransform)tarjeta.transform);
            grupoTarjeta = tarjeta.GetComponent<CanvasGroup>();
            tarjeta.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            var botonFondo = tarjeta.GetComponent<Button>();
            botonFondo.transition = Selectable.Transition.None;
            botonFondo.onClick.AddListener(CerrarTarjeta); // click afuera de la tarjeta: la cierra

            rectTarjeta = NuevoRect("Tarjeta", tarjeta.transform, posicionTarjeta, tamanoTarjeta);
            fondoTarjeta = rectTarjeta.gameObject.AddComponent<Image>();
            var botonTarjeta = rectTarjeta.gameObject.AddComponent<Button>();
            botonTarjeta.transition = Selectable.Transition.None;
            botonTarjeta.onClick.AddListener(AvanzarTarjeta);

            objetoTarjeta = NuevaImagen("Dibujo", rectTarjeta, new Vector2(0f, -10f), tamanoTarjeta * 0.7f, null);
            objetoTarjeta.preserveAspect = true;
            objetoTarjeta.raycastTarget = false;

            ilustracionTarjeta = NuevaImagen("Ilustracion", rectTarjeta, Vector2.zero, tamanoTarjeta, null);
            ilustracionTarjeta.preserveAspect = true;
            ilustracionTarjeta.raycastTarget = false;

            bordeTarjeta = NuevaImagen("Borde", rectTarjeta, Vector2.zero, tamanoTarjeta + new Vector2(14f, 14f), SpriteMarco());
            bordeTarjeta.type = Image.Type.Sliced;
            bordeTarjeta.raycastTarget = false;

            nombreTarjeta = NuevoTexto("Nombre", rectTarjeta, new Vector2(0f, tamanoTarjeta.y * 0.5f - 40f), new Vector2(tamanoTarjeta.x - 60f, 50f), 30f, FontStyles.Italic);
            nombreTarjeta.color = new Color(1f, 1f, 1f, 0.75f);

            textoTarjeta = NuevoTexto("TextoTarjeta", tarjeta.transform,
                new Vector2(0f, posicionTarjeta.y - tamanoTarjeta.y * 0.5f - 100f), new Vector2(1500f, 140f), 42f, FontStyles.Normal);

            Image siguiente = NuevaImagen("BotonSiguiente", tarjeta.transform, new Vector2(800f, -420f), new Vector2(130f, 110f), spriteSiguiente);
            siguiente.preserveAspect = true;
            if (spriteSiguiente == null)
            {
                siguiente.color = new Color(0f, 0f, 0f, 0f);
                NuevoTexto("Flecha", siguiente.transform, Vector2.zero, new Vector2(130f, 110f), 80f, FontStyles.Normal).text = ">";
            }
            var botonSiguiente = siguiente.gameObject.AddComponent<Button>();
            botonSiguiente.onClick.AddListener(AvanzarTarjeta);

            tarjeta.SetActive(false);
        }

        private ZonaCajon NuevaZona(string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano, float rotacion)
        {
            Image img = NuevaImagen(string.IsNullOrEmpty(nombre) ? "Objeto" : nombre, interior, posicion, tamano, sprite);
            img.preserveAspect = true;
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotacion);

            // Sin dibujo todavía: un papel gris con borde blanco (sirve para la foto y la estampita).
            Image borde = null;
            if (sprite == null)
            {
                borde = NuevaImagen("Borde", img.transform, Vector2.zero, tamano, MarcoDibujado.Obtener(64, 4f, 3f, 1f));
                borde.type = Image.Type.Sliced;
                borde.raycastTarget = false;
            }

            var zona = img.gameObject.AddComponent<ZonaCajon>();
            zona.Iniciar(this, nombre, img, borde);
            return zona;
        }

        private static RectTransform NuevoRect(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = posicion;
            rt.sizeDelta = tamano;
            return rt;
        }

        private static Image NuevaImagen(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, Sprite sprite)
        {
            var img = NuevoRect(nombre, padre, posicion, tamano).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        private TextMeshProUGUI NuevoTexto(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, float tamanoFuente, FontStyles estilo)
        {
            var t = NuevoRect(nombre, padre, posicion, tamano).gameObject.AddComponent<TextMeshProUGUI>();
            if (fuente != null) t.font = fuente;
            t.fontSize = tamanoFuente;
            t.fontStyle = estilo;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Sprite CrearDegradeHorizontal()
        {
            const int ancho = 256;
            var tex = new Texture2D(ancho, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < ancho; x++)
            {
                float t = x / (ancho - 1f);
                float a = Mathf.Pow(1f - t, 1.8f) * 0.42f; // fuerte junto a la puerta, se pierde hacia adentro
                tex.SetPixel(x, 0, new Color(0.85f, 0.9f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, ancho, 1), new Vector2(0.5f, 0.5f));
        }

        // ---------- Tinte ----------

        private void PonerTinte(Color c)
        {
            tinteActual = c;
            AplicarTinte();
        }

        private void CambiarTinte(Color destino, float duracion)
        {
            if (rutinaTinte != null) StopCoroutine(rutinaTinte);
            Color desde = tinteActual;
            rutinaTinte = StartCoroutine(Animar(duracion, p => PonerTinte(Color.Lerp(desde, destino, p))));
        }

        private void AplicarTinte()
        {
            if (imagenInterior != null && fondoCajon != null) imagenInterior.color = tinteActual;
            if (marcoCajon != null) marcoCajon.color = tinteActual;
            zonaFosforos?.AplicarTinte();
            zonaNota?.AplicarTinte();
            if (zonasElementos != null)
                foreach (var z in zonasElementos) z?.AplicarTinte();
        }

        // ---------- Texto ----------

        private void Decir(string texto)
        {
            if (textoMensaje == null || string.IsNullOrEmpty(texto)) return;
            if (rutinaMensaje != null) StopCoroutine(rutinaMensaje);
            rutinaMensaje = StartCoroutine(MostrarMensaje(texto));
        }

        private IEnumerator MostrarMensaje(string texto)
        {
            textoMensaje.text = texto;
            textoMensaje.alpha = 1f;
            textoMensaje.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(Mathf.Clamp(texto.Length * 0.06f, 2.5f, 5f));

            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                textoMensaje.alpha = 1f - t / 0.4f;
                yield return null;
            }
            textoMensaje.gameObject.SetActive(false);
            rutinaMensaje = null;
        }

        private void ActualizarEtiqueta()
        {
            if (textoEtiqueta == null) return;

            if (zonaEncima != null && !string.IsNullOrEmpty(zonaEncima.Nombre))
                textoEtiqueta.text = zonaEncima.Nombre;
            else if (TarjetaAbierta)
                textoEtiqueta.text = "";
            else if (fase == Fase.Iluminado)
                textoEtiqueta.text = textoAyudaCerrar;
            else
                textoEtiqueta.text = "";
        }

        // ---------- Efectos ----------

        private void Sonar(AudioClip clip)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip);
        }

        private void Sacudir(float amplitud)
        {
            if (interior == null || amplitud <= 0f) return;
            if (rutinaSacudida != null) StopCoroutine(rutinaSacudida);
            rutinaSacudida = StartCoroutine(HacerSacudida(amplitud));
        }

        private IEnumerator HacerSacudida(float amplitud)
        {
            const float duracion = 0.3f;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / duracion;
                interior.anchoredPosition = posicionBaseInterior + new Vector2(
                    Mathf.Sin(t * 30f * Mathf.PI * 2f) * amplitud * k,
                    Mathf.Cos(t * 39f * Mathf.PI * 2f) * amplitud * 0.5f * k);
                yield return null;
            }
            interior.anchoredPosition = posicionBaseInterior;
            rutinaSacudida = null;
        }

        private IEnumerator Levantar(ZonaCajon zona)
        {
            var rt = (RectTransform)zona.transform;
            Vector2 p0 = rt.anchoredPosition;
            zona.Bloquear();
            yield return Animar(0.35f, p =>
            {
                rt.anchoredPosition = p0 + new Vector2(0f, 60f * p);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, p);
                zona.Imagen.color = new Color(zona.Imagen.color.r, zona.Imagen.color.g, zona.Imagen.color.b, 1f - p);
            });
            zona.gameObject.SetActive(false);
        }

        private static IEnumerator Pop(Transform objetivo)
        {
            const float duracion = 0.35f;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / duracion);
                float s = 1f + 2.70158f * Mathf.Pow(k - 1f, 3f) + 1.70158f * Mathf.Pow(k - 1f, 2f);
                objetivo.localScale = Vector3.one * Mathf.Max(0.01f, s);
                yield return null;
            }
            objetivo.localScale = Vector3.one;
        }

        // Como WaitForSecondsRealtime, pero el reloj se detiene con el menú de pausa
        // (timeScale 0): así el portazo no ocurre por debajo de la pausa.
        private static IEnumerator EsperarSinPausa(float segundos)
        {
            for (float t = 0f; t < segundos; )
            {
                if (Time.timeScale > 0f) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator Animar(float duracion, Action<float> paso)
        {
            if (duracion <= 0f) { paso(1f); yield break; }
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                paso(t / duracion);
                yield return null;
            }
            paso(1f);
        }

        // Con CanvasGroup y no SetActive: lo oculto sigue escuchando sus eventos.
        private void AlfaHudOculto(float alfa)
        {
            if (ocultarHastaPrimerFosforo == null) return;
            foreach (var go in ocultarHastaPrimerFosforo)
            {
                if (go == null) continue;
                var grupo = go.GetComponent<CanvasGroup>();
                if (grupo == null) grupo = go.AddComponent<CanvasGroup>();
                grupo.alpha = alfa;
            }
        }

        // La música del menú (PersistentAudio) llegaba hasta acá; antes la cortaba la cinemática.
        private IEnumerator SilenciarMusicaDelMenu()
        {
            PersistentAudio musica = FindFirstObjectByType<PersistentAudio>();
            AudioSource src = musica != null ? musica.GetComponent<AudioSource>() : null;
            if (src == null) yield break;

            float v0 = src.volume;
            yield return Animar(1f, p => src.volume = Mathf.Lerp(v0, 0f, p));
            Destroy(musica.gameObject);
        }
    }

    /// <summary>
    /// Un objeto tocable dentro del cajón del inicio. Se crea en runtime desde InicioCuarto.
    /// Se ilumina un poco con el mouse encima y avisa los clicks a InicioCuarto.
    /// </summary>
    public class ZonaCajon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private InicioCuarto dueno;
        private bool encima;
        private bool bloqueada;

        private Image borde;

        public string Nombre { get; private set; }
        public Image Imagen { get; private set; }

        internal void Iniciar(InicioCuarto inicio, string nombre, Image imagen, Image bordeSinDibujo)
        {
            dueno = inicio;
            Nombre = nombre;
            Imagen = imagen;
            borde = bordeSinDibujo;
            AplicarTinte();
        }

        internal void Bloquear()
        {
            bloqueada = true;
            Imagen.raycastTarget = false;
            if (encima) dueno.EncimaDeZona(this, false);
            encima = false;
        }

        internal void AplicarTinte()
        {
            if (Imagen == null || bloqueada) return;
            Color c = dueno.Tinte;
            float f = encima ? 1.3f : 1f;
            // Sin dibujo, la zona es un papel gris: se tiñe igual para que no brille en la oscuridad.
            float baseGris = Imagen.sprite == null ? 0.62f : 1f;
            Imagen.color = new Color(Mathf.Min(1f, c.r * f * baseGris), Mathf.Min(1f, c.g * f * baseGris), Mathf.Min(1f, c.b * f * baseGris), 1f);
            if (borde != null)
                borde.color = new Color(Mathf.Min(1f, c.r * f), Mathf.Min(1f, c.g * f), Mathf.Min(1f, c.b * f), 1f);
        }

        private void OnDisable()
        {
            if (encima && dueno != null) dueno.EncimaDeZona(this, false);
            encima = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (bloqueada) return;
            encima = true;
            transform.localScale = Vector3.one * 1.04f;
            AplicarTinte();
            dueno.EncimaDeZona(this, true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (bloqueada) return;
            encima = false;
            transform.localScale = Vector3.one;
            AplicarTinte();
            dueno.EncimaDeZona(this, false);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!bloqueada) dueno.ClicEnZona(this);
        }
    }
}
