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
        [SerializeField] private Vector2 tamano = new Vector2(1240f, 700f);
        [SerializeField] private Vector2 posicion = new Vector2(0f, 20f);
        [SerializeField] private Color colorFondo = new Color(0.06f, 0.06f, 0.07f, 0.94f);
        [SerializeField] private string textoBoton = "Entendido";
        [Tooltip("Tecla que también cierra la tarjeta (la que pide el mensaje). Ninguna = solo X y botón.")]
        [SerializeField] private Key teclaQueCierra = Key.E;
        [SerializeField] private TMP_FontAsset fuente;

        private GameObject fondo;
        private bool construida;

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

            // El fondo va detrás del título: es un hermano anterior (un hijo se dibujaría encima).
            var rt = (RectTransform)transform;
            fondo = new GameObject(name + "_Tarjeta", typeof(RectTransform), typeof(Image));
            var rf = (RectTransform)fondo.transform;
            rf.SetParent(rt.parent, false);
            rf.SetSiblingIndex(rt.GetSiblingIndex());
            rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0.5f, 0.5f);
            rf.anchoredPosition = posicion;
            rf.sizeDelta = tamano;
            var imgFondo = fondo.GetComponent<Image>();
            imgFondo.color = colorFondo;
            var borde = NuevaImagen("Borde", rf, Vector2.zero, tamano + new Vector2(16f, 16f), MarcoDibujado.Obtener(128, 7f, 6f, 1f));
            borde.type = Image.Type.Sliced;
            borde.raycastTarget = false;
            fondo.SetActive(gameObject.activeSelf);

            // El texto del mensaje queda como título, arriba de la tarjeta y con ajuste de línea.
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = posicion + new Vector2(0f, tamano.y * 0.5f - 110f);
            rt.sizeDelta = new Vector2(tamano.x - 200f, 150f);
            titulo.textWrappingMode = TextWrappingModes.Normal;
            titulo.overflowMode = TextOverflowModes.Overflow;
            titulo.alignment = TextAlignmentOptions.Center;
            titulo.fontSize = 46f;

            // Las imágenes y botones van como hijos del título (se mueven con él).
            // Medidas relativas al centro del título, que está 110 px debajo del borde de arriba.
            float yImg = -(tamano.y * 0.5f - 110f) + 10f;
            Vector2 tamImg = new Vector2(440f, 270f);
            Cuadro("Antes", new Vector2(-300f, yImg), tamImg, imagenAntes, textoAntes);
            Cuadro("Despues", new Vector2(300f, yImg), tamImg, imagenDespues, textoDespues);
            var flecha = NuevoTexto("Flecha", transform, new Vector2(0f, yImg), new Vector2(140f, 120f), 90f, FontStyles.Normal, fuente);
            flecha.text = "→";
            if (!fuente || !fuente.HasCharacter('→')) flecha.text = ">";

            // X arriba a la derecha.
            var x = NuevoTexto("Cerrar", transform, new Vector2(tamano.x * 0.5f - 60f, 60f), new Vector2(80f, 80f), 54f, FontStyles.Normal, fuente);
            x.text = "X";
            x.raycastTarget = true;
            BotonPlano(x.gameObject, Cerrar);

            // Botón de abajo.
            float yBoton = -(tamano.y - 110f) + 70f;
            var boton = NuevaImagen("Boton", transform, new Vector2(0f, yBoton), new Vector2(320f, 76f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            boton.type = Image.Type.Sliced;
            BotonPlano(boton.gameObject, Cerrar);
            NuevoTexto("Texto", boton.transform, Vector2.zero, new Vector2(320f, 76f), 36f, FontStyles.Normal, fuente).text = textoBoton;
        }

        private void Cuadro(string nombre, Vector2 pos, Vector2 tam, Sprite sprite, string leyenda)
        {
            var img = NuevaImagen(nombre, transform, pos, tam, sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sprite == null) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var marco = NuevaImagen("Marco", img.transform, Vector2.zero, tam + new Vector2(10f, 10f), MarcoDibujado.Obtener(64, 4f, 3f, 1f));
            marco.type = Image.Type.Sliced;
            marco.raycastTarget = false;
            var t = NuevoTexto("Leyenda", img.transform, new Vector2(0f, -tam.y * 0.5f - 34f), new Vector2(tam.x, 50f), 32f, FontStyles.Italic, fuente);
            t.text = leyenda;
            t.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }
}
