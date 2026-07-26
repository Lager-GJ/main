using System.Collections;
using UnityEngine;
using TMPro; // Necesario para usar TextMeshPro

public class FadeText : MonoBehaviour
{
    [Header("Configuración del Fade")]
    public float fadeDuration = 1.5f; // Cuánto tarda en aparecer el texto

    private TextMeshProUGUI textMesh;
    private Color originalColor;

    void Awake()
    {
        // Obtenemos el componente de texto
        textMesh = GetComponent<TextMeshProUGUI>();
        originalColor = textMesh.color;
    }

    // OnEnable se ejecuta cada vez que el objeto (o su panel padre) se activa
    void OnEnable()
    {
        StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeInRoutine()
    {
        // Empezamos con el texto totalmente transparente (Alpha = 0)
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // Calculamos el porcentaje de opacidad
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null; // Esperamos al siguiente frame
        }

        // Aseguramos que termine completamente visible
        textMesh.color = originalColor;
    }
}
