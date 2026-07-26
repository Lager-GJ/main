using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Muestra el panel de Victoria o Derrota al terminar la partida, con los botones
    /// para reintentar o volver al menu. Antes de esto el jugador quedaba atrapado en
    /// la escena sin salida.
    ///
    /// Los dos paneles tienen que arrancar desactivados en la escena.
    /// </summary>
    public class PanelResultado : MonoBehaviour
    {
        [SerializeField] private GameObject panelVictoria;
        [SerializeField] private GameObject panelDerrota;

        private void Start()
        {
            if (panelVictoria != null) panelVictoria.SetActive(false);
            if (panelDerrota != null) panelDerrota.SetActive(false);

            // En Start y no en OnEnable: asi el Awake de GameStateManager ya corrio y
            // su Instance existe.
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += ManejarCambioEstado;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= ManejarCambioEstado;
        }

        private void ManejarCambioEstado(GameState nuevo)
        {
            if (nuevo == GameState.Victoria && panelVictoria != null)
                panelVictoria.SetActive(true);
            else if (nuevo == GameState.Derrota && panelDerrota != null)
                panelDerrota.SetActive(true);
        }

        public void Reintentar()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.Reiniciar();
        }

        public void VolverAlMenu()
        {
            if (SceneRouter.Instance != null)
                SceneRouter.Instance.CargarEscena(Escenas.Menu);
            else
                Debug.LogError("[PanelResultado] No hay SceneRouter. ¿Arrancaste el juego desde 00_Boot?");
        }
    }
}
