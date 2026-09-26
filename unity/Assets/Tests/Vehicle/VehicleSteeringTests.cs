using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Tests de calibración del viraje y de la reversa sin caja de cambios.
    /// Referencia: Peugeot 308 — batalla 2.613 m, trocha 1.56 m, diámetro de giro 10.4 m.
    /// </summary>
    public class VehicleSteeringTests
    {
        private GameObject go;
        private VehiclePhysics physics;
        private Rigidbody rb;
        private SimulationMode previousMode;

        [SetUp]
        public void Setup()
        {
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            go = new GameObject("SteeringTestVehicle");
            rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            physics = go.AddComponent<VehiclePhysics>();
            physics.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = previousMode;
            Object.DestroyImmediate(go);
        }

        private void Step(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                physics.FixedUpdate();
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        private int Frames(float seconds) => Mathf.CeilToInt(seconds / Time.fixedDeltaTime);

        [Test]
        public void MaxSteerAngle_DerivedFromTurningCircle()
        {
            Assert.AreEqual(35.1f, physics.maxSteerAngle, 0.2f);
        }

        [Test]
        public void Stopped_FullLock_DoesNotRotate()
        {
            physics.steerInput = 1f;
            Step(Frames(2f));
            Assert.AreEqual(0f, go.transform.eulerAngles.y, 0.01f, "Un auto detenido no gira sobre sí mismo");
        }

        [Test]
        public void SteerAngle_RampsLikeASteeringWheel()
        {
            physics.steerInput = 1f;
            Step(Frames(0.25f));
            Assert.Less(physics.CurrentSteerAngle, physics.maxSteerAngle * 0.5f, "Las ruedas no saltan al tope");
            Step(Frames(1.5f));
            Assert.AreEqual(physics.maxSteerAngle, physics.CurrentSteerAngle, 0.01f);
        }

        [Test]
        public void FullLock_TurningRadius_MatchesPeugeot308()
        {
            physics.steerInput = 1f;
            Step(Frames(1.5f)); // ruedas al tope, detenido

            // Avanzar a 1.5 m/s constante y medir el giro
            const float v = 1.5f;
            float seconds = 2f;
            float yaw0 = go.transform.eulerAngles.y;
            for (int i = 0; i < Frames(seconds); i++)
            {
                rb.linearVelocity = go.transform.forward * v;
                physics.FixedUpdate();
                Physics.Simulate(Time.fixedDeltaTime);
            }
            float yawDelta = Mathf.DeltaAngle(yaw0, go.transform.eulerAngles.y) * Mathf.Deg2Rad;
            float radius = v * (Frames(seconds) * Time.fixedDeltaTime) / yawDelta;
            // Radio del centro del eje trasero ≈ 3.72 m (diámetro 10.4 m entre soleras)
            Assert.AreEqual(3.72f, radius, 0.15f);
            Assert.Greater(yawDelta, 0f, "Joystick a la derecha gira a la derecha");
        }

        [Test]
        public void HoldingBrake_WhenStopped_EngagesReverse()
        {
            physics.brakeInput = 1f;
            Step(Frames(0.3f));
            Assert.IsFalse(physics.IsReversing, "Un toque de freno no activa la reversa");
            Step(Frames(0.6f));
            Assert.IsTrue(physics.IsReversing);
            Step(Frames(1f));
            Assert.Less(physics.SignedSpeed, -0.1f, "En reversa el auto retrocede");
        }

        [Test]
        public void Reverse_IsSlow()
        {
            physics.brakeInput = 1f;
            Step(Frames(8f));
            Assert.GreaterOrEqual(physics.SignedSpeed, -physics.maxReverseSpeed - 0.01f);
        }

        [Test]
        public void Reverse_RightSteer_TurnsTailRight()
        {
            physics.brakeInput = 1f;
            physics.steerInput = 1f;
            Step(Frames(3f));
            // Retrocediendo con el volante a la derecha, la trompa gira a la izquierda (yaw negativo)
            Assert.Less(Mathf.DeltaAngle(0f, go.transform.eulerAngles.y), -1f);
        }

        [Test]
        public void ReleasingBrake_WhenStopped_ExitsReverse()
        {
            physics.brakeInput = 1f;
            Step(Frames(1f));
            Assert.IsTrue(physics.IsReversing);
            physics.brakeInput = 0f;
            physics.accelerationInput = 1f; // en reversa, el acelerador frena
            Step(Frames(3f));
            physics.accelerationInput = 0f;
            Step(Frames(1f));
            Assert.IsFalse(physics.IsReversing);
        }

        [Test]
        public void Reverse_StoppingWithAccelerator_DoesNotLurchForward()
        {
            // En reversa el gatillo derecho frena. Al detenerse, mantenerlo NO debe sacar el auto hacia adelante.
            physics.brakeInput = 1f;
            Step(Frames(2f));
            Assert.Less(physics.SignedSpeed, -0.3f);
            physics.brakeInput = 0f;
            physics.accelerationInput = 1f;
            Step(Frames(3f));
            Assert.IsTrue(physics.IsReversing, "Sigue en reversa mientras se mantenga un pedal");
            Assert.AreEqual(0f, physics.SignedSpeed, 0.05f, "Queda detenido, sin salir hacia adelante");
        }

        [Test]
        public void Braking_DoesNotEngageReverse_WhileMoving()
        {
            rb.linearVelocity = go.transform.forward * 5f;
            physics.brakeInput = 1f;
            Step(Frames(0.5f));
            Assert.IsFalse(physics.IsReversing);
            Assert.Greater(physics.SignedSpeed, 0f);
        }
    }
}
