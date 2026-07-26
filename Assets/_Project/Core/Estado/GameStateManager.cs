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
        // Pausa va AL FINAL a proposito: los enum se serializan por entero, asi que
        // insertarlo en el medio cambiaria el significado de los valores ya guardados
        // en escenas y assets.
        Pausa,
    }

    // Maquina de estados global: Inicio -> Juego -> Derrota/Victoria -> Reinicio,
    // con Pausa como desvio temporal desde Juego.
    // Otros sistemas (Fosforo de Dev A, Presencia de Dev C) deben suscribirse a
    // OnStateChanged en vez de leer CurrentState en Update para reaccionar a transiciones.
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Inicio;

        public event Action<GameState> OnStateChanged;

        // Adonde volver al despausar. Se guarda en vez de asumir Juego para no
        // romper si mas adelante se puede pausar desde otro estado.
        private GameState estadoAntesDePausa = GameState.Juego;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // Higiene de singleton. Al recargar escena Unity ya se autorepara solo
            // (un objeto destruido compara == null por la sobrecarga de operadores
            // de UnityEngine.Object), pero dejar Instance colgando a un objeto muerto
            // es una trampa para cualquiera que lo consulte fuera de ese camino.
            if (Instance == this)
                Instance = null;

            // Seguro extra: si la escena se descarga estando en pausa, el timeScale
            // global se quedaria en 0 y la siguiente escena arrancaria congelada.
            if (CurrentState == GameState.Pausa)
                Time.timeScale = 1f;
        }

        private void Start()
        {
            // Todavia no hay pantalla de inicio dentro de la escena de juego: entrar
            // arranca la partida directamente. El menu de verdad es una escena
            // aparte (01_Menu), no un estado de aca.
            IniciarJuego();
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void IniciarJuego() => SetState(GameState.Juego);

        // Ganar y Perder solo valen a mitad de partida. Sin esta guarda se podia
        // perder despues de haber ganado (el miedo seguia subiendo tras la victoria)
        // o ganar despues de perder.
        public void Ganar()
        {
            if (CurrentState == GameState.Juego)
                SetState(GameState.Victoria);
        }

        public void Perder()
        {
            if (CurrentState == GameState.Juego)
                SetState(GameState.Derrota);
        }

        /// <summary>
        /// Pausa la partida. Usa Time.timeScale = 0 ademas del estado porque hacen
        /// falta los dos: el timeScale congela todo lo que avanza por Time.deltaTime
        /// (temporizador del fosforo, miedo, Presencia) sin tener que tocar cada
        /// script, pero NO detiene Update(), asi que el estado es lo que impide que
        /// la tecla E siga gastando fosforos con el menu de pausa abierto.
        /// </summary>
        public void Pausar()
        {
            if (CurrentState != GameState.Juego) return;

            estadoAntesDePausa = CurrentState;
            SetState(GameState.Pausa);
            Time.timeScale = 0f;
        }

        public void Reanudar()
        {
            if (CurrentState != GameState.Pausa) return;

            Time.timeScale = 1f;
            SetState(estadoAntesDePausa);
        }

        public void Reiniciar()
        {
            // Restaurar el timeScale ANTES de cargar: este metodo se llama desde el
            // menu de pausa (timeScale = 0) y salta el SceneRouter, asi que sin esto
            // la escena recargada arrancaria congelada.
            Time.timeScale = 1f;
            SetState(GameState.Reinicio);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
