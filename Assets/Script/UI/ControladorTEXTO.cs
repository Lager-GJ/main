using UnityEngine;
using TMPro; // Necesario para usar TextMeshPro

public class ControladorTEXTO : MonoBehaviour
{
    // Esta variable guardará la referencia a nuestro componente de texto
    public TextMeshProUGUI textoEnPantalla;

    // Este es el método que los botones van a ejecutar
    public void MostrarTexto(string mensaje)
    {
        textoEnPantalla.text = mensaje;
    }
}
