using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Botón "Omitir" de las tarjetas de Historia: salta todas las tarjetas y va directo
/// a la escena del juego (la que diga StoryManager.nombreDeLaEscenaDelJuego).
/// El juego arranca igual en el primer plano del cajón, porque esa parte enseña a
/// prender el fósforo.
/// </summary>
public class BotonOmitirHistoria : MonoBehaviour
{
    [Tooltip("Si queda vacío se busca el StoryManager de la escena.")]
    [SerializeField] private StoryManager historia;

    [Tooltip("Se usa solo si no hay StoryManager.")]
    [SerializeField] private string escenaPorDefecto = "JUEGO";

    private bool cargando;

    public void Omitir()
    {
        if (cargando) return;
        cargando = true;

        if (historia == null) historia = FindFirstObjectByType<StoryManager>();
        string escena = historia != null && !string.IsNullOrEmpty(historia.nombreDeLaEscenaDelJuego)
            ? historia.nombreDeLaEscenaDelJuego
            : escenaPorDefecto;

        SceneManager.LoadScene(escena);
    }
}
