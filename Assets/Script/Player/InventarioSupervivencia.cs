using UnityEngine;
using Terror;

/// <summary>
/// Lleva la cuenta de los objetos de supervivencia (los caramelos) que junta el niño.
///
/// La única forma de ganar el cuarto 1 es abrir la lata de galletas del armario
/// (ObjetoInteractivo con esObjetivoDeVictoria + SecuenciaAperturaLata). Decisión de
/// David, 2026-10-19 (ya se había decidido el 2026-07-31 y volvió a entrar con el PR #3).
/// Juntar los caramelos solo ralentiza el miedo en el modo continuo de FearManager;
/// en el modo por fósforos (el de ahora) no cambia nada.
///
/// La victoria por juntar todos (la versión de Anavi) queda detrás de
/// ganarAlJuntarTodos, apagada, por si el equipo la quiere probar otra vez.
/// </summary>
public class InventarioSupervivencia : MonoBehaviour
{
    public static InventarioSupervivencia Instance { get; private set; }

    [Header("Progreso")]
    [Tooltip("Cantidad de objetos recolectados actualmente.")]
    public int itemsRecolectados = 0;

    [Tooltip("Cuántos objetos hay en el cuarto. Solo gana al juntarlos si 'Ganar Al Juntar Todos' está marcado.")]
    public int itemsParaGanar = 4;

    [Tooltip("Apagado: la única victoria es la lata de galletas. Marcado: juntar todos los objetos también gana (regla vieja).")]
    public bool ganarAlJuntarTodos = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RecolectarItem()
    {
        itemsRecolectados++;
        Debug.Log($"[Inventario] Ítem de supervivencia recolectado. Total: {itemsRecolectados}/{itemsParaGanar}");

        // Ralentiza el nivel de miedo en un 30% adicional (multiplicativo)
        if (FearManager.Instance != null)
        {
            FearManager.Instance.ReducirVelocidadSubida(0.3f);
        }

        if (ganarAlJuntarTodos && itemsRecolectados >= itemsParaGanar)
        {
            Debug.Log("[Victoria] Se recolectaron todos los objetos de supervivencia. Activando victoria.");
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.Ganar();
            }
        }
    }
}
