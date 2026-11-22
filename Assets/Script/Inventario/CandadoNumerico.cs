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
    /// Diseño del equipo (2026-11-17): una tarjeta de papel con el título arriba, 4 ruedas
    /// de 140 x 190 con el número en Jaini y flechas para subir y bajar, y la pista abajo.
    /// La rueda activa lleva borde de 6 px. Se abre solo al poner el código correcto; si
    /// no, no pasa nada. Clic en una rueda: sube un número; flechas ^ v o ruedita del mouse:
    /// sube o baja. Teclado: números, ← → ↑ ↓ y Backspace.
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
        [Tooltip("Segundo objeto que hace falta tener para saber la clave (ej. foto_campo). Vacío = solo el de arriba.")]
        [SerializeField] private string idPista2;
        [Tooltip("Marcado: no es un joyero. Hace falta tener las pistas para siquiera probar, y al acertar el objeto va directo al inventario (sin llave en el cuarto).")]
        [SerializeField] private bool entregaDirecta;
        [TextArea(1, 2)] [SerializeField] private string textoFaltanPistas = "No sé la clave...";

        [Header("Textos")]
        [Tooltip("Título de la tarjeta del candado.")]
        [SerializeField] private string titulo = "Candado";
        [TextArea(1, 2)] [SerializeField] private string textoSinPista = "Un candado de cuatro números. ¿Cuál será la clave?";
        [TextArea(1, 2)] [SerializeField] private string textoConPista = "«Clave del joyero: el año en que volvimos al campo»";
        [Tooltip("En desuso desde 2026-11-17: con un código equivocado no pasa nada.")]
        [TextArea(1, 2)] [SerializeField] private string textoError = "No... esa no es.";
        [TextArea(1, 2)] [SerializeField] private string textoAcierto = "¡Clic! El joyero se abrió.";
        [TextArea(1, 2)] [SerializeField] private string textoVacio = "El joyero ya está vacío.";
        [Tooltip("En desuso desde 2026-11-17: el candado ya no tiene línea de ayuda.")]
        [SerializeField] private string textoAyuda = "";

        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoDial;
        [Tooltip("En desuso desde 2026-11-17: con un código equivocado no pasa nada.")]
        [SerializeField] private AudioClip sonidoError;
        [SerializeField] private AudioClip sonidoAbrir;

        private GameObject panel;
        private RectTransform interior;
        private Image imagenJoyero;
        private GameObject placa;
        private TextMeshProUGUI[] digitos;
        private Image[] marcos;
        private TextMeshProUGUI pista;
        private AudioSource audioFuente;
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

            if (entregaDirecta)
            {
                if (abierto) { DecirAfuera(textoVacio); return; }
                if (!TienePistas()) { DecirAfuera(textoFaltanPistas); return; }
            }

            Construir();
            ActualizarVista();
            pi.Mostrar(panel, origenEnElCuarto != null ? origenEnElCuarto : transform);

            if (abierto && llaveTomada) Decir(textoVacio);
        }

        private bool TienePistas()
        {
            var inv = Inventario.Instance;
            if (inv == null) return false;
            if (!string.IsNullOrEmpty(idPista) && !inv.Tiene(idPista)) return false;
            if (!string.IsNullOrEmpty(idPista2) && !inv.Tiene(idPista2)) return false;
            return true;
        }

        // Un pensamiento abajo en la pantalla (el candado todavía no está abierto en pantalla).
        private void DecirAfuera(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            var cartel = CartelNarrativa.Instancia;
            cartel.Mostrar(this, texto, fuente, 3f);
            cartel.SoltarHover(this);
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
                    Refrescar();
                    Probar();
                    return;
                }
            }
            if (k.leftArrowKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) { cursor = Mathf.Max(0, cursor - 1); Refrescar(); }
            else if (k.rightArrowKey.wasPressedThisFrame) { cursor = Mathf.Min(3, cursor + 1); Refrescar(); }
            else if (k.upArrowKey.wasPressedThisFrame) Girar(cursor, 1);
            else if (k.downArrowKey.wasPressedThisFrame) Girar(cursor, -1);
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) Probar();
            else if (Mouse.current != null)
            {
                // La ruedita del mouse gira la rueda que está debajo del puntero (o la elegida).
                float giro = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(giro) > 0.01f) Girar(RuedaBajoElMouse(), giro > 0f ? 1 : -1);
            }
        }

        private int RuedaBajoElMouse()
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            for (int i = 0; i < 4; i++)
            {
                var rt = digitos[i] != null ? (RectTransform)digitos[i].transform.parent : null;
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, null)) return i;
            }
            return cursor;
        }

        private void Girar(int i, int delta)
        {
            if (abierto) return;
            cursor = i;
            valores[i] = (valores[i] + delta + 10) % 10;
            Sonar(sonidoDial);
            Refrescar();
            Probar();
        }

        // Se abre solo al quedar el código correcto; si no, no pasa nada (diseño 2026-11-17).
        private void Probar()
        {
            if (abierto) return;
            string intento = $"{valores[0]}{valores[1]}{valores[2]}{valores[3]}";
            if (intento == codigo) StartCoroutine(Abrir());
        }

        private IEnumerator Abrir()
        {
            abierto = true;
            Sonar(sonidoAbrir);
            Decir(textoAcierto);
            // Las ruedas quedan un momento a la vista con el código puesto y la tarjeta se va.
            Refrescar();
            yield return EsperarSinPausa(0.5f);
            yield return Animar(0.25f, p => placa.transform.localScale = Vector3.one * (1f - p * p));
            ActualizarVista();
            if (objetoLlave != null)
            {
                objetoLlave.gameObject.SetActive(true);
                StartCoroutine(Pop(objetoLlave.transform));
            }
            if (entregaDirecta) yield return EntregarDirecto();
        }

        // Sin joyero: el objeto se guarda solo, la vista se cierra y se muestra su tarjeta.
        private IEnumerator EntregarDirecto()
        {
            yield return EsperarSinPausa(1.2f);
            var inv = Inventario.Instance;
            if (inv != null && !string.IsNullOrEmpty(idLlave)) inv.Agregar(idLlave);
            llaveTomada = true;
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.VistaActual == panel)
                PanelInspeccion.Instance.Cerrar();
            yield return EsperarSinPausa(0.5f);
            if (inv != null && !string.IsNullOrEmpty(idLlave) && TarjetaLectura.Instance != null)
                TarjetaLectura.Instance.Mostrar(inv.Definicion(idLlave));
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

            bool conPista = entregaDirecta ? TienePistas() : (Inventario.Instance != null && Inventario.Instance.Tiene(idPista));
            pista.text = abierto ? "" : (conPista ? textoConPista : textoSinPista);
            Refrescar();
        }

        private void Refrescar()
        {
            if (digitos == null) return;
            for (int i = 0; i < 4; i++)
            {
                digitos[i].text = valores[i].ToString();
                // La rueda activa lleva borde de 6 px; las otras, de 3.
                marcos[i].sprite = ArteUI.Borde(i == cursor && !abierto ? 6 : 3, RadioRueda);
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
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f); // capa oscura del 50 % (diseño 2026-11-17)
                BotonPlano(panel, () => PanelInspeccion.Instance?.Cerrar()); // clic afuera: cierra
            }

            interior = NuevoRect("Joyero", padreJoyero, posicionJoyero, tamanoJoyero);
            if (EnCuarto) interior.localScale = Vector3.one * (((RectTransform)padreJoyero).sizeDelta.x / tamanoJoyero.x);
            imagenJoyero = interior.gameObject.AddComponent<Image>();
            imagenJoyero.sprite = joyeroCerrado;
            imagenJoyero.preserveAspect = true;
            if (joyeroCerrado == null) imagenJoyero.color = new Color(1f, 1f, 1f, 0f); // sin dibujo (el neceser): solo recibe clics
            BotonPlano(interior.gameObject, null); // un clic sobre el joyero no cierra

            // La tarjeta de papel con el candado: título, 4 ruedas y la pista abajo. Va en la
            // pantalla, también cuando el joyero se abre en el cuarto.
            var tarjeta = NuevaImagen("TarjetaCandado", panel.transform, new Vector2(0f, 20f), TamTarjeta, ArteUI.Papel());
            tarjeta.type = Image.Type.Sliced;
            if (tarjeta.sprite == null) tarjeta.color = new Color(0.92f, 0.91f, 0.88f, 1f);
            BotonPlano(tarjeta.gameObject, null); // un clic sobre la tarjeta no cierra
            placa = tarjeta.gameObject;

            var textoTitulo = NuevoTexto("Titulo", tarjeta.transform, new Vector2(0f, 205f), new Vector2(760f, 70f), 52f, FontStyles.Normal, fuente);
            textoTitulo.text = titulo;
            textoTitulo.color = ArteUI.Tinta;

            digitos = new TextMeshProUGUI[4];
            marcos = new Image[4];
            TMP_FontAsset fuenteNumeros = ArteUI.FuenteNumeros(fuente);
            for (int i = 0; i < 4; i++)
            {
                int indice = i;
                float x = (i - 1.5f) * PasoRuedas;
                var rueda = NuevaImagen("Rueda" + i, tarjeta.transform, new Vector2(x, YRuedas), TamRueda, ArteUI.Relleno(RadioRueda));
                rueda.type = Image.Type.Sliced;
                rueda.color = ArteUI.Casilla;
                // Clic en la rueda: sube un número (en la prueba del 17/11 el clic solo la
                // elegía y no se entendía cómo cambiar el número).
                BotonPlano(rueda.gameObject, () => Girar(indice, 1));
                rueda.gameObject.AddComponent<ResaltarAlPasar>();
                marcos[i] = NuevaImagen("Borde", rueda.transform, Vector2.zero, TamRueda, ArteUI.Borde(3, RadioRueda));
                marcos[i].type = Image.Type.Sliced;
                marcos[i].color = ArteUI.Tinta;
                marcos[i].raycastTarget = false;
                digitos[i] = NuevoTexto("Numero", rueda.transform, new Vector2(0f, -4f), TamRueda, 110f, FontStyles.Normal, fuenteNumeros);
                digitos[i].color = ArteUI.Tinta;

                Flecha("Arriba" + i, tarjeta.transform, x, YRuedas + TamRueda.y * 0.5f + 32f, true, () => Girar(indice, 1));
                Flecha("Abajo" + i, tarjeta.transform, x, YRuedas - TamRueda.y * 0.5f - 32f, false, () => Girar(indice, -1));
            }

            // La llave (aparece al abrir)
            // Acostada sobre el terciopelo del joyero abierto (visto un poco desde arriba).
            if (!entregaDirecta)
            {
                var imgLlave = NuevaImagen("LlavePequena", interior, new Vector2(0f, 22f), new Vector2(440f, 170f), llave);
                imgLlave.preserveAspect = false;
                if (EnCuarto) SpriteIluminado.Agregar(imgLlave, 5, 2f, materialIluminado);
                objetoLlave = imgLlave.gameObject.AddComponent<ObjetoInventario>();
                objetoLlave.Configurar(idLlave, false, null);
                objetoLlave.AlGuardarse += () => llaveTomada = true;
                imgLlave.gameObject.SetActive(false);
            }

            // La pista, abajo dentro de la tarjeta.
            pista = NuevoTexto("Pista", tarjeta.transform, new Vector2(0f, -218f), new Vector2(760f, 70f), 30f, FontStyles.Italic, fuente);
            pista.color = new Color(ArteUI.Tinta.r, ArteUI.Tinta.g, ArteUI.Tinta.b, 0.85f);
            pista.textWrappingMode = TextWrappingModes.Normal;
            pista.enableAutoSizing = true;
            pista.fontSizeMin = 22f;
            pista.fontSizeMax = 30f;

            panel.SetActive(false);
        }

        private static readonly Vector2 TamTarjeta = new Vector2(900f, 580f);
        private static readonly Vector2 TamRueda = new Vector2(140f, 190f);
        private const float PasoRuedas = 170f;
        private const float YRuedas = 0f;
        private const int RadioRueda = 10;

        // Flecha en "V" para subir o bajar una rueda. La zona de clic es más grande que el dibujo.
        private void Flecha(string nombre, Transform padre, float x, float y, bool arriba, System.Action alClic)
        {
            var zona = NuevaImagen(nombre, padre, new Vector2(x, y), new Vector2(TamRueda.x, 52f), null);
            zona.color = new Color(0f, 0f, 0f, 0f);
            BotonPlano(zona.gameObject, alClic);
            var dibujo = NuevaImagen("V", zona.transform, Vector2.zero, new Vector2(48f, 28f), ArteUI.Flecha(arriba));
            dibujo.color = ArteUI.Tinta;
            dibujo.raycastTarget = false;
            zona.gameObject.AddComponent<ResaltarAlPasar>();
        }

        // ---------- Mensajes y sonido ----------

        // Los mensajes (acierto, vacío) van a la franja de diálogos, como el resto.
        private void Decir(string texto) => DecirAfuera(texto);

        private void Sonar(AudioClip clip)
        {
            if (audioFuente != null && clip != null) audioFuente.PlayOneShot(clip);
        }
    }
}
