using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Las 5 entradas del menu: La Caja de Fosforos (jugable) + Cantuña, La Dama
    /// Tapada, El Padre Almeida y La Caja Ronca (bloqueadas). Existe un unico asset
    /// de esta clase en el proyecto; se crea desde
    /// Assets -> Create -> Leyendas del Ecuador -> Catalogo de Leyendas.
    /// </summary>
    [CreateAssetMenu(fileName = "CatalogoLeyendas", menuName = "Leyendas del Ecuador/Catálogo de Leyendas")]
    public class CatalogoLeyendas : ScriptableObject
    {
        [Tooltip("En el orden en que aparecen en el menu.")]
        public LeyendaDefinicion[] leyendas;
    }
}
