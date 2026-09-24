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
        [Tooltip("El objeto o botón (ej. La Llave) que debe aparecer al abrir el armario. Se ignora si hay vistaPrimerPlano asignada.")]
        public GameObject objetoInterior;

        [Header("Primer plano (Fase 2)")]
        [Tooltip("Vista a pantalla completa que se muestra vía PanelInspeccion al abrir el armario (ropa, cajones, la lata...). Si se asigna, reemplaza al toggle inline de objetoInterior.")]
        [SerializeField] private GameObject vistaPrimerPlano;

        [Header("Sprite de las puertas (opcional)")]
        [Tooltip("Image del propio ArmarioB. Si se asigna, este script cambia el sprite directamente en vez de depender del SpriteSwap del Button (que solo dura mientras el botón está presionado/seleccionado).")]
        [SerializeField] private Image imagenArmario;

        [SerializeField] private Sprite spriteAbierto;
        [SerializeField] private Sprite spriteCerrado;

        private bool estaAbierto = false;

        private void Start()
        {
            // Nos aseguramos de que al iniciar el juego, la llave esté oculta porque el armario está cerrado.
            if (objetoInterior != null)
            {
                objetoInterior.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // El armario se cierra solo apenas se apaga el fósforo: sin luz no
            // tiene sentido seguir viendo el interior (decisión de diseño 2026-09-15).
            Terror.GameEvents.OnFosforoApagado += CerrarArmario;

            // Si el primer plano se cierra por otra vía (Escape, click afuera —
            // PanelInspeccion.Cerrar() lo maneja solo), nos enteramos igual y
            // sincronizamos el sprite de las puertas / objetoInterior inline.
            PanelInspeccion.OnCerrado += SincronizarConPanelCerrado;
        }

        private void OnDisable()
        {
            Terror.GameEvents.OnFosforoApagado -= CerrarArmario;
            PanelInspeccion.OnCerrado -= SincronizarConPanelCerrado;
        }

        /// <summary>
        /// Este método cambia entre abierto y cerrado.
        /// Conéctalo al evento OnClick() del botón ArmarioB.
        /// </summary>
        public void AlternarArmario()
        {
            Debug.Log($"[ControladorArmario] AlternarArmario() llamado. estaAbierto antes={estaAbierto}, vistaPrimerPlano={(vistaPrimerPlano != null ? vistaPrimerPlano.name : "NULL")}, PanelInspeccion.Instance={(PanelInspeccion.Instance != null ? "OK" : "NULL")}");

            if (estaAbierto)
            {
                CerrarArmario();
                return;
            }

            estaAbierto = true;
            AplicarEstado();

            if (vistaPrimerPlano != null && PanelInspeccion.Instance != null)
            {
                Debug.Log("[ControladorArmario] Llamando PanelInspeccion.Instance.Mostrar(vistaPrimerPlano)...");
                PanelInspeccion.Instance.Mostrar(vistaPrimerPlano);
                Debug.Log($"[ControladorArmario] Después de Mostrar(): vistaPrimerPlano.activeSelf={vistaPrimerPlano.activeSelf}, activeInHierarchy={vistaPrimerPlano.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning("[ControladorArmario] NO se llamó a Mostrar() -- vistaPrimerPlano o PanelInspeccion.Instance es null.");
            }
        }

        /// <summary>
        /// Cierra el armario. Se llama sola al apagarse el fósforo, y también
        /// se puede conectar a mano (por ejemplo, a un botón "cerrar").
        /// </summary>
        public void CerrarArmario()
        {
            if (!estaAbierto) return;

            // Con primer plano: cerramos vía PanelInspeccion (dispara OnCerrado,
            // que es quien realmente sincroniza nuestro estado — ver
            // SincronizarConPanelCerrado). Así hay un solo camino de cierre real,
            // sea cual sea el motivo (fósforo apagado, Escape, click afuera).
            if (vistaPrimerPlano != null && PanelInspeccion.Instance != null && PanelInspeccion.Instance.EstaAbierto)
            {
                PanelInspeccion.Instance.Cerrar();
                return;
            }

            estaAbierto = false;
            AplicarEstado();
        }

        private void SincronizarConPanelCerrado()
        {
            if (!estaAbierto) return; // el panel que se cerró no era el nuestro

            estaAbierto = false;
            AplicarEstado();
        }

        private void AplicarEstado()
        {
            if (objetoInterior != null && vistaPrimerPlano == null)
                objetoInterior.SetActive(estaAbierto);

            if (imagenArmario != null)
            {
                Sprite deseado = estaAbierto ? spriteAbierto : spriteCerrado;
                if (deseado != null)
                    imagenArmario.sprite = deseado;
            }
        }
    }
}
