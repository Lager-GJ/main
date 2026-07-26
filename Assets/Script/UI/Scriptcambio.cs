using UnityEngine;
using UnityEngine.SceneManagement;

public class Scriptcambio : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void NV()
    {
        SceneManager.LoadScene("Historia");
    }

    public void Niveles()
    {
        SceneManager.LoadScene("Nivel");
    }

    public void Creditos()
    {
        SceneManager.LoadScene("CreditosPan");
    }

    public void Volver()
    {
        SceneManager.LoadScene("Intro");
    }
}
