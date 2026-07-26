using UnityEngine;

/// <summary>
/// Sonido genérico al inspeccionar cualquier objeto interactivo (cajón, armario,
/// llave...). Si el objeto tiene un componente SonidoPropioObjeto, se usa ese sonido
/// en vez del genérico (ej. la lata de galletas). No modifica ObjetoInteractivo.cs:
/// solo escucha su evento público OnObjetoInspeccionado, que ya existe.
/// </summary>
public class SonidoAlInteractuar : MonoBehaviour
{
    [SerializeField] private AudioSource fuenteEfectos;
    [SerializeField] private AudioClip sonidoGenerico;

    private void OnEnable() => ObjetoInteractivo.OnObjetoInspeccionado += Manejar;
    private void OnDisable() => ObjetoInteractivo.OnObjetoInspeccionado -= Manejar;

    private void Manejar(ObjetoInteractivo objeto)
    {
        if (fuenteEfectos == null)
            return;

        SonidoPropioObjeto propio = objeto.GetComponent<SonidoPropioObjeto>();
        AudioClip clip = (propio != null && propio.Clip != null) ? propio.Clip : sonidoGenerico;

        if (clip != null)
            fuenteEfectos.PlayOneShot(clip);
    }
}