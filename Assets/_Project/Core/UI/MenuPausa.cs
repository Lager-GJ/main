using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror
{
    /// <summary>
    /// Menu de pausa de la partida: Reanudar / Reiniciar / Salir al menu.
    /// Se abre y cierra con Escape.
    ///
    /// El congelado no lo hace este script: lo hace GameStateManager.Pausar(), que
    /// combina Time.timeScale = 0 (para todo lo que avanza por deltaTime: fosforo,
    /// miedo, Presencia) con el estado Pausa (para el input, que el timeScale no
    /// detiene). Ver el comentario alli.
    ///
    /// En el Editor: los botones deben usar transicion ColorTint, no Animation — el
    /// Animator se congela con timeScale = 0 y los botones se verian muertos.
    /// </summary>
    public class MenuPausa : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        public bool EstaAbierto => panel != null && panel.activeSelf;

        private void Start()
        {
            // Por si quedo activo en la escena guardada.
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (EstaAbierto)
            {
                Cerrar();
                return;
            }

            // Solo se puede pausar a mitad de partida: con el panel de victoria o
            // derrota en pantalla, Escape no debe hacer nada.
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Juego)
                Abrir();
        }

        public void Abrir()
        {
            if (panel != null) panel.SetActive(true);
            if (GameStateManager.Instance != null) GameStateManager.Instance.Pausar();
        }

        public void Cerrar()
        {
            if (panel != null) panel.SetActive(false);
            if (GameStateManager.Instance != null) GameStateManager.Instance.Reanudar();
        }

        public void Reiniciar()
        {
            // Reiniciar() ya restaura el timeScale antes de recargar la escena.
            if (GameStateManager.Instance != null) GameStateManager.Instance.Reiniciar();
        }

        public void SalirAlMenu()
        {
            if (SceneRouter.Instance != null)
                SceneRouter.Instance.CargarEscena(Escenas.Menu);
            else
                Debug.LogError("[MenuPausa] No hay SceneRouter. ¿Arrancaste el juego desde 00_Boot?");
        }
    }
}
