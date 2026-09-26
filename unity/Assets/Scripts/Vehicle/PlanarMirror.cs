using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Espejo plano físicamente correcto: renderiza la escena desde el ojo reflejado
    /// con una proyección fuera de eje que pasa exactamente por el rectángulo del espejo
    /// (proyección en perspectiva generalizada). Así lo que se ve en el espejo depende
    /// de dónde está el ojo del conductor, igual que en un auto real.
    ///
    /// El espejo es un Quad (1x1 local) escalado al tamaño real del vidrio. La cara visible
    /// de un Quad de Unity mira hacia su -Z local, así que transform.forward debe apuntar
    /// en sentido contrario al conductor (-forward = normal del vidrio hacia el conductor).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class PlanarMirror : MonoBehaviour
    {
        [Tooltip("Ojo del conductor (cámara principal). Si está vacío se usa Camera.main")]
        public Transform eye;
        [Tooltip("Resolución vertical de la textura del espejo (px)")]
        public int textureHeight = 256;
        public LayerMask cullingMask = ~0;

        private Camera mirrorCamera;
        private RenderTexture texture;
        private Material material;

        public Camera MirrorCamera => mirrorCamera;

        private void OnEnable()
        {
            EnsureSetup();
        }

        private void OnDisable()
        {
            if (texture != null) texture.Release();
        }

        private void OnDestroy()
        {
            if (mirrorCamera != null) DestroyImmediate(mirrorCamera.gameObject);
            if (texture != null) DestroyImmediate(texture);
            if (material != null) DestroyImmediate(material);
        }

        public void EnsureSetup()
        {
            if (mirrorCamera == null)
            {
                // Sin padre: el Quad está escalado y eso distorsionaría la cámara
                var go = new GameObject(name + " Camera") { hideFlags = HideFlags.HideAndDontSave };
                mirrorCamera = go.AddComponent<Camera>();
                mirrorCamera.enabled = false; // se renderiza manualmente en LateUpdate
                mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;
            }
            float aspect = Mathf.Max(0.01f, transform.lossyScale.x / Mathf.Max(0.01f, transform.lossyScale.y));
            int w = Mathf.Max(16, Mathf.RoundToInt(textureHeight * aspect));
            if (texture == null || texture.width != w || texture.height != textureHeight)
            {
                if (texture != null) texture.Release();
                texture = new RenderTexture(w, textureHeight, 24) { name = name + " RT", antiAliasing = 2 };
            }
            mirrorCamera.targetTexture = texture;
            mirrorCamera.cullingMask = cullingMask;

            if (material == null)
            {
                material = new Material(Shader.Find("Unlit/Texture")) { name = name + " Mat" };
                // Espejo: la imagen se invierte horizontalmente
                material.mainTextureScale = new Vector2(-1f, 1f);
                material.mainTextureOffset = new Vector2(1f, 0f);
                GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            material.mainTexture = texture;
        }

        private void LateUpdate()
        {
            Render();
        }

        /// <summary>Renderiza el reflejo para la posición actual del ojo</summary>
        public void Render()
        {
            EnsureSetup();
            Transform e = eye != null ? eye : (Camera.main != null ? Camera.main.transform : null);
            if (e == null) return;

            // Plano del espejo: pasa por el centro, normal = hacia el conductor (-forward del Quad)
            Vector3 center = transform.position;
            Vector3 normal = -transform.forward;
            Vector3 eyePos = e.position;
            if (Vector3.Dot(eyePos - center, normal) <= 0f) return; // el ojo está detrás del espejo

            Vector3 reflectedEye = ReflectPoint(eyePos, center, normal);

            // La cámara reflejada mira a través del vidrio hacia el lado del conductor
            mirrorCamera.transform.SetPositionAndRotation(reflectedEye, Quaternion.LookRotation(normal, transform.up));

            // Frustum fuera de eje que pasa exactamente por las esquinas del vidrio
            Vector3 hx = transform.right * transform.lossyScale.x * 0.5f;
            Vector3 hy = transform.up * transform.lossyScale.y * 0.5f;
            Vector3 c0 = mirrorCamera.transform.InverseTransformPoint(center - hx - hy);
            Vector3 c1 = mirrorCamera.transform.InverseTransformPoint(center + hx + hy);
            float d = Mathf.Max(0.001f, c0.z);
            float near = Mathf.Max(0.005f, d - 0.001f); // recorta todo lo que está "detrás" del espejo
            float far = 300f;
            float l = Mathf.Min(c0.x, c1.x) * near / d, r = Mathf.Max(c0.x, c1.x) * near / d;
            float b = Mathf.Min(c0.y, c1.y) * near / d, t = Mathf.Max(c0.y, c1.y) * near / d;
            mirrorCamera.nearClipPlane = near;
            mirrorCamera.farClipPlane = far;
            mirrorCamera.projectionMatrix = Matrix4x4.Frustum(l, r, b, t, near, far);
            mirrorCamera.Render();
        }

        public static Vector3 ReflectPoint(Vector3 p, Vector3 planePoint, Vector3 planeNormal)
        {
            planeNormal.Normalize();
            float d = Vector3.Dot(p - planePoint, planeNormal);
            return p - 2f * d * planeNormal;
        }

    }
}
