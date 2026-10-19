using System;
using UnityEngine;

namespace Terror
{
    // Barra de miedo. Nunca baja. Dos modos (campo miedoPorFosforos):
    // - Por fósforos (por defecto, propuesta de Diego): sube un escalón por cada
    //   fósforo gastado (con 4: 25, 50, 75, 100) y no cambia con el tiempo.
    // - Continuo (regla 2026-10-08): a oscuras sube rápido; con fósforo, 0.5/s.
    // Al llegar a 100 dispara la derrota. La Presencia no multiplica el miedo:
    // escala por fósforos consumidos (ver PresenciaManager) y, en el último
    // fósforo, es ella la que dispara la derrota tras su silencio.
    public class FearManager : MonoBehaviour
    {
        public static FearManager Instance { get; private set; }

        [Header("Estado")]
        [Range(0f, 100f)] public float miedoActual = 0f;

        [Header("Configuracion")]
        [Tooltip("Cuanto sube el miedo por segundo cuando no hay fosforo encendido (a oscuras).")]
        public float velocidadSubidaOscuridad = 5f;

        [Tooltip("Solo en modo continuo: cuanto sube el miedo por segundo mientras hay un fosforo encendido.")]
        public float velocidadSubidaConFosforo = 0.5f;

        [Header("Modo de miedo")]
        [Tooltip("Marcado: el miedo solo sube de a escalones, uno por cada fósforo que se gasta (cuando se apaga). Al gastar el último llega a 100 y se pierde. Desmarcado: sube con el tiempo (rápido a oscuras, lento con fósforo).")]
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
            float escalon = 100f * gastados / fosforos.FosforosIniciales;

            // Nunca baja: si algo ya subió el miedo (p. ej. un hotspot del armario), el escalón no lo recorta.
            float valor = Mathf.Max(miedoActual, escalon);

            // En el último fósforo la derrota la dispara la Presencia después de su silencio.
            // La barra llega a 100 igual, pero sin cortar ese momento.
            bool ultimo = fosforos.FosforosRestantes == 0;
            SetMiedo(valor, dispararDerrota: !(ultimo && PresenciaManager.Instance != null));
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

            float velocidad = fosforoEncendido ? velocidadSubidaConFosforo : velocidadSubidaOscuridad;
            SetMiedo(miedoActual + velocidad * multiplicadorPresencia * multiplicadorItems * Time.deltaTime);
        }

        public void SetMiedo(float valor) => SetMiedo(valor, dispararDerrota: true);

        public void SetMiedo(float valor, bool dispararDerrota)
        {
            float clamped = Mathf.Clamp(valor, 0f, 100f);
            if (!Mathf.Approximately(clamped, miedoActual))
            {
                miedoActual = clamped;
                OnMiedoCambiado?.Invoke(miedoActual);
            }

            if (dispararDerrota && miedoActual >= 100f && !derrotaDisparada)
            {
                derrotaDisparada = true;
                GameStateManager.Instance?.Perder();
            }
        }
    }
}
