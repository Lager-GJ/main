using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Terror;

/// <summary>
/// Apertura de la lata de galletas: la tapa tiembla y se revela que por dentro son
/// hilos y agujas (el chiste local: las abuelas reutilizaban las latas de galletas
/// vacías para guardar su costura).
///
/// Desde 2026-10-22 la lata ya no es la victoria: está cerrada con llave (Cerradura
/// con llave_pequena) y su alAbrir llama a IniciarApertura(). Al terminar entrega
/// 'entregarAlTerminar' (la llave del armario) y dispara alTerminar. Con
/// ganarAlTerminar marcado vuelve a ser la victoria de antes (vía ObjetoInteractivo).
///
/// Corre con tiempo real (la vista del armario está en cámara lenta) pero se detiene
/// con la pausa. Si el juego sale de Juego a mitad de camino, se corta.
/// </summary>
public class SecuenciaAperturaLata : MonoBehaviour
{
    [Header("Sprite al abrirse (hilos y agujas — el chiste)")]
    [SerializeField] private Sprite spriteAbierta;

    [Header("Texto del chiste (objeto con TextMeshProUGUI ya escrito, empieza apagado)")]
    [SerializeField] private GameObject textoChiste;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float duracionTemblor = 0.6f;
    [SerializeField] private float temblorPorSegundo = 14f;
    [Tooltip("En unidades locales: ~0.05 si la lata es un SpriteRenderer en mundo, ~10 si es un Image de UI.")]
    [SerializeField] private float amplitudTemblor = 0.05f;
    [SerializeField] private float esperaAntesDeGanar = 2.5f;

    [Header("Al terminar")]
    [Tooltip("Marcado: la lata es la victoria (regla vieja). Desmarcado: queda abierta y entrega lo de abajo.")]
    [SerializeField] private bool ganarAlTerminar = false;
    [Tooltip("Objeto que se guarda en el inventario al terminar (ej. llave_armario).")]
    [SerializeField] private string entregarAlTerminar;
    [SerializeField] private UnityEngine.Events.UnityEvent alTerminar;

    private bool enCurso;

    private ObjetoInteractivo objetoInteractivo;
    private SpriteRenderer spriteRenderer;
    private Image imagen; // la lata vive en UI dentro del primer plano del armario
    private Vector3 posicionOriginal;

    private void Awake()
    {
        objetoInteractivo = GetComponent<ObjetoInteractivo>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        imagen = GetComponent<Image>();
        posicionOriginal = transform.localPosition;
    }

    private void OnEnable() => ObjetoInteractivo.OnObjetivoDeVictoriaEncontrado += ManejarEncontrado;

    private void OnDisable()
    {
        ObjetoInteractivo.OnObjetivoDeVictoriaEncontrado -= ManejarEncontrado;
        LiberarCierre();
    }

    private void ManejarEncontrado(ObjetoInteractivo objeto)
    {
        if (objetoInteractivo == null || objeto != objetoInteractivo) return; // no es esta lata
        IniciarApertura();
    }

    /// <summary>Conectar al alAbrir de la Cerradura de la lata.</summary>
    public void IniciarApertura()
    {
        if (enCurso) return;
        enCurso = true;
        StartCoroutine(SecuenciaApertura());
    }

    // Tiempo real que avanza solo sin pausa (la vista del armario está en cámara lenta).
    private static float Paso() => Time.timeScale > 0f ? Time.unscaledDeltaTime : 0f;

    private IEnumerator SecuenciaApertura()
    {
        // Mientras dura la secuencia la vista no se puede cerrar (Escape / click
        // afuera): la coroutine vive dentro de ella y la victoria no se dispararía.
        if (PanelInspeccion.Instance != null)
            PanelInspeccion.Instance.CierreBloqueado = true;

        // La tapa tiembla (wobble de posición, sin necesitar un sprite aparte).
        float t = 0f;
        while (t < duracionTemblor)
        {
            if (SeInterrumpio())
            {
                transform.localPosition = posicionOriginal;
                LiberarCierre();
                enCurso = false;
                yield break;
            }

            float offset = Mathf.Sin(t * temblorPorSegundo * Mathf.PI * 2f) * amplitudTemblor;
            transform.localPosition = posicionOriginal + new Vector3(offset, 0f, 0f);
            t += Paso();
            yield return null;
        }
        transform.localPosition = posicionOriginal;

        if (SeInterrumpio())
        {
            LiberarCierre();
            enCurso = false;
            yield break;
        }

        // Se revela el interior: hilos y agujas + el texto del chiste.
        if (spriteAbierta != null)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = spriteAbierta;
            if (imagen != null) imagen.sprite = spriteAbierta;
        }

        if (textoChiste != null)
            textoChiste.SetActive(true);

        float espera = 0f;
        while (espera < esperaAntesDeGanar)
        {
            if (SeInterrumpio())
            {
                if (textoChiste != null) textoChiste.SetActive(false);
                LiberarCierre();
                enCurso = false;
                yield break;
            }
            espera += Paso();
            yield return null;
        }

        if (textoChiste != null)
            textoChiste.SetActive(false);

        LiberarCierre();

        if (ganarAlTerminar)
        {
            gameObject.SetActive(false);
            GameStateManager.Instance.Ganar();
            yield break;
        }

        // Queda abierta, con la costura a la vista, y entrega lo que tenía adentro.
        if (!string.IsNullOrEmpty(entregarAlTerminar) && Inventario.Instance != null)
            Inventario.Instance.Agregar(entregarAlTerminar);
        alTerminar?.Invoke();
    }

    private static void LiberarCierre()
    {
        if (PanelInspeccion.Instance != null)
            PanelInspeccion.Instance.CierreBloqueado = false;
    }

    // Si algo sacó al juego de GameState.Juego (la Presencia, por ejemplo)
    // mientras la secuencia corría, la cortamos.
    private bool SeInterrumpio()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CurrentState != GameState.Juego;
    }
}
