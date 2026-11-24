using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>Precisión: fracción del auto que queda dentro del rectángulo de estacionamiento</summary>
    public class ParkingAccuracyTests
    {
        private GameObject spotGO;
        private Transform spot;
        private const float CarW = 1.856f, CarL = 4.255f;
        // Rectángulo de 2.5 x 5.0 m centrado en el origen
        private const float MinX = -1.25f, MaxX = 1.25f, MinZ = -2.5f, MaxZ = 2.5f;

        [SetUp] public void Setup() { spotGO = new GameObject("Spot"); spot = spotGO.transform; }
        [TearDown] public void TearDown() { Object.DestroyImmediate(spotGO); }

        /// <summary>Esquinas en el orden de ParkingJudgment.VehicleCorners (delantera izq., delantera der., trasera izq., trasera der.)</summary>
        private static Vector3[] Corners(float x, float z, float yawDeg)
        {
            var rot = Quaternion.Euler(0, yawDeg, 0);
            var c = new Vector3(x, 0f, z);
            float hx = CarW / 2f, hz = CarL / 2f;
            return new[]
            {
                c + rot * new Vector3(-hx, 0, hz), c + rot * new Vector3(hx, 0, hz),
                c + rot * new Vector3(-hx, 0, -hz), c + rot * new Vector3(hx, 0, -hz)
            };
        }

        private float Fraction(float x, float z, float yaw) =>
            ParkingAccuracy.InsideFraction(Corners(x, z, yaw), spot, MinX, MaxX, MinZ, MaxZ);

        [Test] public void CarCompletelyInside_IsOne() => Assert.AreEqual(1f, Fraction(0f, 0f, 0f), 1e-4f);

        [Test] public void CarCompletelyOutside_IsZero() => Assert.AreEqual(0f, Fraction(10f, 0f, 0f), 1e-4f);

        [Test]
        public void CarHalfOverALine_IsTheFractionOfItsArea()
        {
            // Desplazado 1 m hacia +x: ocupa x ∈ [0.072, 1.928]; dentro hasta 1.25
            float expected = (MaxX - (1f - CarW / 2f)) / CarW;
            Assert.AreEqual(expected, Fraction(1f, 0f, 0f), 1e-3f);
        }

        [Test]
        public void CarStickingOutTheBack_LosesThatPart()
        {
            // El auto mide 4.255 y el rectángulo llega a z = 2.5: con el centro en z = 1 sobresalen 0.6275 m
            float expected = (CarL - ((1f + CarL / 2f) - MaxZ)) / CarL;
            Assert.AreEqual(expected, Fraction(0f, 1f, 0f), 1e-3f);
        }

        [Test]
        public void CarAcrossTheSpot_IsOnlyPartlyInside()
        {
            // Atravesado: ocupa x ∈ [-2.1275, 2.1275] y el rectángulo solo tiene 2.5 de ancho
            Assert.AreEqual(2.5f / CarL, Fraction(0f, 0f, 90f), 1e-3f);
        }

        [Test]
        public void ItDoesNotMatterWhichWayTheCarFaces()
        {
            Assert.AreEqual(Fraction(0.3f, 0.2f, 0f), Fraction(0.3f, 0.2f, 180f), 1e-4f);
        }

        [Test]
        public void ItUsesTheLocalFrameOfTheSpot()
        {
            spot.position = new Vector3(20f, 0f, 5f);
            spot.rotation = Quaternion.Euler(0, 90f, 0);
            // Auto alineado con el espacio, en su centro
            var rot = Quaternion.Euler(0, 90f, 0);
            var c = spot.position;
            float hx = CarW / 2f, hz = CarL / 2f;
            var corners = new[] { c + rot * new Vector3(-hx, 0, hz), c + rot * new Vector3(hx, 0, hz), c + rot * new Vector3(-hx, 0, -hz), c + rot * new Vector3(hx, 0, -hz) };
            Assert.AreEqual(1f, ParkingAccuracy.InsideFraction(corners, spot, MinX, MaxX, MinZ, MaxZ), 1e-4f);
        }

        [Test]
        public void BadInput_IsZero()
        {
            Assert.AreEqual(0f, ParkingAccuracy.InsideFraction(null, spot, MinX, MaxX, MinZ, MaxZ));
            Assert.AreEqual(0f, ParkingAccuracy.InsideFraction(new Vector3[3], spot, MinX, MaxX, MinZ, MaxZ));
        }

        // ---- Las dos pruebas con los juicios reales ----
        private GameObject MakeCar(Vector3 pos, float yaw)
        {
            var car = new GameObject("Car");
            car.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var box = car.AddComponent<BoxCollider>();
            box.size = new Vector3(CarW, 1.5f, CarL);
            return car;
        }

        [Test]
        public void PerpendicularJudgment_AccuracyFollowsTheCar()
        {
            var j = new GameObject("J").AddComponent<ParkingJudgment>();
            var car = MakeCar(Vector3.zero, 0f);
            j.Configure(spot, 2.5f, 5.0f, car.GetComponent<BoxCollider>(), null);
            Assert.AreEqual(1f, j.Accuracy, 1e-3f);
            car.transform.position = new Vector3(1f, 0f, 0f);
            Assert.Less(j.Accuracy, 0.7f);
            Assert.Greater(j.Accuracy, 0.5f);
            Object.DestroyImmediate(car); Object.DestroyImmediate(j.gameObject);
        }

        [Test]
        public void PerpendicularJudgment_AlignmentAndCentering()
        {
            var j = new GameObject("J").AddComponent<ParkingJudgment>();
            var car = MakeCar(Vector3.zero, 0f);
            j.Configure(spot, 2.5f, 5.0f, car.GetComponent<BoxCollider>(), null);
            Assert.AreEqual(0f, j.AngleDegrees, 0.01f);
            Assert.AreEqual(0f, j.CenterOffset, 0.01f);

            car.transform.position = new Vector3(0.2f, 0f, 0f);          // 20 cm hacia un lado
            Assert.AreEqual(0.2f, j.CenterOffset, 0.01f);
            car.transform.position = new Vector3(-0.2f, 0f, 0f);         // y hacia el otro: mismo descentrado
            Assert.AreEqual(0.2f, j.CenterOffset, 0.01f);

            car.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 5f, 0));
            Assert.AreEqual(5f, j.AngleDegrees, 0.05f);
            car.transform.rotation = Quaternion.Euler(0, 180f - 4f, 0);   // entró en reversa y un poco chueco
            Assert.AreEqual(4f, j.AngleDegrees, 0.05f);
            Object.DestroyImmediate(car); Object.DestroyImmediate(j.gameObject);
        }

        [Test]
        public void ParallelJudgment_AlignmentAndCentering()
        {
            spot.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            var j = new GameObject("J").AddComponent<ParallelParkingJudgment>();
            var car = MakeCar(new Vector3(0f, 0f, 0.2f + CarW / 2f), 90f);
            j.Configure(spot, 6.3f, car.GetComponent<BoxCollider>(), null);
            Assert.AreEqual(0f, j.AngleDegrees, 0.01f);
            Assert.AreEqual(0f, j.CenterOffset, 0.01f);

            car.transform.position = new Vector3(0.5f, 0f, 0.2f + CarW / 2f);   // corrido 50 cm a lo largo de la calle
            Assert.AreEqual(0.5f, j.CenterOffset, 0.01f);
            car.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0.2f + CarW / 2f), Quaternion.Euler(0, 90f + 6f, 0));
            Assert.AreEqual(6f, j.AngleDegrees, 0.05f);
            Object.DestroyImmediate(car); Object.DestroyImmediate(j.gameObject);
        }

        [Test]
        public void ParallelJudgment_AccuracyUsesThePaintedBay()
        {
            spot.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);   // right del espacio = hacia la solera (-Z)
            var j = new GameObject("J").AddComponent<ParallelParkingJudgment>();
            var car = MakeCar(new Vector3(0f, 0f, 0.2f + CarW / 2f), 90f);   // a 20 cm de la solera
            j.Configure(spot, 6.3f, car.GetComponent<BoxCollider>(), null);
            Assert.AreEqual(1f, j.Accuracy, 1e-3f);
            car.transform.position = new Vector3(0f, 0f, 1.0f + CarW / 2f);   // a 1 m de la solera: pisa la línea exterior
            float expected = (ParallelParkingJudgment.PaintedBayWidth - 1.0f) / CarW;
            Assert.AreEqual(expected, j.Accuracy, 1e-3f);
            Object.DestroyImmediate(car); Object.DestroyImmediate(j.gameObject);
        }
    }
}
