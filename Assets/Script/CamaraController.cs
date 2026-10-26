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

    private void Awake()
    {
        Instance = this;
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

        transform.position = posicionSuave + OffsetSacudida();
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
