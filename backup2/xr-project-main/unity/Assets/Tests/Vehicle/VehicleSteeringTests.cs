using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Tests de calibración del viraje, de la caja R / N / D y del freno de mano.
    /// Referencia: Peugeot 308 — batalla 2.613 m, trocha 1.56 m, diámetro de giro 10.4 m.
    /// </summary>
    public class VehicleSteeringTests
    {
        private GameObject go;
        private VehiclePhysics physics;
        private Rigidbody rb;
        private SimulationMode previousMode;
        private GameObject ground;

        [SetUp]
        public void Setup()
        {
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            ground = VehicleTestRig.CreateGround();
            go = new GameObject("SteeringTestVehicle");
            rb = go.AddComponent<Rigidbody>();
            physics = go.AddComponent<VehiclePhysics>();
            physics.Initialize();
            VehicleTestRig.PlaceAtRest(go, physics);
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = previousMode;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ground);
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

        // FullLock_TurningRadius_MatchesPeugeot308: ahora en VehicleWheelPhysicsTests (dinámica libre, sin forzar la velocidad)

        [Test]
        public void HoldingBrake_WhenStopped_DoesNotEngageReverse()
        {
            physics.brakeInput = 1f;
            Step(Frames(2f));
            Assert.IsFalse(physics.IsReversing, "El freno es solo freno: no activa la reversa");
            Assert.AreEqual(0f, physics.SignedSpeed, 0.05f);
        }

        [Test]
        public void NewVehicle_StartsInNeutral_AndTheAcceleratorDoesNotMoveIt()
        {
            var fresh = new GameObject("FreshVehicle");
            fresh.AddComponent<Rigidbody>();
            var p = fresh.AddComponent<VehiclePhysics>();
            p.Initialize();
            Assert.AreEqual(GearMode.Neutral, p.CurrentGear);
            Assert.IsTrue(p.IsNeutral);
            Object.DestroyImmediate(fresh);

            physics.SetGear(GearMode.Neutral);
            physics.accelerationInput = 1f;
            Step(Frames(2f));
            Assert.AreEqual(0f, physics.SignedSpeed, 0.05f, "En Neutro el acelerador no empuja el auto");
        }

        [Test]
        public void ShiftGear_GoesThroughNeutral_BothWays()
        {
            physics.SetGear(GearMode.Reverse);
            Assert.IsTrue(physics.ShiftGearUp());
            Assert.AreEqual(GearMode.Neutral, physics.CurrentGear);
            Assert.IsTrue(physics.ShiftGearUp());
            Assert.AreEqual(GearMode.Drive, physics.CurrentGear);
            Assert.IsFalse(physics.ShiftGearUp(), "D es la última marcha");
            Assert.IsTrue(physics.ShiftGearDown());
            Assert.AreEqual(GearMode.Neutral, physics.CurrentGear);
            Assert.IsTrue(physics.ShiftGearDown());
            Assert.AreEqual(GearMode.Reverse, physics.CurrentGear);
            Assert.IsFalse(physics.ShiftGearDown(), "R es la primera marcha");
        }

        [Test]
        public void ShiftGear_IsRefusedWhileTheCarIsMoving()
        {
            rb.linearVelocity = go.transform.forward * 5f;
            physics.SetGear(GearMode.Drive);
            Assert.IsFalse(physics.ShiftGearDown(), "En movimiento no se cambia de marcha");
            Assert.AreEqual(GearMode.Drive, physics.CurrentGear);
        }

        [Test]
        public void Reverse_AcceleratorGoesBackwards()
        {
            physics.SetGear(GearMode.Reverse);
            Assert.IsTrue(physics.IsReversing);
            physics.accelerationInput = 1f;
            Step(Frames(1.5f));
            Assert.Less(physics.SignedSpeed, -0.1f, "En reversa el auto retrocede");
        }

        [Test]
        public void Reverse_IsSlow()
        {
            physics.SetGear(GearMode.Reverse);
            physics.accelerationInput = 1f;
            Step(Frames(8f));
            Assert.GreaterOrEqual(physics.SignedSpeed, -physics.maxReverseSpeed - 0.01f);
        }

        [Test]
        public void Reverse_RightSteer_TurnsTailRight()
        {
            physics.SetGear(GearMode.Reverse);
            physics.accelerationInput = 1f;
            physics.steerInput = 1f;
            Step(Frames(3f));
            // Retrocediendo con el volante a la derecha, la trompa gira a la izquierda (yaw negativo)
            Assert.Less(Mathf.DeltaAngle(0f, go.transform.eulerAngles.y), -1f);
        }

        [Test]
        public void Reverse_BrakeStopsTheCar_WithoutLurchingForward()
        {
            physics.SetGear(GearMode.Reverse);
            physics.accelerationInput = 1f;
            Step(Frames(2f));
            Assert.Less(physics.SignedSpeed, -0.3f);
            physics.accelerationInput = 0f;
            physics.brakeInput = 1f;
            Step(Frames(3f));
            Assert.IsTrue(physics.IsReversing);
            Assert.AreEqual(0f, physics.SignedSpeed, 0.05f, "Queda detenido, sin salir hacia adelante");
        }

        [Test]
        public void Braking_DoesNotChangeGear_WhileMoving()
        {
            rb.linearVelocity = go.transform.forward * 5f;
            physics.brakeInput = 1f;
            Step(Frames(0.5f));
            Assert.AreEqual(GearMode.Drive, physics.CurrentGear);
            Assert.IsFalse(physics.IsReversing);
            Assert.Greater(physics.SignedSpeed, 0f);
        }

        [Test]
        public void Handbrake_StopsTheCar_AndKeepsItStopped()
        {
            physics.accelerationInput = 1f;
            Step(Frames(1.5f));
            Assert.Greater(physics.SignedSpeed, 0.5f);

            physics.SetHandbrake(true);
            Assert.IsTrue(physics.IsHandbrakeEngaged);
            Step(Frames(2f));
            Assert.AreEqual(0f, rb.linearVelocity.magnitude, 0.05f, "Con el freno de mano el auto queda detenido aunque se acelere");

            physics.SetHandbrake(false);
            Step(Frames(1f));
            Assert.Greater(physics.SignedSpeed, 0.1f, "Al soltarlo vuelve a avanzar");
        }

        [Test]
        public void Handbrake_FiresFinished_OnlyAfterTheCarHasMoved()
        {
            physics.ResetDrivingState();                  // la distancia recorrida parte de cero
            physics.SetGear(GearMode.Drive);
            int fired = 0;
            physics.OnHandbrakeFinished += () => fired++;

            physics.SetHandbrake(true);
            Assert.AreEqual(0, fired, "Sin haberse movido no cierra el intento");
            physics.SetHandbrake(false);

            physics.accelerationInput = 1f;
            Step(Frames(2f));
            physics.accelerationInput = 0f;
            physics.SetHandbrake(true);
            Assert.AreEqual(1, fired, "Tras moverse, el freno de mano cierra el intento");

            physics.SetHandbrake(false);
            physics.SetHandbrake(true);
            Assert.AreEqual(1, fired, "Solo una vez por intento");
        }

        [Test]
        public void ResetDrivingState_ReturnsToNeutral_AndReleasesTheHandbrake()
        {
            physics.SetGear(GearMode.Reverse);
            physics.SetHandbrake(true);
            physics.ResetDrivingState();
            Assert.AreEqual(GearMode.Neutral, physics.CurrentGear);
            Assert.IsFalse(physics.IsHandbrakeEngaged);
        }

    
        [Test]
        public void Brake_HoldsCarCompletelyStopped_NoCreep()
        {
            rb.linearVelocity = go.transform.forward * 0.04f; // arrastre residual muy lento
            physics.brakeInput = 1f;
            Step(Frames(0.3f));
            Assert.AreEqual(0f, rb.linearVelocity.magnitude, 1e-4f, "Con el freno apretado el auto debe quedar detenido del todo");
        }

        [Test]
        public void Brake_DecelerationIsProgressive_NoInstantStop()
        {
            rb.linearVelocity = go.transform.forward * 2f; // ~7 km/h
            physics.brakeInput = 0.5f;
            Step(1);
            Assert.Greater(rb.linearVelocity.magnitude, 1.9f, "Frenar a medias no debe detener de golpe");
        }
}
}
