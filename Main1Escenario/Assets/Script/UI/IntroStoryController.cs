using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroStoryController : MonoBehaviour
{
    [System.Serializable]
    public struct StoryScene
    {
        public Sprite illustration;    // La imagen de la viñeta
        [TextArea(2, 4)]
        public string textContent;     // El texto que acompaña la escena
        public AudioClip voiceOrSound; // Sonido específico si lo hay
    }

    [Header("Configuración de Datos")]
    public List<StoryScene> introScenes;

    [Header("Componentes de UI")]
    public Image displayImage;
    public TextMeshProUGUI displayText;
    public AudioSource audioSource;

    [Header("Tiempos")]
    public float fadeDuration = 1.0f;
    public float textDelay = 0.5f;
    public float sceneDuration = 4.0f;

    private void Start()
    {
        // Asegurar que empezamos en negro/vacío
        SetAlpha(displayImage, 0);
        displayText.text = "";

        StartCoroutine(PlayStoryFlow());
    }

    IEnumerator PlayStoryFlow()
    {
        foreach (StoryScene scene in introScenes)
        {
            // 1. Configurar los recursos de la escena actual
            displayImage.sprite = scene.illustration;
            displayText.text = scene.textContent;
            if (scene.voiceOrSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(scene.voiceOrSound);
            }

            // 2. Aparecer Imagen (Fade In)
            yield return StartCoroutine(FadeImage(displayImage, 0, 1, fadeDuration));

            // 3. Esperar un momento antes del texto (opcional)
            yield return new WaitForSeconds(textDelay);

            // 4. (Opcional) Aquí podrías animar el texto letra por letra. Por ahora aparece directo.
            // Mantener la escena visible por el tiempo configurado
            yield return new WaitForSeconds(sceneDuration);

            // 5. Desaparecer Imagen (Fade Out) para prepararse para la siguiente
            yield return StartCoroutine(FadeImage(displayImage, 1, 0, fadeDuration));
            displayText.text = ""; // Limpiar texto
        }

        // Aquí termina la intro, puedes cargar la escena del juego
        Debug.Log("Fin de la historia. Cargando juego...");
        // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    // Función auxiliar para desvanecer la UI
    IEnumerator FadeImage(Image targetImage, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = targetImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            targetImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        targetImage.color = color;
    }

    private void SetAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}