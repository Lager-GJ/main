using UnityEngine;
using Terror; // Necesario para acceder a FosforoManager

/// <summary>
/// Se coloca en los objetos coleccionables de la escena.
/// Requiere un Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemSupervivencia : MonoBehaviour
{
    private bool yaRecolectado = false;
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private UnityEngine.UI.Graphic[] graficosUI;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider2D>();
        graficosUI = GetComponentsInChildren<UnityEngine.UI.Graphic>();
    }

    private void OnEnable()
    {
        Terror.GameEvents.OnFosforoEncendido += Mostrar;
        Terror.GameEvents.OnFosforoApagado += Ocultar;
    }

    private void OnDisable()
    {
        Terror.GameEvents.OnFosforoEncendido -= Mostrar;
        Terror.GameEvents.OnFosforoApagado -= Ocultar;
    }

    private void Start()
    {
        if (FosforoManager.Instance != null && FosforoManager.Instance.PuedeInteractuar())
            Mostrar();
        else
            Ocultar();
    }

    private void Mostrar()
    {
        if (yaRecolectado) return;
        
        foreach (var r in renderers) if (r != null) r.enabled = true;
        foreach (var c in colliders) if (c != null) c.enabled = true;
        foreach (var g in graficosUI) if (g != null) g.enabled = true;
    }

    private void Ocultar()
    {
        foreach (var r in renderers) if (r != null) r.enabled = false;
        foreach (var c in colliders) if (c != null) c.enabled = false;
        foreach (var g in graficosUI) if (g != null) g.enabled = false;
    }

    private void OnMouseDown()
    {
        Debug.Log($"[ItemSupervivencia] Detectado clic en {gameObject.name}");
        Interactuar();
    }

    // Método expuesto por si se requiere llamar mediante UI (ej: Button o EventTrigger)
    public void Interactuar()
    {
        if (yaRecolectado) return;

        // Regla del MVP: Solo se puede interactuar si hay luz
        if (FosforoManager.Instance == null || !FosforoManager.Instance.PuedeInteractuar())
        {
            Debug.Log("[ItemSupervivencia] Clic rechazado: No se puede recoger en la oscuridad.");
            return;
        }

        yaRecolectado = true;

        if (InventarioSupervivencia.Instance != null)
        {
            InventarioSupervivencia.Instance.RecolectarItem();
        }
        else
        {
            Debug.LogWarning("No se encontró el InventarioSupervivencia en la escena. Asegúrate de agregarlo a un GameObject Manager.");
        }

        // Ocultamos/Destruimos el objeto tras recolectarlo
        gameObject.SetActive(false);
    }
}
