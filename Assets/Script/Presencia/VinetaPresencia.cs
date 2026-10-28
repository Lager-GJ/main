using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Borde rojo que se cierra desde los bordes de la pantalla a medida que la
    /// Presencia se acerca. Va sobre la Image de OSCURO (pantalla completa en
    /// ControladorGeneral): los primeros planos se insertan después de OSCURO, así que
    /// la viñeta nunca los tapa.
    ///
    /// La textura radial se genera en código (transparente al centro, roja en los
    /// bordes). EfectosPresencia le dice cuánto cerrarse y si late.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class VinetaPresencia : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(0.62f, 0.03f, 0.03f, 1f);
        [Tooltip("Segundos que tarda en llegar a la intensidad nueva.")]
        [SerializeField] private float segundosTransicion = 1.5f;
        [Tooltip("Latidos por segundo cuando late.")]
        [SerializeField] private float frecuenciaPulso = 1.1f;

        private Image imagen;
        private float actual;
        private float objetivo;
        private float pulso;
        private float fasePulso;

        private void Awake()
        {
            imagen = GetComponent<Image>();
            imagen.sprite = CrearSpriteRadial();
            imagen.type = Image.Type.Simple;
            imagen.preserveAspect = false;
            imagen.raycastTarget = false;
            Aplicar(0f);
        }

        /// <summary>intensidad 0..1 (0 = no se ve); pulso 0..1 (cuánto late).</summary>
        public void Fijar(float intensidad, float pulsoNuevo)
        {
            objetivo = Mathf.Clamp01(intensidad);
            pulso = Mathf.Clamp01(pulsoNuevo);
        }

        private void Update()
        {
            // deltaTime: con la pausa (timeScale 0) se queda quieta.
            actual = Mathf.MoveTowards(actual, objetivo, Time.deltaTime / Mathf.Max(0.01f, segundosTransicion));
            fasePulso += Time.deltaTime * frecuenciaPulso * Mathf.PI * 2f;

            float latido = pulso > 0f ? (Mathf.Sin(fasePulso) * 0.5f + 0.5f) * pulso * 0.35f : 0f;
            Aplicar(Mathf.Clamp01(actual * (1f - 0.35f * pulso) + latido * actual));
        }

        private void Aplicar(float alfa)
        {
            imagen.enabled = alfa > 0.001f;
            imagen.color = new Color(color.r, color.g, color.b, alfa);
        }

        // Elipse transparente al centro que se oscurece hacia los bordes.
        private static Sprite CrearSpriteRadial()
        {
            const int lado = 256;
            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float dx = (x + 0.5f) / lado * 2f - 1f;
                    float dy = (y + 0.5f) / lado * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f; // 1 en las esquinas
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d));
                    px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f));
        }
    }
}
