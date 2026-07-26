using System;
using UnityEngine;

/// <summary>
/// La Presencia: la entidad que se "acerca" mientras el niño usa fósforos.
/// Regla oficial del MVP: cada fósforo encendido aumenta el riesgo. Además, el riesgo
/// sigue subiendo mientras el fósforo permanece encendido (no solo al prenderlo) —
/// así apagarlo antes con Q (ver FosforoManager) tiene un beneficio real: menos
/// segundos de luz encendida, menos riesgo acumulado.
/// No dibuja nada de la Presencia (sprite, animación, sonido) — eso lo arma quien
/// se encargue del arte/IA de la entidad, enganchándose a los eventos de abajo.
/// </summary>
public class PresenciaManager : MonoBehaviour
{
    public static PresenciaManager Instance { get; private set; }

    [Header("Configuración de riesgo (0 = a salvo, 1 = atrapado)")]
    [Tooltip("Cuánto sube el riesgo de una sola vez cada vez que se enciende un fósforo.")]
    [SerializeField] private float riesgoPorEncendido = 0.08f;

    [Tooltip("Cuánto sube el riesgo por segundo mientras el fósforo sigue encendido.")]
    [SerializeField] private float riesgoPorSegundoEncendido = 0.03f;

    [Tooltip("Cuánto baja el riesgo por segundo mientras el fósforo está apagado (a oscuras, la Presencia pierde el rastro poco a poco).")]
    [SerializeField] private float recuperacionPorSegundoApagado = 0.015f;

    // --- Estado interno ---
    private float riesgo;

    // --- Eventos estáticos ---
    // Mismo patrón que FosforoManager: la barra de miedo, el audio, o la animación
    // de la Presencia se suscriben a esto sin depender directamente de esta clase.
    public static event Action<float> OnRiesgoCambiado;
    public static event Action OnJugadorAtrapado;

    public float Riesgo => riesgo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        FosforoManager.OnFosforoEncendido += ManejarEncendido;
    }

    private void OnDisable()
    {
        FosforoManager.OnFosforoEncendido -= ManejarEncendido;
    }

    private void Update()
    {
        if (FosforoManager.Instance == null)
            return;

        if (FosforoManager.Instance.Encendido)
            SubirRiesgo(riesgoPorSegundoEncendido * Time.deltaTime);
        else
            SubirRiesgo(-recuperacionPorSegundoApagado * Time.deltaTime);
    }

    private void ManejarEncendido()
    {
        SubirRiesgo(riesgoPorEncendido);
    }

    private void SubirRiesgo(float cantidad)
    {
        float riesgoAnterior = riesgo;
        riesgo = Mathf.Clamp01(riesgo + cantidad);

        if (!Mathf.Approximately(riesgoAnterior, riesgo))
            OnRiesgoCambiado?.Invoke(riesgo);

        // TEMPORAL: mientras no exista pantalla de game over, este log confirma que
        // la lógica de riesgo funciona. Se puede borrar cuando haya un game over real.
        if (riesgo >= 1f && riesgoAnterior < 1f)
        {
            Debug.Log("[Presencia] Atrapó al niño. GAME OVER.");
            OnJugadorAtrapado?.Invoke();
        }
    }
}