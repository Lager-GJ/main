using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror
{
    // Controla que "vista de primer plano" esta visible. Solo puede haber una
    // a la vez: mostrar una nueva oculta la anterior. Click en cualquier lado
    // (fuera de un objeto) o Escape cierra la que este abierta.
    public class PanelInspeccion : MonoBehaviour
    {
        public static PanelInspeccion Instance { get; private set; }

        public bool EstaAbierto => vistaActual != null;

        private GameObject vistaActual;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            // Higiene de singleton: no dejar Instance apuntando a un objeto destruido.
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!EstaAbierto)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cerrar();
            }
        }

        public void Mostrar(GameObject vista)
        {
            if (vista == null)
            {
                return;
            }

            if (vistaActual == vista)
            {
                Cerrar();
                return;
            }

            Cerrar();

            vistaActual = vista;
            vistaActual.SetActive(true);
        }

        public void Cerrar()
        {
            if (vistaActual != null)
            {
                vistaActual.SetActive(false);
                vistaActual = null;
            }
        }
    }
}
