using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror.UI
{
    /// <summary>
    /// Este script muestra un texto u objeto de UI por un tiempo determinado,
    /// o hasta que el jugador haga clic en cualquier parte de la pantalla.
    /// </summary>
    public class MensajeTemporal : MonoBehaviour
    {
        [Header("Configuración del Mensaje")]
        [Tooltip("El GameObject del texto que se va a mostrar.")]
        public GameObject textoMostrar;
        
        [Tooltip("Tiempo en segundos antes de que el texto desaparezca.")]
        public float tiempoVisible = 6f;

        private float timer = 0f;
        private bool estaMostrando = false;

        private void Start()
        {
            // Aseguramos que el texto inicie apagado al arrancar
            if (textoMostrar != null)
            {
                textoMostrar.SetActive(false);
            }
        }

        private void Update()
        {
            if (!estaMostrando) return;

            // Pequeña protección para evitar que el mismo clic que activó el texto lo desactive
            if (timer == tiempoVisible) 
            {
                timer -= Time.deltaTime;
                return;
            }

            timer -= Time.deltaTime;

            // Si se acaba el tiempo o si el jugador hace clic en la pantalla
            if (timer <= 0f || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
            {
                OcultarTexto();
            }
        }

        /// <summary>
        /// Llama a esta función desde el evento 'AlInteractuar' del botón.
        /// </summary>
        public void MostrarTexto()
        {
            if (textoMostrar != null)
            {
                textoMostrar.SetActive(true);
                timer = tiempoVisible;
                estaMostrando = true;
            }
        }

        private void OcultarTexto()
        {
            if (textoMostrar != null)
            {
                textoMostrar.SetActive(false);
            }
            estaMostrando = false;
        }
    }
}
