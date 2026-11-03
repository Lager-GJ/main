using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

/// <summary>
/// La caja de fósforos «La Chispa» en la pantalla, arriba a la izquierda. Tiene dos estados:
///  - En reposo (casi todo el juego): una caja chica con el número de fósforos que quedan,
///    un poco transparente y sin recibir clics, para no estorbar.
///  - Al encender: la caja se agranda ahí mismo, sale la bandeja, sale un fósforo, se raspa
///    en el costado, salta la chispa y se prende. Después todo vuelve a ser la caja chica.
///
/// Se arma en runtime al lado de ContenedorFosforos (el objeto donde está este script), que
/// sigue siendo el que se prende al recoger los fósforos: mientras ContenedorFosforos está
/// apagado, la caja tampoco se ve. Los íconos viejos (uno por fósforo) quedan escondidos.
/// Escucha los eventos de FosforoManager, sin que FosforoManager sepa que esto existe.
/// Las animaciones usan tiempo real: el tutorial o la cámara lenta no las dejan a medias.
/// </summary>
public class InventarioFosforo : MonoBehaviour
{
    [Header("Íconos viejos (ya no se usan: se esconden)")]
    [SerializeField] private Image[] iconosFosforos;
    [SerializeField] private Sprite spriteFosforoNuevo;
    [SerializeField] private Sprite spriteFosforoQuemado;

    [Header("Dibujos de la caja")]
    [Tooltip("El frente de la caja (Caja_Frente).")]
    [SerializeField] private Sprite spriteCaja;
    [Tooltip("La bandeja que sale de la caja (Caja_Bandeja).")]
    [SerializeField] private Sprite spriteBandeja;
    [Tooltip("Un fósforo suelto, con la cabeza arriba (Caja_Fosforo).")]
    [SerializeField] private Sprite spriteFosforo;
    [SerializeField] private TMP_FontAsset fuente;

    [Header("Reposo")]
    [Tooltip("Distancia a la esquina de arriba a la izquierda, en píxeles de 1920x1080.")]
    [SerializeField] private Vector2 margen = new Vector2(36f, 36f);
    [Tooltip("Alto de la caja chica.")]
    [SerializeField] private float altoReposo = 96f;
    [Tooltip("Qué tan visible se ve en reposo (1 = del todo).")]
    [SerializeField, Range(0.2f, 1f)] private float alfaReposo = 0.85f;

    [Header("Al encender")]
    [Tooltip("Cuánto se agranda la caja durante la animación.")]
    [SerializeField] private float escalaEncendido = 2.6f;
    [SerializeField] private Color colorLlama = new Color(1f, 0.55f, 0.12f, 1f);
    [SerializeField] private Color colorCentroLlama = new Color(1f, 0.93f, 0.6f, 1f);
    [SerializeField] private Color colorUltimoFosforo = new Color(1f, 0.35f, 0.25f, 1f);

    // Medidas de los dibujos (px).
    private const float AnchoCaja = 607f, AltoCaja = 870f;
    private const float AnchoBandeja = 546f, AltoBandeja = 800f;
    private const float AnchoFosforo = 74f, AltoFosforo = 430f;

    private RectTransform raiz;
    private CanvasGroup grupo;
    private RectTransform caja;
    private RectTransform bandeja;
    private RectTransform fosforo;
    private Image imgFosforo;
    private RectTransform llama;
    private Image[] chispas;
    private TextMeshProUGUI contador;

    private Vector2 tamCaja, tamBandeja, tamFosforo;
    private float yBandejaCerrada, yBandejaAbierta;
    private Vector2 posFosforoEnBandeja;
    private Coroutine animacion;
    private int quedan;
    private static Sprite brillo;

    // Mientras la caja grande del cajón (VistaCajaFosforos) está en pantalla, esta se
    // esconde: no se ven dos cajas a la vez ni dos animaciones de encender.
    private Terror.UI.VistaCajaFosforos cajaGrande;
    private float alfaBase = 1f;
    private float visible = 1f;

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
        // Durante la animación el número cambia al final, cuando la caja vuelve a achicarse.
        if (animacion == null) PintarContador();
    }

    private void AlEncender()
    {
        if (!isActiveAndEnabled || raiz == null) return;
        if (CajaGrandeEnPantalla()) return; // la caja grande ya anima el fósforo que sale
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
        grupo.alpha = alfaBase * visible;
    }

    // ---------- Armado ----------

    private void Armar()
    {
        // Va al lado de ContenedorFosforos (mismo padre, justo después): se dibuja donde se
        // dibujaban los íconos viejos, sin heredar su escala.
        Transform padre = transform.parent != null ? transform.parent : transform;
        raiz = NuevoRect("CajaFosforosHUD", padre, Vector2.zero, Vector2.zero);
        if (padre == transform.parent) raiz.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0f, 1f);
        raiz.anchoredPosition = new Vector2(margen.x, -margen.y);

        tamCaja = new Vector2(altoReposo * AnchoCaja / AltoCaja, altoReposo);
        raiz.sizeDelta = new Vector2(tamCaja.x + 120f, tamCaja.y);
        grupo = raiz.gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // La caja crece desde su esquina de arriba a la izquierda.
        caja = NuevoRect("Caja", raiz, Vector2.zero, tamCaja);
        caja.anchorMin = caja.anchorMax = caja.pivot = new Vector2(0f, 1f);
        caja.anchoredPosition = Vector2.zero;

        // Bandeja detrás del frente: cerrada queda escondida, abierta asoma por arriba.
        float k = tamCaja.y / AltoCaja;
        tamBandeja = new Vector2(AnchoBandeja, AltoBandeja) * k;
        yBandejaCerrada = tamCaja.y * 0.5f - 30f * k - tamBandeja.y * 0.5f;
        yBandejaAbierta = yBandejaCerrada + tamCaja.y * 0.38f;
        var imgBandeja = NuevaImagen("Bandeja", caja, new Vector2(0f, yBandejaCerrada), tamBandeja, spriteBandeja);
        if (spriteBandeja == null) imgBandeja.color = new Color(0.25f, 0.18f, 0.42f, 1f);
        bandeja = imgBandeja.rectTransform;

        // El fósforo que va a salir, dentro de la bandeja (detrás del frente).
        tamFosforo = new Vector2(AnchoFosforo, AltoFosforo) * k;
        posFosforoEnBandeja = new Vector2(0f, tamBandeja.y * 0.5f - 50f * k - tamFosforo.y * 0.5f);
        imgFosforo = NuevaImagen("Fosforo", bandeja, posFosforoEnBandeja, tamFosforo, spriteFosforo);
        if (spriteFosforo == null) imgFosforo.color = new Color(0.85f, 0.75f, 0.55f, 1f);
        fosforo = imgFosforo.rectTransform;

        var imgFrente = NuevaImagen("Frente", caja, Vector2.zero, tamCaja, spriteCaja);
        if (spriteCaja == null) imgFrente.color = new Color(0.78f, 0.7f, 0.38f, 1f);

        // Llama: dos brillos (afuera naranja, adentro casi blanco) en la cabeza del fósforo.
        llama = NuevoRect("Llama", fosforo, new Vector2(0f, tamFosforo.y * 0.5f), Vector2.zero);
        llama.pivot = new Vector2(0.5f, 0.2f);
        var afuera = NuevaImagen("Afuera", llama, new Vector2(0f, tamFosforo.x * 1.6f), new Vector2(tamFosforo.x * 3f, tamFosforo.x * 4.6f), Brillo());
        afuera.color = colorLlama;
        var centro = NuevaImagen("Centro", llama, new Vector2(0f, tamFosforo.x * 1.1f), new Vector2(tamFosforo.x * 1.4f, tamFosforo.x * 2.4f), Brillo());
        centro.color = colorCentroLlama;

        chispas = new Image[7];
        for (int i = 0; i < chispas.Length; i++)
        {
            chispas[i] = NuevaImagen("Chispa" + i, caja, Vector2.zero, Vector2.one * tamFosforo.x * 0.9f, Brillo());
            chispas[i].color = colorLlama;
        }

        // Número de fósforos a la derecha de la caja.
        contador = NuevoTexto("Cantidad", raiz, Vector2.zero, new Vector2(110f, tamCaja.y), tamCaja.y * 0.55f, FontStyles.Bold, fuente);
        contador.rectTransform.anchorMin = contador.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        contador.rectTransform.pivot = new Vector2(0f, 0.5f);
        contador.rectTransform.anchoredPosition = new Vector2(tamCaja.x + 10f, 0f);
        contador.alignment = TextAlignmentOptions.Left;
        contador.outlineWidth = 0.2f;
        contador.outlineColor = new Color32(0, 0, 0, 220);

        foreach (var g in raiz.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
    }

    private void DejarEnReposo()
    {
        if (raiz == null) return;
        caja.localScale = Vector3.one;
        bandeja.anchoredPosition = new Vector2(0f, yBandejaCerrada);
        DevolverFosforo();
        llama.gameObject.SetActive(false);
        foreach (var c in chispas) c.gameObject.SetActive(false);
        alfaBase = alfaReposo;
        contador.alpha = 1f;
        contador.transform.localScale = Vector3.one;
        PintarContador();
    }

    private void DevolverFosforo()
    {
        fosforo.SetParent(bandeja, false);
        fosforo.SetAsFirstSibling();
        fosforo.anchoredPosition = posFosforoEnBandeja;
        fosforo.localRotation = Quaternion.identity;
        fosforo.localScale = Vector3.one;
        imgFosforo.color = new Color(imgFosforo.color.r, imgFosforo.color.g, imgFosforo.color.b, 1f);
        // Sin fósforos, la bandeja está vacía.
        fosforo.gameObject.SetActive(quedan > 0);
    }

    private void PintarContador()
    {
        if (contador == null) return;
        contador.text = quedan.ToString();
        contador.color = quedan <= 1 ? colorUltimoFosforo : Color.white;
    }

    // ---------- Animación de encender ----------

    private IEnumerator AnimarEncendido()
    {
        // El que se prende ya se descontó: en la bandeja todavía está.
        fosforo.gameObject.SetActive(true);

        // 1. La caja se agranda y el número se esconde.
        yield return Animar(0.25f, p =>
        {
            float e = 1f - Mathf.Pow(1f - p, 3f);
            caja.localScale = Vector3.one * Mathf.Lerp(1f, escalaEncendido, e);
            alfaBase = Mathf.Lerp(alfaReposo, 1f, p);
            contador.alpha = 1f - p;
        });

        // 2. Sale la bandeja.
        yield return Animar(0.25f, p =>
            bandeja.anchoredPosition = new Vector2(0f, Mathf.Lerp(yBandejaCerrada, yBandejaAbierta, 1f - Mathf.Pow(1f - p, 3f))));

        // 3. Sale el fósforo de la bandeja.
        Vector2 p0 = fosforo.anchoredPosition;
        yield return Animar(0.25f, p =>
            fosforo.anchoredPosition = p0 + new Vector2(0f, tamFosforo.y * 0.75f * Mathf.SmoothStep(0f, 1f, p)));

        // Pasa a ser hijo de la caja (delante del frente) para poder moverse libre.
        fosforo.SetParent(caja, true);
        fosforo.SetAsLastSibling();

        // 4. Va al costado derecho de la caja, inclinado, con la cabeza contra el borde.
        Vector2 desde = fosforo.anchoredPosition;
        Quaternion giroDesde = fosforo.localRotation;
        Quaternion giroRaspar = Quaternion.Euler(0f, 0f, 55f);
        Vector2 inicioRaspar = new Vector2(tamCaja.x * 0.5f + tamFosforo.y * 0.38f, tamCaja.y * 0.3f - tamFosforo.y * 0.2f);
        yield return Animar(0.22f, p =>
        {
            float e = Mathf.SmoothStep(0f, 1f, p);
            fosforo.anchoredPosition = Vector2.Lerp(desde, inicioRaspar, e);
            fosforo.localRotation = Quaternion.Slerp(giroDesde, giroRaspar, e);
        });

        // 5. Raspa hacia abajo por el costado.
        Vector2 finRaspar = inicioRaspar + new Vector2(0f, -tamCaja.y * 0.4f);
        yield return Animar(0.14f, p => fosforo.anchoredPosition = Vector2.Lerp(inicioRaspar, finRaspar, p * p));

        // 6. Chispa y se prende: la llama apunta siempre hacia arriba.
        Vector2 cabeza = CabezaDelFosforo();
        StartCoroutine(Chispazo(cabeza));
        llama.gameObject.SetActive(true);
        Vector2 sigueDesde = fosforo.anchoredPosition;
        Vector2 alejarse = sigueDesde + new Vector2(tamFosforo.y * 0.25f, tamFosforo.y * 0.25f);
        yield return Animar(0.3f, p =>
        {
            fosforo.localRotation = Quaternion.Slerp(giroRaspar, Quaternion.identity, Mathf.SmoothStep(0f, 1f, p));
            fosforo.anchoredPosition = Vector2.Lerp(sigueDesde, alejarse, Mathf.SmoothStep(0f, 1f, p));
            float s = 1f + 2.70158f * Mathf.Pow(p - 1f, 3f) + 1.70158f * Mathf.Pow(p - 1f, 2f);
            AjustarLlama(Mathf.Max(0.01f, s * 1.3f));
        });

        // 7. Arde un momento.
        yield return Animar(0.35f, p => AjustarLlama(1f + (Mathf.PerlinNoise(Time.unscaledTime * 14f, 0.3f) - 0.5f) * 0.3f));

        // 8. El fósforo se va (a la mano de Luis) y la bandeja vuelve a entrar.
        Vector2 vaDesde = fosforo.anchoredPosition;
        yield return Animar(0.3f, p =>
        {
            fosforo.anchoredPosition = vaDesde + new Vector2(tamFosforo.y * 0.4f * p, -tamFosforo.y * 0.1f * p);
            imgFosforo.color = new Color(imgFosforo.color.r, imgFosforo.color.g, imgFosforo.color.b, 1f - p);
            foreach (var g in llama.GetComponentsInChildren<Image>()) g.canvasRenderer.SetAlpha(1f - p);
            bandeja.anchoredPosition = new Vector2(0f, Mathf.Lerp(yBandejaAbierta, yBandejaCerrada, p * p));
        });
        foreach (var g in llama.GetComponentsInChildren<Image>(true)) g.canvasRenderer.SetAlpha(1f);
        llama.gameObject.SetActive(false);

        // 9. Vuelve a ser la caja chica con el número nuevo.
        PintarContador();
        DevolverFosforo();
        yield return Animar(0.25f, p =>
        {
            float e = p * p * (3f - 2f * p);
            caja.localScale = Vector3.one * Mathf.Lerp(escalaEncendido, 1f, e);
            alfaBase = Mathf.Lerp(1f, alfaReposo, p);
            contador.alpha = p;
        });
        yield return Pop(contador.transform, 0.3f);

        animacion = null;
        DejarEnReposo();
    }

    // Posición de la cabeza del fósforo en coordenadas de la caja.
    private Vector2 CabezaDelFosforo()
    {
        Vector3 mundo = fosforo.TransformPoint(new Vector3(0f, tamFosforo.y * 0.45f, 0f));
        // InverseTransformPoint mide desde el pivote de la caja (arriba a la izquierda);
        // las piezas se ubican desde su centro.
        return (Vector2)caja.InverseTransformPoint(mundo) - caja.rect.center;
    }

    private void AjustarLlama(float escala)
    {
        llama.localScale = Vector3.one * escala;
        // Contra el giro del fósforo, para que la llama apunte hacia arriba.
        llama.localRotation = Quaternion.Inverse(fosforo.localRotation);
    }

    private IEnumerator Chispazo(Vector2 desde)
    {
        var direcciones = new Vector2[chispas.Length];
        for (int i = 0; i < chispas.Length; i++)
        {
            float ang = Random.Range(-30f, 210f) * Mathf.Deg2Rad;
            direcciones[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(0.6f, 1.2f) * tamFosforo.y * 0.45f;
            chispas[i].gameObject.SetActive(true);
            chispas[i].rectTransform.anchoredPosition = desde;
        }
        yield return Animar(0.35f, p =>
        {
            for (int i = 0; i < chispas.Length; i++)
            {
                chispas[i].rectTransform.anchoredPosition = desde + direcciones[i] * (1f - (1f - p) * (1f - p));
                chispas[i].rectTransform.localScale = Vector3.one * (1f - p);
                chispas[i].color = new Color(colorLlama.r, colorLlama.g, colorLlama.b, 1f - p);
            }
        });
        foreach (var c in chispas) c.gameObject.SetActive(false);
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
