using System.Collections;
using TMPro;
using Terror.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror
{
    /// <summary>
    /// El joyero de la abuelita, cerrado con un candado de 4 números (acertijo del
    /// cuarto 1). La clave es el año en que la familia volvió al campo: está atrás de la
    /// foto del campo, y la agenda dice que esa es la clave del joyero.
    ///
    /// Se abre desde el joyero del cuarto (Button → Mostrar) como cualquier primer plano
    /// (PanelInspeccion: zoom, cámara lenta, fósforo en pausa). Se arma en runtime.
    /// Diales con + / - o con el teclado: números, ← → ↑ ↓, Backspace y Enter.
    /// Al acertar, el joyero se abre y aparece la llave pequeña para tomarla.
    /// </summary>
    public class CandadoNumerico : MonoBehaviour
    {
        [Header("Clave")]
        [Tooltip("Tiene que ser el mismo año que dice atrás la foto del campo.")]
        [SerializeField] private string codigo = "1987";

        [Header("Escena")]
        [Tooltip("Canvas de la UI (ControladorGeneral).")]
        [SerializeField] private RectTransform contenedorUI;
        [Tooltip("El candado se pone justo después de este hijo (OSCURO), como los otros primeros planos.")]
        [SerializeField] private RectTransform insertarDespuesDe;
        [Tooltip("El joyero del cuarto: la cámara hace zoom hacia él.")]
        [SerializeField] private Transform origenEnElCuarto;
        [SerializeField] private TMP_FontAsset fuente;

        [Header("Arte")]
        [SerializeField] private Sprite joyeroCerrado;
        [SerializeField] private Sprite joyeroAbierto;
        [SerializeField] private Sprite llave;
        [SerializeField] private Vector2 tamanoJoyero = new Vector2(700f, 560f);
        [Tooltip("Centro de la placa del candado dentro del joyero (0,0 = centro).")]
        [SerializeField] private Vector2 posicionPlaca = new Vector2(0f, -110f);

        [Header("En el cuarto (cambio de perspectiva)")]
        [Tooltip("Canvas del mundo (Interactuables). Con esto asignado, la cámara se acerca al joyero de la cómoda y el candado aparece encima, sin ventana. Vacío = la vista vieja a pantalla completa.")]
        [SerializeField] private RectTransform contenedorMundo;
        [Tooltip("Tamaño de la cámara respecto del normal (0.25 = cuatro veces más cerca).")]
        [SerializeField, Range(0.1f, 1f)] private float zoom = 0.25f;
        [Tooltip("Imagen del joyero en el cuarto (el botón JoyeroB). Al abrirse el candado pasa a 'Joyero Abierto' y queda así.")]
        [SerializeField] private Image joyeroDelCuarto;
        [Tooltip("Tamaño del joyero en el cuarto, en unidades del Canvas del mundo. (0, 0) = el del botón del cuarto.")]
        [SerializeField] private Vector2 tamanoEnCuarto = Vector2.zero;
        [Tooltip("Corrimiento del joyero respecto del centro del botón del cuarto, en unidades del Canvas del mundo.")]
        [SerializeField] private Vector2 corrimientoEnCuarto = new Vector2(0f, -20f);
        [Tooltip("Vacío = el material con luz del fondo.")]
        [SerializeField] private Material materialIluminado;
        [Tooltip("Luz del fósforo sobre el joyero mientras se mira (opcional).")]
        [SerializeField] private GameObject luzVista;

        [Header("Objeto que guarda")]
        [SerializeField] private string idLlave = "llave_pequena";
        [SerializeField] private string idPista = "agenda";

        [Header("Textos")]
        [TextArea(1, 2)] [SerializeField] private string textoSinPista = "Un candado de cuatro números. ¿Cuál será la clave?";
        [TextArea(1, 2)] [SerializeField] private string textoConPista = "«Clave del joyero: el año en que volvimos al campo»";
        [TextArea(1, 2)] [SerializeField] private string textoError = "No... esa no es.";
        [TextArea(1, 2)] [SerializeField] private string textoAcierto = "¡Clic! El joyero se abrió.";
        [TextArea(1, 2)] [SerializeField] private string textoVacio = "El joyero ya está vacío.";
        [SerializeField] private string textoAyuda = "+ / - o los números del teclado  ·  Enter para probar  ·  Esc para salir";

        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoDial;
        [SerializeField] private AudioClip sonidoError;
        [SerializeField] private AudioClip sonidoAbrir;

        private GameObject panel;
        private RectTransform interior;
        private Image imagenJoyero;
        private GameObject placa;
        private TextMeshProUGUI[] digitos;
        private Image[] marcos;
        private TextMeshProUGUI pista;
        private TextMeshProUGUI mensaje;
        private AudioSource audioFuente;
        private Coroutine rutinaMensaje;
        private int[] valores = new int[4];
        private int cursor;
        private bool abierto;
        private bool llaveTomada;
        private ObjetoInventario objetoLlave;

        private bool Visible => panel != null && panel.activeSelf;
        private bool EnCuarto => contenedorMundo != null;

        // En el Input System las teclas de números no van en orden (Digit0 está después de Digit9).
        private static readonly Key[] TeclasNumero =
            { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] TeclasNumpad =
            { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        private void OnEnable() => FosforoManager.OnFosforoApagado += AlApagarse;
        private void OnDisable() => FosforoManager.OnFosforoApagado -= AlApagarse;

        // Igual que el armario: sin luz no se sigue mirando.
        private void AlApagarse()
        {
            if (Visible && PanelInspeccion.Instance != null && PanelInspeccion.Instance.VistaActual == panel)
                PanelInspeccion.Instance.Cerrar();
        }

        /// <summary>Conectar al botón del joyero en el cuarto.</summary>
        public void Mostrar()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return;
            if (FosforoManager.Instance != null && !FosforoManager.Instance.PuedeInteractuar()) return;
            var pi = PanelInspeccion.Instance;
            if (pi == null || pi.CierreBloqueado || pi.HayCapaAbierta) return;

            Construir();
            ActualizarVista();
            pi.Mostrar(panel, origenEnElCuarto != null ? origenEnElCuarto : transform);

            if (abierto && llaveTomada) Decir(textoVacio);
        }

        private void Update()
        {
            if (!Visible || abierto || Keyboard.current == null) return;
            if (Time.timeScale == 0f) return;
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.HayCapaAbierta) return;

            var k = Keyboard.current;
            for (int n = 0; n <= 9; n++)
            {
                if (k[TeclasNumero[n]].wasPressedThisFrame || k[TeclasNumpad[n]].wasPressedThisFrame)
                {
                    valores[cursor] = n;
                    Sonar(sonidoDial);
                    if (cursor < 3) cursor++;
                    else Probar();
                    Refrescar();
                    return;
                }
            }
            if (k.leftArrowKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) { cursor = Mathf.Max(0, cursor - 1); Refrescar(); }
            else if (k.rightArrowKey.wasPressedThisFrame) { cursor = Mathf.Min(3, cursor + 1); Refrescar(); }
            else if (k.upArrowKey.wasPressedThisFrame) Girar(cursor, 1);
            else if (k.downArrowKey.wasPressedThisFrame) Girar(cursor, -1);
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) Probar();
        }

        private void Girar(int i, int delta)
        {
            if (abierto) return;
            cursor = i;
            valores[i] = (valores[i] + delta + 10) % 10;
            Sonar(sonidoDial);
            Refrescar();
        }

        private void Probar()
        {
            if (abierto) return;
            string intento = $"{valores[0]}{valores[1]}{valores[2]}{valores[3]}";
            if (intento == codigo)
            {
                StartCoroutine(Abrir());
                return;
            }
            Sonar(sonidoError);
            Decir(textoError);
            StartCoroutine(Sacudir(interior, 10f));
        }

        private IEnumerator Abrir()
        {
            abierto = true;
            Sonar(sonidoAbrir);
            Decir(textoAcierto);
            yield return Animar(0.2f, p => placa.transform.localScale = Vector3.one * (1f - p));
            ActualizarVista();
            if (objetoLlave != null)
            {
                objetoLlave.gameObject.SetActive(true);
                StartCoroutine(Pop(objetoLlave.transform));
            }
        }

        private void ActualizarVista()
        {
            imagenJoyero.sprite = abierto && joyeroAbierto != null ? joyeroAbierto : joyeroCerrado;
            // En el cuarto, cerrado se ve el joyero del dibujo del fondo; abierto, el dibujo nuevo encima.
            // El joyero que se ve es el del cuarto (abierto o cerrado); el de la vista solo recibe clics.
            if (EnCuarto)
            {
                imagenJoyero.color = new Color(1f, 1f, 1f, 0f);
                if (joyeroDelCuarto != null)
                {
                    Sprite s = abierto ? joyeroAbierto : joyeroCerrado;
                    if (s != null) joyeroDelCuarto.sprite = s;
                }
            }
            placa.SetActive(!abierto);
            placa.transform.localScale = Vector3.one;
            if (objetoLlave != null) objetoLlave.gameObject.SetActive(abierto && !llaveTomada);

            bool conPista = Inventario.Instance != null && Inventario.Instance.Tiene(idPista);
            pista.text = abierto ? "" : (conPista ? textoConPista : textoSinPista);
            Refrescar();
        }

        private void Refrescar()
        {
            if (digitos == null) return;
            for (int i = 0; i < 4; i++)
            {
                digitos[i].text = valores[i].ToString();
                marcos[i].color = i == cursor ? new Color(1f, 0.85f, 0.55f, 1f) : new Color(1f, 1f, 1f, 0.45f);
            }
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            if (panel != null) return;

            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;

            Transform padre = contenedorUI != null ? contenedorUI : transform;
            panel = new GameObject("PanelJoyeroPrimerPlano", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(padre, false);
            Estirar((RectTransform)panel.transform);
            if (insertarDespuesDe != null && insertarDespuesDe.parent == padre)
                panel.transform.SetSiblingIndex(insertarDespuesDe.GetSiblingIndex() + 1);
            Transform padreJoyero = panel.transform;
            Vector2 posicionJoyero = new Vector2(0f, 40f);
            if (EnCuarto)
            {
                // Sin ventana: en la pantalla quedan solo los textos y el botón Probar. El
                // joyero, el candado y la llave van en el cuarto, encima del joyero de la cómoda.
                var fondoPanel = panel.GetComponent<Image>();
                fondoPanel.color = new Color(0f, 0f, 0f, 0f);
                fondoPanel.raycastTarget = false;

                RectTransform molde = origenEnElCuarto as RectTransform;
                RectTransform enCuarto = molde != null
                    ? RectSobre("JoyeroEnCuarto", molde, contenedorMundo)
                    : NuevoRect("JoyeroEnCuarto", contenedorMundo, Vector2.zero, Vector2.zero);
                enCuarto.anchoredPosition += corrimientoEnCuarto;
                if (tamanoEnCuarto != Vector2.zero) enCuarto.sizeDelta = tamanoEnCuarto;
                enCuarto.SetAsLastSibling();
                CapaEncimaEnCuarto(enCuarto.gameObject);

                // Clic afuera del joyero: se vuelve al cuarto.
                var afuera = NuevaImagen("TocarAfueraCierra", enCuarto, Vector2.zero, new Vector2(40000f, 40000f), null);
                afuera.color = new Color(0f, 0f, 0f, 0f);
                BotonPlano(afuera.gameObject, () => PanelInspeccion.Instance?.Cerrar());

                padreJoyero = enCuarto;
                posicionJoyero = Vector2.zero;

                var vista = panel.AddComponent<VistaEnCuarto>();
                vista.Configurar(enCuarto.gameObject, enCuarto, zoom);
                vista.Luz = luzVista;
            }
            else
            {
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f); // se ve el cuarto (con zoom) detrás
                BotonPlano(panel, () => PanelInspeccion.Instance?.Cerrar()); // clic afuera: cierra
            }

            interior = NuevoRect("Joyero", padreJoyero, posicionJoyero, tamanoJoyero);
            if (EnCuarto) interior.localScale = Vector3.one * (((RectTransform)padreJoyero).sizeDelta.x / tamanoJoyero.x);
            imagenJoyero = interior.gameObject.AddComponent<Image>();
            imagenJoyero.sprite = joyeroCerrado;
            imagenJoyero.preserveAspect = true;
            BotonPlano(interior.gameObject, null); // un clic sobre el joyero no cierra

            // Placa con los 4 diales
            placa = NuevoRect("Candado", interior, posicionPlaca, new Vector2(400f, 260f)).gameObject;
            digitos = new TextMeshProUGUI[4];
            marcos = new Image[4];
            Sprite marco = MarcoDibujado.Obtener(64, 4f, 3f, 1f);
            for (int i = 0; i < 4; i++)
            {
                int indice = i;
                float x = -150f + i * 100f;
                var fondo = NuevaImagen("Dial" + i, placa.transform, new Vector2(x, 0f), new Vector2(84f, 104f), null);
                fondo.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
                BotonPlano(fondo.gameObject, () => { cursor = indice; Refrescar(); });
                marcos[i] = NuevaImagen("Marco", fondo.transform, Vector2.zero, new Vector2(92f, 112f), marco);
                marcos[i].type = Image.Type.Sliced;
                marcos[i].raycastTarget = false;
                digitos[i] = NuevoTexto("Numero", fondo.transform, Vector2.zero, new Vector2(84f, 104f), 70f, FontStyles.Bold, fuente);
                digitos[i].color = new Color(0.95f, 0.88f, 0.7f);

                Flecha("Arriba" + i, x, 92f, "+", () => Girar(indice, 1));
                Flecha("Abajo" + i, x, -92f, "-", () => Girar(indice, -1));
            }

            var probar = NuevaImagen("Probar", panel.transform, new Vector2(0f, -330f), new Vector2(260f, 70f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            probar.type = Image.Type.Sliced;
            BotonPlano(probar.gameObject, Probar);
            NuevoTexto("Texto", probar.transform, Vector2.zero, new Vector2(260f, 70f), 34f, FontStyles.Normal, fuente).text = "Probar";

            // La llave (aparece al abrir)
            var imgLlave = NuevaImagen("LlavePequena", interior, new Vector2(0f, 20f), new Vector2(170f, 170f), llave);
            imgLlave.preserveAspect = true;
            if (EnCuarto) SpriteIluminado.Agregar(imgLlave, 5, 2f, materialIluminado);
            objetoLlave = imgLlave.gameObject.AddComponent<ObjetoInventario>();
            objetoLlave.Configurar(idLlave, false, null);
            objetoLlave.AlGuardarse += () => llaveTomada = true;
            imgLlave.gameObject.SetActive(false);

            pista = NuevoTexto("Pista", panel.transform, new Vector2(0f, 400f), new Vector2(1500f, 60f), 36f, FontStyles.Italic, fuente);
            pista.color = new Color(0.96f, 0.9f, 0.78f);
            mensaje = NuevoTexto("Mensaje", panel.transform, new Vector2(0f, -410f), new Vector2(1500f, 60f), 38f, FontStyles.Normal, fuente);
            mensaje.gameObject.SetActive(false);
            var ayuda = NuevoTexto("Ayuda", panel.transform, new Vector2(0f, -490f), new Vector2(1500f, 40f), 24f, FontStyles.Italic, fuente);
            ayuda.text = textoAyuda;
            ayuda.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            panel.SetActive(false);
        }

        private void Flecha(string nombre, float x, float y, string simbolo, System.Action alClic)
        {
            var t = NuevoTexto(nombre, placa.transform, new Vector2(x, y), new Vector2(84f, 60f), 40f, FontStyles.Normal, fuente);
            t.text = simbolo;
            t.raycastTarget = true;
            BotonPlano(t.gameObject, alClic);
        }

        // ---------- Mensajes y sonido ----------

        private void Decir(string texto)
        {
            if (mensaje == null || string.IsNullOrEmpty(texto)) return;
            if (rutinaMensaje != null) StopCoroutine(rutinaMensaje);
            rutinaMensaje = StartCoroutine(MostrarMensaje(texto));
        }

        private IEnumerator MostrarMensaje(string texto)
        {
            mensaje.text = texto;
            mensaje.alpha = 1f;
            mensaje.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.5f);
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                mensaje.alpha = 1f - t / 0.4f;
                yield return null;
            }
            mensaje.gameObject.SetActive(false);
            rutinaMensaje = null;
        }

        private void Sonar(AudioClip clip)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip);
        }
    }
}
