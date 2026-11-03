using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Terror
{
    /// <summary>Canal de mezcla de una fuente de audio (ver AudioManager y CanalAudio).</summary>
    public enum CategoriaAudio
    {
        Efectos,
        Musica,
    }

    /// <summary>
    /// Manager de audio del shell. Se crea solo al arrancar el juego (no hace falta
    /// ponerlo en ninguna escena), sobrevive a los cambios de escena y aplica los 3
    /// volúmenes de Preferencias:
    ///  - General: AudioListener.volume (afecta a todo, sin excepción).
    ///  - Música y ambiente / Efectos: grupos del mezclador Resources/Audio/MezclaPrincipal.
    ///
    /// Cada AudioSource que aparece se manda a un grupo: Música si lo pidió un script
    /// (AudioManager.MarcarMusica) o tiene un CanalAudio en Música; Efectos todo lo demás.
    /// Las fuentes que ya tienen un grupo asignado en el Inspector no se tocan.
    ///
    /// No confundir con AudioJuegoL1 (SFX de La habitacion prohibida, scene-local).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const string RutaMezcla = "Audio/MezclaPrincipal";
        private const string ParamMusica = "VolMusica";
        private const string ParamEfectos = "VolEfectos";
        private const float IntervaloRevision = 0.5f;

        public static AudioManager Instance { get; private set; }

        public float VolMaster => Preferencias.VolMaster;
        public float VolMusica => Preferencias.VolMusica;
        public float VolSfx => Preferencias.VolSfx;

        private AudioMixer mezcla;
        private AudioMixerGroup grupoMusica;
        private AudioMixerGroup grupoEfectos;
        private float proximaRevision;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Arrancar()
        {
            if (Instance != null) return;
            var go = new GameObject("[Preferencias]");
            go.AddComponent<AudioManager>();
            go.AddComponent<AjustesImagen>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            mezcla = Resources.Load<AudioMixer>(RutaMezcla);
            if (mezcla != null)
            {
                grupoMusica = BuscarGrupo("Musica");
                grupoEfectos = BuscarGrupo("Efectos");
            }
            if (grupoMusica == null || grupoEfectos == null)
                Debug.LogWarning($"[AudioManager] No se encontró el mezclador Resources/{RutaMezcla} con los grupos Musica y Efectos: solo funcionará el volumen general.");

            Preferencias.OnCambio += AplicarVolumenes;
            SceneManager.sceneLoaded += AlCargarEscena;
            AplicarVolumenes();
        }

        private void Start()
        {
            // AudioMixer.SetFloat se ignora si se llama en el mismo Awake en que se cargó.
            AplicarVolumenes();
            EnrutarTodas();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            Preferencias.OnCambio -= AplicarVolumenes;
            SceneManager.sceneLoaded -= AlCargarEscena;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            AplicarVolumenes();
            EnrutarTodas();
        }

        private void Update()
        {
            // Muchos scripts crean su AudioSource recién cuando lo necesitan: se revisan
            // de vez en cuando las fuentes que todavía no tienen grupo.
            if (Time.unscaledTime < proximaRevision) return;
            proximaRevision = Time.unscaledTime + IntervaloRevision;
            EnrutarTodas();
        }

        private AudioMixerGroup BuscarGrupo(string nombre)
        {
            foreach (var g in mezcla.FindMatchingGroups(string.Empty))
                if (g.name == nombre) return g;
            return null;
        }

        // ---------- API para otros scripts (compatibles con la versión anterior) ----------

        public void CargarDesde(PerfilJugador perfil) => AplicarVolumenes();

        public void SetVolMaster(float valor) => Preferencias.VolMaster = valor;
        public void SetVolMusica(float valor) => Preferencias.VolMusica = valor;
        public void SetVolSfx(float valor) => Preferencias.VolSfx = valor;

        /// <summary>Manda esta fuente al canal de Música y ambiente.</summary>
        public static void MarcarMusica(AudioSource fuente) => Enrutar(fuente, CategoriaAudio.Musica);

        /// <summary>Manda esta fuente al canal elegido (si ya tenía grupo, lo reemplaza).</summary>
        public static void Enrutar(AudioSource fuente, CategoriaAudio categoria)
        {
            if (fuente == null || Instance == null) return;
            AudioMixerGroup g = categoria == CategoriaAudio.Musica ? Instance.grupoMusica : Instance.grupoEfectos;
            if (g != null) fuente.outputAudioMixerGroup = g;
        }

        /// <summary>
        /// Reemplazo de AudioSource.PlayClipAtPoint que respeta el volumen de Efectos
        /// (el de Unity crea una fuente suelta que no pasa por el mezclador).
        /// </summary>
        public static void ReproducirEfecto(AudioClip clip, float volumen = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("Efecto: " + clip.name);
            if (Camera.main != null) go.transform.position = Camera.main.transform.position;
            var fuente = go.AddComponent<AudioSource>();
            fuente.spatialBlend = 0f;
            fuente.playOnAwake = false;
            fuente.clip = clip;
            fuente.volume = Mathf.Clamp01(volumen);
            Enrutar(fuente, CategoriaAudio.Efectos);
            fuente.Play();
            Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(fuente.pitch)) + 0.1f);
        }

        // ---------- Interno ----------

        private void EnrutarTodas()
        {
            if (grupoEfectos == null) return;
            var fuentes = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var f in fuentes)
            {
                if (f.outputAudioMixerGroup != null) continue;
                var canal = f.GetComponent<CanalAudio>();
                f.outputAudioMixerGroup = canal != null && canal.Categoria == CategoriaAudio.Musica ? grupoMusica : grupoEfectos;
            }
        }

        private void AplicarVolumenes()
        {
            AudioListener.volume = Preferencias.VolMaster;
            if (mezcla == null) return;
            mezcla.SetFloat(ParamMusica, ADecibeles(Preferencias.VolMusica));
            mezcla.SetFloat(ParamEfectos, ADecibeles(Preferencias.VolSfx));
        }

        // El oído es logarítmico: 50% en el control suena "a la mitad" (-6 dB), no a -3 dB.
        private static float ADecibeles(float lineal) => lineal <= 0.0001f ? -80f : Mathf.Log10(lineal) * 20f;
    }
}
