using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Terror
{
    /// <summary>
    /// Punto unico para cargar escenas, con pantalla de carga de por medio. Vive en
    /// 00_Boot como persistente — no lo dupliques en otras escenas.
    /// </summary>
    public class SceneRouter : MonoBehaviour
    {
        public static SceneRouter Instance { get; private set; }

        [SerializeField] private PantallaCarga pantallaCarga;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Red de seguridad del Time.timeScale.
            //
            // El timeScale es global y SOBREVIVE a los cambios de escena, asi que si
            // el jugador sale al menu estando en pausa (timeScale = 0) el menu
            // arrancaria congelado. Restaurarlo solo en CargarEscena no alcanza:
            // GameStateManager.Reiniciar() y StoryManager.CargarJuego() llaman a
            // SceneManager.LoadScene directamente, salteandose este router.
            //
            // Como este es el unico objeto persistente del juego, engancharse aca a
            // sceneLoaded cubre TODOS los caminos de carga, incluidos los que no
            // pasan por aca. Ningun camino puede dejar el juego congelado.
            SceneManager.sceneLoaded += RestaurarTiempo;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= RestaurarTiempo;

            if (Instance == this)
                Instance = null;
        }

        private void RestaurarTiempo(Scene escena, LoadSceneMode modo)
        {
            Time.timeScale = 1f;
        }

        public void CargarEscena(string nombreEscena)
        {
            if (string.IsNullOrEmpty(nombreEscena))
            {
                Debug.LogWarning("[SceneRouter] Se pidio cargar una escena sin nombre; se ignora.");
                return;
            }

            StartCoroutine(CargarEscenaAsync(nombreEscena));
        }

        private IEnumerator CargarEscenaAsync(string nombreEscena)
        {
            // Antes de nada: si veniamos de una pausa, destrabar el tiempo. La
            // corrutina avanza igual con timeScale = 0 (yield return null no depende
            // del tiempo escalado), pero la escena destino no debe heredar el freeze.
            Time.timeScale = 1f;

            if (pantallaCarga != null) pantallaCarga.Mostrar();

            AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreEscena);
            while (operacion != null && !operacion.isDone)
                yield return null;

            if (pantallaCarga != null) pantallaCarga.Ocultar();
        }
    }
}
