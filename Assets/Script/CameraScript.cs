using UnityEngine;

/// <summary>
/// CameraScript: Camara que sigue al Player suavemente.
/// NOTA: En la escena usa SOLO este script O CamaraController, no ambos en la misma camara.
/// CORREGIDO: null-check para evitar NullReferenceException si 'Player' no esta asignado.
/// </summary>
public class CameraScript : MonoBehaviour
{
    public Transform Player;
    public float velocidadCamara = 0.025f;
    private Vector3 desplazamiento;

    private void Start()
    {
        // Si no se asigno en Inspector, buscar por tag
        if (Player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) Player = p.transform;
            else Debug.LogWarning("[CameraScript] No se encontro el Player. Asignalo en el Inspector.");
        }

        if (Player != null)
            desplazamiento = transform.position - Player.position;
    }

    private void LateUpdate()
    {
        if (Player == null) return;

        Vector3 posicionDeseada = Player.position + desplazamiento;
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, velocidadCamara);
    }
}
