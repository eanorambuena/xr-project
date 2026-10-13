using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>
    /// TDD Tests para la evaluación de estacionamiento perpendicular
    /// Espacio: 2.5 m x 5.0 m, centrado en el origen, tope hacia +Z.
    /// Auto: 1.856 m x 4.255 m (Peugeot 308).
    /// </summary>
    public class ParkingJudgmentTests
    {
        private GameObject spotGO;
        private const float W = 2.5f, L = 5.0f, CarW = 1.856f, CarL = 4.255f;

        [SetUp] public void Setup() { spotGO = new GameObject("Spot"); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(spotGO); }

        private static Vector3[] Corners(Vector3 center, float yawDeg)
        {
            var rot = Quaternion.Euler(0, yawDeg, 0);
            float hx = CarW / 2f, hz = CarL / 2f;
            return new[]
            {
                center + rot * new Vector3(-hx, 0, hz), center + rot * new Vector3(hx, 0, hz),
                center + rot * new Vector3(-hx, 0, -hz), center + rot * new Vector3(hx, 0, -hz)
            };
        }

        [Test]
        public void Centered_Aligned_IsGood_WithEqualMargins()
        {
            var r = ParkingJudgment.Evaluate(Corners(Vector3.zero, 0), Vector3.forward, spotGO.transform, W, L);
            Assert.IsTrue(r.Inside);
            Assert.IsTrue(r.Good);
            Assert.AreEqual((W - CarW) / 2f, r.LeftMargin, 0.001f);
            Assert.AreEqual((W - CarW) / 2f, r.RightMargin, 0.001f);
            Assert.AreEqual((L - CarL) / 2f, r.EndMargin, 0.001f);
            Assert.AreEqual(0f, r.AngleDeg, 0.01f);
        }

        [Test]
        public void ShiftedRight_ReducesRightMargin()
        {
            var r = ParkingJudgment.Evaluate(Corners(new Vector3(0.2f, 0, 0), 0), Vector3.forward, spotGO.transform, W, L);
            Assert.AreEqual((W - CarW) / 2f - 0.2f, r.RightMargin, 0.001f);
            Assert.AreEqual((W - CarW) / 2f + 0.2f, r.LeftMargin, 0.001f);
        }

        [Test]
        public void CrossingLine_IsNotInside_AndMarginNegative()
        {
            var r = ParkingJudgment.Evaluate(Corners(new Vector3(0.5f, 0, 0), 0), Vector3.forward, spotGO.transform, W, L);
            Assert.IsFalse(r.Inside);
            Assert.Less(r.RightMargin, 0f);
            Assert.IsFalse(r.Good);
        }

        [Test]
        public void Rotated_ReportsAngle()
        {
            var r = ParkingJudgment.Evaluate(Corners(Vector3.zero, 4f), Quaternion.Euler(0, 4f, 0) * Vector3.forward, spotGO.transform, W, L);
            Assert.AreEqual(4f, r.AngleDeg, 0.05f);
        }

        [Test]
        public void ReversedIn_AngleIsSmall()
        {
            // Entrar en reversa (auto mirando a -Z) también es válido
            var r = ParkingJudgment.Evaluate(Corners(Vector3.zero, 180f), Vector3.back, spotGO.transform, W, L);
            Assert.AreEqual(0f, r.AngleDeg, 0.05f);
            Assert.IsTrue(r.Good);
        }

        [Test]
        public void ReversedIn_LeftRight_AreFromDriverPerspective()
        {
            // Entró en reversa (mirando a -Z) desplazado hacia +X: +X es la IZQUIERDA del conductor
            var r = ParkingJudgment.Evaluate(Corners(new Vector3(0.2f, 0, 0), 180f), Vector3.back, spotGO.transform, W, L);
            Assert.AreEqual((W - CarW) / 2f - 0.2f, r.LeftMargin, 0.001f);
            Assert.AreEqual((W - CarW) / 2f + 0.2f, r.RightMargin, 0.001f);
        }

        [Test]
        public void TooCrooked_IsNotGood()
        {
            var r = ParkingJudgment.Evaluate(Corners(Vector3.zero, 8f), Quaternion.Euler(0, 8f, 0) * Vector3.forward, spotGO.transform, W, L);
            Assert.IsFalse(r.Good);
        }

        [Test]
        public void Message_ShowsMarginsInCm()
        {
            var r = ParkingJudgment.Evaluate(Corners(Vector3.zero, 0), Vector3.forward, spotGO.transform, W, L);
            string msg = r.Message();
            StringAssert.Contains("Bien estacionado", msg);
            StringAssert.Contains("32 cm", msg); // (2.5 - 1.856)/2 = 0.322 m
        }
    }
}
