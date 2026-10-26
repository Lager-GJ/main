using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Barra de luz del fósforo: se vacía a medida que el fósforo encendido se consume.
    /// Escucha FosforoManager.OnProgresoFosforoCambiado (1 = recién encendido, 0 = apagado),
    /// así que no necesita una referencia a FosforoManager ni leer nada en Update().
    /// Mientras el jugador lee un objeto el fósforo está pausado, no llegan eventos nuevos
    /// y la barra se queda quieta, igual que la luz.
    /// Mismo patrón que InventarioFosforo: la lógica vive en FosforoManager y esto solo la dibuja.
    /// </summary>
    public class BarraLuzFosforo : MonoBehaviour
    {
        [Tooltip("Image del RELLENO (no del fondo). Debe ser de tipo Filled (horizontal o vertical, como prefieras).")]
        [SerializeField] private Image relleno;

        [Tooltip("Color del relleno cuando el fósforo recién se encendió.")]
        [SerializeField] private Color colorLleno = new Color(1f, 0.85f, 0.4f, 1f);

        [Tooltip("Color del relleno cuando está por apagarse. Pon el mismo que arriba si quieres un color fijo.")]
        [SerializeField] private Color colorPoco = new Color(0.8f, 0.2f, 0.1f, 1f);

        private void OnEnable()
        {
            FosforoManager.OnProgresoFosforoCambiado += Actualizar;

            // Si la barra se activa con un fósforo ya encendido, nos ponemos al día.
            // Si FosforoManager aún no existe (orden de Awake), arrancamos vacía y listo.
            float inicial = FosforoManager.Instance != null ? FosforoManager.Instance.ProgresoRestante : 0f;
            Actualizar(inicial);
        }

        private void OnDisable()
        {
            FosforoManager.OnProgresoFosforoCambiado -= Actualizar;
        }

        private void Actualizar(float progreso)
        {
            if (relleno == null) return;

            relleno.fillAmount = progreso;
            relleno.color = Color.Lerp(colorPoco, colorLleno, progreso);
        }
    }
}
