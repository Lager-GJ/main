using UnityEngine;
using UnityEngine.UI;

namespace Terror
{
    [RequireComponent(typeof(Image))]
    public class EfectoOscurecimientoMiedo : MonoBehaviour
    {
        private Image imagenOscura;
        
        [Tooltip("Alpha máximo que alcanzará la imagen cuando el miedo llegue a 100 (1.0 = totalmente negro).")]
        [Range(0f, 1f)]
        public float alphaMaximo = 0.85f;

        private void Awake()
        {
            imagenOscura = GetComponent<Image>();
            SetAlpha(0f);
        }

        private void Start()
        {
            if (FearManager.Instance != null)
            {
                FearManager.Instance.OnMiedoCambiado += ActualizarOscurecimiento;
                ActualizarOscurecimiento(FearManager.Instance.miedoActual);
            }
            else
            {
                Debug.LogWarning("[EfectoOscurecimientoMiedo] No se encontró FearManager.Instance en la escena.");
            }
        }

        private void OnDestroy()
        {
            if (FearManager.Instance != null)
            {
                FearManager.Instance.OnMiedoCambiado -= ActualizarOscurecimiento;
            }
        }

        private void ActualizarOscurecimiento(float miedoActual)
        {
            float porcentaje = Mathf.Clamp01(miedoActual / 100f);
            float alphaCalculado = porcentaje * alphaMaximo;
            SetAlpha(alphaCalculado);
        }

        private void SetAlpha(float alpha)
        {
            if (imagenOscura != null)
            {
                Color c = imagenOscura.color;
                c.a = alpha;
                imagenOscura.color = c;
            }
        }
    }
}
