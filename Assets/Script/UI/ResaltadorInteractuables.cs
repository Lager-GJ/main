using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Ilumina el borde de lo que se puede tocar cuando el mouse pasa por encima (pedido
    /// de David, 2026-10-28): fotos, cuadros, objetos de los cajones, muebles, cerraduras...
    /// Así se distingue lo interactuable del dibujo del fondo.
    ///
    /// Uno solo por escena. Mira lo que hay debajo del mouse en el Canvas del mundo (el
    /// mismo raycast que recibe el clic) y, si es algo tocable ahora, le pone alrededor un
    /// borde que brilla y late. Igual que el resto del cuarto, sin fósforo no se marca nada
    /// (salvo lo que se ve con la luz de la puerta al principio).
    /// Las pantallas de UI (inventario, pausa, primeros planos) no se marcan.
    /// </summary>
    public class ResaltadorInteractuables : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.83f, 0.52f, 1f);
        [Tooltip("Separación entre el objeto y el borde (unidades del Canvas del mundo).")]
        [SerializeField] private float margen = 8f;
        [Tooltip("Grosor del brillo (unidades del Canvas del mundo).")]
        [SerializeField] private float grosor = 16f;
        [SerializeField] private float velocidadLatido = 4f;

        private readonly List<RaycastResult> resultados = new List<RaycastResult>();
        private PointerEventData datos;
        private RectTransform objetivo;
        private RectTransform brillo;
        private Image imagenBrillo;
        private readonly Vector3[] esquinas = new Vector3[4];

        private void LateUpdate()
        {
            RectTransform nuevo = Buscar();
            if (nuevo != objetivo)
            {
                objetivo = nuevo;
                if (objetivo == null && brillo != null) brillo.gameObject.SetActive(false);
            }
            if (objetivo == null) return;

            Ubicar(objetivo);
            float a = Mathf.Lerp(0.55f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * velocidadLatido));
            imagenBrillo.color = new Color(color.r, color.g, color.b, color.a * a);
        }

        private void OnDisable()
        {
            objetivo = null;
            if (brillo != null) brillo.gameObject.SetActive(false);
        }

        // ---------- Qué hay debajo del mouse ----------

        private RectTransform Buscar()
        {
            var es = EventSystem.current;
            if (es == null || Mouse.current == null) return null;
            if (Time.timeScale == 0f) return null;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return null;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return null;

            if (datos == null) datos = new PointerEventData(es);
            datos.position = Mouse.current.position.ReadValue();
            resultados.Clear();
            es.RaycastAll(datos, resultados);
            if (resultados.Count == 0) return null;

            // Solo lo de más arriba: lo que recibiría el clic.
            GameObject go = resultados[0].gameObject;
            if (go == null) return null;
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.WorldSpace) return null;

            // El raycast puede caer en un hijo (el texto de un botón): se sube un poco.
            Transform t = go.transform;
            for (int i = 0; i < 3 && t != null; i++, t = t.parent)
                if (Tocable(t.gameObject)) return t as RectTransform;
            return null;
        }

        private static bool HayLuz => FosforoManager.Instance == null || FosforoManager.Instance.PuedeInteractuar();

        private static bool Tocable(GameObject g)
        {
            if (g.GetComponent<ZonaCajon>() != null) return true;            // el cajón decide si se puede
            if (g.GetComponent<ObjetoInventario>() != null) return true;     // sin luz ni se ve
            if (g.GetComponent<ObjetoNarrable>() != null) return HayLuz;
            if (g.GetComponent<ObjetoEnPrimerPlano>() != null) return HayLuz;
            if (g.GetComponent<HotspotArmario>() != null) return HayLuz;

            var b = g.GetComponent<Button>();
            if (b == null || !b.IsInteractable()) return false;
            // Los fondos que cierran y las zonas armadas por código no tienen llamadas
            // guardadas en la escena; los objetos del cuarto, sí.
            int n = b.onClick.GetPersistentEventCount();
            if (n == 0) return false;
            for (int i = 0; i < n; i++)
            {
                // El fondo de una vista que la cierra al tocar afuera no es un objeto.
                if (b.onClick.GetPersistentMethodName(i) == "Cerrar") return false;
                if (b.onClick.GetPersistentTarget(i) is InicioCuarto ic && ic.SeAbreSinFosforo) return true;
            }
            return HayLuz;
        }

        // ---------- El borde ----------

        private void Ubicar(RectTransform t)
        {
            var raiz = t.GetComponentInParent<Canvas>()?.rootCanvas;
            if (raiz == null) return;
            if (brillo == null) Construir();
            if (brillo.parent != raiz.transform)
            {
                brillo.SetParent(raiz.transform, false);
                var c = brillo.GetComponent<Canvas>();
                c.overrideSorting = true;
                c.sortingOrder = 8;
            }

            // La caja que ocupa en el mundo (sirve aunque esté rotado o escalado distinto en x e y).
            t.GetWorldCorners(esquinas);
            Vector3 min = esquinas[0], max = esquinas[0];
            for (int i = 1; i < 4; i++) { min = Vector3.Min(min, esquinas[i]); max = Vector3.Max(max, esquinas[i]); }
            Vector3 escala = raiz.transform.lossyScale;
            Vector3 centro = (min + max) * 0.5f;
            brillo.position = new Vector3(centro.x, centro.y, raiz.transform.position.z);
            brillo.rotation = raiz.transform.rotation;
            brillo.sizeDelta = new Vector2((max.x - min.x) / escala.x, (max.y - min.y) / escala.y) + Vector2.one * (margen + grosor) * 2f;
            imagenBrillo.pixelsPerUnitMultiplier = BordeSprite / grosor;
            if (!brillo.gameObject.activeSelf) brillo.gameObject.SetActive(true);
        }

        private const int TamSprite = 64;
        private const float BordeSprite = 24f;

        private void Construir()
        {
            var go = new GameObject("ResaltadoInteractuable", typeof(RectTransform), typeof(Image));
            brillo = (RectTransform)go.transform;
            brillo.anchorMin = brillo.anchorMax = brillo.pivot = new Vector2(0.5f, 0.5f);
            imagenBrillo = go.GetComponent<Image>();
            imagenBrillo.sprite = CrearSprite();
            imagenBrillo.type = Image.Type.Sliced;
            imagenBrillo.fillCenter = false;
            imagenBrillo.raycastTarget = false;
            // Encima de todo lo del cuarto (muebles abiertos 4, objetos 5, cajón 6), sin luz:
            // es una marca del juego, se tiene que ver.
            var c = go.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 8;
            go.SetActive(false);
        }

        // Un borde que brilla: línea clara al medio del margen y un halo que se apaga hacia
        // los dos lados. Con 9-slice, así sirve para cualquier tamaño.
        private static Sprite CrearSprite()
        {
            var tex = new Texture2D(TamSprite, TamSprite, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float linea = BordeSprite * 0.5f;
            for (int y = 0; y < TamSprite; y++)
            for (int x = 0; x < TamSprite; x++)
            {
                // Distancia al rectángulo de la línea (con esquinas redondeadas).
                float dx = Mathf.Max(linea - x, x - (TamSprite - 1 - linea), 0f);
                float dy = Mathf.Max(linea - y, y - (TamSprite - 1 - linea), 0f);
                float fuera = Mathf.Sqrt(dx * dx + dy * dy);
                float dentro = Mathf.Min(Mathf.Min(x - linea, TamSprite - 1 - linea - x), Mathf.Min(y - linea, TamSprite - 1 - linea - y));
                float d = fuera > 0f ? fuera : Mathf.Max(0f, dentro);
                float halo = Mathf.Exp(-d * d / (2f * 4.5f * 4.5f)) * 0.75f;
                float nucleo = Mathf.Clamp01(1.6f - d) * 0.6f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(halo + nucleo)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, TamSprite, TamSprite), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(BordeSprite, BordeSprite, BordeSprite, BordeSprite));
        }
    }
}
