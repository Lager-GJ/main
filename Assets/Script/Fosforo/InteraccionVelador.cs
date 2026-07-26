using UnityEngine;

public class InteraccionVelador : MonoBehaviour
{
    [Tooltip("La imagen grande de la caja que aparece en el centro")]
    public GameObject imagenCajaLaChispa;

    [Tooltip("El objeto padre que agrupa los 3 fósforos en el Canvas")]
    public GameObject fosforosInventario;

    [Tooltip("Luz del velador (Spot Light)")]
    public GameObject luzVelador;

    private bool yaInteractuo = false;

    private void Start()
    {
        // Nos aseguramos que la luz arranque encendida
        if (luzVelador != null)
        {
            luzVelador.SetActive(true);
        }
    }

    // Este método se ejecuta cuando el jugador hace clic en el velador
    public void AbrirCajaDeFosforos()
    {
        if (yaInteractuo) return;
        yaInteractuo = true;

        // Si es un botón de UI, lo deshabilitamos
        var btn = GetComponent<UnityEngine.UI.Button>();
        if (btn != null) btn.interactable = false;

        // Si usa colliders para el click, los apagamos
        var col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;
        
        var col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = false;

        // 1. Muestras la caja grande
        imagenCajaLaChispa.SetActive(true);

        // 2. ACTIVAS LOS FÓSFOROS EN LA ESQUINA SUPERIOR IZQUIERDA
        fosforosInventario.SetActive(true);

        // 3. APAGAS LA LUZ DEL VELADOR
        if (luzVelador != null)
        {
            luzVelador.SetActive(false);
        }
    }
}