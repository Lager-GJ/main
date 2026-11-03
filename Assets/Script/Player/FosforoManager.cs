using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Mouse.current: el proyecto usa el Input System nuevo (activeInputHandler = 1),
                               // así que UnityEngine.Input.GetMouseButtonDown tiraría una excepción en runtime.
using UnityEngine.Rendering.Universal; // Light2D vive aquí en URP.

/// <summary>
/// Núcleo mecánico del sistema de fósforos ("La Vela de la Abuela").
/// Controla el recurso limitado (fósforos), el ciclo de encendido/apagado con
/// temporizador, y expone el estado mediante eventos estáticos para que el resto
/// del equipo (Presencia, UI, audio/VFX) reaccione sin acoplarse
/// directamente a esta clase.
/// </summary>
public class FosforoManager : MonoBehaviour
{
    // Singleton simple: para un MVP de una sola escena/habitación es suficiente
    // y evita que cada ObjetoInteractivo tenga que arrastrar una referencia manual.
    public static FosforoManager Instance { get; private set; }

    [Header("Configuración de fósforos")]
    [Tooltip("Fósforos disponibles al iniciar la partida. Es la perilla principal de dificultad del MVP.")]
    [SerializeField] private int fosforosIniciales = 4;

    [Tooltip("Segundos que dura encendido un fósforo antes de apagarse solo.")]
    [SerializeField] private float duracionFosforo = 15f;

    [Header("Cómo se consume la llama")]
    [Tooltip("Parte de la duración en que la luz queda al máximo (0.7 = el 70%).")]
    [SerializeField, Range(0f, 1f)] private float parteEstable = 0.7f;
    [Tooltip("Parte siguiente en que la luz baja despacio (0.2 = el 20%). Lo que sobra es el final, cuando se apaga rápido.")]
    [SerializeField, Range(0f, 1f)] private float parteBajada = 0.2f;
    [Tooltip("Cuánta luz queda al terminar la bajada lenta (0.5 = la mitad). Desde ahí cae a 0 en el tramo final.")]
    [SerializeField, Range(0.05f, 1f)] private float luzAlFinalDeLaBajada = 0.5f;
    [Tooltip("Parpadeo extra en el tramo final, cuando la llama se está muriendo (0 = nada).")]
    [SerializeField, Range(0f, 1f)] private float parpadeoAlMorir = 0.35f;
    [Tooltip("Color al que va la luz mientras el fósforo se muere.")]
    [SerializeField] private Color colorAlMorir = new Color(1f, 0.32f, 0.12f, 1f);

    [Header("Referencias")]
    [Tooltip("Luz 2D que representa el fósforo encendido. Se activa/desactiva junto con el estado.")]
    [SerializeField] private Light2D luzFosforo;

    // --- Estado interno ---
    private int fosforosRestantes;
    private float tiempoRestanteQuemado;
    private bool encendido;
    // Quién tiene pausado el consumo del fósforo (una lectura, el inventario, el candado...).
    // Es un conjunto y no un bool: cerrar el primer plano no debe soltar la pausa del
    // inventario abierto encima. Las llamadas sin motivo usan uno compartido.
    private readonly HashSet<object> motivosPausa = new HashSet<object>();
    private static readonly object MotivoComun = new object();
    private Coroutine coroutineQuemado;
    private float intensidadBaseLuz; // intensidad que configuraste en el Inspector, se guarda una sola vez
    private float radioInteriorBase; // radios de la luz tal como quedaron en el Inspector
    private float radioExteriorBase;
    private Color colorBaseLuz;
    private bool encendidoHabilitado = true; // el tutorial lo apaga hasta que el jugador recoge los fósforos
    private float fuerzaParpadeo; // 0 = llama quieta; la Presencia la hace temblar más en cada nivel

    // --- Eventos estáticos ---
    // Se usan Action estáticos (no UnityEvents) a propósito: cualquier script del
    // equipo puede suscribirse sin necesitar una referencia directa a esta instancia.
    // Así la Presencia, la UI y el audio no se acoplan al sistema de fósforo.
    public static event Action OnFosforoEncendido;
    public static event Action OnFosforoApagado;
    public static event Action OnSinFosforos;

    // Notifica el conteo cada vez que cambia, para que la UI pinte el contador
    // sin hacer polling en su propio Update() ni pedir una referencia a este script.
    public static event Action<int> OnFosforosRestantesCambiado;

    // Progreso de la luz del fósforo encendido: 1 = recién encendido, 0 = apagado.
    // Se publica como evento para que la barra de luz de la UI se dibuje sin
    // hacer polling ni depender de esta clase (mismo patrón que el contador).
    public static event Action<float> OnProgresoFosforoCambiado;

    public bool Encendido => encendido;
    public int FosforosRestantes => fosforosRestantes;
    public int FosforosIniciales => fosforosIniciales;
    public float DuracionFosforo => duracionFosforo;
    public bool PausadoPorInspeccion => motivosPausa.Count > 0;
    public float TiempoRestante => encendido ? tiempoRestanteQuemado : 0f;
    public float ProgresoRestante => encendido ? Mathf.Clamp01(tiempoRestanteQuemado / duracionFosforo) : 0f;

    /// <summary>Tamaño/fuerza de la llama ahora: 1 = entera, 0 = apagada (sigue la curva 70/20/10).</summary>
    public float FuerzaLlama => encendido ? CurvaLlama(1f - ProgresoRestante) : 0f;

    /// <summary>Cuánto se enrojece la llama al morir: 0 = color normal, 1 = roja del todo.</summary>
    public float RojezLlama => encendido ? CurvaRojez(1f - ProgresoRestante) : 0f;

    /// <summary>Nivel de temblor que pidió la Presencia (0 = quieta).</summary>
    public float FuerzaParpadeo => fuerzaParpadeo;

    // t = parte de la duración que ya se quemó (0 recién encendido, 1 apagado).
    private float CurvaLlama(float t)
    {
        float finEstable = parteEstable;
        float finBajada = Mathf.Min(1f, parteEstable + parteBajada);
        if (t <= finEstable) return 1f;
        if (t <= finBajada)
            return Mathf.Lerp(1f, luzAlFinalDeLaBajada, Mathf.SmoothStep(0f, 1f, (t - finEstable) / Mathf.Max(0.0001f, finBajada - finEstable)));
        float u = (t - finBajada) / Mathf.Max(0.0001f, 1f - finBajada);
        return luzAlFinalDeLaBajada * (1f - Mathf.Clamp01(u));
    }

    // Empieza a ponerse roja con la bajada lenta y termina roja del todo al apagarse.
    private float CurvaRojez(float t)
    {
        if (t <= parteEstable) return 0f;
        return Mathf.Clamp01((t - parteEstable) / Mathf.Max(0.0001f, 1f - parteEstable));
    }

    private bool EnTramoFinal(float t) => t > Mathf.Min(1f, parteEstable + parteBajada);

    /// <summary>
    /// Permite o bloquea el encendido con la tecla E. Por defecto está permitido; solo
    /// el tutorial lo bloquea hasta que el jugador recoge los fósforos de la mesita.
    /// </summary>
    public void HabilitarEncendido(bool habilitado)
    {
        encendidoHabilitado = habilitado;
    }

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
            radioInteriorBase = luzFosforo.pointLightInnerRadius;
            radioExteriorBase = luzFosforo.pointLightOuterRadius;
            colorBaseLuz = luzFosforo.color;
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

        // Con el juego pausado (menú de pausa o un paso del tutorial, timeScale 0), o ya
        // ganado/perdido, E y Q no hacen nada: antes gastaban fósforos y subían la Presencia.
        if (Time.timeScale == 0f)
            return;
        if (Terror.GameStateManager.Instance != null &&
            Terror.GameStateManager.Instance.CurrentState != Terror.GameState.Juego)
            return;

        // Mientras corre la apertura de la lata la vista no se puede cerrar; apagar el
        // fósforo con Q en ese momento contaba como fósforo consumido en plena victoria.
        if (teclaQ && Terror.PanelInspeccion.Instance != null && Terror.PanelInspeccion.Instance.CierreBloqueado)
            teclaQ = false;

        if (teclaE && !encendido && fosforosRestantes > 0 && encendidoHabilitado)
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
            luzFosforo.pointLightInnerRadius = radioInteriorBase; // y a radio completo, igual en todos los fósforos
            luzFosforo.pointLightOuterRadius = radioExteriorBase;
            luzFosforo.color = colorBaseLuz;
        }

        // TEMPORAL: mientras no haya arte ni panel de inspección, este log confirma
        // en la Console que el sistema reacciona al click. Se puede borrar después.
        Debug.Log($"[Fosforo] Encendido. Quedan {fosforosRestantes} fósforos.");

        OnFosforoEncendido?.Invoke();
        Terror.GameEvents.RaiseFosforoEncendido();
        OnFosforosRestantesCambiado?.Invoke(fosforosRestantes);
        OnProgresoFosforoCambiado?.Invoke(1f);

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
            if (motivosPausa.Count == 0)
                tiempoRestanteQuemado -= Time.deltaTime;

            // Aunque esté pausado se refresca: el tiempo no avanza, pero la llama
            // puede seguir temblando (parpadeo de la Presencia).
            ActualizarIntensidadLuz();

            yield return null;
        }

        coroutineQuemado = null;
        ApagarFosforo();
    }

    /// <summary>
    /// La luz sigue la curva de la llama: el 70% del tiempo queda al máximo, el 20%
    /// siguiente baja despacio (y se va poniendo roja) y en el 10% final se apaga rápido.
    /// Mientras está pausado por inspección el tiempo no avanza, así que la luz tampoco
    /// "muere", pero la llama puede seguir temblando.
    /// </summary>
    private void ActualizarIntensidadLuz()
    {
        // 1 = recién encendido, 0 = a punto de apagarse.
        float progreso = Mathf.Clamp01(tiempoRestanteQuemado / duracionFosforo);
        float quemado = 1f - progreso;

        // Se publica aunque no haya Light2D asignada: la barra de UI no depende de la luz.
        OnProgresoFosforoCambiado?.Invoke(progreso);

        if (luzFosforo == null)
            return;

        float fuerza = CurvaLlama(quemado);

        float temblor = fuerzaParpadeo;
        if (EnTramoFinal(quemado)) temblor = Mathf.Max(temblor, parpadeoAlMorir);
        float parpadeo = 1f;
        if (temblor > 0f && Time.timeScale > 0f)
            parpadeo = 1f - temblor * Mathf.PerlinNoise(Time.time * 12f, 0.37f);

        luzFosforo.intensity = intensidadBaseLuz * fuerza * parpadeo;
        luzFosforo.color = Color.Lerp(colorBaseLuz, colorAlMorir, CurvaRojez(quemado));

        // El alcance sigue la misma curva: radio completo casi todo el fósforo, y se
        // cierra recién cuando la llama empieza a morir.
        luzFosforo.pointLightInnerRadius = radioInteriorBase * fuerza;
        luzFosforo.pointLightOuterRadius = radioExteriorBase * fuerza;
    }

    /// <summary>
    /// Hace temblar la llama: 0 = quieta, 0.35 = baja hasta un 35% de golpe.
    /// La usa la Presencia (EfectosPresencia) para que la luz se sienta amenazada.
    /// </summary>
    public void SetParpadeo(float fuerza)
    {
        fuerzaParpadeo = Mathf.Clamp01(fuerza);
    }

    private void ApagarFosforo()
    {
        encendido = false;

        if (luzFosforo != null)
            luzFosforo.gameObject.SetActive(false);

        // TEMPORAL: mismo log de depuración que en EncenderFosforo.
        Debug.Log("[Fosforo] Apagado (se acabó el tiempo).");

        OnFosforoApagado?.Invoke();
        Terror.GameEvents.RaiseFosforoApagado();
        OnProgresoFosforoCambiado?.Invoke(0f);
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
    public void PausarQuemado() => PausarQuemado(MotivoComun);

    /// <summary>
    /// Pausa con un motivo propio (el inventario, la tarjeta de lectura, el candado):
    /// solo ReanudarQuemado(mismo motivo) la suelta.
    /// </summary>
    public void PausarQuemado(object motivo)
    {
        motivosPausa.Add(motivo ?? MotivoComun);
    }

    /// <summary>
    /// Llamar cuando se cierra el panel de inspección. Reanuda el descuento
    /// justo donde se había quedado.
    /// </summary>
    public void ReanudarQuemado() => ReanudarQuemado(MotivoComun);

    public void ReanudarQuemado(object motivo)
    {
        motivosPausa.Remove(motivo ?? MotivoComun);
    }
}