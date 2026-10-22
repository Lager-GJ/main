using System;
using TMPro;
using Terror.UI;
using UnityEngine;
using UnityEngine.UI;
using static Terror.UI.UIRuntime;

namespace Terror
{
    /// <summary>
    /// Tarjeta para leer un objeto (la foto, la agenda, una llave): el mismo concepto que
    /// las tarjetas de Historia y del cajón del inicio. Ilustración grande (o el dibujo
    /// del objeto sobre gris con borde de tiza), texto abajo y botón &gt;. El último &gt;,
    /// el clic afuera o Escape la cierran.
    ///
    /// Mientras está abierta el fósforo no se gasta y Luis no camina. La arma
    /// InventarioUI en runtime; hay una sola por escena (Instance).
    /// </summary>
    public class TarjetaLectura : MonoBehaviour, ICapaEscape
    {
        public static TarjetaLectura Instance { get; private set; }

        private static readonly Vector2 TamanoTarjeta = new Vector2(1000f, 510f);
        private static readonly Vector2 PosicionTarjeta = new Vector2(0f, 70f);

        private CanvasGroup grupo;
        private RectTransform rectTarjeta;
        private Image fondoTarjeta;
        private Image ilustracion;
        private Image dibujo;
        private Image borde;
        private TextMeshProUGUI titulo;
        private TextMeshProUGUI texto;
        private AudioSource audioFuente;
        private AudioClip sonidoPagina;

        private string[] textos = Array.Empty<string>();
        private Sprite[] dibujos;
        private Sprite dibujoBase;
        private int indice;
        private Action alCerrar;

        public bool CapaAbierta => gameObject.activeSelf;

        public void CerrarCapa() => Cerrar();

        /// <summary>La arma InventarioUI dentro de 'raiz' (pantalla completa).</summary>
        internal static TarjetaLectura Crear(Transform raiz, TMP_FontAsset fuente, Sprite spriteSiguiente, AudioClip sonidoPagina)
        {
            var go = new GameObject("TarjetaLectura", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(raiz, false);
            Estirar((RectTransform)go.transform);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var t = go.AddComponent<TarjetaLectura>();
            t.grupo = go.GetComponent<CanvasGroup>();
            t.sonidoPagina = sonidoPagina;
            t.audioFuente = go.AddComponent<AudioSource>();
            t.audioFuente.playOnAwake = false;
            BotonPlano(go, t.Cerrar); // clic afuera: cierra

            t.rectTarjeta = NuevoRect("Tarjeta", go.transform, PosicionTarjeta, TamanoTarjeta);
            t.fondoTarjeta = t.rectTarjeta.gameObject.AddComponent<Image>();
            BotonPlano(t.rectTarjeta.gameObject, t.Avanzar);

            t.dibujo = NuevaImagen("Dibujo", t.rectTarjeta, new Vector2(0f, -20f), TamanoTarjeta * 0.72f, null);
            t.dibujo.preserveAspect = true;
            t.dibujo.raycastTarget = false;

            t.ilustracion = NuevaImagen("Ilustracion", t.rectTarjeta, Vector2.zero, TamanoTarjeta, null);
            t.ilustracion.preserveAspect = true;
            t.ilustracion.raycastTarget = false;

            t.borde = NuevaImagen("Borde", t.rectTarjeta, Vector2.zero, TamanoTarjeta + new Vector2(14f, 14f), MarcoDibujado.Obtener(128, 7f, 6f, 1f));
            t.borde.type = Image.Type.Sliced;
            t.borde.raycastTarget = false;

            t.titulo = NuevoTexto("Titulo", t.rectTarjeta, new Vector2(0f, TamanoTarjeta.y * 0.5f - 40f), new Vector2(TamanoTarjeta.x - 60f, 50f), 30f, FontStyles.Italic, fuente);
            t.titulo.color = new Color(1f, 1f, 1f, 0.8f);

            t.texto = NuevoTexto("Texto", go.transform, new Vector2(0f, PosicionTarjeta.y - TamanoTarjeta.y * 0.5f - 100f), new Vector2(1500f, 140f), 42f, FontStyles.Normal, fuente);

            Image siguiente = NuevaImagen("BotonSiguiente", go.transform, new Vector2(800f, -420f), new Vector2(130f, 110f), spriteSiguiente);
            siguiente.preserveAspect = true;
            if (spriteSiguiente == null)
            {
                siguiente.color = new Color(0f, 0f, 0f, 0f);
                NuevoTexto("Flecha", siguiente.transform, Vector2.zero, new Vector2(130f, 110f), 80f, FontStyles.Normal, fuente).text = ">";
            }
            siguiente.gameObject.AddComponent<Button>().onClick.AddListener(t.Avanzar);

            NuevoTexto("Ayuda", go.transform, new Vector2(0f, -515f), new Vector2(1500f, 44f), 24f, FontStyles.Italic, fuente).text =
                "Clic o > para seguir  ·  Esc para cerrar";

            Instance = t;
            go.SetActive(false);
            return t;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            // Si se apaga por fuera (cambio de estado, escena), no dejamos el fósforo pausado.
            FosforoManager.Instance?.ReanudarQuemado(this);
            PanelInspeccion.Instance?.QuitarCapa(this);
        }

        public void Mostrar(DefinicionObjeto def, Action alCerrarTarjeta = null)
        {
            if (def == null) return;
            Mostrar(def.nombre, def.icono, def.ilustracion, def.textos, def.dibujoPorTexto, alCerrarTarjeta);
        }

        public void Mostrar(string nombre, Sprite dibujoObjeto, Sprite arte, string[] textosTarjeta, Sprite[] dibujosPorTexto = null, Action alCerrarTarjeta = null)
        {
            textos = textosTarjeta != null && textosTarjeta.Length > 0 ? textosTarjeta : new[] { "" };
            dibujos = dibujosPorTexto;
            dibujoBase = dibujoObjeto;
            indice = 0;
            alCerrar = alCerrarTarjeta;

            // Con arte final, la ilustración es la tarjeta entera (trae su propio borde).
            bool conArte = arte != null;
            ilustracion.sprite = arte;
            ilustracion.enabled = conArte;
            fondoTarjeta.color = conArte ? new Color(0f, 0f, 0f, 0f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            borde.enabled = !conArte;
            dibujo.enabled = !conArte;
            titulo.gameObject.SetActive(!conArte);
            titulo.text = nombre;

            MostrarPagina();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            PanelInspeccion.Instance?.AbrirCapa(this);
            FosforoManager.Instance?.PausarQuemado(this);
            StartCoroutine(Animar(0.25f, p =>
            {
                grupo.alpha = p;
                rectTarjeta.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, p);
            }));
            Sonar();
        }

        private void MostrarPagina()
        {
            texto.text = indice < textos.Length ? textos[indice] : "";
            Sprite s = dibujos != null && indice < dibujos.Length && dibujos[indice] != null ? dibujos[indice] : dibujoBase;
            dibujo.sprite = s;
            dibujo.color = s != null ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
        }

        private void Avanzar()
        {
            if (!gameObject.activeSelf) return;
            if (PauseManager.Instance != null && PauseManager.Instance.EstaPausado) return;

            indice++;
            if (indice >= textos.Length)
            {
                Cerrar();
                return;
            }
            MostrarPagina();
            Sonar();
        }

        public void Cerrar()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false); // OnDisable suelta la pausa y la capa
            var accion = alCerrar;
            alCerrar = null;
            accion?.Invoke();
        }

        private void Sonar()
        {
            if (sonidoPagina != null) audioFuente.PlayOneShot(sonidoPagina, 0.6f);
        }
    }
}
