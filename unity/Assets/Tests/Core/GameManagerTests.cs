using NUnit.Framework;
using UnityEngine;
using VRCar.Core;
using VRCar.Vehicle;
using VRCar.Obstacles;
using VRCar.Tests.Mocks;

namespace VRCar.Tests.Core
{
    /// <summary>
    /// Integration Tests para GameManager
    /// Verifica que todos los sistemas se inicialicen y conecten correctamente
    /// </summary>
    public class GameManagerTests
    {
        private GameObject gameManagerGO;
        private GameManager gameManager;
        private GameObject vehicleGO;
        private VehiclePhysics vehiclePhysics;
        private InputMapper inputMapper;
        private ObstacleDetector obstacleDetector;
        private CollisionFeedback collisionFeedback;

        [SetUp]
        public void Setup()
        {
            // Crear vehículo completo
            vehicleGO = new GameObject("Vehicle");
            vehicleGO.tag = "Vehicle";
            vehicleGO.AddComponent<Rigidbody>();
            vehicleGO.AddComponent<BoxCollider>();

            // Agregar componentes del vehículo
            vehiclePhysics = vehicleGO.AddComponent<VehiclePhysics>();
            inputMapper = vehicleGO.AddComponent<InputMapper>();
            obstacleDetector = vehicleGO.AddComponent<ObstacleDetector>();

            var meshRenderer = vehicleGO.AddComponent<MeshRenderer>();
            var audioSource = vehicleGO.AddComponent<AudioSource>();
            collisionFeedback = vehicleGO.AddComponent<CollisionFeedback>();
            collisionFeedback.SetReferences(meshRenderer, audioSource);

            // Crear GameManager
            gameManagerGO = new GameObject("GameManager");
            gameManager = gameManagerGO.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(gameManagerGO);
            Object.Destroy(vehicleGO);
        }

        // ============================================================
        // Integration Tests: Verificar inicialización de sistemas
        // ============================================================
        /// <summary>
        /// TEST I.1: GameManager_Initialize_FindsAllSystems
        ///
        /// GIVEN: GameManager en escena con vehículo configurado
        /// WHEN: Awake() se ejecuta
        /// THEN: Encuentra referencias a VehiclePhysics, InputMapper, ObstacleDetector
        /// </summary>
        [Test]
        public void GameManager_Initialize_FindsVehiclePhysics()
        {
            // Simular inicialización (Awake)
            gameManager.SendMessage("InitializeGameSystems");

            // Assert: obtener referencias debe funcionar
            VehiclePhysics physics = gameManager.GetVehiclePhysics();
            Assert.IsNotNull(physics, "GameManager debe encontrar VehiclePhysics");
            Assert.AreEqual(physics, vehiclePhysics, "Debe ser la misma instancia");
        }

        /// <summary>
        /// TEST I.1b: GameManager_Initialize_FindsInputMapper
        /// </summary>
        [Test]
        public void GameManager_Initialize_FindsInputMapper()
        {
            gameManager.SendMessage("InitializeGameSystems");

            InputMapper mapper = gameManager.GetInputMapper();
            Assert.IsNotNull(mapper, "GameManager debe encontrar InputMapper");
        }

        /// <summary>
        /// TEST I.1c: GameManager_Initialize_FindsObstacleDetector
        /// </summary>
        [Test]
        public void GameManager_Initialize_FindsObstacleDetector()
        {
            gameManager.SendMessage("InitializeGameSystems");

            ObstacleDetector detector = gameManager.GetObstacleDetector();
            Assert.IsNotNull(detector, "GameManager debe encontrar ObstacleDetector");
        }

        /// <summary>
        /// TEST I.1d: GameManager_Initialize_FindsCollisionFeedback
        /// </summary>
        [Test]
        public void GameManager_Initialize_FindsCollisionFeedback()
        {
            gameManager.SendMessage("InitializeGameSystems");

            CollisionFeedback feedback = gameManager.GetCollisionFeedback();
            Assert.IsNotNull(feedback, "GameManager debe encontrar CollisionFeedback");
        }

        // ============================================================
        // Integration Tests: Control del juego
        // ============================================================
        /// <summary>
        /// TEST I.2: GameManager_GameState_StartsRunning
        ///
        /// GIVEN: GameManager iniciado
        /// WHEN: Start() se ejecuta
        /// THEN: IsGameRunning es true
        /// </summary>
        [Test]
        public void GameManager_GameState_StartsRunning()
        {
            // Ejecutar inicialización e inicio
            gameManager.SendMessage("InitializeGameSystems");
            gameManager.SendMessage("StartGame");

            Assert.IsTrue(gameManager.IsGameRunning, "Juego debe estar corriendo");
        }

        /// <summary>
        /// TEST I.2b: GameManager_GameState_PauseResume
        /// </summary>
        [Test]
        public void GameManager_GameState_PauseResume()
        {
            gameManager.SendMessage("InitializeGameSystems");
            gameManager.SendMessage("StartGame");

            // Pausar
            gameManager.PauseGame();
            Assert.IsFalse(gameManager.IsGameRunning, "Juego debe estar pausado");
            Assert.AreEqual(Time.timeScale, 0f, "timeScale debe ser 0");

            // Reanudar
            gameManager.ResumeGame();
            Assert.IsTrue(gameManager.IsGameRunning, "Juego debe estar corriendo");
            Assert.AreEqual(Time.timeScale, 1f, "timeScale debe ser 1");
        }

        // ============================================================
        // Integration Tests: Comunicación entre sistemas
        // ============================================================
        /// <summary>
        /// TEST I.3: VehiclePhysics_InputMapper_Integration
        ///
        /// GIVEN: InputMapper y VehiclePhysics en mismo GameObject
        /// WHEN: InputMapper recibe input
        /// THEN: VehiclePhysics recibe ese input correctamente
        /// </summary>
        [Test]
        public void VehiclePhysics_InputMapper_Integration()
        {
            // Setup: mock controllers
            var mockRight = new MockXRController();
            var mockLeft = new MockXRController();

            mockRight.SetTriggerValue(0.75f);
            mockRight.SetJoystickValue(new Vector2(0.5f, 0f));
            mockLeft.SetTriggerValue(0.25f);

            inputMapper.SetMockControllers(mockRight, mockLeft);

            // Execute: actualizar inputs
            inputMapper.Update();

            // Assert: VehiclePhysics recibió los inputs
            Assert.AreEqual(vehiclePhysics.accelerationInput, 0.75f, 0.01f);
            Assert.AreEqual(vehiclePhysics.steerInput, 0.5f, 0.01f);
            Assert.AreEqual(vehiclePhysics.brakeInput, 0.25f, 0.01f);
        }

        /// <summary>
        /// TEST I.3b: ObstacleDetector_HUDManager_Integration
        /// Verificar que HUDManager puede leer distancias de ObstacleDetector
        /// </summary>
        [Test]
        public void ObstacleDetector_SystemAccess_Integration()
        {
            // El ObstacleDetector debe estar accesible y funcional
            ObstacleDetector detector = gameManager.GetObstacleDetector();
            Assert.IsNotNull(detector);

            // Debe poder retornar una distancia
            float distance = detector.GetClosestDistance();
            Assert.AreEqual(distance, float.MaxValue, "Sin obstáculos debe retornar MaxValue");
        }

        // ============================================================
        // Integration Tests: Verificación de configuración
        // ============================================================
        /// <summary>
        /// TEST I.4: VehiclePhysics_ConfigurationCorrect
        ///
        /// GIVEN: VehiclePhysics configurado desde GameManager
        /// THEN: Parámetros están en valores correctos según SPEC 1.1
        /// </summary>
        [Test]
        public void VehiclePhysics_ConfigurationCorrect()
        {
            VehiclePhysics physics = gameManager.GetVehiclePhysics();

            // Verificar parámetros según SPEC 1.1
            Assert.AreEqual(physics.maxSpeed, 20f, 0.01f, "maxSpeed debe ser 20 m/s");
            Assert.AreEqual(physics.accelerationForce, 5000f, 1f, "accelerationForce debe ser 5000 N");
            Assert.AreEqual(physics.brakingForce, 3000f, 1f, "brakingForce debe ser 3000 N");
            Assert.AreEqual(physics.maxSteerAngle, 45f, 0.01f, "maxSteerAngle debe ser 45°");
        }

        /// <summary>
        /// TEST I.4b: ObstacleDetector_ConfigurationCorrect
        /// GIVEN: ObstacleDetector configurado desde GameManager
        /// THEN: Parámetros están en valores correctos según SPEC 3.6
        /// </summary>
        [Test]
        public void ObstacleDetector_ConfigurationCorrect()
        {
            ObstacleDetector detector = gameManager.GetObstacleDetector();

            // Verificar parámetros según SPEC 3.6
            Assert.AreEqual(detector.detectionRange, 10f, 0.01f, "detectionRange debe ser 10m");
            Assert.AreEqual(detector.vehicleWidth, 1.856f, 0.001f, "vehicleWidth debe ser 1.856m (Peugeot 308)");
        }
    }
}
