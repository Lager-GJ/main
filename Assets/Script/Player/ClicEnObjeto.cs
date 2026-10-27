using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detecta el clic del ratón sobre el Collider2D de este objeto y avisa a su
/// ObjetoInteractivo u ObjetoRecogible. Existe porque OnMouseDown NO se dispara cuando el
/// proyecto usa solo el Input System nuevo (Active Input Handling = Input System Package):
/// aquí se lee el ratón con Mouse.current y se comprueba con Collider2D.OverlapPoint.
/// No necesita nada en la escena, así que sirve dentro de un prefab.
/// Es como un "addEventListener('click')" escrito a mano: cada frame mira si hubo clic
/// y si cayó dentro del área del objeto.
/// </summary>
public class ClicEnObjeto : MonoBehaviour
{
    // Frame en que se cerró la última ventana de objeto. Se comparte entre todos los
    // objetos: el clic en "Volver" no debe caer también sobre un objeto que quede detrás.
    private static int frameUltimoCierre = -10;

    private Collider2D area;
    private ObjetoInteractivo interactivo;
    private ObjetoRecogible recogible;
    private Camera camara;

    private void Awake()
    {
        area = GetComponent<Collider2D>();
        interactivo = GetComponent<ObjetoInteractivo>();
        recogible = GetComponent<ObjetoRecogible>();

        if (area == null)
            Debug.LogWarning($"[ClicEnObjeto] {name} no tiene Collider2D: no se podrá hacer clic en él.", this);
    }

    private void OnEnable() => ObjetoInteractivo.OnInspeccionCerrada += RegistrarCierre;

    private void OnDisable() => ObjetoInteractivo.OnInspeccionCerrada -= RegistrarCierre;

    private static void RegistrarCierre() => frameUltimoCierre = Time.frameCount;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (area == null) return;

        // Juego pausado (menú de pausa, tutorial de movimiento): no se interactúa.
        if (Time.timeScale == 0f) return;

        // Ese clic cerró una ventana de objeto en este mismo frame o el anterior.
        if (Time.frameCount <= frameUltimoCierre + 1) return;

        // Ya hay una ventana de objeto abierta (el fósforo está pausado por inspección).
        var fosforo = FosforoManager.Instance;
        if (fosforo != null && fosforo.PausadoPorInspeccion) return;

        // Vista de primer plano del armario abierta.
        if (Terror.PanelInspeccion.Instance != null && Terror.PanelInspeccion.Instance.EstaAbierto) return;

        if (camara == null) camara = Camera.main;
        if (camara == null) return;

        Vector2 punto = camara.ScreenToWorldPoint(mouse.position.ReadValue());
        if (!area.OverlapPoint(punto)) return;

        // ObjetoInteractivo comprueba solo que haya fósforo encendido; ObjetoRecogible no.
        if (interactivo != null) interactivo.InteractuarManual();
        if (recogible != null) recogible.Recoger();
    }
}
