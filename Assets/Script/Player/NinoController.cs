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

    private void Update()
    {
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