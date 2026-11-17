using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Piezas del diseño de la interfaz de Lumbre (hoja del equipo, 2026-11-17):
    /// la tarjeta de papel, los bordes de las casillas y del candado, el degradado de los
    /// diálogos, las flechas del candado y la fuente de los números (Jaini).
    ///
    /// La tarjeta de papel y la fuente se cargan de Resources (UI/Papel_Tarjeta y
    /// Fuentes/Jaini-Regular); el resto se dibuja acá, una sola vez por tamaño.
    /// </summary>
    public static class ArteUI
    {
        // Colores de la hoja de diseño.
        public static readonly Color Tinta = new Color32(0x1E, 0x1B, 0x18, 0xFF);           // texto sobre papel
        public static readonly Color TextoClaro = new Color32(0xF2, 0xF0, 0xEC, 0xFF);      // "Negro/50": diálogos y etiquetas
        public static readonly Color Casilla = new Color32(0xF1, 0xEF, 0xEA, 0xFF);         // casilla con objeto
        public static readonly Color CasillaElegida = new Color32(0xCF, 0xCB, 0xC3, 0xFF);  // casilla seleccionada
        public static readonly Color BordeVacio = new Color32(0x3E, 0x3E, 0x3E, 0xFF);      // casilla vacía (punteada)

        private static Sprite papel;
        private static bool papelBuscado;
        private static TMP_FontAsset fuenteNumeros;
        private static bool fuenteBuscada;
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>La tarjeta de papel (con 9-slice: se estira sin deformar las esquinas).</summary>
        public static Sprite Papel()
        {
            if (!papelBuscado)
            {
                papelBuscado = true;
                papel = Resources.Load<Sprite>("UI/Papel_Tarjeta");
            }
            return papel;
        }

        /// <summary>Jaini, para los números del candado. Sin ella, 'respaldo'.</summary>
        public static TMP_FontAsset FuenteNumeros(TMP_FontAsset respaldo)
        {
            if (!fuenteBuscada)
            {
                fuenteBuscada = true;
                var ttf = Resources.Load<Font>("Fuentes/Jaini-Regular");
                if (ttf != null) fuenteNumeros = TMP_FontAsset.CreateFontAsset(ttf);
            }
            return fuenteNumeros != null ? fuenteNumeros : respaldo;
        }

        /// <summary>
        /// Sombra negra suave debajo de las letras, sin contorno (las etiquetas de objeto:
        /// negro al 90 %, corrida 2 px hacia abajo, desenfoque 8).
        /// </summary>
        public static void Sombra(TMP_Text t)
        {
            if (t == null) return;
            Material m = t.fontMaterial;
            if (m != null && m.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.9f));
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
                m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.7f);
            }
            t.UpdateMeshPadding();
            t.SetAllDirty();
        }

        // ---------- Bordes y rellenos (9-slice) ----------

        /// <summary>Rectángulo redondeado lleno, para teñir con el color de la Image (Sliced).</summary>
        public static Sprite Relleno(int radio)
        {
            string clave = "relleno" + radio;
            if (cache.TryGetValue(clave, out var s)) return s;
            int b = radio + 2, lado = b * 2 + 2;
            s = CrearRedondeado(lado, radio, -1f, b);
            cache[clave] = s;
            return s;
        }

        /// <summary>Solo el borde de un rectángulo redondeado, de 'grosor' px (Sliced, sin centro).</summary>
        public static Sprite Borde(int grosor, int radio)
        {
            string clave = "borde" + grosor + "_" + radio;
            if (cache.TryGetValue(clave, out var s)) return s;
            int b = Mathf.Max(radio, grosor) + 2, lado = b * 2 + 2;
            s = CrearRedondeado(lado, radio, grosor, b);
            cache[clave] = s;
            return s;
        }

        // Distancia con signo a un rectángulo redondeado (negativa adentro).
        private static float DistanciaRedondeado(float x, float y, float ancho, float alto, float radio)
        {
            float qx = Mathf.Abs(x - ancho * 0.5f) - (ancho * 0.5f - radio);
            float qy = Mathf.Abs(y - alto * 0.5f) - (alto * 0.5f - radio);
            float afuera = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return afuera + Mathf.Min(Mathf.Max(qx, qy), 0f) - radio;
        }

        // grosor < 0 = lleno.
        private static Sprite CrearRedondeado(int lado, int radio, float grosor, int borde9)
        {
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    float d = DistanciaRedondeado(x + 0.5f, y + 0.5f, lado, lado, radio);
                    float a = Mathf.Clamp01(0.5f - d); // adentro del contorno exterior
                    if (grosor >= 0f) a *= Mathf.Clamp01(d + grosor + 0.5f); // afuera del contorno interior
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(borde9, borde9, borde9, borde9));
        }

        /// <summary>Borde punteado de una casilla vacía, al tamaño justo (no se estira).</summary>
        public static Sprite Punteado(int ancho, int alto, int grosor = 3, int raya = 12, int hueco = 8)
        {
            string clave = $"punteado{ancho}x{alto}_{grosor}";
            if (cache.TryGetValue(clave, out var s)) return s;
            var tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[ancho * alto];
            int ciclo = raya + hueco;
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    bool enBorde = x < grosor || y < grosor || x >= ancho - grosor || y >= alto - grosor;
                    if (!enBorde) continue;
                    // Las rayas corren a lo largo de cada lado, empezando en la esquina.
                    bool lateral = x < grosor || x >= ancho - grosor;
                    int pos = lateral ? y : x;
                    if (pos % ciclo < raya) px[y * ancho + x] = new Color32(255, 255, 255, 255);
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, ancho, alto), new Vector2(0.5f, 0.5f));
            cache[clave] = s;
            return s;
        }

        /// <summary>Degradado vertical: transparente arriba, negro abajo (fondo de los diálogos).</summary>
        public static Sprite Degradado()
        {
            if (cache.TryGetValue("degradado", out var s)) return s;
            const int alto = 128;
            var tex = new Texture2D(1, alto, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < alto; y++)
            {
                float t = 1f - y / (float)(alto - 1); // 1 abajo, 0 arriba
                tex.SetPixel(0, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, t)));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 1, alto), new Vector2(0.5f, 0.5f));
            cache["degradado"] = s;
            return s;
        }

        /// <summary>Flecha en "V" del candado: hacia arriba (^) o hacia abajo (v).</summary>
        public static Sprite Flecha(bool arriba)
        {
            string clave = arriba ? "flechaArriba" : "flechaAbajo";
            if (cache.TryGetValue(clave, out var s)) return s;
            const int ancho = 48, alto = 28;
            const float grosor = 3.2f;
            var tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 punta = new Vector2(ancho * 0.5f, arriba ? alto - 5f : 5f);
            Vector2 izq = new Vector2(5f, arriba ? 5f : alto - 5f);
            Vector2 der = new Vector2(ancho - 5f, izq.y);
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(DistanciaASegmento(p, izq, punta), DistanciaASegmento(p, punta, der));
                    float a = Mathf.Clamp01(grosor * 0.5f + 0.5f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, ancho, alto), new Vector2(0.5f, 0.5f));
            cache[clave] = s;
            return s;
        }

        private static float DistanciaASegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
