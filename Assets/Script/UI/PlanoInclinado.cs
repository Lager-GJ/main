using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Perspectiva de un plano (homografía) definida por 4 esquinas de origen y 4 de destino.
    /// Sirve para "acostar" los objetos de un cajón sobre su piso inclinado: las posiciones
    /// están pensadas para el cajón visto desde arriba y se pasan al dibujo del mueble en el
    /// cuarto, achicándose hacia el fondo y aplastándose como el piso.
    /// </summary>
    public readonly struct PlanoInclinado
    {
        private readonly float a, b, c, d, e, f, g, h;

        private PlanoInclinado(float[] k)
        {
            a = k[0]; b = k[1]; c = k[2]; d = k[3]; e = k[4]; f = k[5]; g = k[6]; h = k[7];
        }

        /// <summary>Esquinas en el mismo orden en los dos (por ejemplo: fondo izq., fondo der., frente der., frente izq.).</summary>
        public static PlanoInclinado Desde(Vector2[] origen, Vector2[] destino)
        {
            var m = new float[8, 9];
            for (int i = 0; i < 4; i++)
            {
                float x = origen[i].x, y = origen[i].y, u = destino[i].x, v = destino[i].y;
                int r = i * 2;
                m[r, 0] = x; m[r, 1] = y; m[r, 2] = 1; m[r, 6] = -u * x; m[r, 7] = -u * y; m[r, 8] = u;
                m[r + 1, 3] = x; m[r + 1, 4] = y; m[r + 1, 5] = 1; m[r + 1, 6] = -v * x; m[r + 1, 7] = -v * y; m[r + 1, 8] = v;
            }
            // Eliminación de Gauss con pivote.
            for (int col = 0; col < 8; col++)
            {
                int piv = col;
                for (int r = col + 1; r < 8; r++)
                    if (Mathf.Abs(m[r, col]) > Mathf.Abs(m[piv, col])) piv = r;
                if (piv != col)
                    for (int k = 0; k < 9; k++) { float t = m[col, k]; m[col, k] = m[piv, k]; m[piv, k] = t; }
                for (int r = 0; r < 8; r++)
                {
                    if (r == col || m[col, col] == 0f) continue;
                    float fct = m[r, col] / m[col, col];
                    for (int k = col; k < 9; k++) m[r, k] -= fct * m[col, k];
                }
            }
            var sol = new float[8];
            for (int i = 0; i < 8; i++) sol[i] = m[i, i] != 0f ? m[i, 8] / m[i, i] : 0f;
            return new PlanoInclinado(sol);
        }

        public Vector2 Aplicar(Vector2 p)
        {
            float w = g * p.x + h * p.y + 1f;
            return new Vector2((a * p.x + b * p.y + c) / w, (d * p.x + e * p.y + f) / w);
        }
    }
}
