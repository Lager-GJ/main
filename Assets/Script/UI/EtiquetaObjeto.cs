using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Lo que dice la etiqueta de un objeto cuando el mouse pasa por encima, con un verbo:
    /// «Revisar el armario», «Recoger la foto» (diseño del equipo, 2026-11-17). La dibuja
    /// ResaltadorInteractuables, centrada justo debajo del objeto resaltado.
    /// Va en el mismo objeto que recibe el clic (o en uno de sus padres).
    /// </summary>
    public class EtiquetaObjeto : MonoBehaviour
    {
        [Tooltip("Verbo + objeto. Vacío = sin etiqueta.")]
        public string texto = "Revisar";
    }
}
