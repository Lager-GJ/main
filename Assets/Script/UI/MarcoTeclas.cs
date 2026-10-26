using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Dibuja un marco (como una tecla) alrededor de las partes del texto marcadas con
    /// &lt;link=tecla&gt;...&lt;/link&gt;. Ejemplo: "Pulsa &lt;link=tecla&gt;E&lt;/link&gt; para encender".
    /// Sirve para cualquier mensaje del tutorial: basta con agregar este componente al
    /// texto y marcar las palabras en el propio texto.
    /// Sin sprite asignado, el marco se genera en código (borde blanco con esquinas
    /// redondeadas); para un marco dibujado a mano, asignar un sprite con bordes 9-slice.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class MarcoTeclas : MonoBehaviour
    {
        private const string IdEnlace = "tecla";

        [Tooltip("Vacío = se genera un borde simple en código. Si se asigna, que tenga bordes 9-slice.")]
        [SerializeField] private Sprite marco;
        [SerializeField] private Color colorMarco = new Color(1f, 1f, 1f, 0.9f);
        [Tooltip("Espacio entre la letra y el marco (horizontal, vertical).")]
        [SerializeField] private Vector2 relleno = new Vector2(16f, 6f);
        [Tooltip("Grosor visual del borde generado (más alto = borde más fino).")]
        [SerializeField] private float finuraBorde = 1.6f;
        [Tooltip("El marco late suavemente para llamar la atención.")]
        [SerializeField] private bool latir = true;
        [SerializeField] private float velocidadLatido = 2.2f;

        private TMP_Text texto;
        private readonly List<Image> marcos = new List<Image>();
        private bool sucio = true;

        private void Awake()
        {
            texto = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(AlCambiarTexto);
            sucio = true;
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(AlCambiarTexto);
        }

        private void AlCambiarTexto(Object obj)
        {
            if (obj == texto) sucio = true;
        }

        private void LateUpdate()
        {
            if (sucio)
            {
                sucio = false;
                Reconstruir();
            }

            if (latir && marcos.Count > 0)
            {
                // Tiempo sin escala: el mensaje puede verse con el juego congelado (timeScale 0).
                float a = colorMarco.a * Mathf.Lerp(0.55f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * velocidadLatido));
                foreach (var m in marcos)
                    if (m.gameObject.activeSelf) m.color = new Color(colorMarco.r, colorMarco.g, colorMarco.b, a);
            }
        }

        private void Reconstruir()
        {
            texto.ForceMeshUpdate();
            TMP_TextInfo info = texto.textInfo;

            int usados = 0;
            for (int i = 0; i < info.linkCount; i++)
            {
                TMP_LinkInfo enlace = info.linkInfo[i];
                if (enlace.GetLinkID() != IdEnlace) continue;
                if (!Limites(info, enlace, out Vector2 min, out Vector2 max)) continue;

                Image img = ObtenerMarco(usados++);
                var rt = img.rectTransform;
                rt.anchoredPosition = (min + max) * 0.5f;
                Vector2 tam = max - min + relleno * 2f;
                tam.x = Mathf.Max(tam.x, tam.y); // una letra sola queda como tecla cuadrada
                rt.sizeDelta = tam;
                img.gameObject.SetActive(true);
            }

            for (int i = usados; i < marcos.Count; i++)
                marcos[i].gameObject.SetActive(false);
        }

        // Rectángulo de los caracteres del enlace, en coordenadas locales del texto.
        // El alto sale de la línea (ascender/descender) para que "E" y "4" midan lo mismo.
        private static bool Limites(TMP_TextInfo info, TMP_LinkInfo enlace, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            bool alguno = false;

            int fin = enlace.linkTextfirstCharacterIndex + enlace.linkTextLength;
            for (int c = enlace.linkTextfirstCharacterIndex; c < fin && c < info.characterCount; c++)
            {
                TMP_CharacterInfo ch = info.characterInfo[c];
                if (!ch.isVisible) continue;
                alguno = true;
                min.x = Mathf.Min(min.x, ch.bottomLeft.x);
                max.x = Mathf.Max(max.x, ch.topRight.x);
                min.y = Mathf.Min(min.y, ch.baseLine + (ch.descender - ch.baseLine) * 0.35f);
                max.y = Mathf.Max(max.y, ch.ascender);
            }
            return alguno;
        }

        private Image ObtenerMarco(int indice)
        {
            if (indice < marcos.Count) return marcos[indice];

            var go = new GameObject("MarcoTecla", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);

            // Las posiciones de TMP son locales al texto, con origen en su pivot. Anclando
            // el marco a ese mismo punto, anchoredPosition = posición local del carácter.
            rt.anchorMin = rt.anchorMax = ((RectTransform)transform).pivot;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.sprite = marco != null ? marco : MarcoDibujado.Obtener(64, 3.2f, 14f, 0.5f);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = marco != null ? 1f : finuraBorde;
            img.color = colorMarco;
            img.raycastTarget = false;

            marcos.Add(img);
            return img;
        }
    }
}
