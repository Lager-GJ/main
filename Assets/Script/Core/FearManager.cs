using System;
using UnityEngine;

namespace Terror
{
    // Barra de miedo (regla acordada 2026-10-08): nunca baja. A oscuras sube
    // rapido; con un fosforo encendido sigue subiendo, pero lento (0.5/s).
    // Al llegar a 100 dispara la derrota. La Presencia ya no multiplica el miedo:
    // escala por fosforos consumidos (ver PresenciaManager).
    public class FearManager : MonoBehaviour
    {
        public static FearManager Instance { get; private set; }

        [Header("Estado")]
        [Range(0f, 100f)] public float miedoActual = 0f;

        [Header("Configuracion")]
        [Tooltip("Cuanto sube el miedo por segundo cuando no hay fosforo encendido (a oscuras).")]
        public float velocidadSubidaOscuridad = 5f;

        [Tooltip("Cuanto sube el miedo por segundo mientras hay un fosforo encendido.")]
        public float velocidadSubidaConFosforo = 0.5f;

        public event Action<float> OnMiedoCambiado;

        private bool fosforoEncendido;
        private float multiplicadorPresencia = 1f;
        private bool derrotaDisparada;
        private bool pausadoPorDialogo;
        private float multiplicadorItems = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnFosforoEncendido += ManejarFosforoEncendido;
            GameEvents.OnFosforoApagado += ManejarFosforoApagado;
            GameEvents.OnCercaniaPresenciaCambiada += ManejarCercaniaPresencia;
            GameEvents.OnDialogoIniciado += ManejarDialogoIniciado;
            GameEvents.OnDialogoTerminado += ManejarDialogoTerminado;
        }

        private void OnDisable()
        {
            GameEvents.OnFosforoEncendido -= ManejarFosforoEncendido;
            GameEvents.OnFosforoApagado -= ManejarFosforoApagado;
            GameEvents.OnCercaniaPresenciaCambiada -= ManejarCercaniaPresencia;
            GameEvents.OnDialogoIniciado -= ManejarDialogoIniciado;
            GameEvents.OnDialogoTerminado -= ManejarDialogoTerminado;
        }

        private void ManejarFosforoEncendido() => fosforoEncendido = true;

        private void ManejarFosforoApagado() => fosforoEncendido = false;

        private void ManejarCercaniaPresencia(int nivel, float multiplicador) => multiplicadorPresencia = multiplicador;

        private void ManejarDialogoIniciado() => pausadoPorDialogo = true;
        
        private void ManejarDialogoTerminado() => pausadoPorDialogo = false;

        public void ReducirVelocidadSubida(float reduccion)
        {
            multiplicadorItems *= (1f - reduccion);
            Debug.Log($"[FearManager] Velocidad de miedo reducida. Multiplicador actual: {multiplicadorItems:F2}");
        }

        private void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego)
                return;

            if (pausadoPorDialogo) return;

            float velocidad = fosforoEncendido ? velocidadSubidaConFosforo : velocidadSubidaOscuridad;
            SetMiedo(miedoActual + velocidad * multiplicadorPresencia * multiplicadorItems * Time.deltaTime);
        }

        public void SetMiedo(float valor)
        {
            float clamped = Mathf.Clamp(valor, 0f, 100f);
            if (!Mathf.Approximately(clamped, miedoActual))
            {
                miedoActual = clamped;
                OnMiedoCambiado?.Invoke(miedoActual);
            }

            if (miedoActual >= 100f && !derrotaDisparada)
            {
                derrotaDisparada = true;
                GameStateManager.Instance?.Perder();
            }
        }
    }
}
