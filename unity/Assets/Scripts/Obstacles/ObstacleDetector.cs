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
    public class ObstacleDetector : MonoBehaviour
    {
        // Parámetros de detección
        public float detectionRange = 10f;           // Rango máximo de detección (m)
        public float vehicleWidth = 1.856f;          // Ancho del vehículo Peugeot 308 (m)
        public float vehicleLength = 4.255f;         // Largo del vehículo (m)

        // Estado actual
        private float closestDistance = float.MaxValue;
        private Collider closestObstacle;

        private void FixedUpdate()
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

            // Calcular posiciones de los 4 corners frontales del vehículo
            // Puntos de inicio de raycast: esquinas delanteras izq/der
            Vector3 frontLeftCorner = transform.position +
                (transform.right * -vehicleWidth / 2f) +
                (transform.forward * vehicleLength / 2f);

            Vector3 frontRightCorner = transform.position +
                (transform.right * vehicleWidth / 2f) +
                (transform.forward * vehicleLength / 2f);

            // También raycast desde el centro para mayor cobertura
            Vector3 centerFront = transform.position +
                (transform.forward * vehicleLength / 2f);

            // Realizar raycasts desde cada corner
            CheckRaycast(frontLeftCorner);
            CheckRaycast(frontRightCorner);
            CheckRaycast(centerFront);
        }

        /// <summary>
        /// Lanzar raycast desde una posición y actualizar el obstáculo más cercano
        /// </summary>
        private void CheckRaycast(Vector3 origin)
        {
            Ray ray = new Ray(origin, transform.forward);
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
