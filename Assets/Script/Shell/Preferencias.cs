using System;
using UnityEngine;

namespace Terror
{
    /// <summary>
    /// La configuración del jugador, en un solo lugar. Cualquier script la lee de acá
    /// (Preferencias.FactorSacudidas, Preferencias.ReducirDestellos...) y se entera de
    /// los cambios con OnCambio. Los cambios se ven al instante; se escriben en el
    /// guardado con Guardar() (el panel de Opciones lo hace al cerrarse).
    ///
    /// Guardar() vuelve a leer el perfil antes de escribir: así no pisa el progreso
    /// (cuartos completados) que otro script haya guardado mientras tanto.
    /// </summary>
    public static class Preferencias
    {
        /// <summary>Se dispara cada vez que cambia cualquier ajuste.</summary>
        public static event Action OnCambio;

        private static bool cargado;
        private static float volMaster = 1f, volMusica = 1f, volSfx = 1f;
        private static float brillo, contraste;
        private static float intensidadSacudidas = 1f;
        private static bool reducirDestellos;
        private static int resaltadoObjetos = 1;

        public static float VolMaster { get { Cargar(); return volMaster; } set => Poner(ref volMaster, Mathf.Clamp01(value)); }
        public static float VolMusica { get { Cargar(); return volMusica; } set => Poner(ref volMusica, Mathf.Clamp01(value)); }
        public static float VolSfx { get { Cargar(); return volSfx; } set => Poner(ref volSfx, Mathf.Clamp01(value)); }

        /// <summary>-1 (más oscuro) .. 1 (más claro). 0 = como se diseñó.</summary>
        public static float Brillo { get { Cargar(); return brillo; } set => Poner(ref brillo, Mathf.Clamp(value, -1f, 1f)); }
        /// <summary>-1 (menos contraste) .. 1 (más contraste). 0 = como se diseñó.</summary>
        public static float Contraste { get { Cargar(); return contraste; } set => Poner(ref contraste, Mathf.Clamp(value, -1f, 1f)); }

        /// <summary>Multiplica la amplitud de todas las sacudidas (cámara, muebles, portazo). 0 = ninguna.</summary>
        public static float FactorSacudidas { get { Cargar(); return intensidadSacudidas; } set => Poner(ref intensidadSacudidas, Mathf.Clamp01(value)); }

        /// <summary>Menos parpadeo de luces y estática (pensado para fotosensibilidad).</summary>
        public static bool ReducirDestellos
        {
            get { Cargar(); return reducirDestellos; }
            set { Cargar(); if (reducirDestellos == value) return; reducirDestellos = value; OnCambio?.Invoke(); }
        }

        /// <summary>0 = apagado, 1 = normal, 2 = alto contraste.</summary>
        public static int ResaltadoObjetos
        {
            get { Cargar(); return resaltadoObjetos; }
            set { Cargar(); value = Mathf.Clamp(value, 0, 2); if (resaltadoObjetos == value) return; resaltadoObjetos = value; OnCambio?.Invoke(); }
        }

        private static void Poner(ref float campo, float valor)
        {
            Cargar();
            if (Mathf.Approximately(campo, valor)) return;
            campo = valor;
            OnCambio?.Invoke();
        }

        /// <summary>Lee el guardado una sola vez (la primera vez que alguien pregunta).</summary>
        public static void Cargar()
        {
            if (cargado) return;
            cargado = true;
            LeerDe(SaveSystem.Cargar());
        }

        private static void LeerDe(PerfilJugador p)
        {
            volMaster = Mathf.Clamp01(p.volMaster);
            volMusica = Mathf.Clamp01(p.volMusica);
            volSfx = Mathf.Clamp01(p.volSfx);
            brillo = Mathf.Clamp(p.brillo, -1f, 1f);
            contraste = Mathf.Clamp(p.contraste, -1f, 1f);
            intensidadSacudidas = Mathf.Clamp01(p.intensidadSacudidas);
            reducirDestellos = p.reducirDestellos;
            resaltadoObjetos = Mathf.Clamp(p.resaltadoObjetos, 0, 2);
            Controles.LeerDe(p.teclas);
        }

        /// <summary>Escribe la configuración actual en el guardado (sin tocar el progreso).</summary>
        public static void Guardar()
        {
            Cargar();
            PerfilJugador p = SaveSystem.Cargar();
            p.volMaster = volMaster;
            p.volMusica = volMusica;
            p.volSfx = volSfx;
            p.brillo = brillo;
            p.contraste = contraste;
            p.intensidadSacudidas = intensidadSacudidas;
            p.reducirDestellos = reducirDestellos;
            p.resaltadoObjetos = resaltadoObjetos;
            p.teclas = Controles.Escribir();
            SaveSystem.Guardar(p);
        }

        /// <summary>Vuelve audio, imagen y accesibilidad a los valores de fábrica (no los controles).</summary>
        public static void RestablecerAjustes()
        {
            Cargar();
            var porDefecto = new PerfilJugador();
            volMaster = porDefecto.volMaster;
            volMusica = porDefecto.volMusica;
            volSfx = porDefecto.volSfx;
            brillo = porDefecto.brillo;
            contraste = porDefecto.contraste;
            intensidadSacudidas = porDefecto.intensidadSacudidas;
            reducirDestellos = porDefecto.reducirDestellos;
            resaltadoObjetos = porDefecto.resaltadoObjetos;
            OnCambio?.Invoke();
        }

        /// <summary>Lo llama Controles cuando cambia una tecla, para avisar a los textos.</summary>
        internal static void AvisarCambio() => OnCambio?.Invoke();

#if UNITY_EDITOR
        // Con "Enter Play Mode Options" sin recarga de dominio, lo estático sobrevive
        // entre partidas del Editor: se vuelve a leer el guardado en cada Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            cargado = false;
            OnCambio = null;
        }
#endif
    }
}
