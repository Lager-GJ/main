using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror.UI
{
    /// <summary>
    /// Un objeto en primer plano, en la mano de Luis (pedido de David, 2026-10-28).
    /// Sirve para todos los objetos que se tocan: los del cajón del velador, la foto, la
    /// agenda, las llaves, la lata, el joyero...
    ///  - Clic en el objeto (o A / D): se da vuelta y se ve el reverso.
    ///  - Botón "Inspeccionar" abajo: lee la descripción, una página por clic, o hace la
    ///    acción del objeto (abrir la lata con la llave, el candado del joyero...).
    ///  - Esc o clic afuera: se vuelve a lo que se estaba mirando.
    ///
    /// Mientras está abierto el fósforo no se gasta y el tiempo del cuarto pasa lento,
    /// como con la tarjeta de lectura. Hay uno solo por escena (Instance); lo arma
    /// InventarioUI en runtime, encima de los primeros planos de los muebles.
    /// La caja de fósforos tiene su propia vista (VistaCajaFosforos), con el mismo manejo.
    /// </summary>
    public class PrimerPlanoObjeto : MonoBehaviour, ICapaEscape
    {
        /// <summary>Lo que se muestra.</summary>
        public class Datos
        {
            public string nombre;
            public Sprite frente;
            [Tooltip("Vacío = el objeto no se da vuelta (solo se mueve un poco).")]
            public Sprite reverso;
            [Tooltip("Descripción: una página por cada clic en Inspeccionar.")]
            public string[] textos;
            [Tooltip("Opcional: el dibujo de cada página (el reverso de la foto en la página que lo lee).")]
            public Sprite[] dibujoPorTexto;
            [Tooltip("Vacío = \"Inspeccionar\".")]
            public string textoBoton;
            [Tooltip("La interacción del objeto. Si está, Inspeccionar la hace en vez de leer.")]
            public Action accion;
            [Tooltip("Cierra el primer plano antes de la acción (para las que abren otra vista).")]
            public bool cerrarAntesDeAccion = true;
            [Tooltip("Una línea abajo del objeto (ej. \"Lo guardé en la bolsa\").")]
            public string nota;
            public Action alCerrar;
        }

        public static PrimerPlanoObjeto Instance { get; private set; }

        private static readonly Vector2 TamanoMaximo = new Vector2(640f, 500f);
        private static readonly Vector2 PosicionObjeto = new Vector2(0f, 110f);
        private const float AmpliacionMaxima = 3f;

        private const string TextoInspeccionar = "Inspeccionar";
        private const string TextoSeguir = "Seguir";
        private const string AyudaVoltear = "Clic en el objeto para darle la vuelta";
        private const string AyudaCerrar = "Esc o clic afuera para volver";

        private CanvasGroup grupo;
        private Image fondo;
        private RectTransform objeto;
        private RectTransform girador;
        private Image dibujo;
        private TextMeshProUGUI nombre;
        private TextMeshProUGUI nota;
        private TextMeshProUGUI descripcion;
        private TextMeshProUGUI ayuda;
        private GameObject boton;
        private TextMeshProUGUI textoBoton;
        private AudioSource audioFuente;
        private AudioClip sonidoVoltear;
        private AudioClip sonidoPagina;

        private Datos datos;
        private bool reverso;
        private bool girando;
        private int pagina = -1;

        public bool Visible => gameObject.activeSelf;

        public bool CapaAbierta => gameObject.activeSelf;

        public void CerrarCapa() => Cerrar();

        /// <summary>Lo arma InventarioUI dentro de 'raiz' (pantalla completa).</summary>
        public static PrimerPlanoObjeto Crear(Transform raiz, TMP_FontAsset fuente, AudioClip sonidoVoltear, AudioClip sonidoPagina)
        {
            var go = new GameObject("PrimerPlanoObjeto", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(raiz, false);
            var p = go.AddComponent<PrimerPlanoObjeto>();
            p.sonidoVoltear = sonidoVoltear;
            p.sonidoPagina = sonidoPagina;
            p.Construir(fuente);
            Instance = p;
            go.SetActive(false);
            return p;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            // Si se apaga por fuera (cambio de estado o de escena), no deja nada pausado.
            FosforoManager.Instance?.ReanudarQuemado(this);
            PanelInspeccion.Instance?.QuitarCapa(this);
            TiempoLento.Soltar(this);
        }

        // ---------- Mostrar / cerrar ----------

        /// <summary>Un objeto del inventario (con su nombre, dibujos y textos del catálogo).</summary>
        public void Mostrar(DefinicionObjeto def, string notaAbajo = null)
        {
            if (def == null) return;
            Mostrar(new Datos
            {
                nombre = def.nombre,
                frente = def.icono != null ? def.icono : def.ilustracion,
                reverso = def.reverso,
                textos = def.textos,
                dibujoPorTexto = def.dibujoPorTexto,
                nota = notaAbajo,
            });
        }

        public void Mostrar(Datos d)
        {
            if (d == null) return;
            datos = d;
            reverso = false;
            girando = false;
            pagina = -1;
            StopAllCoroutines();

            dibujo.sprite = d.frente;
            dibujo.color = d.frente != null ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
            Ajustar(d.frente);
            girador.localScale = Vector3.one;
            girador.localEulerAngles = Vector3.zero;

            nombre.text = d.nombre ?? "";
            nota.text = d.nota ?? "";
            descripcion.text = "";
            bool hayAlgo = d.accion != null || (d.textos != null && d.textos.Length > 0);
            boton.SetActive(hayAlgo);
            textoBoton.text = string.IsNullOrEmpty(d.textoBoton) ? TextoInspeccionar : d.textoBoton;
            ayuda.text = d.reverso != null ? AyudaVoltear + "   ·   " + AyudaCerrar : AyudaCerrar;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            PanelInspeccion.Instance?.AbrirCapa(this);
            FosforoManager.Instance?.PausarQuemado(this);
            TiempoLento.Pedir(this);

            grupo.alpha = 0f;
            StartCoroutine(Animar(0.25f, p =>
            {
                grupo.alpha = p;
                objeto.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f - Mathf.Pow(1f - p, 3f));
            }));
        }

        public void Cerrar()
        {
            if (!Visible) return;
            StopAllCoroutines();
            girando = false;
            gameObject.SetActive(false); // OnDisable suelta la pausa y la capa
            var accion = datos?.alCerrar;
            datos = null;
            accion?.Invoke();
        }

        /// <summary>Un mensaje en la línea de la descripción (ej. "Está cerrada").</summary>
        public void Decir(string texto)
        {
            if (Visible) descripcion.text = texto ?? "";
        }

        // El dibujo entra en el espacio disponible sin deformarse ni agrandarse demasiado
        // (los provisorios son chicos y se verían borrosos).
        private void Ajustar(Sprite s)
        {
            Vector2 tam = TamanoMaximo;
            if (s != null && s.rect.height > 0f)
            {
                Vector2 px = s.rect.size;
                float k = Mathf.Min(TamanoMaximo.x / px.x, TamanoMaximo.y / px.y, AmpliacionMaxima);
                tam = px * k;
            }
            objeto.sizeDelta = tam;
            girador.sizeDelta = tam;
            ((RectTransform)dibujo.transform).sizeDelta = tam;
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
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
                Inspeccionar();
        }

        private static bool Pausado() => PauseManager.Instance != null && PauseManager.Instance.EstaPausado;

        public void Voltear()
        {
            if (!Visible || girando || Pausado() || datos == null) return;
            StartCoroutine(datos.reverso != null ? Girar(null) : Mecer());
        }

        // Da vuelta el objeto. 'destino' = el dibujo que queda (null = el otro lado).
        private IEnumerator Girar(Sprite destino)
        {
            girando = true;
            Sonar(sonidoVoltear, 0.8f);
            yield return Animar(0.16f, p => girador.localScale = new Vector3(Mathf.Cos(p * Mathf.PI * 0.5f), 1f + 0.04f * p, 1f));
            if (destino == null)
            {
                reverso = !reverso;
                destino = reverso ? datos.reverso : datos.frente;
            }
            else reverso = destino == datos.reverso;
            dibujo.sprite = destino;
            Ajustar(destino);
            yield return Animar(0.16f, p => girador.localScale = new Vector3(Mathf.Sin(p * Mathf.PI * 0.5f), 1.04f - 0.04f * p, 1f));
            girador.localScale = Vector3.one;
            girando = false;
        }

        // Sin reverso: se mueve un poco en la mano.
        private IEnumerator Mecer()
        {
            girando = true;
            yield return Animar(0.4f, p => girador.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(p * Mathf.PI * 3f) * 6f * (1f - p)));
            girador.localEulerAngles = Vector3.zero;
            girando = false;
        }

        public void Inspeccionar()
        {
            if (!Visible || girando || Pausado() || datos == null) return;

            if (datos.accion != null)
            {
                var accion = datos.accion;
                if (datos.cerrarAntesDeAccion) Cerrar();
                accion();
                return;
            }

            string[] t = datos.textos;
            if (t == null || t.Length == 0) return;
            pagina = (pagina + 1) % t.Length;
            descripcion.text = t[pagina];
            textoBoton.text = pagina < t.Length - 1 ? TextoSeguir : (string.IsNullOrEmpty(datos.textoBoton) ? TextoInspeccionar : datos.textoBoton);
            Sonar(sonidoPagina, 0.6f);

            // La página puede pedir otro dibujo (el reverso de la foto cuando lo lee).
            Sprite s = datos.dibujoPorTexto != null && pagina < datos.dibujoPorTexto.Length ? datos.dibujoPorTexto[pagina] : null;
            if (s != null && s != dibujo.sprite)
            {
                if (s == datos.reverso || s == datos.frente) StartCoroutine(Girar(s));
                else { dibujo.sprite = s; Ajustar(s); }
            }
        }

        private void Sonar(AudioClip clip, float volumen)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip, volumen);
        }

        // ---------- Construcción ----------

        private void Construir(TMP_FontAsset fuente)
        {
            var rt = (RectTransform)transform;
            Estirar(rt);
            grupo = GetComponent<CanvasGroup>();
            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;

            // Fondo: oscurece lo de atrás; un clic afuera del objeto vuelve.
            fondo = gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.72f);
            BotonPlano(gameObject, Cerrar);

            nombre = NuevoTexto("Nombre", transform, new Vector2(0f, 420f), new Vector2(1400f, 60f), 40f, FontStyles.Italic, fuente);
            nombre.color = new Color(0.96f, 0.9f, 0.78f, 1f);

            objeto = NuevoRect("Objeto", transform, PosicionObjeto, TamanoMaximo);
            girador = NuevoRect("Girador", objeto, Vector2.zero, TamanoMaximo);
            dibujo = NuevaImagen("Dibujo", girador, Vector2.zero, TamanoMaximo, null);
            dibujo.preserveAspect = true;
            BotonPlano(dibujo.gameObject, Voltear);
            dibujo.gameObject.AddComponent<ResaltarAlPasar>();

            nota = NuevoTexto("Nota", transform, new Vector2(0f, -170f), new Vector2(1400f, 44f), 28f, FontStyles.Italic, fuente);
            nota.color = new Color(0.96f, 0.9f, 0.78f, 0.9f);

            descripcion = NuevoTexto("Descripcion", transform, new Vector2(0f, -255f), new Vector2(1500f, 120f), 40f, FontStyles.Normal, fuente);
            descripcion.textWrappingMode = TextWrappingModes.Normal;
            descripcion.enableAutoSizing = true;
            descripcion.fontSizeMin = 28f;
            descripcion.fontSizeMax = 40f;

            // Botón Inspeccionar, con el mismo borde a mano que el resto.
            var marco = NuevaImagen("BotonInspeccionar", transform, new Vector2(0f, -385f), new Vector2(300f, 72f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            marco.type = Image.Type.Sliced;
            marco.color = new Color(1f, 1f, 1f, 0.85f);
            BotonPlano(marco.gameObject, Inspeccionar);
            marco.gameObject.AddComponent<ResaltarAlPasar>();
            textoBoton = NuevoTexto("Texto", marco.transform, Vector2.zero, new Vector2(300f, 72f), 34f, FontStyles.Normal, fuente);
            boton = marco.gameObject;

            ayuda = NuevoTexto("Ayuda", transform, new Vector2(0f, -470f), new Vector2(1500f, 40f), 24f, FontStyles.Italic, fuente);
            ayuda.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        }
    }

    /// <summary>Se agranda un poco con el mouse encima (objetos y botones de los primeros planos).</summary>
    public class ResaltarAlPasar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float escala = 1.04f;

        public void OnPointerEnter(PointerEventData e) => transform.localScale = Vector3.one * escala;
        public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;
        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
