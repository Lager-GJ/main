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
            Debug.Log($"[PanelInspeccion] Mostrar('{(vista != null ? vista.name : "NULL")}') llamado. vistaActual antes={(vistaActual != null ? vistaActual.name : "NULL")}");

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

            Debug.Log($"[PanelInspeccion] '{vista.name}'.SetActive(true) ejecutado. activeSelf={vista.activeSelf}");
        }

        public void Cerrar()
        {
            if (vistaActual != null)
            {
                vistaActual.SetActive(false);
                vistaActual = null;

                if (FosforoManager.Instance != null)
                {
                    FosforoManager.Instance.ReanudarQuemado();
                }

                OnCerrado?.Invoke();
            }
        }
    }
}
