using UnityEngine;
using Terror;

public class InventarioSupervivencia : MonoBehaviour
{
    public static InventarioSupervivencia Instance { get; private set; }

    [Header("Progreso")]
    [Tooltip("Cantidad de objetos recolectados actualmente.")]
    public int itemsRecolectados = 0;
    
    [Tooltip("Cantidad de objetos necesarios para ganar la partida.")]
    public int itemsParaGanar = 4;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

        // Condición de victoria al obtener los 4
        if (itemsRecolectados >= itemsParaGanar)
        {
            Debug.Log("[Victoria] Se recolectaron todos los objetos de supervivencia. Activando victoria.");
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.Ganar();
            }
        }
    }
}
