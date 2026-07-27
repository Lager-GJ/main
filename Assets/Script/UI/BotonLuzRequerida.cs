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
        private Sprite spriteOriginal;
        
        [Header("Configuración (Buró / Canasta)")]
        [Tooltip("Si es true, el botón solo se podrá presionar una vez y cambiará de sprite.")]
        public bool unSoloUso = false;
        
        [Tooltip("Eventos extra que se ejecutarán al hacer clic (ej. mostrar texto).")]
        public UnityEngine.Events.UnityEvent AlInteractuar;

        private void Awake()
        {
            // Obtenemos el componente Button que está en este mismo GameObject
            miBoton = GetComponent<Button>();

            // Guardamos el sprite original por si necesitamos restaurarlo (como en Buró/Canasta)
            Image img = GetComponent<Image>();
            if (img != null)
            {
                spriteOriginal = img.sprite;
            }

            // Si es de un solo uso, agregamos la lógica de deshabilitar al hacer clic
            if (unSoloUso)
            {
                miBoton.onClick.AddListener(DeshabilitarBoton);
            }
            
            // Disparamos nuestro evento personalizado al hacer clic (para textos, etc.)
            miBoton.onClick.AddListener(() => AlInteractuar?.Invoke());
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
            if (yaInteractuo && unSoloUso)
            {
                // Restauramos el sprite abierto al encender la luz si ya se había interactuado
                Image img = GetComponent<Image>();
                if (img != null && miBoton != null && miBoton.spriteState.pressedSprite != null)
                {
                    img.sprite = miBoton.spriteState.pressedSprite;
                }
            }
            else if (miBoton != null && !yaInteractuo)
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
            
            if (yaInteractuo && unSoloUso)
            {
                // Devolvemos el sprite al original cerrado/sin cuando se apaga la luz
                Image img = GetComponent<Image>();
                if (img != null && spriteOriginal != null)
                {
                    img.sprite = spriteOriginal;
                }
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
