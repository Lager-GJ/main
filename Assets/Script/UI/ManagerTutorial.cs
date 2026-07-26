using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ManagerTutorial : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Button botonObjetivo; // El boton que obligatoriamente deben presionar
    public GameObject panelTutorial; // El panel u oscurecimiento visual

    private bool tutorialActivo = false;

    void Start()
    {
        // Asegurarnos de que el tutorial este oculto y el tiempo corra normal
        if (panelTutorial != null) panelTutorial.SetActive(false);
        Time.timeScale = 1f;

        // Iniciar la cuenta regresiva
        StartCoroutine(IniciarPausaTutorial());

        // Escuchar el clic del boton mediante codigo
        botonObjetivo.onClick.AddListener(OnBotonCorrectoClickeado);
    }

    IEnumerator IniciarPausaTutorial()
    {
        // Esperar 1.5 segundos de tiempo de juego
        yield return new WaitForSeconds(1.5f);

        // Pausar el juego
        Time.timeScale = 0f;
        tutorialActivo = true;

        // Mostrar la indicacion visual (el panel, la flecha, etc.)
        if (panelTutorial != null) panelTutorial.SetActive(true);

        Debug.Log("Tutorial: Juego pausado. Esperando input...");
    }

    void OnBotonCorrectoClickeado()
    {
        // Solo reanudamos si el tutorial esta activo
        if (tutorialActivo)
        {
            // Reanudar el tiempo
            Time.timeScale = 1f;
            tutorialActivo = false;

            // Ocultar la indicacion visual del tutorial
            if (panelTutorial != null) panelTutorial.SetActive(false);

            Debug.Log("Tutorial: Boton presionado, juego reanudado.");

            // Opcional: Remover el listener si este tutorial ya no se repetira
            // botonObjetivo.onClick.RemoveListener(OnBotonCorrectoClickeado);
        }
        else
        {
            // Aqui va lo que hace tu boton normalmente durante el juego
            Debug.Log("El boton hizo su accion normal.");
        }
    }
}
