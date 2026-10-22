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
    /// - Una bolsa abajo a la izquierda que aparece con el primer objeto guardado.
    /// - Aviso "Guardaste: ..." al recoger algo.
    /// - Tecla I o Tab (o clic en la bolsa): abre/cierra la lista. Clic en un objeto
    ///   abre su tarjeta (TarjetaLectura) para releer pistas.
    /// Mientras está abierto, el fósforo no se gasta y Luis no camina.
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
        [SerializeField] private string textoAyuda = "Clic en un objeto para mirarlo  ·  I o Esc para cerrar";
        [SerializeField] private string prefijoGuardado = "Guardaste: ";

        private const int MaxRanuras = 7;

        private RectTransform raiz;
        private CanvasGroup grupoBolsa;
        private RectTransform rectBolsa;
        private TextMeshProUGUI aviso;
        private GameObject panel;
        private CanvasGroup grupoPanel;
        private RectTransform filaRanuras;
        private TextMeshProUGUI etiquetaNombre;
        private TextMeshProUGUI textoSinObjetos;
        private TarjetaLectura tarjeta;
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
            bool tecla = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame;
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
            StartCoroutine(Animar(0.2f, p => grupoPanel.alpha = p));
            Sonar(sonidoAbrir);
        }

        public void Cerrar()
        {
            if (!Abierto) return;
            if (tarjeta != null) tarjeta.Cerrar();
            panel.SetActive(false);
            etiquetaNombre.text = "";
            PanelInspeccion.Instance?.QuitarCapa(this);
            FosforoManager.Instance?.ReanudarQuemado(this);
        }

        private void AlCambiarEstado(GameState estado)
        {
            if (estado == GameState.Juego) return;
            Cerrar();
            if (tarjeta != null) tarjeta.Cerrar();
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
            if (rutinaAviso != null) StopCoroutine(rutinaAviso);
            rutinaAviso = StartCoroutine(MostrarAviso(prefijoGuardado + def.nombre));
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

            // Bolsa (abajo a la izquierda, encima de la franja de narración)
            rectBolsa = NuevoRect("BotonInventario", raiz, Vector2.zero, new Vector2(120f, 120f));
            rectBolsa.anchorMin = rectBolsa.anchorMax = Vector2.zero;
            rectBolsa.anchoredPosition = new Vector2(100f, 275f);
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
            var tecla = NuevoTexto("Tecla", rectBolsa, new Vector2(0f, -78f), new Vector2(160f, 40f), 26f, FontStyles.Bold, fuente);
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
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
            grupoPanel = panel.GetComponent<CanvasGroup>();
            BotonPlano(panel, Cerrar); // clic afuera: cierra

            var caja = NuevaImagen("Caja", panel.transform, new Vector2(0f, 30f), new Vector2(1500f, 470f), null);
            caja.color = new Color(0.07f, 0.07f, 0.07f, 0.96f);
            BotonPlano(caja.gameObject, null); // clic sobre la caja no cierra
            var marco = NuevaImagen("Marco", caja.transform, Vector2.zero, new Vector2(1520f, 490f), MarcoDibujado.Obtener(128, 7f, 6f, 1f));
            marco.type = Image.Type.Sliced;
            marco.raycastTarget = false;

            NuevoTexto("Titulo", caja.transform, new Vector2(0f, 180f), new Vector2(1400f, 60f), 44f, FontStyles.Italic, fuente).text = titulo;

            filaRanuras = NuevoRect("Ranuras", caja.transform, new Vector2(0f, 10f), new Vector2(1440f, 180f));
            textoSinObjetos = NuevoTexto("Vacio", caja.transform, new Vector2(0f, 10f), new Vector2(1400f, 60f), 34f, FontStyles.Italic, fuente);
            textoSinObjetos.text = textoVacio;
            textoSinObjetos.color = new Color(1f, 1f, 1f, 0.6f);

            etiquetaNombre = NuevoTexto("Nombre", caja.transform, new Vector2(0f, -135f), new Vector2(1400f, 50f), 36f, FontStyles.Normal, fuente);
            etiquetaNombre.color = new Color(0.96f, 0.9f, 0.78f);

            var ayuda = NuevoTexto("Ayuda", caja.transform, new Vector2(0f, -200f), new Vector2(1400f, 40f), 24f, FontStyles.Italic, fuente);
            ayuda.text = textoAyuda;
            ayuda.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            panel.SetActive(false);

            tarjeta = TarjetaLectura.Crear(raiz, fuente, spriteSiguiente, sonidoPagina);
        }

        private void Rearmar()
        {
            if (filaRanuras == null) return;

            for (int i = filaRanuras.childCount - 1; i >= 0; i--)
                Destroy(filaRanuras.GetChild(i).gameObject);

            var inv = Inventario.Instance;
            IReadOnlyList<string> ids = inv != null ? inv.Objetos : new List<string>();
            textoSinObjetos.gameObject.SetActive(ids.Count == 0);

            int n = Mathf.Min(ids.Count, MaxRanuras);
            const float paso = 195f;
            float x0 = -(n - 1) * paso * 0.5f;
            for (int i = 0; i < n; i++)
            {
                DefinicionObjeto def = inv.Definicion(ids[i]) ?? new DefinicionObjeto { id = ids[i], nombre = ids[i] };

                var ranura = NuevaImagen("Ranura_" + def.id, filaRanuras, new Vector2(x0 + i * paso, 0f), new Vector2(165f, 165f), spriteRanura);
                if (spriteRanura == null) ranura.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                var icono = NuevaImagen("Icono", ranura.transform, Vector2.zero, new Vector2(125f, 125f), def.icono);
                icono.preserveAspect = true;
                icono.raycastTarget = false;
                if (def.icono == null) icono.color = new Color(0.6f, 0.6f, 0.6f, 1f);

                var r = ranura.gameObject.AddComponent<RanuraInventario>();
                r.Iniciar(this, def);
            }
        }

        internal void EncimaDe(DefinicionObjeto def, bool encima)
        {
            etiquetaNombre.text = encima && def != null ? def.nombre : "";
        }

        internal void Mirar(DefinicionObjeto def)
        {
            if (tarjeta == null || def == null) return;
            tarjeta.transform.SetAsLastSibling();
            tarjeta.Mostrar(def);
        }

        private void Sonar(AudioClip clip)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip);
        }
    }

    /// <summary>Un casillero del inventario: se agranda con el mouse y al clic abre la tarjeta.</summary>
    public class RanuraInventario : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private InventarioUI dueno;
        private DefinicionObjeto def;

        internal void Iniciar(InventarioUI inventario, DefinicionObjeto definicion)
        {
            dueno = inventario;
            def = definicion;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            transform.localScale = Vector3.one * 1.08f;
            dueno.EncimaDe(def, true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            transform.localScale = Vector3.one;
            dueno.EncimaDe(def, false);
        }

        public void OnPointerClick(PointerEventData e) => dueno.Mirar(def);
    }
}
