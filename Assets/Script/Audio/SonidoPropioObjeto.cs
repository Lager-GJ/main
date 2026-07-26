using UnityEngine;

/// <summary>
/// Componente opcional para darle a un objeto interactivo puntual (ej. la lata de
/// galletas) un sonido propio distinto del genérico de "explorar objeto". Agrégalo
/// solo a los objetos que lo necesiten, como componente extra — no modifica
/// ObjetoInteractivo.cs ni afecta a los demás objetos que no lo tengan.
/// </summary>
public class SonidoPropioObjeto : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    public AudioClip Clip => clip;
}