using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra visualmente cuántos fósforos le quedan al niño: un ícono de UI por cada
/// fósforo, que cambia a la versión "quemado" a medida que se van consumiendo.
/// Se engancha a FosforoManager.OnFosforosRestantesCambiado (evento que ya existía)
/// en vez de que FosforoManager sepa que este inventario existe.
/// </summary>
public class InventarioFosforo : MonoBehaviour
{
    [Tooltip("Un Image de UI por cada fósforo, en el orden en que se van a ir consumiendo.")]
    [SerializeField] private Image[] iconosFosforos;

    [Tooltip("Sprite de un fósforo nuevo, sin usar.")]
    [SerializeField] private Sprite spriteFosforoNuevo;

    [Tooltip("Sprite de un fósforo ya consumido/quemado.")]
    [SerializeField] private Sprite spriteFosforoQuemado;

    private void OnEnable()
    {
        FosforoManager.OnFosforosRestantesCambiado += ActualizarIconos;
    }

    private void OnDisable()
    {
        FosforoManager.OnFosforosRestantesCambiado -= ActualizarIconos;
    }

    private void Start()
    {
        // Pintamos el estado inicial (todos nuevos) usando el conteo real de
        // FosforoManager, no el largo del array — evita que se desincronicen si
        // alguien cambia "Fosforos Iniciales" en el Inspector y se olvida de acá.
        if (FosforoManager.Instance != null)
            ActualizarIconos(FosforoManager.Instance.FosforosRestantes);
    }

    private void ActualizarIconos(int fosforosRestantes)
    {
        for (int i = 0; i < iconosFosforos.Length; i++)
        {
            // De izquierda a derecha: los primeros "fosforosRestantes" iconos se ven
            // nuevos, el resto (ya gastados) se ven quemados.
            bool esNuevo = i < fosforosRestantes;
            iconosFosforos[i].sprite = esNuevo ? spriteFosforoNuevo : spriteFosforoQuemado;
        }
    }
}