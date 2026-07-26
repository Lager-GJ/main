using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Conecta PresenciaManager.cs (namespace global, riesgo 0-1 en tiempo real
    /// del fósforo real) al multiplicador que Terror.FearManager espera recibir
    /// por GameEvents.OnCercaniaPresenciaCambiada. Sin este puente, FearManager
    /// sube el miedo a ritmo fijo sin importar qué tan cerca está la Presencia
    /// (ver CLAUDE.md, "Known issues to port/fix" #2).
    ///
    /// No modifica PresenciaManager.cs ni BarraDeMiedo.cs a propósito — este
    /// script solo escucha y traduce, no reemplaza nada de lo que ya existe.
    /// </summary>
    public class PresenciaFearBridge : MonoBehaviour
    {
        [Tooltip("Multiplicador de velocidad de subida del miedo por nivel de riesgo (0..N-1), mismo rango que el PresenciaController original.")]
        public float[] multiplicadorPorNivel = { 1f, 1.2f, 1.5f, 2f, 2.5f, 3.5f };

        private int nivelActual = -1;

        private void OnEnable()
        {
            PresenciaManager.OnRiesgoCambiado += ManejarRiesgoCambiado;
        }

        private void OnDisable()
        {
            PresenciaManager.OnRiesgoCambiado -= ManejarRiesgoCambiado;
        }

        private void ManejarRiesgoCambiado(float riesgo)
        {
            int nivel = Mathf.Clamp(Mathf.RoundToInt(riesgo * (multiplicadorPorNivel.Length - 1)), 0, multiplicadorPorNivel.Length - 1);
            if (nivel == nivelActual)
                return;

            nivelActual = nivel;
            GameEvents.RaiseCercaniaPresenciaCambiada(nivel, multiplicadorPorNivel[nivel]);
        }
    }
}
