using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ManagerTutorial : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Button botonObjetivo; // El botón que obligatoriamente deben presionar
    public GameObject panelTutorial; // El panel u oscurecimiento visual

    private bool tutorialActivo = false;

    void Start()
    {
        // Asegurarnos de que el tutorial esté oculto y el tiempo corra normal
        if (panelTutorial != null) panelTutorial.SetActive(false);
        Time.timeScale = 1f;

        // Iniciar la cuenta regresiva de 5 segundos
        StartCoroutine(IniciarPausaTutorial());

        // Escuchar el clic del botón mediante código
        botonObjetivo.onClick.AddListener(OnBotonCorrectoClickeado);
    }

    IEnumerator IniciarPausaTutorial()
    {
        // Esperar 5 segundos de tiempo de juego
        yield return new WaitForSeconds(5f);

        // Pausar el juego
        Time.timeScale = 0f;
        tutorialActivo = true;

        // Mostrar la indicación visual (el panel, la flecha, etc.)
        if (panelTutorial != null) panelTutorial.SetActive(true);

        Debug.Log("Tutorial: Juego pausado. Esperando input...");
    }

    void OnBotonCorrectoClickeado()
    {
        // Solo reanudamos si el tutorial está activo
        if (tutorialActivo)
        {
            // Reanudar el tiempo
            Time.timeScale = 1f;
            tutorialActivo = false;

            // Ocultar la indicación visual del tutorial
            if (panelTutorial != null) panelTutorial.SetActive(false);

            Debug.Log("Tutorial: Botón presionado, juego reanudado.");

            // Opcional: Remover el listener si este tutorial ya no se repetirá
            // botonObjetivo.onClick.RemoveListener(OnBotonCorrectoClickeado);
        }
        else
        {
            // Aquí va lo que hace tu botón normalmente durante el juego
            Debug.Log("El botón hizo su acción normal.");
        }
    }
}