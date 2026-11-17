using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Mientras se mira algo de cerca (el cajón, un cuadro, la caja de fósforos, el
    /// inventario...) Luis se desvanece y vuelve al cerrar (2026-11-17). Antes, con la
    /// cámara acercada, su dibujo quedaba encima del primer plano: el brazo tapaba el
    /// velador y la cabeza el botón Inspeccionar.
    ///
    /// Lo agrega solo NinoController; no hace falta ponerlo en la escena.
    /// </summary>
    public class LuisEnPrimerPlano : MonoBehaviour
    {
        private const float Duracion = 0.2f;

        private SpriteRenderer[] dibujos;
        private float[] alfasBase;
        private float visible = 1f;

        private void Start()
        {
            dibujos = GetComponentsInChildren<SpriteRenderer>(true);
            alfasBase = new float[dibujos.Length];
            for (int i = 0; i < dibujos.Length; i++) alfasBase[i] = dibujos[i].color.a;
        }

        private static bool HayPrimerPlano()
        {
            var panel = PanelInspeccion.Instance;
            return panel != null && (panel.EstaAbierto || panel.HayCapaAbierta);
        }

        private void LateUpdate()
        {
            if (dibujos == null) return;
            float objetivo = HayPrimerPlano() ? 0f : 1f;
            if (Mathf.Approximately(visible, objetivo)) return;

            visible = Mathf.MoveTowards(visible, objetivo, Time.unscaledDeltaTime / Duracion);
            for (int i = 0; i < dibujos.Length; i++)
            {
                if (dibujos[i] == null) continue;
                Color c = dibujos[i].color;
                c.a = alfasBase[i] * visible;
                dibujos[i].color = c;
            }
        }

        private void OnDisable()
        {
            // Nunca queda invisible si esto se apaga a mitad de camino.
            visible = 1f;
            if (dibujos == null) return;
            for (int i = 0; i < dibujos.Length; i++)
            {
                if (dibujos[i] == null) continue;
                Color c = dibujos[i].color;
                c.a = alfasBase[i];
                dibujos[i].color = c;
            }
        }
    }
}
