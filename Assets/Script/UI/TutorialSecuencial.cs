using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Terror.UI
{
    /// <summary>
    /// Tutorial del cuarto 1 como una lista de pasos en orden. Cada paso espera su
    /// "disparador" (llegar a un marcador, recoger un objeto, encender el fósforo...),
    /// muestra su panel de mensaje y lo oculta solo. Reemplaza a ManagerTutorial y
    /// TutorialManager, que se desactivan en la escena (sin borrar sus scripts).
    /// Los textos viven en los paneles de UI, no aquí: se editan en el Editor.
    /// Es parecido a un array de "pasos" en JS que se va recorriendo con un índice.
    /// </summary>
    public class TutorialSecuencial : MonoBehaviour
    {
        public enum Disparador
        {
            AlIniciar,              // pasa el "retraso inicial" desde que arranca la escena
            AlTerminarPasoAnterior, // en cuanto se cierra el mensaje del paso anterior
            PasarMarcador,          // el niño llega (hacia la derecha) a la X del marcador
            ObjetoRecogido,         // el jugador recogió un ObjetoRecogible
            FosforoEncendido,       // el jugador encendió un fósforo
            ObjetoInspeccionado     // el jugador abrió un ObjetoInteractivo
        }

        // Los campos son public porque el tutorial (otra clase) los lee; es solo un
        // contenedor de datos que se rellena en el Inspector.
        [Serializable]
        public class Paso
        {
            [Tooltip("Solo para identificarlo en el Inspector y en la Consola.")]
            public string nombre;

            [Tooltip("Qué tiene que pasar para que aparezca el mensaje de este paso.")]
            public Disparador disparador;

            [Tooltip("Solo si el disparador es PasarMarcador: objeto vacío colocado en la escena.")]
            public Transform marcador;

            [Tooltip("Panel de UI con el texto del mensaje. Se enciende y apaga solo.")]
            public GameObject panel;

            [Tooltip("Marcado: congela el juego y el mensaje se cierra al pulsar A o D (flechas también). Úsalo solo en el paso de movimiento.")]
            public bool esperaMovimiento;

            [Tooltip("Segundos que se ve el mensaje. Se ignora si 'Espera Movimiento' está marcado.")]
            public float segundosVisible = 6f;

            [Tooltip("Opcional: objeto que parpadea desde que sale este mensaje hasta que arranca el paso siguiente.")]
            public ResaltadoParpadeo resaltar;
        }

        [Header("Referencias")]
        [Tooltip("El niño. Si queda vacío se busca el NinoController en la escena.")]
        [SerializeField] private Transform jugador;

        [Header("Configuración")]
        [Tooltip("Segundos de juego antes de que arranque un paso con disparador AlIniciar.")]
        [SerializeField] private float retrasoInicial = 1f;

        [Tooltip("Si está marcado, la tecla E no enciende fósforos hasta que se recoja un ObjetoRecogible.")]
        [SerializeField] private bool bloquearFosforosHastaRecoger = true;

        [Header("Pasos (en orden)")]
        [SerializeField] private Paso[] pasos;

        private int indice;                 // paso actual
        private bool mensajeVisible;        // false = esperando disparador, true = mostrando el mensaje
        private bool terminado;
        private bool pausandoJuego;
        private bool fosforosBloqueados;
        private float tiempoActivo;
        private float temporizadorMensaje;

        // Banderas "ya pasó": si el jugador hace algo antes de que le toque ese paso
        // (por ejemplo recoge el objeto antes de llegar al marcador), el tutorial no
        // se queda esperando un evento que ya ocurrió.
        private bool fosforoYaEncendido;
        private bool objetoYaRecogido;
        private bool objetoYaInspeccionado;

        private void OnEnable()
        {
            FosforoManager.OnFosforoEncendido += MarcarFosforoEncendido;
            ObjetoRecogible.OnRecogido += MarcarObjetoRecogido;
            ObjetoInteractivo.OnObjetoInspeccionado += MarcarObjetoInspeccionado;
        }

        private void OnDisable()
        {
            FosforoManager.OnFosforoEncendido -= MarcarFosforoEncendido;
            ObjetoRecogible.OnRecogido -= MarcarObjetoRecogido;
            ObjetoInteractivo.OnObjetoInspeccionado -= MarcarObjetoInspeccionado;

            // timeScale es global y sobrevive al cambio de escena: nunca lo dejamos en 0.
            if (pausandoJuego)
            {
                Time.timeScale = 1f;
                pausandoJuego = false;
            }

            // Si el tutorial se apaga a medias, no dejamos al jugador sin poder encender fósforos.
            DesbloquearFosforos();
        }

        private void Start()
        {
            if (jugador == null)
            {
                var nino = FindFirstObjectByType<NinoController>();
                if (nino != null) jugador = nino.transform;
            }

            // Todo apagado de entrada: cada paso enciende lo suyo cuando le toca.
            foreach (var paso in pasos)
            {
                if (paso.panel != null) paso.panel.SetActive(false);
                if (paso.resaltar != null) paso.resaltar.Desactivar();
            }

            if (bloquearFosforosHastaRecoger && FosforoManager.Instance != null)
            {
                FosforoManager.Instance.HabilitarEncendido(false);
                fosforosBloqueados = true;
            }

            if (pasos.Length == 0) terminado = true;
        }

        private void Update()
        {
            if (terminado) return;

            tiempoActivo += Time.deltaTime;
            Paso paso = pasos[indice];

            if (!mensajeVisible)
            {
                if (DisparadorCumplido(paso)) MostrarPaso(paso);
                return;
            }

            if (paso.esperaMovimiento)
            {
                if (JugadorPulsoMovimiento()) CerrarPaso(paso);
            }
            else
            {
                temporizadorMensaje -= Time.deltaTime;
                if (temporizadorMensaje <= 0f) CerrarPaso(paso);
            }
        }

        private bool DisparadorCumplido(Paso paso)
        {
            switch (paso.disparador)
            {
                case Disparador.AlIniciar:
                    return tiempoActivo >= retrasoInicial;

                case Disparador.AlTerminarPasoAnterior:
                    return true;

                case Disparador.PasarMarcador:
                    if (paso.marcador == null || jugador == null)
                    {
                        // Preferimos saltar el paso a dejar el tutorial trabado para siempre.
                        Debug.LogWarning($"[Tutorial] El paso '{paso.nombre}' no tiene marcador o jugador asignado. Se muestra de inmediato.");
                        return true;
                    }
                    return jugador.position.x >= paso.marcador.position.x;

                case Disparador.ObjetoRecogido:
                    return objetoYaRecogido;

                case Disparador.FosforoEncendido:
                    return fosforoYaEncendido;

                case Disparador.ObjetoInspeccionado:
                    return objetoYaInspeccionado;

                default:
                    return true;
            }
        }

        private void MostrarPaso(Paso paso)
        {
            // El resalte del paso anterior dura hasta que arranca este.
            if (indice > 0 && pasos[indice - 1].resaltar != null)
                pasos[indice - 1].resaltar.Desactivar();

            mensajeVisible = true;
            temporizadorMensaje = paso.segundosVisible;

            if (paso.panel != null) paso.panel.SetActive(true);
            if (paso.resaltar != null) paso.resaltar.Activar();

            if (paso.esperaMovimiento)
            {
                Time.timeScale = 0f;
                pausandoJuego = true;
            }

            Debug.Log($"[Tutorial] Paso {indice + 1}/{pasos.Length}: {paso.nombre}");
        }

        private void CerrarPaso(Paso paso)
        {
            if (paso.panel != null) paso.panel.SetActive(false);

            if (pausandoJuego)
            {
                Time.timeScale = 1f;
                pausandoJuego = false;
            }

            mensajeVisible = false;
            indice++;

            if (indice >= pasos.Length) Terminar();
        }

        private void Terminar()
        {
            terminado = true;

            // El último resalte no tiene "paso siguiente" que lo apague.
            var ultimo = pasos[pasos.Length - 1];
            if (ultimo.resaltar != null) ultimo.resaltar.Desactivar();

            Debug.Log("[Tutorial] Terminado.");
        }

        private bool JugadorPulsoMovimiento()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return false;

            return teclado.aKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame
                || teclado.leftArrowKey.wasPressedThisFrame || teclado.rightArrowKey.wasPressedThisFrame;
        }

        private void DesbloquearFosforos()
        {
            if (!fosforosBloqueados) return;
            fosforosBloqueados = false;

            if (FosforoManager.Instance != null)
                FosforoManager.Instance.HabilitarEncendido(true);
        }

        private void MarcarFosforoEncendido() => fosforoYaEncendido = true;

        private void MarcarObjetoRecogido(ObjetoRecogible objeto)
        {
            objetoYaRecogido = true;
            DesbloquearFosforos();
        }

        private void MarcarObjetoInspeccionado(ObjetoInteractivo objeto) => objetoYaInspeccionado = true;
    }
}
