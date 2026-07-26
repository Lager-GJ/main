using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Este script hace que un botón de UI solo sea interactuable cuando 
    /// el fósforo (luz) está encendido.
    /// Especialmente diseñado para usarse en el botón ArmarioB u otros similares.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BotonLuzRequerida : MonoBehaviour
    {
        private Button miBoton;
        private bool yaInteractuo = false;
        private bool esUnSoloUso = false;

        private void Awake()
        {
            // Obtenemos el componente Button que está en este mismo GameObject
            miBoton = GetComponent<Button>();

            // Validamos si es Buro o Canasta para aplicar la regla de un solo uso
            if (gameObject.name == "Buro" || gameObject.name == "Canasta")
            {
                esUnSoloUso = true;
                miBoton.onClick.AddListener(DeshabilitarBoton);
            }
        }

        private void OnEnable()
        {
            // Nos suscribimos a los eventos del fósforo para saber cuándo se prende o se apaga
            FosforoManager.OnFosforoEncendido += PermitirInteraccion;
            FosforoManager.OnFosforoApagado += BloquearInteraccion;
        }

        private void OnDisable()
        {
            // Nos desuscribimos para evitar errores si este botón es destruido
            FosforoManager.OnFosforoEncendido -= PermitirInteraccion;
            FosforoManager.OnFosforoApagado -= BloquearInteraccion;
        }

        private void Start()
        {
            // Al iniciar, verificamos si la luz ya está encendida para configurar el estado correcto
            if (FosforoManager.Instance != null)
            {
                miBoton.interactable = FosforoManager.Instance.Encendido;
            }
            else
            {
                // Si por alguna razón no hay FosforoManager, por seguridad lo bloqueamos
                miBoton.interactable = false;
            }
        }

        private void PermitirInteraccion()
        {
            if (miBoton != null && !yaInteractuo)
            {
                miBoton.interactable = true;
            }
        }

        private void BloquearInteraccion()
        {
            if (miBoton != null)
            {
                miBoton.interactable = false;
            }
        }

        private void DeshabilitarBoton()
        {
            if (yaInteractuo) return;
            yaInteractuo = true;
            
            // Fijar el sprite abierto permanentemente (sprite presionado/seleccionado)
            Image img = GetComponent<Image>();
            if (img != null && miBoton.spriteState.pressedSprite != null)
            {
                img.sprite = miBoton.spriteState.pressedSprite;
            }
            
            BloquearInteraccion();
            
            // Asegurar que no se oscurezca al estar deshabilitado permanentemente
            ColorBlock cb = miBoton.colors;
            cb.disabledColor = Color.white;
            miBoton.colors = cb;
        }
    }
}
