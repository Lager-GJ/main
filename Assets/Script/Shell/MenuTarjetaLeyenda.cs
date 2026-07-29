using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Una tarjeta de cuarto. Se pinta sola a partir de su LeyendaDefinicion, en
    /// uno de 3 estados: bloqueada (candado, no clickeable), activa
    /// ("ENFRÉNTALO", clickeable), completada ("ESCAPASTE", sigue clickeable —
    /// se puede volver a jugar).
    ///
    /// En el Editor: armar UNA tarjeta completa y duplicarla para las otras 3,
    /// cambiando solo el campo 'leyenda'. El Button debe llamar a
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
        [SerializeField] private TMP_Text textoEstado;
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

            GetComponent<Button>().interactable = desbloqueada;
        }

        public void OnClickTarjeta()
        {
            if (menu != null)
                menu.EntrarACuarto(leyenda);
        }
    }
}
