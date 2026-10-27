using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror.UI
{
    /// <summary>
    /// Convierte un mensaje del tutorial en una tarjeta (boceto de David, 2026-10-26):
    /// arriba el texto del mensaje, en el medio dos imágenes con una flecha (por ejemplo
    /// "a oscuras" → "con un fósforo"), una X arriba a la derecha y un botón abajo.
    ///
    /// Con 'papel' asignado (2026-10-27) es la hoja de instrucciones de la caja de
    /// fósforos: papel viejo con borde roto, tinta oscura y el encabezado "INSTRUCCIONES"
    /// (el reverso de la caja dice "Lea las instrucciones antes de usar").
    ///
    /// Va en el mismo objeto del mensaje (su texto pasa a ser el título de la tarjeta).
    /// El tutorial lo enciende y lo apaga como cualquier mensaje. La X, el botón o la
    /// tecla indicada lo cierran.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TarjetaTutorial : MonoBehaviour
    {
        [Header("Imágenes")]
        [SerializeField] private Sprite imagenAntes;
        [SerializeField] private Sprite imagenDespues;
        [SerializeField] private string textoAntes = "A oscuras";
        [SerializeField] private string textoDespues = "Con un fósforo";

        [Header("Tarjeta")]
        [SerializeField] private Vector2 tamano = new Vector2(1080f, 700f);
        [SerializeField] private Vector2 posicion = new Vector2(0f, 20f);
        [Tooltip("Papel de fondo (Tutorial_Papel). Vacío = rectángulo oscuro con borde de tiza.")]
        [SerializeField] private Sprite papel;
        [Tooltip("Color del texto, los marcos y los botones sobre el papel.")]
        [SerializeField] private Color tinta = new Color(0.2f, 0.13f, 0.09f, 1f);
        [SerializeField] private string encabezado = "INSTRUCCIONES";
        [SerializeField] private Color colorFondo = new Color(0.06f, 0.06f, 0.07f, 0.94f);
        [SerializeField] private string textoBoton = "Entendido";
        [Tooltip("Tecla que también cierra la tarjeta (la que pide el mensaje). Ninguna = solo X y botón.")]
        [SerializeField] private Key teclaQueCierra = Key.E;
        [SerializeField] private TMP_FontAsset fuente;

        private GameObject fondo;
        private bool construida;

        private bool ConPapel => papel != null;
        private Color ColorTexto => ConPapel ? tinta : Color.white;

        private void Awake() => Construir();

        private void OnEnable()
        {
            Construir();
            if (fondo != null) fondo.SetActive(true);
        }

        private void OnDisable()
        {
            if (fondo != null) fondo.SetActive(false);
        }

        private void OnDestroy()
        {
            if (fondo != null) Destroy(fondo);
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null || teclaQueCierra == Key.None) return;
            if (k[teclaQueCierra].wasPressedThisFrame) Cerrar();
        }

        public void Cerrar() => gameObject.SetActive(false);

        private void Construir()
        {
            if (construida) return;
            construida = true;

            var titulo = GetComponent<TMP_Text>();
            if (fuente == null) fuente = titulo.font;
            float medioAlto = tamano.y * 0.5f, medioAncho = tamano.x * 0.5f;

            // El fondo va detrás del título: es un hermano anterior (un hijo se dibujaría
            // encima). Las imágenes y los botones van dentro del fondo.
            var rt = (RectTransform)transform;
            fondo = new GameObject(name + "_Tarjeta", typeof(RectTransform), typeof(Image));
            var rf = (RectTransform)fondo.transform;
            rf.SetParent(rt.parent, false);
            rf.SetSiblingIndex(rt.GetSiblingIndex());
            rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0.5f, 0.5f);
            rf.anchoredPosition = posicion;
            rf.sizeDelta = tamano;
            var imgFondo = fondo.GetComponent<Image>();
            if (ConPapel)
            {
                imgFondo.sprite = papel;
                imgFondo.color = Color.white;
            }
            else
            {
                imgFondo.color = colorFondo;
                var borde = NuevaImagen("Borde", rf, Vector2.zero, tamano + new Vector2(16f, 16f), MarcoDibujado.Obtener(128, 7f, 6f, 1f));
                borde.type = Image.Type.Sliced;
                borde.raycastTarget = false;
            }
            fondo.SetActive(gameObject.activeSelf);

            // Encabezado de la hoja.
            float yTitulo = medioAlto - 115f;
            if (ConPapel && !string.IsNullOrEmpty(encabezado))
            {
                var enc = NuevoTexto("Encabezado", rf, new Vector2(0f, medioAlto - 92f), new Vector2(tamano.x - 260f, 50f), 34f, FontStyles.Normal, fuente);
                enc.text = encabezado;
                enc.characterSpacing = 18f;
                enc.color = new Color(tinta.r, tinta.g, tinta.b, 0.85f);
                var linea = NuevaImagen("Linea", rf, new Vector2(0f, medioAlto - 122f), new Vector2(260f, 3f), null);
                linea.color = new Color(tinta.r, tinta.g, tinta.b, 0.6f);
                linea.raycastTarget = false;
                yTitulo = medioAlto - 190f;
            }

            // El texto del mensaje queda como título: centrado, con ajuste de línea, sin
            // márgenes heredados (en la escena traía un margen negativo y se salía) y
            // achicándose si no entra.
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = posicion + new Vector2(0f, yTitulo);
            rt.sizeDelta = new Vector2(tamano.x - 220f, 120f);
            titulo.margin = Vector4.zero;
            titulo.textWrappingMode = TextWrappingModes.Normal;
            titulo.overflowMode = TextOverflowModes.Overflow;
            titulo.alignment = TextAlignmentOptions.Center;
            titulo.enableAutoSizing = true;
            titulo.fontSizeMin = 30f;
            titulo.fontSizeMax = 44f;
            titulo.lineSpacing = 12f;
            titulo.color = ColorTexto;
            var teclas = GetComponent<MarcoTeclas>();
            if (teclas != null && ConPapel) teclas.ColorMarco = new Color(tinta.r, tinta.g, tinta.b, 0.9f);

            // Las dos imágenes con la flecha.
            float yImg = ConPapel ? -30f : -(medioAlto - 110f) + 10f + yTitulo;
            Vector2 tamImg = new Vector2(tamano.x * 0.32f, tamano.x * 0.32f * 270f / 440f);
            float xImg = tamano.x * 0.24f;
            Cuadro("Antes", rf, new Vector2(-xImg, yImg), tamImg, imagenAntes, textoAntes);
            Cuadro("Despues", rf, new Vector2(xImg, yImg), tamImg, imagenDespues, textoDespues);
            var flecha = NuevoTexto("Flecha", rf, new Vector2(0f, yImg), new Vector2(140f, 120f), 90f, FontStyles.Normal, fuente);
            flecha.text = "→";
            if (!fuente || !fuente.HasCharacter('→')) flecha.text = ">";
            flecha.color = ColorTexto;

            // X arriba a la derecha, dentro del marco del papel.
            var x = NuevoTexto("Cerrar", rf, new Vector2(medioAncho - 100f, medioAlto - 92f), new Vector2(80f, 80f), 54f, FontStyles.Normal, fuente);
            x.text = "X";
            x.color = ColorTexto;
            x.raycastTarget = true;
            BotonPlano(x.gameObject, Cerrar);

            // Botón de abajo.
            float yBoton = -medioAlto + 105f;
            var boton = NuevaImagen("Boton", rf, new Vector2(0f, yBoton), new Vector2(300f, 72f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            boton.type = Image.Type.Sliced;
            boton.color = ColorTexto;
            BotonPlano(boton.gameObject, Cerrar);
            var tb = NuevoTexto("Texto", boton.transform, Vector2.zero, new Vector2(300f, 72f), 36f, FontStyles.Normal, fuente);
            tb.text = textoBoton;
            tb.color = ColorTexto;
        }

        private void Cuadro(string nombre, Transform padre, Vector2 pos, Vector2 tam, Sprite sprite, string leyenda)
        {
            var img = NuevaImagen(nombre, padre, pos, tam, sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite == null) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var marco = NuevaImagen("Marco", img.transform, Vector2.zero, tam + new Vector2(12f, 12f), MarcoDibujado.Obtener(64, 4f, 3f, 1f));
            marco.type = Image.Type.Sliced;
            marco.raycastTarget = false;
            marco.color = ColorTexto;
            var t = NuevoTexto("Leyenda", img.transform, new Vector2(0f, -tam.y * 0.5f - 34f), new Vector2(tam.x + 40f, 50f), 32f, FontStyles.Italic, fuente);
            t.text = leyenda;
            t.color = ConPapel ? tinta : new Color(1f, 1f, 1f, 0.8f);
        }
    }
}
