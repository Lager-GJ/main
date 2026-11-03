using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror.UI
{
    /// <summary>
    /// Mirar un objeto de información en su lugar, en el cuarto (pedido de David,
    /// 2026-10-30): la misma transición que el joyero. Al hacer clic la cámara se acerca
    /// al objeto con cámara lenta y una luz cálida lo alumbra (como LuzVistaJoyero). Lo
    /// que se ve es el objeto del cuarto mismo (el dibujo de la pared, el del cajón): no
    /// hay ventana ni se oscurece nada; el cuarto se sigue viendo alrededor.
    ///  - Clic en el objeto (o A / D): se da vuelta; el reverso aparece en su lugar.
    ///  - Botón "Inspeccionar" abajo: lee la descripción de a una página, o hace la acción.
    ///  - Esc o clic afuera: la cámara vuelve a donde estaba (al cuarto o al mueble abierto).
    ///
    /// Sirve para los cuadros, fotos y demás objetos pintados en las paredes
    /// (ObjetoNarrable), para los ObjetoEnPrimerPlano sin acción y para los objetos del
    /// cajón del inicio. Los objetos chicos que Luis se guarda (ObjetoInventario) no pasan
    /// por acá: van directo a la bolsa. Los del inventario siguen usando PrimerPlanoObjeto,
    /// porque ya no están en el cuarto.
    ///
    /// Mientras está abierto el fósforo no se gasta y el tiempo pasa lento. Hay uno solo
    /// por escena (Instance); lo arma InventarioUI junto a PrimerPlanoObjeto.
    /// </summary>
    public class AcercamientoObjeto : MonoBehaviour, ICapaEscape
    {
        public static AcercamientoObjeto Instance { get; private set; }

        // Parte del alto y del ancho de la pantalla que ocupa el objeto. Poco, como el
        // joyero (zoom 0.25): se tiene que seguir viendo el cuarto alrededor.
        private const float AltoEnPantalla = 0.3f;
        private const float AnchoEnPantalla = 0.45f;
        private const float ZoomMinimo = 0.2f;
        private const float ZoomMaximo = 0.75f;
        // Luz sobre el objeto: la misma que LuzVistaJoyero.
        private static readonly Color ColorLuz = new Color(1f, 0.82f, 0.62f, 1f);
        private const float IntensidadLuz = 2f;
        private const float RadioLuzMinimo = 420f;
        private const float DuracionCamara = 0.45f;
        private const int OrdenEnCuarto = 7; // encima de los muebles abiertos (6)

        private const string TextoInspeccionar = "Inspeccionar";
        private const string TextoSeguir = "Seguir";
        private const string AyudaVoltear = "Clic en el objeto para darle la vuelta";
        private const string AyudaCerrar = "Esc o clic afuera para volver";

        // En la pantalla: solo los textos y el botón.
        private CanvasGroup grupo;
        private TextMeshProUGUI nombre;
        private TextMeshProUGUI descripcion;
        private TextMeshProUGUI ayuda;
        private GameObject boton;
        private TextMeshProUGUI textoBoton;
        private AudioSource audioFuente;
        private AudioClip sonidoVoltear;
        private AudioClip sonidoPagina;

        // En el cuarto: se arma en cada apertura sobre el objeto tocado.
        private RectTransform enCuarto;
        private CanvasGroup grupoCuarto;
        private RectTransform girador;
        private Image dibujo;
        private Light2D luz;

        private PrimerPlanoObjeto.Datos datos;
        private bool reverso;
        private bool girando;
        private int pagina = -1;

        // Dónde estaba la cámara antes (para volver al mueble abierto, si había uno).
        private bool habiaEnfoque;
        private Vector3 puntoPrevio;
        private float zoomPrevio;

        public bool Visible => gameObject.activeSelf;
        public bool CapaAbierta => gameObject.activeSelf;
        public void CerrarCapa() => Cerrar();

        /// <summary>Lo arma InventarioUI dentro de 'raiz' (pantalla completa, Canvas de la UI).</summary>
        public static AcercamientoObjeto Crear(Transform raiz, TMP_FontAsset fuente, AudioClip sonidoVoltear, AudioClip sonidoPagina)
        {
            var go = new GameObject("AcercamientoObjeto", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(raiz, false);
            var a = go.AddComponent<AcercamientoObjeto>();
            a.sonidoVoltear = sonidoVoltear;
            a.sonidoPagina = sonidoPagina;
            a.Construir(fuente);
            Instance = a;
            go.SetActive(false);
            return a;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (enCuarto != null) Destroy(enCuarto.gameObject);
        }

        private void OnEnable()
        {
            FosforoManager.OnFosforoApagado += Cerrar;
        }

        private void OnDisable()
        {
            FosforoManager.OnFosforoApagado -= Cerrar;
            // Si se apaga por fuera (cambio de estado o de escena), no deja nada pausado.
            FosforoManager.Instance?.ReanudarQuemado(this);
            PanelInspeccion.Instance?.QuitarCapa(this);
            TiempoLento.Soltar(this);
            if (enCuarto != null) Destroy(enCuarto.gameObject);
            enCuarto = null;
        }

        // ---------- Mostrar / cerrar ----------

        /// <summary>
        /// Se acerca a 'zona' (un objeto del Canvas del mundo) y lo muestra ahí.
        /// Devuelve false si no se puede (la zona no está en el cuarto): quien llama usa
        /// entonces la ventana de siempre (PrimerPlanoObjeto).
        /// </summary>
        public bool Mostrar(PrimerPlanoObjeto.Datos d, RectTransform zona)
        {
            if (d == null || zona == null) return false;
            var camara = CamaraController.Instance;
            var canvas = zona.GetComponentInParent<Canvas>();
            if (camara == null || canvas == null || canvas.rootCanvas.renderMode != RenderMode.WorldSpace) return false;
            if (Visible) return true;

            datos = d;
            reverso = false;
            girando = false;
            pagina = -1;
            StopAllCoroutines();

            ArmarEnCuarto(zona, canvas.rootCanvas.transform);

            nombre.text = d.nombre ?? "";
            descripcion.text = d.nota ?? "";
            bool hayAlgo = d.accion != null || (d.textos != null && d.textos.Length > 0);
            boton.SetActive(hayAlgo);
            textoBoton.text = string.IsNullOrEmpty(d.textoBoton) ? TextoInspeccionar : d.textoBoton;
            ayuda.text = d.reverso != null ? AyudaVoltear + "   ·   " + AyudaCerrar : AyudaCerrar;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            PanelInspeccion.Instance?.AbrirCapa(this);
            FosforoManager.Instance?.PausarQuemado(this);
            TiempoLento.Pedir(this);

            // La cámara va hacia el objeto; si ya miraba un mueble abierto, sale de ahí.
            habiaEnfoque = camara.Enfocada;
            puntoPrevio = camara.PuntoEnfoque;
            zoomPrevio = camara.ZoomEnfoque;
            CalcularEnfoque(girador, out Vector3 punto, out float zoom);
            camara.MoverEnfoque(punto, zoom, DuracionCamara);

            grupo.alpha = 0f;
            grupoCuarto.alpha = 0f;
            StartCoroutine(Aparecer());
            return true;
        }

        private IEnumerator Aparecer()
        {
            // El objeto aparece cuando la cámara ya casi llegó, como en PanelInspeccion.
            grupoCuarto.alpha = 1f;
            yield return Animar(DuracionCamara * 0.6f, p => AjustarLuz(p * 0.6f));
            yield return Animar(0.25f, p =>
            {
                AjustarLuz(0.6f + 0.4f * p);
                grupo.alpha = p;
            });
        }

        public void Cerrar()
        {
            if (!Visible) return;
            StopAllCoroutines();
            girando = false;

            // La cámara vuelve al mueble abierto (si sigue abierto) o al cuarto.
            var camara = CamaraController.Instance;
            if (camara != null)
            {
                bool hayVista = PanelInspeccion.Instance != null && PanelInspeccion.Instance.EstaAbierto;
                if (habiaEnfoque && hayVista) camara.MoverEnfoque(puntoPrevio, zoomPrevio, DuracionCamara);
                else camara.Soltar(DuracionCamara);
            }

            if (luz != null) luz.enabled = false;
            luz = null;

            // Lo del cuarto se desvanece solo; este objeto se apaga ya (OnDisable suelta todo).
            if (enCuarto != null)
            {
                var resto = enCuarto;
                enCuarto = null;
                resto.gameObject.AddComponent<DesvanecerYDestruir>();
            }
            gameObject.SetActive(false);
            var accion = datos?.alCerrar;
            datos = null;
            accion?.Invoke();
        }

        /// <summary>Un mensaje en la línea de la descripción.</summary>
        public void Decir(string texto)
        {
            if (Visible) descripcion.text = texto ?? "";
        }

        // Punto y zoom para que el objeto quede en la parte de arriba de la pantalla
        // (abajo van los textos y el botón).
        private static void CalcularEnfoque(RectTransform zona, out Vector3 punto, out float zoom)
        {
            var esquinas = new Vector3[4];
            zona.GetWorldCorners(esquinas);
            Vector3 centro = (esquinas[0] + esquinas[2]) * 0.5f;
            float alto = Mathf.Abs(esquinas[2].y - esquinas[0].y);
            float ancho = Mathf.Abs(esquinas[2].x - esquinas[0].x);

            var camara = CamaraController.Instance;
            var cam = camara.GetComponent<Camera>();
            float tamano = camara.TamanoBase > 0f ? camara.TamanoBase : (cam != null ? cam.orthographicSize : 540f);
            float aspecto = cam != null ? cam.aspect : 16f / 9f;

            zoom = Mathf.Max(alto / (2f * tamano * AltoEnPantalla), ancho / (2f * tamano * aspecto * AnchoEnPantalla));
            zoom = Mathf.Clamp(zoom, ZoomMinimo, ZoomMaximo);

            float altoVisible = 2f * tamano * zoom;
            punto = centro - new Vector3(0f, altoVisible * 0.12f, 0f);
        }

        // ---------- En el cuarto ----------

        private void ArmarEnCuarto(RectTransform zona, Transform canvasMundo)
        {
            if (enCuarto != null) Destroy(enCuarto.gameObject);

            enCuarto = RectSobre("Acercamiento_" + zona.name, zona, canvasMundo);
            CapaEncimaEnCuarto(enCuarto.gameObject, OrdenEnCuarto);
            grupoCuarto = enCuarto.GetComponent<CanvasGroup>();

            // Un clic afuera vuelve. Transparente: el cuarto se sigue viendo, como en el joyero.
            var afuera = NuevaImagen("TocarAfueraCierra", enCuarto, Vector2.zero, new Vector2(40000f, 40000f), null);
            afuera.color = new Color(0f, 0f, 0f, 0f);
            BotonPlano(afuera.gameObject, Cerrar);

            // Encima del objeto del cuarto, con la forma de su dibujo (cubre la zona y, si
            // está aplastada, como los objetos acostados del cajón, queda "levantado"). De
            // frente no se ve: se ve el objeto del cuarto. Al darlo vuelta aparece el reverso.
            Vector2 tam = enCuarto.sizeDelta;
            Sprite s = datos.reverso != null ? datos.reverso : datos.frente;
            if (s != null && s.rect.width > 0f && s.rect.height > 0f && tam.x > 0f && tam.y > 0f)
            {
                float k = Mathf.Max(tam.x / s.rect.width, tam.y / s.rect.height);
                tam = s.rect.size * k;
            }
            girador = NuevoRect("Girador", enCuarto, Vector2.zero, tam);
            dibujo = NuevaImagen("Dibujo", girador, Vector2.zero, tam, null);
            dibujo.preserveAspect = true;
            dibujo.color = new Color(1f, 1f, 1f, 0f); // recibe el clic, pero no se ve
            BotonPlano(dibujo.gameObject, Voltear);
            SpriteIluminado.Agregar(dibujo, OrdenEnCuarto); // lo alumbran el fósforo y la luz de abajo

            // La luz cálida sobre el objeto (como LuzVistaJoyero).
            var go = new GameObject("LuzAcercamiento");
            go.transform.SetParent(enCuarto, false);
            go.transform.position = enCuarto.TransformPoint(Vector3.zero);
            luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.color = ColorLuz;
            var esquinas = new Vector3[4];
            girador.GetWorldCorners(esquinas);
            float lado = Mathf.Max(Mathf.Abs(esquinas[2].x - esquinas[0].x), Mathf.Abs(esquinas[2].y - esquinas[0].y));
            luz.pointLightOuterRadius = Mathf.Max(RadioLuzMinimo, lado * 1.6f);
            luz.pointLightInnerRadius = luz.pointLightOuterRadius * 0.25f;
            AjustarLuz(0f);
        }

        // 0..1 de la intensidad de la luz. Como LuzVistaJoyero: solo con el fósforo encendido
        // (en el cajón del inicio alumbra la luz de la puerta).
        private void AjustarLuz(float k)
        {
            if (luz == null) return;
            bool fosforo = FosforoManager.Instance == null || FosforoManager.Instance.Encendido;
            luz.enabled = fosforo && k > 0f;
            luz.intensity = IntensidadLuz * k;
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
            if (!Visible || girando || Pausado() || datos == null || dibujo == null) return;
            if (datos.reverso == null) return; // el objeto del cuarto no se puede mover
            StartCoroutine(Girar(!reverso));
        }

        // Da vuelta el objeto. De frente se ve el objeto del cuarto; de reverso, el dibujo
        // del reverso encima, que aparece y se va "girando".
        private IEnumerator Girar(bool alReverso)
        {
            if (alReverso == reverso) yield break;
            girando = true;
            Sonar(sonidoVoltear, 0.8f);
            if (alReverso)
            {
                dibujo.sprite = datos.reverso;
                dibujo.color = Color.white;
                yield return Animar(0.25f, p => girador.localScale = new Vector3(Mathf.Sin(p * Mathf.PI * 0.5f), 1f, 1f));
            }
            else
            {
                yield return Animar(0.25f, p => girador.localScale = new Vector3(Mathf.Cos(p * Mathf.PI * 0.5f), 1f, 1f));
                dibujo.color = new Color(1f, 1f, 1f, 0f);
            }
            reverso = alReverso;
            girador.localScale = Vector3.one;
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
            if (s != null)
            {
                if (s == datos.reverso) StartCoroutine(Girar(true));
                else if (s == datos.frente) StartCoroutine(Girar(false));
                else { dibujo.sprite = s; dibujo.color = Color.white; reverso = true; }
            }
        }

        private void Sonar(AudioClip clip, float volumen)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip, volumen);
        }

        // ---------- Construcción (pantalla) ----------

        private void Construir(TMP_FontAsset fuente)
        {
            var rt = (RectTransform)transform;
            Estirar(rt);
            grupo = GetComponent<CanvasGroup>();
            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            // Sin Image de fondo: los clics pasan al cuarto (el objeto y el "afuera").

            nombre = NuevoTexto("Nombre", transform, new Vector2(0f, 440f), new Vector2(1400f, 60f), 40f, FontStyles.Italic, fuente);
            nombre.color = new Color(0.96f, 0.9f, 0.78f, 1f);
            nombre.outlineWidth = 0.15f;
            nombre.outlineColor = new Color32(0, 0, 0, 200);

            descripcion = NuevoTexto("Descripcion", transform, new Vector2(0f, -275f), new Vector2(1500f, 120f), 40f, FontStyles.Normal, fuente);
            descripcion.textWrappingMode = TextWrappingModes.Normal;
            descripcion.enableAutoSizing = true;
            descripcion.fontSizeMin = 28f;
            descripcion.fontSizeMax = 40f;
            descripcion.outlineWidth = 0.15f;
            descripcion.outlineColor = new Color32(0, 0, 0, 200);

            var marco = NuevaImagen("BotonInspeccionar", transform, new Vector2(0f, -395f), new Vector2(300f, 72f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            marco.type = Image.Type.Sliced;
            marco.color = new Color(1f, 1f, 1f, 0.85f);
            BotonPlano(marco.gameObject, Inspeccionar);
            marco.gameObject.AddComponent<ResaltarAlPasar>();
            textoBoton = NuevoTexto("Texto", marco.transform, Vector2.zero, new Vector2(300f, 72f), 34f, FontStyles.Normal, fuente);
            boton = marco.gameObject;

            ayuda = NuevoTexto("Ayuda", transform, new Vector2(0f, -475f), new Vector2(1500f, 40f), 24f, FontStyles.Italic, fuente);
            ayuda.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        }

        /// <summary>Lo que quedó en el cuarto al cerrar: se desvanece y se borra.</summary>
        private class DesvanecerYDestruir : MonoBehaviour
        {
            private IEnumerator Start()
            {
                var g = GetComponent<CanvasGroup>();
                if (g != null)
                {
                    g.blocksRaycasts = false;
                    float a0 = g.alpha;
                    yield return Animar(0.25f, p => g.alpha = a0 * (1f - p));
                }
                Destroy(gameObject);
            }
        }
    }
}
