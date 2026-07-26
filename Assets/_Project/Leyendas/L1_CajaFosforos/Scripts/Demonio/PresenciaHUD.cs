using UnityEngine;

namespace Terror
{
    // Feedback perceptible minimo del checklist de Dev C: muestra el nivel de
    // cercania de la Presencia en pantalla. Usa OnGUI para no depender de un
    // Canvas/TextMeshPro configurado (eso lo entrega Diseñador B mas adelante).
    public class PresenciaHUD : MonoBehaviour
    {
        private int nivelActual;
        private float multiplicadorActual = 1f;

        private void OnEnable()
        {
            GameEvents.OnCercaniaPresenciaCambiada += ActualizarValores;
        }

        private void OnDisable()
        {
            GameEvents.OnCercaniaPresenciaCambiada -= ActualizarValores;
        }

        private void ActualizarValores(int nivel, float multiplicador)
        {
            nivelActual = nivel;
            multiplicadorActual = multiplicador;
        }

        private void OnGUI()
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 400, 30),
                $"Presencia: nivel {nivelActual} (miedo x{multiplicadorActual:0.0})");
        }
    }
}
