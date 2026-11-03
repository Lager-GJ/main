using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

namespace Terror
{
    /// <summary>Lo que el jugador puede hacer con el teclado y se puede cambiar de tecla.</summary>
    public enum Accion
    {
        Izquierda,
        Derecha,
        Encender,
        Apagar,
        Inventario,
        Pausa,
    }

    /// <summary>
    /// Teclas del juego, cambiables desde Opciones → Controles. Cada acción tiene una
    /// tecla principal (la que se cambia) y, algunas, una tecla fija extra que siempre
    /// funciona (las flechas para moverse, Tab para el inventario, Escape para pausar).
    ///
    /// En vez de Keyboard.current.eKey, los scripts preguntan Controles.Pulsada(Accion.Encender).
    /// Los textos con &lt;link=tecla&gt;E&lt;/link&gt; (MarcoTeclas) muestran la tecla elegida.
    /// </summary>
    public static class Controles
    {
        private struct Definicion
        {
            public string nombre;
            public Key porDefecto;
            public Key fija;
        }

        private static readonly Dictionary<Accion, Definicion> Definiciones = new Dictionary<Accion, Definicion>
        {
            { Accion.Izquierda,  new Definicion { nombre = "Caminar a la izquierda", porDefecto = Key.A, fija = Key.LeftArrow } },
            { Accion.Derecha,    new Definicion { nombre = "Caminar a la derecha",   porDefecto = Key.D, fija = Key.RightArrow } },
            { Accion.Encender,   new Definicion { nombre = "Encender un fósforo",    porDefecto = Key.E, fija = Key.None } },
            { Accion.Apagar,     new Definicion { nombre = "Apagar el fósforo",      porDefecto = Key.Q, fija = Key.None } },
            { Accion.Inventario, new Definicion { nombre = "Abrir la bolsa",         porDefecto = Key.I, fija = Key.Tab } },
            { Accion.Pausa,      new Definicion { nombre = "Pausa",                  porDefecto = Key.P, fija = Key.Escape } },
        };

        /// <summary>
        /// Teclas que no se pueden asignar: las usan menús, primeros planos y el candado
        /// (Escape, Enter, flechas, números, borrar) o ya son la tecla fija de algo (Tab).
        /// </summary>
        private static readonly HashSet<Key> Reservadas = new HashSet<Key>
        {
            Key.Escape, Key.Enter, Key.NumpadEnter, Key.Tab, Key.Backspace,
            Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow,
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
            Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
            Key.LeftWindows, Key.RightWindows, Key.LeftCommand, Key.RightCommand,
            Key.ContextMenu, Key.PrintScreen, Key.None,
        };

        private static readonly Dictionary<Accion, Key> actuales = new Dictionary<Accion, Key>();

        public static IEnumerable<Accion> Todas => (Accion[])Enum.GetValues(typeof(Accion));

        public static string NombreAccion(Accion a) => Definiciones[a].nombre;

        /// <summary>La tecla principal elegida para la acción.</summary>
        public static Key Tecla(Accion a)
        {
            Preferencias.Cargar();
            return actuales.TryGetValue(a, out Key k) ? k : Definiciones[a].porDefecto;
        }

        /// <summary>La tecla extra que no se puede cambiar (Key.None si no tiene).</summary>
        public static Key TeclaFija(Accion a) => Definiciones[a].fija;

        /// <summary>¿Se apretó en este frame la tecla principal o la fija?</summary>
        public static bool Pulsada(Accion a)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            Key fija = Definiciones[a].fija;
            return k[Tecla(a)].wasPressedThisFrame || (fija != Key.None && k[fija].wasPressedThisFrame);
        }

        /// <summary>¿Está apretada ahora la tecla principal o la fija?</summary>
        public static bool Mantenida(Accion a)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            Key fija = Definiciones[a].fija;
            return k[Tecla(a)].isPressed || (fija != Key.None && k[fija].isPressed);
        }

        /// <summary>¿Se apretó en este frame solo la tecla principal (sin contar la fija)?</summary>
        public static bool PulsadaPrincipal(Accion a)
        {
            var k = Keyboard.current;
            return k != null && k[Tecla(a)].wasPressedThisFrame;
        }

        public static bool EsReservada(Key tecla) => Reservadas.Contains(tecla);

        /// <summary>
        /// Asigna la tecla. Si otra acción ya la usaba, intercambian teclas (así nunca
        /// quedan dos acciones con la misma). Devuelve la acción que cambió de tecla por
        /// el intercambio, o null.
        /// </summary>
        public static Accion? Asignar(Accion a, Key tecla)
        {
            if (EsReservada(tecla)) return null;
            Key anterior = Tecla(a);
            if (anterior == tecla) return null;

            Accion? intercambiada = null;
            foreach (Accion otra in Todas)
            {
                if (otra != a && Tecla(otra) == tecla)
                {
                    actuales[otra] = anterior;
                    intercambiada = otra;
                }
            }
            actuales[a] = tecla;
            Preferencias.AvisarCambio();
            return intercambiada;
        }

        public static void Restablecer()
        {
            actuales.Clear();
            Preferencias.AvisarCambio();
        }

        /// <summary>
        /// Si 'porDefecto' es la tecla de fábrica de alguna acción, devuelve la que eligió
        /// el jugador para esa acción. Si no, la misma. Sirve para campos del Inspector
        /// que guardan una tecla fija (por ejemplo la tarjeta "Pulsa E").
        /// </summary>
        public static Key Equivalente(Key porDefecto)
        {
            foreach (var par in Definiciones)
                if (par.Value.porDefecto == porDefecto) return Tecla(par.Key);
            return porDefecto;
        }

        /// <summary>
        /// Nombre corto para mostrar en pantalla ("E", "Espacio", "Flecha izq.").
        /// Sin símbolos de flecha: la fuente IM Fell no los tiene.
        /// </summary>
        public static string NombreTecla(Key tecla)
        {
            switch (tecla)
            {
                case Key.None: return "—";
                case Key.Space: return "Espacio";
                case Key.LeftArrow: return "Flecha izq.";
                case Key.RightArrow: return "Flecha der.";
                case Key.UpArrow: return "Flecha arriba";
                case Key.DownArrow: return "Flecha abajo";
                case Key.Escape: return "Esc";
                case Key.Tab: return "Tab";
                case Key.Enter: return "Enter";
                case Key.LeftShift: return "Shift izq.";
                case Key.RightShift: return "Shift der.";
                case Key.LeftCtrl: return "Ctrl izq.";
                case Key.RightCtrl: return "Ctrl der.";
                case Key.LeftAlt: return "Alt izq.";
                case Key.RightAlt: return "Alt der.";
                case Key.CapsLock: return "Bloq Mayús";
            }

            // El nombre que da el teclado respeta la distribución (AZERTY, QWERTZ...).
            var k = Keyboard.current;
            if (k != null)
            {
                string nombre = k[tecla].displayName;
                if (!string.IsNullOrEmpty(nombre)) return nombre.Length == 1 ? nombre.ToUpperInvariant() : nombre;
            }
            return tecla.ToString();
        }

        // <link=tecla>  (etiquetas como <b><size=115%>)  LETRA  (cierres)  </link>
        private static readonly Regex EnlaceTecla = new Regex(
            @"(<link=""?tecla""?>(?:\s*<[^>]+>)*\s*)([^<\s]+)(\s*(?:<[^>]+>\s*)*</link>)",
            RegexOptions.Compiled);

        /// <summary>
        /// Cambia, dentro de los &lt;link=tecla&gt;, la tecla de fábrica por la elegida.
        /// "Pulsa &lt;link=tecla&gt;E&lt;/link&gt;" con Encender en F → "Pulsa &lt;link=tecla&gt;F&lt;/link&gt;".
        /// </summary>
        public static string TraducirTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto) || texto.IndexOf("tecla", StringComparison.Ordinal) < 0) return texto;
            return EnlaceTecla.Replace(texto, m =>
            {
                string dentro = m.Groups[2].Value;
                foreach (var par in Definiciones)
                {
                    if (string.Equals(dentro, par.Value.porDefecto.ToString(), StringComparison.OrdinalIgnoreCase))
                        return m.Groups[1].Value + NombreTecla(Tecla(par.Key)) + m.Groups[3].Value;
                }
                return m.Value;
            });
        }

        // ---------- Guardado ("Accion=Tecla") ----------

        internal static void LeerDe(List<string> guardadas)
        {
            actuales.Clear();
            if (guardadas == null) return;
            foreach (string linea in guardadas)
            {
                int i = linea.IndexOf('=');
                if (i <= 0) continue;
                if (!Enum.TryParse(linea.Substring(0, i), out Accion a)) continue;
                if (!Enum.TryParse(linea.Substring(i + 1), out Key k) || EsReservada(k)) continue;
                actuales[a] = k;
            }
        }

        internal static List<string> Escribir()
        {
            var lista = new List<string>();
            foreach (var par in actuales)
                if (par.Value != Definiciones[par.Key].porDefecto)
                    lista.Add(par.Key + "=" + par.Value);
            return lista;
        }
    }
}
