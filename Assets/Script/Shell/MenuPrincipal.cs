using UnityEngine;
using UnityEngine.SceneManagement;

namespace Terror
{
    /// <summary>
    /// Cerebro del menu: decide que cuartos estan desbloqueados y navega al
    /// elegido. Va en la misma escena Intro.unity -- no hace falta una escena de
    /// Boot separada, esto reemplaza/complementa el boton "Nuevo Juego" de
    /// Scriptcambio con las 4 tarjetas de cuartos.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        private PerfilJugador perfil;

        private void Awake()
        {
            // En Awake y no en Start a proposito: MenuTarjetaLeyenda consulta
            // EstaDesbloqueada() desde su propio Start(), y Unity garantiza que
            // TODOS los Awake() corren antes que CUALQUIER Start().
            perfil = SaveSystem.Cargar();

            if (AudioManager.Instance != null)
                AudioManager.Instance.CargarDesde(perfil);
        }

        public bool EstaDesbloqueada(LeyendaDefinicion leyenda)
        {
            if (leyenda == null) return false;

            return leyenda.desbloqueadaPorDefecto
                || (perfil != null && perfil.leyendasDesbloqueadas.Contains(leyenda.id));
        }

        public bool EstaCompletada(LeyendaDefinicion leyenda)
        {
            if (leyenda == null || perfil == null) return false;
            return perfil.leyendasCompletadas.Contains(leyenda.id);
        }

        public void EntrarACuarto(LeyendaDefinicion leyenda)
        {
            if (leyenda == null || !EstaDesbloqueada(leyenda))
                return;

            if (string.IsNullOrEmpty(leyenda.nombreEscena))
            {
                Debug.LogWarning($"[MenuPrincipal] '{leyenda.nombre}' no tiene escena asignada; no se puede entrar.");
                return;
            }

            SceneManager.LoadScene(leyenda.nombreEscena);
        }
    }
}
