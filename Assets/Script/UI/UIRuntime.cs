using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Terror.UI
{
    /// <summary>
    /// Ayudas para armar UI en runtime (el cajón del inicio, el inventario, el candado...).
    /// Antes vivían dentro de InicioCuarto; se sacaron acá para reusarlas.
    /// Todo lo animado usa tiempo sin escala, salvo EsperarSinPausa.
    /// </summary>
    public static class UIRuntime
    {
        public static RectTransform NuevoRect(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = posicion;
            rt.sizeDelta = tamano;
            return rt;
        }

        public static Image NuevaImagen(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, Sprite sprite)
        {
            var img = NuevoRect(nombre, padre, posicion, tamano).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        public static TextMeshProUGUI NuevoTexto(string nombre, Transform padre, Vector2 posicion, Vector2 tamano,
            float tamanoFuente, FontStyles estilo, TMP_FontAsset fuente)
        {
            var t = NuevoRect(nombre, padre, posicion, tamano).gameObject.AddComponent<TextMeshProUGUI>();
            if (fuente != null) t.font = fuente;
            t.fontSize = tamanoFuente;
            t.fontStyle = estilo;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// Un rectángulo nuevo dentro de 'padre' que ocupa exactamente el mismo lugar del
        /// mundo que 'molde' (sin heredar su escala, que puede ser desigual en x e y).
        /// Sirve para armar el mueble abierto encima del mueble cerrado del cuarto.
        /// </summary>
        public static RectTransform RectSobre(string nombre, RectTransform molde, Transform padre)
        {
            var esquinas = new Vector3[4];
            molde.GetWorldCorners(esquinas);
            var rt = NuevoRect(nombre, padre, Vector2.zero, Vector2.zero);
            Vector3 centro = (esquinas[0] + esquinas[2]) * 0.5f;
            rt.position = new Vector3(centro.x, centro.y, padre.position.z);
            Vector3 escala = padre.lossyScale;
            rt.sizeDelta = new Vector2((esquinas[2].x - esquinas[0].x) / escala.x, (esquinas[2].y - esquinas[0].y) / escala.y);
            return rt;
        }

        /// <summary>
        /// Prepara un objeto del Canvas del mundo para que su UI se dibuje encima de Luis y
        /// de los muebles abiertos (orden 6) y siga recibiendo clics.
        /// </summary>
        public static void CapaEncimaEnCuarto(GameObject go, int orden = 6)
        {
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null) canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = orden;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
        }

        /// <summary>Panel a pantalla completa (anclas estiradas, sin márgenes).</summary>
        public static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Botón sin transición de color (para fondos que cierran y zonas tocables).</summary>
        public static Button BotonPlano(GameObject go, Action alClic)
        {
            var b = go.GetComponent<Button>();
            if (b == null) b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            if (alClic != null) b.onClick.AddListener(() => alClic());
            return b;
        }

        public static IEnumerator Animar(float duracion, Action<float> paso)
        {
            if (duracion <= 0f) { paso(1f); yield break; }
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                paso(t / duracion);
                yield return null;
            }
            paso(1f);
        }

        /// <summary>Aparece "saltando": escala con ease-out-back, se pasa un poco de 1 y vuelve.</summary>
        public static IEnumerator Pop(Transform objetivo, float duracion = 0.35f)
        {
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                if (objetivo == null) yield break;
                float k = Mathf.Clamp01(t / duracion);
                float s = 1f + 2.70158f * Mathf.Pow(k - 1f, 3f) + 1.70158f * Mathf.Pow(k - 1f, 2f);
                objetivo.localScale = Vector3.one * Mathf.Max(0.01f, s);
                yield return null;
            }
            if (objetivo != null) objetivo.localScale = Vector3.one;
        }

        /// <summary>Sacude un RectTransform alrededor de su posición actual (para "no, así no").</summary>
        public static IEnumerator Sacudir(RectTransform rt, float amplitud, float duracion = 0.3f)
        {
            if (rt == null) yield break;
            Vector2 p0 = rt.anchoredPosition;
            for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                float k = 1f - t / duracion;
                rt.anchoredPosition = p0 + new Vector2(
                    Mathf.Sin(t * 30f * Mathf.PI * 2f) * amplitud * k,
                    Mathf.Cos(t * 39f * Mathf.PI * 2f) * amplitud * 0.5f * k);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = p0;
        }

        // Como WaitForSecondsRealtime, pero el reloj se detiene con el menú de pausa
        // (timeScale 0): así lo que espera no ocurre por debajo de la pausa.
        public static IEnumerator EsperarSinPausa(float segundos)
        {
            for (float t = 0f; t < segundos; )
            {
                if (Time.timeScale > 0f) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Pone 'hijo' en 'padre' justo antes de 'antesDe' (si es hermano), o al final.
        /// </summary>
        public static void UbicarAntesDe(Transform hijo, Transform padre, Transform antesDe)
        {
            hijo.SetParent(padre, false);
            if (antesDe != null && antesDe.parent == padre)
                hijo.SetSiblingIndex(antesDe.GetSiblingIndex());
            else
                hijo.SetAsLastSibling();
        }
    }
}
