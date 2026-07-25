using System;
using UnityEngine;

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

    private void OnMouseDown()
    {
        // OnMouseDown funciona igual con el Input System nuevo: no pasa por la clase
        // UnityEngine.Input, así que no rompe aunque el proyecto tenga
        // "Active Input Handling" en modo "Input System Package (New)".
        if (FosforoManager.Instance == null)
            return;

        // Regla obligatoria del MVP: sin fósforo encendido no se puede interactuar.
        if (!FosforoManager.Instance.PuedeInteractuar())
            return;

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