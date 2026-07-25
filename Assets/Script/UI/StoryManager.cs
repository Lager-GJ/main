using UnityEngine;
using UnityEngine.SceneManagement; // ¡Esta línea es nueva y obligatoria!

public class StoryManager : MonoBehaviour
{
    [Header("Paneles de la Historia")]
    public GameObject[] storyPages;

    [Header("Configuración de Escena")]
    public string nombreDeLaEscenaDelJuego = "Nivel1"; // Escribe aquí el nombre exacto de tu escena

    private int currentPageIndex = 0;

    void Start()
    {
        foreach (GameObject page in storyPages)
        {
            page.SetActive(false);
        }

        if (storyPages.Length > 0)
        {
            storyPages[0].SetActive(true);
        }
    }

    public void NextPage()
    {
        if (currentPageIndex < storyPages.Length - 1)
        {
            storyPages[currentPageIndex].SetActive(false);
            currentPageIndex++;
            storyPages[currentPageIndex].SetActive(true);
        }
        else
        {
            // Cuando ya no hay más páginas, cargamos la siguiente escena
            CargarJuego();
        }
    }

    void CargarJuego()
    {
        // Esto carga la escena que hayas escrito en el Inspector
        SceneManager.LoadScene(nombreDeLaEscenaDelJuego);
    }
}