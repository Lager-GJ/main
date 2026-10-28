using System.Collections;
using Terror.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Un objeto del cuarto que Luis puede guardar (la foto del campo, la agenda, una
    /// llave). Va sobre una Image de UI: en el Canvas del mundo (Interactuables) o dentro
    /// de un primer plano (el cajón del armario, el secretero).
    ///
    /// Igual que el resto del cuarto, solo se ve y se toca con un fósforo encendido.
    /// Al hacer clic se guarda en el Inventario, se desvanece y se ve en primer plano
    /// (PrimerPlanoObjeto): se le da la vuelta y se inspecciona (2026-10-28).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ObjetoInventario : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("Id del catálogo de Inventario (ej. foto_campo).")]
        [SerializeField] private string idObjeto;
        [Tooltip("Sin PrimerPlanoObjeto en la escena: abrir la tarjeta del objeto apenas se guarda (para pistas que hay que leer). Con primer plano, siempre se muestra.")]
        [SerializeField] private bool mostrarTarjetaAlGuardar;
        [Tooltip("Desmarcado: se ve y se toca aunque no haya fósforo encendido.")]
        [SerializeField] private bool requiereFosforo = true;
        [SerializeField] private AudioClip sonido;
        [Tooltip("Late un poco para que se note (objetos chicos en el piso).")]
        [SerializeField] private bool latir = true;
        [SerializeField] private UnityEvent alGuardar;
        [Tooltip("Línea abajo del primer plano al guardarlo.")]
        [SerializeField] private string textoGuardado = "Lo guardé en la bolsa  (I para ver lo que llevo)";

        private Image imagen;
        private Color colorBase;
        private bool guardado;
        private bool encima;

        private void Awake()
        {
            imagen = GetComponent<Image>();
            colorBase = imagen.color;
        }

        /// <summary>Para los que se crean en runtime (la llave del joyero).</summary>
        public void Configurar(string id, bool tarjetaAlGuardar, AudioClip sonidoAlGuardar = null)
        {
            idObjeto = id;
            mostrarTarjetaAlGuardar = tarjetaAlGuardar;
            sonido = sonidoAlGuardar;
        }

        /// <summary>Se dispara al guardarlo (para quien lo crea en runtime).</summary>
        public event System.Action AlGuardarse;

        private void OnEnable()
        {
            FosforoManager.OnFosforoEncendido += Actualizar;
            FosforoManager.OnFosforoApagado += Actualizar;
            Actualizar();
        }

        private void OnDisable()
        {
            FosforoManager.OnFosforoEncendido -= Actualizar;
            FosforoManager.OnFosforoApagado -= Actualizar;
            encima = false;
        }

        private void Start() => Actualizar();

        private bool HayLuz => !requiereFosforo || FosforoManager.Instance == null || FosforoManager.Instance.PuedeInteractuar();

        private void Actualizar()
        {
            if (imagen == null || guardado) return;
            imagen.enabled = HayLuz;
            imagen.raycastTarget = HayLuz;
        }

        private void Update()
        {
            if (guardado || !imagen.enabled) return;
            float k = encima ? 1.25f : (latir ? 1f + 0.12f * (Mathf.Sin(Time.unscaledTime * 3f) * 0.5f + 0.5f) : 1f);
            imagen.color = new Color(Mathf.Min(1f, colorBase.r * k), Mathf.Min(1f, colorBase.g * k), Mathf.Min(1f, colorBase.b * k), colorBase.a);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (guardado) return;
            encima = true;
            transform.localScale = Vector3.one * 1.06f;
        }

        public void OnPointerExit(PointerEventData e)
        {
            encima = false;
            if (!guardado) transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData e) => Guardar();

        /// <summary>También se puede llamar desde un Button o un UnityEvent.</summary>
        public void Guardar()
        {
            if (guardado || !PuedeTocar()) return;

            var inv = Inventario.Instance;
            if (inv == null)
            {
                Debug.LogWarning("[ObjetoInventario] No hay Inventario en la escena.");
                return;
            }

            guardado = true;
            imagen.raycastTarget = false;
            inv.Agregar(idObjeto);
            if (sonido != null) AudioSource.PlayClipAtPoint(sonido, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            alGuardar?.Invoke();
            AlGuardarse?.Invoke();

            if (PrimerPlanoObjeto.Instance != null && inv.Definicion(idObjeto) != null)
                PrimerPlanoObjeto.Instance.Mostrar(inv.Definicion(idObjeto), textoGuardado);
            else if (mostrarTarjetaAlGuardar && TarjetaLectura.Instance != null)
                TarjetaLectura.Instance.Mostrar(inv.Definicion(idObjeto));

            StartCoroutine(Desvanecer());
        }

        private bool PuedeTocar()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return false;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return false;
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.CierreBloqueado) return false;
            if (PanelInspeccion.Instance != null && PanelInspeccion.Instance.HayCapaAbierta) return false;
            return HayLuz;
        }

        private IEnumerator Desvanecer()
        {
            Vector3 escala0 = transform.localScale;
            Color c0 = imagen.color;
            yield return UIRuntime.Animar(0.35f, p =>
            {
                transform.localScale = escala0 * Mathf.Lerp(1f, 1.3f, p);
                imagen.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - p));
            });
            gameObject.SetActive(false);
        }
    }
}
