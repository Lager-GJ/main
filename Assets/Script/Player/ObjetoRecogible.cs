using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Objeto que se recoge con un clic, SIN necesitar fósforo encendido (a diferencia de
/// ObjetoInteractivo, que exige luz). Existe para cosas como los fósforos de la mesita
/// de noche: el jugador todavía no tiene luz cuando los recoge.
/// Avisa con un evento estático, igual que el resto de sistemas, para que el tutorial
/// reaccione sin tener una referencia directa a este objeto.
/// El clic lo detecta ClicEnObjeto, que debe ir en el mismo objeto junto a un Collider2D.
/// </summary>
public class ObjetoRecogible : MonoBehaviour
{
    [Tooltip("Objetos que desaparecen al recogerlo (por ejemplo, el sprite de la cajita de fósforos). Pueden ser este mismo objeto.")]
    [SerializeField] private GameObject[] ocultarAlRecoger;

    [Tooltip("Para conectar sonidos u otros efectos desde el Inspector, sin tocar código.")]
    [SerializeField] private UnityEvent alRecoger;

    public static event Action<ObjetoRecogible> OnRecogido;

    private bool yaRecogido;

    public void Recoger()
    {
        if (yaRecogido) return;
        yaRecogido = true;

        Debug.Log($"[ObjetoRecogible] Recogido: {gameObject.name}");

        // Aviso primero y ocultamos después: si ocultáramos este mismo objeto antes,
        // el evento saldría de un objeto ya desactivado.
        OnRecogido?.Invoke(this);
        alRecoger?.Invoke();

        foreach (var objeto in ocultarAlRecoger)
            if (objeto != null) objeto.SetActive(false);
    }
}
