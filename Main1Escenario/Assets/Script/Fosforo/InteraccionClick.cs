using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror
{
    // Regla de interaccion: click sobre un objeto solo es valido si hay un
    // fosforo encendido (FosforoController.Instance.EstaEncendido).
    public class InteraccionClick : MonoBehaviour
    {
        private Camera camaraPrincipal;

        private void Awake()
        {
            camaraPrincipal = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (FosforoController.Instance == null || !FosforoController.Instance.EstaEncendido)
            {
                Debug.Log("[Interaccion] Necesitas un fosforo encendido para interactuar.");
                return;
            }

            Vector2 posMundo = camaraPrincipal.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D impacto = Physics2D.Raycast(posMundo, Vector2.zero);

            if (impacto.collider != null && impacto.collider.TryGetComponent(out ObjetoInteractable objeto))
            {
                objeto.Inspeccionar();
            }
            else
            {
                Debug.Log("[Interaccion] Click en vacio.");
            }
        }
    }
}
