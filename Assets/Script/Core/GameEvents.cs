using System;
using UnityEngine;

namespace Terror
{
    // Contrato de eventos compartido entre los 3 sistemas (Dev A, B, C).
    // Cada dev trabaja en su propia escena de prueba y se conecta solo a traves
    // de estos eventos estaticos, para evitar dependencias directas entre escenas.
    public static class GameEvents
    {
        // Dev A -> Dev C: se dispara cada vez que el jugador enciende un fosforo.
        public static event Action OnFosforoEncendido;

        // Dev A -> quien lo necesite: se dispara cuando el fosforo se apaga (fin del temporizador).
        public static event Action OnFosforoApagado;

        // Dev C -> Dev B: nivel de cercania actual y el multiplicador que debe aplicar
        // Dev B a la velocidad de subida de la barra de miedo.
        public static event Action<int, float> OnCercaniaPresenciaCambiada;

        public static void RaiseFosforoEncendido()
        {
            OnFosforoEncendido?.Invoke();
        }

        public static void RaiseFosforoApagado()
        {
            OnFosforoApagado?.Invoke();
        }

        public static void RaiseCercaniaPresenciaCambiada(int nivel, float multiplicadorMiedo)
        {
            OnCercaniaPresenciaCambiada?.Invoke(nivel, multiplicadorMiedo);
        }
    }
}
