using System.Collections;
using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class TextSequenceManager : MonoBehaviour
{
    [Header("Asigna los 4 textos aquí")]
    [SerializeField] private TextMeshProUGUI text1;
    [SerializeField] private TextMeshProUGUI text2;
    [SerializeField] private TextMeshProUGUI text3;
    [SerializeField] private TextMeshProUGUI text4;

    [Header("Tiempos de la secuencia (en segundos)")]
    [SerializeField] private float initialDelay = 8f; // Espera inicial
    [SerializeField] private float displayDuration = 3f; // Tiempo que permanece visible
    [SerializeField] private float intervalDelay = 1f;  // Pausa entre textos

    private void Start()
    {
        // Aseguramos que inicien apagados por código
        SetTextActive(text1, false);
        SetTextActive(text2, false);
        SetTextActive(text3, false);
        SetTextActive(text4, false);

        // Iniciamos la secuencia
        StartCoroutine(TextSequenceRoutine());
    }

    private IEnumerator TextSequenceRoutine()
    {
        Terror.GameEvents.RaiseDialogoIniciado();

        // 1. Espera inicial
        yield return new WaitForSeconds(initialDelay);

        // 2. Primer texto
        yield return StartCoroutine(ShowAndHideText(text1));

        // Pausa
        yield return new WaitForSeconds(intervalDelay);

        // 3. Segundo texto
        yield return StartCoroutine(ShowAndHideText(text2));

        // Pausa
        yield return new WaitForSeconds(intervalDelay);

        // 4. Tercer texto
        yield return StartCoroutine(ShowAndHideText(text3));
        
        // Pausa
        yield return new WaitForSeconds(intervalDelay);

        // 5. Cuarto texto
        yield return StartCoroutine(ShowAndHideText(text4));

        Terror.GameEvents.RaiseDialogoTerminado();
    }

    private IEnumerator ShowAndHideText(TextMeshProUGUI textElement)
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(true);
            yield return new WaitForSeconds(displayDuration);
            textElement.gameObject.SetActive(false);
        }
    }

    private void SetTextActive(TextMeshProUGUI textElement, bool isActive)
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(isActive);
        }
    }
}
