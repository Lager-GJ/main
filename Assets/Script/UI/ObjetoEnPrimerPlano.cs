using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Un objeto del cuarto que se mira al tocarlo. Se conecta al clic del objeto
    /// (Button → Mostrar).
    ///  - Si 'alInspeccionar' tiene algo (el candado del joyero, la cerradura de la lata),
    ///    el clic lo hace directo: el joyero acerca la cámara y se abre en su lugar.
    ///  - Si no, la cámara se acerca al objeto en el cuarto (AcercamientoObjeto): clic
    ///    para darle la vuelta e "Inspeccionar" para leer los textos de a uno. Si no se
    ///    puede, la ventana de primer plano (PrimerPlanoObjeto).
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
        [Tooltip("Lo que hace el clic (vacío = acercarse y leer los textos).")]
        [SerializeField] private UnityEvent alInspeccionar;

        /// <summary>Conectar al clic del objeto.</summary>
        public void Mostrar()
        {
            if (!PuedeTocar()) return;

            // Con acción (el candado del joyero, la cerradura de la lata) el clic la hace
            // directo: es la transición del joyero que pidió David (2026-10-30), sin pasar
            // antes por la ventana.
            if (TieneAccion)
            {
                alInspeccionar.Invoke();
                return;
            }

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

            var datos = new PrimerPlanoObjeto.Datos
            {
                nombre = nombre,
                frente = f,
                reverso = reverso,
                textos = textos,
                textoBoton = textoBoton,
            };
            // La cámara se acerca al objeto en su lugar; si no se puede, la ventana.
            var acercamiento = AcercamientoObjeto.Instance;
            if (acercamiento != null && acercamiento.Mostrar(datos, transform as RectTransform)) return;
            pp.Mostrar(datos);
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
