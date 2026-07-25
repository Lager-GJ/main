using UnityEngine;

namespace Terror
{
    // Hooks de audio para los eventos reales del equipo: fosforo encendido,
    // acercamiento de la Presencia, miedo alto, victoria, derrota. Los clips
    // se asignan en el Inspector cuando lleguen los assets finales (Diseñador C).
    // Nota: se suscribe en Start() (no OnEnable) para dar tiempo a que
    // GameStateManager/FearManager ya hayan corrido su Awake().
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Clips (asignar cuando lleguen los assets finales)")]
        public AudioClip sfxFosforoEncendido;
        public AudioClip sfxPresenciaCerca;
        public AudioClip sfxMiedoAlto;
        public AudioClip sfxVictoria;
        public AudioClip sfxDerrota;

        [Header("Umbrales")]
        [Range(0f, 100f)] public float umbralMiedoAlto = 80f;
        [Tooltip("Nivel de Presencia (GameEvents.OnCercaniaPresenciaCambiada) a partir del cual se considera 'cerca'.")]
        public int umbralNivelPresenciaCerca = 3;

        private AudioSource audioSource;
        private bool miedoAltoDisparado;
        private bool presenciaCercaDisparada;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            GameEvents.OnFosforoEncendido += HandleFosforoEncendido;
            GameEvents.OnCercaniaPresenciaCambiada += HandlePresencia;
            if (FearManager.Instance != null) FearManager.Instance.OnMiedoCambiado += HandleMiedo;
            if (GameStateManager.Instance != null) GameStateManager.Instance.OnStateChanged += HandleEstado;
        }

        private void OnDestroy()
        {
            GameEvents.OnFosforoEncendido -= HandleFosforoEncendido;
            GameEvents.OnCercaniaPresenciaCambiada -= HandlePresencia;
            if (FearManager.Instance != null) FearManager.Instance.OnMiedoCambiado -= HandleMiedo;
            if (GameStateManager.Instance != null) GameStateManager.Instance.OnStateChanged -= HandleEstado;
        }

        private void HandleFosforoEncendido() => Reproducir(sfxFosforoEncendido);

        private void HandlePresencia(int nivel, float multiplicador)
        {
            if (nivel >= umbralNivelPresenciaCerca && !presenciaCercaDisparada)
            {
                presenciaCercaDisparada = true;
                Reproducir(sfxPresenciaCerca);
            }
            else if (nivel < umbralNivelPresenciaCerca)
            {
                presenciaCercaDisparada = false;
            }
        }

        private void HandleMiedo(float miedo)
        {
            if (miedo >= umbralMiedoAlto && !miedoAltoDisparado)
            {
                miedoAltoDisparado = true;
                Reproducir(sfxMiedoAlto);
            }
            else if (miedo < umbralMiedoAlto)
            {
                miedoAltoDisparado = false;
            }
        }

        private void HandleEstado(GameState estado)
        {
            if (estado == GameState.Victoria) Reproducir(sfxVictoria);
            else if (estado == GameState.Derrota) Reproducir(sfxDerrota);
        }

        private void Reproducir(AudioClip clip)
        {
            if (clip == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}
