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
///   Último fósforo (el 4to) consumido → derrota automática.
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

    [Header("Nivel 2: pasos y ruidos")]
    [SerializeField] private AudioClip sonidoPasos;
    [Tooltip("Ruidos sueltos que se alternan con los pasos (puertas, cajones...).")]
    [SerializeField] private AudioClip[] sonidosRuidos;
    [SerializeField] private float intervaloRuidosMinimo = 4f;
    [SerializeField] private float intervaloRuidosMaximo = 9f;
    [SerializeField, Range(0f, 1f)] private float volumenRuidos = 0.8f;

    [Header("Nivel 3: alcance de la luz")]
    [Tooltip("Fracción del radio original de la luz del fósforo desde el tercer fósforo consumido.")]
    [SerializeField, Range(0.1f, 1f)] private float factorAlcanceLuz = 0.6f;

    private int nivel;
    private AudioSource fuenteRespiracion;
    private AudioSource fuenteRuidos;
    private Coroutine coroutineRuidos;

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

    private void ManejarFosforoConsumido()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego)
            return;

        nivel++;
        Debug.Log($"[Presencia] Fósforo consumido. Nivel {nivel}.");
        GameEvents.RaiseCercaniaPresenciaCambiada(nivel, 1f);

        if (nivel == 1)
            IniciarRespiracion();
        else if (nivel == 2)
            coroutineRuidos = StartCoroutine(ReproducirRuidos());
        else if (nivel == 3)
            FosforoManager.Instance?.SetFactorAlcanceLuz(factorAlcanceLuz);

        if (FosforoManager.Instance != null && FosforoManager.Instance.FosforosRestantes == 0)
            GameStateManager.Instance?.Perder();
    }

    private void IniciarRespiracion()
    {
        if (sonidoRespiracion == null)
            return;

        fuenteRespiracion.clip = sonidoRespiracion;
        fuenteRespiracion.volume = volumenRespiracion;
        fuenteRespiracion.Play();
    }

    private IEnumerator ReproducirRuidos()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervaloRuidosMinimo, intervaloRuidosMaximo));

            AudioClip clip = ElegirRuido();
            if (clip != null)
                fuenteRuidos.PlayOneShot(clip, volumenRuidos);
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
        if (estado == GameState.Juego)
            return;

        fuenteRespiracion.Stop();
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
