using UnityEngine;
using VRCar.Vehicle;

namespace VRCar.Core
{
    /// <summary>
    /// Reinicia el ejercicio: el auto vuelve a la posición inicial, detenido y sin reversa.
    /// Tecla R, o botón A/X del control (mantener 1 s para evitar reinicios accidentales).
    /// </summary>
    public class ExerciseReset : MonoBehaviour
    {
        public float holdSeconds = 1f;
        private Vector3 startPos;
        private Quaternion startRot;
        private bool captured;
        private float held;

        public void CaptureStart()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            captured = true;
        }

        private void Start() { if (!captured) CaptureStart(); }

        public void ResetExercise()
        {
            if (!captured) CaptureStart();
            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.position = startPos; rb.rotation = startRot; }
            transform.SetPositionAndRotation(startPos, startRot);
            GetComponent<VehiclePhysics>()?.ResetDrivingState();
            Physics.SyncTransforms();
            FindFirstObjectByType<UI.HUDManager>()?.ShowMessage("Ejercicio reiniciado: estaciona en el espacio A4");
        }

        private void Update()
        {
            bool key = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
            if (key) { ResetExercise(); return; }
            bool pressed = false;
            foreach (var node in new[] { UnityEngine.XR.XRNode.LeftHand, UnityEngine.XR.XRNode.RightHand })
            {
                var dev = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
                if (dev.isValid && dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool b) && b) pressed = true;
            }
            held = pressed ? held + Time.deltaTime : 0f;
            if (held >= holdSeconds) { held = float.MinValue; ResetExercise(); }
            if (!pressed && held < 0f) held = 0f;
        }
    }
}
