using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Terror
{
    /// <summary>
    /// Brillo y contraste de Opciones → Imagen. Va junto al AudioManager en el objeto
    /// persistente [Preferencias]. Usa un Volume global propio (Color Adjustments, de
    /// prioridad alta) y prende el post-procesado de las cámaras solo cuando el jugador
    /// movió algo de 0: con los valores de fábrica el juego se ve y rinde igual que antes.
    ///
    /// La UI en Screen Space Overlay (menús, textos, inventario) no se ve afectada, así
    /// que los textos siguen legibles aunque se oscurezca mucho la imagen.
    /// </summary>
    public class AjustesImagen : MonoBehaviour
    {
        [Tooltip("Exposición (EV) con el brillo al máximo / mínimo.")]
        [SerializeField] private float rangoExposicion = 1.2f;
        [Tooltip("Contraste de Color Adjustments con el control al máximo / mínimo (-100..100).")]
        [SerializeField] private float rangoContraste = 45f;

        private Volume volumen;
        private VolumeProfile perfil;
        private ColorAdjustments ajustes;

        private void Awake()
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "AjustesImagen (runtime)";
            ajustes = perfil.Add<ColorAdjustments>(true);

            volumen = gameObject.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 1000f;
            volumen.sharedProfile = perfil;

            Preferencias.OnCambio += Aplicar;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void Start() => Aplicar();

        private void OnDestroy()
        {
            Preferencias.OnCambio -= Aplicar;
            SceneManager.sceneLoaded -= AlCargarEscena;
            if (perfil != null) Destroy(perfil);
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo) => Aplicar();

        private void Aplicar()
        {
            if (ajustes == null) return;

            float brillo = Preferencias.Brillo;
            float contraste = Preferencias.Contraste;
            bool hayBrillo = !Mathf.Approximately(brillo, 0f);
            bool hayContraste = !Mathf.Approximately(contraste, 0f);

            // overrideState en false = no pisa lo que ya tenga el perfil de la escena.
            ajustes.postExposure.overrideState = hayBrillo;
            ajustes.postExposure.value = brillo * rangoExposicion;
            ajustes.contrast.overrideState = hayContraste;
            ajustes.contrast.value = contraste * rangoContraste;

            if (hayBrillo || hayContraste)
                PrenderPostProcesado();
        }

        private static void PrenderPostProcesado()
        {
            foreach (var cam in Camera.allCameras)
            {
                var datos = cam.GetUniversalAdditionalCameraData();
                if (datos != null && !datos.renderPostProcessing) datos.renderPostProcessing = true;
            }
        }
    }
}
