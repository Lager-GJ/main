using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Una tarjeta del menu. Se pinta sola a partir de su LeyendaDefinicion: nombre,
    /// portada, y si esta bloqueada muestra el candado con el teaser y deja de
    /// responder al click.
    ///
    /// En el Editor: se arma UNA tarjeta completa y se duplica para las otras 4,
    /// cambiando solo el campo 'leyenda'. El Button tiene que llamar a
    /// OnClickTarjeta() desde su OnClick.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuTarjetaLeyenda : MonoBehaviour
    {
        [Header("Datos")]
        [SerializeField] private MenuPrincipal menu;
        [SerializeField] private LeyendaDefinicion leyenda;

        [Header("Piezas visuales (opcionales: si falta alguna, se ignora)")]
        [SerializeField] private TMP_Text textoNombre;
        [SerializeField] private Image imagenPortada;
        [SerializeField] private GameObject candado;
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

            if (candado != null)
                candado.SetActive(!desbloqueada);

            if (textoTeaser != null)
            {
                textoTeaser.gameObject.SetActive(!desbloqueada);
                textoTeaser.text = leyenda.teaser;
            }

            // Doble cierre: el Button deshabilitado ya no dispara OnClick, y aunque
            // lo hiciera, MenuPrincipal.EntrarALeyenda vuelve a chequear el bloqueo.
            GetComponent<Button>().interactable = desbloqueada;
        }

        public void OnClickTarjeta()
        {
            if (menu != null)
                menu.EntrarALeyenda(leyenda);
        }
    }
}
