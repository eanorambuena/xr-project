using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Los dos controles como volante. Espacio del auto: +x derecha, +y arriba, +z adelante.
    /// Manos separadas 40 cm: izquierda en x = -0.2, derecha en x = +0.2.
    /// </summary>
    public class WheelSteeringTests
    {
        private GameObject go;
        private WheelSteering wheel;

        [SetUp]
        public void Setup()
        {
            WheelSteering.ForgetCalibration();
            go = new GameObject("Wheel");
            wheel = go.AddComponent<WheelSteering>();
        }

        [TearDown]
        public void TearDown() { WheelSteering.ForgetCalibration(); Object.DestroyImmediate(go); }

        private static readonly Vector3 L = new Vector3(-0.2f, 0f, 0f), R = new Vector3(0.2f, 0f, 0f);

        /// <summary>Manos de un volante de 40 cm de ancho girado "deg" grados (positivo = horario visto desde el conductor)</summary>
        private static void Hands(float deg, out Vector3 left, out Vector3 right)
        {
            float a = deg * Mathf.Deg2Rad;
            var half = new Vector3(Mathf.Cos(a), -Mathf.Sin(a), 0f) * 0.2f;   // la mano derecha baja con el giro horario
            left = -half; right = half;
        }

        [Test]
        public void HandsLevel_IsZero() => Assert.AreEqual(0f, WheelSteering.ComputeAngle(L, R), 1e-4f);

        [Test]
        public void RightHandDown_IsAPositiveTurn_ToTheRight()
        {
            Hands(30f, out var l, out var r);
            Assert.AreEqual(30f, WheelSteering.ComputeAngle(l, r), 0.01f);
            Assert.Greater(r.x, 0f); Assert.Less(r.y, 0f); Assert.Greater(l.y, 0f);
        }

        [Test]
        public void LeftHandDown_IsANegativeTurn_ToTheLeft()
        {
            Hands(-45f, out var l, out var r);
            Assert.AreEqual(-45f, WheelSteering.ComputeAngle(l, r), 0.01f);
            Assert.Less(l.y, 0f);
        }

        [Test]
        public void TheDirectionTheUserFaces_DoesNotMatter()
        {
            Hands(25f, out var l, out var r);
            var yaw = Quaternion.Euler(0f, 40f, 0f);   // cuerpo girado 40° respecto al auto
            Assert.AreEqual(25f, WheelSteering.ComputeAngle(yaw * l, yaw * r), 0.01f);
        }

        [Test]
        public void TheHeightAndDistanceOfTheHands_DoNotMatter()
        {
            Hands(20f, out var l, out var r);
            var offset = new Vector3(0.3f, 0.9f, 0.5f);
            Assert.AreEqual(20f, WheelSteering.ComputeAngle(l + offset, r + offset), 0.01f);
        }

        [Test]
        public void ControlsTooClose_CannotBeMeasured()
        {
            Assert.IsTrue(float.IsNaN(WheelSteering.ComputeAngle(Vector3.zero, new Vector3(0.03f, 0f, 0f))));
        }

        // ---- De ángulo a dirección ----
        [Test]
        public void ToSteer_HasADeadZone_AndReachesTheLockAtTheLockAngle()
        {
            Assert.AreEqual(0f, WheelSteering.ToSteer(1.5f, 70f, 2f));
            Assert.AreEqual(0f, WheelSteering.ToSteer(-2f, 70f, 2f));
            Assert.AreEqual(1f, WheelSteering.ToSteer(70f, 70f, 2f), 1e-4f);
            Assert.AreEqual(-1f, WheelSteering.ToSteer(-70f, 70f, 2f), 1e-4f);
            Assert.AreEqual(1f, WheelSteering.ToSteer(120f, 70f, 2f), 1e-4f, "más allá del tope no pasa de 1");
        }

        [Test]
        public void ToSteer_GrowsSmoothlyAndKeepsTheSign()
        {
            float a = WheelSteering.ToSteer(10f, 70f, 2f), b = WheelSteering.ToSteer(30f, 70f, 2f);
            Assert.Greater(a, 0f); Assert.Greater(b, a); Assert.Less(b, 1f);
            Assert.AreEqual(-a, WheelSteering.ToSteer(-10f, 70f, 2f), 1e-5f);
        }

        // ---- Componente ----
        [Test]
        public void Feed_TurningTheWheelRight_SteersRight()
        {
            Hands(35f, out var l, out var r);
            Assert.IsTrue(wheel.Feed(l, r, 1f));
            Assert.IsTrue(wheel.HasInput);
            Assert.AreEqual(35f, wheel.WheelAngle, 0.1f);
            Assert.Greater(wheel.Steer, 0.3f);
        }

        [Test]
        public void Feed_SmoothsTheReading_ButTheFirstOneIsImmediate()
        {
            Hands(0f, out var l0, out var r0);
            Hands(40f, out var l1, out var r1);
            wheel.Feed(l0, r0, 0.01f);
            Assert.AreEqual(0f, wheel.WheelAngle, 1e-3f);
            wheel.Feed(l1, r1, 0.01f);   // un salto brusco de las manos
            Assert.Greater(wheel.WheelAngle, 0f);
            Assert.Less(wheel.WheelAngle, 40f, "el filtro no lo copia de golpe");
            for (int i = 0; i < 100; i++) wheel.Feed(l1, r1, 0.02f);
            Assert.AreEqual(40f, wheel.WheelAngle, 0.1f);
        }

        [Test]
        public void FeedSimulated_BehavesLikeRealHands()
        {
            Assert.IsTrue(wheel.FeedSimulated(30f, 1f));
            Assert.IsTrue(wheel.HasInput);
            Assert.AreEqual(30f, wheel.WheelAngle, 0.1f);
            Assert.Greater(wheel.Steer, 0f);
            wheel.FeedSimulated(-30f, 10f);
            Assert.Less(wheel.Steer, 0f);
        }

        [Test]
        public void Feed_WithoutAMeasure_ReportsNoInput()
        {
            Assert.IsFalse(wheel.Feed(Vector3.zero, Vector3.zero, 0.02f));
            Assert.IsFalse(wheel.HasInput);
        }

        [Test]
        public void Recenter_MakesTheCurrentPositionStraight()
        {
            Hands(12f, out var l, out var r);
            wheel.Feed(l, r, 1f);
            Assert.AreNotEqual(0f, wheel.Steer);
            wheel.Recenter();
            Assert.AreEqual(0f, wheel.WheelAngle, 0.05f);
            Assert.AreEqual(0f, wheel.Steer, 1e-4f);

            Hands(12f + 30f, out var l2, out var r2);   // desde ahí, 30° más a la derecha
            for (int i = 0; i < 100; i++) wheel.Feed(l2, r2, 0.02f);
            Assert.AreEqual(30f, wheel.WheelAngle, 0.2f);
        }

        [Test]
        public void TheCenter_IsKeptWhenTheSceneReloads()
        {
            Hands(8f, out var l, out var r);
            wheel.Feed(l, r, 1f);
            wheel.Recenter();
            var other = new GameObject("Other").AddComponent<WheelSteering>();   // la escena recargada
            other.Feed(l, r, 1f);
            Assert.AreEqual(0f, other.WheelAngle, 0.05f);
            Object.DestroyImmediate(other.gameObject);
        }
    }
}
