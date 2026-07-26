using UnityEngine;

namespace Terror
{
    // Feedback visual perceptible del avance de la Presencia: un tinte rojo en
    // los bordes de pantalla que se intensifica con el nivel de cercania.
    // Placeholder sin arte final: cuando Diseñador C entregue una vineta real,
    // esto se puede reemplazar sin tocar el resto del sistema.
    public class PresenciaFeedbackVisual : MonoBehaviour
    {
        [Tooltip("Opacidad maxima del tinte cuando la Presencia esta en su nivel mas alto")]
        [Range(0f, 1f)]
        public float opacidadMaxima = 0.5f;

        private int nivelActual;
        private int nivelMaximoConocido = 5;
        private Texture2D texturaTinte;

        private void OnEnable()
        {
            GameEvents.OnCercaniaPresenciaCambiada += ActualizarNivel;
        }

        private void OnDisable()
        {
            GameEvents.OnCercaniaPresenciaCambiada -= ActualizarNivel;
        }

        private void Awake()
        {
            texturaTinte = Texture2D.whiteTexture;
        }

        private void ActualizarNivel(int nivel, float multiplicador)
        {
            nivelActual = nivel;
            if (nivel > nivelMaximoConocido)
            {
                nivelMaximoConocido = nivel;
            }
        }

        private void OnGUI()
        {
            if (nivelActual <= 0)
            {
                return;
            }

            float proporcion = (float)nivelActual / nivelMaximoConocido;
            float opacidad = proporcion * opacidadMaxima;

            GUI.color = new Color(0.6f, 0f, 0f, opacidad);
            int grosor = 40;

            GUI.DrawTexture(new Rect(0, 0, Screen.width, grosor), texturaTinte);
            GUI.DrawTexture(new Rect(0, Screen.height - grosor, Screen.width, grosor), texturaTinte);
            GUI.DrawTexture(new Rect(0, 0, grosor, Screen.height), texturaTinte);
            GUI.DrawTexture(new Rect(Screen.width - grosor, 0, grosor, Screen.height), texturaTinte);

            GUI.color = Color.white;
        }
    }
}
