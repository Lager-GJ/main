using UnityEngine;

public class PageManager : MonoBehaviour
{
    [Tooltip("Arrastra aquí tus paneles Pagina_1, Pagina_2, etc.")]
    public GameObject[] levelPages;
    private int currentPageIndex = 0;

    void Start()
    {
        // Asegurarnos de que solo la primera página sea visible al iniciar
        ShowPage(currentPageIndex);
    }

    public void NextPage()
    {
        if (currentPageIndex < levelPages.Length - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    private void ShowPage(int index)
    {
        // Apaga todos los paneles y enciende solo el que coincide con el índice
        for (int i = 0; i < levelPages.Length; i++)
        {
            levelPages[i].SetActive(i == index);
        }
    }
}