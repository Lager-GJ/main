using UnityEngine;
using UnityEngine.SceneManagement;

namespace Terror
{
    /// <summary>
    /// Unico script de 00_Boot: carga el perfil guardado, aplica los volumenes y
    /// pasa al menu. Da por sentado que SceneRouter y AudioManager ya estan en la
    /// escena (colocados en el Editor, en el objeto [Persistentes]).
    ///
    /// 00_Boot tiene que ser la escena de indice 0 en Build Settings.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string escenaMenu = Escenas.Menu;

        private void Start()
        {
            PerfilJugador perfil = SaveSystem.Cargar();

            if (AudioManager.Instance != null)
                AudioManager.Instance.CargarDesde(perfil);
            else
                Debug.LogWarning("[BootLoader] No hay AudioManager en 00_Boot: los volumenes guardados no se van a aplicar.");

            if (SceneRouter.Instance != null)
                SceneRouter.Instance.CargarEscena(escenaMenu);
            else
                SceneManager.LoadScene(escenaMenu); // sin router igual llegamos al menu
        }
    }
}
