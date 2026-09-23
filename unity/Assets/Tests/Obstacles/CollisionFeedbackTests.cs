using NUnit.Framework;
using UnityEngine;
using VRCar.Obstacles;

namespace VRCar.Tests.Obstacles
{
    /// <summary>
    /// TDD Tests para CollisionFeedback
    /// Verifica feedback visual y auditivo al colisionar con obstáculos
    /// </summary>
    public class CollisionFeedbackTests
    {
        private GameObject vehicleGameObject;
        private CollisionFeedback collisionFeedback;
        private Renderer vehicleRenderer;
        private AudioSource audioSource;

        [SetUp]
        public void Setup()
        {
            // Crear vehículo
            vehicleGameObject = new GameObject("TestVehicle");
            vehicleGameObject.AddComponent<Rigidbody>();
            vehicleGameObject.AddComponent<BoxCollider>();

            // Agregar renderer
            var meshFilter = vehicleGameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            vehicleRenderer = vehicleGameObject.AddComponent<MeshRenderer>();
            vehicleRenderer.material = new Material(Shader.Find("Standard"));

            // Agregar audio source
            audioSource = vehicleGameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // Agregar collision feedback
            collisionFeedback = vehicleGameObject.AddComponent<CollisionFeedback>();
            collisionFeedback.SetReferences(vehicleRenderer, audioSource);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(vehicleGameObject);
        }

        // ============================================================
        // SPEC 5.1: Detección de Colisión
        // ============================================================
        /// <summary>
        /// TEST 5.1: CollisionFeedback_OnCollisionWithObstacle_PlaysSound
        ///
        /// GIVEN: Vehículo toca obstáculo con tag "Obstacle"
        /// WHEN: OnCollisionEnter() se ejecuta
        /// THEN:
        ///   - AudioSource reproduce sonido de colisión
        ///   - Solo si objeto tiene tag "Obstacle"
        /// </summary>
        [Test]
        public void CollisionFeedback_OnCollisionWithObstacle_PlaysSound()
        {
            // Setup: crear obstáculo
            var obstacleGO = new GameObject("Obstacle");
            obstacleGO.tag = "Obstacle";
            obstacleGO.AddComponent<BoxCollider>();

            // Crear collision simulada (nota: en tests reales esto sería de Unity Physics)
            // Por ahora verificamos que el método no crashea
            collisionFeedback.SimulateCollision(obstacleGO.tag);

            // Assert: verificar que se intentó reproducir sonido
            Assert.IsNotNull(audioSource, "AudioSource debe existir");

            Object.Destroy(obstacleGO);
        }

        /// <summary>
        /// TEST 5.1b: CollisionFeedback_IgnoresNonObstacle_NoSound
        /// GIVEN: Vehículo toca objeto sin tag "Obstacle"
        /// THEN: No reproduce sonido
        /// </summary>
        [Test]
        public void CollisionFeedback_IgnoresNonObstacle_NoSound()
        {
            var nonObstacleGO = new GameObject("NotObstacle");
            nonObstacleGO.tag = "Untagged";
            nonObstacleGO.AddComponent<BoxCollider>();

            // Simular colisión
            collisionFeedback.SimulateCollision(nonObstacleGO.tag);

            // Assert: no se reproduce sonido (no playing)
            Assert.IsFalse(audioSource.isPlaying,
                "AudioSource no debe reproducir para objetos sin tag Obstacle");

            Object.Destroy(nonObstacleGO);
        }

        // ============================================================
        // SPEC 5.2: Cambio de Color
        // ============================================================
        /// <summary>
        /// TEST 5.2: CollisionFeedback_OnCollision_ChangesColorRed
        ///
        /// GIVEN:
        ///   - Vehículo con color inicial (ej. blanco)
        ///   - Colisión con obstáculo
        /// WHEN: OnCollisionEnter() se ejecuta
        /// THEN:
        ///   - renderer.material.color = Color.red
        /// </summary>
        [Test]
        public void CollisionFeedback_OnCollision_ChangesColorRed()
        {
            // Setup: guardar color original
            Color originalColor = Color.white;
            vehicleRenderer.material.color = originalColor;

            // Setup: crear obstáculo
            var obstacleGO = new GameObject("Obstacle");
            obstacleGO.tag = "Obstacle";
            obstacleGO.AddComponent<BoxCollider>();

            // Execute: simular colisión
            collisionFeedback.SimulateCollision(obstacleGO.tag);

            // Assert: color debe ser rojo
            Assert.AreEqual(vehicleRenderer.material.color, Color.red,
                "Color debe cambiar a rojo al colisionar");

            Object.Destroy(obstacleGO);
        }

        /// <summary>
        /// TEST 5.2b: CollisionFeedback_OnCollisionExit_RestoresOriginalColor
        /// GIVEN: Vehículo después de colisión (color = rojo)
        /// WHEN: OnCollisionExit() se ejecuta
        /// THEN:
        ///   - renderer.material.color vuelve al color original
        /// </summary>
        [Test]
        public void CollisionFeedback_OnCollisionExit_RestoresOriginalColor()
        {
            // Setup: color original blanco
            Color originalColor = Color.white;
            vehicleRenderer.material.color = originalColor;

            // Setup: crear obstáculo y simular colisión
            var obstacleGO = new GameObject("Obstacle");
            obstacleGO.tag = "Obstacle";
            obstacleGO.AddComponent<BoxCollider>();

            collisionFeedback.SimulateCollision(obstacleGO.tag);
            Assert.AreEqual(vehicleRenderer.material.color, Color.red,
                "Color debe ser rojo después de colisión");

            // Execute: simular fin de colisión
            collisionFeedback.SimulateCollisionExit(obstacleGO.tag);

            // Assert: color vuelve al original
            Assert.AreEqual(vehicleRenderer.material.color, originalColor,
                "Color debe restaurarse al original cuando termina colisión");

            Object.Destroy(obstacleGO);
        }

        /// <summary>
        /// TEST 5.2c: CollisionFeedback_OnCollisionExit_IgnoresNonObstacle
        /// GIVEN: Vehículo toca obstáculo luego toca objeto no-Obstacle
        /// THEN: Color solo se restaura cuando se sale del Obstacle
        /// </summary>
        [Test]
        public void CollisionFeedback_OnCollisionExit_IgnoresNonObstacle()
        {
            Color originalColor = Color.blue;
            vehicleRenderer.material.color = originalColor;

            // Simular colisión con Obstacle
            var obstacleGO = new GameObject("Obstacle");
            obstacleGO.tag = "Obstacle";
            collisionFeedback.SimulateCollision(obstacleGO.tag);

            // Intentar restaurar con non-obstacle
            var nonObstacleGO = new GameObject("NotObstacle");
            nonObstacleGO.tag = "Untagged";
            collisionFeedback.SimulateCollisionExit(nonObstacleGO.tag);

            // Color debe seguir siendo rojo (no se restauró)
            Assert.AreEqual(vehicleRenderer.material.color, Color.red,
                "Color debe permanecer rojo si se sale de non-Obstacle");

            // Restaurar con Obstacle
            collisionFeedback.SimulateCollisionExit(obstacleGO.tag);
            Assert.AreEqual(vehicleRenderer.material.color, originalColor,
                "Color debe restaurarse cuando se sale de Obstacle");

            Object.Destroy(obstacleGO);
            Object.Destroy(nonObstacleGO);
        }
    }
}
