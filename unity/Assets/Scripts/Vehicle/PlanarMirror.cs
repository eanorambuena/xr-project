using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Espejo plano físicamente correcto y reutilizable.
    /// Técnica: por cada ojo se renderiza la escena desde el ojo REFLEJADO en el plano del vidrio,
    /// con un frustum fuera de eje que pasa exactamente por las 4 esquinas del vidrio.
    /// - En VR se calcula un reflejo por ojo (estéreo): el espejo tiene profundidad real.
    /// - Fuera de VR se calcula uno solo y se usa para ambos.
    /// El espejo es un Quad (1x1 local) escalado al tamaño real del vidrio. La cara visible
    /// de un Quad de Unity mira hacia su -Z local, así que -forward = normal hacia el conductor.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class PlanarMirror : MonoBehaviour
    {
        [Tooltip("Cámara del conductor. Si está vacía se usa Camera.main")]
        public Camera eyeCamera;
        [Tooltip("Compatibilidad: transform del ojo (si no hay cámara)")]
        public Transform eye;
        [Tooltip("Resolución vertical de la textura por ojo (px)")]
        public int textureHeight = 256;
        public LayerMask cullingMask = ~0;
        public Shader mirrorShader;

        private Camera mirrorCamera;
        private RenderTexture leftTexture, rightTexture;
        private Material material;

        public RenderTexture LeftTexture => leftTexture;
        public RenderTexture RightTexture => rightTexture;

        private void OnEnable() { EnsureSetup(); }

        private void OnDisable()
        {
            if (leftTexture != null) leftTexture.Release();
            if (rightTexture != null) rightTexture.Release();
        }

        private void OnDestroy()
        {
            if (mirrorCamera != null) DestroyImmediate(mirrorCamera.gameObject);
            if (leftTexture != null) DestroyImmediate(leftTexture);
            if (rightTexture != null) DestroyImmediate(rightTexture);
            if (material != null) DestroyImmediate(material);
        }

        private RenderTexture MakeTexture(RenderTexture current, string suffix)
        {
            float aspect = Mathf.Max(0.01f, transform.lossyScale.x / Mathf.Max(0.01f, transform.lossyScale.y));
            int w = Mathf.Max(16, Mathf.RoundToInt(textureHeight * aspect));
            if (current != null && current.width == w && current.height == textureHeight) return current;
            if (current != null) current.Release();
            return new RenderTexture(w, textureHeight, 24) { name = name + suffix, antiAliasing = 2 };
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
            mirrorCamera.cullingMask = cullingMask;
            leftTexture = MakeTexture(leftTexture, " RT L");
            rightTexture = MakeTexture(rightTexture, " RT R");

            if (material == null)
            {
                var shader = mirrorShader != null ? mirrorShader : Shader.Find("VRCar/StereoMirror");
                material = new Material(shader) { name = name + " Mat" };
                GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            material.SetTexture("_LeftTex", leftTexture);
            material.SetTexture("_RightTex", rightTexture);
        }

        private void LateUpdate() { Render(); }

        /// <summary>Renderiza el reflejo para la posición actual de los ojos</summary>
        public void Render()
        {
            EnsureSetup();
            Camera cam = eyeCamera != null ? eyeCamera : Camera.main;
            if (cam != null && cam.stereoEnabled)
            {
                RenderFrom(EyePosition(cam, Camera.StereoscopicEye.Left), leftTexture);
                RenderFrom(EyePosition(cam, Camera.StereoscopicEye.Right), rightTexture);
                return;
            }
            Transform e = cam != null ? cam.transform : eye;
            if (e == null) return;
            RenderFrom(e.position, leftTexture);
            Graphics.CopyTexture(leftTexture, rightTexture);
        }

        /// <summary>Posición de un ojo en mundo a partir de su matriz de vista</summary>
        public static Vector3 EyePosition(Camera cam, Camera.StereoscopicEye which)
        {
            Matrix4x4 view = cam.GetStereoViewMatrix(which);
            return view.inverse.MultiplyPoint(Vector3.zero);
        }

        private void RenderFrom(Vector3 eyePos, RenderTexture target)
        {
            // Plano del espejo: pasa por el centro, normal = hacia el conductor (-forward del Quad)
            Vector3 center = transform.position;
            Vector3 normal = -transform.forward;
            if (Vector3.Dot(eyePos - center, normal) <= 0f) return; // el ojo está detrás del espejo

            Vector3 reflectedEye = ReflectPoint(eyePos, center, normal);
            mirrorCamera.transform.SetPositionAndRotation(reflectedEye, Quaternion.LookRotation(normal, transform.up));
            mirrorCamera.projectionMatrix = GlassFrustum(mirrorCamera.transform, center,
                transform.right * transform.lossyScale.x * 0.5f, transform.up * transform.lossyScale.y * 0.5f, 300f, out float near);
            mirrorCamera.nearClipPlane = near;
            mirrorCamera.farClipPlane = 80f; // el estacionamiento completo cabe; menos geometría por espejo (Quest)
            mirrorCamera.targetTexture = target;
            // Sin sombras en el reflejo: en un espejo chico no se notan y ahorran un pase de sombras por ojo
            var prevShadows = QualitySettings.shadows;
            QualitySettings.shadows = ShadowQuality.Disable;
            mirrorCamera.Render();
            QualitySettings.shadows = prevShadows;
        }

        /// <summary>
        /// Frustum fuera de eje cuyo plano cercano coincide con el vidrio: recorta todo lo que
        /// queda "detrás" del espejo y encuadra exactamente sus 4 esquinas.
        /// </summary>
        public static Matrix4x4 GlassFrustum(Transform viewer, Vector3 center, Vector3 halfRight, Vector3 halfUp, float far, out float near)
        {
            Vector3 c0 = viewer.InverseTransformPoint(center - halfRight - halfUp);
            Vector3 c1 = viewer.InverseTransformPoint(center + halfRight + halfUp);
            float d = Mathf.Max(0.001f, c0.z);
            near = Mathf.Max(0.005f, d - 0.001f);
            float l = Mathf.Min(c0.x, c1.x) * near / d, r = Mathf.Max(c0.x, c1.x) * near / d;
            float b = Mathf.Min(c0.y, c1.y) * near / d, t = Mathf.Max(c0.y, c1.y) * near / d;
            return Matrix4x4.Frustum(l, r, b, t, near, far);
        }

        public static Vector3 ReflectPoint(Vector3 p, Vector3 planePoint, Vector3 planeNormal)
        {
            planeNormal.Normalize();
            float d = Vector3.Dot(p - planePoint, planeNormal);
            return p - 2f * d * planeNormal;
        }
    }
}
