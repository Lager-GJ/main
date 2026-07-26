using UnityEngine;
using UnityEngine.SceneManagement;
using Terror;

/// <summary>
/// Cargador de escena de un solo boton, heredado de la splash original.
///
/// Con el shell de la antologia esto quedo practicamente obsoleto: la navegacion al
/// juego ahora la hace el menu (MenuTarjetaLeyenda -> MenuPrincipal -> SceneRouter),
/// que ademas respeta el bloqueo de leyendas y la pantalla de carga. Se conserva por
/// si alguna escena vieja todavia lo tiene cableado, pero apunta a la constante en
/// vez de a un string suelto: antes decia "Historia", que dejo de existir cuando esa
/// escena paso a llamarse L1_Intro.
/// </summary>
public class Scriptcambio : MonoBehaviour
{
    public void NV()
    {
        SceneManager.LoadScene(Escenas.L1Intro);
    }
}
