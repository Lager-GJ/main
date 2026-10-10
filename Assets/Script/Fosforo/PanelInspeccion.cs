using System;
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

        // Estatico (no un evento de instancia) a proposito, mismo patron que
        // GameEvents/FosforoManager: quien se suscribe en su propio OnEnable no
        // depende de que Instance ya exista (a diferencia de suscribirse a
        // GameStateManager.Instance.OnStateChanged, que si tiene ese riesgo).
        public static event Action OnCerrado;

        public bool EstaAbierto => vistaActual != null;

        // Mientras sea true, ni Escape ni click afuera ni Cerrar() cierran la vista.
        // Lo usa SecuenciaAperturaLata: su coroutine vive dentro de la vista, así
        // que cerrarla a mitad de camino dejaría la victoria sin dispararse.
        public bool CierreBloqueado { get; set; }

        private GameObject vistaActual;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            if (vista == null || CierreBloqueado)
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

            // Si la vista trae TransicionPanel, ella se encarga del fade de entrada.
            var transicion = vista.GetComponent<Terror.UI.TransicionPanel>();
            if (transicion != null)
                transicion.Mostrar();
            else
                vista.SetActive(true);
        }

        public void Cerrar()
        {
            if (vistaActual != null && !CierreBloqueado)
            {
                var vista = vistaActual;
                vistaActual = null;

                var transicion = vista.GetComponent<Terror.UI.TransicionPanel>();
                if (transicion != null)
                    transicion.Ocultar();
                else
                    vista.SetActive(false);

                if (FosforoManager.Instance != null)
                {
                    FosforoManager.Instance.ReanudarQuemado();
                }

                OnCerrado?.Invoke();
            }
        }
    }
}
