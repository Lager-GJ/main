using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Un objeto del cuarto que al tocarlo se ve en primer plano (PrimerPlanoObjeto):
    /// clic para darle la vuelta e "Inspeccionar" para leerlo o usarlo.
    ///
    /// Se conecta al clic del objeto (Button → Mostrar). Si 'alInspeccionar' tiene algo
    /// (la cerradura de la lata, el candado del joyero), el botón hace eso; si no, lee
    /// los textos de a uno.
    /// </summary>
    public class ObjetoEnPrimerPlano : MonoBehaviour
    {
        [SerializeField] private string nombre;
        [Tooltip("Vacío = el dibujo de la Image de este objeto.")]
        [SerializeField] private Sprite frente;
        [Tooltip("Vacío = no se da vuelta.")]
        [SerializeField] private Sprite reverso;
        [Tooltip("Descripción: una página por cada clic en Inspeccionar (si no hay acción).")]
        [TextArea(1, 3)] [SerializeField] private string[] textos;
        [Tooltip("Vacío = \"Inspeccionar\".")]
        [SerializeField] private string textoBoton;
        [Tooltip("Solo se puede tocar con un fósforo encendido (como todo el cuarto).")]
        [SerializeField] private bool requiereFosforo = true;
        [Tooltip("Lo que hace Inspeccionar (vacío = leer los textos). El primer plano se cierra antes.")]
        [SerializeField] private UnityEvent alInspeccionar;

        /// <summary>Conectar al clic del objeto.</summary>
        public void Mostrar()
        {
            if (!PuedeTocar()) return;
            var pp = PrimerPlanoObjeto.Instance;
            if (pp == null)
            {
                // Sin primer plano en la escena: se hace directo lo que haría el botón.
                if (TieneAccion) alInspeccionar.Invoke();
                return;
            }

            Sprite f = frente;
            if (f == null)
            {
                var img = GetComponent<Image>();
                if (img != null) f = img.sprite;
            }

            pp.Mostrar(new PrimerPlanoObjeto.Datos
            {
                nombre = nombre,
                frente = f,
                reverso = reverso,
                textos = textos,
                textoBoton = textoBoton,
                accion = TieneAccion ? () => alInspeccionar.Invoke() : null,
                cerrarAntesDeAccion = true,
            });
        }

        private bool TieneAccion => alInspeccionar != null && alInspeccionar.GetPersistentEventCount() > 0;

        private bool PuedeTocar()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return false;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return false;
            var panel = PanelInspeccion.Instance;
            if (panel != null && (panel.CierreBloqueado || panel.HayCapaAbierta)) return false;
            if (requiereFosforo && FosforoManager.Instance != null && !FosforoManager.Instance.PuedeInteractuar()) return false;
            return true;
        }
    }
}
