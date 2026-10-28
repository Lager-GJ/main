using TMPro;
using Terror.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Algo cerrado que se abre con un objeto del inventario: el marco (con la foto),
    /// la lata (con la llave pequeña), el cajón del armario, la puerta...
    ///
    /// Se conecta al clic del objeto (Button → Intentar, o un HotspotArmario con este
    /// componente asignado). Al tocarlo:
    /// - Si ya está abierto: alClicAbierta (si tiene algo) o textoYaAbierta.
    /// - Si Luis lleva el objeto: lo usa solo ("Usas la llave pequeña.") y se abre.
    /// - Si no: textoBloqueada.
    /// Al abrirse puede cambiar su dibujo, entregar objetos y disparar alAbrir.
    /// Abrir() también se puede llamar directo (el candado numérico, al acertar).
    /// </summary>
    public class Cerradura : MonoBehaviour
    {
        [Header("Qué hace falta")]
        [Tooltip("Id del objeto que la abre. Vacío = no hace falta nada (se abre al tocarla, o la abre otro script con Abrir()).")]
        [SerializeField] private string idRequerido;
        [Tooltip("El objeto se gasta al usarlo (sale del inventario).")]
        [SerializeField] private bool consumir = true;
        [Tooltip("Solo se puede tocar con un fósforo encendido (como todo el cuarto).")]
        [SerializeField] private bool requiereFosforo = true;
        [Tooltip("Desmarcado: sin el objeto, tocarla no la abre aunque idRequerido esté vacío (la abre otro script).")]
        [SerializeField] private bool abrirAlTocarSiNoPideNada = true;

        [Header("Al abrirse")]
        [Tooltip("Objetos que se guardan en el inventario al abrirse.")]
        [SerializeField] private string[] entregar;
        [Tooltip("Image que cambia de dibujo al abrirse (opcional).")]
        [SerializeField] private Image imagenCambiar;
        [SerializeField] private Sprite spriteAbierta;
        [SerializeField] private UnityEvent alAbrir;
        [Tooltip("Al tocarla ya abierta. Vacío = se muestra textoYaAbierta.")]
        [SerializeField] private UnityEvent alClicAbierta;

        [Header("Textos ({0} = nombre del objeto que se usa)")]
        [TextArea(1, 2)] [SerializeField] private string textoBloqueada = "Está cerrado.";
        [TextArea(1, 2)] [SerializeField] private string textoUsar = "Usas: {0}.";
        [TextArea(1, 2)] [SerializeField] private string textoYaAbierta = "";

        [Header("Sonidos")]
        [SerializeField] private AudioClip sonidoBloqueada;
        [SerializeField] private AudioClip sonidoAbrir;

        [Header("Dónde se muestran los textos")]
        [Tooltip("Dentro de un primer plano (el armario, el secretero): su VistaArmario. Vacío = abajo en la pantalla, como las narraciones.")]
        [SerializeField] private VistaArmario vista;
        [SerializeField] private TMP_FontAsset fuente;

        private bool abierta;

        public bool Abierta => abierta;
        public string IdRequerido => idRequerido;

        /// <summary>Conectar al clic del objeto.</summary>
        public void Intentar()
        {
            if (!PuedeTocar()) return;

            if (abierta)
            {
                if (alClicAbierta != null && alClicAbierta.GetPersistentEventCount() > 0)
                    alClicAbierta.Invoke();
                else
                    Decir(textoYaAbierta);
                return;
            }

            var inv = Inventario.Instance;
            bool pideAlgo = !string.IsNullOrEmpty(idRequerido);

            if (pideAlgo && inv != null && inv.Tiene(idRequerido))
            {
                Decir(string.Format(textoUsar, inv.Nombre(idRequerido)));
                Abrir();
            }
            else if (!pideAlgo && abrirAlTocarSiNoPideNada)
            {
                Abrir();
            }
            else
            {
                Decir(textoBloqueada);
                Sonar(sonidoBloqueada);
                if (vista != null) vista.Sacudir(3f);
            }
        }

        /// <summary>La abre sin pedir nada (la usa el candado numérico, por ejemplo).</summary>
        public void Abrir()
        {
            if (abierta) return;
            abierta = true;

            var inv = Inventario.Instance;
            if (consumir && !string.IsNullOrEmpty(idRequerido) && inv != null)
                inv.Quitar(idRequerido);

            Sonar(sonidoAbrir);
            if (imagenCambiar != null && spriteAbierta != null)
                imagenCambiar.sprite = spriteAbierta;

            if (entregar != null && inv != null)
                foreach (var id in entregar)
                    inv.Agregar(id);

            alAbrir?.Invoke();
        }

        private bool PuedeTocar()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return false;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return false;
            var panel = PanelInspeccion.Instance;
            if (panel != null && (panel.CierreBloqueado || panel.HayCapaAbierta)) return false;
            if (requiereFosforo && FosforoManager.Instance != null && !FosforoManager.Instance.PuedeInteractuar()) return false;
            return true;
        }

        private void Decir(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            if (vista != null)
            {
                vista.Decir(texto);
                return;
            }
            var cartel = CartelNarrativa.Instancia;
            cartel.Mostrar(this, texto, fuente, 3f);
            cartel.SoltarHover(this);
        }

        private void Sonar(AudioClip clip)
        {
            if (clip == null) return;
            if (vista != null) vista.Sonar(clip);
            else AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }
}
