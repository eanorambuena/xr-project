using NUnit.Framework;
using UnityEngine;
using VRCar.Core;
using VRCar.Vehicle;

namespace VRCar.Tests.Core
{
    public class ExerciseResetTests
    {
        GameObject go;
        [TearDown] public void TearDown() => Object.DestroyImmediate(go);

        [Test]
        public void Reset_RestoresStartPose_AndStopsCar()
        {
            go = new GameObject("Car");
            var rb = go.AddComponent<Rigidbody>(); rb.useGravity = false;
            var physics = go.AddComponent<VehiclePhysics>(); physics.Initialize();
            go.transform.SetPositionAndRotation(new Vector3(1, 0, 2), Quaternion.Euler(0, 90, 0));
            var reset = go.AddComponent<ExerciseReset>();
            reset.CaptureStart();

            go.transform.SetPositionAndRotation(new Vector3(5, 0, 7), Quaternion.Euler(0, 180, 0));
            rb.linearVelocity = new Vector3(0, 0, 3);
            physics.steerInput = 1f;

            reset.ResetExercise();

            Assert.AreEqual(new Vector3(1, 0, 2), go.transform.position);
            Assert.AreEqual(90f, go.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(Vector3.zero, rb.linearVelocity);
            Assert.AreEqual(0f, physics.CurrentSteerAngle);
            Assert.IsFalse(physics.IsReversing);
        }
    }
}
