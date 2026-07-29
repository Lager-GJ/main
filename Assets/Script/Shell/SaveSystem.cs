using System;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Guardado del perfil en JSON via PlayerPrefs (no File.WriteAllText): el
    /// destino es WebGL, donde persistentDataPath vuelca a IndexedDB de forma
    /// asincrona y un cierre de pestaña temprano pierde el guardado.
    /// PlayerPrefs.Save() es el volcado sincrono explicito.
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
            PlayerPrefs.Save();
        }

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

        public static void Borrar()
        {
            PlayerPrefs.DeleteKey(Clave);
            PlayerPrefs.Save();
        }
    }
}
