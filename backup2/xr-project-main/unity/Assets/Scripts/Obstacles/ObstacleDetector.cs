using UnityEngine;

namespace VRCar.Obstacles
{
    /// <summary>
    /// Detecta obstáculos adelante y atrás del vehículo mediante raycasts desde esquinas y sensores bajos.
    /// Mantiene compatibilidad con IObstacleDetector / GetClosestDistance() y expone distancias frontal y trasera.
    /// </summary>
    public class ObstacleDetector : MonoBehaviour, IObstacleDetector
    {
        public float detectionRange = 10f;           // Rango máximo de detección (m)
        public float vehicleWidth = 1.856f;          // Ancho del vehículo Peugeot 308 (m)
        public float vehicleLength = 4.255f;         // Largo del vehículo (m)
        public float lowSensorHeight = 0.10f;        // Altura de los sensores bajos sobre la base del auto (m)

        private float frontDistance = float.MaxValue;
        private float rearDistance = float.MaxValue;
        private Collider closestFrontObstacle;
        private Collider closestRearObstacle;

        private bool reversingOverride;
        private bool hasOverride;
        private VRCar.Vehicle.VehiclePhysics vehiclePhysics;

        public float FrontDistance => frontDistance;
        public float RearDistance => rearDistance;

        /// <summary>Forzar la dirección de detección principal (tests)</summary>
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
            DetectFrontAndRear();
        }

        private void DetectFrontAndRear()
        {
            DetectDirection(transform.forward, out frontDistance, out closestFrontObstacle);
            DetectDirection(-transform.forward, out rearDistance, out closestRearObstacle);
        }

        private void DetectDirection(Vector3 dir, out float bestDist, out Collider bestCollider)
        {
            bestDist = float.MaxValue;
            bestCollider = null;

            Vector3 leftCorner = transform.position +
                (transform.right * -vehicleWidth / 2f) +
                (dir * vehicleLength / 2f);

            Vector3 rightCorner = transform.position +
                (transform.right * vehicleWidth / 2f) +
                (dir * vehicleLength / 2f);

            Vector3 center = transform.position +
                (dir * vehicleLength / 2f);

            CheckRaycast(leftCorner, dir, ref bestDist, ref bestCollider);
            CheckRaycast(rightCorner, dir, ref bestDist, ref bestCollider);
            CheckRaycast(center, dir, ref bestDist, ref bestCollider);

            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                float bottom = box.center.y - box.size.y / 2f;
                Vector3 low = transform.up * (bottom + lowSensorHeight);
                CheckRaycast(leftCorner + low, dir, ref bestDist, ref bestCollider);
                CheckRaycast(rightCorner + low, dir, ref bestDist, ref bestCollider);
                CheckRaycast(center + low, dir, ref bestDist, ref bestCollider);
            }
        }

        private void CheckRaycast(Vector3 origin, Vector3 direction, ref float bestDist, ref Collider bestCollider)
        {
            Ray ray = new Ray(origin, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, detectionRange);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;

                if (hit.collider.CompareTag("Obstacle"))
                {
                    if (hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        bestCollider = hit.collider;
                    }
                }
            }
        }

        public float GetFrontDistance() => frontDistance;
        public float GetRearDistance() => rearDistance;

        /// <summary>
        /// Retorna la distancia en la dirección actual de marcha (o atrás si está en reversa).
        /// </summary>
        public float GetClosestDistance()
        {
            return IsReversing() ? rearDistance : frontDistance;
        }

        public Collider GetClosestObstacle()
        {
            return IsReversing() ? closestRearObstacle : closestFrontObstacle;
        }
    }
}