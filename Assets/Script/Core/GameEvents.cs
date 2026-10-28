using System;
using UnityEngine;

namespace Terror
{
    // Contrato de eventos compartido entre los sistemas (fósforos, Presencia, audio, UI).
    // Cada sistema se conecta solo a través de estos eventos estáticos, para evitar
    // dependencias directas entre escenas.
    public static class GameEvents
    {
        // Se dispara cada vez que el jugador enciende un fósforo.
        public static event Action OnFosforoEncendido;

        // Se dispara cuando el fósforo se apaga (se consumió o el jugador lo apagó con Q).
        public static event Action OnFosforoApagado;

        // Compatibilidad con scripts viejos de Presencia (PresenciaAudio, PresenciaHUD,
        // AudioJuegoL1). Desde 2026-10-22 ya no hay barra de miedo: el segundo valor
        // siempre es 1 y no significa nada. Lo nuevo debe usar OnNivelPresenciaCambiado.
        public static event Action<int, float> OnCercaniaPresenciaCambiada;

        // La Presencia subió de nivel: uno por cada fósforo consumido (1, 2, 3...).
        // Es lo único que avanza en el cuarto; cada nivel trae sus propios efectos
        // (ver PresenciaManager y EfectosPresencia).
        public static event Action<int> OnNivelPresenciaCambiado;

        // Sonó un ruido de la Presencia (pasos, golpes). cercania va de 0 a 1:
        // sirve para efectos chicos que acompañan al sonido (una micro-sacudida).
        public static event Action<float> OnRuidoPresencia;

        public static void RaiseFosforoEncendido()
        {
            OnFosforoEncendido?.Invoke();
        }

        public static void RaiseFosforoApagado()
        {
            OnFosforoApagado?.Invoke();
        }

        public static void RaiseCercaniaPresenciaCambiada(int nivel, float multiplicador)
        {
            OnCercaniaPresenciaCambiada?.Invoke(nivel, multiplicador);
        }

        public static void RaiseNivelPresenciaCambiado(int nivel)
        {
            OnNivelPresenciaCambiado?.Invoke(nivel);
        }

        public static void RaiseRuidoPresencia(float cercania)
        {
            OnRuidoPresencia?.Invoke(Mathf.Clamp01(cercania));
        }
    }
}
