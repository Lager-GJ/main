using System;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Guardado del perfil en JSON. Sin versionado ni encriptacion: es el guardado
    /// de un MVP, no un sistema anti-trampas.
    ///
    /// Usa PlayerPrefs y no File.WriteAllText a proposito. El destino del juego es
    /// WebGL (itch.io), y ahi Application.persistentDataPath escribe a un sistema de
    /// archivos que se vuelca a IndexedDB de forma ASINCRONA: si el jugador cierra la
    /// pestaña poco despues de mover un slider, ese guardado se pierde. Es el clasico
    /// "se me borraron los datos" de los juegos web. PlayerPrefs.Save() es el volcado
    /// sincrono explicito y no tiene ese problema.
    /// </summary>
    public static class SaveSystem
    {
        private const string Clave = "perfil";

        public static PerfilJugador Cargar()
        {
            string json = PlayerPrefs.GetString(Clave, string.Empty);

            if (string.IsNullOrEmpty(json))
                return new PerfilJugador();

            try
            {
                PerfilJugador perfil = JsonUtility.FromJson<PerfilJugador>(json);
                return perfil ?? new PerfilJugador();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Perfil guardado ilegible, se arranca de cero. {e.Message}");
                return new PerfilJugador();
            }
        }

        public static void Guardar(PerfilJugador perfil)
        {
            if (perfil == null) return;

            PlayerPrefs.SetString(Clave, JsonUtility.ToJson(perfil));
            PlayerPrefs.Save(); // volcado sincrono: sin esto WebGL puede perderlo
        }

        /// <summary>Marca una leyenda como completada y guarda. Idempotente.</summary>
        public static void MarcarCompletada(string idLeyenda)
        {
            if (string.IsNullOrEmpty(idLeyenda)) return;

            PerfilJugador perfil = Cargar();
            if (!perfil.leyendasCompletadas.Contains(idLeyenda))
            {
                perfil.leyendasCompletadas.Add(idLeyenda);
                Guardar(perfil);
            }
        }

        /// <summary>Desbloquea una leyenda y guarda. Idempotente.</summary>
        public static void Desbloquear(string idLeyenda)
        {
            if (string.IsNullOrEmpty(idLeyenda)) return;

            PerfilJugador perfil = Cargar();
            if (!perfil.leyendasDesbloqueadas.Contains(idLeyenda))
            {
                perfil.leyendasDesbloqueadas.Add(idLeyenda);
                Guardar(perfil);
            }
        }

        /// <summary>Borra el perfil. Util para probar el arranque en limpio.</summary>
        public static void Borrar()
        {
            PlayerPrefs.DeleteKey(Clave);
            PlayerPrefs.Save();
        }
    }
}
