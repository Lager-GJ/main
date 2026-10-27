using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Conecta "La habitación prohibida" (JUEGO.unity) al shell del menu: al ganar
    /// (GameStateManager pasa a Victoria), marca el cuarto como completado y
    /// desbloquea el siguiente. No duplica ni reemplaza CondiciondeVictoria.cs —
    /// solo escucha el mismo GameStateManager.OnStateChanged que ese script ya
    /// usa para decidir la victoria.
    ///
    /// Colocar en JUEGO.unity, en cualquier GameObject (por ejemplo, junto al que
    /// ya tiene GameStateManager).
    /// </summary>
    public class L1Controller : MonoBehaviour
    {
        [Tooltip("La LeyendaDefinicion de este cuarto (Leyenda_L1_CajaFosforos). De aca sale el id que se guarda como completado y a que cuarto se desbloquea despues.")]
        [SerializeField] private LeyendaDefinicion definicion;

        private bool yaTermino;

        // En Start() (no OnEnable): Unity solo garantiza que todos los Awake() corran
        // antes que los Start(), así que acá GameStateManager.Instance ya existe.
        private void Start()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += ManejarCambioEstado;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= ManejarCambioEstado;
        }

        private void ManejarCambioEstado(GameState nuevo)
        {
            if (yaTermino || nuevo != GameState.Victoria || definicion == null)
                return;

            yaTermino = true;
            SaveSystem.MarcarCompletada(definicion.id);

            if (definicion.siguienteLeyenda != null)
                SaveSystem.Desbloquear(definicion.siguienteLeyenda.id);
        }
    }
}
