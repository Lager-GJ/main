using UnityEngine;

namespace Terror
{
    /// <summary>
    /// UI de la pantalla de carga entre escenas. No decide nada por su cuenta: la
    /// muestra y la oculta el SceneRouter.
    ///
    /// Su Canvas tiene que estar en Screen Space - Overlay, no Camera: durante el
    /// LoadSceneAsync la camara de la escena vieja se destruye, y un canvas en modo
    /// Camera desapareceria justo en mitad de la transicion.
    /// </summary>
    public class PantallaCarga : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        public void Mostrar()
        {
            if (panel != null) panel.SetActive(true);
        }

        public void Ocultar()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
