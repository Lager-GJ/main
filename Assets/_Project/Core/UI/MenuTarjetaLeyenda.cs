using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Una tarjeta del menu ("Los secretos de la casa"). Se pinta sola a partir de
    /// su LeyendaDefinicion, en uno de 3 estados:
    ///   - Bloqueada: candado visible, sin texto de estado, no clickeable.
    ///   - Activa (desbloqueada, sin completar): "ENFRÉNTALO", clickeable.
    ///   - Completada (desbloqueada y ya jugada): "ESCAPASTE", sigue clickeable
    ///     -- se puede volver a jugar (decision de David, 2026-07-26).
    ///
    /// En el Editor: se arma UNA tarjeta completa y se duplica para las demas,
    /// cambiando solo el campo 'leyenda'. El Button tiene que llamar a
    /// OnClickTarjeta() desde su OnClick.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuTarjetaLeyenda : MonoBehaviour
    {
        private const string TextoActiva = "ENFRÉNTALO";
        private const string TextoCompletada = "ESCAPASTE";

        [Header("Datos")]
        [SerializeField] private MenuPrincipal menu;
        [SerializeField] private LeyendaDefinicion leyenda;

        [Header("Piezas visuales (opcionales: si falta alguna, se ignora)")]
        [SerializeField] private TMP_Text textoNombre;
        [SerializeField] private Image imagenPortada;
        [SerializeField] private GameObject candado;

        [Tooltip("Muestra ENFRÉNTALO o ESCAPASTE. Se oculta si la tarjeta esta bloqueada.")]
        [SerializeField] private TMP_Text textoEstado;

        [Tooltip("Frase corta debajo del candado. Opcional -- el mockup actual no la usa, pero queda disponible.")]
        [SerializeField] private TMP_Text textoTeaser;

        private void Start()
        {
            if (leyenda == null)
            {
                Debug.LogWarning($"[MenuTarjetaLeyenda] '{name}' no tiene LeyendaDefinicion asignada.");
                GetComponent<Button>().interactable = false;
                return;
            }

            if (textoNombre != null)
                textoNombre.text = leyenda.nombre;

            // Sin portada asignada se deja el Image como este en la escena (color de
            // fondo). Todavia no hay arte de portadas.
            if (imagenPortada != null && leyenda.portada != null)
                imagenPortada.sprite = leyenda.portada;

            bool desbloqueada = menu != null && menu.EstaDesbloqueada(leyenda);
            bool completada = menu != null && menu.EstaCompletada(leyenda);

            if (candado != null)
                candado.SetActive(!desbloqueada);

            if (textoTeaser != null)
            {
                textoTeaser.gameObject.SetActive(!desbloqueada);
                textoTeaser.text = leyenda.teaser;
            }

            if (textoEstado != null)
            {
                textoEstado.gameObject.SetActive(desbloqueada);
                textoEstado.text = completada ? TextoCompletada : TextoActiva;
            }

            // Completada tambien cuenta como desbloqueada (son cosas independientes):
            // una vez que se juega, se puede volver a entrar. Doble cierre: el Button
            // deshabilitado ya no dispara OnClick, y aunque lo hiciera,
            // MenuPrincipal.EntrarALeyenda vuelve a chequear el bloqueo.
            GetComponent<Button>().interactable = desbloqueada;
        }

        public void OnClickTarjeta()
        {
            if (menu != null)
                menu.EntrarALeyenda(leyenda);
        }
    }
}
