using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terror
{
    /// <summary>Un objeto que se puede llevar en el inventario. Se define en el catálogo de Inventario.</summary>
    [Serializable]
    public class DefinicionObjeto
    {
        [Tooltip("Identificador que usan las cerraduras y los objetos de la escena (ej. llave_puerta).")]
        public string id;
        public string nombre;
        [Tooltip("Dibujo chico: el casillero del inventario y la tarjeta si no hay ilustración.")]
        public Sprite icono;
        [Tooltip("Arte de la tarjeta (1905x970, como las de Historia). Vacío = el ícono en grande sobre gris.")]
        public Sprite ilustracion;
        [Tooltip("Textos de su tarjeta, en orden (el botón > pasa al siguiente).")]
        [TextArea(1, 3)] public string[] textos;
        [Tooltip("Opcional: un dibujo distinto para cada texto (ej. el frente y el reverso de una foto). Vacío en un lugar = el dibujo de siempre.")]
        public Sprite[] dibujoPorTexto;
    }

    /// <summary>
    /// Lo que lleva Luis. Es solo una lista de ids: qué es cada id (nombre, dibujo,
    /// textos) se define en 'catalogo', en el Inspector. Las cerraduras preguntan con
    /// Tiene(id) y entregan con Agregar(id). Lo muestra InventarioUI (tecla I o Tab).
    ///
    /// No se guarda entre partidas: al reiniciar el cuarto se empieza sin nada.
    /// </summary>
    public class Inventario : MonoBehaviour
    {
        public static Inventario Instance { get; private set; }

        [SerializeField] private List<DefinicionObjeto> catalogo = new List<DefinicionObjeto>();

        private readonly List<string> objetos = new List<string>();

        /// <summary>Se agregó un objeto (para el aviso "Guardaste..." y el tutorial).</summary>
        public static event Action<DefinicionObjeto> OnObjetoAgregado;

        /// <summary>Cambió el contenido (se agregó o se usó algo).</summary>
        public static event Action OnCambiado;

        public IReadOnlyList<string> Objetos => objetos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Tiene(string id) => !string.IsNullOrEmpty(id) && objetos.Contains(id);

        public DefinicionObjeto Definicion(string id)
        {
            foreach (var d in catalogo)
                if (d != null && d.id == id) return d;
            return null;
        }

        public string Nombre(string id)
        {
            var d = Definicion(id);
            return d != null && !string.IsNullOrEmpty(d.nombre) ? d.nombre : id;
        }

        /// <summary>Agrega el objeto. Devuelve false si ya lo tenía (no se duplica).</summary>
        public bool Agregar(string id)
        {
            if (string.IsNullOrEmpty(id) || objetos.Contains(id)) return false;

            DefinicionObjeto def = Definicion(id);
            if (def == null)
            {
                Debug.LogWarning($"[Inventario] '{id}' no está en el catálogo: se guarda sin nombre ni dibujo.");
                def = new DefinicionObjeto { id = id, nombre = id };
            }

            objetos.Add(id);
            Debug.Log($"[Inventario] Guardado: {def.nombre}");
            OnObjetoAgregado?.Invoke(def);
            OnCambiado?.Invoke();
            return true;
        }

        /// <summary>Saca el objeto (una llave que se usó). Devuelve false si no lo tenía.</summary>
        public bool Quitar(string id)
        {
            if (!objetos.Remove(id)) return false;
            OnCambiado?.Invoke();
            return true;
        }

        [ContextMenu("Debug: dar todos los objetos")]
        private void DarTodos()
        {
            foreach (var d in catalogo)
                if (d != null) Agregar(d.id);
        }
    }
}
