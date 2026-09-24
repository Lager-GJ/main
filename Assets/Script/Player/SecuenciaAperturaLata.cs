using System.Collections;
using UnityEngine;
using Terror;

/// <summary>
/// Reemplaza la victoria instantánea de ObjetoInteractivo para la lata de galletas:
/// antes de ganar, la tapa tiembla y se revela que por dentro son hilos y agujas
/// (el chiste local: las abuelas reutilizaban las latas de galletas vacías para
/// guardar su costura) — recién ahí se llama a Ganar(). El sonido de apertura ya
/// suena solo vía SonidoAlInteractuar/SonidoPropioObjeto (se dispara con
/// OnObjetoInspeccionado, que ObjetoInteractivo ya invoca antes que esto);
/// no hace falta repetirlo acá.
///
/// Nada de esto pausa el fósforo ni el miedo aparte de lo que ObjetoInteractivo ya
/// hace siempre (PausarQuemado() al inspeccionar) — decisión de diseño 2026-09-15,
/// misma que la del armario: la tensión sigue corriendo. Si el miedo llega a 100
/// durante la secuencia, GameStateManager pasa a Derrota y la secuencia se corta
/// sin llamar a Ganar().
/// </summary>
[RequireComponent(typeof(ObjetoInteractivo))]
public class SecuenciaAperturaLata : MonoBehaviour
{
    [Header("Sprite al abrirse (hilos y agujas — el chiste)")]
    [SerializeField] private Sprite spriteAbierta;

    [Header("Texto del chiste (objeto con TextMeshProUGUI ya escrito, empieza apagado)")]
    [SerializeField] private GameObject textoChiste;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float duracionTemblor = 0.6f;
    [SerializeField] private float temblorPorSegundo = 14f;
    [SerializeField] private float amplitudTemblor = 0.05f;
    [SerializeField] private float esperaAntesDeGanar = 2.5f;

    private ObjetoInteractivo objetoInteractivo;
    private SpriteRenderer spriteRenderer;
    private Vector3 posicionOriginal;

    private void Awake()
    {
        objetoInteractivo = GetComponent<ObjetoInteractivo>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        posicionOriginal = transform.localPosition;
    }

    private void OnEnable() => ObjetoInteractivo.OnObjetivoDeVictoriaEncontrado += ManejarEncontrado;
    private void OnDisable() => ObjetoInteractivo.OnObjetivoDeVictoriaEncontrado -= ManejarEncontrado;

    private void ManejarEncontrado(ObjetoInteractivo objeto)
    {
        if (objeto != objetoInteractivo) return; // no es esta lata
        StartCoroutine(SecuenciaApertura());
    }

    private IEnumerator SecuenciaApertura()
    {
        // La tapa tiembla (wobble de posición, sin necesitar un sprite aparte).
        float t = 0f;
        while (t < duracionTemblor)
        {
            if (SeInterrumpio())
            {
                transform.localPosition = posicionOriginal;
                yield break;
            }

            float offset = Mathf.Sin(t * temblorPorSegundo * Mathf.PI * 2f) * amplitudTemblor;
            transform.localPosition = posicionOriginal + new Vector3(offset, 0f, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = posicionOriginal;

        if (SeInterrumpio()) yield break;

        // Se revela el interior: hilos y agujas + el texto del chiste.
        if (spriteRenderer != null && spriteAbierta != null)
            spriteRenderer.sprite = spriteAbierta;

        if (textoChiste != null)
            textoChiste.SetActive(true);

        float espera = 0f;
        while (espera < esperaAntesDeGanar)
        {
            if (SeInterrumpio())
            {
                if (textoChiste != null) textoChiste.SetActive(false);
                yield break;
            }
            espera += Time.deltaTime;
            yield return null;
        }

        if (textoChiste != null)
            textoChiste.SetActive(false);

        gameObject.SetActive(false);
        GameStateManager.Instance.Ganar();
    }

    // Si el miedo llegó a 100 (u otra cosa sacó al juego de GameState.Juego)
    // mientras la secuencia corría, la cortamos sin ganar.
    private bool SeInterrumpio()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CurrentState != GameState.Juego;
    }
}
