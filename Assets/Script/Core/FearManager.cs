using System;
using UnityEngine;

namespace Terror
{
    // Barra de miedo: sube en oscuridad y BAJA (alivio real) mientras hay un
    // fosforo encendido -- decision de diseno confirmada 2026-07-25 (ver
    // CLAUDE.md, "Confirmed game-design decisions"). La velocidad de subida se
    // multiplica por la cercania de la Presencia (Dev C) via
    // GameEvents.OnCercaniaPresenciaCambiada — esta es la conexion que hace que
    // "todo tiene un costo" sea mecanico y no solo narrativo. Al llegar a 100
    // dispara la derrota.
    public class FearManager : MonoBehaviour
    {
        public static FearManager Instance { get; private set; }

        [Header("Estado")]
        [Range(0f, 100f)] public float miedoActual = 0f;

        [Header("Configuracion")]
        [Tooltip("Cuanto sube el miedo por segundo (antes del multiplicador de la Presencia) cuando no hay fosforo encendido.")]
        public float velocidadSubidaOscuridad = 5f;

        [Tooltip("Cuanto baja el miedo por segundo mientras hay un fosforo encendido (alivio real).")]
        public float velocidadBajadaConFosforo = 8f;

        public event Action<float> OnMiedoCambiado;

        private bool fosforoEncendido;
        private float multiplicadorPresencia = 1f;
        private bool derrotaDisparada;

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
        }

        private void OnDisable()
        {
            GameEvents.OnFosforoEncendido -= ManejarFosforoEncendido;
            GameEvents.OnFosforoApagado -= ManejarFosforoApagado;
            GameEvents.OnCercaniaPresenciaCambiada -= ManejarCercaniaPresencia;
        }

        private void ManejarFosforoEncendido() => fosforoEncendido = true;

        private void ManejarFosforoApagado() => fosforoEncendido = false;

        private void ManejarCercaniaPresencia(int nivel, float multiplicador) => multiplicadorPresencia = multiplicador;

        private void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego)
                return;

            if (fosforoEncendido)
                SetMiedo(miedoActual - velocidadBajadaConFosforo * Time.deltaTime);
            else
                SetMiedo(miedoActual + velocidadSubidaOscuridad * multiplicadorPresencia * Time.deltaTime);
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
