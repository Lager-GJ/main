using System.Collections;
using UnityEngine;
// 1. Añadimos la librería del nuevo sistema de inputs
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    [Header("UI del Tutorial")]
    public GameObject tutorialPanel;

    private bool isTutorialActive = false;
    private bool tutorialCompleted = false;

    void Start()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        Time.timeScale = 1f;
        StartCoroutine(TutorialTimer());
    }

    IEnumerator TutorialTimer()
    {
        yield return new WaitForSeconds(1f);

        Time.timeScale = 0f;
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        isTutorialActive = true;
    }

    void Update()
    {
        if (isTutorialActive && !tutorialCompleted)
        {
            // 2. Cambiamos la forma de leer el teclado usando el nuevo Input System
            if (Keyboard.current != null && (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame))
            {
                ResumeGame();
            }
        }
    }

    void ResumeGame()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        isTutorialActive = false;
        tutorialCompleted = true;
    }
}