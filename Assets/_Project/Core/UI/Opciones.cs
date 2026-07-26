using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// Pantalla de Opciones: 3 sliders de volumen que se guardan al instante.
    ///
    /// En el Editor: cablear el OnValueChanged de cada Slider al Set correspondiente,
    /// y ademas arrastrar los 3 Sliders a los campos de abajo. Eso ultimo es lo que
    /// permite que al reabrir el panel los sliders muestren el valor guardado en vez
    /// de la posicion que quedo en la escena.
    /// </summary>
    public class Opciones : MonoBehaviour
    {
        [Header("Sliders (para reflejar el valor guardado al abrir)")]
        [SerializeField] private Slider sliderMaster;
        [SerializeField] private Slider sliderMusica;
        [SerializeField] private Slider sliderSfx;

        // Evita que al mover los sliders por codigo (en OnEnable) se disparen sus
        // OnValueChanged y se reguarde el perfil en cadena.
        private bool sincronizando;

        private void OnEnable()
        {
            if (AudioManager.Instance == null) return;

            sincronizando = true;
            if (sliderMaster != null) sliderMaster.value = AudioManager.Instance.VolMaster;
            if (sliderMusica != null) sliderMusica.value = AudioManager.Instance.VolMusica;
            if (sliderSfx != null) sliderSfx.value = AudioManager.Instance.VolSfx;
            sincronizando = false;
        }

        public void SetVolMaster(float valor)
        {
            if (sincronizando) return;
            if (AudioManager.Instance != null) AudioManager.Instance.SetVolMaster(valor);
            GuardarVolumenes();
        }

        public void SetVolMusica(float valor)
        {
            if (sincronizando) return;
            if (AudioManager.Instance != null) AudioManager.Instance.SetVolMusica(valor);
            GuardarVolumenes();
        }

        public void SetVolSfx(float valor)
        {
            if (sincronizando) return;
            if (AudioManager.Instance != null) AudioManager.Instance.SetVolSfx(valor);
            GuardarVolumenes();
        }

        private void GuardarVolumenes()
        {
            if (AudioManager.Instance == null) return;

            PerfilJugador perfil = SaveSystem.Cargar();
            perfil.volMaster = AudioManager.Instance.VolMaster;
            perfil.volMusica = AudioManager.Instance.VolMusica;
            perfil.volSfx = AudioManager.Instance.VolSfx;
            SaveSystem.Guardar(perfil);
        }
    }
}
