using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Adapta la Leyenda 1 al contrato LeyendaController del shell. No duplica la
    /// logica de estado: traduce los eventos de GameStateManager y, al ganar, deja
    /// registrado el progreso en el perfil.
    ///
    /// Va en el mismo GameObject que GameStateManager, en la escena L1_Juego.
    /// </summary>
    [RequireComponent(typeof(GameStateManager))]
    public class L1Controller : LeyendaController
    {
        [Tooltip("La LeyendaDefinicion de esta leyenda (Leyenda_L1_CajaFosforos). De aca sale el id que se guarda como completada.")]
        [SerializeField] private LeyendaDefinicion definicion;

        private GameStateManager estado;
        private bool yaTermino;

        private void Awake()
        {
            estado = GetComponent<GameStateManager>();
        }

        private void OnEnable()
        {
            if (estado != null)
                estado.OnStateChanged += ManejarCambioEstado;
        }

        private void OnDisable()
        {
            if (estado != null)
                estado.OnStateChanged -= ManejarCambioEstado;
        }

        private void Start()
        {
            Iniciar(definicion);
        }

        public override void Iniciar(LeyendaDefinicion def)
        {
            // La Leyenda 1 todavia no varia segun su definicion (un solo escenario,
            // sin variantes). El hook queda listo para la Semana 5, cuando el
            // catalogo alimente los 7 objetos ecuatorianos.
            yaTermino = false;
        }

        private void ManejarCambioEstado(GameState nuevo)
        {
            // Una sola vez por partida: Victoria y Derrota ya son estados terminales
            // en GameStateManager, pero esto protege ante un Reiniciar que vuelva a
            // pasar por aca.
            if (yaTermino) return;

            switch (nuevo)
            {
                case GameState.Victoria:
                    yaTermino = true;
                    if (definicion != null)
                    {
                        SaveSystem.MarcarCompletada(definicion.id);

                        // Secuencia de "Los secretos de la casa": completar un cuarto
                        // desbloquea el siguiente. Vacio en el ultimo de la cadena.
                        if (definicion.siguienteLeyenda != null)
                            SaveSystem.Desbloquear(definicion.siguienteLeyenda.id);
                    }
                    Terminar(ResultadoLeyenda.Victoria);
                    break;

                case GameState.Derrota:
                    yaTermino = true;
                    Terminar(ResultadoLeyenda.Derrota);
                    break;
            }
        }
    }
}
