using TMPro;
using Terror.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror
{
    /// <summary>
    /// Fin del cuarto 1: al ganar (GameState.Victoria) aparece una pantalla con dos opciones,
    /// volver al menú principal o seguir al siguiente nivel. Se arma sola en runtime, sobre
    /// todo lo demás, y reemplaza al PanelVictoria viejo (CondiciondeVictoria).
    /// </summary>
    public class PantallaFinNivel : MonoBehaviour
    {
        [SerializeField] private string escenaMenuPrincipal = "Intro";
        [SerializeField] private string escenaSiguienteNivel = "Cuarto2";
        [SerializeField] private string titulo = "¡Nivel completado!";
        [TextArea(1, 3)] [SerializeField] private string subtitulo = "Solo eran hilos y agujas...";
        [SerializeField] private string textoMenu = "Menú principal";
        [SerializeField] private string textoSiguiente = "Siguiente nivel";
        [SerializeField] private TMP_FontAsset fuente;

        private GameObject raiz;

        private void Start()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += AlCambiarEstado;
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= AlCambiarEstado;
            if (raiz != null) Destroy(raiz);
        }

        private void AlCambiarEstado(GameState estado)
        {
            if (estado == GameState.Victoria && raiz == null) Armar();
        }

        private void Armar()
        {
            raiz = new GameObject("PantallaFinNivel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var escalador = raiz.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            var grupo = raiz.GetComponent<CanvasGroup>();
            grupo.alpha = 0f;

            var fondo = NuevaImagen("Fondo", raiz.transform, Vector2.zero, Vector2.zero, null);
            Estirar(fondo.rectTransform);
            fondo.color = new Color(0f, 0f, 0f, 0.82f);

            var t = NuevoTexto("Titulo", raiz.transform, new Vector2(0f, 150f), new Vector2(1500f, 130f), 96f, FontStyles.Bold, fuente);
            t.text = titulo;
            var sub = NuevoTexto("Subtitulo", raiz.transform, new Vector2(0f, 40f), new Vector2(1500f, 90f), 44f, FontStyles.Italic, fuente);
            sub.text = subtitulo;
            sub.color = new Color(0.9f, 0.85f, 0.75f, 0.95f);

            Boton("BotonMenu", new Vector2(-280f, -130f), textoMenu, () => Ir(escenaMenuPrincipal));
            Boton("BotonSiguiente", new Vector2(280f, -130f), textoSiguiente, () => Ir(escenaSiguienteNivel));

            StartCoroutine(Animar(0.8f, p => grupo.alpha = p));
        }

        private void Boton(string nombre, Vector2 posicion, string texto, System.Action alClic)
        {
            var marco = NuevaImagen(nombre, raiz.transform, posicion, new Vector2(480f, 100f), MarcoDibujado.Obtener(64, 4f, 6f, 1f));
            marco.type = Image.Type.Sliced;
            marco.color = new Color(1f, 1f, 1f, 0.9f);
            BotonPlano(marco.gameObject, alClic);
            marco.gameObject.AddComponent<ResaltarAlPasar>();
            NuevoTexto("Texto", marco.transform, Vector2.zero, new Vector2(480f, 100f), 46f, FontStyles.Normal, fuente).text = texto;
        }

        private static void Ir(string escena)
        {
            // timeScale es global y sobrevive al cambio de escena.
            Time.timeScale = 1f;
            if (string.IsNullOrEmpty(escena)) return;
            SceneManager.LoadScene(escena);
        }
    }
}
