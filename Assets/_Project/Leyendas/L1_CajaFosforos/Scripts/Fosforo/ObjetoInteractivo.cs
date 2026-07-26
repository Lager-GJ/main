using System;
using UnityEngine;
using Terror;

/// <summary>
/// Clase base para cualquier objeto clickeable del escenario (llave, caja de dulces, etc.).
/// Aplica la regla obligatoria del MVP: solo se puede interactuar mientras hay un
/// fósforo encendido. Este script NO dibuja el panel de inspección (eso lo arma otro
/// dev) — solo decide si el click es válido y avisa mediante un evento estático con
/// los datos del objeto, para no acoplarse al script del panel.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObjetoInteractivo : MonoBehaviour
{
    [Header("Datos para el panel de inspección")]
    [Tooltip("Nombre que se muestra en el panel de inspección.")]
    [SerializeField] private string nombreObjeto;

    [TextArea]
    [Tooltip("Descripción temática que se muestra al inspeccionar este objeto.")]
    [SerializeField] private string descripcion;

    [Tooltip("Marcar true si este objeto es un objetivo de victoria (llave / caja de dulces).")]
    [SerializeField] private bool esObjetivoDeVictoria;

    public string NombreObjeto => nombreObjeto;
    public string Descripcion => descripcion;
    public bool EsObjetivoDeVictoria => esObjetivoDeVictoria;

    // El script del panel de inspección (de otro dev) se suscribe a esto para saber
    // CUÁNDO y QUÉ mostrar, sin que ObjetoInteractivo conozca cómo es ese panel.
    // Se mantiene el mismo patrón de Action estáticos que usa FosforoManager.
    public static event Action<ObjetoInteractivo> OnObjetoInspeccionado;
    public static event Action OnInspeccionCerrada;

    private Renderer[] renderers;
    private UnityEngine.UI.Graphic[] graficosUI;

    private void Awake()
    {
        // Buscamos cualquier componente visual (Sprite, Mesh o UI) en este objeto y en sus hijos
        renderers = GetComponentsInChildren<Renderer>();
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
        // Al arrancar, comprobamos si ya hay un fósforo encendido
        if (FosforoManager.Instance != null && FosforoManager.Instance.PuedeInteractuar())
            Mostrar();
        else
            Ocultar();
    }

    private void Mostrar()
    {
        foreach (var r in renderers) if (r != null) r.enabled = true;
        foreach (var g in graficosUI) if (g != null) g.enabled = true;
    }

    private void Ocultar()
    {
        foreach (var r in renderers) if (r != null) r.enabled = false;
        foreach (var g in graficosUI) if (g != null) g.enabled = false;
    }

    private void OnMouseDown()
    {
        Debug.Log("[ObjetoInteractivo] ¡El ratón ha hecho clic (OnMouseDown) sobre " + gameObject.name + "!");

        // OnMouseDown funciona igual con el Input System nuevo: no pasa por la clase
        // UnityEngine.Input, así que no rompe aunque el proyecto tenga
        // "Active Input Handling" en modo "Input System Package (New)".
        if (FosforoManager.Instance == null)
        {
            Debug.LogWarning("[ObjetoInteractivo] Error: No se encontró FosforoManager.Instance");
            return;
        }

        // Regla obligatoria del MVP: sin fósforo encendido no se puede interactuar.
        if (!FosforoManager.Instance.PuedeInteractuar())
        {
            Debug.Log("[ObjetoInteractivo] Clic rechazado: Fosforo no está encendido.");
            return;
        }

        Inspeccionar();
    }

    // Método público alternativo por si el usuario usa un Button o EventTrigger en lugar de Collider
    public void InteractuarManual()
    {
        Debug.Log("[ObjetoInteractivo] ¡Clic detectado por InteractuarManual() en " + gameObject.name + "!");
        
        if (FosforoManager.Instance == null) return;
        if (!FosforoManager.Instance.PuedeInteractuar()) return;

        Inspeccionar();
    }

    private void Inspeccionar()
    {
        // Congelamos el consumo del fósforo mientras dura la inspección: el jugador
        // no debería perder luz solo por leer la descripción de un objeto.
        FosforoManager.Instance.PausarQuemado();

        // TEMPORAL: mientras no exista el panel de inspección, este log confirma
        // en la Console que el click sobre el objeto fue válido. Se puede borrar después.
        Debug.Log($"[Inspección] Inspeccionando: {nombreObjeto}");

        OnObjetoInspeccionado?.Invoke(this);

        // ¡AQUÍ ESTÁ LA CONDICIÓN DE VICTORIA!
        if (esObjetivoDeVictoria && GameStateManager.Instance != null)
        {
            Debug.Log("[Victoria] ¡Encontraste el objetivo final y lo agarraste!");
            
            // Hacemos que el objeto desaparezca para simular que lo hemos recogido
            gameObject.SetActive(false);

            GameStateManager.Instance.Ganar();
        }
    }

    /// <summary>
    /// El script del panel de inspección debe llamar este método al cerrarse
    /// (por ejemplo, al clickear "volver" o presionar Escape).
    /// </summary>
    public void CerrarInspeccion()
    {
        if (FosforoManager.Instance != null)
            FosforoManager.Instance.ReanudarQuemado();

        OnInspeccionCerrada?.Invoke();
    }
}