using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Opcional: indica a qué volumen de Opciones responde el AudioSource de este
    /// objeto. Sin este componente, todo va a "Efectos" salvo la música del menú y el
    /// ambiente del cuarto, que ya se marcan solos. Ponerlo en una música nueva.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CanalAudio : MonoBehaviour
    {
        [SerializeField] private CategoriaAudio categoria = CategoriaAudio.Musica;

        public CategoriaAudio Categoria => categoria;

        private void OnEnable()
        {
            foreach (var fuente in GetComponents<AudioSource>())
                AudioManager.Enrutar(fuente, categoria);
        }
    }
}
