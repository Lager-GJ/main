using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentAudio : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Escribe el nombre exacto de la escena donde el audio debe detenerse.")]
    public string sceneToStop;

    private static PersistentAudio instance;

    void Awake()
    {
        // Evita que la música se duplique si regresas a la escena original
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // Hace que el objeto sobreviva al cambio de escena
    }

    void OnEnable()
    {
        // Nos suscribimos al evento que avisa cuando una escena nueva termina de cargar
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Comprobamos si la escena que acaba de cargar es la escena objetivo
        if (scene.name == sceneToStop)
        {
            Destroy(gameObject);
            // Nota: Si prefieres que el objeto siga existiendo pero mudo, 
            // usa GetComponent().Stop(); en lugar de Destroy.
        }
    }
}
