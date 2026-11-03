using System;
using System.Collections.Generic;

namespace Terror
{
    /// <summary>
    /// Datos persistentes del jugador: que cuartos desbloqueo/completo y su
    /// configuracion (audio, imagen, controles, accesibilidad). Lo serializa
    /// SaveSystem a JSON. Un perfil viejo sin los campos nuevos los recibe con
    /// estos valores por defecto (JsonUtility respeta los inicializadores).
    /// </summary>
    [Serializable]
    public class PerfilJugador
    {
        public List<string> leyendasDesbloqueadas = new List<string> { "L1_CajaFosforos" };
        public List<string> leyendasCompletadas = new List<string>();

        // --- Audio (0..1) ---
        public float volMaster = 1f;
        public float volMusica = 1f;
        public float volSfx = 1f;

        // --- Imagen (-1..1, 0 = como está hecho el juego) ---
        public float brillo = 0f;
        public float contraste = 0f;

        // --- Accesibilidad ---
        /// <summary>0 = sin sacudidas, 1 = normal.</summary>
        public float intensidadSacudidas = 1f;
        /// <summary>Menos parpadeos y destellos de luz (tele, llama, estática).</summary>
        public bool reducirDestellos = false;
        /// <summary>0 = apagado, 1 = normal, 2 = alto contraste.</summary>
        public int resaltadoObjetos = 1;

        // --- Controles: "Accion=Tecla" (nombres de UnityEngine.InputSystem.Key) ---
        public List<string> teclas = new List<string>();
    }
}
