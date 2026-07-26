using UnityEngine;
using Terror; // Necesario para acceder a GameStateManager y GameState

public class PantallaDerrota : MonoBehaviour
{
    [Tooltip("Arrastra aquí el panel o imagen que contiene tu jumpscare")]
    public GameObject panelJumpscare;

    private void Start()
    {
        // Aseguramos que el panel inicie apagado
        if (panelJumpscare != null)
        {
            panelJumpscare.SetActive(false);
        }

        // Nos suscribimos al cambio de estado del juego
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += ManejarCambioEstado;
        }
    }

    private void OnDestroy()
    {
        // Limpiamos la suscripción al destruir el objeto
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= ManejarCambioEstado;
        }
    }

    private void ManejarCambioEstado(GameState estado)
    {
        // Si el estado cambia a Derrota, prendemos el panel
        if (estado == GameState.Derrota)
        {
            if (panelJumpscare != null)
            {
                panelJumpscare.SetActive(true);
            }
        }
    }
}
