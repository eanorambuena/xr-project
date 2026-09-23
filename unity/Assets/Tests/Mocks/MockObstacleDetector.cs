using UnityEngine;
using VRCar.Obstacles;

namespace VRCar.Tests.Mocks
{
    /// <summary>
    /// Mock de ObstacleDetector para testing de HUDManager
    /// Simula detección de obstáculos sin necesidad de raycasts reales
    /// </summary>
    public class MockObstacleDetector : MonoBehaviour
    {
        private float mockDistance = float.MaxValue;

        public void SetDistance(float distance)
        {
            mockDistance = distance;
        }

        public float GetClosestDistance()
        {
            return mockDistance;
        }
    }
}
