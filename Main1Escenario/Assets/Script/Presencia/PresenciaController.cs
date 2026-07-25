using UnityEngine;

namespace Terror
{
    // Sistema de "La Presencia": la mecanica de costo del reto.
    // Escucha GameEvents.OnFosforoEncendido (Dev A) y sube un contador de cercania.
    // Publica GameEvents.OnCercaniaPresenciaCambiada para que Dev B acelere la barra de miedo.
    public class PresenciaController : MonoBehaviour
    {
        [Header("Niveles de cercania")]
        [Tooltip("Nivel maximo de cercania (0 = lejos, nivelMaximo = critico)")]
        public int nivelMaximo = 5;

        [Tooltip("Multiplicador de velocidad de la barra de miedo por nivel, indice 0..nivelMaximo")]
        public float[] multiplicadorMiedoPorNivel = { 1f, 1.2f, 1.5f, 2f, 2.5f, 3.5f };

        [Header("Avance")]
        [Range(0f, 1f)]
        [Tooltip("Probabilidad de subir un nivel al encender un fosforo. 1 = siempre sube")]
        public float probabilidadDeAvance = 1f;

        public int NivelActual { get; private set; }

        private void OnEnable()
        {
            GameEvents.OnFosforoEncendido += ManejarFosforoEncendido;
        }

        private void OnDisable()
        {
            GameEvents.OnFosforoEncendido -= ManejarFosforoEncendido;
        }

        private void Start()
        {
            NotificarCercaniaActual();
        }

        private void ManejarFosforoEncendido()
        {
            if (NivelActual >= nivelMaximo)
            {
                return;
            }

            if (Random.value <= probabilidadDeAvance)
            {
                NivelActual++;
                NotificarCercaniaActual();
            }
        }

        private void NotificarCercaniaActual()
        {
            int indice = Mathf.Clamp(NivelActual, 0, multiplicadorMiedoPorNivel.Length - 1);
            float multiplicador = multiplicadorMiedoPorNivel[indice];

            GameEvents.RaiseCercaniaPresenciaCambiada(NivelActual, multiplicador);
            Debug.Log($"[Presencia] Nivel {NivelActual}/{nivelMaximo} - multiplicador de miedo x{multiplicador}");
        }

        public void ReiniciarPresencia()
        {
            NivelActual = 0;
            NotificarCercaniaActual();
        }
    }
}
