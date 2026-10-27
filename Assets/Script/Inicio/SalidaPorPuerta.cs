using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror
{
    /// <summary>
    /// El final del cuarto 1 (desde 2026-10-22): con la llave de la puerta, Luis abre la
    /// puerta por la que entró. La cámara se acerca a la puerta, vuelve a entrar la luz
    /// de afuera (la misma LuzPuerta del inicio), suena la puerta, fundido a negro y se
    /// gana. Reemplaza a la lata como victoria.
    ///
    /// Se conecta al alAbrir de la Cerradura de la puerta (llave_puerta).
    /// </summary>
    public class SalidaPorPuerta : MonoBehaviour
    {
        [Tooltip("La luz de la puerta abierta (la del inicio).")]
        [SerializeField] private Light2D luzPuerta;
        [SerializeField] private float intensidadLuz = 3f;
        [SerializeField] private AudioClip sonidoAbrir;
        [SerializeField] private float duracionLuz = 1.2f;
        [SerializeField] private float duracionFundido = 1.2f;
        [Tooltip("Zoom hacia la puerta mientras se abre (fracción de lo que se ve normalmente).")]
        [SerializeField, Range(0.2f, 1f)] private float zoom = 0.7f;

        private bool saliendo;

        public void Salir()
        {
            if (saliendo) return;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Juego) return;
            saliendo = true;
            StartCoroutine(Secuencia());
        }

        private IEnumerator Secuencia()
        {
            // Lo que pase de acá en adelante no gasta fósforo ni deja mover a Luis.
            FosforoManager.Instance?.PausarQuemado(this);
            if (PanelInspeccion.Instance != null) PanelInspeccion.Instance.CierreBloqueado = true;

            var rt = transform as RectTransform;
            Vector3 centro = transform.position;
            if (rt != null)
            {
                var esquinas = new Vector3[4];
                rt.GetWorldCorners(esquinas);
                centro = (esquinas[0] + esquinas[2]) * 0.5f;
            }
            CamaraController.Instance?.Enfocar(centro, zoom, 0.8f);

            if (sonidoAbrir != null)
                AudioSource.PlayClipAtPoint(sonidoAbrir, Camera.main != null ? Camera.main.transform.position : Vector3.zero);

            if (luzPuerta != null)
            {
                luzPuerta.gameObject.SetActive(true);
                luzPuerta.intensity = 0f;
                yield return Animar(duracionLuz, p => luzPuerta.intensity = intensidadLuz * p);
            }
            else
            {
                yield return EsperarSinPausa(duracionLuz);
            }

            // Fundido a negro en un Canvas propio, debajo del cartel de narración.
            var go = new GameObject("FundidoSalida", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var negro = NuevaImagen("Negro", go.transform, Vector2.zero, Vector2.zero, null);
            Estirar(negro.rectTransform);
            negro.color = new Color(0f, 0f, 0f, 0f);
            yield return Animar(duracionFundido, p => negro.color = new Color(0f, 0f, 0f, p));

            if (PanelInspeccion.Instance != null) PanelInspeccion.Instance.CierreBloqueado = false;
            FosforoManager.Instance?.ReanudarQuemado(this);
            CamaraController.Instance?.Soltar(0.01f);
            GameStateManager.Instance?.Ganar();

            // Se levanta el negro sobre la pantalla de victoria.
            yield return Animar(0.8f, p => negro.color = new Color(0f, 0f, 0f, 1f - p));
            Destroy(go);
        }
    }
}
