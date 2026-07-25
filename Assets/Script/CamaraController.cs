using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;          // El Transform de tu jugador

    [Header("Configuración de Seguimiento")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f; // Suavizado de movimiento (menor valor = más suave)
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Mantiene la cámara a distancia en Z

    [Header("Límites del Mapa")]
    public Vector2 minBounds; // Esquina inferior izquierda (X mínima, Y mínima)
    public Vector2 maxBounds; // Esquina superior derecha (X máxima, Y máxima)

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculamos la posición a la que quiere ir la cámara
        Vector3 desiredPosition = target.position + offset;

        // 2. Restringimos las coordenadas X e Y dentro de los límites
        float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, desiredPosition.z);

        // 3. Movemos la cámara de forma fluida
        transform.position = Vector3.Lerp(transform.position, clampedPosition, smoothSpeed);
    }
}