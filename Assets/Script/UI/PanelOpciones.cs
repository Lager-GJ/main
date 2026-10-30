using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Pantalla de Opciones, armada en código (no hay que poner nada en la escena).
    /// Se abre desde la pausa (PauseManager.AbrirConfiguracion) y desde el menú
    /// principal (Scriptcambio.Opciones) con PanelOpciones.Abrir().
    ///
    /// Cuatro pestañas:
    ///  - Audio: volumen general, música y ambiente, efectos.
    ///  - Imagen: brillo, contraste, pantalla completa.
    ///  - Controles: cambiar las teclas (clic en la tecla y apretar la nueva).
    ///  - Accesibilidad: sacudidas, reducir destellos, resaltado de objetos.
    ///
    /// Los cambios se ven al instante y se guardan al cerrar. Escape vuelve (o cancela
    /// la tecla que se está esperando). Funciona con el juego en pausa (timeScale 0) y
    /// se puede usar con el teclado: flechas para moverse, Enter para elegir.
    /// </summary>
    public class PanelOpciones : MonoBehaviour
    {
        // ---------- Colores y medidas (pantalla de referencia 1920×1080) ----------
        private static readonly Color ColFondo = new Color(0f, 0f, 0f, 0.82f);
        private static readonly Color ColVentana = new Color(0.065f, 0.055f, 0.05f, 0.98f);
        private static readonly Color ColBorde = new Color(0.55f, 0.42f, 0.30f, 0.6f);
        private static readonly Color ColTexto = new Color(0.93f, 0.89f, 0.81f, 1f);
        private static readonly Color ColTenue = new Color(0.66f, 0.61f, 0.55f, 1f);
        private static readonly Color ColAcento = new Color(0.93f, 0.52f, 0.22f, 1f);
        private static readonly Color ColBoton = new Color(0.15f, 0.13f, 0.12f, 1f);
        private static readonly Color ColBotonActivo = new Color(0.42f, 0.13f, 0.08f, 1f);
        private static readonly Color ColPista = new Color(0.25f, 0.22f, 0.20f, 1f);

        private const float AnchoVentana = 1280f, AltoVentana = 860f;
        private const float AltoFila = 90f;
        private const float XEtiqueta = -580f, AnchoEtiqueta = 560f;
        private const float XControl = 250f, AnchoControl = 520f;

        private enum Pestana { Audio, Imagen, Controles, Accesibilidad }

        private static PanelOpciones instancia;

        /// <summary>¿Está abierto ahora? (la pausa no debe reaccionar a Escape/P mientras tanto).</summary>
        public static bool EstaAbierto => instancia != null && instancia.abierto;

        /// <summary>Frame en que se cerró: ese mismo Escape no tiene que llegar a la pausa.</summary>
        public static int FrameCierre { get; private set; } = -1;

        private bool abierto;
        private Action alCerrar;
        private TMP_FontAsset fuente;
        private Sprite spriteCirculo;

        private RectTransform ventana;
        private RectTransform contenido;
        private TextMeshProUGUI textoAyuda;
        private TextMeshProUGUI textoRestablecer;
        private Button botonVolver;
        private readonly Dictionary<Pestana, Button> botonesPestana = new Dictionary<Pestana, Button>();
        private Pestana pestanaActual = Pestana.Audio;
        private readonly List<Action> refrescos = new List<Action>();

        // Cambio de tecla en curso.
        private Accion? esperandoTecla;
        private int frameInicioEspera;
        private Button botonEsperando;

        // ---------- API ----------

        /// <summary>Abre las opciones. 'alCerrar' se llama al volver (por ejemplo, para mostrar la pausa otra vez).</summary>
        public static void Abrir(Action alCerrar = null)
        {
            if (instancia == null)
            {
                var go = new GameObject("[PanelOpciones]");
                instancia = go.AddComponent<PanelOpciones>();
                instancia.Construir();
            }
            instancia.Mostrar(alCerrar);
        }

        public static void CerrarSiEstaAbierto()
        {
            if (EstaAbierto) instancia.Cerrar();
        }

        private void Mostrar(Action cerrar)
        {
            alCerrar = cerrar;
            abierto = true;
            gameObject.SetActive(true);
            AsegurarEventSystem();
            MostrarPestana(pestanaActual);
        }

        public void Cerrar()
        {
            if (!abierto) return;
            CancelarEspera();
            abierto = false;
            FrameCierre = Time.frameCount;
            Preferencias.Guardar();
            gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            var cb = alCerrar;
            alCerrar = null;
            cb?.Invoke();
        }

        private void OnDestroy()
        {
            if (instancia == this) instancia = null;
            Preferencias.OnCambio -= Refrescar;
            if (abierto) Preferencias.Guardar();
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (!abierto || k == null) return;

            if (esperandoTecla.HasValue)
            {
                EsperarTecla(k);
                return;
            }

            if (k.escapeKey.wasPressedThisFrame)
            {
                Cerrar();
                return;
            }

            // Si nada está seleccionado, la primera flecha selecciona algo (para jugar sin mouse).
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject == null &&
                (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame ||
                 k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame))
                es.SetSelectedGameObject(botonesPestana[pestanaActual].gameObject);
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            fuente = BuscarFuente();
            spriteCirculo = CrearCirculo();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // encima de la pausa y de todo el HUD
            var escalador = gameObject.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Fondo oscuro que tapa el juego y frena los clics.
            var fondo = UIRuntime.NuevaImagen("Fondo", transform, Vector2.zero, Vector2.zero, null);
            UIRuntime.Estirar(fondo.rectTransform);
            fondo.color = ColFondo;

            var imgVentana = UIRuntime.NuevaImagen("Ventana", transform, Vector2.zero, new Vector2(AnchoVentana, AltoVentana), null);
            imgVentana.color = ColVentana;
            var borde = imgVentana.gameObject.AddComponent<Outline>();
            borde.effectColor = ColBorde;
            borde.effectDistance = new Vector2(2f, -2f);
            ventana = imgVentana.rectTransform;

            var titulo = Texto("Titulo", ventana, new Vector2(0f, 370f), new Vector2(800f, 80f), 58f, "OPCIONES");
            titulo.fontStyle = FontStyles.Bold;
            titulo.characterSpacing = 8f;

            // Pestañas.
            string[] nombres = { "AUDIO", "IMAGEN", "CONTROLES", "ACCESIBILIDAD" };
            const float anchoPestana = 290f, separacion = 12f;
            float x0 = -(anchoPestana * 4 + separacion * 3) * 0.5f + anchoPestana * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                var p = (Pestana)i;
                var b = Boton("Pestana" + nombres[i], ventana, new Vector2(x0 + i * (anchoPestana + separacion), 285f),
                    new Vector2(anchoPestana, 62f), nombres[i], 30f, () => MostrarPestana(p));
                botonesPestana[p] = b;
            }

            var linea = UIRuntime.NuevaImagen("Linea", ventana, new Vector2(0f, 245f), new Vector2(AnchoVentana - 80f, 2f), null);
            linea.color = ColBorde;

            contenido = UIRuntime.NuevoRect("Contenido", ventana, new Vector2(0f, -20f), new Vector2(AnchoVentana - 80f, 500f));

            // Ayuda: explica la opción que está bajo el mouse o seleccionada.
            textoAyuda = Texto("Ayuda", ventana, new Vector2(0f, -292f), new Vector2(AnchoVentana - 120f, 76f), 26f, "");
            textoAyuda.color = ColTenue;
            textoAyuda.fontStyle = FontStyles.Italic;
            textoAyuda.textWrappingMode = TextWrappingModes.Normal;

            var restablecer = Boton("Restablecer", ventana, new Vector2(-330f, -375f), new Vector2(420f, 64f),
                "Restablecer", 30f, Restablecer);
            textoRestablecer = restablecer.GetComponentInChildren<TextMeshProUGUI>();
            botonVolver = Boton("Volver", ventana, new Vector2(330f, -375f), new Vector2(420f, 64f),
                "Volver  (Esc)", 30f, Cerrar);

            Preferencias.OnCambio += Refrescar;
        }

        private void MostrarPestana(Pestana p)
        {
            CancelarEspera();
            pestanaActual = p;
            refrescos.Clear();
            for (int i = contenido.childCount - 1; i >= 0; i--)
                Destroy(contenido.GetChild(i).gameObject);

            foreach (var par in botonesPestana)
                Pintar(par.Value, par.Key == p ? ColBotonActivo : ColBoton);

            float y = 210f;
            switch (p)
            {
                case Pestana.Audio:
                    y = FilaDeslizador(y, "Volumen general", "Afecta a todo el sonido del juego.",
                        () => Preferencias.VolMaster, v => Preferencias.VolMaster = v, 0f, 1f, Porcentaje);
                    y = FilaDeslizador(y, "Música y ambiente", "La música del menú y el ambiente del cuarto (grillos, latidos).",
                        () => Preferencias.VolMusica, v => Preferencias.VolMusica = v, 0f, 1f, Porcentaje);
                    y = FilaDeslizador(y, "Efectos", "Fósforos, puertas, cajones, objetos, voces y sustos.",
                        () => Preferencias.VolSfx, v => Preferencias.VolSfx = v, 0f, 1f, Porcentaje);
                    textoRestablecer.text = "Restablecer audio";
                    break;

                case Pestana.Imagen:
                    y = FilaDeslizador(y, "Brillo", "Si el cuarto se ve demasiado negro en tu pantalla, súbelo un poco. Los menús y textos no cambian.",
                        () => Preferencias.Brillo, v => Preferencias.Brillo = v, -1f, 1f, ConSigno);
                    y = FilaDeslizador(y, "Contraste", "Más contraste separa mejor las luces de las sombras.",
                        () => Preferencias.Contraste, v => Preferencias.Contraste = v, -1f, 1f, ConSigno);
#if !UNITY_EDITOR
                    y = FilaOpciones(y, "Pantalla completa", "Usa toda la pantalla. En el navegador también se sale con Esc.",
                        new[] { "No", "Sí" }, () => Screen.fullScreen ? 1 : 0, i => Screen.fullScreen = i == 1);
#endif
                    textoRestablecer.text = "Restablecer imagen";
                    break;

                case Pestana.Controles:
                    y += 10f;
                    foreach (Accion a in Controles.Todas)
                        y = FilaTecla(y, a);
                    var fijas = Texto("Fijas", contenido, new Vector2(0f, y - 30f), new Vector2(AnchoVentana - 120f, 90f), 25f,
                        "Clic: caminar y tocar objetos     ·     Enter: inspeccionar     ·     Esc: volver / pausa\n" +
                        "Flechas: caminar     ·     Tab: bolsa     ·     números: candado");
                    fijas.color = ColTenue;
                    textoRestablecer.text = "Restablecer teclas";
                    break;

                case Pestana.Accesibilidad:
                    y = FilaDeslizador(y, "Sacudidas de la cámara", "Temblores de la cámara, portazos y muebles. En 0% no se mueve nada.",
                        () => Preferencias.FactorSacudidas, v => Preferencias.FactorSacudidas = v, 0f, 1f, Porcentaje);
                    y = FilaOpciones(y, "Reducir destellos", "Menos parpadeo de la llama, la tele y la estática. Recomendado si eres sensible a las luces intermitentes.",
                        new[] { "No", "Sí" }, () => Preferencias.ReducirDestellos ? 1 : 0, i => Preferencias.ReducirDestellos = i == 1);
                    y = FilaOpciones(y, "Resaltar objetos", "El borde que aparece al pasar el mouse sobre lo que se puede tocar. «Alto contraste» lo hace más grueso y claro.",
                        new[] { "Apagado", "Normal", "Alto contraste" }, () => Preferencias.ResaltadoObjetos, i => Preferencias.ResaltadoObjetos = i);
                    textoRestablecer.text = "Restablecer accesibilidad";
                    break;
            }

            textoAyuda.text = "";
            Refrescar();

            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null && !es.currentSelectedGameObject.activeInHierarchy)
                es.SetSelectedGameObject(null);
        }

        private void Refrescar()
        {
            foreach (var r in refrescos) r();
        }

        private void Restablecer()
        {
            switch (pestanaActual)
            {
                case Pestana.Controles:
                    Controles.Restablecer();
                    break;
                case Pestana.Audio:
                    Preferencias.VolMaster = Preferencias.VolMusica = Preferencias.VolSfx = 1f;
                    break;
                case Pestana.Imagen:
                    Preferencias.Brillo = 0f;
                    Preferencias.Contraste = 0f;
                    break;
                case Pestana.Accesibilidad:
                    Preferencias.FactorSacudidas = 1f;
                    Preferencias.ReducirDestellos = false;
                    Preferencias.ResaltadoObjetos = 1;
                    break;
            }
            Mensaje("Listo: valores de fábrica.");
        }

        // ---------- Filas ----------

        private RectTransform NuevaFila(float y, string etiqueta, string ayuda, float alto = AltoFila)
        {
            var fila = UIRuntime.NuevoRect("Fila " + etiqueta, contenido, new Vector2(0f, y), new Vector2(AnchoVentana - 80f, alto));
            // Transparente pero recibe el mouse: así la ayuda aparece al pasar por cualquier parte de la fila.
            var img = fila.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            fila.gameObject.AddComponent<AyudaOpcion>().Configurar(this, ayuda);

            var t = Texto("Etiqueta", fila, new Vector2(XEtiqueta + AnchoEtiqueta * 0.5f, 0f), new Vector2(AnchoEtiqueta, alto), 34f, etiqueta);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            return fila;
        }

        private float FilaDeslizador(float y, string etiqueta, string ayuda, Func<float> leer, Action<float> escribir,
            float min, float max, Func<float, string> formato)
        {
            var fila = NuevaFila(y, etiqueta, ayuda);
            var slider = Deslizador(fila, new Vector2(XControl - 70f, 0f), new Vector2(AnchoControl - 150f, 40f), min, max);
            slider.gameObject.AddComponent<AyudaOpcion>().Configurar(this, ayuda);
            SoloVertical(slider);

            var valor = Texto("Valor", fila, new Vector2(XControl + AnchoControl * 0.5f - 60f, 0f), new Vector2(120f, AltoFila), 32f, "");
            valor.alignment = TextAlignmentOptions.MidlineRight;
            valor.color = ColAcento;

            bool actualizando = false;
            slider.onValueChanged.AddListener(v =>
            {
                if (actualizando) return;
                // Pasos de 5: más fácil de volver al mismo valor.
                escribir(Mathf.Round(v * 20f) / 20f);
            });
            refrescos.Add(() =>
            {
                actualizando = true;
                slider.SetValueWithoutNotify(leer());
                actualizando = false;
                valor.text = formato(leer());
            });
            return y - AltoFila;
        }

        private float FilaOpciones(float y, string etiqueta, string ayuda, string[] opciones, Func<int> leer, Action<int> escribir)
        {
            var fila = NuevaFila(y, etiqueta, ayuda);
            float anchoFlecha = 64f;
            var centro = new Vector2(XControl, 0f);

            var anterior = Boton("Anterior", fila, centro + new Vector2(-(AnchoControl - anchoFlecha) * 0.5f, 0f),
                new Vector2(anchoFlecha, 56f), "‹", 40f, () => escribir((leer() - 1 + opciones.Length) % opciones.Length));
            var siguiente = Boton("Siguiente", fila, centro + new Vector2((AnchoControl - anchoFlecha) * 0.5f, 0f),
                new Vector2(anchoFlecha, 56f), "›", 40f, () => escribir((leer() + 1) % opciones.Length));
            // El valor del medio también es un botón: clic = siguiente opción.
            var medio = Boton("Valor", fila, centro, new Vector2(AnchoControl - anchoFlecha * 2f - 16f, 56f), "", 32f,
                () => escribir((leer() + 1) % opciones.Length));
            var textoMedio = medio.GetComponentInChildren<TextMeshProUGUI>();
            textoMedio.color = ColAcento;

            foreach (var b in new[] { anterior, siguiente, medio })
                b.gameObject.AddComponent<AyudaOpcion>().Configurar(this, ayuda);
            // Con el teclado se elige el valor del medio y se cambia con ← →; las flechas
            // dibujadas son para el mouse.
            SinNavegacion(anterior);
            SinNavegacion(siguiente);
            SoloVertical(medio);

            // Con el valor seleccionado por teclado, ← y → lo cambian (como un deslizador).
            medio.gameObject.AddComponent<FlechasCambian>().Configurar(
                () => escribir((leer() - 1 + opciones.Length) % opciones.Length),
                () => escribir((leer() + 1) % opciones.Length));

            refrescos.Add(() => textoMedio.text = opciones[Mathf.Clamp(leer(), 0, opciones.Length - 1)]);
            return y - AltoFila;
        }

        private float FilaTecla(float y, Accion a)
        {
            const float alto = 62f;
            string ayuda = "Haz clic en la tecla y aprieta la nueva. Esc cancela.";
            var fila = NuevaFila(y, Controles.NombreAccion(a), ayuda, alto);

            Button boton = null;
            boton = Boton("Tecla", fila, new Vector2(XControl - 90f, 0f), new Vector2(240f, 52f), "", 32f,
                () => EmpezarEspera(a, boton));
            boton.gameObject.AddComponent<AyudaOpcion>().Configurar(this, ayuda);
            var textoTecla = boton.GetComponentInChildren<TextMeshProUGUI>();

            Key fija = Controles.TeclaFija(a);
            if (fija != Key.None)
            {
                var extra = Texto("Fija", fila, new Vector2(XControl + 160f, 0f), new Vector2(220f, alto), 26f,
                    "o  " + Controles.NombreTecla(fija));
                extra.alignment = TextAlignmentOptions.MidlineLeft;
                extra.color = ColTenue;
            }

            refrescos.Add(() =>
            {
                if (esperandoTecla == a) return;
                textoTecla.text = Controles.NombreTecla(Controles.Tecla(a));
                textoTecla.color = ColAcento;
            });
            return y - alto;
        }

        // ---------- Cambio de tecla ----------

        private void EmpezarEspera(Accion a, Button boton)
        {
            CancelarEspera();
            esperandoTecla = a;
            botonEsperando = boton;
            frameInicioEspera = Time.frameCount;
            var t = boton.GetComponentInChildren<TextMeshProUGUI>();
            t.text = "Pulsa una tecla…";
            t.color = ColTexto;
            Pintar(boton, ColBotonActivo);
            Mensaje($"Nueva tecla para «{Controles.NombreAccion(a)}». Esc cancela.");
            // Sin selección: las flechas no tienen que mover el foco mientras se espera.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void EsperarTecla(Keyboard k)
        {
            // El Enter/Espacio que activó el botón no cuenta como la tecla nueva.
            if (Time.frameCount == frameInicioEspera) return;

            if (k.escapeKey.wasPressedThisFrame)
            {
                Button b = botonEsperando;
                CancelarEspera();
                Mensaje("Cancelado.");
                Seleccionar(b);
                return;
            }

            foreach (KeyControl tecla in k.allKeys)
            {
                if (tecla == null || !tecla.wasPressedThisFrame) continue;
                Key key = tecla.keyCode;
                if (Controles.EsReservada(key))
                {
                    Mensaje($"«{Controles.NombreTecla(key)}» está reservada para los menús. Prueba con otra.");
                    return;
                }

                Accion a = esperandoTecla.Value;
                Button b = botonEsperando;
                esperandoTecla = null;
                botonEsperando = null;
                Pintar(b, ColBoton);

                Accion? cambiada = Controles.Asignar(a, key);
                Mensaje(cambiada.HasValue
                    ? $"Listo. «{Controles.NombreAccion(cambiada.Value)}» pasó a {Controles.NombreTecla(Controles.Tecla(cambiada.Value))}."
                    : $"Listo: {Controles.NombreAccion(a)} = {Controles.NombreTecla(key)}.");
                Refrescar();
                Seleccionar(b);
                return;
            }
        }

        private void CancelarEspera()
        {
            if (!esperandoTecla.HasValue) return;
            esperandoTecla = null;
            if (botonEsperando != null) Pintar(botonEsperando, ColBoton);
            botonEsperando = null;
            Refrescar();
        }

        // ---------- Ayuda ----------

        internal void MostrarAyuda(string texto)
        {
            if (esperandoTecla.HasValue) return;
            textoAyuda.text = texto;
            textoAyuda.color = ColTenue;
        }

        private void Mensaje(string texto)
        {
            textoAyuda.text = texto;
            textoAyuda.color = ColTexto;
        }

        // ---------- Navegación con teclado ----------

        // Los deslizadores y los valores usan ← → para cambiar; con ellos solo arriba/abajo
        // pasa a otra fila. El resto (pestañas, botones de abajo) navega en las 4 direcciones.
        private static void SoloVertical(Selectable s)
        {
            var nav = s.navigation;
            nav.mode = Navigation.Mode.Vertical;
            s.navigation = nav;
        }

        private static void SinNavegacion(Selectable s)
        {
            var nav = s.navigation;
            nav.mode = Navigation.Mode.None;
            s.navigation = nav;
        }

        private static void Seleccionar(Selectable s)
        {
            if (s != null && EventSystem.current != null && s.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(s.gameObject);
        }

        private static void AsegurarEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ---------- Piezas ----------

        private TextMeshProUGUI Texto(string nombre, Transform padre, Vector2 pos, Vector2 tam, float tamano, string texto)
        {
            var t = UIRuntime.NuevoTexto(nombre, padre, pos, tam, tamano, FontStyles.Normal, fuente);
            t.text = texto;
            t.color = ColTexto;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        private Button Boton(string nombre, Transform padre, Vector2 pos, Vector2 tam, string texto, float tamano, Action alHacerClic)
        {
            var img = UIRuntime.NuevaImagen(nombre, padre, pos, tam, null);
            img.color = Color.white;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Pintar(b, ColBoton);
            b.onClick.AddListener(() => alHacerClic());

            var t = Texto("Texto", img.rectTransform, Vector2.zero, tam, tamano, texto);
            UIRuntime.Estirar(t.rectTransform);
            // Textos largos ("Pulsa una tecla…", "Restablecer accesibilidad") se achican solos.
            t.enableAutoSizing = true;
            t.fontSizeMax = tamano;
            t.fontSizeMin = tamano * 0.6f;
            t.margin = new Vector4(10f, 0f, 10f, 0f);
            return b;
        }

        // La Image queda blanca y el Button la tiñe: normal = color de base, con el
        // mouse encima o seleccionado por teclado = más cálido (así se ve dónde está el foco).
        private static void Pintar(Button b, Color baseColor)
        {
            if (b == null) return;
            ((Image)b.targetGraphic).color = Color.white;
            var foco = Color.Lerp(baseColor, ColAcento, 0.45f);
            var c = b.colors;
            c.normalColor = baseColor;
            c.highlightedColor = foco;
            c.selectedColor = foco;
            c.pressedColor = Color.Lerp(baseColor, Color.black, 0.3f);
            c.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.4f);
            c.colorMultiplier = 1f;
            c.fadeDuration = 0.08f;
            b.colors = c;
        }

        private Slider Deslizador(Transform padre, Vector2 pos, Vector2 tam, float min, float max)
        {
            var raiz = UIRuntime.NuevoRect("Deslizador", padre, pos, tam);

            var fondo = UIRuntime.NuevaImagen("Fondo", raiz, Vector2.zero, Vector2.zero, null);
            var rf = fondo.rectTransform;
            rf.anchorMin = new Vector2(0f, 0.5f);
            rf.anchorMax = new Vector2(1f, 0.5f);
            rf.sizeDelta = new Vector2(0f, 10f);
            fondo.color = ColPista;

            var areaRelleno = UIRuntime.NuevoRect("Area", raiz, Vector2.zero, Vector2.zero);
            areaRelleno.anchorMin = new Vector2(0f, 0.5f);
            areaRelleno.anchorMax = new Vector2(1f, 0.5f);
            areaRelleno.sizeDelta = new Vector2(0f, 10f);
            var relleno = UIRuntime.NuevaImagen("Relleno", areaRelleno, Vector2.zero, Vector2.zero, null);
            relleno.color = ColAcento;
            relleno.rectTransform.sizeDelta = Vector2.zero;

            var areaManija = UIRuntime.NuevoRect("AreaManija", raiz, Vector2.zero, Vector2.zero);
            UIRuntime.Estirar(areaManija);
            areaManija.offsetMin = new Vector2(14f, 0f);
            areaManija.offsetMax = new Vector2(-14f, 0f);
            var manija = UIRuntime.NuevaImagen("Manija", areaManija, Vector2.zero, new Vector2(30f, 0f), spriteCirculo);
            manija.preserveAspect = true;
            manija.color = Color.white;

            var s = raiz.gameObject.AddComponent<Slider>();
            s.fillRect = relleno.rectTransform;
            s.handleRect = manija.rectTransform;
            s.targetGraphic = manija;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = min;
            s.maxValue = max;
            var c = s.colors;
            c.highlightedColor = ColAcento;
            c.selectedColor = ColAcento;
            c.pressedColor = new Color(1f, 0.8f, 0.6f, 1f);
            s.colors = c;
            return s;
        }

        private static Sprite CrearCirculo()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // La fuente del juego (IM Fell) si hay algún texto que la use en la escena; si no, la de TMP.
        private static TMP_FontAsset BuscarFuente()
        {
            TMP_FontAsset cualquiera = null;
            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.font == null) continue;
                if (t.font.name.IndexOf("Fell", StringComparison.OrdinalIgnoreCase) >= 0) return t.font;
                if (cualquiera == null) cualquiera = t.font;
            }
            return cualquiera != null ? cualquiera : TMP_Settings.defaultFontAsset;
        }

        private static string Porcentaje(float v) => Mathf.RoundToInt(v * 100f) + "%";

        private static string ConSigno(float v)
        {
            int n = Mathf.RoundToInt(v * 100f);
            return n > 0 ? "+" + n : n.ToString();
        }

        // ---------- Componentes chicos ----------

        /// <summary>Muestra la explicación de la opción al pasar el mouse o seleccionarla con el teclado.</summary>
        private class AyudaOpcion : MonoBehaviour, IPointerEnterHandler, ISelectHandler
        {
            private PanelOpciones panel;
            private string ayuda;

            public void Configurar(PanelOpciones p, string texto)
            {
                panel = p;
                ayuda = texto;
            }

            public void OnPointerEnter(PointerEventData e) => panel?.MostrarAyuda(ayuda);
            public void OnSelect(BaseEventData e) => panel?.MostrarAyuda(ayuda);
        }

        /// <summary>Con el botón seleccionado, ← y → cambian la opción en vez de mover el foco.</summary>
        private class FlechasCambian : MonoBehaviour, IMoveHandler
        {
            private Action izquierda, derecha;

            public void Configurar(Action izq, Action der)
            {
                izquierda = izq;
                derecha = der;
            }

            public void OnMove(AxisEventData e)
            {
                if (e.moveDir == MoveDirection.Left) { izquierda?.Invoke(); e.Use(); }
                else if (e.moveDir == MoveDirection.Right) { derecha?.Invoke(); e.Use(); }
                // Arriba/abajo los maneja el propio botón (navegación vertical).
            }
        }
    }
}
