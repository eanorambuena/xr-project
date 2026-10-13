using NUnit.Framework;
using UnityEngine;
using VRCar.Obstacles;

namespace VRCar.Tests.Obstacles
{
    /// <summary>Los sensores bajos deben detectar un tope de 15 cm detrás del auto en reversa.</summary>
    public class ObstacleDetectorLowTests
    {
        GameObject car, curb;

        [TearDown] public void TearDown() { Object.DestroyImmediate(car); Object.DestroyImmediate(curb); }

        [Test]
        public void DetectsLowCurbBehind_WhenReversing()
        {
            car = new GameObject("Car");
            car.transform.position = new Vector3(0, 0.7675f, 0);
            var box = car.AddComponent<BoxCollider>();
            box.size = new Vector3(1.856f, 1.535f, 4.255f);
            var det = car.AddComponent<ObstacleDetector>();
            det.SetReversing(true);

            curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.tag = "Obstacle";
            curb.transform.position = new Vector3(0, 0.075f, -(4.255f / 2f + 0.5f + 0.15f));
            curb.transform.localScale = new Vector3(5f, 0.15f, 0.3f);
            Physics.SyncTransforms();

            det.FixedUpdate();
            Assert.AreEqual(0.5f, det.GetClosestDistance(), 0.02f);
        }
    }
}
