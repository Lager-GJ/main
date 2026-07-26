using UnityEngine;

/// <summary>
/// Utilidad genérica: reproduce un clip cuando se llama a ReproducirSonido(). Pensado
/// para conectarlo al OnClick() de cualquier Button en el Inspector (ej. el botón
/// "Siguiente" de StoryManager, para el sonido de pasar hoja de cómic), agregándolo
/// como una acción más del OnClick — sin tocar StoryManager.cs.
/// </summary>
public class ReproducirSonidoUI : MonoBehaviour
{
    [SerializeField] private AudioSource fuenteEfectos;
    [SerializeField] private AudioClip sonido;

    public void ReproducirSonido()
    {
        if (fuenteEfectos != null && sonido != null)
            fuenteEfectos.PlayOneShot(sonido);
    }
}