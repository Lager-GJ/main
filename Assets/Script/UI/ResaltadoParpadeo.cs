using UnityEngine;

namespace Terror.UI
{
    /// <summary>
    /// Hace que un objeto brille y lata suavemente para decirle al jugador "pulsa esto".
    /// Se controla con Activar() / Desactivar(): mientras está desactivado no hace nada y
    /// el objeto conserva su color y tamaño originales. Lo usa TutorialSecuencial, pero
    /// sirve para cualquier objeto con SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResaltadoParpadeo : MonoBehaviour
    {
        [Tooltip("Color hacia el que 'brilla' el objeto en lo más alto del pulso.")]
        [SerializeField] private Color colorResalte = new Color(1f, 0.85f, 0.4f, 1f);

        [Tooltip("Pulsos por segundo. 1 = un latido por segundo.")]
        [SerializeField] private float pulsosPorSegundo = 1.2f;

        [Tooltip("Cuánto crece el objeto en lo más alto del pulso (0.08 = 8%). 0 = no cambia de tamaño.")]
        [SerializeField, Range(0f, 0.3f)] private float variacionEscala = 0.08f;

        [Tooltip("Si está marcado, empieza parpadeando. Normalmente queda sin marcar y lo activa el tutorial.")]
        [SerializeField] private bool activoAlInicio = false;

        private SpriteRenderer sprite;
        private Color colorOriginal;
        private Vector3 escalaOriginal;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            colorOriginal = sprite.color;
            escalaOriginal = transform.localScale;

            // Se ignora el checkbox del componente: se controla con "Activo Al Inicio"
            // para que un prefab no quede parpadeando por accidente.
            enabled = activoAlInicio;
        }

        private void Update()
        {
            // Tiempo sin escala: sigue latiendo aunque el tutorial pause el juego.
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulsosPorSegundo * Mathf.PI * 2f);

            sprite.color = Color.Lerp(colorOriginal, colorResalte, t);
            transform.localScale = escalaOriginal * (1f + variacionEscala * t);
        }

        private void OnDisable()
        {
            // Al apagarse devolvemos el objeto a su estado original, no a medio pulso.
            if (sprite == null) return;

            sprite.color = colorOriginal;
            transform.localScale = escalaOriginal;
        }

        public void Activar() => enabled = true;

        public void Desactivar() => enabled = false;
    }
}
