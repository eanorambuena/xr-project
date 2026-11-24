using System.Collections.Generic;
using UnityEngine;

namespace VRCar.Core
{
    /// <summary>Un espacio de estacionamiento que sabe cuánto del auto está adentro</summary>
    public interface IParkingZone
    {
        /// <summary>Fracción de las esquinas del auto dentro del espacio (0 = afuera, 1 = todo adentro)</summary>
        float VehicleOverlap { get; }
    }

    /// <summary>
    /// Flecha plana (casi 2D) que flota sobre el espacio objetivo y apunta hacia abajo.
    /// Siempre mira al conductor (gira solo sobre el eje vertical), sube y baja suavemente
    /// y desaparece apenas el auto cruza la línea del espacio; reaparece si sale (por ejemplo al reiniciar).
    /// Se construye por código (malla propia, sin colliders) y sigue al Transform del espacio,
    /// así acompaña al hueco cuando cambia la dificultad.
    /// </summary>
    public class TargetMarker : MonoBehaviour
    {
        [SerializeField] private Transform target;                 // espacio objetivo
        [SerializeField] private Vector3 localOffset = Vector3.zero; // centro del espacio, en coordenadas locales del Transform
        [SerializeField] private float tipHeight = 2.4f;           // m sobre el piso a la punta de la flecha
        [SerializeField] private float bobAmplitude = 0.12f;       // m
        [SerializeField] private float bobSpeed = 2.0f;            // rad/s
        [SerializeField] private Material material;

        public const float HeadHeight = 0.90f, HeadHalfWidth = 0.65f, ShaftHeight = 0.70f, ShaftHalfWidth = 0.22f;
        public const float Thickness = 0.06f;
        /// <summary>
        /// Fracción del auto dentro del espacio a partir de la cual la flecha se oculta / vuelve a mostrarse.
        /// Se oculta apenas una esquina del auto cruza la línea (1 de 4 = 0.25) y vuelve cuando el auto salió por completo.
        /// </summary>
        public const float HideAt = 0.2f, ShowAt = 0.01f;
        /// <summary>Color de la flecha y cuánta luz propia emite (la hace verse brillante aun a la sombra)</summary>
        public static readonly Color ArrowColor = new Color(0.88f, 0.68f, 0.18f);
        public const float EmissionIntensity = 1.0f;
        private const float FadeSpeed = 4f;   // 1/s

        private Transform arrow;
        private MeshRenderer arrowRenderer;
        private Material runtimeMaterial;
        private IParkingZone zone;
        private bool entered;
        private float visibility = 1f;

        public bool Entered => entered;
        public float Visibility => visibility;

        public void Configure(Transform targetSpot, Vector3 centerOffset, Material mat)
        {
            target = targetSpot; localOffset = centerOffset; material = mat;
        }

        public void SetZone(IParkingZone z) { zone = z; }

        /// <summary>Punto de la punta de la flecha en reposo (sin el vaivén)</summary>
        public Vector3 AnchorPosition()
        {
            if (target == null) return transform.position;
            Vector3 p = target.TransformPoint(localOffset);
            p.y += tipHeight;   // sobre el piso del espacio (en pendiente el espacio está más arriba o abajo)
            return p;
        }

        /// <summary>Histéresis: se oculta apenas el auto cruza la línea y vuelve a mostrarse cuando salió por completo</summary>
        public static bool NextEntered(bool wasEntered, float overlap) => wasEntered ? overlap >= ShowAt : overlap >= HideAt;

        private void Awake() { Build(); }

        private void Start()
        {
            if (zone != null) return;
            IParkingZone found = FindFirstObjectByType<ParkingJudgment>();
            if (found == null) found = FindFirstObjectByType<ParallelParkingJudgment>();
            zone = found;
        }

        /// <summary>Crea la flecha (una sola vez)</summary>
        public Transform Build()
        {
            if (arrow != null) return arrow;
            var go = new GameObject("Arrow");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildArrowMesh();
            arrowRenderer = go.AddComponent<MeshRenderer>();
            arrowRenderer.sharedMaterial = MakeBrightMaterial();
            arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arrowRenderer.receiveShadows = false;
            arrow = go.transform;
            arrow.position = AnchorPosition();
            return arrow;
        }

        /// <summary>Copia del material con color claro y emisión fuerte (se arma al iniciar, no depende de regenerar la escena)</summary>
        private Material MakeBrightMaterial()
        {
            runtimeMaterial = material != null ? new Material(material) : new Material(Shader.Find("Sprites/Default"));
            runtimeMaterial.color = ArrowColor;
            if (runtimeMaterial.HasProperty("_EmissionColor"))
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.SetColor("_EmissionColor", ArrowColor * EmissionIntensity);
                runtimeMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return runtimeMaterial;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) { if (Application.isPlaying) Destroy(runtimeMaterial); else DestroyImmediate(runtimeMaterial); }
        }

        /// <summary>Actualiza si la flecha debe verse (con transición corta)</summary>
        public void UpdateVisibility(float dt)
        {
            float overlap = zone != null ? zone.VehicleOverlap : 0f;
            entered = NextEntered(entered, overlap);
            visibility = Mathf.MoveTowards(visibility, entered ? 0f : 1f, dt * FadeSpeed);
            if (arrow == null) return;
            arrow.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, visibility);
            if (arrowRenderer != null) arrowRenderer.enabled = visibility > 0.001f;
        }

        private void LateUpdate()
        {
            if (arrow == null || target == null) return;
            UpdateVisibility(Time.deltaTime);

            Vector3 p = AnchorPosition();
            p.y += (Mathf.Sin(Time.time * bobSpeed) * 0.5f + 0.5f) * bobAmplitude * 2f;
            arrow.position = p;

            // Plana: siempre de frente al conductor (solo gira sobre el eje vertical)
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - arrow.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 1e-4f) arrow.rotation = Quaternion.LookRotation(toCam);
            }
        }

        /// <summary>Flecha plana hacia abajo (triángulo + poste, con poco espesor); origen en la punta</summary>
        public static Mesh BuildArrowMesh()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();

            // Cara convexa con las normales hacia afuera del cuerpo (centro = centro de ese cuerpo)
            void Face(Vector3 center, params Vector3[] poly)
            {
                Vector3 c = Vector3.zero;
                foreach (var q in poly) c += q;
                c /= poly.Length;
                bool flip = Vector3.Dot(Vector3.Cross(poly[1] - poly[0], poly[2] - poly[0]), c - center) < 0f;
                int start = verts.Count;
                verts.AddRange(poly);
                for (int i = 1; i < poly.Length - 1; i++)
                {
                    tris.Add(start);
                    tris.Add(start + (flip ? i + 1 : i));
                    tris.Add(start + (flip ? i : i + 1));
                }
            }

            float z = Thickness * 0.5f;

            // Cabeza: prisma triangular (el triángulo mira al frente, +Z)
            float h = HeadHeight, w = HeadHalfWidth;
            Vector3 tipF = new Vector3(0, 0, z), tipB = new Vector3(0, 0, -z);
            Vector3 lF = new Vector3(-w, h, z), rF = new Vector3(w, h, z), lB = new Vector3(-w, h, -z), rB = new Vector3(w, h, -z);
            var headCenter = new Vector3(0f, h / 3f, 0f);
            Face(headCenter, tipF, lF, rF);                 // frente
            Face(headCenter, tipB, rB, lB);                 // atrás
            Face(headCenter, tipF, tipB, lB, lF);           // costado izquierdo
            Face(headCenter, tipF, rF, rB, tipB);           // costado derecho
            Face(headCenter, lF, lB, rB, rF);               // tapa superior

            // Poste: caja
            float s = ShaftHalfWidth, y0 = h, y1 = h + ShaftHeight;
            Vector3 a0 = new Vector3(-s, y0, z), a1 = new Vector3(s, y0, z), a2 = new Vector3(s, y0, -z), a3 = new Vector3(-s, y0, -z);
            Vector3 c0 = new Vector3(-s, y1, z), c1 = new Vector3(s, y1, z), c2 = new Vector3(s, y1, -z), c3 = new Vector3(-s, y1, -z);
            var shaftCenter = new Vector3(0f, (y0 + y1) * 0.5f, 0f);
            Face(shaftCenter, a0, a1, c1, c0);              // frente
            Face(shaftCenter, a2, a3, c3, c2);              // atrás
            Face(shaftCenter, a3, a0, c0, c3);              // izquierda
            Face(shaftCenter, a1, a2, c2, c1);              // derecha
            Face(shaftCenter, c0, c1, c2, c3);              // arriba

            var mesh = new Mesh { name = "TargetArrow" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
