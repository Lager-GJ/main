using UnityEngine;

public class CamaraController : MonoBehaviour
{
    /// <summary>La cámara del cuarto. La usa la Presencia para sacudirla.</summary>
    public static CamaraController Instance { get; private set; }

    [Header("Objetivo")]
    public Transform target;          // El Transform de tu jugador

    [Header("Configuración de Seguimiento")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f; // Suavizado de movimiento (menor valor = más suave)
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Mantiene la cámara a distancia en Z

    [Header("Límites del Mapa")]
    public Vector2 minBounds; // Esquina inferior izquierda (X mínima, Y mínima)
    public Vector2 maxBounds; // Esquina superior derecha (X máxima, Y máxima)

    // La sacudida se suma DESPUÉS del suavizado y se guarda aparte: si se metiera en
    // transform.position, el Lerp del frame siguiente la arrastraría y la cámara
    // quedaría corrida.
    private Vector3 posicionSuave;
    private bool posicionIniciada;
    private float amplitudSacudida;
    private float duracionSacudida;
    private float tiempoSacudida;
    private float semillaSacudida;

    // Enfoque de primer plano: la cámara se acerca al objeto que se está mirando
    // (zoom ortográfico) y vuelve al soltarlo. Corre con tiempo real, así que no se
    // frena con la cámara lenta.
    private Camera cam;
    private float tamanoBase;
    private Vector3 puntoEnfoque;
    private float zoomEnfoque = 1f;
    private float pesoEnfoque;        // 0 = cámara normal, 1 = enfocada
    private float pesoObjetivo;
    private float duracionEnfoque = 0.45f;

    /// <summary>True mientras la cámara está acercándose, enfocada o volviendo.</summary>
    public bool Enfocando => pesoEnfoque > 0f || pesoObjetivo > 0f;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam != null) tamanoBase = cam.orthographicSize;
    }

    /// <summary>
    /// Acerca la cámara a 'punto' (mundo). zoom = fracción del tamaño normal
    /// (0.55 = se ve un 55% de la altura de siempre).
    /// </summary>
    public void Enfocar(Vector3 punto, float zoom, float duracion)
    {
        puntoEnfoque = punto;
        zoomEnfoque = Mathf.Clamp(zoom, 0.1f, 1f);
        duracionEnfoque = Mathf.Max(0.01f, duracion);
        pesoObjetivo = 1f;
    }

    /// <summary>Vuelve a la cámara normal (siguiendo a Luis).</summary>
    public void Soltar(float duracion)
    {
        duracionEnfoque = Mathf.Max(0.01f, duracion);
        pesoObjetivo = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Sacude la cámara (temblor que se apaga solo). amplitud en unidades del mundo.
    /// Corre con Time.deltaTime, así que se detiene con la pausa. Si llega una
    /// sacudida más fuerte mientras dura otra, la reemplaza.
    /// </summary>
    public void Sacudir(float amplitud, float duracion)
    {
        if (amplitud <= 0f || duracion <= 0f) return;

        float restante = tiempoSacudida < duracionSacudida
            ? amplitudSacudida * (1f - tiempoSacudida / duracionSacudida)
            : 0f;
        if (amplitud < restante) return;

        amplitudSacudida = amplitud;
        duracionSacudida = duracion;
        tiempoSacudida = 0f;
        semillaSacudida = Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (!posicionIniciada)
        {
            posicionSuave = transform.position;
            posicionIniciada = true;
        }

        // 1. Calculamos la posición a la que quiere ir la cámara
        Vector3 desiredPosition = target.position + offset;

        // 2. Restringimos las coordenadas X e Y dentro de los límites
        float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, desiredPosition.z);

        // 3. Movemos la cámara de forma fluida
        posicionSuave = Vector3.Lerp(posicionSuave, clampedPosition, smoothSpeed);

        pesoEnfoque = Mathf.MoveTowards(pesoEnfoque, pesoObjetivo, Time.unscaledDeltaTime / duracionEnfoque);
        float e = Mathf.SmoothStep(0f, 1f, pesoEnfoque);
        Vector3 foco = new Vector3(puntoEnfoque.x, puntoEnfoque.y, posicionSuave.z);
        transform.position = Vector3.Lerp(posicionSuave, foco, e) + OffsetSacudida();

        if (cam != null && cam.orthographic && tamanoBase > 0f)
            cam.orthographicSize = Mathf.Lerp(tamanoBase, tamanoBase * zoomEnfoque, e);
    }

    private Vector3 OffsetSacudida()
    {
        if (tiempoSacudida >= duracionSacudida) return Vector3.zero;

        tiempoSacudida += Time.deltaTime;
        float k = 1f - Mathf.Clamp01(tiempoSacudida / duracionSacudida);
        float t = tiempoSacudida * 22f;
        float x = (Mathf.PerlinNoise(semillaSacudida, t) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(semillaSacudida + 37f, t) - 0.5f) * 2f;
        return new Vector3(x, y * 0.6f, 0f) * amplitudSacudida * k;
    }
}
