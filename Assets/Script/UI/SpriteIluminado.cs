using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Hace que una Image del cuarto (en el Canvas del mundo, Interactuables) reciba la luz
    /// como el fondo: la luz del fósforo, la de la puerta... Las Image de UI nunca se
    /// oscurecen, así que en la oscuridad se veían igual de claras.
    ///
    /// Crea un SpriteRenderer hijo con material iluminado (Sprite-Lit-Default) que copia
    /// todo de la Image: sprite, color, transparencia (también la de los CanvasGroup de
    /// arriba), si está encendida, tamaño y rotación. La Image queda invisible pero sigue
    /// recibiendo los clics, así que botones y scripts funcionan igual que antes.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class SpriteIluminado : MonoBehaviour
    {
        [Tooltip("Material con luz 2D (Sprite-Lit-Default). Vacío = el del fondo del cuarto.")]
        [SerializeField] private Material material;
        [Tooltip("Orden de dibujo. El fondo es 0 y Luis 1: los muebles abiertos y lo que tienen adentro van por encima de Luis (4 y 5).")]
        [SerializeField] private int ordenEnCapa;
        [Tooltip("Se multiplica por el color de la Image.")]
        [SerializeField] private Color tinte = Color.white;
        [Tooltip("Cuánto más cerca de la cámara que el Canvas: con el mismo orden, lo más cercano se dibuja encima.")]
        [SerializeField] private float adelante = 1f;

        private static Material materialDelCuarto;
        private static Sprite spriteBlanco;

        private Image imagen;
        private RectTransform rect;
        private SpriteRenderer sr;
        private readonly List<CanvasGroup> grupos = new List<CanvasGroup>();

        public int OrdenEnCapa
        {
            get => ordenEnCapa;
            set { ordenEnCapa = value; if (sr != null) sr.sortingOrder = value; }
        }

        /// <summary>Para las Image que se crean por código (cajón del inicio, joyero...).</summary>
        public static SpriteIluminado Agregar(Image img, int orden, float adelante = 1f, Material mat = null)
        {
            var s = img.GetComponent<SpriteIluminado>();
            if (s == null) s = img.gameObject.AddComponent<SpriteIluminado>();
            s.adelante = adelante;
            if (mat != null) s.material = mat;
            s.OrdenEnCapa = orden;
            return s;
        }

        private void Awake()
        {
            imagen = GetComponent<Image>();
            rect = (RectTransform)transform;

            var go = new GameObject("SpriteIluminado");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = ordenEnCapa;
            Material m = material != null ? material : MaterialDelCuarto();
            if (m != null) sr.sharedMaterial = m;

            ActualizarGrupos();
        }

        private void OnEnable()
        {
            if (sr != null) sr.enabled = false; // lo decide Sincronizar en el mismo frame
        }

        private void OnDisable()
        {
            if (sr != null) sr.enabled = false;
            // Si se apaga solo este componente, la Image vuelve a verse como siempre.
            if (imagen != null) imagen.canvasRenderer.SetAlpha(1f);
        }

        private void OnTransformParentChanged() => ActualizarGrupos();

        private void ActualizarGrupos()
        {
            grupos.Clear();
            GetComponentsInParent(true, grupos);
        }

        private void LateUpdate() => Sincronizar();

        private void Sincronizar()
        {
            if (sr == null || imagen == null) return;

            // La Image no se ve nunca: solo recibe clics.
            imagen.canvasRenderer.SetAlpha(0f);

            float alfa = imagen.color.a;
            foreach (var g in grupos)
            {
                if (g == null || !g.enabled) continue;
                alfa *= g.alpha;
                if (g.ignoreParentGroups) break;
            }

            // overrideSprite: el que se ve de verdad (incluye el cambio de sprite de los botones).
            Sprite sprite = imagen.overrideSprite != null ? imagen.overrideSprite : SpriteBlanco();
            bool visible = imagen.enabled && alfa > 0.002f && sprite != null;
            sr.enabled = visible;
            if (!visible) return;

            sr.sprite = sprite;
            sr.sortingOrder = ordenEnCapa;
            Color c = imagen.color * tinte;
            c.a = alfa;
            sr.color = c;

            // Ocupa el mismo lugar que la Image (respetando "Preserve Aspect").
            Rect r = rect.rect;
            Vector2 tam = r.size;
            Vector2 tamSprite = sprite.bounds.size;
            if (tamSprite.x <= 0f || tamSprite.y <= 0f) return;
            if (imagen.preserveAspect)
            {
                float k = Mathf.Min(tam.x / tamSprite.x, tam.y / tamSprite.y);
                tam = tamSprite * k;
            }
            var escala = new Vector3(tam.x / tamSprite.x, tam.y / tamSprite.y, 1f);
            Transform t = sr.transform;
            t.localScale = escala;
            t.localRotation = Quaternion.identity;
            t.localPosition = (Vector3)(r.center - Vector2.Scale(sprite.bounds.center, escala));

            Vector3 p = t.position;
            p.z = transform.position.z - adelante;
            t.position = p;
        }

        private static Material MaterialDelCuarto()
        {
            if (materialDelCuarto != null) return materialDelCuarto;
            foreach (var r in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                Material m = r.sharedMaterial;
                if (m != null && m.name.Contains("Lit"))
                {
                    materialDelCuarto = m;
                    break;
                }
            }
            return materialDelCuarto;
        }

        private static Sprite SpriteBlanco()
        {
            if (spriteBlanco == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                spriteBlanco = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            return spriteBlanco;
        }
    }
}
