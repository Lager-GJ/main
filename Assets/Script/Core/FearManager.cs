using System;
using UnityEngine;

namespace Terror
{
    // Barra de miedo: sube en oscuridad, BAJA (alivio real) mientras hay un
    // fosforo encendido -- decision de diseno confirmada 2026-07-25 (ver
    // CLAUDE.md, "Confirmed game-design decisions": "lit match gives real
    // relief -- fear actively goes down while a match burns"). La velocidad de
    // subida se multiplica por la cercania de la Presencia (Dev C) via
    // GameEvents.OnCercaniaPresenciaCambiada — esta es la conexion que hace que
    // "todo tiene un costo" sea mecanico y no solo narrativo. Al llegar a 100
    // dispara la derrota.
    //
    // Fix 2026-07-28: la direccion estaba invertida (subia con el fosforo
    // encendido, bajaba en oscuridad) -- se corrige el sentido sin tocar lo
    // demas (pausa por dialogo, multiplicador de items siguen igual).
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

            if (fosforoEncendido)
            {
                // Alivio real: encender un fosforo es un refugio momentaneo.
                SetMiedo(miedoActual - velocidadBajadaConFosforo * Time.deltaTime);
            }
            else
            {
                // En oscuridad el miedo sube, mas rapido cuanto mas cerca esta
                // la Presencia (multiplicadorPresencia) y menos si hay items que
                // lo mitigan (multiplicadorItems).
                SetMiedo(miedoActual + velocidadSubidaOscuridad * multiplicadorPresencia * multiplicadorItems * Time.deltaTime);
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
