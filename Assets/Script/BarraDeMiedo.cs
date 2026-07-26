using System;
using UnityEngine;

/// <summary>
/// Barra de miedo del niño. Regla oficial del MVP: sube en la oscuridad y con la
/// cercanía de la Presencia (usamos su "riesgo" como proxy de cercanía), y baja
/// levemente mientras hay luz. Al llegar a 1.0, el niño ya no aguanta más.
/// </summary>
public class BarraDeMiedo : MonoBehaviour
{
    public static BarraDeMiedo Instance { get; private set; }

    [Header("Configuración de miedo (0 = tranquilo, 1 = game over)")]
    [Tooltip("Cuánto sube el miedo por segundo estando a oscuras (fósforo apagado).")]
    [SerializeField] private float miedoPorSegundoOscuridad = 0.05f;

    [Tooltip("Cuánto baja el miedo por segundo mientras hay luz encendida.")]
    [SerializeField] private float alivioPorSegundoConLuz = 0.03f;

    [Tooltip("Qué tanto pesa el riesgo actual de la Presencia al subir el miedo en la oscuridad.")]
    [SerializeField] private float influenciaRiesgoPresencia = 0.5f;

    private float miedo;

    // Mismo patrón de Action estáticos: la UI de la barra (de otro dev, o tú mismo
    // después) se suscribe a OnMiedoCambiado para pintarse, sin acoplarse a esta clase.
    public static event Action<float> OnMiedoCambiado;
    public static event Action OnMiedoMaximo;

    public float Miedo => miedo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (FosforoManager.Instance == null)
            return;

        if (FosforoManager.Instance.Encendido)
        {
            CambiarMiedo(-alivioPorSegundoConLuz * Time.deltaTime);
        }
        else
        {
            float riesgoActual = PresenciaManager.Instance != null ? PresenciaManager.Instance.Riesgo : 0f;
            float subida = (miedoPorSegundoOscuridad + riesgoActual * influenciaRiesgoPresencia) * Time.deltaTime;
            CambiarMiedo(subida);
        }
    }

    private void CambiarMiedo(float cantidad)
    {
        float anterior = miedo;
        miedo = Mathf.Clamp01(miedo + cantidad);

        if (!Mathf.Approximately(anterior, miedo))
            OnMiedoCambiado?.Invoke(miedo);

        // TEMPORAL: log de depuración mientras no haya UI de la barra ni pantalla de game over.
        if (miedo >= 1f && anterior < 1f)
        {
            Debug.Log("[Miedo] El niño no pudo más. GAME OVER.");
            OnMiedoMaximo?.Invoke();
        }
    }
}