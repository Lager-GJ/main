using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    //
    // Al abrir una vista (pedido de David, 2026-10-22): el tiempo empieza a pasar lento
    // (TiempoLento), la cámara hace zoom hacia el objeto del cuarto que se tocó
    // (CamaraController.Enfocar) y el fósforo deja de consumirse. Recién cuando la
    // cámara llegó aparece el primer plano. Al cerrar, todo vuelve.
    public class PanelInspeccion : MonoBehaviour
    {
        public static PanelInspeccion Instance { get; private set; }

        [Header("Al abrir un primer plano")]
        [Tooltip("El fósforo no se gasta mientras se mira algo de cerca.")]
        [SerializeField] private bool pausarFosforo = true;
        [Tooltip("Cámara lenta: velocidad del tiempo del cuarto mientras hay un primer plano (1 = normal).")]
        [SerializeField, Range(0.05f, 1f)] private float velocidadTiempoLento = 0.3f;
        [Tooltip("Zoom hacia el objeto: fracción de lo que se ve normalmente (0.55 = se acerca casi al doble).")]
        [SerializeField, Range(0.2f, 1f)] private float zoomHaciaObjeto = 0.55f;
        [Tooltip("Segundos (reales) del zoom de ida y de vuelta.")]
        [SerializeField] private float duracionZoom = 0.45f;
        [Tooltip("Parte del zoom que ocurre antes de que aparezca el primer plano (0 = aparece de una).")]
        [SerializeField, Range(0f, 1f)] private float esperaAntesDeMostrar = 0.75f;

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
        private Coroutine rutinaMostrar;

        /// <summary>La vista abierta (o null).</summary>
        public GameObject VistaActual => vistaActual;

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

        public void Mostrar(GameObject vista) => Mostrar(vista, null);

        /// <summary>
        /// Abre una vista de primer plano. 'origen' es el objeto del cuarto que se tocó
        /// (el armario, el joyero...): la cámara hace zoom hacia él. Sin origen se usa
        /// el botón que se acaba de clickear; si no es del mundo, no hay zoom.
        /// </summary>
        public void Mostrar(GameObject vista, Transform origen)
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

            if (pausarFosforo) FosforoManager.Instance?.PausarQuemado(this);
            TiempoLento.Factor = velocidadTiempoLento;
            TiempoLento.Duracion = duracionZoom;
            TiempoLento.Pedir(this);

            Vector3? punto = PuntoDelMundo(origen);
            var camara = CamaraController.Instance;
            if (punto.HasValue && camara != null)
            {
                camara.Enfocar(punto.Value, zoomHaciaObjeto, duracionZoom);
                rutinaMostrar = StartCoroutine(MostrarTrasZoom(vista, duracionZoom * esperaAntesDeMostrar));
            }
            else
            {
                MostrarVista(vista);
            }
        }

        private IEnumerator MostrarTrasZoom(GameObject vista, float espera)
        {
            for (float t = 0f; t < espera; t += Time.unscaledDeltaTime)
                yield return null;
            rutinaMostrar = null;
            if (vistaActual == vista) MostrarVista(vista);
        }

        private static void MostrarVista(GameObject vista)
        {
            // Si la vista trae TransicionPanel, ella se encarga del fade de entrada.
            var transicion = vista.GetComponent<Terror.UI.TransicionPanel>();
            if (transicion != null)
                transicion.Mostrar();
            else
                vista.SetActive(true);
        }

        // Punto del cuarto hacia el que hacer zoom: el origen dado o el botón recién
        // clickeado, solo si está en el mundo (un Canvas World Space o sin Canvas).
        private static Vector3? PuntoDelMundo(Transform origen)
        {
            if (origen == null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                origen = EventSystem.current.currentSelectedGameObject.transform;
            if (origen == null) return null;

            var canvas = origen.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace) return null;

            if (origen is RectTransform rt)
            {
                // El centro del rectángulo, no su pivote.
                var esquinas = new Vector3[4];
                rt.GetWorldCorners(esquinas);
                return (esquinas[0] + esquinas[2]) * 0.5f;
            }
            return origen.position;
        }

        public void Cerrar()
        {
            if (vistaActual != null && !CierreBloqueado)
            {
                var vista = vistaActual;
                vistaActual = null;
                MarcarCierre();

                if (rutinaMostrar != null)
                {
                    // Se cerró mientras la cámara todavía se acercaba: nunca llegó a verse.
                    StopCoroutine(rutinaMostrar);
                    rutinaMostrar = null;
                }
                else
                {
                    var transicion = vista.GetComponent<Terror.UI.TransicionPanel>();
                    if (transicion != null)
                        transicion.Ocultar();
                    else
                        vista.SetActive(false);
                }

                CamaraController.Instance?.Soltar(duracionZoom);
                TiempoLento.Soltar(this);

                // Suelta la pausa propia y la "común" (la de ObjetoInteractivo). El
                // inventario o el candado tienen la suya y la sueltan ellos.
                if (FosforoManager.Instance != null)
                {
                    FosforoManager.Instance.ReanudarQuemado(this);
                    FosforoManager.Instance.ReanudarQuemado();
                }

                OnCerrado?.Invoke();
            }
        }
    }
}
