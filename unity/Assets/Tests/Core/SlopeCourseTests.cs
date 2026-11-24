using NUnit.Framework;
using UnityEngine;
using VRCar.Core;

namespace VRCar.Tests.Core
{
    /// <summary>Escenario de pendiente: inclinación por dificultad, entorno vertical y auto al pie de la cuesta</summary>
    public class SlopeCourseTests
    {
        private GameObject rootGO, uprightGO, vehicleGO, courseGO;

        [TearDown]
        public void TearDown()
        {
            foreach (var g in new[] { rootGO, vehicleGO, courseGO }) if (g != null) Object.DestroyImmediate(g);
            ScenarioSession.SetDifficulty(1);
        }

        private SlopeCourse Build(out Transform upright, out Transform vehicle)
        {
            rootGO = new GameObject("SlopeRoot");
            uprightGO = new GameObject("Upright");
            uprightGO.transform.SetParent(rootGO.transform, false);
            vehicleGO = new GameObject("Vehicle");
            courseGO = new GameObject("Course");
            var course = courseGO.AddComponent<SlopeCourse>();
            course.Configure(rootGO.transform, uprightGO.transform, vehicleGO.transform, new Vector3(-52f, 0.78f, -1.45f), 90f, null);
            upright = uprightGO.transform; vehicle = vehicleGO.transform;
            return course;
        }

        [Test]
        public void Angles_GrowWithDifficulty_AndStayWithinWhatTheCarCanClimb()
        {
            float easy = SlopeCourse.AngleFor(0), medium = SlopeCourse.AngleFor(1), hard = SlopeCourse.AngleFor(2);
            Assert.Greater(easy, 0f); Assert.Greater(medium, easy); Assert.Greater(hard, medium);
            // La tracción (5000 N) debe vencer la gravedad en la cuesta: m·g·sen(a) < fuerza motriz
            Assert.Less(1200f * 9.81f * Mathf.Sin(hard * Mathf.Deg2Rad), 5000f * 0.6f, "El auto puede subir aun en la más empinada");
            Assert.AreEqual(hard, SlopeCourse.AngleFor(99));
            Assert.AreEqual(easy, SlopeCourse.AngleFor(-1));
        }

        [Test]
        public void PercentFor_IsHundredTimesTheTangent()
        {
            for (int d = 0; d < 3; d++)
                Assert.AreEqual(Mathf.Tan(SlopeCourse.AngleFor(d) * Mathf.Deg2Rad) * 100f, SlopeCourse.PercentFor(d), 1e-3f);
            Assert.AreEqual(12.3f, SlopeCourse.PercentFor(1), 0.1f);
        }

        [Test]
        public void Apply_TiltsTheRoot_RisingTowardsPlusX()
        {
            var course = Build(out _, out _);
            foreach (int d in new[] { 0, 1, 2 })
            {
                course.Apply(d);
                float a = SlopeCourse.AngleFor(d);
                Assert.AreEqual(a, course.CurrentAngle, 1e-4f);
                Vector3 p = rootGO.transform.TransformPoint(new Vector3(10f, 0f, 0f));
                Assert.AreEqual(10f * Mathf.Sin(a * Mathf.Deg2Rad), p.y, 1e-3f, "Sube hacia +X");
                Vector3 side = rootGO.transform.TransformPoint(new Vector3(0f, 0f, 10f));
                Assert.AreEqual(0f, side.y, 1e-3f, "A lo ancho de la calle no hay inclinación");
            }
        }

        [Test]
        public void Apply_KeepsTheUprightChildrenVertical_AtTheirPositionOnTheSlope()
        {
            var course = Build(out var upright, out _);
            var unit = new GameObject("Lamp").transform;
            unit.SetParent(upright, false);
            unit.localPosition = new Vector3(14f, 0f, 5.6f);
            var pole = new GameObject("Pole").transform;
            pole.SetParent(unit, false);
            pole.localPosition = new Vector3(0f, 2.5f, 0f);

            course.Apply(2);
            Assert.AreEqual(1f, Vector3.Dot(unit.up, Vector3.up), 1e-4f, "El poste queda vertical");
            Assert.AreEqual(1f, Vector3.Dot(pole.up, Vector3.up), 1e-4f, "Sus hijos también");
            Vector3 expected = rootGO.transform.TransformPoint(new Vector3(14f, 0f, 5.6f));
            Assert.AreEqual(0f, Vector3.Distance(expected, unit.position), 1e-3f, "Sigue apoyado sobre la cuesta");
            Assert.Greater(pole.position.y, unit.position.y + 2.4f);
        }

        [Test]
        public void Apply_PutsTheCarOnTheFlatStart_Level_FacingTheHill_AndStopped()
        {
            var course = Build(out _, out var vehicle);
            var rb = vehicleGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = Vector3.one;
            var physics = vehicleGO.AddComponent<VRCar.Vehicle.VehiclePhysics>();
            Assert.IsFalse(physics.alignPitchToGround, "Por defecto el auto no cabecea (estacionamientos planos)");

            course.Apply(2);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(-52f, 0.78f, -1.45f), vehicle.position), 1e-3f, "Parte donde se le dijo, en el mundo");
            Assert.AreEqual(1f, Vector3.Dot(vehicle.forward, Vector3.right), 1e-4f, "Mira hacia la cuesta (+X)");
            Assert.AreEqual(0f, vehicle.forward.y, 1e-4f, "Horizontal: sobre el plano no hay pendiente");
            Assert.AreEqual(1f, Vector3.Dot(vehicle.up, Vector3.up), 1e-4f, "Derecho");
            Assert.AreEqual(Vector3.zero, rb.linearVelocity);
            Assert.IsTrue(physics.alignPitchToGround, "Se enciende para poder subir de lo plano a la cuesta");
        }

        [Test]
        public void TheCarStartsBeforeTheHinge_AtGroundLevel_EvenWhenTheHillIsSteep()
        {
            var course = Build(out _, out var vehicle);
            rootGO.transform.position = new Vector3(-40f, 0f, 0f);   // la bisagra: donde termina el plano y empieza la cuesta
            course.Apply(2);
            Assert.Less(vehicle.position.x, rootGO.transform.position.x, "El auto parte antes de la bisagra");
            Assert.AreEqual(0.78f, vehicle.position.y, 1e-3f, "A la altura del plano, no de la cuesta");
            // Un punto de la calle pasada la bisagra sí sube
            Vector3 onSlope = rootGO.transform.TransformPoint(new Vector3(20f, 0f, 0f));
            Assert.Greater(onSlope.y, 3f);
        }

        [Test]
        public void PitchCorrection_IsZeroOnLevelGround_AndTurnsTheCarUpToTheSlopeNormal()
        {
            Vector3 up = Vector3.up, right = new Vector3(0f, 0f, -1f);   // auto mirando a +X
            Assert.AreEqual(0f, VRCar.Vehicle.VehiclePhysics.ComputePitchCorrection(up, right, Vector3.up), 1e-4f);

            foreach (float deg in new[] { 4f, 10f, -7f })   // cuesta que sube (o baja) hacia +X
            {
                float a = deg * Mathf.Deg2Rad;
                Vector3 normal = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
                float c = VRCar.Vehicle.VehiclePhysics.ComputePitchCorrection(up, right, normal);
                Assert.AreEqual(Mathf.Abs(deg), Mathf.Abs(c), 1e-3f, "Corrige exactamente la inclinación");
                Vector3 turned = Quaternion.AngleAxis(c, right) * up;
                Assert.AreEqual(0f, Vector3.Distance(turned, normal), 1e-4f, "Tras girar, el techo del auto queda sobre la normal");
            }
        }

        [Test]
        public void PitchCorrection_IgnoresTheSideSlope_OnlyPitchesAlongTheCar()
        {
            Vector3 up = Vector3.up, right = new Vector3(0f, 0f, -1f);
            Vector3 lateral = new Vector3(0f, Mathf.Cos(0.2f), Mathf.Sin(0.2f));   // el suelo se ladea, no sube hacia adelante
            Assert.AreEqual(0f, VRCar.Vehicle.VehiclePhysics.ComputePitchCorrection(up, right, lateral), 1e-3f);
        }

        [Test]
        public void Apply_GivesTheJudgmentAnIntroWithTheLevelAndSlope()
        {
            var course = Build(out _, out _);
            var judgmentGO = new GameObject("Judgment");
            var judgment = judgmentGO.AddComponent<ParallelParkingJudgment>();
            course.Configure(rootGO.transform, uprightGO.transform, vehicleGO.transform, Vector3.zero, 90f, judgment);
            course.Apply(1);
            StringAssert.Contains("Media", judgment.Intro);
            StringAssert.Contains("12 %", judgment.Intro);
            StringAssert.Contains("freno", judgment.Intro);
            Object.DestroyImmediate(judgmentGO);
        }

        [Test]
        public void Apply_WithoutPieces_DoesNotThrow()
        {
            courseGO = new GameObject("Course");
            var course = courseGO.AddComponent<SlopeCourse>();
            Assert.DoesNotThrow(() => course.Apply(1));
        }
    }
}
