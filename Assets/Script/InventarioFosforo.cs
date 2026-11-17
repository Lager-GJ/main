using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

/// <summary>
/// La caja de fósforos «El Cuco» en la pantalla, arriba a la derecha (diseño del equipo,
/// 2026-11-17): una etiqueta oscura con la caja y los 4 fósforos. Los que quedan tienen
/// la cabeza roja; los gastados son solo el contorno. Hay un dibujo por cantidad (0 a 4).
///
/// Al encender uno: la etiqueta late, la cabeza del fósforo que se usa se prende (un
/// brillo naranja que sube y se apaga) y el dibujo pasa al de un fósforo menos.
///
/// Se arma en runtime al lado de ContenedorFosforos (el objeto donde está este script), que
/// sigue siendo el que se prende al recoger los fósforos: mientras ContenedorFosforos está
/// apagado, la caja tampoco se ve. Los íconos viejos (uno por fósforo) quedan escondidos.
/// Escucha los eventos de FosforoManager, sin que FosforoManager sepa que esto existe.
/// Las animaciones usan tiempo real: el tutorial o la cámara lenta no las dejan a medias.
/// Antes (hasta 2026-11-17) era la caja «La Chispa» arriba a la izquierda, que se abría y
/// raspaba un fósforo; esas piezas ya no se usan.
/// </summary>
public class InventarioFosforo : MonoBehaviour
{
    [Header("Dibujos (uno por cantidad de fósforos)")]
    [Tooltip("Índice = fósforos que quedan: 0 = los cuatro gastados ... 4 = los cuatro nuevos (HUD_Fosforos_0..4).")]
    [SerializeField] private Sprite[] estados = new Sprite[5];

    [Header("Lugar")]
    [Tooltip("Distancia a la esquina de arriba a la derecha, en píxeles de 1920x1080.")]
    [SerializeField] private Vector2 margen = new Vector2(36f, 30f);
    [Tooltip("Alto de la etiqueta en pantalla.")]
    [SerializeField] private float alto = 118f;

    [Header("Al encender")]
    [SerializeField] private Color colorLlama = new Color(1f, 0.55f, 0.12f, 1f);

    // Cabezas de los 4 fósforos en el dibujo (320 x 118 px), medidas desde arriba a la izquierda.
    private static readonly float[] CabezaX = { 173f, 200f, 226f, 253f };
    private const float CabezaY = 28f;
    private const float AnchoDibujo = 320f, AltoDibujo = 118f;

    private RectTransform raiz;
    private CanvasGroup grupo;
    private Image etiqueta;
    private Image llama;
    private Coroutine animacion;
    private int quedan;
    private static Sprite brillo;

    // Mientras la caja grande del cajón (VistaCajaFosforos) está en pantalla, esta se
    // esconde: no se ven dos cajas a la vez ni dos animaciones de encender.
    private Terror.UI.VistaCajaFosforos cajaGrande;
    private float visible = 1f;

    /// <summary>Alto que ocupa en pantalla desde el borde de arriba (para poner cosas debajo).</summary>
    public float BordeInferior => margen.y + alto;

    private void Awake()
    {
        // Los íconos viejos se esconden (siguen en la escena por si se quiere volver atrás).
        foreach (Transform hijo in transform) hijo.gameObject.SetActive(false);
        Armar();
    }

    private void OnEnable()
    {
        FosforoManager.OnFosforosRestantesCambiado += AlCambiarCantidad;
        FosforoManager.OnFosforoEncendido += AlEncender;
        if (raiz != null) raiz.gameObject.SetActive(true);
        quedan = FosforoManager.Instance != null ? FosforoManager.Instance.FosforosRestantes : 0;
        DejarEnReposo();
    }

    private void OnDisable()
    {
        FosforoManager.OnFosforosRestantesCambiado -= AlCambiarCantidad;
        FosforoManager.OnFosforoEncendido -= AlEncender;
        animacion = null; // Unity corta las coroutines al apagar el objeto
        if (raiz != null) raiz.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (raiz != null) Destroy(raiz.gameObject);
    }

    private void AlCambiarCantidad(int restantes)
    {
        quedan = restantes;
        // Durante la animación el dibujo cambia a mitad del latido.
        if (animacion == null) PintarEstado();
    }

    private void AlEncender()
    {
        if (!isActiveAndEnabled || raiz == null) return;
        StartCoroutine(EncenderAlFrameSiguiente());
    }

    // Se espera un frame: al prender con la caja grande en la mano, InicioCuarto la
    // cierra en el mismo evento y el fósforo tiene que salir de esta caja (2026-11-17).
    // Si la caja grande sigue en pantalla, ella ya anima el fósforo que sale.
    private IEnumerator EncenderAlFrameSiguiente()
    {
        yield return null;
        if (CajaGrandeEnPantalla()) { PintarEstado(); yield break; }
        if (animacion != null) StopCoroutine(animacion);
        DejarEnReposo();
        animacion = StartCoroutine(AnimarEncendido());
    }

    private bool CajaGrandeEnPantalla()
    {
        if (cajaGrande == null)
            cajaGrande = FindFirstObjectByType<Terror.UI.VistaCajaFosforos>(FindObjectsInactive.Include);
        return cajaGrande != null && cajaGrande.CapaAbierta;
    }

    private void LateUpdate()
    {
        if (grupo == null) return;
        // La caja grande se crea recién al abrir el cajón: se busca de vez en cuando.
        bool tapada = (cajaGrande != null || Time.frameCount % 20 == 0) && CajaGrandeEnPantalla();
        visible = Mathf.MoveTowards(visible, tapada ? 0f : 1f, Time.unscaledDeltaTime * 4f);
        grupo.alpha = visible;
    }

    // ---------- Armado ----------

    private void Armar()
    {
        // Va al lado de ContenedorFosforos (mismo padre, justo después), sin heredar su escala.
        Transform padre = transform.parent != null ? transform.parent : transform;
        raiz = NuevoRect("CajaFosforosHUD", padre, Vector2.zero, Vector2.zero);
        if (padre == transform.parent) raiz.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(1f, 1f);
        raiz.anchoredPosition = new Vector2(-margen.x, -margen.y);

        Sprite muestra = Estado(4);
        float proporcion = muestra != null && muestra.rect.height > 0f ? muestra.rect.width / muestra.rect.height : AnchoDibujo / AltoDibujo;
        raiz.sizeDelta = new Vector2(alto * proporcion, alto);
        grupo = raiz.gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        etiqueta = NuevaImagen("Etiqueta", raiz, Vector2.zero, raiz.sizeDelta, muestra);
        etiqueta.preserveAspect = true;
        if (muestra == null) etiqueta.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        llama = NuevaImagen("Llama", etiqueta.transform, Vector2.zero, Vector2.one * alto * 0.5f, Brillo());
        llama.color = new Color(colorLlama.r, colorLlama.g, colorLlama.b, 0f);

        foreach (var g in raiz.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
    }

    private Sprite Estado(int cantidad)
    {
        if (estados == null || estados.Length == 0) return null;
        return estados[Mathf.Clamp(cantidad, 0, estados.Length - 1)];
    }

    private void DejarEnReposo()
    {
        if (raiz == null) return;
        etiqueta.rectTransform.localScale = Vector3.one;
        llama.color = new Color(colorLlama.r, colorLlama.g, colorLlama.b, 0f);
        PintarEstado();
    }

    private void PintarEstado()
    {
        if (etiqueta == null) return;
        Sprite s = Estado(quedan);
        if (s != null) etiqueta.sprite = s;
    }

    // Centro de la cabeza del fósforo i (0 = el de la izquierda) dentro de la etiqueta.
    private Vector2 Cabeza(int i)
    {
        Vector2 tam = etiqueta.rectTransform.sizeDelta;
        float x = (CabezaX[Mathf.Clamp(i, 0, 3)] / AnchoDibujo - 0.5f) * tam.x;
        float y = (0.5f - CabezaY / AltoDibujo) * tam.y;
        return new Vector2(x, y);
    }

    // ---------- Animación de encender ----------

    private IEnumerator AnimarEncendido()
    {
        // El que se prende ya se descontó: es el primero de los gastados, de izquierda a derecha.
        int usado = Mathf.Clamp(quedan, 0, 3);
        Sprite antes = Estado(quedan + 1);
        if (antes != null) etiqueta.sprite = antes;
        RectTransform rl = llama.rectTransform;
        Vector2 cabeza = Cabeza(usado);

        // La etiqueta late y la cabeza del fósforo se prende.
        yield return Animar(0.18f, p =>
        {
            etiqueta.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, 1f - (1f - p) * (1f - p));
            rl.anchoredPosition = cabeza;
            rl.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, p);
            llama.color = new Color(colorLlama.r, colorLlama.g, colorLlama.b, p);
        });

        // A mitad del latido el dibujo pasa a un fósforo menos.
        PintarEstado();

        // La llama sube un poco (se va a la mano de Luis) y se apaga; la etiqueta vuelve.
        yield return Animar(0.55f, p =>
        {
            float e = p * p * (3f - 2f * p);
            etiqueta.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, e);
            rl.anchoredPosition = cabeza + new Vector2(0f, alto * 0.35f * e);
            rl.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, e);
            llama.color = new Color(colorLlama.r, colorLlama.g, colorLlama.b, 1f - p);
        });

        animacion = null;
        DejarEnReposo();
    }

    // Círculo blanco que se desvanece hacia los bordes (se tiñe con el color de la Image).
    private static Sprite Brillo()
    {
        if (brillo != null) return brillo;
        const int lado = 64;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[lado * lado];
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float dx = (x + 0.5f) / lado * 2f - 1f, dy = (y + 0.5f) / lado * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        brillo = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f));
        return brillo;
    }
}
