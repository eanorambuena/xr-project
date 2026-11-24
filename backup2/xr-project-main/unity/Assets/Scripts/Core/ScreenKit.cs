using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRCar.Core
{
    /// <summary>
    /// Piezas comunes de las pantallas flotantes (menú de escenarios, resultado): paleta sobria, formas angulares
    /// y textos. Una instancia guarda las texturas y sprites de las formas que dibuja; hay que llamar a Dispose().
    /// </summary>
    public sealed class ScreenKit
    {
        /// <summary>Capa de Unity que usan las pantallas que se dibujan por encima de todo (UI)</summary>
        public const int OverlayLayer = 5;

        // Paleta sobria: carbón, gris acero y un solo acento azul
        public static readonly Color Charcoal = new Color(0.075f, 0.090f, 0.115f, 0.97f);
        public static readonly Color BorderLine = new Color(0.42f, 0.52f, 0.64f, 0.85f);
        public static readonly Color Wedge = new Color(0.135f, 0.165f, 0.215f, 1f);
        public static readonly Color Steel = new Color(0.78f, 0.82f, 0.87f, 1f);
        public static readonly Color Accent = new Color(0.45f, 0.62f, 0.85f, 1f);
        public static readonly Color ButtonNormal = new Color(0.075f, 0.085f, 0.11f, 1f);
        public static readonly Color ButtonSelected = new Color(0.24f, 0.42f, 0.68f, 1f);
        public static readonly Color TextDim = new Color(0.45f, 0.48f, 0.54f);
        public static readonly Color TextLight = new Color(0.88f, 0.90f, 0.94f);

        private readonly List<Object> created = new List<Object>();   // texturas y sprites de las formas

        /// <summary>Destruye las texturas y sprites creados</summary>
        public void Dispose()
        {
            foreach (var o in created)
                if (o != null) { if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }
            created.Clear();
        }

        /// <summary>
        /// Hace que toda la interfaz de este objeto se dibuje encima de lo demás (sin test de profundidad y al final),
        /// con la cámara principal. No usa una segunda cámara: en el visor (varias cámaras estéreo) eso dejaba la imagen
        /// sin borrar ("hall de espejos"). Los materiales son copias propias; Dispose las destruye.
        /// </summary>
        public void AlwaysOnTop(GameObject root)
        {
            const int Always = (int)UnityEngine.Rendering.CompareFunction.Always;
            foreach (var g in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                Material m;
                if (g is TextMeshProUGUI tmp)
                {
                    m = tmp.fontMaterial;   // copia propia de este texto
                    if (m == null) continue;
                }
                else
                {
                    m = new Material(UnityEngine.UI.Graphic.defaultGraphicMaterial);
                    g.material = m;
                }
                m.SetInt("unity_GUIZTestMode", Always);   // los shaders de UI y TMP leen esta propiedad
                m.renderQueue = 4000;                      // después de los vidrios y demás transparentes
                created.Add(m);
            }
        }

        /// <summary>Pone un objeto y todos sus hijos en una capa</summary>
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        // ---- geometría ----
        public static Vector2 P(float x, float y) => new Vector2(x, y);

        /// <summary>Rectángulo con las esquinas superior-izquierda e inferior-derecha cortadas</summary>
        public static Vector2[] Chamfered(float l, float t, float r, float b, float cut) => new[]
        {
            P(l + cut, t), P(r, t), P(r, b + cut), P(r - cut, b), P(l, b), P(l, t - cut)
        };

        /// <summary>Botón centrado en el origen con dos esquinas cortadas (arriba-izquierda y abajo-derecha)</summary>
        public static Vector2[] CutCorners(float w, float h, float cut) => Chamfered(-w / 2f, h / 2f, w / 2f, -h / 2f, cut);

        /// <summary>
        /// Dibuja un polígono convexo como Image: lo rasteriza (con suavizado) en una textura y la usa de sprite.
        /// Así no depende de nada más que del Image normal de uGUI.
        /// </summary>
        public Image NewPoly(string name, Transform parent, Vector2 pos, Vector2[] pts, Color color)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, area = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                minX = Mathf.Min(minX, pts[i].x); maxX = Mathf.Max(maxX, pts[i].x);
                minY = Mathf.Min(minY, pts[i].y); maxY = Mathf.Max(maxY, pts[i].y);
                var a = pts[i]; var b = pts[(i + 1) % pts.Length];
                area += a.x * b.y - b.x * a.y;
            }
            float w = maxX - minX, h = maxY - minY, sign = area >= 0f ? 1f : -1f;
            float density = w * h > 200000f ? 0.5f : 1f;   // texeles por unidad del canvas
            int tw = Mathf.Max(2, Mathf.CeilToInt(w * density)), th = Mathf.Max(2, Mathf.CeilToInt(h * density));

            var px = new Color32[tw * th];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            var q = new Vector2(minX + (x + (sx + 0.5f) * 0.5f) / density, minY + (y + (sy + 0.5f) * 0.5f) / density);
                            bool inside = true;
                            for (int e = 0; e < pts.Length && inside; e++)
                            {
                                var a = pts[e]; var b = pts[(e + 1) % pts.Length];
                                if (((b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x)) * sign < 0f) inside = false;
                            }
                            if (inside) hit++;
                        }
                    px[y * tw + x] = new Color32(255, 255, 255, (byte)(hit * 255 / 4));
                }

            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixels32(px); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            created.Add(tex); created.Add(sprite);

            var r = NewRect(name, parent, pos + new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f), new Vector2(w, h));
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static TextMeshProUGUI NewText(string name, Transform parent, string text, Vector2 pos, Vector2 size,
            float fontSize, Color color, FontStyles style)
        {
            var r = NewRect(name, parent, pos, size);
            var tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
            // Fuera del modo juego (tests) el componente no pasa por Awake y queda sin fuente: se la damos explícitamente
            if (tmp.font == null && TMP_Settings.instance != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
