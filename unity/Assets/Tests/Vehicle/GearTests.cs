using NUnit.Framework;
using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// TDD Tests para marchas (Hito 1: "Información de maniobra (D/R/P)")
    /// - D: acelera hacia adelante
    /// - R: acelera hacia atrás
    /// - P: no acelera
    /// - Solo se puede cambiar de marcha casi detenido
    /// </summary>
    public class GearTests
    {
        private GameObject vehicleGO;
        private VehiclePhysics physics;
        private Rigidbody rb;
        private SimulationMode previousMode;

        [SetUp]
        public void Setup()
        {
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            vehicleGO = new GameObject("GearTestVehicle");
            rb = vehicleGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            physics = vehicleGO.AddComponent<VehiclePhysics>();
            physics.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = previousMode;
            Object.DestroyImmediate(vehicleGO);
        }

        private void Step(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                physics.FixedUpdate();
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        [Test]
        public void Gear_DefaultsToDrive()
        {
            Assert.AreEqual(VehiclePhysics.Gear.Drive, physics.CurrentGear);
        }

        [Test]
        public void Drive_Accelerates_Forward()
        {
            physics.accelerationInput = 1f;
            Step(10);
            Assert.Greater(Vector3.Dot(rb.linearVelocity, vehicleGO.transform.forward), 0f);
        }

        [Test]
        public void Reverse_Accelerates_Backward()
        {
            physics.CycleGear(); // D -> R
            Assert.AreEqual(VehiclePhysics.Gear.Reverse, physics.CurrentGear);
            physics.accelerationInput = 1f;
            Step(10);
            Assert.Less(Vector3.Dot(rb.linearVelocity, vehicleGO.transform.forward), 0f,
                "En reversa el gatillo de aceleración debe mover el auto hacia atrás");
        }

        [Test]
        public void Park_DoesNotAccelerate()
        {
            physics.CycleGear(); // R
            physics.CycleGear(); // P
            Assert.AreEqual(VehiclePhysics.Gear.Park, physics.CurrentGear);
            physics.accelerationInput = 1f;
            Step(10);
            Assert.AreEqual(0f, rb.linearVelocity.magnitude, 0.01f);
        }

        [Test]
        public void CycleGear_Wraps_P_To_D()
        {
            physics.CycleGear(); physics.CycleGear(); physics.CycleGear();
            Assert.AreEqual(VehiclePhysics.Gear.Drive, physics.CurrentGear);
        }

        [Test]
        public void CycleGear_Ignored_WhenMoving()
        {
            rb.linearVelocity = vehicleGO.transform.forward * 5f;
            Assert.IsFalse(physics.CycleGear(), "No se debe cambiar de marcha en movimiento");
            Assert.AreEqual(VehiclePhysics.Gear.Drive, physics.CurrentGear);
        }

        [Test]
        public void GearLetter_MatchesGear()
        {
            Assert.AreEqual("D", physics.GearLetter);
            physics.CycleGear();
            Assert.AreEqual("R", physics.GearLetter);
            physics.CycleGear();
            Assert.AreEqual("P", physics.GearLetter);
        }
    }
}
