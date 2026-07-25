using UnityEngine;

namespace Terror
{
    // Marca un objeto de la escena como clickeable/inspeccionable.
    // Requiere un Collider2D para que el raycast de InteraccionClick lo detecte.
    [RequireComponent(typeof(Collider2D))]
    public class ObjetoInteractable : MonoBehaviour
    {
        public string nombreObjeto = "Objeto sin nombre";

        public void Inspeccionar()
        {
            Debug.Log($"[Interaccion] Inspeccionando: {nombreObjeto}");
        }
    }
}
