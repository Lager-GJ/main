using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Controla el estado de un contenedor (como un Armario) para mostrar u ocultar
    /// los objetos (como una llave) que están en su interior al interactuar con él.
    /// </summary>
    public class ControladorArmario : MonoBehaviour
    {
        [Tooltip("El objeto o botón (ej. La Llave) que debe aparecer al abrir el armario.")]
        public GameObject objetoInterior;

        private bool estaAbierto = false;

        private void Start()
        {
            // Nos aseguramos de que al iniciar el juego, la llave esté oculta porque el armario está cerrado.
            if (objetoInterior != null)
            {
                objetoInterior.SetActive(false);
            }
        }

        /// <summary>
        /// Este método cambia entre abierto y cerrado. 
        /// Conéctalo al evento OnClick() del botón ArmarioB.
        /// </summary>
        public void AlternarArmario()
        {
            estaAbierto = !estaAbierto;

            if (objetoInterior != null)
            {
                objetoInterior.SetActive(estaAbierto);
            }
        }

        /// <summary>
        /// Si quieres que el armario se cierre automáticamente cuando deseleccionas o haces clic en otro lado,
        /// puedes llamar a este método.
        /// </summary>
        public void CerrarArmario()
        {
            estaAbierto = false;
            
            if (objetoInterior != null)
            {
                objetoInterior.SetActive(false);
            }
        }
    }
}
