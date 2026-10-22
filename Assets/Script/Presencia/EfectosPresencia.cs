using System;
using System.Collections;
using TMPro;
using Terror.UI;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Lo que se ve y se siente cuando la Presencia sube de nivel (uno por fósforo
    /// consumido, GameEvents.OnNivelPresenciaCambiado). Desde 2026-10-22 es lo único
    /// que avanza en el cuarto: no hay barra de miedo.
    ///
    /// Cada nivel tiene su fila en 'niveles' (la 0 es el nivel 1): temblor de cámara,
    /// cuánto se cierra la viñeta, cuánto parpadea la llama, un golpe de sonido y un
    /// pensamiento de Luis. La respiración, los pasos/ruidos y el alcance de la luz los
    /// sigue manejando PresenciaManager; acá se suma una micro-sacudida a cada ruido.
    ///
    /// Va en el mismo GameObject que PresenciaManager (FosforoManager).
    /// </summary>
    public class EfectosPresencia : MonoBehaviour
    {
        [Serializable]
        public class NivelPresencia
        {
            [Tooltip("Temblor de cámara al subir a este nivel (unidades del mundo; 0 = nada).")]
            public float sacudida = 12f;
            public float duracionSacudida = 0.4f;
            [Tooltip("Cuánto se cierra la oscuridad desde los bordes (0 = nada, 1 = casi todo).")]
            [Range(0f, 1f)] public float vineta = 0.25f;
            [Tooltip("Late la viñeta (0 = quieta).")]
            [Range(0f, 1f)] public float pulsoVineta;
            [Tooltip("Cuánto tiembla la llama del fósforo (0 = quieta, 0.35 = baja hasta un 35%).")]
            [Range(0f, 1f)] public float parpadeoLuz;
            [Tooltip("Sonido de una vez al subir a este nivel (un portazo lejano, un foco que se rompe...).")]
            public AudioClip golpe;
            [Range(0f, 1f)] public float volumenGolpe = 0.7f;
            [Tooltip("Lo que piensa Luis (abajo, como las narraciones). Vacío = nada.")]
            [TextArea(1, 2)] public string pensamiento;
        }

        [Tooltip("Fila 0 = nivel 1 (primer fósforo consumido). El último fósforo dispara la derrota en PresenciaManager; su fila solo agrega el golpe y el temblor.")]
        [SerializeField] private NivelPresencia[] niveles =
        {
            new NivelPresencia { sacudida = 12f, duracionSacudida = 0.4f, vineta = 0.25f, pulsoVineta = 0f, parpadeoLuz = 0f, volumenGolpe = 0.35f, pensamiento = "¿Abuelita...? ¿Eres tú?" },
            new NivelPresencia { sacudida = 22f, duracionSacudida = 0.6f, vineta = 0.45f, pulsoVineta = 0.25f, parpadeoLuz = 0.15f, volumenGolpe = 0.6f, pensamiento = "Algo camina en el cuarto..." },
            new NivelPresencia { sacudida = 35f, duracionSacudida = 0.8f, vineta = 0.65f, pulsoVineta = 0.4f, parpadeoLuz = 0.35f, volumenGolpe = 0.8f, pensamiento = "Está aquí. Me está mirando." },
            new NivelPresencia { sacudida = 50f, duracionSacudida = 1.2f, vineta = 0.9f, pulsoVineta = 0f, parpadeoLuz = 0f, volumenGolpe = 0f, pensamiento = "" },
        };

        [Header("Ruidos de la Presencia")]
        [Tooltip("Temblor por cada paso/ruido, multiplicado por la cercanía (0..1).")]
        [SerializeField] private float sacudidaPorRuido = 6f;

        [Header("Referencias")]
        [Tooltip("Viñeta sobre OSCURO. Vacío = se busca en la escena.")]
        [SerializeField] private VinetaPresencia vineta;
        [SerializeField] private TMP_FontAsset fuentePensamientos;
        [SerializeField] private float segundosPensamiento = 3.5f;

        private AudioSource fuenteGolpes;

        private void Awake()
        {
            fuenteGolpes = gameObject.AddComponent<AudioSource>();
            fuenteGolpes.playOnAwake = false;
            fuenteGolpes.spatialBlend = 0f;
        }

        private void Start()
        {
            if (vineta == null) vineta = FindFirstObjectByType<VinetaPresencia>();
        }

        private void OnEnable()
        {
            GameEvents.OnNivelPresenciaCambiado += AlSubirNivel;
            GameEvents.OnRuidoPresencia += AlRuido;
        }

        private void OnDisable()
        {
            GameEvents.OnNivelPresenciaCambiado -= AlSubirNivel;
            GameEvents.OnRuidoPresencia -= AlRuido;
        }

        private void AlSubirNivel(int nivel)
        {
            if (niveles == null || niveles.Length == 0 || nivel <= 0) return;
            NivelPresencia n = niveles[Mathf.Min(nivel, niveles.Length) - 1];

            CamaraController.Instance?.Sacudir(n.sacudida, n.duracionSacudida);
            FosforoManager.Instance?.SetParpadeo(n.parpadeoLuz);
            if (vineta != null) vineta.Fijar(n.vineta, n.pulsoVineta);

            if (n.golpe != null && n.volumenGolpe > 0f)
            {
                fuenteGolpes.panStereo = UnityEngine.Random.Range(-0.6f, 0.6f);
                fuenteGolpes.PlayOneShot(n.golpe, n.volumenGolpe);
            }

            if (!string.IsNullOrEmpty(n.pensamiento))
                StartCoroutine(Pensar(n.pensamiento));
        }

        // Un instante después del temblor, para que no se pise con el mensaje del tutorial
        // que aparece justo cuando se apaga el fósforo.
        private IEnumerator Pensar(string texto)
        {
            yield return new WaitForSeconds(0.6f);
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) yield break;

            var cartel = CartelNarrativa.Instancia;
            cartel.Mostrar(this, texto, fuentePensamientos, segundosPensamiento);
            cartel.SoltarHover(this);
        }

        private void AlRuido(float cercania)
        {
            CamaraController.Instance?.Sacudir(sacudidaPorRuido * cercania, 0.25f);
        }
    }
}
