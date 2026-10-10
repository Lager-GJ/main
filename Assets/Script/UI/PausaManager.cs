using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Terror
{
    /// <summary>
    /// Pausa manual del juego, invocable por el jugador en cualquier momento (P o
    /// Escape) — a diferencia de ManagerTutorial/TutorialManager, que solo pausan una
    /// vez al inicio de forma forzada. Usa Time.timeScale = 0, el mismo patrón que ya
    /// usan esos dos scripts, así que es consistente con el resto del proyecto.
    /// Botones del panel: Continuar, Reiniciar, Configuración y Menú principal.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        private const float PasoVolumen = 0.1f;

        public static PauseManager Instance { get; private set; }

        [Tooltip("Panel de UI con las opciones de pausa (Continuar / Reiniciar / Configuración / Menú principal). Puede quedar vacío mientras no exista el diseño final.")]
        [SerializeField] private GameObject panelPausa;

        [Tooltip("Panel de configuración que se abre desde la pausa. Escape vuelve de él al panel de pausa.")]
        [SerializeField] private GameObject panelConfiguracion;

        [Tooltip("Texto donde se muestra el volumen actual (\"VOLUMEN 80%\").")]
        [SerializeField] private TMP_Text textoVolumen;

        [Tooltip("Escena del menú principal (la misma que usa Scriptcambio.Volver).")]
        [SerializeField] private string escenaMenuPrincipal = "Intro";

        public bool EstaPausado { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panelPausa != null)
                panelPausa.SetActive(false);
            if (panelConfiguracion != null)
                panelConfiguracion.SetActive(false);

            // El volumen elegido queda guardado en el perfil; se aplica al entrar a
            // la partida para que no vuelva al 100% cada vez que se abre el juego.
            AplicarVolumen(SaveSystem.Cargar().volMaster);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            bool teclaPausa = Keyboard.current.pKey.wasPressedThisFrame ||
                               Keyboard.current.escapeKey.wasPressedThisFrame;

            if (!teclaPausa)
                return;

            // Desde la configuración, Escape/P regresa al panel de pausa en vez de
            // reanudar directamente.
            if (EstaPausado && panelConfiguracion != null && panelConfiguracion.activeSelf)
            {
                CerrarConfiguracion();
                return;
            }

            // Si el panel de inspección está abierto, dejamos que Escape lo cierre a
            // él primero (su propio Update ya lo maneja) — no pausamos encima en el
            // mismo click para evitar que las dos cosas reaccionen a la vez.
            if (!EstaPausado && PanelInspeccion.Instance != null && PanelInspeccion.Instance.EstaAbierto)
                return;

            // Solo se puede iniciar la pausa durante la partida (no en Inicio/Derrota/Victoria).
            if (!EstaPausado && GameStateManager.Instance != null &&
                GameStateManager.Instance.CurrentState != GameState.Juego)
                return;

            if (EstaPausado)
                Reanudar();
            else
                Pausar();
        }

        public void Pausar()
        {
            EstaPausado = true;
            Time.timeScale = 0f;

            if (panelPausa != null)
                panelPausa.SetActive(true);
        }

        /// <summary>Conectado al botón "Continuar" del panel de pausa.</summary>
        public void Reanudar()
        {
            EstaPausado = false;
            Time.timeScale = 1f;

            if (panelPausa != null)
                panelPausa.SetActive(false);
            if (panelConfiguracion != null)
                panelConfiguracion.SetActive(false);
        }

        /// <summary>Botón "Reiniciar": vuelve a cargar la escena actual desde cero.</summary>
        public void Reiniciar()
        {
            // timeScale es global y sobrevive a la recarga: sin esto la escena nueva
            // arrancaría congelada.
            Time.timeScale = 1f;
            EstaPausado = false;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.Reiniciar();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Botón "Menú principal".</summary>
        public void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            EstaPausado = false;
            SceneManager.LoadScene(escenaMenuPrincipal);
        }

        /// <summary>Botón "Configuración": cambia el panel de pausa por el de configuración.</summary>
        public void AbrirConfiguracion()
        {
            if (panelConfiguracion == null)
                return;

            if (panelPausa != null)
                panelPausa.SetActive(false);
            panelConfiguracion.SetActive(true);
            ActualizarTextoVolumen();
        }

        /// <summary>Botón "Volver" de la configuración.</summary>
        public void CerrarConfiguracion()
        {
            if (panelConfiguracion != null)
                panelConfiguracion.SetActive(false);
            if (panelPausa != null)
                panelPausa.SetActive(true);
        }

        public void SubirVolumen() => CambiarVolumen(PasoVolumen);

        public void BajarVolumen() => CambiarVolumen(-PasoVolumen);

        private void CambiarVolumen(float delta)
        {
            // Redondeo a décimas para que 10 pulsaciones den exactamente 0% o 100%.
            float nuevo = Mathf.Round((AudioListener.volume + delta) * 10f) / 10f;
            AplicarVolumen(nuevo);

            PerfilJugador perfil = SaveSystem.Cargar();
            perfil.volMaster = AudioListener.volume;
            SaveSystem.Guardar(perfil);
        }

        private void AplicarVolumen(float valor)
        {
            valor = Mathf.Clamp01(valor);

            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolMaster(valor);
            else
                AudioListener.volume = valor;

            ActualizarTextoVolumen();
        }

        private void ActualizarTextoVolumen()
        {
            if (textoVolumen != null)
                textoVolumen.text = $"VOLUMEN {Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }
    }
}
