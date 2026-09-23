using NUnit.Framework;
using UnityEngine;
using VRCar.Tests.Mocks;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// TDD Tests para InputMapper
    /// Verifica que los inputs del XR Controller se mapeen correctamente a VehiclePhysics
    /// </summary>
    public class InputMapperTests
    {
        private GameObject vehicleGameObject;
        private InputMapper inputMapper;
        private VehiclePhysics vehiclePhysics;
        private MockXRController mockRightController;
        private MockXRController mockLeftController;

        [SetUp]
        public void Setup()
        {
            // Crear vehículo
            vehicleGameObject = new GameObject("TestVehicle");
            vehicleGameObject.AddComponent<Rigidbody>();
            vehiclePhysics = vehicleGameObject.AddComponent<VehiclePhysics>();

            // Crear InputMapper
            inputMapper = vehicleGameObject.AddComponent<InputMapper>();

            // Crear mocks
            mockRightController = new MockXRController();
            mockLeftController = new MockXRController();

            // Asignar mocks al InputMapper (para testing)
            inputMapper.SetMockControllers(mockRightController, mockLeftController);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(vehicleGameObject);
        }

        // ============================================================
        // SPEC 2.1: Input de Aceleración (Right Trigger)
        // ============================================================
        /// <summary>
        /// TEST 2.1: InputMapper_RightTrigger_MapsToAcceleration
        ///
        /// GIVEN: Right controller con trigger presionado
        /// WHEN: Update() se ejecuta
        /// THEN: vehiclePhysics.accelerationInput = valor del trigger
        /// </summary>
        [Test]
        public void InputMapper_RightTrigger_MapsToAcceleration()
        {
            // Setup: Right trigger a 75%
            mockRightController.SetTriggerValue(0.75f);

            // Execute
            inputMapper.Update();

            // Assert
            Assert.AreEqual(vehiclePhysics.accelerationInput, 0.75f, 0.01f,
                "Right trigger debe mapear a accelerationInput");
        }

        /// <summary>
        /// TEST 2.1b: RightTrigger_FullPress
        /// </summary>
        [Test]
        public void InputMapper_RightTrigger_FullPress()
        {
            mockRightController.SetTriggerValue(1.0f);
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.accelerationInput, 1.0f, 0.01f);
        }

        /// <summary>
        /// TEST 2.1c: RightTrigger_NoPress
        /// </summary>
        [Test]
        public void InputMapper_RightTrigger_NoPress()
        {
            mockRightController.SetTriggerValue(0.0f);
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.accelerationInput, 0.0f, 0.01f);
        }

        // ============================================================
        // SPEC 2.2: Input de Frenado (Left Trigger)
        // ============================================================
        /// <summary>
        /// TEST 2.2: InputMapper_LeftTrigger_MapsToBraking
        ///
        /// GIVEN: Left controller con trigger presionado
        /// WHEN: Update() se ejecuta
        /// THEN: vehiclePhysics.brakeInput = valor del trigger
        /// </summary>
        [Test]
        public void InputMapper_LeftTrigger_MapsToBraking()
        {
            mockLeftController.SetTriggerValue(0.5f);
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.brakeInput, 0.5f, 0.01f,
                "Left trigger debe mapear a brakeInput");
        }

        /// <summary>
        /// TEST 2.2b: LeftTrigger_FullPress
        /// </summary>
        [Test]
        public void InputMapper_LeftTrigger_FullPress()
        {
            mockLeftController.SetTriggerValue(1.0f);
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.brakeInput, 1.0f, 0.01f);
        }

        // ============================================================
        // SPEC 2.3: Input de Dirección (Right Joystick)
        // ============================================================
        /// <summary>
        /// TEST 2.3: InputMapper_RightJoystick_MapsToSteering
        ///
        /// GIVEN: Right joystick X axis a 0.8
        /// WHEN: Update() se ejecuta
        /// THEN: vehiclePhysics.steerInput = 0.8
        /// </summary>
        [Test]
        public void InputMapper_RightJoystick_MapsToSteering()
        {
            mockRightController.SetJoystickValue(new Vector2(0.8f, 0.1f));
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.steerInput, 0.8f, 0.01f,
                "Right joystick X debe mapear a steerInput");
        }

        /// <summary>
        /// TEST 2.3b: RightJoystick_Left
        /// </summary>
        [Test]
        public void InputMapper_RightJoystick_SteerLeft()
        {
            mockRightController.SetJoystickValue(new Vector2(-0.5f, 0f));
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.steerInput, -0.5f, 0.01f,
                "Joystick negativo debe girar izquierda");
        }

        /// <summary>
        /// TEST 2.3c: RightJoystick_Centered
        /// </summary>
        [Test]
        public void InputMapper_RightJoystick_Centered()
        {
            mockRightController.SetJoystickValue(Vector2.zero);
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.steerInput, 0f, 0.01f,
                "Joystick centrado debe dar steer = 0");
        }

        // ============================================================
        // SPEC 2.4: Independencia de inputs
        // ============================================================
        /// <summary>
        /// TEST 2.4: MultipleInputs_AreIndependent
        ///
        /// Acelerar con trigger derecho no afecta frenado (trigger izquierdo)
        /// </summary>
        [Test]
        public void InputMapper_MultipleInputs_AreIndependent()
        {
            // Setup: acelerar y girar simultáneamente
            mockRightController.SetTriggerValue(1.0f);
            mockRightController.SetJoystickValue(new Vector2(0.7f, 0f));
            mockLeftController.SetTriggerValue(0f);

            inputMapper.Update();

            // Ambos inputs deben aplicarse independientemente
            Assert.AreEqual(vehiclePhysics.accelerationInput, 1.0f, 0.01f);
            Assert.AreEqual(vehiclePhysics.steerInput, 0.7f, 0.01f);
            Assert.AreEqual(vehiclePhysics.brakeInput, 0f, 0.01f);
        }

        /// <summary>
        /// TEST 2.5: AllInputsSimultaneous
        /// </summary>
        [Test]
        public void InputMapper_AllInputs_SimultaneouslyActive()
        {
            mockRightController.SetTriggerValue(0.8f);
            mockRightController.SetJoystickValue(new Vector2(0.5f, 0f));
            mockLeftController.SetTriggerValue(0.3f);

            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.accelerationInput, 0.8f, 0.01f);
            Assert.AreEqual(vehiclePhysics.steerInput, 0.5f, 0.01f);
            Assert.AreEqual(vehiclePhysics.brakeInput, 0.3f, 0.01f);
        }
    }
}
