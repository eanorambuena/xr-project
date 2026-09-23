using UnityEngine;
using TMPro;
using VRCar.Tests.Mocks;

namespace VRCar.UI
{
    /// <summary>
    /// Gestiona el HUD del vehículo mostrando distancia a obstáculos y velocidad
    /// Implementa la especificación SDD:
    /// - SPEC 4.1: Mostrar distancia
    /// - SPEC 4.2: Cambio de color por distancia
    /// - SPEC 4.3: Mostrar velocidad en km/h
    /// - SPEC 4.4: Manejar caso sin obstáculos (float.MaxValue)
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        // Referencias a UI
        private TextMeshProUGUI distanceText;
        private TextMeshProUGUI speedText;

        // Referencias a sistema
        private MockObstacleDetector obstacleDetector;
        private Rigidbody vehicleRigidbody;

        // Parámetros de color por distancia
        private const float DISTANCE_GREEN_THRESHOLD = 1.5f;  // > 1.5m = verde
        private const float DISTANCE_YELLOW_THRESHOLD = 1.0f; // 1.0-1.5m = amarillo
                                                                // < 1.0m = rojo

        /// <summary>
        /// Asignar referencias (usado por tests y setup)
        /// </summary>
        public void SetReferences(TextMeshProUGUI distText, TextMeshProUGUI speedTxt,
                                 MockObstacleDetector detector, Rigidbody rb)
        {
            distanceText = distText;
            speedText = speedTxt;
            obstacleDetector = detector;
            vehicleRigidbody = rb;
        }

        private void Start()
        {
            // Auto-find components si no fueron asignadas en SetReferences
            if (distanceText == null)
            {
                distanceText = FindComponentInChildren<TextMeshProUGUI>("DistanceText");
            }
            if (speedText == null)
            {
                speedText = FindComponentInChildren<TextMeshProUGUI>("SpeedText");
            }
            if (obstacleDetector == null)
            {
                obstacleDetector = FindObjectOfType<MockObstacleDetector>();
            }
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = FindObjectOfType<Rigidbody>();
            }
        }

        private void Update()
        {
            UpdateDistance();
            UpdateSpeed();
        }

        /// <summary>
        /// Actualizar distancia a obstáculo y cambiar color según cercanía
        /// SPEC 4.1 y 4.2
        /// </summary>
        private void UpdateDistance()
        {
            if (distanceText == null || obstacleDetector == null) return;

            float distance = obstacleDetector.GetClosestDistance();

            // Mostrar distancia
            if (float.IsInfinity(distance) || distance == float.MaxValue)
            {
                distanceText.text = "Distancia: ∞";
            }
            else
            {
                distanceText.text = $"Distancia: {distance:F1}m";
            }

            // Cambiar color según distancia
            // SPEC 4.2: distancia > 1.5m → verde
            //           1.0m < distancia ≤ 1.5m → amarillo
            //           distancia ≤ 1.0m → rojo
            if (distance > DISTANCE_GREEN_THRESHOLD)
            {
                distanceText.color = Color.green;
            }
            else if (distance > DISTANCE_YELLOW_THRESHOLD)
            {
                distanceText.color = Color.yellow;
            }
            else
            {
                distanceText.color = Color.red;
            }
        }

        /// <summary>
        /// Actualizar velocidad del vehículo en km/h
        /// SPEC 4.3
        /// </summary>
        private void UpdateSpeed()
        {
            if (speedText == null || vehicleRigidbody == null) return;

            float speedMs = vehicleRigidbody.velocity.magnitude;
            float speedKmh = speedMs * 3.6f; // Convertir m/s a km/h

            speedText.text = $"Velocidad: {speedKmh:F0} km/h";
        }

        /// <summary>
        /// Helper para encontrar componentes por nombre
        /// </summary>
        private T FindComponentInChildren<T>(string name) where T : Component
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.gameObject.name == name)
                {
                    return child.GetComponent<T>();
                }
            }
            return null;
        }
    }
}
