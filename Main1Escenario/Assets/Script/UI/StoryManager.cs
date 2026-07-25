using UnityEngine;

public class StoryManager : MonoBehaviour
{
    [Header("Paneles de la Historia")]
    public GameObject[] storyPages; // Arrastraremos los paneles aquí

    private int currentPageIndex = 0;

    void Start()
    {
        // Apagamos todas las páginas primero
        foreach (GameObject page in storyPages)
        {
            page.SetActive(false);
        }

        // Encendemos solo la primera página si existe
        if (storyPages.Length > 0)
        {
            storyPages[0].SetActive(true);
        }
    }

    // Esta función será llamada por el Botón
    public void NextPage()
    {
        // Si aún nos quedan páginas por mostrar
        if (currentPageIndex < storyPages.Length - 1)
        {
            // Apagamos la página actual
            storyPages[currentPageIndex].SetActive(false);

            // Avanzamos al índice de la siguiente
            currentPageIndex++;

            // Encendemos la nueva página (Esto activará automáticamente el FadeText)
            storyPages[currentPageIndex].SetActive(true);
        }
        else
        {
            // Aquí pones lo que pasa al terminar la historia
            Debug.Log("¡Historia Terminada! Cargando el juego...");
            // UnityEngine.SceneManagement.SceneManager.LoadScene("NombreDeTuNivel");
        }
    }
}