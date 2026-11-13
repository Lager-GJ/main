using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Botón "Omitir" de las tarjetas de Historia: salta todas las tarjetas y va directo
/// a la escena del juego (la que diga StoryManager.nombreDeLaEscenaDelJuego).
/// También omite el tutorial del cuarto: Luis arranca con la caja de fósforos y solo
/// falta pulsar E para encender el primero (ver InicioCuarto.OmitirTutorial). Se puede
/// volver a ver con el botón "Volver a ver tutorial" que aparece en su lugar.
/// </summary>
public class BotonOmitirHistoria : MonoBehaviour
{
    [Tooltip("Si queda vacío se busca el StoryManager de la escena.")]
    [SerializeField] private StoryManager historia;

    [Tooltip("Se usa solo si no hay StoryManager.")]
    [SerializeField] private string escenaPorDefecto = "JUEGO";

    private bool cargando;

    // Cada vez que se entra a Historia se empieza de cero: el tutorial se omite solo si se pulsa el botón.
    private void Awake() => Terror.UI.InicioCuarto.OmitirTutorial = false;

    public void Omitir()
    {
        if (cargando) return;
        cargando = true;
        Terror.UI.InicioCuarto.OmitirTutorial = true;

        if (historia == null) historia = FindFirstObjectByType<StoryManager>();
        string escena = historia != null && !string.IsNullOrEmpty(historia.nombreDeLaEscenaDelJuego)
            ? historia.nombreDeLaEscenaDelJuego
            : escenaPorDefecto;

        SceneManager.LoadScene(escena);
    }
}
