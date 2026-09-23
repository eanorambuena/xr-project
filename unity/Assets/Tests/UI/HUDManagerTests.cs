using NUnit.Framework;
using UnityEngine;
using TMPro;
using VRCar.Tests.Mocks;
using VRCar.UI;

namespace VRCar.Tests.UI
{
    /// <summary>
    /// TDD Tests para HUDManager
    /// Verifica que el HUD muestre distancia, velocidad y cambien colores según condiciones
    /// </summary>
    public class HUDManagerTests
    {
        private GameObject hudGameObject;
        private HUDManager hudManager;
        private MockObstacleDetector mockObstacleDetector;
        private GameObject vehicleGameObject;
        private Rigidbody vehicleRigidbody;

        // Canvas y textos para el HUD
        private Canvas canvas;
        private TextMeshProUGUI distanceText;
        private TextMeshProUGUI speedText;

        [SetUp]
        public void Setup()
        {
            // Crear vehículo con Rigidbody
            vehicleGameObject = new GameObject("TestVehicle");
            vehicleRigidbody = vehicleGameObject.AddComponent<Rigidbody>();

            // Crear HUD GameObject
            hudGameObject = new GameObject("HUD");
            hudManager = hudGameObject.AddComponent<HUDManager>();

            // Crear Canvas para HUD
            var canvasGameObject = new GameObject("Canvas");
            canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Crear textos
            var distanceTextGO = new GameObject("DistanceText");
            distanceTextGO.transform.SetParent(canvasGameObject.transform);
            distanceText = distanceTextGO.AddComponent<TextMeshProUGUI>();

            var speedTextGO = new GameObject("SpeedText");
            speedTextGO.transform.SetParent(canvasGameObject.transform);
            speedText = speedTextGO.AddComponent<TextMeshProUGUI>();

            // Crear mock de ObstacleDetector
            mockObstacleDetector = new GameObject("MockDetector")
                .AddComponent<MockObstacleDetector>();

            // Asignar referencias al HUDManager
            hudManager.SetReferences(distanceText, speedText, mockObstacleDetector, vehicleRigidbody);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(hudGameObject);
            Object.Destroy(vehicleGameObject);
            Object.Destroy(mockObstacleDetector.gameObject);
            if (distanceText != null) Object.Destroy(distanceText.gameObject.transform.parent.gameObject);
        }

        // ============================================================
        // SPEC 4.1: Mostrar Distancia
        // ============================================================
        /// <summary>
        /// TEST 4.1: HUDManager_UpdateDistance_DisplaysCorrectValue
        ///
        /// GIVEN: ObstacleDetector retorna 5.0m
        /// WHEN: Update() se ejecuta
        /// THEN:
        ///   - distanceText muestra "Distancia: 5.0m"
        ///   - Color es verde (distancia > 1.5m)
        /// </summary>
        [Test]
        public void HUDManager_UpdateDistance_DisplaysCorrectValue()
        {
            // Setup: ObstacleDetector retorna 5.0m
            mockObstacleDetector.SetDistance(5.0f);

            // Execute
            hudManager.Update();

            // Assert: texto muestra distancia
            Assert.That(distanceText.text, Contains.Substring("5.0"),
                "HUD debe mostrar la distancia del obstáculo");
        }

        /// <summary>
        /// TEST 4.1b: HUDManager_UpdateDistance_ColorGreen_WhenFar
        /// GIVEN: Distancia > 1.5m (segura)
        /// THEN: Color del texto es verde
        /// </summary>
        [Test]
        public void HUDManager_UpdateDistance_ColorGreen_WhenFar()
        {
            mockObstacleDetector.SetDistance(5.0f);
            hudManager.Update();

            Assert.AreEqual(distanceText.color, Color.green,
                "Color debe ser verde cuando hay distancia segura (> 1.5m)");
        }

        // ============================================================
        // SPEC 4.2: Cambio de Color por Distancia
        // ============================================================
        /// <summary>
        /// TEST 4.2: HUDManager_UpdateDistance_ColorChangesWithDistance
        ///
        /// GIVEN: ObstacleDetector retorna diferentes distancias
        /// WHEN: Update() se ejecuta
        /// THEN:
        ///   - distancia > 1.5m → Color.green
        ///   - 1.0m < distancia ≤ 1.5m → Color.yellow
        ///   - distancia ≤ 1.0m → Color.red
        /// </summary>
        [Test]
        public void HUDManager_UpdateDistance_ColorYellow_WhenModerate()
        {
            // Setup: distancia moderada (1.2m)
            mockObstacleDetector.SetDistance(1.2f);

            // Execute
            hudManager.Update();

            // Assert: color amarillo
            Assert.AreEqual(distanceText.color, Color.yellow,
                "Color debe ser amarillo cuando distancia es moderada (1.0-1.5m)");
        }

        /// <summary>
        /// TEST 4.2b: HUDManager_UpdateDistance_ColorRed_WhenDangerous
        /// GIVEN: Distancia ≤ 1.0m (peligroso)
        /// THEN: Color es rojo
        /// </summary>
        [Test]
        public void HUDManager_UpdateDistance_ColorRed_WhenClose()
        {
            mockObstacleDetector.SetDistance(0.5f);
            hudManager.Update();

            Assert.AreEqual(distanceText.color, Color.red,
                "Color debe ser rojo cuando hay peligro de colisión (≤ 1.0m)");
        }

        /// <summary>
        /// TEST 4.2c: HUDManager_UpdateDistance_ColorGreen_Boundary
        /// GIVEN: Distancia exactamente en límites
        /// </summary>
        [Test]
        public void HUDManager_UpdateDistance_ColorBoundaries()
        {
            // 1.5m exacto debe ser verde (> 1.5m es la condición)
            mockObstacleDetector.SetDistance(1.51f);
            hudManager.Update();
            Assert.AreEqual(distanceText.color, Color.green, "1.51m debe ser verde");

            // 1.5m exacto es el límite superior de amarillo
            mockObstacleDetector.SetDistance(1.5f);
            hudManager.Update();
            Assert.AreEqual(distanceText.color, Color.yellow, "1.5m debe ser amarillo");

            // 1.0m exacto es el límite superior de rojo
            mockObstacleDetector.SetDistance(1.0f);
            hudManager.Update();
            Assert.AreEqual(distanceText.color, Color.red, "1.0m debe ser rojo");
        }

        // ============================================================
        // SPEC 4.3: Mostrar Velocidad
        // ============================================================
        /// <summary>
        /// TEST 4.3: HUDManager_UpdateSpeed_ConvertsToKmh
        ///
        /// GIVEN: Rigidbody con velocidad de 10 m/s
        /// WHEN: Update() se ejecuta
        /// THEN:
        ///   - speedText muestra "36" (10 * 3.6 = 36 km/h)
        /// </summary>
        [Test]
        public void HUDManager_UpdateSpeed_ConvertsToKmh()
        {
            // Setup: vehículo con velocidad de 10 m/s
            vehicleRigidbody.velocity = new Vector3(10, 0, 0);

            // Execute
            hudManager.Update();

            // Assert: velocidad en km/h (10 * 3.6 = 36 km/h)
            Assert.That(speedText.text, Contains.Substring("36"),
                "HUD debe mostrar 36 km/h (10 m/s * 3.6)");
        }

        /// <summary>
        /// TEST 4.3b: HUDManager_UpdateSpeed_VelocityZero
        /// GIVEN: Vehículo parado (velocidad = 0)
        /// THEN: speedText muestra "0"
        /// </summary>
        [Test]
        public void HUDManager_UpdateSpeed_VelocityZero()
        {
            vehicleRigidbody.velocity = Vector3.zero;
            hudManager.Update();

            Assert.That(speedText.text, Contains.Substring("0"),
                "HUD debe mostrar 0 km/h cuando vehículo está parado");
        }

        /// <summary>
        /// TEST 4.3c: HUDManager_UpdateSpeed_MaxSpeed
        /// GIVEN: Vehículo a velocidad máxima (20 m/s = 72 km/h)
        /// </summary>
        [Test]
        public void HUDManager_UpdateSpeed_MaxSpeed()
        {
            vehicleRigidbody.velocity = new Vector3(0, 0, 20);
            hudManager.Update();

            Assert.That(speedText.text, Contains.Substring("72"),
                "HUD debe mostrar 72 km/h (20 m/s * 3.6)");
        }

        // ============================================================
        // SPEC 4.4: Sin obstáculos (distancia infinita)
        // ============================================================
        /// <summary>
        /// TEST 4.4: HUDManager_NoObstacles_DisplaysMaxValue
        /// GIVEN: ObstacleDetector retorna float.MaxValue (sin obstáculos)
        /// THEN: HUD muestra distancia muy grande (no crashea)
        /// </summary>
        [Test]
        public void HUDManager_NoObstacles_DisplaysMaxValue()
        {
            mockObstacleDetector.SetDistance(float.MaxValue);
            hudManager.Update();

            // No debe crashear, solo verificar que el texto existe
            Assert.IsNotNull(distanceText.text,
                "HUD debe manejar float.MaxValue sin errores");
        }
    }
}
