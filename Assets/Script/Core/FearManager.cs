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

        [Header("Modo de miedo")]
        [Tooltip("Marcado: el miedo solo sube de a escalones, uno por cada fósforo que se gasta (cuando se apaga). Al gastar el último llega a 100 y se pierde. Desmarcado: comportamiento anterior, que cambia con el tiempo.")]
        public bool miedoPorFosforos = true;

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

        private void ManejarFosforoApagado()
        {
            fosforoEncendido = false;

            if (miedoPorFosforos) SubirMiedoPorFosforoGastado();
        }

        // Un escalón por fósforo gastado: con 4 fósforos son 25, 50, 75 y 100.
        // Se cuenta al APAGARSE y no al encenderse, para que quien esté cerca de ganar
        // pueda usar el último fósforo completo sin perder apenas lo prende.
        private void SubirMiedoPorFosforoGastado()
        {
            var fosforos = FosforoManager.Instance;
            if (fosforos == null || fosforos.FosforosIniciales <= 0) return;

            // Si ya se ganó o se perdió (p. ej. el fósforo se apaga con la pantalla de victoria), no tocamos nada.
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego)
                return;

            int gastados = fosforos.FosforosIniciales - fosforos.FosforosRestantes;
            SetMiedo(100f * gastados / fosforos.FosforosIniciales);
        }

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

            // En este modo el miedo no cambia con el tiempo: solo sube al gastarse un fósforo.
            if (miedoPorFosforos) return;

            if (fosforoEncendido)
            {
                SetMiedo(miedoActual + velocidadSubidaOscuridad * multiplicadorPresencia * multiplicadorItems * Time.deltaTime);
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
