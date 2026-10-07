using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    [Tooltip("En unidades locales: ~0.05 si la lata es un SpriteRenderer en mundo, ~10 si es un Image de UI.")]
    [SerializeField] private float amplitudTemblor = 0.05f;
    [SerializeField] private float esperaAntesDeGanar = 2.5f;

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
        if (objeto != objetoInteractivo) return; // no es esta lata
        StartCoroutine(SecuenciaApertura());
    }

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
                yield break;
            }

            float offset = Mathf.Sin(t * temblorPorSegundo * Mathf.PI * 2f) * amplitudTemblor;
            transform.localPosition = posicionOriginal + new Vector3(offset, 0f, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = posicionOriginal;

        if (SeInterrumpio())
        {
            LiberarCierre();
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
                yield break;
            }
            espera += Time.deltaTime;
            yield return null;
        }

        if (textoChiste != null)
            textoChiste.SetActive(false);

        LiberarCierre();
        gameObject.SetActive(false);
        GameStateManager.Instance.Ganar();
    }

    private static void LiberarCierre()
    {
        if (PanelInspeccion.Instance != null)
            PanelInspeccion.Instance.CierreBloqueado = false;
    }

    // Si el miedo llegó a 100 (u otra cosa sacó al juego de GameState.Juego)
    // mientras la secuencia corría, la cortamos sin ganar.
    private bool SeInterrumpio()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CurrentState != GameState.Juego;
    }
}
