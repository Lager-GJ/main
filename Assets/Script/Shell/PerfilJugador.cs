using System;
using System.Collections.Generic;

namespace Terror
{
    /// <summary>
    /// Datos persistentes del jugador: que cuartos desbloqueo/completo y sus
    /// volumenes. Lo serializa SaveSystem a JSON.
    /// </summary>
    [Serializable]
    public class PerfilJugador
    {
        public List<string> leyendasDesbloqueadas = new List<string> { "L1_CajaFosforos" };
        public List<string> leyendasCompletadas = new List<string>();

        public float volMaster = 1f;
        public float volMusica = 1f;
        public float volSfx = 1f;
    }
}
