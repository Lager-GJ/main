using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Manager de audio del shell: vive en 00_Boot, sobrevive a los cambios de escena
    /// y es el dueño de los 3 volumenes del jugador.
    ///
    /// No confundir con AudioJuegoL1 (en Leyendas/L1_CajaFosforos/Scripts/): aquel
    /// reproduce los SFX de la Leyenda 1 y es scene-local. Este solo guarda y aplica
    /// volumenes, y persiste. La mezcla real por canal (musica vs sfx con un
    /// AudioMixer) es Semana 6, cuando existan los clips.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public float VolMaster { get; private set; } = 1f;
        public float VolMusica { get; private set; } = 1f;
        public float VolSfx { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void CargarDesde(PerfilJugador perfil)
        {
            if (perfil == null) return;

            VolMaster = perfil.volMaster;
            VolMusica = perfil.volMusica;
            VolSfx = perfil.volSfx;
            AplicarVolumenes();
        }

        public void SetVolMaster(float valor)
        {
            VolMaster = Mathf.Clamp01(valor);
            AplicarVolumenes();
        }

        public void SetVolMusica(float valor)
        {
            VolMusica = Mathf.Clamp01(valor);
            AplicarVolumenes();
        }

        public void SetVolSfx(float valor)
        {
            VolSfx = Mathf.Clamp01(valor);
            AplicarVolumenes();
        }

        private void AplicarVolumenes()
        {
            // El master via AudioListener alcanza para el MVP. Musica y Sfx se guardan
            // y persisten, pero todavia no tienen canal propio que aplicarles — eso
            // llega con el AudioMixer de la Semana 6.
            AudioListener.volume = VolMaster;
        }
    }
}
