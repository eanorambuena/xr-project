using NUnit.Framework;
using UnityEngine;

namespace VRCar.Tests.Obstacles
{
    /// <summary>
    /// TDD Tests para ObstacleDetector
    /// Verifica detección de distancia a obstáculos mediante raycasts
    /// </summary>
    public class ObstacleDetectorTests
    {
        private GameObject vehicleGameObject;
        private ObstacleDetector obstacleDetector;

        [SetUp]
        public void Setup()
        {
            vehicleGameObject = new GameObject("TestVehicle");
            vehicleGameObject.AddComponent<Collider>();
            obstacleDetector = vehicleGameObject.AddComponent<ObstacleDetector>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(vehicleGameObject);
        }

        // ============================================================
        // SPEC 3.1: Detección básica de distancia
        // ============================================================
        /// <summary>
        /// TEST 3.1: ObstacleDetector_WithObstacleAhead_ReturnsCorrectDistance
        ///
        /// GIVEN:
        ///   - Vehículo en (0, 0, 0)
        ///   - Obstáculo en (0, 0, 5) a 5m adelante
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - GetClosestDistance() retorna ~5.0m (±0.1m)
        ///   - Solo detecta objetos con tag "Obstacle"
        /// </summary>
        [Test]
        public void ObstacleDetector_WithObstacleAhead_ReturnsCorrectDistance()
        {
            // Setup: crear obstáculo
            var obstacle = new GameObject("TestObstacle");
            obstacle.tag = "Obstacle";
            obstacle.transform.position = new Vector3(0, 0, 5); // 5m adelante
            obstacle.AddComponent<BoxCollider>();

            vehicleGameObject.transform.position = new Vector3(0, 0, 0);
            vehicleGameObject.transform.rotation = Quaternion.identity;

            // Execute
            obstacleDetector.FixedUpdate();

            // Assert
            float distance = obstacleDetector.GetClosestDistance();
            Assert.AreApproximatelyEqual(distance, 5.0f, 0.2f,
                "Distancia al obstáculo debe ser ~5.0m");

            Object.Destroy(obstacle);
        }

        // ============================================================
        // SPEC 3.2: Múltiples obstáculos - detectar el más cercano
        // ============================================================
        /// <summary>
        /// TEST 3.2: ObstacleDetector_WithMultipleObstacles_ReturnClosest
        ///
        /// GIVEN:
        ///   - Obstáculo 1 a 5m adelante
        ///   - Obstáculo 2 a 3m adelante (más cercano)
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - GetClosestDistance() retorna 3.0m (el más cercano)
        /// </summary>
        [Test]
        public void ObstacleDetector_WithMultipleObstacles_ReturnClosest()
        {
            // Setup: crear múltiples obstáculos
            var obstacle1 = new GameObject("Obstacle1");
            obstacle1.tag = "Obstacle";
            obstacle1.transform.position = new Vector3(0, 0, 5); // 5m
            obstacle1.AddComponent<BoxCollider>();

            var obstacle2 = new GameObject("Obstacle2");
            obstacle2.tag = "Obstacle";
            obstacle2.transform.position = new Vector3(0, 0, 3); // 3m (más cercano)
            obstacle2.AddComponent<BoxCollider>();

            vehicleGameObject.transform.position = new Vector3(0, 0, 0);
            vehicleGameObject.transform.rotation = Quaternion.identity;

            // Execute
            obstacleDetector.FixedUpdate();

            // Assert
            float distance = obstacleDetector.GetClosestDistance();
            Assert.AreApproximatelyEqual(distance, 3.0f, 0.2f,
                "Debe retornar la distancia al obstáculo más cercano");

            Object.Destroy(obstacle1);
            Object.Destroy(obstacle2);
        }

        // ============================================================
        // SPEC 3.3: Sin obstáculos en rango
        // ============================================================
        /// <summary>
        /// TEST 3.3: ObstacleDetector_NoObstacles_ReturnsMaxValue
        ///
        /// GIVEN: Sin obstáculos en la escena
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - GetClosestDistance() retorna float.MaxValue
        /// </summary>
        [Test]
        public void ObstacleDetector_NoObstacles_ReturnsMaxValue()
        {
            vehicleGameObject.transform.position = new Vector3(0, 0, 0);

            // Execute
            obstacleDetector.FixedUpdate();

            // Assert
            float distance = obstacleDetector.GetClosestDistance();
            Assert.AreEqual(distance, float.MaxValue,
                "Sin obstáculos debe retornar MaxValue");
        }

        // ============================================================
        // SPEC 3.4: Ignorar objetos sin tag "Obstacle"
        // ============================================================
        /// <summary>
        /// TEST 3.4: ObstacleDetector_IgnoresNonObstacles
        ///
        /// GIVEN:
        ///   - GameObject sin tag "Obstacle" a 5m
        ///   - Sin otros obstáculos
        /// WHEN: FixedUpdate() se ejecuta
        /// THEN:
        ///   - GetClosestDistance() retorna float.MaxValue (ignoró el objeto)
        /// </summary>
        [Test]
        public void ObstacleDetector_IgnoresNonObstacles()
        {
            // Setup: objeto SIN tag "Obstacle"
            var ignoredObject = new GameObject("NotAnObstacle");
            ignoredObject.tag = "Untagged"; // No es un obstáculo
            ignoredObject.transform.position = new Vector3(0, 0, 5);
            ignoredObject.AddComponent<BoxCollider>();

            vehicleGameObject.transform.position = new Vector3(0, 0, 0);
            vehicleGameObject.transform.rotation = Quaternion.identity;

            // Execute
            obstacleDetector.FixedUpdate();

            // Assert
            float distance = obstacleDetector.GetClosestDistance();
            Assert.AreEqual(distance, float.MaxValue,
                "Debe ignorar objetos sin tag 'Obstacle'");

            Object.Destroy(ignoredObject);
        }

        // ============================================================
        // SPEC 3.5: Detección lateral (corners del auto)
        // ============================================================
        /// <summary>
        /// TEST 3.5: ObstacleDetector_DetectsLateral_FromVehicleCorners
        ///
        /// Verifica que detecta obstáculos en el rango frontal
        /// desde los 4 corners del vehículo (ancho = 1.856m)
        /// </summary>
        [Test]
        public void ObstacleDetector_DetectsFromMultipleCorners()
        {
            // Setup: vehículo con ancho 1.856m (Peugeot 308)
            vehicleGameObject.transform.position = Vector3.zero;

            // Obstáculo a la derecha frontal (corner derecho)
            var obstacleRight = new GameObject("ObstacleRight");
            obstacleRight.tag = "Obstacle";
            obstacleRight.transform.position = new Vector3(1.5f, 0, 3f);
            obstacleRight.AddComponent<BoxCollider>();

            // Execute
            obstacleDetector.FixedUpdate();

            // Assert
            float distance = obstacleDetector.GetClosestDistance();
            Assert.Less(distance, 3.5f,
                "Debe detectar obstáculo en corner frontal");

            Object.Destroy(obstacleRight);
        }

        // ============================================================
        // SPEC 3.6: Rango de detección máximo
        // ============================================================
        /// <summary>
        /// TEST 3.6: ObstacleDetector_RangeLimit
        ///
        /// GIVEN: Obstáculo a 15m adelante (dentro de rango)
        /// THEN: Se detecta
        ///
        /// GIVEN: Obstáculo a 30m adelante (fuera de rango)
        /// THEN: No se detecta
        /// </summary>
        [Test]
        public void ObstacleDetector_RespectRangeLimit()
        {
            vehicleGameObject.transform.position = Vector3.zero;

            // Obstáculo dentro de rango (10m)
            var nearObstacle = new GameObject("NearObstacle");
            nearObstacle.tag = "Obstacle";
            nearObstacle.transform.position = new Vector3(0, 0, 8f);
            nearObstacle.AddComponent<BoxCollider>();

            obstacleDetector.FixedUpdate();
            float nearDistance = obstacleDetector.GetClosestDistance();
            Assert.Less(nearDistance, float.MaxValue, "Debe detectar obstáculo cercano");

            Object.Destroy(nearObstacle);

            // Obstáculo fuera de rango (20m)
            var farObstacle = new GameObject("FarObstacle");
            farObstacle.tag = "Obstacle";
            farObstacle.transform.position = new Vector3(0, 0, 20f);
            farObstacle.AddComponent<BoxCollider>();

            obstacleDetector.FixedUpdate();
            float farDistance = obstacleDetector.GetClosestDistance();
            Assert.AreEqual(farDistance, float.MaxValue, "No debe detectar obstáculo lejano");

            Object.Destroy(farObstacle);
        }
    }
}
