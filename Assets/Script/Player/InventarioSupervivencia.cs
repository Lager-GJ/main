using UnityEngine;
using Terror;

/// <summary>
/// Lleva la cuenta de los objetos de supervivencia (los caramelos) que junta el niño.
///
/// La forma de ganar el cuarto 1 es salir por la puerta con su llave (cadena de
/// acertijos, 2026-10-22). Los caramelos no tienen función por ahora: quedan como
/// detalle del cuarto hasta que el equipo decida qué hacer con ellos (la barra de
/// miedo, que ralentizaban, se eliminó el 2026-10-22).
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

    [Tooltip("Apagado: la única victoria es salir por la puerta. Marcado: juntar todos los objetos también gana (regla vieja).")]
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
