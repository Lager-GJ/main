using System;
using System.Collections.Generic;

namespace Terror
{
    /// <summary>
    /// Datos persistentes del jugador: que leyendas desbloqueo/completo y sus
    /// volumenes. Lo serializa SaveSystem a JSON.
    ///
    /// Ojo con los valores por defecto: JsonUtility los pisa al deserializar, asi
    /// que un perfil guardado con la lista vacia NO recupera "L1_CajaFosforos".
    /// Por eso el desbloqueo de la Leyenda 1 no depende de esta lista sino del flag
    /// desbloqueadaPorDefecto de su LeyendaDefinicion (ver MenuPrincipal).
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
