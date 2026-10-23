using System.Collections.Generic;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Cámara lenta mientras el jugador mira algo de cerca, lee o resuelve un acertijo
    /// (pedido de David, 2026-10-22): el tiempo del cuarto empieza a pasar lento y
    /// vuelve a la normalidad al cerrar. Va junto con el zoom de la cámara
    /// (CamaraController.Enfocar) y con la pausa del fósforo.
    ///
    /// Funciona por motivos, igual que la pausa del fósforo: cada sistema pide
    /// (Pedir) y suelta (Soltar) el suyo, y el tiempo vuelve a 1 cuando no queda ninguno.
    /// Nunca toca el tiempo si está congelado (timeScale 0: el menú de pausa o un paso
    /// del tutorial), y lo devuelve a 1 al ganar o perder.
    /// </summary>
    public static class TiempoLento
    {
        /// <summary>Velocidad del tiempo en cámara lenta (1 = normal).</summary>
        public static float Factor = 0.3f;

        /// <summary>Segundos reales que tarda en frenar o en volver a la normalidad.</summary>
        public static float Duracion = 0.45f;

        private static readonly HashSet<object> motivos = new HashSet<object>();
        private static TiempoLentoRunner runner;

        public static bool Activo => motivos.Count > 0;

        public static void Pedir(object motivo)
        {
            if (motivo == null) return;
            motivos.Add(motivo);
            Asegurar();
        }

        public static void Soltar(object motivo)
        {
            if (motivo != null) motivos.Remove(motivo);
        }

        internal static void Limpiar()
        {
            motivos.RemoveWhere(m => m is Object o && o == null);
        }

        internal static void SoltarTodo() => motivos.Clear();

        private static void Asegurar()
        {
            if (runner != null) return;
            Limpiar(); // lo que quedó de la escena anterior
            runner = new GameObject("TiempoLento").AddComponent<TiempoLentoRunner>();
        }

        internal static void RunnerDestruido(TiempoLentoRunner r)
        {
            if (runner == r) runner = null;
        }
    }

    /// <summary>Mueve Time.timeScale hacia el valor que pide TiempoLento. Se crea solo.</summary>
    public class TiempoLentoRunner : MonoBehaviour
    {
        private void OnDestroy() => TiempoLento.RunnerDestruido(this);

        private void Update()
        {
            TiempoLento.Limpiar();

            // Congelado por la pausa o el tutorial: no se toca (ellos restauran su valor).
            if (Time.timeScale == 0f) return;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return;

            bool enJuego = GameStateManager.Instance == null || GameStateManager.Instance.CurrentState == GameState.Juego;
            if (!enJuego)
            {
                TiempoLento.SoltarTodo();
                Time.timeScale = 1f;
                return;
            }

            float objetivo = TiempoLento.Activo ? TiempoLento.Factor : 1f;
            if (Mathf.Approximately(Time.timeScale, objetivo)) return;

            float paso = Mathf.Abs(1f - TiempoLento.Factor) * Time.unscaledDeltaTime / Mathf.Max(0.01f, TiempoLento.Duracion);
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, objetivo, paso);
        }
    }
}
