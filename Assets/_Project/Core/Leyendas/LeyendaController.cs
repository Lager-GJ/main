using System;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// El contrato que hace que el shell escale a 5 leyendas. Cada leyenda tiene su
    /// propia implementacion (hoy solo L1Controller); el shell la arranca, escucha
    /// AlTerminar, guarda el progreso y decide adonde ir.
    ///
    /// La idea de fondo: ninguna leyenda conoce a otra, ni al menu. Solo a esto.
    /// </summary>
    public abstract class LeyendaController : MonoBehaviour
    {
        /// <summary>Se dispara una sola vez por partida, al ganar, perder o abandonar.</summary>
        public event Action<ResultadoLeyenda> AlTerminar;

        public abstract void Iniciar(LeyendaDefinicion definicion);

        protected void Terminar(ResultadoLeyenda resultado) => AlTerminar?.Invoke(resultado);
    }
}
