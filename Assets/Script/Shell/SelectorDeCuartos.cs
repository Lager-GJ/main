using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Pantalla de selección de niveles ("Los secretos de la casa"). Arma en runtime
    /// una tarjeta por cuarto del catálogo, con su estado real (bloqueada / activa /
    /// completada) sacado de MenuPrincipal. Al hacer click en una desbloqueada:
    /// se ocultan las demás, el marco elegido crece hasta llenar la pantalla
    /// mostrando el fondo del cuarto, fundido a negro y se carga su escena.
    ///
    /// Va en Nivel.unity junto a MenuPrincipal. Las tarjetas se construyen por
    /// código para no depender de piezas armadas a mano en el Editor: para ajustar
    /// tamaños y tiempos, usar los campos del Inspector.
    /// </summary>
    [RequireComponent(typeof(MenuPrincipal))]
    public class SelectorDeCuartos : MonoBehaviour
    {
        private const string TextoActiva = "ENFRÉNTALO";
        private const string TextoCompletada = "ESCAPASTE";

        [Header("Datos")]
        [SerializeField] private CatalogoLeyendas catalogo;

        [Header("Escena")]
        [Tooltip("RectTransform del Canvas raíz (Scale With Screen Size, 1920x1080).")]
        [SerializeField] private RectTransform canvas;
        [SerializeField] private TMP_FontAsset fuente;
        [SerializeField] private Sprite spriteCandado;

        [Header("Distribución")]
        [SerializeField] private string titulo = "LOS SECRETOS DE LA CASA";
        [SerializeField] private Vector2 tamanoTarjeta = new Vector2(315f, 655f);
        [SerializeField] private float separacion = 60f;
        [SerializeField] private float alturaTarjetas = -40f;
        [SerializeField] private float grosorMarco = 6f;
        [SerializeField] private Color colorMarco = new Color(0.92f, 0.92f, 0.9f, 1f);
        [SerializeField] private float escalaAlPasarMouse = 1.04f;

        [Header("Transición al entrar")]
        [SerializeField] private float duracionOcultar = 0.35f;
        [SerializeField] private float duracionAgrandar = 0.9f;
        [SerializeField] private float esperaConFondo = 0.6f;
        [SerializeField] private float duracionFundidoNegro = 0.5f;

        private class Tarjeta
        {
            public LeyendaDefinicion leyenda;
            public RectTransform rect;
            public RectTransform mascara;
            public Image portada;
            public Image fondo;
            public Image velo;
            public CanvasGroup textos;
            public Button boton;
            public float escalaObjetivo = 1f;
        }

        private readonly List<Tarjeta> tarjetas = new List<Tarjeta>();
        private MenuPrincipal menu;
        private RectTransform raiz;
        private CanvasGroup grupoTitulo;
        private bool entrando;

        private void Awake()
        {
            menu = GetComponent<MenuPrincipal>();
        }

        private void Start()
        {
            // Se puede llegar acá desde el menú de pausa de un nivel (timeScale = 0).
            Time.timeScale = 1f;
            Construir();
        }

        private void Update()
        {
            if (entrando)
                return;

            foreach (Tarjeta t in tarjetas)
            {
                float actual = t.rect.localScale.x;
                float nueva = Mathf.MoveTowards(actual, t.escalaObjetivo, Time.unscaledDeltaTime * 0.6f);
                t.rect.localScale = new Vector3(nueva, nueva, 1f);
            }
        }

        // ---------- Construcción ----------

        private void Construir()
        {
            if (catalogo == null || canvas == null)
            {
                Debug.LogWarning("[SelectorDeCuartos] Falta asignar el catálogo o el canvas.");
                return;
            }

            raiz = CrearRect("Cuartos", canvas);
            Estirar(raiz, 0f);
            raiz.SetAsFirstSibling(); // el botón Volver de la escena queda por encima
            CrearImagen("FondoNegro", raiz, null, Color.black).raycastTarget = false;

            TMP_Text textoTitulo = CrearTexto("Titulo", raiz, titulo, 52f, TextAlignmentOptions.Left);
            RectTransform rt = textoTitulo.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(1200f, 90f);
            rt.anchoredPosition = new Vector2(135f, -140f);
            grupoTitulo = textoTitulo.gameObject.AddComponent<CanvasGroup>();

            int n = catalogo.leyendas.Length;
            for (int i = 0; i < n; i++)
            {
                LeyendaDefinicion leyenda = catalogo.leyendas[i];
                if (leyenda == null)
                    continue;

                float x = (i - (n - 1) / 2f) * (tamanoTarjeta.x + separacion);
                tarjetas.Add(CrearTarjeta(leyenda, new Vector2(x, alturaTarjetas)));
            }
        }

        private Tarjeta CrearTarjeta(LeyendaDefinicion leyenda, Vector2 posicion)
        {
            bool desbloqueada = menu.EstaDesbloqueada(leyenda);
            bool completada = menu.EstaCompletada(leyenda);
            var t = new Tarjeta { leyenda = leyenda };

            t.rect = CrearRect("Tarjeta_" + leyenda.id, raiz);
            t.rect.sizeDelta = tamanoTarjeta;
            t.rect.anchoredPosition = posicion;
            Image marco = t.rect.gameObject.AddComponent<Image>();
            marco.color = colorMarco;

            t.mascara = CrearRect("Mascara", t.rect);
            Estirar(t.mascara, grosorMarco);
            t.mascara.gameObject.AddComponent<RectMask2D>();
            CrearImagen("Negro", t.mascara, null, Color.black).raycastTarget = false;

            t.portada = CrearImagen("Portada", t.mascara, leyenda.portada, Color.white);
            if (leyenda.fondo != null)
            {
                t.fondo = CrearImagen("Fondo", t.mascara, leyenda.fondo, new Color(1f, 1f, 1f, 0f));
                t.fondo.raycastTarget = false;
            }
            t.portada.raycastTarget = false;
            t.velo = CrearImagen("Velo", t.mascara, null, new Color(0f, 0f, 0f, desbloqueada ? 0.2f : 0.55f));
            t.velo.raycastTarget = false;
            AjustarImagenes(t);

            RectTransform textos = CrearRect("Textos", t.rect);
            Estirar(textos, 0f);
            t.textos = textos.gameObject.AddComponent<CanvasGroup>();
            t.textos.blocksRaycasts = false;

            TMP_Text nombre = CrearTexto("Nombre", textos, leyenda.nombre, 38f, TextAlignmentOptions.Top);
            RectTransform rn = nombre.rectTransform;
            rn.anchorMin = new Vector2(0f, 1f);
            rn.anchorMax = new Vector2(1f, 1f);
            rn.pivot = new Vector2(0.5f, 1f);
            rn.sizeDelta = new Vector2(-30f, 150f);
            rn.anchoredPosition = new Vector2(0f, -45f);

            if (!desbloqueada)
            {
                if (spriteCandado != null)
                {
                    Image candado = CrearImagen("Candado", textos, spriteCandado, Color.white);
                    candado.preserveAspect = true;
                    candado.raycastTarget = false;
                    candado.rectTransform.anchorMin = candado.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    candado.rectTransform.sizeDelta = new Vector2(130f, 160f);
                    candado.rectTransform.anchoredPosition = new Vector2(0f, -10f);
                }
            }
            else
            {
                RectTransform estado = CrearRect("Estado", textos);
                estado.anchorMin = estado.anchorMax = new Vector2(0.5f, 0f);
                estado.sizeDelta = new Vector2(240f, 64f);
                estado.anchoredPosition = new Vector2(0f, 75f);
                if (!completada)
                {
                    // "Botón" ENFRÉNTALO: fondo gris con borde claro, como en el mockup.
                    Image borde = estado.gameObject.AddComponent<Image>();
                    borde.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
                    borde.raycastTarget = false;
                    Image relleno = CrearImagen("Relleno", estado, null, new Color(0.38f, 0.38f, 0.38f, 0.95f));
                    Estirar(relleno.rectTransform, 3f);
                    relleno.raycastTarget = false;
                }
                TMP_Text textoEstado = CrearTexto("Texto", estado, completada ? TextoCompletada : TextoActiva, 28f, TextAlignmentOptions.Center);
                Estirar(textoEstado.rectTransform, 0f);
            }

            t.boton = t.rect.gameObject.AddComponent<Button>();
            t.boton.transition = Selectable.Transition.None;
            t.boton.targetGraphic = marco;
            t.boton.interactable = desbloqueada;
            t.boton.onClick.AddListener(() => StartCoroutine(Entrar(t)));

            if (desbloqueada)
            {
                EventTrigger trigger = t.rect.gameObject.AddComponent<EventTrigger>();
                AgregarEvento(trigger, EventTriggerType.PointerEnter, () => t.escalaObjetivo = escalaAlPasarMouse);
                AgregarEvento(trigger, EventTriggerType.PointerExit, () => t.escalaObjetivo = 1f);
            }

            return t;
        }

        // ---------- Transición ----------

        private IEnumerator Entrar(Tarjeta elegida)
        {
            if (entrando || string.IsNullOrEmpty(elegida.leyenda.nombreEscena))
            {
                if (!entrando)
                    Debug.LogWarning($"[SelectorDeCuartos] '{elegida.leyenda.nombre}' no tiene escena asignada.");
                yield break;
            }
            entrando = true;

            foreach (Tarjeta t in tarjetas)
                t.boton.interactable = false;

            // Lo demás del canvas (botón Volver, flechas) se va de una.
            foreach (Transform hijo in canvas)
                if (hijo != raiz)
                    hijo.gameObject.SetActive(false);

            elegida.rect.SetAsLastSibling();

            // 1) Se ocultan las demás tarjetas y el título.
            var otras = new List<CanvasGroup> { grupoTitulo };
            foreach (Tarjeta t in tarjetas)
                if (t != elegida)
                    otras.Add(t.rect.gameObject.AddComponent<CanvasGroup>());

            Vector3 escalaInicial = elegida.rect.localScale;
            yield return Animar(duracionOcultar, p =>
            {
                foreach (CanvasGroup g in otras) g.alpha = 1f - p;
                float e = Mathf.Lerp(escalaInicial.x, 1f, p);
                elegida.rect.localScale = new Vector3(e, e, 1f);
            });

            // 2) El marco crece hasta llenar la pantalla y aparece el fondo del cuarto.
            AsyncOperation carga = SceneManager.LoadSceneAsync(elegida.leyenda.nombreEscena);
            if (carga != null)
                carga.allowSceneActivation = false;

            Vector2 posInicial = elegida.rect.anchoredPosition;
            Vector2 tamInicial = elegida.rect.sizeDelta;
            Vector2 tamFinal = raiz.rect.size;
            float veloInicial = elegida.velo.color.a;
            yield return Animar(duracionAgrandar, p =>
            {
                float e = Suavizar(p);
                elegida.rect.anchoredPosition = Vector2.Lerp(posInicial, Vector2.zero, e);
                elegida.rect.sizeDelta = Vector2.Lerp(tamInicial, tamFinal, e);
                Estirar(elegida.mascara, Mathf.Lerp(grosorMarco, 0f, Mathf.InverseLerp(0.6f, 1f, e)));
                elegida.textos.alpha = 1f - Mathf.Clamp01(p * 2.5f);
                SetAlpha(elegida.velo, Mathf.Lerp(veloInicial, 0f, e));
                if (elegida.fondo != null)
                    SetAlpha(elegida.fondo, Mathf.Clamp01((e - 0.25f) / 0.6f));
                AjustarImagenes(elegida);
            });

            yield return new WaitForSecondsRealtime(esperaConFondo);

            // 3) Fundido a negro y arranca el nivel.
            Image negro = CrearImagen("FundidoNegro", raiz, null, new Color(0f, 0f, 0f, 0f));
            yield return Animar(duracionFundidoNegro, p => SetAlpha(negro, p));

            if (carga != null)
                carga.allowSceneActivation = true;
            else
                SceneManager.LoadScene(elegida.leyenda.nombreEscena);
        }

        private static IEnumerator Animar(float duracion, System.Action<float> paso)
        {
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                paso(Mathf.Clamp01(t / duracion));
                yield return null;
            }
            paso(1f);
        }

        private static float Suavizar(float p) => p < 0.5f ? 4f * p * p * p : 1f - Mathf.Pow(-2f * p + 2f, 3f) / 2f;

        // ---------- Ayudantes de UI ----------

        // Portada y fondo "cubren" la máscara sin deformarse (recortando lo que sobra).
        private void AjustarImagenes(Tarjeta t)
        {
            Vector2 area = t.mascara.rect.size;
            Cubrir(t.portada, area);
            if (t.fondo != null)
                Cubrir(t.fondo, area);
        }

        private static void Cubrir(Image img, Vector2 area)
        {
            RectTransform r = img.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            if (img.sprite == null || area.x <= 0f || area.y <= 0f)
            {
                r.sizeDelta = area;
                return;
            }
            float aspecto = img.sprite.rect.width / img.sprite.rect.height;
            r.sizeDelta = area.x / area.y > aspecto
                ? new Vector2(area.x, area.x / aspecto)
                : new Vector2(area.y * aspecto, area.y);
        }

        private static RectTransform CrearRect(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = padre.gameObject.layer;
            var r = (RectTransform)go.transform;
            r.SetParent(padre, false);
            return r;
        }

        private static void Estirar(RectTransform r, float margen)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(margen, margen);
            r.offsetMax = new Vector2(-margen, -margen);
        }

        private static Image CrearImagen(string nombre, Transform padre, Sprite sprite, Color color)
        {
            RectTransform r = CrearRect(nombre, padre);
            Estirar(r, 0f);
            Image img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private TMP_Text CrearTexto(string nombre, Transform padre, string texto, float tamano, TextAlignmentOptions alineacion)
        {
            RectTransform r = CrearRect(nombre, padre);
            TextMeshProUGUI tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (fuente != null)
                tmp.font = fuente;
            tmp.text = texto;
            tmp.fontSize = tamano;
            tmp.alignment = alineacion;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetAlpha(Graphic g, float a)
        {
            Color c = g.color;
            c.a = a;
            g.color = c;
        }

        private static void AgregarEvento(EventTrigger trigger, EventTriggerType tipo, System.Action accion)
        {
            var entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(_ => accion());
            trigger.triggers.Add(entrada);
        }

        // ---------- Debug (click derecho sobre el componente en el Inspector, en Play) ----------

        [ContextMenu("Debug: desbloquear todos los cuartos")]
        private void DebugDesbloquearTodo()
        {
            if (catalogo == null) return;
            foreach (LeyendaDefinicion l in catalogo.leyendas)
                if (l != null) SaveSystem.Desbloquear(l.id);
            Reconstruir();
        }

        [ContextMenu("Debug: borrar progreso")]
        private void DebugBorrarProgreso()
        {
            SaveSystem.Borrar();
            Reconstruir();
        }

        private void Reconstruir()
        {
            if (!Application.isPlaying || entrando) return;
            menu.RecargarPerfil();
            tarjetas.Clear();
            if (raiz != null) Destroy(raiz.gameObject);
            Construir();
        }
    }
}
