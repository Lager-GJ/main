using UnityEngine;
using Terror; // GameStateManager

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

            // Ya no hay barra de miedo (2026-10-22). La derrota por fósforos la
            // decide la Presencia al consumirse el último, así que si se llegó a
            // Victoria es porque se salió a tiempo: solo queda mostrar la pantalla.
            MostrarVictoria();
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
