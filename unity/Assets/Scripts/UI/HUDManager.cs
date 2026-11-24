using System.Globalization;
using UnityEngine;
using TMPro;
using VRCar.Obstacles;
using VRCar.Vehicle;

namespace VRCar.UI
{
    /// <summary>
    /// Gestiona el HUD del vehículo mostrando distancia frontal/trasera y velocidad.
    /// Implementa la especificación SDD:
    /// - SPEC 4.1: Mostrar distancia
    /// - SPEC 4.2: Cambio de color por distancia
    /// - SPEC 4.3: Mostrar velocidad en km/h
    /// - SPEC 4.4: Manejar caso sin obstáculos (float.MaxValue)
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        private TextMeshProUGUI distanceText;
        private TextMeshProUGUI speedText;
        private TextMeshProUGUI messageText;
        private string pendingMessage;

        private CarRadio radio;
        private float radioMessageUntil;   // mientras sea > 0 se muestra el aviso de la radio y luego vuelve el mensaje normal

        private IObstacleDetector obstacleDetector;
        private Rigidbody vehicleRigidbody;

        private const float DISTANCE_GREEN_THRESHOLD = 1.5f;  // > 1.5m = verde
        private const float DISTANCE_YELLOW_THRESHOLD = 1.0f; // 1.0-1.5m = amarillo
                                                                // <= 1.0m = rojo

        public void SetReferences(TextMeshProUGUI distText, TextMeshProUGUI speedTxt,
                                 IObstacleDetector detector, Rigidbody rb)
        {
            distanceText = distText;
            speedText = speedTxt;
            obstacleDetector = detector;
            vehicleRigidbody = rb;
        }

        private void Start()
        {
            if (distanceText == null)
                distanceText = FindComponentInChildren<TextMeshProUGUI>("DistanceText");
            if (speedText == null)
                speedText = FindComponentInChildren<TextMeshProUGUI>("SpeedText");
            if (obstacleDetector == null)
                obstacleDetector = FindFirstObjectByType<ObstacleDetector>();
            if (vehicleRigidbody == null)
                vehicleRigidbody = FindFirstObjectByType<Rigidbody>();
            if (messageText == null)
                messageText = FindComponentInChildren<TextMeshProUGUI>("MessageText");
            if (messageText != null && pendingMessage != null)
                messageText.text = pendingMessage;

            // Avisos de la radio (canción, volumen) un par de segundos
            if (radio == null) radio = FindFirstObjectByType<CarRadio>();
            if (radio != null) radio.OnRadioFeedback += ShowRadioMessage;
        }

        private void OnDestroy()
        {
            if (radio != null) radio.OnRadioFeedback -= ShowRadioMessage;
        }

        private void ShowRadioMessage(string message)
        {
            if (messageText == null) return;
            messageText.text = message;
            radioMessageUntil = Time.unscaledTime + 2.5f;
        }

        public void SetMessageText(TextMeshProUGUI messageTxt)
        {
            messageText = messageTxt;
        }

        public void ShowMessage(string message)
        {
            pendingMessage = message;
            radioMessageUntil = 0f;
            if (messageText != null) messageText.text = message;
        }

        public void Update()
        {
            UpdateDistance();
            UpdateSpeed();

            if (radioMessageUntil > 0f && Time.unscaledTime >= radioMessageUntil)
            {
                radioMessageUntil = 0f;
                if (messageText != null) messageText.text = pendingMessage ?? "";
            }
        }

        private static string FormatSingleDistance(float distance)
        {
            if (float.IsInfinity(distance) || distance >= float.MaxValue * 0.5f)
                return "libre";
            return distance.ToString("F1", CultureInfo.InvariantCulture) + "m";
        }

        private static Color ColorForDistance(float distance)
        {
            if (distance > DISTANCE_GREEN_THRESHOLD) return Color.green;
            if (distance > DISTANCE_YELLOW_THRESHOLD) return Color.yellow;
            return Color.red;
        }

        private void UpdateDistance()
        {
            if (distanceText == null || obstacleDetector == null) return;

            if (obstacleDetector is ObstacleDetector fullDetector)
            {
                float front = fullDetector.GetFrontDistance();
                float rear = fullDetector.GetRearDistance();

                distanceText.text = $"Frente: {FormatSingleDistance(front)} · Atrás: {FormatSingleDistance(rear)}";

                float minDist = Mathf.Min(front, rear);
                distanceText.color = ColorForDistance(minDist);
                return;
            }

            float distance = obstacleDetector.GetClosestDistance();
            distanceText.text = float.IsInfinity(distance) || distance >= float.MaxValue * 0.5f
                ? "Distancia: libre"
                : "Distancia: " + distance.ToString("F1", CultureInfo.InvariantCulture) + "m";
            distanceText.color = ColorForDistance(distance);
        }

        private void UpdateSpeed()
        {
            if (speedText == null || vehicleRigidbody == null) return;

            float speedMs = vehicleRigidbody.linearVelocity.magnitude;
            float speedKmh = speedMs * 3.6f;
            var physics = vehicleRigidbody.GetComponent<VRCar.Vehicle.VehiclePhysics>();
            string mode = physics != null && physics.IsReversing ? " · Reversa" : "";

            speedText.text = "Velocidad: " + speedKmh.ToString("F0", CultureInfo.InvariantCulture) + " km/h" + mode;
        }

        private T FindComponentInChildren<T>(string name) where T : Component
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.name == name)
                    return child.GetComponent<T>();
            }
            return null;
        }
    }
}