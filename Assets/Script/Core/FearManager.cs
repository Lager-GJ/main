using System;
using UnityEngine;

namespace Terror
{
    // Barra de miedo: sube en oscuridad, se mantiene fija (no baja) mientras
    // hay un fosforo encendido. La velocidad de subida se multiplica por la
    // cercania de la Presencia (Dev C) via GameEvents.OnCercaniaPresenciaCambiada
    // — esta es la conexion que hace que "todo tiene un costo" sea mecanico y
    // no solo narrativo. Al llegar a 100 dispara la derrota.
    public class FearManager : MonoBehaviour
    {
        public static FearManager Instance { get; private set; }

        [Header("Estado")]
        [Range(0f, 100f)] public float miedoActual = 0f;

        [Header("Configuracion")]
        [Tooltip("Cuanto sube el miedo por segundo (antes del multiplicador de la Presencia) cuando no hay fosforo encendido.")]
        public float velocidadSubidaOscuridad = 5f;

        public event Action<float> OnMiedoCambiado;

        private bool fosforoEncendido;
        private float multiplicadorPresencia = 1f;
        private bool derrotaDisparada;
        private bool pausadoPorDialogo;

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

        private void Update()
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego)
                return;

            if (pausadoPorDialogo) return;

            if (fosforoEncendido)
            {
                SetMiedo(miedoActual + velocidadSubidaOscuridad * multiplicadorPresencia * Time.deltaTime);
            }
            else
            {
                SetMiedo(miedoActual - 0.5f * Time.deltaTime);
            }
        }

        public void SetMiedo(float valor)
        {
            // El miedo ahora puede bajar hasta 0
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
