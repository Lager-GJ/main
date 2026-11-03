using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento del niño solo en el eje horizontal (izquierda/derecha), como en los
/// point & click clásicos de una sola habitación: el personaje camina por el piso
/// del cuarto de un lado a otro, no se mueve verticalmente en el mundo.
/// </summary>
public class NinoController : MonoBehaviour
{
    [Tooltip("Velocidad de caminata en unidades por segundo.")]
    [SerializeField] private float velocidad = 3f;

    [Header("Límites del cuarto (eje X)")]
    [Tooltip("Posición X más a la izquierda a la que puede llegar el niño. Ajústalo al borde izquierdo del fondo/room cuando llegue el arte final.")]
    [SerializeField] private float limiteIzquierdo = -4f;

    [Tooltip("Posición X más a la derecha a la que puede llegar el niño. Ajústalo al borde derecho del fondo/room cuando llegue el arte final.")]
    [SerializeField] private float limiteDerecho = 4f;

    [Tooltip("Opcional: Animator del niño. Puede quedar vacío mientras no haya animaciones listas.")]
    [SerializeField] private Animator animator;

    /// <summary>
    /// Se dispara cuando Luis camina contra un límite: -1 = izquierdo, 1 = derecho.
    /// Lo usa ParedesCuarto para pasar a la pared siguiente.
    /// </summary>
    public event System.Action<int> OnEmpujaBorde;

    /// <summary>Mientras es true Luis no camina (por ejemplo, durante el fundido entre paredes).</summary>
    public bool Bloqueado { get; set; }

    /// <summary>Cambia los límites del cuarto (cada pared tiene los suyos).</summary>
    public void SetLimites(float izquierdo, float derecho)
    {
        limiteIzquierdo = izquierdo;
        limiteDerecho = derecho;
    }

    /// <summary>Lleva a Luis a otra X al instante, sin caminar (también mueve su Rigidbody2D).</summary>
    public void Teletransportar(float x)
    {
        Vector3 p = new Vector3(x, transform.position.y, transform.position.z);
        transform.position = p;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = p;
    }

    private void Awake()
    {
        // Auto-asignamos el Animator si se nos olvidó arrastrarlo en el inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            // Si el Animator está en un GameObject hijo (como un Sprite), usaríamos:
            // animator = GetComponentInChildren<Animator>();
        }
    }
    private void Update()
    {
        // Con una ventana de objeto abierta el fósforo está pausado por inspección:
        // el niño no camina mientras el jugador lee, o tendría ventaja sobre la Presencia.
        if (Bloqueado || (FosforoManager.Instance != null && FosforoManager.Instance.PausadoPorInspeccion))
        {
            Mover(0f); // también apaga la animación de caminar
            return;
        }

        float direccionX = LeerDireccionX();
        Mover(direccionX);
    }

    private float LeerDireccionX()
    {
        if (Keyboard.current == null)
            return 0f;

        float x = 0f;

        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            x -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            x += 1f;

        return x;
    }

    private void Mover(float direccionX)
    {
        bool caminando = direccionX != 0f;

        if (caminando)
        {
            float nuevaX = transform.position.x + direccionX * velocidad * Time.deltaTime;

            // Clamp: no dejamos que el niño camine más allá de los bordes del cuarto.
            nuevaX = Mathf.Clamp(nuevaX, limiteIzquierdo, limiteDerecho);

            // Llegó al borde y sigue empujando hacia afuera: avisamos (cambio de pared).
            if ((direccionX < 0f && nuevaX <= limiteIzquierdo) || (direccionX > 0f && nuevaX >= limiteDerecho))
                OnEmpujaBorde?.Invoke(direccionX < 0f ? -1 : 1);

            transform.position = new Vector3(nuevaX, transform.position.y, transform.position.z);

            // Volteamos el sprite mirando hacia donde camina, para no depender de
            // animaciones separadas de cada lado.
            Vector3 escala = transform.localScale;
            transform.localScale = new Vector3(Mathf.Sign(direccionX) * Mathf.Abs(escala.x), escala.y, escala.z);
        }

        if (animator != null)
            animator.SetBool("EstaCaminando", caminando);
    }
}