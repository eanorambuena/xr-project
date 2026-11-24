using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>
    /// Evaluación del estacionamiento en paralelo.
    /// El espacio (spot) está sobre la cara de la solera, forward = sentido de la calle (+X), right = hacia la solera (-Z).
    /// Hueco de 6.3 m centrado en el origen. Auto de 1.856 x 4.255 m.
    /// </summary>
    public class ParallelParkingJudgmentTests
    {
        private GameObject spotGO;
        private Transform spot;
        private const float Gap = 6.3f, CarW = 1.856f, CarL = 4.255f;

        [SetUp]
        public void Setup()
        {
            spotGO = new GameObject("Spot");
            spot = spotGO.transform;
            spot.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            spot.position = Vector3.zero;
        }

        [TearDown] public void TearDown() { Object.DestroyImmediate(spotGO); }

        /// <summary>Esquinas del auto. alongX = posición a lo largo de la calle; curbDist = distancia del costado a la solera; yaw = giro en grados (0 = mira a +X)</summary>
        private Vector3[] Corners(float alongX, float curbDist, float yawDeg)
        {
            // la cara de la solera está en z = 0 y "hacia la solera" es -Z (right del spot): el auto queda hacia +Z
            Vector3 center = new Vector3(alongX, 0f, curbDist + CarW / 2f);
            var rot = Quaternion.Euler(0, 90f + yawDeg, 0);   // forward del auto = +X
            float hx = CarW / 2f, hz = CarL / 2f;
            return new[]
            {
                center + rot * new Vector3(-hx, 0, hz), center + rot * new Vector3(hx, 0, hz),
                center + rot * new Vector3(-hx, 0, -hz), center + rot * new Vector3(hx, 0, -hz)
            };
        }

        private ParallelParkingJudgment.Result Eval(float alongX, float curbDist, float yawDeg)
        {
            var forward = Quaternion.Euler(0, 90f + yawDeg, 0) * Vector3.forward;
            return ParallelParkingJudgment.Evaluate(Corners(alongX, curbDist, yawDeg), forward, spot, Gap);
        }

        [Test]
        public void CarCenteredAndCloseToTheCurb_IsGood()
        {
            var r = Eval(0f, 0.20f, 0f);
            Assert.IsTrue(r.InGap);
            Assert.IsTrue(r.Good);
            Assert.AreEqual(0.20f, r.CurbDistance, 0.01f);
            Assert.AreEqual((Gap - CarL) / 2f, r.FrontMargin, 0.01f);
            Assert.AreEqual((Gap - CarL) / 2f, r.RearMargin, 0.01f);
            Assert.AreEqual(0f, r.AngleDeg, 0.01f);
            Assert.AreEqual(1f, r.Overlap);
        }

        [Test]
        public void CarFarFromTheCurb_IsNotGood_ButStillInTheGap()
        {
            var r = Eval(0f, 0.80f, 0f);
            Assert.IsTrue(r.InGap);
            Assert.IsFalse(r.Good);
            Assert.AreEqual(0.80f, r.CurbDistance, 0.01f);
            StringAssert.Contains("lejos de la solera", r.Message());
        }

        [Test]
        public void CrookedCar_IsNotGood()
        {
            var r = Eval(0f, 0.25f, 12f);
            Assert.Greater(r.AngleDeg, ParallelParkingJudgment.MaxAngle);
            Assert.IsFalse(r.Good);
        }

        [Test]
        public void SmallAngle_StillCountsAsGood()
        {
            var r = Eval(0f, 0.25f, 3f);
            Assert.AreEqual(3f, r.AngleDeg, 0.05f);
            Assert.IsTrue(r.Good);
        }

        [Test]
        public void CarOverTheCurb_ReportsNegativeDistance()
        {
            var r = Eval(0f, -0.10f, 0f);
            Assert.Less(r.CurbDistance, 0f);
            Assert.IsFalse(r.InGap);
            Assert.IsFalse(r.Good);
            StringAssert.Contains("solera", r.Message());
        }

        [Test]
        public void CarShiftedForward_ShrinksTheFrontMarginAndGrowsTheRear()
        {
            var r = Eval(0.8f, 0.20f, 0f);
            Assert.AreEqual((Gap - CarL) / 2f - 0.8f, r.FrontMargin, 0.01f);
            Assert.AreEqual((Gap - CarL) / 2f + 0.8f, r.RearMargin, 0.01f);
        }

        [Test]
        public void CarSticking_OutOfTheGap_IsOutside()
        {
            var r = Eval(2.5f, 0.20f, 0f);   // se sale por delante
            Assert.IsFalse(r.InGap);
            Assert.Less(r.FrontMargin, 0f);
            Assert.Greater(r.Overlap, 0f);
            StringAssert.Contains("Fuera del espacio", r.Message());
        }

        [Test]
        public void CarFarAway_HasNoOverlap()
        {
            var r = Eval(20f, 0.20f, 0f);
            Assert.AreEqual(0f, r.Overlap);
        }

        [Test]
        public void CarFacingTheOtherWay_SwapsFrontAndRearButKeepsTheAngle()
        {
            var forward = Quaternion.Euler(0, 270f, 0) * Vector3.forward;   // mira a -X
            var corners = Corners(0.8f, 0.20f, 0f);
            var r = ParallelParkingJudgment.Evaluate(corners, forward, spot, Gap);
            Assert.AreEqual(0f, r.AngleDeg, 0.05f);
            // desplazado hacia +X mirando a -X: su "delante" es el lado -X (más margen)
            Assert.AreEqual((Gap - CarL) / 2f + 0.8f, r.FrontMargin, 0.01f);
            Assert.AreEqual((Gap - CarL) / 2f - 0.8f, r.RearMargin, 0.01f);
        }

        [Test]
        public void Message_ShowsAllTheMeasurements()
        {
            var msg = Eval(0f, 0.20f, 0f).Message();
            StringAssert.Contains("Bien estacionado", msg);
            StringAssert.Contains("Solera 20 cm", msg);
            StringAssert.Contains("Delante", msg);
            StringAssert.Contains("Atrás", msg);
        }
    }
}
