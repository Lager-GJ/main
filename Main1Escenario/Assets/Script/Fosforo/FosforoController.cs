using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Terror
{
    // Sistema de fosforo y luz. Tecla F enciende (mientras haya fosforos disponibles).
    // La luz (Light2D) es opcional: si no se asigna en el Inspector, el sistema
    // funciona igual (util para probar la logica antes de resolver la parte visual).
    public class FosforoController : MonoBehaviour
    {
        public static FosforoController Instance { get; private set; }

        [Header("Recurso")]
        public int fosforosRestantes = 8;

        [Header("Tiempos")]
        public float duracionQuemado = 5f;

        [Header("Luz (opcional, se puede asignar despues)")]
        public Light2D luz;

        public bool EstaEncendido { get; private set; }

        private float tiempoRestante;
        private Camera camaraPrincipal;

        private void Awake()
        {
            Instance = this;
            camaraPrincipal = Camera.main;

            if (luz != null)
            {
                luz.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                Encender();
            }

            if (!EstaEncendido)
            {
                return;
            }

            SeguirCursor();

            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0f)
            {
                Apagar();
            }
        }

        public void Encender()
        {
            if (EstaEncendido || fosforosRestantes <= 0)
            {
                return;
            }

            fosforosRestantes--;
            tiempoRestante = duracionQuemado;
            EstaEncendido = true;

            if (luz != null)
            {
                luz.gameObject.SetActive(true);
            }

            Debug.Log($"[Fosforo] Encendido. Quedan {fosforosRestantes} fosforos.");
            GameEvents.RaiseFosforoEncendido();
        }

        private void Apagar()
        {
            EstaEncendido = false;

            if (luz != null)
            {
                luz.gameObject.SetActive(false);
            }

            Debug.Log("[Fosforo] Se apago.");
            GameEvents.RaiseFosforoApagado();
        }

        private void SeguirCursor()
        {
            if (camaraPrincipal == null || Mouse.current == null || luz == null)
            {
                return;
            }

            Vector2 posPantalla = Mouse.current.position.ReadValue();
            Vector3 posMundo = camaraPrincipal.ScreenToWorldPoint(
                new Vector3(posPantalla.x, posPantalla.y, camaraPrincipal.nearClipPlane + 10f));
            posMundo.z = luz.transform.position.z;
            luz.transform.position = posMundo;
        }
    }
}
