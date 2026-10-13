using UnityEngine;

namespace VRCar.Obstacles
{
    /// <summary>
    /// Detecta obstáculos adelante del vehículo mediante raycasts desde los 4 corners frontales
    /// Implementa la especificación SDD:
    /// - SPEC 3.1: Detección de distancia
    /// - SPEC 3.2: Múltiples obstáculos (retorna el más cercano)
    /// - SPEC 3.3: Sin obstáculos retorna float.MaxValue
    /// - SPEC 3.4: Ignora objetos sin tag "Obstacle"
    /// - SPEC 3.5: Detección lateral desde corners
    /// - SPEC 3.6: Rango de detección máximo (10m)
    /// </summary>
    public class ObstacleDetector : MonoBehaviour, IObstacleDetector
    {
        // Parámetros de detección
        public float detectionRange = 10f;           // Rango máximo de detección (m)
        public float vehicleWidth = 1.856f;          // Ancho del vehículo Peugeot 308 (m)
        public float vehicleLength = 4.255f;         // Largo del vehículo (m)
        public float lowSensorHeight = 0.10f;        // Altura de los sensores bajos sobre la base del auto (m)

        // Estado actual
        private float closestDistance = float.MaxValue;
        private Collider closestObstacle;

        // Dirección de detección: adelante, o atrás cuando el vehículo va en reversa
        private bool reversingOverride;
        private bool hasOverride;
        private VRCar.Vehicle.VehiclePhysics vehiclePhysics;

        /// <summary>Forzar la dirección de detección (tests)</summary>
        public void SetReversing(bool reversing)
        {
            reversingOverride = reversing;
            hasOverride = true;
        }

        private bool IsReversing()
        {
            if (hasOverride) return reversingOverride;
            if (vehiclePhysics == null) vehiclePhysics = GetComponent<VRCar.Vehicle.VehiclePhysics>();
            return vehiclePhysics != null && vehiclePhysics.IsReversing;
        }

        public void FixedUpdate()
        {
            DetectClosestObstacle();
        }

        /// <summary>
        /// Raycast desde los 4 corners frontales del vehículo
        /// Retorna la distancia al obstáculo más cercano con tag "Obstacle"
        /// </summary>
        private void DetectClosestObstacle()
        {
            closestDistance = float.MaxValue;
            closestObstacle = null;

            // En reversa se mide desde las esquinas traseras hacia atrás
            Vector3 dir = IsReversing() ? -transform.forward : transform.forward;

            // Calcular posiciones de las esquinas del parachoques en la dirección de avance
            // Puntos de inicio de raycast: esquinas delanteras izq/der
            Vector3 frontLeftCorner = transform.position +
                (transform.right * -vehicleWidth / 2f) +
                (dir * vehicleLength / 2f);

            Vector3 frontRightCorner = transform.position +
                (transform.right * vehicleWidth / 2f) +
                (dir * vehicleLength / 2f);

            // También raycast desde el centro para mayor cobertura
            Vector3 centerFront = transform.position +
                (dir * vehicleLength / 2f);

            // Realizar raycasts desde cada corner
            CheckRaycast(frontLeftCorner, dir);
            CheckRaycast(frontRightCorner, dir);
            CheckRaycast(centerFront, dir);

            // Sensores bajos (altura de parachoques inferior, ~10 cm del suelo): detectan topes/soleras
            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                float bottom = box.center.y - box.size.y / 2f;
                Vector3 low = transform.up * (bottom + lowSensorHeight);
                CheckRaycast(frontLeftCorner + low, dir);
                CheckRaycast(frontRightCorner + low, dir);
                CheckRaycast(centerFront + low, dir);
            }
        }

        /// <summary>
        /// Lanzar raycast desde una posición y actualizar el obstáculo más cercano
        /// </summary>
        private void CheckRaycast(Vector3 origin, Vector3 direction)
        {
            Ray ray = new Ray(origin, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, detectionRange);

            foreach (RaycastHit hit in hits)
            {
                // Solo contar objetos con tag "Obstacle"
                if (hit.collider.CompareTag("Obstacle"))
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestObstacle = hit.collider;
                    }
                }
            }
        }

        /// <summary>
        /// Retorna la distancia al obstáculo más cercano
        /// Retorna float.MaxValue si no hay obstáculos en rango
        /// </summary>
        public float GetClosestDistance()
        {
            return closestDistance;
        }

        /// <summary>
        /// Retorna referencia al obstáculo más cercano detectado
        /// Retorna null si no hay obstáculos
        /// </summary>
        public Collider GetClosestObstacle()
        {
            return closestObstacle;
        }
    }
}
