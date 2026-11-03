using TMPro;
using Terror.UI;
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
    /// "Configuración" abre PanelOpciones (audio, imagen, controles, accesibilidad); el
    /// viejo panelConfiguracion de la escena (solo volumen −/+) queda sin usar.
    /// La tecla de pausa se puede cambiar en Opciones → Controles; Escape siempre pausa.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        private const float PasoVolumen = 0.1f;

        public static PauseManager Instance { get; private set; }

        [Tooltip("Panel de UI con las opciones de pausa (Continuar / Reiniciar / Configuración / Menú principal). Puede quedar vacío mientras no exista el diseño final.")]
        [SerializeField] private GameObject panelPausa;

        [Tooltip("Panel viejo de configuración (solo volumen). Ya no se muestra: \"Configuración\" abre PanelOpciones, armado en código.")]
        [SerializeField] private GameObject panelConfiguracion;

        [Tooltip("Texto donde se muestra el volumen actual (\"VOLUMEN 80%\").")]
        [SerializeField] private TMP_Text textoVolumen;

        [Tooltip("Escena del menú principal (la misma que usa Scriptcambio.Volver).")]
        [SerializeField] private string escenaMenuPrincipal = "Intro";

        public bool EstaPausado { get; private set; }

        private float timeScaleAntesDePausar = 1f;

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

            // Los volúmenes guardados los aplica AudioManager al arrancar el juego.
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

            bool teclaP = Controles.PulsadaPrincipal(Accion.Pausa);
            bool teclaEscape = Keyboard.current.escapeKey.wasPressedThisFrame;

            if (!teclaP && !teclaEscape)
                return;

            // Las opciones manejan su propio Escape (vuelven a la pausa) y, mientras
            // esperan una tecla nueva, cualquier tecla es para ellas.
            if (PanelOpciones.EstaAbierto || PanelOpciones.FrameCierre == Time.frameCount)
                return;

            // Con un primer plano abierto, Escape lo cierra a él (su propio Update lo
            // maneja) y no pausa encima. Si se cerró en este mismo frame tampoco: según
            // el orden de los Update, el panel ya puede figurar como cerrado.
            // P pausa siempre, y Escape también si el primer plano no se puede cerrar
            // (el cajón del inicio antes del primer fósforo): si no, en todo el inicio
            // no había forma de llegar al menú de pausa.
            if (!EstaPausado && teclaEscape && !teclaP && PanelInspeccion.Instance != null)
            {
                var panel = PanelInspeccion.Instance;
                if (panel.FrameUltimoCierre == Time.frameCount)
                    return;
                if (panel.EscapeLoManejaElPanel)
                    return;
            }

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
            if (EstaPausado)
                return;

            // Un paso del tutorial también congela el juego con timeScale 0: al
            // reanudar se vuelve a ese valor, no a 1, para no soltar al tutorial.
            timeScaleAntesDePausar = Time.timeScale;
            EstaPausado = true;
            Time.timeScale = 0f;

            if (panelPausa != null)
                panelPausa.SetActive(true);
        }

        /// <summary>Conectado al botón "Continuar" del panel de pausa.</summary>
        public void Reanudar()
        {
            if (!EstaPausado)
                return;

            EstaPausado = false;
            Time.timeScale = timeScaleAntesDePausar;

            if (panelPausa != null)
                panelPausa.SetActive(false);
            if (panelConfiguracion != null)
                panelConfiguracion.SetActive(false);
            PanelOpciones.CerrarSiEstaAbierto();
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

        /// <summary>Botón "Configuración": cambia el panel de pausa por las opciones.</summary>
        public void AbrirConfiguracion()
        {
            if (panelPausa != null)
                panelPausa.SetActive(false);
            if (panelConfiguracion != null)
                panelConfiguracion.SetActive(false);

            // Al volver de las opciones reaparece la pausa (si se sigue en pausa).
            PanelOpciones.Abrir(() =>
            {
                if (EstaPausado && panelPausa != null)
                    panelPausa.SetActive(true);
            });
        }

        /// <summary>Botón "Volver" del panel viejo de configuración.</summary>
        public void CerrarConfiguracion()
        {
            if (panelConfiguracion != null)
                panelConfiguracion.SetActive(false);
            PanelOpciones.CerrarSiEstaAbierto();
            if (panelPausa != null)
                panelPausa.SetActive(true);
        }

        public void SubirVolumen() => CambiarVolumen(PasoVolumen);

        public void BajarVolumen() => CambiarVolumen(-PasoVolumen);

        // Los botones −/+ del panel viejo siguen funcionando, ahora sobre Preferencias.
        private void CambiarVolumen(float delta)
        {
            // Redondeo a décimas para que 10 pulsaciones den exactamente 0% o 100%.
            Preferencias.VolMaster = Mathf.Round((Preferencias.VolMaster + delta) * 10f) / 10f;
            Preferencias.Guardar();
            if (textoVolumen != null)
                textoVolumen.text = $"VOLUMEN {Mathf.RoundToInt(Preferencias.VolMaster * 100f)}%";
        }
    }
}
