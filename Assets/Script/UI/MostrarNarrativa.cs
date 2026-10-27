using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Terror.UI
{
    /// <summary>
    /// Escucha cuando el jugador interactúa con un ObjetoInteractivo y muestra 
    /// su texto narrativo (Descripción) en la pantalla.
    /// Se cierra con un botón de la UI conectado a Cerrar(): ya no con clic en cualquier
    /// parte ni con Escape, porque ese clic podía cerrar el panel en el mismo frame que lo abría.
    /// </summary>
    public class MostrarNarrativa : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("El panel oscuro o fondo que contiene el texto (GameObject).")]
        public GameObject panelNarrativa;
        
        [Tooltip("El componente de texto donde se escribirá la historia.")]
        public TextMeshProUGUI textoNarrativa;

        [Tooltip("Opcional: Image donde se ve el objeto ampliado (toma el sprite y el color del objeto pulsado).")]
        public Image imagenObjeto;

        [Tooltip("Opcional: título de la ventana (el Nombre Objeto del ObjetoInteractivo).")]
        public TextMeshProUGUI textoTitulo;

        private ObjetoInteractivo objetoActual;

        private void OnEnable()
        {
            // Nos suscribimos al evento del ObjetoInteractivo
            ObjetoInteractivo.OnObjetoInspeccionado += AbrirNarrativa;
            FosforoManager.OnFosforoApagado += AlApagarseElFosforo;
        }

        private void OnDisable()
        {
            ObjetoInteractivo.OnObjetoInspeccionado -= AbrirNarrativa;
            FosforoManager.OnFosforoApagado -= AlApagarseElFosforo;
        }

        // Igual que el armario y el cajón: sin luz no se sigue leyendo (si se apaga con Q).
        private void AlApagarseElFosforo()
        {
            if (panelNarrativa != null && panelNarrativa.activeSelf)
                Cerrar();
        }

        private void Start()
        {
            // Asegurarse de que inicia oculto
            if (panelNarrativa != null)
                panelNarrativa.SetActive(false);
        }

        private void AbrirNarrativa(ObjetoInteractivo objeto)
        {
            // Sin descripción no hay nada que mostrar, pero hay que reanudar el fósforo:
            // ObjetoInteractivo ya lo pausó y solo CerrarInspeccion() lo reanuda.
            if (string.IsNullOrEmpty(objeto.Descripcion))
            {
                objeto.CerrarInspeccion();
                return;
            }

            objetoActual = objeto;

            if (textoNarrativa != null)
                textoNarrativa.text = objeto.Descripcion;

            if (textoTitulo != null)
                textoTitulo.text = objeto.NombreObjeto;

            MostrarImagenAmpliada(objeto);

            if (panelNarrativa != null)
                panelNarrativa.SetActive(true);
        }

        private void MostrarImagenAmpliada(ObjetoInteractivo objeto)
        {
            if (imagenObjeto == null) return;

            // Se toma el sprite del propio objeto: cuando llegue el arte final se verá
            // ampliado sin configurar nada más. Los objetos de UI no tienen SpriteRenderer.
            var sr = objeto.GetComponentInChildren<SpriteRenderer>();
            imagenObjeto.enabled = sr != null && sr.sprite != null;
            if (!imagenObjeto.enabled) return;

            imagenObjeto.sprite = sr.sprite;
            imagenObjeto.color = sr.color;
            imagenObjeto.preserveAspect = true;
        }

        public void Cerrar()
        {
            if (panelNarrativa != null)
                panelNarrativa.SetActive(false);

            if (objetoActual != null)
            {
                // Esto le avisa al objeto que ya dejamos de leer,
                // lo que automáticamente reanuda el consumo del fósforo.
                objetoActual.CerrarInspeccion(); 
                objetoActual = null;
            }
        }
    }
}
