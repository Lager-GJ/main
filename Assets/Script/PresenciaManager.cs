using System.Collections;
using Terror;
using UnityEngine;

/// <summary>
/// La Presencia: la entidad que se acerca a medida que el niño gasta sus fósforos.
/// Regla acordada 2026-10-08: la Presencia sube un nivel cada vez que un fósforo se
/// consume (se apaga solo o con Q).
///   1er fósforo consumido → respiración más profunda (loop).
///   2do fósforo consumido → se empiezan a escuchar pasos y ruidos (al azar).
///   3er fósforo consumido → la luz del fósforo alcanza menos.
///   Último fósforo (el 4to) consumido → silencio breve y derrota automática.
/// Cada nivel suma: la respiración se oye más fuerte y agitada, y los ruidos se
/// vuelven más frecuentes y se acercan.
/// El nivel se avisa por GameEvents.OnCercaniaPresenciaCambiada (multiplicador 1:
/// la Presencia ya no acelera la barra de miedo).
/// </summary>
public class PresenciaManager : MonoBehaviour
{
    public static PresenciaManager Instance { get; private set; }

    [Header("Nivel 1: respiración")]
    [Tooltip("Loop de respiración que empieza al consumirse el primer fósforo. Sin clip, no suena nada.")]
    [SerializeField] private AudioClip sonidoRespiracion;
    [SerializeField, Range(0f, 1f)] private float volumenRespiracion = 0.6f;
    [Tooltip("Segundos que tarda la respiración en aparecer (fade in) o en subir de intensidad.")]
    [SerializeField] private float segundosFadeRespiracion = 2.5f;

    [Header("Nivel 2: pasos y ruidos")]
    [SerializeField] private AudioClip sonidoPasos;
    [Tooltip("Ruidos sueltos que se alternan con los pasos (puertas, cajones...).")]
    [SerializeField] private AudioClip[] sonidosRuidos;
    [SerializeField] private float intervaloRuidosMinimo = 4f;
    [SerializeField] private float intervaloRuidosMaximo = 9f;
    [SerializeField, Range(0f, 1f)] private float volumenRuidos = 0.8f;
    [Tooltip("Cuánto se acortan los intervalos entre ruidos por cada nivel extra (0.7 = 30% más seguido).")]
    [SerializeField, Range(0.3f, 1f)] private float aceleracionRuidosPorNivel = 0.7f;

    [Header("Nivel 3: alcance de la luz")]
    [Tooltip("Fracción del radio original de la luz del fósforo desde el tercer fósforo consumido.")]
    [SerializeField, Range(0.1f, 1f)] private float factorAlcanceLuz = 0.6f;

    [Header("Último fósforo")]
    [Tooltip("Segundos de silencio total entre que se apaga el último fósforo y la derrota. 0 = inmediata.")]
    [SerializeField] private float segundosSilencioAntesDeDerrota = 1.5f;

    private int nivel;
    private AudioSource fuenteRespiracion;
    private AudioSource fuenteRuidos;
    private Coroutine coroutineRuidos;
    private float volumenObjetivoRespiracion;
    private bool pausadoPorTiempo;

    public int Nivel => nivel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Fuentes creadas en runtime para no tener que cablear AudioSources en la escena.
        fuenteRespiracion = CrearFuente(true);
        fuenteRuidos = CrearFuente(false);
    }

    private void OnEnable()
    {
        GameEvents.OnFosforoApagado += ManejarFosforoConsumido;
    }

    private void OnDisable()
    {
        GameEvents.OnFosforoApagado -= ManejarFosforoConsumido;
    }

    private void Start()
    {
        // En Start() (no OnEnable) para que GameStateManager ya haya corrido su Awake().
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged += ManejarEstado;
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged -= ManejarEstado;
    }

    private void Update()
    {
        // El menú de pausa y el tutorial congelan el juego con Time.timeScale = 0
        // sin cambiar de estado: callamos también la Presencia mientras tanto.
        bool pausado = Time.timeScale == 0f;
        if (pausado != pausadoPorTiempo)
        {
            pausadoPorTiempo = pausado;
            if (pausado) { fuenteRespiracion.Pause(); fuenteRuidos.Pause(); }
            else { fuenteRespiracion.UnPause(); fuenteRuidos.UnPause(); }
        }

        if (fuenteRespiracion.isPlaying && !Mathf.Approximately(fuenteRespiracion.volume, volumenObjetivoRespiracion))
        {
            float paso = volumenRespiracion / Mathf.Max(0.01f, segundosFadeRespiracion) * Time.deltaTime;
            fuenteRespiracion.volume = Mathf.MoveTowards(fuenteRespiracion.volume, volumenObjetivoRespiracion, paso);
        }
    }

    private void ManejarFosforoConsumido()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego)
            return;

        nivel++;
        Debug.Log($"[Presencia] Fósforo consumido. Nivel {nivel}.");
        GameEvents.RaiseCercaniaPresenciaCambiada(nivel, 1f);

        if (FosforoManager.Instance != null && FosforoManager.Instance.FosforosRestantes == 0)
        {
            StartCoroutine(DerrotaTrasSilencio());
            return;
        }

        IntensificarRespiracion();
        if (nivel == 2)
            coroutineRuidos = StartCoroutine(ReproducirRuidos());
        else if (nivel == 3)
            FosforoManager.Instance?.SetFactorAlcanceLuz(factorAlcanceLuz);
    }

    // Arranca la respiración en el nivel 1 y la vuelve más fuerte y agitada en cada nivel siguiente.
    private void IntensificarRespiracion()
    {
        if (sonidoRespiracion == null)
            return;

        volumenObjetivoRespiracion = Mathf.Clamp01(volumenRespiracion * (1f + 0.25f * (nivel - 1)));
        fuenteRespiracion.pitch = 1f + 0.06f * (nivel - 1);

        if (!fuenteRespiracion.isPlaying)
        {
            fuenteRespiracion.clip = sonidoRespiracion;
            fuenteRespiracion.volume = 0f;
            fuenteRespiracion.Play();
        }
    }

    // Se apaga el último fósforo: todo se calla un instante (la Presencia ya está ahí) y se pierde.
    private IEnumerator DerrotaTrasSilencio()
    {
        Callar();

        if (segundosSilencioAntesDeDerrota > 0f)
            yield return new WaitForSeconds(segundosSilencioAntesDeDerrota);

        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Juego)
            GameStateManager.Instance.Perder();
    }

    private IEnumerator ReproducirRuidos()
    {
        while (true)
        {
            // Cuanto más alto el nivel, más seguido suenan.
            float factor = Mathf.Pow(aceleracionRuidosPorNivel, nivel - 2);
            yield return new WaitForSeconds(Random.Range(intervaloRuidosMinimo, intervaloRuidosMaximo) * factor);

            AudioClip clip = ElegirRuido();
            if (clip == null)
                continue;

            // Cada ruido viene de un lado distinto, y desde el nivel 3 suenan más cerca (más fuerte).
            fuenteRuidos.panStereo = Random.Range(-0.8f, 0.8f);
            float cercania = nivel >= 3 ? 1f : 0.7f;
            fuenteRuidos.PlayOneShot(clip, volumenRuidos * cercania);
        }
    }

    // Los pasos salen la mitad de las veces; el resto, un ruido al azar.
    private AudioClip ElegirRuido()
    {
        bool hayRuidos = sonidosRuidos != null && sonidosRuidos.Length > 0;
        if (sonidoPasos != null && (!hayRuidos || Random.value < 0.5f))
            return sonidoPasos;

        return hayRuidos ? sonidosRuidos[Random.Range(0, sonidosRuidos.Length)] : null;
    }

    // Al ganar o perder se callan la respiración y los ruidos.
    private void ManejarEstado(GameState estado)
    {
        if (estado != GameState.Juego)
            Callar();
    }

    private void Callar()
    {
        fuenteRespiracion.Stop();
        fuenteRuidos.Stop();
        if (coroutineRuidos != null)
        {
            StopCoroutine(coroutineRuidos);
            coroutineRuidos = null;
        }
    }

    private AudioSource CrearFuente(bool loop)
    {
        AudioSource fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = loop;
        fuente.spatialBlend = 0f;
        return fuente;
    }
}
