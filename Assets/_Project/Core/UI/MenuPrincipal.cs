using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Cerebro del menu principal: decide que leyendas estan desbloqueadas y navega
    /// a la elegida. La parte visual (las 5 tarjetas) la arma el Editor — ver
    /// MenuTarjetaLeyenda, que es el componente que va en cada una.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        private PerfilJugador perfil;

        private void Awake()
        {
            // En Awake y no en Start a proposito: MenuTarjetaLeyenda consulta
            // EstaDesbloqueada() desde su propio Start(), y Unity no garantiza el
            // orden entre los Start() de distintos scripts. Lo que si garantiza es
            // que TODOS los Awake() corren antes que CUALQUIER Start().
            perfil = SaveSystem.Cargar();

            if (AudioManager.Instance != null)
                AudioManager.Instance.CargarDesde(perfil);
        }

        public bool EstaDesbloqueada(LeyendaDefinicion leyenda)
        {
            if (leyenda == null) return false;

            // El flag manda sobre la lista guardada: asi la Leyenda 1 sigue abierta
            // aunque un perfil viejo tenga la lista vacia (JsonUtility pisa los
            // valores por defecto al deserializar — ver PerfilJugador).
            return leyenda.desbloqueadaPorDefecto
                || (perfil != null && perfil.leyendasDesbloqueadas.Contains(leyenda.id));
        }

        public bool EstaCompletada(LeyendaDefinicion leyenda)
        {
            if (leyenda == null || perfil == null) return false;
            return perfil.leyendasCompletadas.Contains(leyenda.id);
        }

        public void EntrarALeyenda(LeyendaDefinicion leyenda)
        {
            if (leyenda == null || !EstaDesbloqueada(leyenda))
                return;

            // Las bloqueadas no tienen escena asignada; esto es el ultimo cortafuegos
            // por si alguna quedara marcada como desbloqueada por error.
            if (string.IsNullOrEmpty(leyenda.nombreEscena))
            {
                Debug.LogWarning($"[MenuPrincipal] '{leyenda.nombre}' no tiene escena asignada; no se puede entrar.");
                return;
            }

            if (SceneRouter.Instance != null)
                SceneRouter.Instance.CargarEscena(leyenda.nombreEscena);
            else
                Debug.LogError("[MenuPrincipal] No hay SceneRouter. ¿Arrancaste el juego desde 00_Boot?");
        }
    }
}
