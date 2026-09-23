using UnityEngine;
using UnityEngine.XR;
using VRCar.Tests.Mocks;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Mapea inputs de XR Controllers a VehiclePhysics
    /// Implementa la especificación SDD:
    /// - SPEC 2.1: Right trigger → accelerationInput
    /// - SPEC 2.2: Left trigger → brakeInput
    /// - SPEC 2.3: Right joystick X → steerInput
    ///
    /// Soporta mode de testing con MockXRController
    /// </summary>
    public class InputMapper : MonoBehaviour
    {
        [SerializeField]
        private VehiclePhysics vehiclePhysics;

        // Referencias a controllers reales (asignadas en inspector o runtime)
        private InputDevice rightControllerDevice;
        private InputDevice leftControllerDevice;

        // Para testing: mocks opcionales
        private MockXRController mockRightController;
        private MockXRController mockLeftController;
        private bool useMocks = false;

        private void OnEnable()
        {
            if (vehiclePhysics == null)
            {
                vehiclePhysics = GetComponent<VehiclePhysics>();
            }

            // Encontrar controllers en hardware real
            if (!useMocks)
            {
                InitializeRealControllers();
            }
        }

        private void InitializeRealControllers()
        {
            // Buscar controllers de XR
            var rightHandDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
            if (rightHandDevices.Count > 0)
            {
                rightControllerDevice = rightHandDevices[0];
            }

            var leftHandDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            if (leftHandDevices.Count > 0)
            {
                leftControllerDevice = leftHandDevices[0];
            }

            if (rightControllerDevice.isValid == false || leftControllerDevice.isValid == false)
            {
                Debug.LogWarning("XR Controllers no encontrados. Verifica que XR esté configurado.");
            }
        }

        private void Update()
        {
            if (vehiclePhysics == null) return;

            if (useMocks)
            {
                UpdateWithMocks();
            }
            else
            {
                UpdateWithRealControllers();
            }
        }

        /// <summary>
        /// Actualizar inputs desde controllers reales (Meta Quest 3)
        /// </summary>
        private void UpdateWithRealControllers()
        {
            // SPEC 2.1: Right trigger → accelerationInput
            if (rightControllerDevice.isValid)
            {
                if (rightControllerDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
                {
                    vehiclePhysics.accelerationInput = Mathf.Clamp01(triggerValue);
                }
            }

            // SPEC 2.2: Left trigger → brakeInput
            if (leftControllerDevice.isValid)
            {
                if (leftControllerDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
                {
                    vehiclePhysics.brakeInput = Mathf.Clamp01(triggerValue);
                }
            }

            // SPEC 2.3: Right joystick X → steerInput
            if (rightControllerDevice.isValid)
            {
                if (rightControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
                {
                    vehiclePhysics.steerInput = Mathf.Clamp(joystickValue.x, -1f, 1f);
                }
            }
        }

        /// <summary>
        /// Actualizar inputs desde mocks (para testing)
        /// </summary>
        private void UpdateWithMocks()
        {
            if (mockRightController != null)
            {
                vehiclePhysics.accelerationInput = mockRightController.GetTriggerValue();
                vehiclePhysics.steerInput = mockRightController.GetJoystickValue().x;
            }

            if (mockLeftController != null)
            {
                vehiclePhysics.brakeInput = mockLeftController.GetTriggerValue();
            }
        }

        /// <summary>
        /// Asignar mocks para testing (usado por InputMapperTests)
        /// </summary>
        public void SetMockControllers(MockXRController right, MockXRController left)
        {
            mockRightController = right;
            mockLeftController = left;
            useMocks = true;
        }

        /// <summary>
        /// Resetear a modo real (sin mocks)
        /// </summary>
        public void UseSealControllers()
        {
            useMocks = false;
            mockRightController = null;
            mockLeftController = null;
            InitializeRealControllers();
        }
    }
}
