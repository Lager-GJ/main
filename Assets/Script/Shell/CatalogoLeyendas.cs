using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Los 4 cuartos de "Los secretos de la casa", en orden. Un unico asset en el
    /// proyecto: Assets -> Create -> Leyendas del Ecuador -> Catalogo de Leyendas.
    /// </summary>
    [CreateAssetMenu(fileName = "CatalogoLeyendas", menuName = "Leyendas del Ecuador/Catálogo de Leyendas")]
    public class CatalogoLeyendas : ScriptableObject
    {
        public LeyendaDefinicion[] leyendas;
    }
}
