using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Mouse.current: el proyecto usa el Input System nuevo (activeInputHandler = 1),
                               // así que UnityEngine.Input.GetMouseButtonDown tiraría una excepción en runtime.
using UnityEngine.Rendering.Universal; // Light2D vive aquí en URP.

/// <summary>
/// Núcleo mecánico del sistema de fósforos ("La Vela de la Abuela").
/// Controla el recurso limitado (fósforos), el ciclo de encendido/apagado con
/// temporizador, y expone el estado mediante eventos estáticos para que el resto
/// del equipo (Presencia, barra de miedo, UI, audio/VFX) reaccione sin acoplarse
/// directamente a esta clase.
/// </summary>
public class FosforoManager : MonoBehaviour
{
    // Singleton simple: para un MVP de una sola escena/habitación es suficiente
    // y evita que cada ObjetoInteractivo tenga que arrastrar una referencia manual.
    public static FosforoManager Instance { get; private set; }

    [Header("Configuración de fósforos")]
    [Tooltip("Fósforos disponibles al iniciar la partida. Es la perilla principal de dificultad del MVP.")]
    [SerializeField] private int fosforosIniciales = 5;

    [Tooltip("Segundos que dura encendido un fósforo antes de apagarse solo.")]
    [SerializeField] private float duracionFosforo = 20f;

    [Header("Referencias")]
    [Tooltip("Luz 2D que representa el fósforo encendido. Se activa/desactiva junto con el estado.")]
    [SerializeField] private Light2D luzFosforo;

    // --- Estado interno ---
    private int fosforosRestantes;
    private float tiempoRestanteQuemado;
    private bool encendido;
    private bool pausadoPorInspeccion; // true mientras el jugador tiene abierto el panel de inspección
    private Coroutine coroutineQuemado;
    private float intensidadBaseLuz; // intensidad que configuraste en el Inspector, se guarda una sola vez

    // --- Eventos estáticos ---
    // Se usan Action estáticos (no UnityEvents) a propósito: cualquier script del
    // equipo puede suscribirse sin necesitar una referencia directa a esta instancia.
    // Esto es clave porque Presencia y la barra de miedo se programan en paralelo
    // por otros devs y no deben acoplarse al sistema de fósforo.
    public static event Action OnFosforoEncendido;
    public static event Action OnFosforoApagado;
    public static event Action OnSinFosforos;

    // Notifica el conteo cada vez que cambia, para que la UI pinte el contador
    // sin hacer polling en su propio Update() ni pedir una referencia a este script.
    public static event Action<int> OnFosforosRestantesCambiado;

    public bool Encendido => encendido;
    public int FosforosRestantes => fosforosRestantes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        fosforosRestantes = fosforosIniciales;

        if (luzFosforo != null)
        {
            intensidadBaseLuz = luzFosforo.intensity; // recordamos el valor que dejaste en el Inspector
            luzFosforo.gameObject.SetActive(false); // arranca apagado: sin fósforo, sin luz
        }
    }

    private void Update()
    {
        // Encendido con la tecla E (y no con click) porque el click izquierdo ahora
        // lo usa el movimiento del niño (point & click) — si usáramos el mismo botón
        // para las dos cosas, cada vez que el jugador camina se gastaría un fósforo.
        bool teclaE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool teclaQ = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        if (teclaE && !encendido && fosforosRestantes > 0)
        {
            EncenderFosforo();
        }

        if (teclaQ && encendido)
        {
            ApagarManualmente();
        }
    }

    /// <summary>
    /// Otros scripts (ObjetoInteractivo, por ejemplo) consultan este método antes
    /// de permitir que el jugador interactúe con algo del escenario.
    /// Regla obligatoria del MVP: solo se puede clickear con un fósforo encendido.
    /// </summary>
    public bool PuedeInteractuar()
    {
        return encendido;
    }

    private void EncenderFosforo()
    {
        fosforosRestantes--;
        encendido = true;
        tiempoRestanteQuemado = duracionFosforo;

        if (luzFosforo != null)
        {
            luzFosforo.gameObject.SetActive(true);
            luzFosforo.intensity = intensidadBaseLuz; // arranca a full brillo, el fade lo maneja QuemarFosforo()
        }

        // TEMPORAL: mientras no haya arte ni panel de inspección, este log confirma
        // en la Console que el sistema reacciona al click. Se puede borrar después.
        Debug.Log($"[Fosforo] Encendido. Quedan {fosforosRestantes} fósforos.");

        OnFosforoEncendido?.Invoke();
        Terror.GameEvents.RaiseFosforoEncendido(); // <-- CONEXION A FEAR MANAGER
        OnFosforosRestantesCambiado?.Invoke(fosforosRestantes);

        // Avisamos "sin fósforos" apenas se usa el último, no solo cuando se apague:
        // así la Presencia o el manager de game over pueden empezar a reaccionar antes.
        if (fosforosRestantes == 0)
            OnSinFosforos?.Invoke();

        // Si por algún motivo había una coroutine de quemado corriendo, la cortamos
        // antes de lanzar una nueva. Esto evita que spamear click deje coroutines
        // colgadas actualizando un estado viejo (fuga de memoria / bugs fantasma).
        if (coroutineQuemado != null)
            StopCoroutine(coroutineQuemado);
        coroutineQuemado = StartCoroutine(QuemarFosforo());
    }

    /// <summary>
    /// Consume el tiempo de vida del fósforo.
    /// Usamos un loop con Time.deltaTime en vez de "yield return new WaitForSeconds(duracionFosforo)"
    /// a propósito: con WaitForSeconds no se puede pausar el conteo sin perder el progreso,
    /// y pausarlo con Time.timeScale pausaría TODO el juego (incluida la Presencia).
    /// Descontando manualmente podemos congelar el fósforo justo donde iba mientras el
    /// jugador inspecciona un objeto, y seguir desde ahí — la regla oficial del MVP.
    /// </summary>
    private IEnumerator QuemarFosforo()
    {
        while (tiempoRestanteQuemado > 0f)
        {
            if (!pausadoPorInspeccion)
            {
                tiempoRestanteQuemado -= Time.deltaTime;
                ActualizarIntensidadLuz();
            }

            yield return null;
        }

        coroutineQuemado = null;
        ApagarFosforo();
    }

    /// <summary>
    /// Baja la intensidad de la luz a medida que se acaba el tiempo del fósforo,
    /// para que se sienta que se está "apagando" en vez de cortarse de golpe.
    /// No tocamos esto mientras está pausado por inspección: el fósforo no debe
    /// seguir "muriendo" visualmente si tampoco está perdiendo tiempo.
    /// </summary>
    private void ActualizarIntensidadLuz()
    {
        if (luzFosforo == null)
            return;

        // 1 = recién encendido, 0 = a punto de apagarse.
        float progreso = Mathf.Clamp01(tiempoRestanteQuemado / duracionFosforo);
        luzFosforo.intensity = intensidadBaseLuz * progreso;
    }

    private void ApagarFosforo()
    {
        encendido = false;

        if (luzFosforo != null)
            luzFosforo.gameObject.SetActive(false);

        // TEMPORAL: mismo log de depuración que en EncenderFosforo.
        Debug.Log("[Fosforo] Apagado (se acabó el tiempo).");

        OnFosforoApagado?.Invoke();
        Terror.GameEvents.RaiseFosforoApagado(); // <-- CONEXION A FEAR MANAGER
    }

    /// <summary>
    /// Apagado voluntario con la tecla Q, antes de que se acabe el tiempo solo.
    /// Regla de diseño: no hay reembolso del fósforo ni de los segundos que quedaban —
    /// ese es el costo de esconderse de la Presencia ya mismo en vez de esperar a que
    /// el fósforo se consuma solo.
    /// </summary>
    private void ApagarManualmente()
    {
        if (coroutineQuemado != null)
        {
            StopCoroutine(coroutineQuemado);
            coroutineQuemado = null;
        }

        ApagarFosforo();
    }

    /// <summary>
    /// Llamar cuando se abre el panel de inspección de un objeto (lo hace ObjetoInteractivo).
    /// Congela el consumo del fósforo: inspeccionar no debe "costar" tiempo de luz.
    /// No apaga la luz ni detiene la coroutine, solo detiene el descuento del tiempo.
    /// </summary>
    public void PausarQuemado()
    {
        pausadoPorInspeccion = true;
    }

    /// <summary>
    /// Llamar cuando se cierra el panel de inspección. Reanuda el descuento
    /// justo donde se había quedado.
    /// </summary>
    public void ReanudarQuemado()
    {
        pausadoPorInspeccion = false;
    }
}