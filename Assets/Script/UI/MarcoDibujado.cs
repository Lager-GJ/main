using System.Collections.Generic;
using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Genera en código bordes blancos irregulares (como el borde de tiza de las tarjetas
    /// de Historia), listos para una Image en modo Sliced. Es un reemplazo provisorio:
    /// cuando el equipo de arte entregue un marco dibujado, se asigna ese sprite en el
    /// Inspector y esto deja de usarse.
    /// </summary>
    public static class MarcoDibujado
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <param name="lado">Tamaño de la textura (px).</param>
        /// <param name="grosor">Grosor del trazo (px).</param>
        /// <param name="radio">Radio de las esquinas (px).</param>
        /// <param name="aspereza">0 = trazo limpio, 1 = trazo de tiza.</param>
        public static Sprite Obtener(int lado, float grosor, float radio, float aspereza)
        {
            string clave = $"{lado}_{grosor}_{radio}_{aspereza}";
            if (cache.TryGetValue(clave, out Sprite s) && s != null) return s;

            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float medio = lado * 0.5f;
            for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                // Distancia con signo al contorno de un rectángulo redondeado (negativa adentro).
                float px = Mathf.Abs(x + 0.5f - medio) - (medio - radio - 1f);
                float py = Mathf.Abs(y + 0.5f - medio) - (medio - radio - 1f);
                float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(px, py), 0f) - radio;

                // El trazo vive hacia adentro del contorno; su grosor y su opacidad varían
                // un poco con ruido, como una línea hecha a mano.
                float g = grosor * (1f - aspereza * 0.35f + aspereza * 0.5f * Mathf.PerlinNoise(x * 0.23f, y * 0.21f));
                float a = Mathf.Clamp01(1f - Mathf.Abs(d + g * 0.5f) / (g * 0.5f + 0.7f));
                a *= 1f - aspereza * 0.45f * Mathf.PerlinNoise(x * 0.9f + 13f, y * 0.9f + 7f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();

            float b = Mathf.Min(medio - 1f, radio + grosor + 3f);
            s = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = "MarcoDibujado_" + clave;
            cache[clave] = s;
            return s;
        }
    }
}
