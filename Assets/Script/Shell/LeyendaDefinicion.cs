using UnityEngine;

namespace Terror
{
    /// <summary>
    /// Datos de un cuarto para el menu "Los secretos de la casa". Se crea desde
    /// Assets -> Create -> Leyendas del Ecuador -> Definicion de Leyenda.
    /// </summary>
    [CreateAssetMenu(fileName = "Leyenda_", menuName = "Leyendas del Ecuador/Definición de Leyenda")]
    public class LeyendaDefinicion : ScriptableObject
    {
        [Tooltip("Id estable, usado en el perfil guardado. NO cambiar una vez publicado.")]
        public string id;

        [Tooltip("Nombre del cuarto, el que se ve en la tarjeta. Ej: El patio de la procesión")]
        public string nombre;

        [Tooltip("Que leyenda cuenta este cuarto. Ej: La Caja Ronca")]
        public string leyenda;

        [TextArea]
        public string descripcion;

        [TextArea]
        [Tooltip("Frase corta en la tarjeta bloqueada, debajo del candado.")]
        public string teaser;

        [Tooltip("Portada de la tarjeta. Sin asignar se ve el color de fondo del Image.")]
        public Sprite portada;

        [Tooltip("Escena a cargar al entrar. Vacio en las bloqueadas.")]
        public string nombreEscena;

        [Tooltip("Marcar solo en el primer cuarto: es el unico jugable de arranque.")]
        public bool desbloqueadaPorDefecto;

        [Tooltip("Al completar ESTE cuarto, se desbloquea el que se arrastre aca. Vacio en el ultimo de la secuencia.")]
        public LeyendaDefinicion siguienteLeyenda;
    }
}
