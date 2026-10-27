using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Convierte una vista de primer plano en un "cambio de perspectiva" dentro del cuarto
    /// (pedido de David, 2026-10-26): la cámara se acerca al mueble de verdad y el mueble
    /// se abre ahí mismo, con lo que tiene adentro para tocar. No aparece ninguna ventana.
    ///
    /// Va en el panel de la vista (el que abre PanelInspeccion: PanelArmarioPrimerPlano...).
    /// Ese panel queda en la pantalla solo con los textos; lo que se ve se arma en
    /// 'enCuarto', un objeto dentro del Canvas del mundo (Interactuables) con el mueble
    /// abierto y sus zonas. Este componente lo enciende y lo apaga junto con el panel,
    /// copia su transparencia (las transiciones) y le dice a PanelInspeccion adónde
    /// acercar la cámara y cuánto.
    /// </summary>
    public class VistaEnCuarto : MonoBehaviour
    {
        [Tooltip("Lo que aparece en el cuarto mientras la vista está abierta (hijo de Interactuables): el mueble abierto, sus zonas, el fondo que cierra con un clic afuera.")]
        [SerializeField] private GameObject enCuarto;
        [Tooltip("Punto al que se acerca la cámara. Vacío = el centro de 'En Cuarto'.")]
        [SerializeField] private RectTransform foco;
        [Tooltip("Corrimiento del punto de la cámara, en unidades del mundo.")]
        [SerializeField] private Vector2 desplazamientoFoco;
        [Tooltip("Tamaño de la cámara respecto del normal: 0.5 = se ve la mitad de alto (el doble de cerca).")]
        [SerializeField, Range(0.1f, 1f)] private float zoom = 0.5f;
        [Tooltip("Imágenes del cuarto que se esconden mientras está abierta: el mueble cerrado.")]
        [SerializeField] private Graphic[] esconder;
        [Tooltip("Luz que acompaña a la vista (la del fósforo sobre el mueble). Solo se enciende con un fósforo encendido.")]
        [SerializeField] private GameObject luz;

        private CanvasGroup grupoCuarto;

        public float Zoom => zoom;

        public GameObject Luz
        {
            get => luz;
            set { luz = value; if (luz != null && !isActiveAndEnabled) luz.SetActive(false); }
        }

        /// <summary>Para las vistas que se arman por código (cajón del inicio, joyero).</summary>
        public void Configurar(GameObject enCuarto, RectTransform foco, float zoom, params Graphic[] esconder)
        {
            this.enCuarto = enCuarto;
            this.foco = foco;
            this.zoom = zoom;
            this.esconder = esconder;
            grupoCuarto = null;
            if (enCuarto != null && !isActiveAndEnabled) enCuarto.SetActive(false);
        }

        public Vector3 PuntoFoco
        {
            get
            {
                RectTransform rt = foco != null ? foco : enCuarto != null ? enCuarto.transform as RectTransform : null;
                Vector3 p;
                if (rt != null)
                {
                    var esquinas = new Vector3[4];
                    rt.GetWorldCorners(esquinas);
                    p = (esquinas[0] + esquinas[2]) * 0.5f;
                }
                else p = enCuarto != null ? enCuarto.transform.position : transform.position;
                return p + (Vector3)desplazamientoFoco;
            }
        }

        private void OnEnable()
        {
            if (enCuarto != null) enCuarto.SetActive(true);
            Esconder(true);
            Sincronizar();
        }

        private void OnDisable()
        {
            if (enCuarto != null) enCuarto.SetActive(false);
            if (luz != null) luz.SetActive(false);
            Esconder(false);
        }

        private void LateUpdate() => Sincronizar();

        private void Esconder(bool si)
        {
            if (esconder == null) return;
            foreach (var g in esconder)
                if (g != null) g.enabled = !si;
        }

        private void Sincronizar()
        {
            if (enCuarto == null) return;

            if (grupoCuarto == null)
            {
                grupoCuarto = enCuarto.GetComponent<CanvasGroup>();
                if (grupoCuarto == null) grupoCuarto = enCuarto.AddComponent<CanvasGroup>();
            }

            // Misma transparencia que el panel (TransicionPanel la anima en el panel).
            float alfa = 1f;
            for (Transform t = transform; t != null; t = t.parent)
            {
                var g = t.GetComponent<CanvasGroup>();
                if (g == null || !g.enabled) continue;
                alfa *= g.alpha;
                if (g.ignoreParentGroups) break;
            }
            grupoCuarto.alpha = alfa;
            grupoCuarto.blocksRaycasts = alfa > 0.5f;

            if (luz != null)
            {
                bool encendida = FosforoManager.Instance != null && FosforoManager.Instance.Encendido;
                if (luz.activeSelf != encendida) luz.SetActive(encendida);
            }
        }
    }
}
