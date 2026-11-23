using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Los espejos deben coincidir con la carrocería de CADA vehículo seleccionable (sedán y pickup).
    /// Construye vehículo + origen XR + espejos de SceneSetup (por reflexión, en una escena temporal)
    /// y mide posiciones contra los renderers reales de la carrocería.
    /// </summary>
    public class MirrorPlacementTests
    {
        const string KindPref = "VRCar.VehicleKind";
        string previousKind;

        static Type SetupType =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); } })
                .FirstOrDefault(t => t.Name == "SceneSetup" && t.Namespace == null);

        [SetUp] public void SetUp() => previousKind = EditorPrefs.GetString(KindPref, "sedan");

        [TearDown]
        public void TearDown()
        {
            EditorPrefs.SetString(KindPref, previousKind);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static object Call(string method, params object[] args)
        {
            var m = SetupType.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "SceneSetup." + method + " no existe");
            return m.Invoke(null, args);
        }

        static Transform Build(string kind)
        {
            EditorPrefs.SetString(KindPref, kind);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var vehicle = (GameObject)Call("CreateVehicle");
            var cam = (Camera)Call("CreateXROrigin", vehicle.transform);
            Call("CreateMirrors", vehicle.transform, cam.transform);
            return vehicle.transform;
        }

        static void BodyExtents(Transform vehicle, out float halfWidth, out float roofY)
        {
            halfWidth = 0f; roofY = float.MinValue;
            var visual = vehicle.Find("Visual");
            Assume.That(visual, Is.Not.Null, "El vehículo no tiene 'Visual'");
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                if (r.name.Contains("Mirror") || r.name.Contains("Wheel") || r.name.Contains("Tire")) continue;
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var l = vehicle.InverseTransformPoint(c);
                    halfWidth = Mathf.Max(halfWidth, Mathf.Abs(l.x));
                    roofY = Mathf.Max(roofY, l.y);
                }
            }
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void SideMirrors_SitJustOutsideTheBodywork_AndAreSymmetric(string kind)
        {
            var v = Build(kind);
            BodyExtents(v, out float half, out _);
            var left = v.Find("Mirror_Left"); var right = v.Find("Mirror_Right");
            Assert.NotNull(left); Assert.NotNull(right);

            float lx = v.InverseTransformPoint(left.position).x, rx = v.InverseTransformPoint(right.position).x;
            Assert.Less(lx, 0f, kind + ": el espejo izquierdo debe quedar a la izquierda");
            Assert.Greater(rx, 0f, kind + ": el espejo derecho debe quedar a la derecha");
            // En el sedán el ancho medido incluye el espejo del modelo (se conserva) y el nuestro va
            // delante de él, hacia el ojo: su centro queda algo hacia dentro del borde exterior
            Assert.That(-lx - half, Is.InRange(-0.15f, 0.18f), kind + ": hueco izquierdo vs carrocería");
            Assert.That(rx - half, Is.InRange(-0.15f, 0.18f), kind + ": hueco derecho vs carrocería");
            Assert.AreEqual(-lx, rx, 0.05f, kind + ": simétricos");
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void InteriorMirror_IsCentred_AndBelowTheRoof(string kind)
        {
            var v = Build(kind);
            BodyExtents(v, out _, out float roofY);
            var l = v.InverseTransformPoint(v.Find("Mirror_Interior").position);
            Assert.AreEqual(0f, l.x, 0.05f, kind + ": centrado");
            Assert.Less(l.y + 0.035f, roofY, kind + ": el vidrio debe quedar bajo el techo");
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void InteriorMirror_Stalk_TouchesRoof_AndHousing(string kind)
        {
            var v = Build(kind);
            BodyExtents(v, out _, out float roofY);
            var stem = v.Find("Mirror_Interior_Stem");
            var housing = v.Find("Mirror_Interior_Housing");
            Assert.NotNull(stem, "falta el soporte"); Assert.NotNull(housing, "falta la carcasa");

            float stemTop = v.InverseTransformPoint(stem.position).y + stem.lossyScale.y / 2f;
            float stemBottom = v.InverseTransformPoint(stem.position).y - stem.lossyScale.y / 2f;
            float housingTop = v.InverseTransformPoint(housing.position).y + housing.lossyScale.y / 2f;

            Assert.AreEqual(roofY, stemTop, 0.03f, kind + ": el soporte debe tocar el techo");
            Assert.LessOrEqual(stemBottom, housingTop + 0.001f, kind + ": el soporte debe tocar la carcasa del espejo");
            Assert.Greater(stem.lossyScale.y, 0.01f, kind + ": soporte con largo positivo");
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void InteriorMirror_IsInDriverView_AndHidesOwnCar(string kind)
        {
            var v = Build(kind);
            var cam = v.GetComponentInChildren<Camera>();
            var m = v.Find("Mirror_Interior");
            Vector3 toMirror = m.position - cam.transform.position;
            // En un auto real el retrovisor queda ~40° a la derecha/arriba del conductor (FOV Quest 3 ~110°)
            Assert.Less(Vector3.Angle(cam.transform.forward, toMirror), 50f, kind + ": retrovisor dentro del campo de visión");
            Assert.Greater(Vector3.Dot(cam.transform.position - m.position, -m.forward), 0f, kind + ": el ojo mira la cara del vidrio");
            var pm = m.GetComponent<VRCar.Vehicle.PlanarMirror>();
            Assert.NotNull(pm.hiddenInReflection);
            Assert.Greater(pm.hiddenInReflection.Length, 0);
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void Mirrors_ReflectTheRearView_WithCorrectAngle(string kind)
        {
            var v = Build(kind);
            var cam = v.GetComponentInChildren<Camera>();
            foreach (var n in new[] { "Mirror_Left", "Mirror_Right", "Mirror_Interior" })
            {
                var m = v.Find(n);
                Vector3 normal = -m.forward; // hacia el conductor
                Vector3 toEye = (cam.transform.position - m.position).normalized;
                Assert.Greater(Vector3.Dot(toEye, normal), 0.1f, kind + " " + n + ": el ojo debe ver la cara del vidrio");
                // Rayo del ojo al espejo, reflejado: debe apuntar hacia atrás del auto (-Z local)
                Vector3 incident = -toEye;
                Vector3 reflected = Vector3.Reflect(incident, normal);
                Vector3 local = v.InverseTransformDirection(reflected);
                Assert.Less(local.z, -0.7f, kind + " " + n + ": el reflejo debe mirar hacia atrás");
                Assert.Less(Mathf.Abs(local.y), 0.35f, kind + " " + n + ": sin mirar al cielo/suelo");
            }
        }

        [Test]
        public void SedanMirrors_HideTheModelMirrors_FromTheDriverSeat()
        {
            var v = Build("sedan");
            var cam = v.GetComponentInChildren<Camera>();
            Vector3 eye = cam.transform.position;
            var body = v.GetComponentsInChildren<MeshFilter>().First(f => f.name == "NormalCar1_Cube.012");
            var sedan = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); } })
                .First(t => t.Name == "SedanModel" && t.Namespace == null);
            var hulls = (System.Collections.Generic.List<Vector3[]>)sedan.GetMethod("ModelMirrors").Invoke(null, new object[] { body.sharedMesh });
            Assert.AreEqual(2, hulls.Count, "el modelo tiene 2 espejos laterales (se conservan)");
            foreach (var hull in hulls)
            {
                Vector3 first = body.transform.TransformPoint(hull[0]);
                var m = v.Find(v.InverseTransformPoint(first).x < 0 ? "Mirror_Left" : "Mirror_Right");
                foreach (var p in hull)
                {
                    // El rayo del ojo a cada punto del espejo del modelo debe cruzar nuestro vidrio antes
                    Vector3 w = body.transform.TransformPoint(p);
                    var plane = new Plane(-m.forward, m.position);
                    Assert.IsTrue(plane.Raycast(new Ray(eye, w - eye), out float dist), m.name + ": el rayo cruza el plano del vidrio");
                    Assert.Less(dist, Vector3.Distance(eye, w), m.name + ": el vidrio queda delante del espejo del modelo");
                    var local = m.InverseTransformPoint(eye + (w - eye).normalized * dist);
                    Assert.LessOrEqual(Mathf.Abs(local.x), 0.5f, m.name + ": cubre el ancho");
                    Assert.LessOrEqual(Mathf.Abs(local.y), 0.5f, m.name + ": cubre el alto");
                }
            }
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void Mirrors_HaveShaderAndEye(string kind)
        {
            var v = Build(kind);
            foreach (var n in new[] { "Mirror_Left", "Mirror_Right", "Mirror_Interior" })
            {
                var pm = v.Find(n).GetComponent<VRCar.Vehicle.PlanarMirror>();
                Assert.NotNull(pm.mirrorShader, kind + " " + n + ": falta el shader VRCar/StereoMirror");
                Assert.NotNull(pm.eyeCamera, kind + " " + n + ": falta la cámara del ojo");
            }
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void Mirrors_AreInsideVehicleLength_AndAheadOfDriverEyes(string kind)
        {
            var v = Build(kind);
            var cam = v.GetComponentInChildren<Camera>();
            float eyeZ = v.InverseTransformPoint(cam.transform.position).z;
            foreach (var n in new[] { "Mirror_Left", "Mirror_Right", "Mirror_Interior" })
            {
                float z = v.InverseTransformPoint(v.Find(n).position).z;
                Assert.Greater(z, eyeZ, kind + " " + n + ": delante de los ojos");
                Assert.Less(z, 2.5f, kind + " " + n + ": dentro del largo del vehículo");
            }
        }
    }
}
