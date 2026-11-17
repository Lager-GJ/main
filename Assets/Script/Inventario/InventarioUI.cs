using System.Collections;
using System.Collections.Generic;
using TMPro;
using Terror.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror
{
    /// <summary>
    /// Pantalla del inventario. Se arma en runtime dentro del Canvas de la UI
    /// (ControladorGeneral), justo antes de PanelVictoria: queda encima de los primeros
    /// planos y debajo de la pausa, la victoria y la derrota.
    ///
    /// - La mochila arriba a la izquierda, que aparece con el primer objeto guardado.
    /// - Tecla I o Tab (o clic en la mochila): abre/cierra la lista. Clic en un objeto
    ///   lo muestra en primer plano (PrimerPlanoObjeto): se da vuelta y se inspecciona.
    /// Diseño del equipo (2026-11-17): la lista es una tarjeta de papel con casillas de
    /// 170 x 170. Vacía: borde punteado gris. Con objeto: borde negro de 3 px. Elegida
    /// (mouse encima): borde de 7 px y más oscura; su descripción sale abajo.
    /// Mientras está abierto, el fósforo no se gasta, Luis no camina y el tiempo del
    /// cuarto pasa lento (TiempoLento).
    ///
    /// Los objetos se USAN solos: al tocar algo cerrado, si Luis lleva lo que hace
    /// falta, se usa (ver Cerradura). El inventario es para ver lo que se lleva.
    /// </summary>
    public class InventarioUI : MonoBehaviour, ICapaEscape
    {
        [Header("Escena")]
        [Tooltip("Canvas de la UI (ControladorGeneral).")]
        [SerializeField] private RectTransform contenedorUI;
        [Tooltip("El inventario se pone justo antes de este hijo (PanelVictoria).")]
        [SerializeField] private RectTransform insertarAntesDe;
        [SerializeField] private TMP_FontAsset fuente;

        [Header("Arte")]
        [SerializeField] private Sprite iconoBolsa;
        [Tooltip("En desuso desde 2026-11-17: las casillas se dibujan en código (ArteUI).")]
        [SerializeField] private Sprite spriteRanura;
        [Tooltip("Botón > de las tarjetas (el mismo de Historia).")]
        [SerializeField] private Sprite spriteSiguiente;

        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoAbrir;
        [SerializeField] private AudioClip sonidoGuardar;
        [SerializeField] private AudioClip sonidoPagina;

        [Header("Textos")]
        [SerializeField] private string titulo = "Lo que llevo";
        [SerializeField] private string textoVacio = "Todavía no llevo nada.";
        [SerializeField] private string prefijoGuardado = "Guardaste: ";

        private const int MaxRanuras = 5;
        private const float LadoCasilla = 170f;
        private const float PasoCasillas = 205f;
        private const int RadioCasilla = 10;
        private static readonly Vector2 MargenMochila = new Vector2(36f, 30f);

        private RectTransform raiz;
        private CanvasGroup grupoBolsa;
        private RectTransform rectBolsa;
        private TextMeshProUGUI aviso;
        private GameObject panel;
        private CanvasGroup grupoPanel;
        private RectTransform filaRanuras;
        private TextMeshProUGUI etiquetaNombre;
        private TextMeshProUGUI etiquetaDescripcion;
        private TarjetaLectura tarjeta;
        private PrimerPlanoObjeto primerPlano;
        private AudioSource audioFuente;
        private Coroutine rutinaAviso;
        private bool bolsaVisible;

        public bool Abierto => panel != null && panel.activeSelf;

        public bool CapaAbierta => Abierto;

        public void CerrarCapa() => Cerrar();

        private void OnEnable()
        {
            Inventario.OnObjetoAgregado += AlGuardar;
            Inventario.OnCambiado += Rearmar;
        }

        private void OnDisable()
        {
            Inventario.OnObjetoAgregado -= AlGuardar;
            Inventario.OnCambiado -= Rearmar;
            FosforoManager.Instance?.ReanudarQuemado(this);
            TiempoLento.Soltar(this);
        }

        private void Start()
        {
            Construir();
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += AlCambiarEstado;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= AlCambiarEstado;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Terror.UI.PanelOpciones.EstaAbierto) return; // las teclas son de las opciones
            bool tecla = Controles.Pulsada(Accion.Inventario); // I (cambiable) o Tab
            if (!tecla) return;

            if (Abierto)
            {
                Cerrar();
                return;
            }
            if (PuedeAbrir()) Abrir();
        }

        private static bool PuedeAbrir()
        {
            if (Time.timeScale == 0f) return false; // pausa o un paso del tutorial
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return false;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return false;
            // El inicio del cajón y la secuencia de la lata bloquean el resto del juego.
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.CierreBloqueado) return false;
            // Con un objeto en la mano, la I no abre la bolsa por encima.
            if (PrimerPlanoObjeto.Instance != null && PrimerPlanoObjeto.Instance.Visible) return false;
            return true;
        }

        // ---------- Abrir / cerrar ----------

        public void Alternar()
        {
            if (Abierto) Cerrar();
            else if (PuedeAbrir()) Abrir();
        }

        public void Abrir()
        {
            if (panel == null || Abierto) return;
            Rearmar();
            panel.transform.SetAsLastSibling();
            if (tarjeta != null) tarjeta.transform.SetAsLastSibling();
            panel.SetActive(true);
            PanelInspeccion.Instance?.AbrirCapa(this);
            FosforoManager.Instance?.PausarQuemado(this);
            TiempoLento.Pedir(this);
            StartCoroutine(Animar(0.2f, p => grupoPanel.alpha = p));
            Sonar(sonidoAbrir);
        }

        public void Cerrar()
        {
            if (!Abierto) return;
            if (tarjeta != null) tarjeta.Cerrar();
            if (primerPlano != null) primerPlano.Cerrar();
            panel.SetActive(false);
            EncimaDe(null, false);
            PanelInspeccion.Instance?.QuitarCapa(this);
            FosforoManager.Instance?.ReanudarQuemado(this);
            TiempoLento.Soltar(this);
        }

        private void AlCambiarEstado(GameState estado)
        {
            if (estado == GameState.Juego) return;
            Cerrar();
            if (tarjeta != null) tarjeta.Cerrar();
            if (primerPlano != null) primerPlano.Cerrar();
            if (grupoBolsa != null) grupoBolsa.alpha = 0f;
        }

        // ---------- Aviso al guardar ----------

        private void AlGuardar(DefinicionObjeto def)
        {
            if (raiz == null) return;

            if (!bolsaVisible)
            {
                bolsaVisible = true;
                grupoBolsa.alpha = 1f;
                grupoBolsa.blocksRaycasts = true;
                grupoBolsa.interactable = true;
                StartCoroutine(Pop(rectBolsa, 0.45f));
            }
            else
            {
                StartCoroutine(Pop(rectBolsa, 0.3f));
            }

            Sonar(sonidoGuardar);
            // El aviso "Guardaste: ..." ya no sale (2026-11-17): al guardar algo salían tres
            // textos a la vez. Queda uno solo, abajo, en la franja de subtítulos
            // (ObjetoInventario), más la bolsa que salta y su sonido. Para volver a
            // mostrarlo: StartCoroutine(MostrarAviso(prefijoGuardado + def.nombre)).
        }

        private IEnumerator MostrarAviso(string texto)
        {
            aviso.text = texto;
            aviso.alpha = 1f;
            yield return new WaitForSecondsRealtime(2.8f);
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                aviso.alpha = 1f - t / 0.5f;
                yield return null;
            }
            aviso.alpha = 0f;
            rutinaAviso = null;
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;

            Transform padre = contenedorUI != null ? contenedorUI : transform;
            raiz = NuevoRect("UIInventario", padre, Vector2.zero, Vector2.zero);
            Estirar(raiz);
            UbicarAntesDe(raiz, padre, insertarAntesDe);

            // Mochila (arriba a la izquierda; los fósforos van arriba a la derecha)
            Vector2 tamBolsa = iconoBolsa != null && iconoBolsa.rect.height > 0f
                ? new Vector2(118f * iconoBolsa.rect.width / iconoBolsa.rect.height, 118f)
                : new Vector2(120f, 120f);
            rectBolsa = NuevoRect("BotonInventario", raiz, Vector2.zero, tamBolsa);
            rectBolsa.anchorMin = rectBolsa.anchorMax = rectBolsa.pivot = new Vector2(0f, 1f);
            rectBolsa.anchoredPosition = new Vector2(MargenMochila.x, -MargenMochila.y);
            var imgBolsa = rectBolsa.gameObject.AddComponent<Image>();
            imgBolsa.sprite = iconoBolsa;
            imgBolsa.preserveAspect = true;
            if (iconoBolsa == null) imgBolsa.color = new Color(0.4f, 0.32f, 0.22f, 1f);
            var botonBolsa = rectBolsa.gameObject.AddComponent<Button>();
            botonBolsa.onClick.AddListener(Alternar);
            grupoBolsa = rectBolsa.gameObject.AddComponent<CanvasGroup>();
            grupoBolsa.alpha = 0f;
            grupoBolsa.blocksRaycasts = false;
            grupoBolsa.interactable = false;
            var tecla = NuevoTexto("Tecla", rectBolsa, new Vector2(0f, -tamBolsa.y * 0.5f - 22f), new Vector2(160f, 40f), 26f, FontStyles.Bold, fuente);
            tecla.text = "<link=tecla>I</link>";
            tecla.gameObject.AddComponent<MarcoTeclas>();

            // Aviso "Guardaste: ..."
            aviso = NuevoTexto("AvisoGuardado", raiz, Vector2.zero, new Vector2(1000f, 60f), 36f, FontStyles.Italic, fuente);
            var ra = aviso.rectTransform;
            ra.anchorMin = ra.anchorMax = Vector2.zero;
            ra.pivot = new Vector2(0f, 0.5f);
            ra.anchoredPosition = new Vector2(185f, 285f);
            aviso.alignment = TextAlignmentOptions.MidlineLeft;
            aviso.color = new Color(0.96f, 0.9f, 0.78f);
            aviso.alpha = 0f;

            // Panel
            panel = new GameObject("PanelInventario", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(raiz, false);
            Estirar((RectTransform)panel.transform);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            grupoPanel = panel.GetComponent<CanvasGroup>();
            BotonPlano(panel, Cerrar); // clic afuera: cierra

            // La tarjeta de papel (se estira con 9-slice, sin deformar las esquinas rotas).
            var caja = NuevaImagen("Tarjeta", panel.transform, new Vector2(0f, 30f), new Vector2(1240f, 600f), ArteUI.Papel());
            caja.type = Image.Type.Sliced;
            if (caja.sprite == null) caja.color = new Color(0.92f, 0.91f, 0.88f, 1f);
            BotonPlano(caja.gameObject, null); // clic sobre la tarjeta no cierra

            var tituloTexto = NuevoTexto("Titulo", caja.transform, new Vector2(0f, 198f), new Vector2(1000f, 70f), 52f, FontStyles.Normal, fuente);
            tituloTexto.text = titulo;
            tituloTexto.color = ArteUI.Tinta;

            filaRanuras = NuevoRect("Ranuras", caja.transform, new Vector2(0f, 25f), new Vector2(PasoCasillas * MaxRanuras, LadoCasilla));

            // Debajo de las casillas: el nombre y la descripción de la elegida.
            etiquetaNombre = NuevoTexto("Nombre", caja.transform, new Vector2(0f, -130f), new Vector2(1000f, 50f), 38f, FontStyles.Normal, fuente);
            etiquetaNombre.color = ArteUI.Tinta;
            etiquetaDescripcion = NuevoTexto("Descripcion", caja.transform, new Vector2(0f, -188f), new Vector2(1000f, 80f), 30f, FontStyles.Italic, fuente);
            etiquetaDescripcion.color = new Color(ArteUI.Tinta.r, ArteUI.Tinta.g, ArteUI.Tinta.b, 0.85f);
            etiquetaDescripcion.textWrappingMode = TextWrappingModes.Normal;
            etiquetaDescripcion.enableAutoSizing = true;
            etiquetaDescripcion.fontSizeMin = 22f;
            etiquetaDescripcion.fontSizeMax = 30f;

            panel.SetActive(false);

            tarjeta = TarjetaLectura.Crear(raiz, fuente, spriteSiguiente, sonidoPagina);
            primerPlano = PrimerPlanoObjeto.Crear(raiz, fuente, sonidoAbrir, sonidoPagina);
            // Mirar en el cuarto los objetos de información (2026-10-30).
            AcercamientoObjeto.Crear(raiz, fuente, sonidoAbrir, sonidoPagina);
        }

        private void Rearmar()
        {
            if (filaRanuras == null) return;

            for (int i = filaRanuras.childCount - 1; i >= 0; i--)
                Destroy(filaRanuras.GetChild(i).gameObject);

            var inv = Inventario.Instance;
            IReadOnlyList<string> ids = inv != null ? inv.Objetos : new List<string>();
            int n = Mathf.Min(ids.Count, MaxRanuras);
            float x0 = -(MaxRanuras - 1) * PasoCasillas * 0.5f;
            Vector2 lado = new Vector2(LadoCasilla, LadoCasilla);

            // Siempre se ven las 5 casillas: las que no tienen nada, punteadas.
            for (int i = 0; i < MaxRanuras; i++)
            {
                Vector2 pos = new Vector2(x0 + i * PasoCasillas, 0f);
                if (i >= n)
                {
                    var vacia = NuevaImagen("Vacia_" + i, filaRanuras, pos, lado, ArteUI.Punteado((int)LadoCasilla, (int)LadoCasilla));
                    vacia.color = ArteUI.BordeVacio;
                    vacia.raycastTarget = false;
                    continue;
                }

                DefinicionObjeto def = inv.Definicion(ids[i]) ?? new DefinicionObjeto { id = ids[i], nombre = ids[i] };

                var fondo = NuevaImagen("Ranura_" + def.id, filaRanuras, pos, lado, ArteUI.Relleno(RadioCasilla));
                fondo.type = Image.Type.Sliced;
                fondo.color = ArteUI.Casilla;
                var borde = NuevaImagen("Borde", fondo.transform, Vector2.zero, lado, ArteUI.Borde(3, RadioCasilla));
                borde.type = Image.Type.Sliced;
                borde.color = ArteUI.Tinta;
                borde.raycastTarget = false;

                if (def.icono != null)
                {
                    var icono = NuevaImagen("Icono", fondo.transform, Vector2.zero, new Vector2(126f, 126f), def.icono);
                    icono.preserveAspect = true;
                    icono.raycastTarget = false;
                }
                else
                {
                    // Sin dibujo: el nombre del objeto, como en la hoja de diseño.
                    var nombre = NuevoTexto("Nombre", fondo.transform, Vector2.zero, lado - new Vector2(20f, 20f), 30f, FontStyles.Normal, fuente);
                    nombre.text = def.nombre;
                    nombre.color = ArteUI.Tinta;
                    nombre.textWrappingMode = TextWrappingModes.Normal;
                    nombre.enableAutoSizing = true;
                    nombre.fontSizeMin = 18f;
                    nombre.fontSizeMax = 30f;
                }

                var r = fondo.gameObject.AddComponent<RanuraInventario>();
                r.Iniciar(this, def, fondo, borde);
            }
            EncimaDe(null, false);
        }

        internal void EncimaDe(DefinicionObjeto def, bool encima)
        {
            if (etiquetaNombre == null) return;
            bool hay = encima && def != null;
            bool vacio = Inventario.Instance == null || Inventario.Instance.Objetos.Count == 0;
            etiquetaNombre.text = hay ? def.nombre : (vacio ? textoVacio : "");
            etiquetaDescripcion.text = hay && def.textos != null && def.textos.Length > 0 ? def.textos[0] : "";
        }

        internal void Mirar(DefinicionObjeto def)
        {
            if (def == null) return;
            // Los libros (la agenda) se hojean en la tarjeta: clic en la hoja derecha/izquierda.
            if (tarjeta != null && def.paginasLibro != null && def.paginasLibro.Length > 0)
            {
                tarjeta.transform.SetAsLastSibling();
                tarjeta.Mostrar(def);
                return;
            }
            if (primerPlano != null)
            {
                primerPlano.Mostrar(def);
                return;
            }
            if (tarjeta == null) return;
            tarjeta.transform.SetAsLastSibling();
            tarjeta.Mostrar(def);
        }

        private void Sonar(AudioClip clip)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Una casilla del inventario. Con el mouse encima queda elegida (borde de 7 px y más
    /// oscura) y su descripción sale abajo; al clic la muestra en primer plano.
    /// </summary>
    public class RanuraInventario : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const int Radio = 10;
        private InventarioUI dueno;
        private DefinicionObjeto def;
        private Image fondo;
        private Image borde;

        internal void Iniciar(InventarioUI inventario, DefinicionObjeto definicion, Image fondo, Image borde)
        {
            dueno = inventario;
            def = definicion;
            this.fondo = fondo;
            this.borde = borde;
            Elegir(false);
        }

        private void Elegir(bool si)
        {
            if (fondo != null) fondo.color = si ? ArteUI.CasillaElegida : ArteUI.Casilla;
            if (borde != null) borde.sprite = ArteUI.Borde(si ? 7 : 3, Radio);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            Elegir(true);
            dueno.EncimaDe(def, true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            Elegir(false);
            dueno.EncimaDe(def, false);
        }

        public void OnPointerClick(PointerEventData e) => dueno.Mirar(def);
    }
}
