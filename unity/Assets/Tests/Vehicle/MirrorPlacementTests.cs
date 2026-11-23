using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Los espejos deben coincidir con la carrocería de CADA vehículo seleccionable (sedán y pickup).
    /// Construye vehículo + origen XR + espejos de SceneSetup (por reflexión, en una escena temporal
    /// sin guardar) y mide la posición de los espejos contra los renderers reales de la carrocería.
    /// </summary>
    public class MirrorPlacementTests
    {
        const string KindPref = "VRCar.VehicleKind";
        string previousKind;

        static Type SetupType =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany_Types()
                .FirstOrDefault_Named("SceneSetup");

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

        /// <summary>Límites de la carrocería en coordenadas del vehículo (sin espejos ni ruedas).</summary>
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

            // Pegados a la carrocería: entre 0 y 25 cm fuera del costado (ni flotando ni dentro del auto)
            Assert.That(-lx - half, Is.InRange(-0.02f, 0.25f), kind + ": hueco izquierdo vs carrocería");
            Assert.That(rx - half, Is.InRange(-0.02f, 0.25f), kind + ": hueco derecho vs carrocería");
            Assert.AreEqual(-lx, rx, 0.05f, kind + ": simétricos");
        }

        [TestCase("sedan")]
        [TestCase("pickup")]
        public void InteriorMirror_IsCentred_AndBelowTheRoof(string kind)
        {
            var v = Build(kind);
            BodyExtents(v, out _, out float roofY);
            var m = v.Find("Mirror_Interior");
            Assert.NotNull(m);
            var l = v.InverseTransformPoint(m.position);
            Assert.AreEqual(0f, l.x, 0.05f, kind + ": centrado");
            Assert.Less(l.y + 0.035f, roofY, kind + ": el borde superior del vidrio debe quedar bajo el techo");
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
                Assert.Greater(z, eyeZ, kind + " " + n + ": debe estar delante de los ojos");
                Assert.Less(z, 2.5f, kind + " " + n + ": dentro del largo del vehículo");
            }
        }
    }

    static class ReflectionHelpers
    {
        public static System.Collections.Generic.IEnumerable<Type> SelectMany_Types(this Assembly[] asms)
        {
            foreach (var a in asms)
            {
                Type[] ts;
                try { ts = a.GetTypes(); } catch (ReflectionTypeLoadException e) { ts = Array.FindAll(e.Types, t => t != null); }
                foreach (var t in ts) yield return t;
            }
        }
        public static Type FirstOrDefault_Named(this System.Collections.Generic.IEnumerable<Type> ts, string name)
        {
            foreach (var t in ts) if (t.Name == name && t.Namespace == null) return t;
            return null;
        }
    }
}
