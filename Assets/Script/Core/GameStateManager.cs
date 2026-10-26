using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Terror
{
    public enum GameState
    {
        Inicio,
        Juego,
        Derrota,
        Victoria,
        Reinicio,
    }

    // Maquina de estados global: Inicio -> Juego -> Derrota/Victoria -> Reinicio.
    // Otros sistemas (Fosforo de Dev A, Presencia de Dev C) deben suscribirse a
    // OnStateChanged en vez de leer CurrentState en Update para reaccionar a transiciones.
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Inicio;

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Todavia no existe pantalla de inicio (eso es Dia 2), asi que
            // entrar a JUEGO.unity arranca la partida directamente.
            IniciarJuego();
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void IniciarJuego() => SetState(GameState.Juego);

        // Solo se gana durante la partida: si ya se perdió (o se está reiniciando),
        // una secuencia que termine tarde no puede convertir la derrota en victoria.
        public void Ganar()
        {
            if (CurrentState != GameState.Juego) return;
            SetState(GameState.Victoria);
        }

        // Desde Victoria sí se permite: CondiciondeVictoria fuerza la derrota si se
        // gana sin fósforos o con el miedo al máximo.
        public void Perder()
        {
            if (CurrentState != GameState.Juego && CurrentState != GameState.Victoria) return;
            SetState(GameState.Derrota);
        }

        public void Reiniciar()
        {
            // timeScale es global y sobrevive a la recarga: si el tutorial o la pausa
            // lo dejaron en 0, la escena nueva arrancaría congelada.
            Time.timeScale = 1f;
            SetState(GameState.Reinicio);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
