using UnityEngine;
using UnityEngine.Serialization;
using Terror;

/// <summary>
/// Lleva la cuenta de los objetos de supervivencia que junta el niño. Cada uno
/// ralentiza la subida del miedo -- son ayudas para aguantar mas, NO la condicion
/// de victoria.
///
/// La victoria es encontrar la caja de galletas (el tesoro de la abuela), y la
/// dispara ObjetoInteractivo cuando se inspecciona el objeto marcado como
/// esObjetivoDeVictoria. Decision de diseno confirmada 2026-07-31.
///
/// Hasta esa fecha este script tambien llamaba a Ganar() al juntar todos los
/// objetos, asi que habia dos formas independientes de ganar conviviendo. Se
/// quito la de aca; el contador se conserva porque sigue sirviendo para mostrar
/// progreso y para el efecto acumulativo sobre el miedo.
/// </summary>
public class InventarioSupervivencia : MonoBehaviour
{
    public static InventarioSupervivencia Instance { get; private set; }

    [Header("Progreso")]
    [Tooltip("Cantidad de objetos recolectados actualmente.")]
    public int itemsRecolectados = 0;

    // FormerlySerializedAs conserva el valor que ya estaba guardado en la escena
    // bajo el nombre viejo; sin esto Unity lo perderia al renombrar el campo.
    [FormerlySerializedAs("itemsParaGanar")]
    [Tooltip("Cuantos objetos hay en total en el cuarto. Solo informativo (para mostrar progreso); juntarlos todos NO gana la partida.")]
    public int itemsTotales = 3;

    [Tooltip("Cuanto ralentiza el miedo cada objeto recolectado (0.3 = 30% mas lento, acumulativo).")]
    [Range(0f, 1f)] public float reduccionMiedoPorItem = 0.3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RecolectarItem()
    {
        itemsRecolectados++;
        Debug.Log($"[Inventario] Ítem de supervivencia recolectado. Total: {itemsRecolectados}/{itemsTotales}");

        if (FearManager.Instance != null)
        {
            FearManager.Instance.ReducirVelocidadSubida(reduccionMiedoPorItem);
        }
    }
}
