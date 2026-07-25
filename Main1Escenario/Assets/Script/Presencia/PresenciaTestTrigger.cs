using UnityEngine;

namespace Terror
{
    // Simulador temporal del sistema de fosforos de Dev A, para probar la Presencia
    // de forma aislada en la escena de prueba mientras ese sistema no existe.
    // Quitar de la escena principal cuando el sistema real de fosforos este integrado.
    public class PresenciaTestTrigger : MonoBehaviour
    {
        public KeyCode teclaSimularFosforo = KeyCode.Space;

        private void Update()
        {
            if (Input.GetKeyDown(teclaSimularFosforo))
            {
                GameEvents.RaiseFosforoEncendido();
            }
        }
    }
}
