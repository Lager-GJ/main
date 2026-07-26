using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Voces de fondo que suenan solo mientras el fósforo está encendido, subiendo
    /// de volumen gradualmente ("llaman a los espíritus" cuanto más dura la luz
    /// prendida). Script nuevo y aparte: se suscribe a GameEvents (evento público que
    /// ya existe) sin tocar FosforoManager.cs ni Core/AudioManager.cs.
    /// </summary>
    public class VocesFosforoManager : MonoBehaviour
    {
        [SerializeField] private AudioSource fuenteVoces;
        [SerializeField] private AudioClip sonidoVocesConLuz;
        [SerializeField] private float volumenInicial = 0.1f;
        [SerializeField] private float volumenMaximo = 1f;
        [Tooltip("Segundos que tarda en llegar del volumen inicial al máximo mientras el fósforo sigue encendido.")]
        [SerializeField] private float segundosHastaVolumenMaximo = 5f;

        private float tiempoEncendido;

        private void OnEnable()
        {
            GameEvents.OnFosforoEncendido += ManejarEncendido;
            GameEvents.OnFosforoApagado += ManejarApagado;
        }

        private void OnDisable()
        {
            GameEvents.OnFosforoEncendido -= ManejarEncendido;
            GameEvents.OnFosforoApagado -= ManejarApagado;
        }

        private void Update()
        {
            if (fuenteVoces == null || !fuenteVoces.isPlaying)
                return;

            tiempoEncendido += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoEncendido / segundosHastaVolumenMaximo);
            fuenteVoces.volume = Mathf.Lerp(volumenInicial, volumenMaximo, progreso);
        }

        private void ManejarEncendido()
        {
            if (fuenteVoces == null || sonidoVocesConLuz == null)
                return;

            tiempoEncendido = 0f;
            fuenteVoces.clip = sonidoVocesConLuz;
            fuenteVoces.loop = true;
            fuenteVoces.volume = volumenInicial;
            fuenteVoces.Play();
        }

        private void ManejarApagado()
        {
            if (fuenteVoces != null)
                fuenteVoces.Stop();
        }
    }
}