using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace Terror.UI
{
    /// <summary>
    /// Escucha cuando el jugador interactúa con un ObjetoInteractivo y muestra 
    /// su texto narrativo (Descripción) en la pantalla.
    /// También permite cerrar el texto haciendo clic o presionando Escape.
    /// </summary>
    public class MostrarNarrativa : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("El panel oscuro o fondo que contiene el texto (GameObject).")]
        public GameObject panelNarrativa;
        
        [Tooltip("El componente de texto donde se escribirá la historia.")]
        public TextMeshProUGUI textoNarrativa;

        private ObjetoInteractivo objetoActual;

        private void OnEnable()
        {
            // Nos suscribimos al evento del ObjetoInteractivo
            ObjetoInteractivo.OnObjetoInspeccionado += AbrirNarrativa;
        }

        private void OnDisable()
        {
            ObjetoInteractivo.OnObjetoInspeccionado -= AbrirNarrativa;
        }

        private void Start()
        {
            // Asegurarse de que inicia oculto
            if (panelNarrativa != null)
                panelNarrativa.SetActive(false);
        }

        private void AbrirNarrativa(ObjetoInteractivo objeto)
        {
            // Si el objeto no tiene descripción, no hacemos nada
            if (string.IsNullOrEmpty(objeto.Descripcion)) return;

            objetoActual = objeto;

            if (textoNarrativa != null)
                textoNarrativa.text = objeto.Descripcion;

            if (panelNarrativa != null)
                panelNarrativa.SetActive(true);
        }

        private void Update()
        {
            // Si el panel no está activo, no hay nada que cerrar
            if (panelNarrativa == null || !panelNarrativa.activeSelf) return;

            // Detectar clic izquierdo o la tecla Escape para cerrar
            bool clic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            if (clic || escape)
            {
                Cerrar();
            }
        }

        public void Cerrar()
        {
            if (panelNarrativa != null)
                panelNarrativa.SetActive(false);

            if (objetoActual != null)
            {
                // Esto le avisa al objeto que ya dejamos de leer,
                // lo que automáticamente reanuda el consumo del fósforo.
                objetoActual.CerrarInspeccion(); 
                objetoActual = null;
            }
        }
    }
}
