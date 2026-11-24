using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using VRCar.Tests.Mocks;
using InputAction = UnityEngine.InputSystem.InputAction;
using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Mapea inputs de Meta Quest 3 (y teclado de respaldo en PC) a VehiclePhysics y CarRadio,
    /// además de exponer eventos de navegación/menú y freno de mano.
    ///   Gatillo derecho = acelerar · gatillo izquierdo = frenar (solo freno)
    ///   Agarre derecho = subir marcha (R→N→D) · agarre izquierdo = bajar marcha (D→N→R). El auto parte en N.
    ///   Botón Y = freno de mano (cierra el intento si ya se movió) · botón B = terminar (lo maneja ExperienceController)
    ///   Dirección: los dos controles como volante (WheelSteering); si no están, joystick izquierdo o teclado.
    ///   Joystick derecho = radio (clic: encender/apagar, izq/der: canción, arriba/abajo: volumen)
    /// </summary>
    public class InputMapper : MonoBehaviour
    {
        [SerializeField] private VehiclePhysics vehiclePhysics;
        [SerializeField] private CarRadio carRadio;

        [Header("Input System (opcional)")]
        [SerializeField] private InputActionAsset inputActions;
        private InputAction accelerateAction;
        private InputAction brakeAction;
        private InputAction steerAction;
        private WheelSteering wheel;   // volante con los dos controles (tiene prioridad cuando está rastreado y en uso)

        [Header("Giro alternativo con joystick izquierdo")]
        [Tooltip("Si está activo, el joystick izquierdo puede aportar giro cuando no se usa el volante con 2 controles")]
        public bool allowLeftStickSteering = true;
        [Tooltip("Tiempo (s) manteniendo B en el auto para volver al menú")]
        public float backHoldToMenuSeconds = 1.0f;

        private UnityEngine.XR.InputDevice rightControllerDevice;
        private UnityEngine.XR.InputDevice leftControllerDevice;

        private MockXRController mockRightController;
        private MockXRController mockLeftController;
        private bool useMocks = false;

        private bool prevRightGrip;
        private bool prevLeftGrip;
        private bool prevHandbrakeBtn;
        private bool prevRightStickClick;
        private bool prevA;
        private bool prevB;
        private float bHeldSeconds;
        private bool bMenuTriggered;

        public Vector2 LeftStickAxis { get; private set; }
        public Vector2 RightStickAxis { get; private set; }
        public float AlternativeSteerInput { get; private set; }

        public event System.Action OnSelectPressed;         // Botón A (menú)
        public event System.Action OnBackPressed;           // Botón B (menú)
        public event System.Action OnReturnToMenuHeld;      // Mantener B en el auto

        private void OnEnable()
        {
            if (vehiclePhysics == null) vehiclePhysics = GetComponent<VehiclePhysics>();
            if (carRadio == null) carRadio = GetComponent<CarRadio>();
            if (carRadio == null) carRadio = GetComponentInChildren<CarRadio>();

            if (!useMocks)
            {
                BindInputActions();
                RefreshControllers();
            }
        }

        public void SetInputActions(InputActionAsset asset)
        {
            inputActions = asset;
            BindInputActions();
        }

        private void BindInputActions()
        {
            if (inputActions == null) return;
            var map = inputActions.FindActionMap("VRCar");
            if (map == null) return;
            accelerateAction = map.FindAction("Accelerate");
            brakeAction = map.FindAction("Brake");
            steerAction = map.FindAction("Steer");
            map.Enable();
        }

        private void RefreshControllers()
        {
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
            if (rightHandDevices.Count > 0) rightControllerDevice = rightHandDevices[0];

            var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
            if (leftHandDevices.Count > 0) leftControllerDevice = leftHandDevices[0];
        }

        public void Update()
        {
            if (vehiclePhysics == null) vehiclePhysics = GetComponent<VehiclePhysics>();
            if (vehiclePhysics == null) return;

            if (useMocks)
            {
                UpdateWithMocks();
                return;
            }

            if (!rightControllerDevice.isValid || !leftControllerDevice.isValid)
                RefreshControllers();

            UpdateTriggersAndSteering();
            UpdateButtonsAndSticks();
        }

        private void UpdateTriggersAndSteering()
        {
            float accel = 0f;
            float brake = 0f;
            float actionSteer = 0f;

            if (accelerateAction != null)
            {
                accel = Mathf.Clamp01(accelerateAction.ReadValue<float>());
                brake = brakeAction != null ? Mathf.Clamp01(brakeAction.ReadValue<float>()) : 0f;
                actionSteer = steerAction != null ? Mathf.Clamp(steerAction.ReadValue<Vector2>().x, -1f, 1f) : 0f;
            }

            if (rightControllerDevice.isValid && rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float rTrig))
                accel = Mathf.Max(accel, Mathf.Clamp01(rTrig));

            if (leftControllerDevice.isValid && leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float lTrig))
                brake = Mathf.Max(brake, Mathf.Clamp01(lTrig));

            // Respaldo de teclado para pruebas rápidas en PC
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) accel = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) brake = 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) actionSteer = -1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) actionSteer = 1f;
            }

            vehiclePhysics.accelerationInput = accel;
            vehiclePhysics.brakeInput = brake;

            Vector2 lStick = Vector2.zero;
            if (leftControllerDevice.isValid && leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 ls))
                lStick = ls;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed) lStick.x = -1f;
                if (kb.rightArrowKey.isPressed) lStick.x = 1f;
                if (kb.upArrowKey.isPressed) lStick.y = 1f;
                if (kb.downArrowKey.isPressed) lStick.y = -1f;
            }
            LeftStickAxis = lStick;

            AlternativeSteerInput = allowLeftStickSteering && Mathf.Abs(lStick.x) > 0.05f
                ? Mathf.Clamp(lStick.x, -1f, 1f)
                : 0f;

            // Dirección: el volante (dos controles) manda cuando está rastreado, salvo que esté quieto y haya joystick/teclado
            if (wheel == null) wheel = GetComponent<WheelSteering>();
            bool wheelActive = wheel != null && wheel.HasInput;
            bool alternativeInUse = Mathf.Abs(AlternativeSteerInput) > 0.05f || Mathf.Abs(actionSteer) > 0.01f;
            if (wheelActive && !(alternativeInUse && Mathf.Abs(wheel.Steer) < 0.05f))
                vehiclePhysics.steerInput = wheel.Steer;
            else if (Mathf.Abs(actionSteer) > 0.01f)
                vehiclePhysics.steerInput = actionSteer;
            else if (allowLeftStickSteering)
                vehiclePhysics.steerInput = AlternativeSteerInput;
        }

        private void UpdateButtonsAndSticks()
        {
            bool rightGrip = false;
            bool leftGrip = false;
            bool handbrakePressed = false;
            bool aPressed = false;
            bool bPressed = false;
            bool rightStickClick = false;
            Vector2 rStick = Vector2.zero;

            if (rightControllerDevice.isValid)
            {
                rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out rightGrip);
                rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out aPressed);   // A
                rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bPressed); // B
                rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out rightStickClick);
                rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out rStick);
            }

            if (leftControllerDevice.isValid)
            {
                leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out leftGrip);
                // Y = freno de mano. X queda libre: mantener A/X reinicia el ejercicio (ExerciseReset)
                leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out handbrakePressed);
            }

            // Atajos de teclado en PC para probar sin visor
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.isPressed) rightGrip = true;
                if (kb.qKey.isPressed) leftGrip = true;
                if (kb.spaceKey.isPressed || kb.hKey.isPressed) handbrakePressed = true;
                if (kb.tKey.isPressed) rightStickClick = true;   // T = radio (M queda para terminar)
                if (kb.enterKey.isPressed) aPressed = true;
                if (kb.escapeKey.isPressed) bPressed = true;
                if (kb.periodKey.isPressed) rStick.x = 1f;
                if (kb.commaKey.isPressed) rStick.x = -1f;
                if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) rStick.y = 1f;
                if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) rStick.y = -1f;
            }

            RightStickAxis = rStick;

            // Gatillo interior/grip derecho: subir marcha (R -> N -> D)
            if (rightGrip && !prevRightGrip)
                vehiclePhysics.ShiftGearUp();
            prevRightGrip = rightGrip;

            // Gatillo interior/grip izquierdo: bajar marcha (D -> N -> R)
            if (leftGrip && !prevLeftGrip)
                vehiclePhysics.ShiftGearDown();
            prevLeftGrip = leftGrip;

            // Y: poner / sacar freno de mano
            if (handbrakePressed && !prevHandbrakeBtn)
                vehiclePhysics.ToggleHandbrake();
            prevHandbrakeBtn = handbrakePressed;

            // Botón A: seleccionar en menú
            if (aPressed && !prevA)
                OnSelectPressed?.Invoke();
            prevA = aPressed;

            // Botón B: retroceder en menú / mantener apretado en el auto vuelve al menú
            if (bPressed && !prevB)
            {
                OnBackPressed?.Invoke();
                bHeldSeconds = 0f;
                bMenuTriggered = false;
            }
            if (bPressed)
            {
                bHeldSeconds += Time.deltaTime;
                if (!bMenuTriggered && bHeldSeconds >= backHoldToMenuSeconds)
                {
                    bMenuTriggered = true;
                    OnReturnToMenuHeld?.Invoke();
                }
            }
            else
            {
                bHeldSeconds = 0f;
                bMenuTriggered = false;
            }
            prevB = bPressed;

            // Joystick derecho: apretar apaga/enciende radio; izq/der canción; arriba/abajo volumen 0..20
            if (carRadio != null)
            {
                if (rightStickClick && !prevRightStickClick)
                    carRadio.TogglePower();
                carRadio.HandleStickAxis(rStick, Time.time);
            }
            prevRightStickClick = rightStickClick;
        }

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

        public void SetMockControllers(MockXRController right, MockXRController left)
        {
            mockRightController = right;
            mockLeftController = left;
            useMocks = true;
        }

        public void UseSealControllers()
        {
            useMocks = false;
            mockRightController = null;
            mockLeftController = null;
            RefreshControllers();
        }
    }
}