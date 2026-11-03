using Terror;
using UnityEngine;

/// <summary>
/// La llama del fósforo como dibujo aparte de Luis (hoy la llama viene pintada dentro de
/// sus sprites "-LUZ"). Va en un hijo de Luis, en la mano, con un SpriteRenderer que
/// tenga el dibujo de la llama de Anavi. Hace tres cosas:
///  - al caminar se inclina hacia atrás, como una llama de verdad;
///  - mientras el fósforo se muere (el 30% final) se achica y se pone roja;
///  - con cada nivel de la Presencia se agita más.
/// Solo se ve con el fósforo encendido.
///
/// El dibujo tiene que tener el pivote en la base de la llama (abajo al centro), para
/// que se incline y se achique desde la cabeza del fósforo y no desde su centro.
/// </summary>
public class LlamaFosforo : MonoBehaviour
{
    [Tooltip("El dibujo de la llama. Vacío = usa el SpriteRenderer de este mismo objeto.")]
    [SerializeField] private SpriteRenderer llama;

    [Header("Al morir")]
    [Tooltip("Color de la llama entera (blanco = el dibujo tal cual).")]
    [SerializeField] private Color colorNormal = Color.white;
    [Tooltip("Color cuando ya casi se apaga.")]
    [SerializeField] private Color colorAlMorir = new Color(1f, 0.25f, 0.1f, 1f);
    [Tooltip("Tamaño justo antes de apagarse (0.35 = un 35% del tamaño normal).")]
    [SerializeField, Range(0.05f, 1f)] private float tamanoAlMorir = 0.35f;

    [Header("Al caminar")]
    [Tooltip("Grados que se inclina hacia atrás caminando a toda velocidad.")]
    [SerializeField] private float inclinacionMaxima = 25f;
    [Tooltip("Velocidad de Luis (unidades por segundo) a la que la llama llega a la inclinación máxima. Luis camina a 1000.")]
    [SerializeField] private float velocidadParaInclinacionMaxima = 1000f;
    [Tooltip("Qué tan rápido sigue la llama al movimiento (más = más rígida).")]
    [SerializeField] private float suavizado = 8f;

    [Header("Presencia")]
    [Tooltip("Cuánto se agita en cada nivel de la Presencia. Posición 0 = sin fósforos gastados.")]
    [SerializeField] private float[] agitacionPorNivel = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    [Tooltip("Grados que gira de un lado a otro con la agitación máxima.")]
    [SerializeField] private float gradosAgitacion = 15f;
    [Tooltip("Cuánto cambia de tamaño con la agitación máxima (0.2 = ±20%).")]
    [SerializeField] private float escalaAgitacion = 0.2f;

    private Vector3 escalaBase;
    private float ultimaX;
    private float inclinacion;
    private float agitacion;
    private float semilla;

    private void Awake()
    {
        if (llama == null) llama = GetComponent<SpriteRenderer>();
        escalaBase = transform.localScale;
        semilla = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        GameEvents.OnNivelPresenciaCambiado += AlCambiarNivel;
        ultimaX = transform.position.x;
    }

    private void OnDisable()
    {
        GameEvents.OnNivelPresenciaCambiado -= AlCambiarNivel;
    }

    private void AlCambiarNivel(int nivel)
    {
        if (agitacionPorNivel == null || agitacionPorNivel.Length == 0) return;
        agitacion = agitacionPorNivel[Mathf.Clamp(nivel, 0, agitacionPorNivel.Length - 1)];
    }

    private void LateUpdate()
    {
        var fosforo = FosforoManager.Instance;
        bool encendido = fosforo != null && fosforo.Encendido;
        if (llama != null) llama.enabled = encendido;

        float x = transform.position.x;
        float dt = Time.deltaTime;
        if (!encendido || dt <= 0f)
        {
            ultimaX = x;
            return;
        }

        // Velocidad real de Luis. Un salto grande (cambio de pared) no cuenta como caminar.
        float vx = (x - ultimaX) / dt;
        ultimaX = x;
        if (Mathf.Abs(vx) > velocidadParaInclinacionMaxima * 3f) vx = 0f;

        // Caminando a la derecha la punta se va a la izquierda (giro positivo) y al revés.
        float objetivo = inclinacionMaxima * Mathf.Clamp(vx / Mathf.Max(1f, velocidadParaInclinacionMaxima), -1f, 1f);
        inclinacion = Mathf.Lerp(inclinacion, objetivo, 1f - Mathf.Exp(-suavizado * dt));

        // Agitación de la Presencia: ruido que tiembla más rápido cuanto más fuerte es.
        float t = Time.time * (6f + 10f * agitacion);
        float ruidoGiro = (Mathf.PerlinNoise(t, semilla) * 2f - 1f) * gradosAgitacion * agitacion;
        float ruidoTam = (Mathf.PerlinNoise(semilla, t) * 2f - 1f) * escalaAgitacion * agitacion;

        // Luis se da vuelta con escala negativa en x: el giro local se invierte con él.
        float signo = Mathf.Sign(transform.lossyScale.x) * Mathf.Sign(transform.localScale.x);
        transform.localRotation = Quaternion.Euler(0f, 0f, (inclinacion + ruidoGiro) * signo);

        float fuerza = Mathf.Lerp(tamanoAlMorir, 1f, fosforo.FuerzaLlama);
        transform.localScale = escalaBase * fuerza * (1f + ruidoTam);

        if (llama != null)
            llama.color = Color.Lerp(colorNormal, colorAlMorir, fosforo.RojezLlama);
    }
}
