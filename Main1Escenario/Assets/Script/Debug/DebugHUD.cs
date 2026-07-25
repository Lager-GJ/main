using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror
{
    // Script de prueba TEMPORAL para verificar Core/ (GameStateManager,
    // FearManager, AudioManager) contra el sistema real de Fosforo (Dev A) y
    // Presencia (Dev C). Nada de Core/ depende de este script — bórralo
    // (junto con esta carpeta) cuando ya no lo necesites.
    //
    // El fosforo se enciende con la tecla F (ya la maneja FosforoController de
    // Dev A). Este HUD solo agrega:
    //   P -> forzar derrota (GameStateManager.Perder)
    //   G -> forzar victoria (GameStateManager.Ganar)
    //   R -> reiniciar (GameStateManager.Reiniciar)
    public class DebugHUD : MonoBehaviour
    {
        [Tooltip("Cada cuantos segundos se imprime el estado actual en la Console.")]
        public float intervaloLogSegundos = 1f;

        private float tiempoParaSiguienteLog;
        private GUIStyle estilo;

        private void Start()
        {
            if (FearManager.Instance == null)
                Debug.LogWarning("[DebugHUD] No hay FearManager en la escena.");

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += s => Debug.Log($"[DebugHUD] GameState = {s}");
            else
                Debug.LogWarning("[DebugHUD] No hay GameStateManager en la escena.");

            if (FosforoController.Instance == null)
                Debug.LogWarning("[DebugHUD] No hay FosforoController en la escena.");
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.pKey.wasPressedThisFrame)
                GameStateManager.Instance?.Perder();

            if (kb.gKey.wasPressedThisFrame)
                GameStateManager.Instance?.Ganar();

            if (kb.rKey.wasPressedThisFrame)
                GameStateManager.Instance?.Reiniciar();

            tiempoParaSiguienteLog -= Time.deltaTime;
            if (tiempoParaSiguienteLog <= 0f)
            {
                tiempoParaSiguienteLog = intervaloLogSegundos;
                Debug.Log(ConstruirLineaEstado());
            }
        }

        private string ConstruirLineaEstado()
        {
            string estado = GameStateManager.Instance != null ? GameStateManager.Instance.CurrentState.ToString() : "sin GameStateManager";
            string miedo = FearManager.Instance != null ? FearManager.Instance.miedoActual.ToString("0.0") : "sin FearManager";
            string fosforo = FosforoController.Instance != null ? FosforoController.Instance.EstaEncendido.ToString() : "sin FosforoController";
            string restantes = FosforoController.Instance != null ? FosforoController.Instance.fosforosRestantes.ToString() : "-";

            return $"[DebugHUD] Estado={estado} | Miedo={miedo}/100 | Fosforo={fosforo} (restantes: {restantes})";
        }

        private void OnGUI()
        {
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label) { fontSize = 28, normal = { textColor = Color.white } };
            }

            GUI.Label(new Rect(10, 10, 900, 40), ConstruirLineaEstado(), estilo);
            GUI.Label(new Rect(10, 55, 900, 40), "F = encender fosforo, P = forzar derrota, G = forzar victoria, R = reiniciar", estilo);
        }
    }
}
