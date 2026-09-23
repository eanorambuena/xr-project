using NUnit.Framework;
using UnityEngine;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// TDD Tests para VehiclePhysics
    /// Especificaciones que define el comportamiento del vehículo
    /// </summary>
    public class VehiclePhysicsTests
    {
        private GameObject vehicleGameObject;
        private VehiclePhysics vehiclePhysics;
        private Rigidbody rigidbody;

        [SetUp]
        public void Setup()
        {
            // Crear GameObject con Rigidbody
            vehicleGameObject = new GameObject("TestVehicle");
            rigidbody = vehicleGameObject.AddComponent<Rigidbody>();
            vehiclePhysics = vehicleGameObject.AddComponent<VehiclePhysics>();

            // Configurar Rigidbody
            rigidbody.mass = 1200f;
            rigidbody.drag = 0.5f;
            rigidbody.angularDrag = 0.5f;
            rigidbody.freezeRotation = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(vehicleGameObject);
        }

        // ============================================================
        // SPEC 1.1: Inicialización - Valores por defecto
        // ============================================================
        /// <summary>
        /// TEST 1.1: VehiclePhysics_Initialize_SetCorrectDefaults
        ///
        /// GIVEN: VehiclePhysics adjunto a Rigidbody
        /// WHEN: Se llama al constructor/Start
        /// THEN:
        ///   - maxSpeed = 20 m/s (72 km/h)
        ///   - accelerationForce = 5000 N
        ///   - brakingForce = 3000 N
        /// </summary>
        [Test]
        public void VehiclePhysics_Initialize_SetCorrectDefaults()
        {
            Assert.AreEqual(vehiclePhysics.maxSpeed, 20f, 0.01f,
                "maxSpeed debe ser 20 m/s");
            Assert.AreEqual(vehiclePhysics.accelerationForce, 5000f, 1f,
                "accelerationForce debe ser 5000 N");
            Assert.AreEqual(vehiclePhysics.brakingForce, 3000f, 1f,
                "brakingForce debe ser 3000 N");
        }

        // ============================================================
        // SPEC 1.2: Aceleración - Aumenta velocidad
        // ============================================================
        /// <summary>
        /// TEST 1.2: ApplyMotorForce_WithFullThrottle_IncreasesVelocity
        ///
        /// GIVEN: accelerationInput = 1.0f (trigger completamente presionado)
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - velocidad aumenta
        ///   - velocidad nunca excede maxSpeed (20 m/s)
        ///   - dirección = transform.forward
        /// </summary>
        [Test]
        public void ApplyMotorForce_WithFullThrottle_IncreasesVelocity()
        {
            // Inicializar posición
            vehicleGameObject.transform.position = new Vector3(0, 0, 0);
            vehiclePhysics.accelerationInput = 1.0f;

            // Simular varios frames
            for (int i = 0; i < 10; i++)
            {
                vehiclePhysics.FixedUpdate();
            }

            float velocityMagnitude = rigidbody.velocity.magnitude;
            Assert.Greater(velocityMagnitude, 0,
                "Velocidad debe aumentar con aceleración");
            Assert.LessOrEqual(velocityMagnitude, vehiclePhysics.maxSpeed,
                "Velocidad nunca debe exceder maxSpeed");
        }

        // ============================================================
        // SPEC 1.3: Frenado - Disminuye velocidad
        // ============================================================
        /// <summary>
        /// TEST 1.3: ApplyBraking_WithBrakeActive_DecreasesVelocity
        ///
        /// GIVEN: brakeInput = 1.0f
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - velocidad disminuye progresivamente
        ///   - sin "snap" a cero (movimiento natural)
        /// </summary>
        [Test]
        public void ApplyBraking_WithBrakeActive_DecreasesVelocity()
        {
            // Setup: dar velocidad inicial
            rigidbody.velocity = new Vector3(10, 0, 0);
            float initialSpeed = rigidbody.velocity.magnitude;

            // Frenar
            vehiclePhysics.brakeInput = 1.0f;
            vehiclePhysics.FixedUpdate();

            float finalSpeed = rigidbody.velocity.magnitude;
            Assert.Less(finalSpeed, initialSpeed,
                "Velocidad debe disminuir al frenar");
        }

        // ============================================================
        // SPEC 1.4: Dirección - Gira según joystick
        // ============================================================
        /// <summary>
        /// TEST 1.4: ApplySteering_WithRightSteer_RotatesRight
        ///
        /// GIVEN: steerInput = 0.5f (joystick a la derecha)
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - girar sobre eje Y en dirección positiva
        ///   - steerInput = 0 → sin rotación
        /// </summary>
        [Test]
        public void ApplySteering_WithRightSteer_RotatesRight()
        {
            vehicleGameObject.transform.rotation = Quaternion.identity;

            vehiclePhysics.steerInput = 0.5f;
            Quaternion initialRot = vehicleGameObject.transform.rotation;

            vehiclePhysics.FixedUpdate();

            Quaternion finalRot = vehicleGameObject.transform.rotation;

            // Verificar que Y rotation aumentó (giró a la derecha)
            float initialYaw = initialRot.eulerAngles.y;
            float finalYaw = finalRot.eulerAngles.y;

            // Normalizar ángulos para comparación
            if (finalYaw < 180 && initialYaw > 180)
                finalYaw += 360;

            Assert.Greater(finalYaw, initialYaw,
                "Debe girar a la derecha con steerInput positivo");
        }

        /// <summary>
        /// TEST 1.4b: ApplySteering_WithNullSteer_NoRotation
        /// </summary>
        [Test]
        public void ApplySteering_WithNullSteer_NoRotation()
        {
            vehicleGameObject.transform.rotation = Quaternion.identity;

            vehiclePhysics.steerInput = 0f;
            Quaternion initialRot = vehicleGameObject.transform.rotation;

            vehiclePhysics.FixedUpdate();

            Quaternion finalRot = vehicleGameObject.transform.rotation;

            Assert.AreEqual(initialRot, finalRot,
                "No debe girar cuando steerInput = 0");
        }

        // ============================================================
        // SPEC 1.5: Límite de velocidad
        // ============================================================
        /// <summary>
        /// TEST 1.5: LimitSpeed_EnforcesMaxSpeed
        ///
        /// GIVEN: Rigidbody con velocidad > maxSpeed
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN: velocidad = maxSpeed
        /// </summary>
        [Test]
        public void LimitSpeed_EnforcesMaxSpeed()
        {
            // Forzar velocidad superior al máximo
            rigidbody.velocity = new Vector3(0, 0, 25f); // 25 m/s > 20 m/s

            vehiclePhysics.FixedUpdate();

            float finalSpeed = rigidbody.velocity.magnitude;
            Assert.LessOrEqual(finalSpeed, vehiclePhysics.maxSpeed,
                "Velocidad debe estar limitada a maxSpeed");
        }

        // ============================================================
        // SPEC 1.6: Combinación de inputs
        // ============================================================
        /// <summary>
        /// TEST 1.6: CombinedInputs_AccelerationAndSteering
        ///
        /// Acelerar mientras giras
        /// </summary>
        [Test]
        public void CombinedInputs_AccelerationAndSteering()
        {
            vehiclePhysics.accelerationInput = 1.0f;
            vehiclePhysics.steerInput = 0.3f;

            Quaternion initialRot = vehicleGameObject.transform.rotation;
            Vector3 initialVel = rigidbody.velocity;

            vehiclePhysics.FixedUpdate();

            // Debe acelerar
            Assert.Greater(rigidbody.velocity.magnitude, initialVel.magnitude);

            // Debe girar
            Assert.AreNotEqual(vehicleGameObject.transform.rotation, initialRot);
        }
    }
}
