using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Tests de la geometría del espejo (reflejo del ojo y encuadre del vidrio)
    /// </summary>
    public class PlanarMirrorTests
    {
        [Test]
        public void ReflectPoint_MirrorsAcrossPlane()
        {
            Vector3 r = PlanarMirror.ReflectPoint(new Vector3(1, 2, 3), Vector3.zero, Vector3.forward);
            Assert.AreEqual(new Vector3(1, 2, -3), r);
        }

        [Test]
        public void ReflectPoint_IsSymmetric_ForTiltedPlane()
        {
            Vector3 n = new Vector3(1, 0, 1).normalized;
            Vector3 p = new Vector3(0.3f, 1.1f, -0.7f), c = new Vector3(0.5f, 1f, 0.5f);
            Vector3 r = PlanarMirror.ReflectPoint(p, c, n);
            Assert.AreEqual(Vector3.Dot(p - c, n), -Vector3.Dot(r - c, n), 1e-5f);
            Assert.AreEqual(0f, Vector3.Distance(PlanarMirror.ReflectPoint(r, c, n), p), 1e-5f);
        }

        [Test]
        public void GlassFrustum_NearPlaneIsTheGlass_AndCoversCorners()
        {
            var viewer = new GameObject("Viewer").transform;
            viewer.position = new Vector3(0, 0, -1f); // 1 m detrás del vidrio mirando +Z
            Vector3 hr = new Vector3(0.1f, 0, 0), hu = new Vector3(0, 0.05f, 0);
            Matrix4x4 m = PlanarMirror.GlassFrustum(viewer, Vector3.zero, hr, hu, 100f, out float near);
            Assert.AreEqual(1f, near, 0.01f, "El plano cercano coincide con el vidrio");

            // Las esquinas del vidrio caen exactamente en los bordes del encuadre (NDC ±1)
            Vector3 corner = viewer.InverseTransformPoint(hr + hu);
            Vector4 clip = m * new Vector4(corner.x, corner.y, -corner.z, 1f);
            Assert.AreEqual(1f, Mathf.Abs(clip.x / clip.w), 0.02f);
            Assert.AreEqual(1f, Mathf.Abs(clip.y / clip.w), 0.02f);
            Object.DestroyImmediate(viewer.gameObject);
        }

        [Test]
        public void Render_Mono_FillsBothEyes()
        {
            var camGO = new GameObject("Eye");
            var cam = camGO.AddComponent<Camera>();
            camGO.transform.position = new Vector3(0, 0, -1f);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.rotation = Quaternion.LookRotation(Vector3.forward); // cara visible mira a -Z (al ojo)
            quad.transform.localScale = new Vector3(0.2f, 0.1f, 1f);
            var mirror = quad.AddComponent<PlanarMirror>();
            mirror.eyeCamera = cam;
            mirror.Render();
            Assert.IsNotNull(mirror.LeftTexture);
            Assert.IsNotNull(mirror.RightTexture);
            Assert.AreEqual(mirror.LeftTexture.width, mirror.RightTexture.width);
            Object.DestroyImmediate(quad);
            Object.DestroyImmediate(camGO);
        }
    }
}
