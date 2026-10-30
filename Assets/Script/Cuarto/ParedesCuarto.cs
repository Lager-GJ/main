using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    /// <summary>
    /// El cuarto dividido en paredes: cada pared ocupa una pantalla y la cámara queda
    /// quieta en ella. Cuando Luis camina contra un borde, la pantalla funde a negro,
    /// aparece la pared siguiente y Luis entra por el lado contrario.
    ///
    /// Las paredes están una al lado de la otra en el mundo, con espacio negro entre
    /// ellas, así que cada objeto del cuarto vive en la X de su pared y no hace falta
    /// prender ni apagar nada al cambiar. El recorrido es circular: a la derecha de la
    /// última pared vuelve la primera.
    /// </summary>
    public class ParedesCuarto : MonoBehaviour
    {
        public static ParedesCuarto Instance { get; private set; }

        /// <summary>Se dispara al cambiar de pared, con el índice de la nueva (0 = pared 1).</summary>
        public static event Action<int> OnParedCambiada;

        [Header("Paredes")]
        [Tooltip("X del centro de cada pared, en orden (a la derecha de la última vuelve la primera).")]
        [SerializeField] private float[] centrosX = { -3590f, 410f, 4410f, 8410f };
        [Tooltip("Y del centro de las paredes (donde queda la cámara).")]
        [SerializeField] private float centroY = -708f;
        [Tooltip("Ancho de una pared en unidades del mundo (el ancho de la cámara en 16:9).")]
        [SerializeField] private float anchoPared = 3170.95f;
        [Tooltip("Distancia del borde de la pantalla a la que Luis toca el límite.")]
        [SerializeField] private float margenBorde = 200f;
        [Tooltip("Al entrar a una pared, Luis aparece este tanto más adentro del límite.")]
        [SerializeField] private float entradaExtra = 40f;

        [Header("Referencias (vacías = se buscan solas)")]
        [SerializeField] private NinoController nino;
        [SerializeField] private CamaraController camara;

        [Header("Fundido")]
        [SerializeField] private float segundosOscurecer = 0.15f;
        [SerializeField] private float segundosAclarar = 0.2f;

        private int actual;
        private bool cambiando;
        private CanvasGroup fundido;

        public int ParedActual => actual;
        public bool Cambiando => cambiando;

        private void Awake()
        {
            Instance = this;
            if (nino == null) nino = FindFirstObjectByType<NinoController>();
            if (camara == null) camara = FindFirstObjectByType<CamaraController>();
        }

        private void OnEnable()
        {
            if (nino != null) nino.OnEmpujaBorde += AlEmpujarBorde;
        }

        private void OnDisable()
        {
            if (nino != null) nino.OnEmpujaBorde -= AlEmpujarBorde;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (centrosX == null || centrosX.Length == 0 || nino == null) return;

            // La pared inicial es la más cercana a donde arranca Luis.
            actual = ParedMasCercana(nino.transform.position.x);
            Aplicar(actual, true);
        }

        private int ParedMasCercana(float x)
        {
            int mejor = 0;
            for (int i = 1; i < centrosX.Length; i++)
                if (Mathf.Abs(centrosX[i] - x) < Mathf.Abs(centrosX[mejor] - x)) mejor = i;
            return mejor;
        }

        private float LimiteIzquierdo(int i) => centrosX[i] - anchoPared * 0.5f + margenBorde;
        private float LimiteDerecho(int i) => centrosX[i] + anchoPared * 0.5f - margenBorde;

        private void Aplicar(int i, bool saltar)
        {
            nino.SetLimites(LimiteIzquierdo(i), LimiteDerecho(i));
            if (camara != null) camara.FijarEn(new Vector2(centrosX[i], centroY), saltar);
        }

        private void AlEmpujarBorde(int direccion)
        {
            if (cambiando || !isActiveAndEnabled || centrosX.Length < 2) return;
            // Con la cámara acercada a un mueble o con el juego en pausa no se cambia.
            if (Time.timeScale == 0f || (camara != null && camara.Enfocando)) return;
            StartCoroutine(Cambiar(direccion));
        }

        /// <summary>Pasa a la pared de al lado: -1 = izquierda, 1 = derecha.</summary>
        private IEnumerator Cambiar(int direccion)
        {
            cambiando = true;
            nino.Bloqueado = true;

            yield return Fundir(0f, 1f, segundosOscurecer);

            int n = centrosX.Length;
            actual = ((actual + direccion) % n + n) % n;
            Aplicar(actual, true);

            // Entra por el lado contrario al que salió.
            float x = direccion > 0 ? LimiteIzquierdo(actual) + entradaExtra : LimiteDerecho(actual) - entradaExtra;
            nino.Teletransportar(x);
            OnParedCambiada?.Invoke(actual);

            nino.Bloqueado = false;
            yield return Fundir(1f, 0f, segundosAclarar);
            cambiando = false;
        }

        // Corre con tiempo real: la cámara lenta de los primeros planos no lo frena.
        private IEnumerator Fundir(float desde, float hasta, float segundos)
        {
            CanvasGroup g = Fundido();
            g.blocksRaycasts = true;
            for (float t = 0f; t < segundos; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(desde, hasta, t / segundos);
                yield return null;
            }
            g.alpha = hasta;
            g.blocksRaycasts = hasta > 0f;
        }

        // Negro a pantalla completa, encima de todo. Se arma la primera vez que hace falta.
        private CanvasGroup Fundido()
        {
            if (fundido != null) return fundido;

            var go = new GameObject("FundidoParedes", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            go.AddComponent<GraphicRaycaster>();

            var negro = new GameObject("Negro", typeof(RectTransform), typeof(Image));
            negro.transform.SetParent(go.transform, false);
            var rt = (RectTransform)negro.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            negro.GetComponent<Image>().color = Color.black;

            fundido = go.GetComponent<CanvasGroup>();
            fundido.alpha = 0f;
            fundido.blocksRaycasts = false;
            return fundido;
        }

        private void OnDrawGizmos()
        {
            if (centrosX == null) return;
            float alto = anchoPared * 9f / 16f;
            for (int i = 0; i < centrosX.Length; i++)
            {
                Gizmos.color = i == actual ? Color.yellow : new Color(1f, 0.6f, 0f, 0.6f);
                Gizmos.DrawWireCube(new Vector3(centrosX[i], centroY, 0f), new Vector3(anchoPared, alto, 0f));
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(new Vector3(LimiteIzquierdo(i), centroY - alto * 0.5f), new Vector3(LimiteIzquierdo(i), centroY + alto * 0.5f));
                Gizmos.DrawLine(new Vector3(LimiteDerecho(i), centroY - alto * 0.5f), new Vector3(LimiteDerecho(i), centroY + alto * 0.5f));
            }
        }
    }
}
