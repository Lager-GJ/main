using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror
{
    /// <summary>
    /// Algo que se abre encima del juego o de un primer plano (la tarjeta de un objeto,
    /// el inventario, el candado...) y que Escape cierra antes que todo lo demás.
    /// </summary>
    public interface ICapaEscape
    {
        bool CapaAbierta { get; }
        void CerrarCapa();
    }

    // Controla que "vista de primer plano" esta visible. Solo puede haber una
    // a la vez: mostrar una nueva oculta la anterior. Click en cualquier lado
    // (fuera de un objeto) o Escape cierra la que este abierta.
    //
    // Además ordena las "capas" (ICapaEscape): Escape cierra primero la última capa
    // que se abrió, después la vista, y recién entonces le toca a la pausa.
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
        // que cerrarla a mitad de camino dejaría la secuencia sin terminar.
        public bool CierreBloqueado { get; set; }

        // Frame en que se cerró la última vista o capa. PauseManager lo mira para que
        // un mismo Escape no cierre algo y abra la pausa a la vez.
        public int FrameUltimoCierre { get; private set; } = -1;

        /// <summary>Hay algo abierto encima (una tarjeta, el inventario...).</summary>
        public bool HayCapaAbierta => CapaSuperior() != null;

        /// <summary>
        /// True si un Escape ahora lo resolvería este panel (cerrando una capa o la
        /// vista). Si es false, Escape le corresponde a la pausa.
        /// </summary>
        public bool EscapeLoManejaElPanel => HayCapaAbierta || (EstaAbierto && !CierreBloqueado);

        private GameObject vistaActual;

        // En orden de apertura: la última es la de arriba.
        private readonly List<ICapaEscape> capas = new List<ICapaEscape>();

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
            if (!EstaAbierto && capas.Count == 0)
            {
                return;
            }

            // Con el menú de pausa abierto, Escape es de la pausa (la cierra) y no
            // debe cerrar además lo que quedó debajo.
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ICapaEscape capa = CapaSuperior();
                if (capa != null)
                {
                    capa.CerrarCapa();
                    MarcarCierre();
                    return;
                }
                if (EstaAbierto) Cerrar();
            }
        }

        /// <summary>Avisar al abrir una capa: queda arriba de todo para Escape.</summary>
        public void AbrirCapa(ICapaEscape capa)
        {
            if (capa == null) return;
            capas.Remove(capa);
            capas.Add(capa);
        }

        /// <summary>Avisar al cerrar una capa.</summary>
        public void QuitarCapa(ICapaEscape capa)
        {
            if (capas.Remove(capa)) MarcarCierre();
        }

        /// <summary>Para quien cierra algo propio (el inventario con su tecla) en este frame.</summary>
        public void MarcarCierre() => FrameUltimoCierre = Time.frameCount;

        private ICapaEscape CapaSuperior()
        {
            for (int i = capas.Count - 1; i >= 0; i--)
            {
                ICapaEscape c = capas[i];
                // Una capa destruida (cambio de escena) o que ya no está abierta se descarta.
                if (c == null || (c is UnityEngine.Object o && o == null) || !c.CapaAbierta)
                {
                    capas.RemoveAt(i);
                    continue;
                }
                return c;
            }
            return null;
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
                MarcarCierre();

                var transicion = vista.GetComponent<Terror.UI.TransicionPanel>();
                if (transicion != null)
                    transicion.Ocultar();
                else
                    vista.SetActive(false);

                // Solo suelta la pausa "común" (la de ObjetoInteractivo): el inventario o
                // el candado tienen la suya y la sueltan ellos.
                if (FosforoManager.Instance != null)
                {
                    FosforoManager.Instance.ReanudarQuemado();
                }

                OnCerrado?.Invoke();
            }
        }
    }
}
