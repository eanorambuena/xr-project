using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Tests.Vehicle
{
    /// <summary>
    /// Soporte común para las pruebas de física por rueda: un suelo plano con collider
    /// (cara superior en y = 0) y un auto colocado a su altura de reposo, ya asentado.
    /// El modelo de ruedas con raycast necesita suelo: sin él las ruedas no tocan nada.
    /// </summary>
    public static class VehicleTestRig
    {
        public static GameObject CreateGround()
        {
            var ground = new GameObject("TestGround");
            var box = ground.AddComponent<BoxCollider>();
            box.size = new Vector3(500f, 1f, 500f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f); // cara superior en y = 0
            return ground;
        }

        /// <summary>Coloca el auto a su altura de reposo sobre el suelo, mirando a +Z, sin velocidad.</summary>
        public static void PlaceAtRest(GameObject car, VehiclePhysics physics)
        {
            car.transform.SetPositionAndRotation(new Vector3(0f, physics.restPivotHeight, 0f), Quaternion.identity);
            var rb = car.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        public static void Step(VehiclePhysics physics, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                physics.FixedUpdate();
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        public static int Frames(float seconds) => Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
    }
}
