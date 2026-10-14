using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Física por rueda: cada rueda lanza un raycast al suelo y aporta suspensión,
    /// agarre lateral (poco derrape: es un estacionamiento, no una pista) y tracción/freno.
    /// </summary>
    public class VehicleWheelPhysicsTests
    {
        private GameObject ground, car;
        private VehiclePhysics physics;
        private Rigidbody rb;
        private SimulationMode previousMode;

        [SetUp]
        public void Setup()
        {
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            ground = VehicleTestRig.CreateGround();
            car = new GameObject("WheelTestVehicle");
            rb = car.AddComponent<Rigidbody>();
            physics = car.AddComponent<VehiclePhysics>();
            physics.Initialize();
            VehicleTestRig.PlaceAtRest(car, physics);
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = previousMode;
            Object.DestroyImmediate(car);
            Object.DestroyImmediate(ground);
        }

        private void Step(float seconds) => VehicleTestRig.Step(physics, VehicleTestRig.Frames(seconds));

        /// <summary>Velocidad lateral (m/s, respecto de la carrocería) del eje trasero</summary>
        private float RearAxleLateralSpeed()
        {
            Vector3 rearAxle = car.transform.TransformPoint(new Vector3(0f, 0f, -physics.wheelbase * 0.5f));
            return Vector3.Dot(rb.GetPointVelocity(rearAxle), car.transform.right);
        }

        private float RearAxleSpeed()
        {
            Vector3 rearAxle = car.transform.TransformPoint(new Vector3(0f, 0f, -physics.wheelbase * 0.5f));
            return rb.GetPointVelocity(rearAxle).magnitude;
        }

        [Test]
        public void AtRest_SuspensionHoldsCarAtRideHeight()
        {
            Step(2f);
            Assert.AreEqual(physics.restPivotHeight, car.transform.position.y, 0.01f,
                "La suspensión sostiene el peso del auto a su altura de reposo");
            Assert.Less(Mathf.Abs(rb.linearVelocity.y), 0.01f, "Asentado: sin rebote");
        }

        [Test]
        public void DroppedFromHeight_SettlesWithoutBouncingForever()
        {
            car.transform.position += Vector3.up * 0.15f;
            Physics.SyncTransforms();
            Step(3f);
            Assert.AreEqual(physics.restPivotHeight, car.transform.position.y, 0.02f);
            Assert.Less(Mathf.Abs(rb.linearVelocity.y), 0.02f, "El amortiguador apaga el rebote");
        }

        [Test]
        public void Airborne_ThrottleGivesNoThrust()
        {
            car.transform.position += Vector3.up * 5f; // las ruedas no tocan el suelo
            Physics.SyncTransforms();
            physics.accelerationInput = 1f;
            Step(0.2f);
            Assert.Less(new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude, 0.01f,
                "Sin contacto de las ruedas con el suelo no hay tracción");
        }

        [Test]
        public void Throttle_StraightLine_NoYawNoSideDrift()
        {
            physics.accelerationInput = 0.6f;
            Step(3f);
            Assert.Greater(physics.SignedSpeed, 2f, "Acelera");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, car.transform.eulerAngles.y), 0.05f, "No gira solo");
            Assert.AreEqual(0f, car.transform.position.x, 0.05f, "No se va de lado");
        }

        [Test]
        public void Braking_FromSpeed_StopsInAStraightLine()
        {
            rb.linearVelocity = car.transform.forward * 8f;
            physics.brakeInput = 1f;
            // Se frena hasta detenerse (mantener el freno ya detenido activaría la reversa, por diseño)
            float t = 0f;
            while (Mathf.Abs(physics.SignedSpeed) > 0.05f && t < 6f)
            {
                Step(0.1f);
                t += 0.1f;
            }
            Assert.Less(t, 5f, "Frena a 8 m/s en menos de 5 s (≈2,5 m/s²)");
            Assert.AreEqual(0f, car.transform.position.x, 0.05f, "Frena derecho");
            Assert.That(car.transform.position.z, Is.InRange(6f, 20f), "Frenar no es instantáneo: v²/2a ≈ 13 m");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, car.transform.eulerAngles.y), 0.1f, "No se atraviesa");
        }

        [Test]
        public void SteadyTurn_RearWheelsDoNotSlide()
        {
            physics.steerInput = 1f;
            physics.accelerationInput = 0.25f;
            rb.linearVelocity = car.transform.forward * 2f;
            Step(4f);
            Assert.Less(physics.SignedSpeed, 5f, "Velocidad de maniobra");
            Assert.Greater(physics.SignedSpeed, 1f, "Sigue avanzando");
            Assert.Less(Mathf.Abs(RearAxleLateralSpeed()), 0.2f,
                "Auto real, poco derrape: el eje trasero sigue la trayectoria");
        }

        [Test]
        public void Reverse_WithFullLock_RearWheelsDoNotSlide()
        {
            physics.brakeInput = 1f;
            physics.steerInput = -1f;
            Step(5f);
            Assert.IsTrue(physics.IsReversing);
            Assert.Less(Mathf.Abs(RearAxleLateralSpeed()), 0.2f);
        }

        [Test]
        public void FullLock_TurningRadius_MatchesPeugeot308()
        {
            physics.steerInput = 1f;
            physics.accelerationInput = 0.2f;
            Step(1.5f);                         // ruedas al tope
            rb.linearVelocity = car.transform.forward * 1.5f;
            Step(3f);                           // régimen estable
            float yawRate = Mathf.Abs(rb.angularVelocity.y);
            Assert.Greater(yawRate, 0.05f, "Está girando");
            // Radio del centro del eje trasero ≈ 3.72 m (diámetro 10.4 m entre soleras)
            float radius = RearAxleSpeed() / yawRate;
            Assert.AreEqual(3.72f, radius, 0.35f);
        }

        [Test]
        public void Ackermann_InnerWheelTurnsMoreThanOuter()
        {
            VehiclePhysics.ComputeAckermann(20f, physics.wheelbase, physics.track, out float left, out float right);
            Assert.Greater(right, left, "Giro a la derecha: la rueda derecha (interior) gira más");
            Assert.Greater(left, 0f);
            VehiclePhysics.ComputeAckermann(-20f, physics.wheelbase, physics.track, out left, out right);
            Assert.Less(left, right, "Giro a la izquierda: la rueda izquierda (interior) gira más (más negativo)");
            Assert.Less(right, 0f);
        }

        [Test]
        public void Ackermann_ZeroSteer_WheelsStraight()
        {
            VehiclePhysics.ComputeAckermann(0f, physics.wheelbase, physics.track, out float left, out float right);
            Assert.AreEqual(0f, left, 1e-6f);
            Assert.AreEqual(0f, right, 1e-6f);
        }
    }
}
