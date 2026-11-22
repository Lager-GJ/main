using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Botón de papel rasgado de Lumbre (arte del equipo, 2026-11-21): tres dibujos según el estado.
    ///   Resources/UI/BotonBase     = reposo
    ///   Resources/UI/BotonHover    = mouse encima (o elegido con el teclado)
    ///   Resources/UI/BotonPressed  = mientras se mantiene presionado
    ///
    /// Va en el mismo objeto que el Button y su Image. Al empezar esconde la Image de la raíz y dibuja el
    /// papel en un hijo "Papel" que queda detrás del texto. Los tres dibujos no miden igual (el de hover tiene
    /// sombra de más, el presionado es más bajo), así que el hijo cambia de tamaño según la proporción de cada
    /// uno: el papel se mantiene del mismo tamaño y no se deforma.
    ///
    /// Los colores/sprites de transición del Button no se usan (se pone Transition = None).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class BotonPapel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Tooltip("El papel es claro: el texto de los hijos pasa a tinta oscura.")]
        [SerializeField] private bool tintaOscuraEnElTexto = true;

        private static Sprite spriteBase, spriteHover, spritePresionado;
        private static bool cargado;

        private Button boton;
        private Image papel;
        private RectTransform rt;
        private bool encima, apretado, elegido, marcado;
        private bool ultimoInteractable = true;
        private Vector2 ultimoTamano;

        private static void Cargar()
        {
            if (cargado) return;
            cargado = true;
            spriteBase = Resources.Load<Sprite>("UI/BotonBase");
            spriteHover = Resources.Load<Sprite>("UI/BotonHover");
            spritePresionado = Resources.Load<Sprite>("UI/BotonPressed");
        }

        /// <summary>Hover fijo (para el botón "activo" de un grupo, como la pestaña elegida de Opciones).</summary>
        public void Marcar(bool si)
        {
            marcado = si;
            Aplicar();
        }

        private void Awake()
        {
            Cargar();
            rt = (RectTransform)transform;
            boton = GetComponent<Button>();
            boton.transition = Selectable.Transition.None;

            var raiz = GetComponent<Image>();
            if (raiz != null)
            {
                boton.targetGraphic = raiz; // sigue recibiendo los clics, pero ya no se ve
                raiz.sprite = null;
                raiz.color = new Color(1f, 1f, 1f, 0f);
                raiz.raycastTarget = true;
                var cr = raiz.GetComponent<CanvasRenderer>();
                if (cr != null) cr.cullTransparentMesh = false; // transparente pero tocable
            }

            var go = new GameObject("Papel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            papel = go.GetComponent<Image>();
            papel.raycastTarget = false;
            papel.type = Image.Type.Simple;
            papel.preserveAspect = false;
            Aplicar();
        }

        // En Start y no en Awake: los botones armados por código le agregan el texto después de crearlo.
        private void Start()
        {
            if (!tintaOscuraEnElTexto) return;
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
                t.color = ArteUI.Tinta;
        }

        private void OnEnable()
        {
            encima = apretado = elegido = false;
            if (papel != null) Aplicar();
        }

        private void LateUpdate()
        {
            if (papel == null) return;
            bool activo = boton.IsInteractable();
            Vector2 tam = rt.rect.size;
            if (activo != ultimoInteractable || tam != ultimoTamano) Aplicar();
        }

        public void OnPointerEnter(PointerEventData e) { encima = true; Aplicar(); }
        public void OnPointerExit(PointerEventData e) { encima = false; Aplicar(); }
        public void OnPointerDown(PointerEventData e) { apretado = true; Aplicar(); }
        public void OnPointerUp(PointerEventData e) { apretado = false; Aplicar(); }
        public void OnSelect(BaseEventData e) { elegido = true; Aplicar(); }
        public void OnDeselect(BaseEventData e) { elegido = false; Aplicar(); }

        private void Aplicar()
        {
            if (papel == null || spriteBase == null) return;
            bool activo = boton.IsInteractable();
            ultimoInteractable = activo;
            ultimoTamano = rt.rect.size;

            Sprite s = spriteBase;
            if (activo)
            {
                if (apretado && encima && spritePresionado != null) s = spritePresionado;
                else if ((encima || elegido || marcado) && spriteHover != null) s = spriteHover;
            }
            papel.sprite = s;
            papel.color = activo ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.55f);

            // Mismo tamaño de papel en los tres estados: cada dibujo se agranda/achica según su proporción
            // respecto al de reposo (el hover trae sombra extra, el presionado es más bajo).
            Vector2 baseTam = spriteBase.rect.size;
            Vector2 f = new Vector2(s.rect.width / baseTam.x, s.rect.height / baseTam.y);
            Vector2 extra = new Vector2(ultimoTamano.x * (f.x - 1f), ultimoTamano.y * (f.y - 1f)) * 0.5f;
            var r = papel.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = -extra;
            r.offsetMax = extra;
        }
    }
}
