using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    // Barra de miedo estilo "Minecraft": una fila de segmentos (iconos) que se
    // van llenando de a uno conforme sube FearManager.miedoActual (0-100).
    // Asigna los 5 Image en orden de izquierda a derecha en el Inspector.
    public class FearBarUI : MonoBehaviour
    {
        [Tooltip("Un Image por segmento, en orden de izquierda a derecha (ej. 5, como en Minecraft).")]
        public Image[] segmentos;

        [Tooltip("Opcional: sprite del segmento lleno. Si dejas esto y 'Sprite Vacio' sin asignar, el script solo activa/desactiva el segmento (util si tu fondo ya dibuja el hueco vacio debajo).")]
        public Sprite spriteLleno;
        [Tooltip("Opcional: sprite del segmento vacio (solo se usa si tambien asignas Sprite Lleno).")]
        public Sprite spriteVacio;

        private void Start()
        {
            if (FearManager.Instance == null)
            {
                Debug.LogWarning("[FearBarUI] No hay FearManager en la escena.");
                return;
            }

            FearManager.Instance.OnMiedoCambiado += ActualizarBarra;
            ActualizarBarra(FearManager.Instance.miedoActual);
        }

        private void OnDestroy()
        {
            if (FearManager.Instance != null)
                FearManager.Instance.OnMiedoCambiado -= ActualizarBarra;
        }

        private void ActualizarBarra(float miedo)
        {
            if (segmentos == null || segmentos.Length == 0) return;

            float porcentaje = miedo / 100f;
            int llenos = Mathf.Clamp(Mathf.RoundToInt(porcentaje * segmentos.Length), 0, segmentos.Length);

            bool usarSprites = spriteLleno != null && spriteVacio != null;

            for (int i = 0; i < segmentos.Length; i++)
            {
                bool lleno = i < llenos;

                if (usarSprites)
                    segmentos[i].sprite = lleno ? spriteLleno : spriteVacio;
                else
                    segmentos[i].enabled = lleno;
            }
        }
    }
}
