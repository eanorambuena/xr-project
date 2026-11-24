using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>Ancho del espacio A4 por dificultad (estacionamiento en batería)</summary>
    public class PerpendicularParkingCourseTests
    {
        private GameObject go;
        [TearDown] public void TearDown() { if (go != null) Object.DestroyImmediate(go); }

        private const float Cx = 1.25f;

        private Transform Child(string name, float x, float z = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(go.transform, false);
            t.localPosition = new Vector3(x, 0f, z);
            return t;
        }

        [Test]
        public void Widths_ShrinkWithDifficulty_AndAllFitTheCar()
        {
            float easy = PerpendicularParkingCourse.WidthFor(0), medium = PerpendicularParkingCourse.WidthFor(1), hard = PerpendicularParkingCourse.WidthFor(2);
            Assert.Greater(easy, medium); Assert.Greater(medium, hard);
            Assert.AreEqual(PerpendicularParkingCourse.BaseWidth, medium, 1e-4f, "Media = espacio original");
            Assert.Greater(hard, 1.856f + 0.2f, "Aun el más difícil deja pasar al auto con holgura");
            Assert.AreEqual(hard, PerpendicularParkingCourse.WidthFor(99));
            Assert.AreEqual(easy, PerpendicularParkingCourse.WidthFor(-1));
        }

        [Test]
        public void Apply_MovesLinesEndLineAndNeighbours_SymmetricallyAroundTheSpot()
        {
            go = new GameObject("ParkingLot");
            var spot = Child("TargetSpot", Cx, 2.5f);
            var left = Child("Line_R0_3", Cx - 1.25f); var right = Child("Line_R0_4", Cx + 1.25f);
            var end = Child("Line_Target_End", Cx); end.localScale = new Vector3(2.5f, 0.005f, 0.1f);
            var carL = Child("ParkedCar", Cx - 2.5f + 0.1f, 2.6f); var carR = Child("ParkedCar", Cx + 2.5f - 0.1f, 2.6f);
            var course = go.AddComponent<PerpendicularParkingCourse>();
            course.Configure(spot, left, right, end, new[] { carL }, new[] { carR }, null);

            foreach (int d in new[] { 0, 1, 2, 0 }) // repetir 0 comprueba que no se acumula el desplazamiento
            {
                course.Apply(d);
                float w = PerpendicularParkingCourse.WidthFor(d), delta = (w - 2.5f) / 2f;
                Assert.AreEqual(w, course.CurrentWidth, 1e-4f);
                Assert.AreEqual(w, right.localPosition.x - left.localPosition.x, 1e-4f, "Las líneas distan el ancho del espacio");
                Assert.AreEqual(Cx, (right.localPosition.x + left.localPosition.x) / 2f, 1e-4f, "Centradas en el espacio");
                Assert.AreEqual(w, end.localScale.x, 1e-4f);
                Assert.AreEqual(Cx - 2.5f + 0.1f - delta, carL.localPosition.x, 1e-4f);
                Assert.AreEqual(Cx + 2.5f - 0.1f + delta, carR.localPosition.x, 1e-4f);
                Assert.AreEqual(2.6f, carL.localPosition.z, 1e-4f, "No cambia la profundidad");
            }
        }

        [Test]
        public void Apply_PassesTheWidthToTheJudgment()
        {
            go = new GameObject("ParkingLot");
            var spot = Child("TargetSpot", Cx, 2.5f);
            var judgment = go.AddComponent<ParkingJudgment>();
            var course = go.AddComponent<PerpendicularParkingCourse>();
            course.Configure(spot, null, null, null, null, null, judgment);
            course.Apply(2);
            Assert.AreEqual(PerpendicularParkingCourse.WidthFor(2), judgment.SpotWidth, 1e-4f);
        }

        [Test]
        public void Apply_WithoutPieces_DoesNotThrow()
        {
            go = new GameObject("ParkingLot");
            var course = go.AddComponent<PerpendicularParkingCourse>();
            Assert.DoesNotThrow(() => course.Apply(1)); // sin TargetSpot: busca por nombre y no encuentra nada
        }
    }
}
