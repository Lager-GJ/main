using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Datos de una leyenda para el menu. Se crea desde
    /// Assets -> Create -> Leyendas del Ecuador -> Definicion de Leyenda.
    /// </summary>
    [CreateAssetMenu(fileName = "Leyenda_", menuName = "Leyendas del Ecuador/Definición de Leyenda")]
    public class LeyendaDefinicion : ScriptableObject
    {
        [Tooltip("Id estable, usado en el perfil guardado. NO cambiar una vez publicado: romperia los guardados existentes.")]
        public string id;

        [Tooltip("Nombre que se muestra en la tarjeta del menu.")]
        public string nombre;

        [TextArea]
        [Tooltip("Descripcion larga. Todavia no se muestra en ningun lado; queda para cuando la tarjeta tenga vista de detalle.")]
        public string descripcion;

        [TextArea]
        [Tooltip("Frase corta que se muestra en la tarjeta bloqueada, debajo del candado.")]
        public string teaser;

        [Tooltip("Portada de la tarjeta. Sin asignar se ve el color de fondo del Image.")]
        public Sprite portada;

        [Tooltip("Escena a cargar al entrar. Para la Leyenda 1 es L1_Intro (la historia va antes del juego). Vacio en las bloqueadas.")]
        public string nombreEscena;

        [Tooltip("Marcar solo en la Leyenda 1: es la unica jugable de arranque.")]
        public bool desbloqueadaPorDefecto;

        [Tooltip("Al completar ESTA leyenda, se desbloquea automaticamente la que se arrastre aca. Vacio en la ultima de la secuencia (hoy: El Patio de la Procesion).")]
        public LeyendaDefinicion siguienteLeyenda;
    }
}
