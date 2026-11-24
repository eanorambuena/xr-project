using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>Flecha flotante sobre el espacio objetivo</summary>
    public class TargetMarkerTests
    {
        private GameObject go, spotGO;
        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
            if (spotGO != null) Object.DestroyImmediate(spotGO);
        }

        [Test]
        public void Mesh_HasGeometry_AndStartsAtTheTipPointingDown()
        {
            var mesh = TargetMarker.BuildArrowMesh();
            Assert.Greater(mesh.vertexCount, 12);
            Assert.AreEqual(0, mesh.triangles.Length % 3);
            var b = mesh.bounds;
            Assert.AreEqual(0f, b.min.y, 1e-4f, "La punta está en el origen");
            Assert.AreEqual(TargetMarker.HeadHeight + TargetMarker.ShaftHeight, b.max.y, 1e-4f, "El poste queda arriba de la punta");
            Assert.AreEqual(TargetMarker.HeadHalfWidth * 2f, b.size.x, 1e-4f);
            Assert.AreEqual(TargetMarker.Thickness, b.size.z, 1e-4f, "Plana: casi 2D");
            Assert.Less(b.size.z, b.size.x / 5f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Mesh_AllFacesPointOutward()
        {
            var mesh = TargetMarker.BuildArrowMesh();
            var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                // Cada cuerpo es convexo: la normal debe apuntar lejos del eje central de la flecha o hacia arriba/abajo de su tapa
                Vector3 axisPoint = new Vector3(0f, centroid.y, 0f);
                Vector3 outward = centroid - axisPoint;
                if (outward.sqrMagnitude > 1e-6f) Assert.GreaterOrEqual(Vector3.Dot(n, outward), -1e-6f, $"Triángulo {i / 3} mira hacia adentro");
            }
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void AnchorPosition_IsAboveTheSpotCentre_UsingTheLocalOffset()
        {
            spotGO = new GameObject("Spot");
            spotGO.transform.SetPositionAndRotation(new Vector3(10f, 0f, -5f), Quaternion.Euler(0f, 90f, 0f));
            go = new GameObject("Marker");
            var marker = go.AddComponent<TargetMarker>();
            marker.Configure(spotGO.transform, new Vector3(-1.15f, 0f, 0f), null);
            Vector3 expected = spotGO.transform.TransformPoint(new Vector3(-1.15f, 0f, 0f));
            Vector3 p = marker.AnchorPosition();
            Assert.AreEqual(expected.x, p.x, 1e-4f);
            Assert.AreEqual(expected.z, p.z, 1e-4f);
            Assert.Greater(p.y, 1.8f, "Flota por encima del techo de los autos vecinos");
        }

        private class FakeZone : IParkingZone { public float VehicleOverlap { get; set; } }

        [Test]
        public void NextEntered_HidesAsSoonAsOneCornerCrossesTheLine_AndShowsAgainOnlyWhenTheCarLeft()
        {
            Assert.IsFalse(TargetMarker.NextEntered(false, 0f), "Auto afuera: se ve");
            Assert.IsTrue(TargetMarker.NextEntered(false, 0.25f), "Una esquina adentro (1 de 4): se oculta");
            Assert.IsTrue(TargetMarker.NextEntered(true, 0.25f), "Histéresis: no parpadea al borde");
            Assert.IsTrue(TargetMarker.NextEntered(true, 1f));
            Assert.IsFalse(TargetMarker.NextEntered(true, 0f), "Si el auto salió por completo, vuelve a mostrarse");
        }

        [Test]
        public void Build_UsesABrightEmissiveCopyOfTheMaterial()
        {
            go = new GameObject("Marker");
            var marker = go.AddComponent<TargetMarker>();
            var src = new Material(Shader.Find("Standard")) { color = Color.gray };
            marker.Configure(null, Vector3.zero, src);
            var mat = marker.Build().GetComponent<MeshRenderer>().sharedMaterial;
            Assert.AreNotSame(src, mat, "No modifica el material guardado en el proyecto");
            Assert.AreEqual(Color.gray, src.color);
            Color got = mat.color, want = TargetMarker.ArrowColor;
            Assert.AreEqual(want.r, got.r, 1e-3f); Assert.AreEqual(want.g, got.g, 1e-3f);
            Assert.AreEqual(want.b, got.b, 1e-3f); Assert.AreEqual(want.a, got.a, 1e-3f);
            Assert.Greater(mat.GetColor("_EmissionColor").maxColorComponent, 0.5f, "Emite luz propia");
            Object.DestroyImmediate(src);
        }

        [Test]
        public void UpdateVisibility_FadesOutWhenTheCarEnters_AndBackWhenItLeaves()
        {
            go = new GameObject("Marker");
            var marker = go.AddComponent<TargetMarker>();
            var arrow = marker.Build();
            var zone = new FakeZone();
            marker.SetZone(zone);

            marker.UpdateVisibility(0.1f);
            Assert.AreEqual(1f, marker.Visibility, 1e-4f, "Auto afuera: visible");
            Assert.IsFalse(marker.Entered);

            zone.VehicleOverlap = 1f;
            for (int i = 0; i < 20; i++) marker.UpdateVisibility(0.1f);
            Assert.IsTrue(marker.Entered);
            Assert.AreEqual(0f, marker.Visibility, 1e-4f);
            Assert.IsFalse(arrow.GetComponent<MeshRenderer>().enabled, "Oculta del todo");

            zone.VehicleOverlap = 0f;
            for (int i = 0; i < 20; i++) marker.UpdateVisibility(0.1f);
            Assert.AreEqual(1f, marker.Visibility, 1e-4f);
            Assert.IsTrue(arrow.GetComponent<MeshRenderer>().enabled);
        }

        [Test]
        public void Build_CreatesTheArrowOnce_WithoutColliders()
        {
            go = new GameObject("Marker");
            var marker = go.AddComponent<TargetMarker>();
            var first = marker.Build();
            Assert.AreSame(first, marker.Build());
            Assert.AreEqual(1, go.transform.childCount);
            Assert.IsNull(first.GetComponent<Collider>());
            Assert.IsNotNull(first.GetComponent<MeshRenderer>().sharedMaterial);
        }
    }
}
