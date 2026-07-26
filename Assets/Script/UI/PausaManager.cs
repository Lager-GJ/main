using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror
{
    /// <summary>
    /// Pausa manual del juego, invocable por el jugador en cualquier momento (P o
    /// Escape) — a diferencia de ManagerTutorial/TutorialManager, que solo pausan una
    /// vez al inicio de forma forzada. Usa Time.timeScale = 0, el mismo patrón que ya
    /// usan esos dos scripts, así que es consistente con el resto del proyecto.
    /// No modifica ningún script existente.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Tooltip("Panel de UI con las opciones de pausa (Reanudar / Reiniciar / Salir). Puede quedar vacío mientras no exista el diseño final.")]
        [SerializeField] private GameObject panelPausa;

        public bool EstaPausado { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panelPausa != null)
                panelPausa.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            bool teclaPausa = Keyboard.current.pKey.wasPressedThisFrame ||
                               Keyboard.current.escapeKey.wasPressedThisFrame;

            if (!teclaPausa)
                return;

            // Si el panel de inspección está abierto, dejamos que Escape lo cierre a
            // él primero (su propio Update ya lo maneja) — no pausamos encima en el
            // mismo click para evitar que las dos cosas reaccionen a la vez.
            if (!EstaPausado && PanelInspeccion.Instance != null && PanelInspeccion.Instance.EstaAbierto)
                return;

            // Solo se puede iniciar la pausa durante la partida (no en Inicio/Derrota/Victoria).
            if (!EstaPausado && GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState != GameState.Juego)
                return;

            if (EstaPausado)
                Reanudar();
            else
                Pausar();
        }

        public void Pausar()
        {
            EstaPausado = true;
            Time.timeScale = 0f;

            if (panelPausa != null)
                panelPausa.SetActive(true);
        }

        /// <summary>Conectar también al botón "Reanudar" del panel de pausa.</summary>
        public void Reanudar()
        {
            EstaPausado = false;
            Time.timeScale = 1f;

            if (panelPausa != null)
                panelPausa.SetActive(false);
        }
    }
}