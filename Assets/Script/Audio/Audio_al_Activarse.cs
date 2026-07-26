using UnityEngine;

/// <summary>
/// Reproduce un sonido apenas este GameObject se activa (SetActive(true)). Pensado
/// para pegarlo directamente en los cuadros de cómic de StoryManager (storyPages) o
/// en momentos puntuales (la puerta abriéndose, el foco quebrándose) — sin tocar
/// StoryManager.cs ni ningún otro script existente, solo se agrega como componente extra.
/// </summary>
public class AudioAlActivarse : MonoBehaviour
{
    [SerializeField] private AudioSource fuenteEfectos;
    [SerializeField] private AudioClip sonido;

    private void OnEnable()
    {
        if (fuenteEfectos != null && sonido != null)
            fuenteEfectos.PlayOneShot(sonido);
    }
}