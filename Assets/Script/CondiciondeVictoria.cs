using UnityEngine;
using Terror; // Necesario para acceder a GameStateManager y FearManager

public class CondiciondeVictoria : MonoBehaviour
{
    [Tooltip("El objeto visual (Panel/Canvas) que muestra el mensaje de victoria.")]
    public GameObject panelVictoria;

    private void Start()
    {
        // 1. Ocultar la pantalla de victoria al inicio
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(false);
        }

        // Suscribirse a los cambios de estado del juego
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += VerificarCondicionesDeVictoria;
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= VerificarCondicionesDeVictoria;
        }
    }

    private void VerificarCondicionesDeVictoria(GameState nuevoEstado)
    {
        // Cuando ObjetoInteractivo.cs recolecta el objeto clave, cambia el estado a Victoria.
        // Aquí interceptamos ese momento para verificar las condiciones extras.
        if (nuevoEstado == GameState.Victoria)
        {
            // Condición 1: Ya se recolectó el objeto (por eso entramos a este if)

            // Condición 2: ¿Aún le quedan fósforos? (O tiene uno actualmente encendido)
            bool tieneFosforos = FosforoManager.Instance.FosforosRestantes > 0 || FosforoManager.Instance.Encendido;

            // Condición 3: ¿Su nivel de miedo es menor al 100%?
            bool noEstaAterrorizado = FearManager.Instance.miedoActual < 100f;

            if (tieneFosforos && noEstaAterrorizado)
            {
                // ¡Todas las condiciones se cumplen! Mostramos la pantalla.
                MostrarVictoria();
            }
            else
            {
                // Si agarró el objeto pero ya no tiene fósforos o su miedo llegó a 100
                // Forzamos la derrota.
                Debug.Log("Objeto recolectado, pero sin fósforos o con miedo al máximo. ¡Derrota!");
                GameStateManager.Instance.Perder();
            }
        }
    }

    private void MostrarVictoria()
    {
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }
        
        Debug.Log("¡Condiciones de victoria cumplidas! Mostrando pantalla.");
    }
}
