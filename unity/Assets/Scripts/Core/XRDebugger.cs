using UnityEngine;
using UnityEngine.XR;
using TMPro;
using VRCar.Vehicle;

namespace VRCar.Core
{
    /// <summary>
    /// XR Runtime Debugger - displays XR and performance metrics during gameplay
    /// Shows input, tracking, performance, and device information
    /// Press 'D' to toggle debug display
    /// </summary>
    public class XRDebugger : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI debugText;

        private bool debugVisible = false;
        private VehiclePhysics vehiclePhysics;
        private InputMapper inputMapper;

        // Performance tracking
        private float frameTime;
        private float targetFrameTime = 1f / 90f; // 90 FPS for Quest 3
        private int frameCount = 0;
        private float fpsUpdateTime = 0f;
        private float currentFPS = 0f;

        private void Start()
        {
            vehiclePhysics = GetComponent<VehiclePhysics>();
            inputMapper = GetComponent<InputMapper>();

            // Create debug canvas if not present
            if (debugText == null)
            {
                CreateDebugCanvas();
            }
        }

        private void Update()
        {
            // Toggle debug display with 'D' key
            if (Input.GetKeyDown(KeyCode.D))
            {
                debugVisible = !debugVisible;
                if (debugText != null)
                {
                    debugText.gameObject.SetActive(debugVisible);
                }
            }

            if (debugVisible && debugText != null)
            {
                UpdateDebugDisplay();
            }

            UpdateFPS();
        }

        /// <summary>
        /// Update FPS counter
        /// </summary>
        private void UpdateFPS()
        {
            frameCount++;
            fpsUpdateTime += Time.deltaTime;

            if (fpsUpdateTime >= 0.1f)
            {
                currentFPS = frameCount / fpsUpdateTime;
                frameCount = 0;
                fpsUpdateTime = 0f;
            }
        }

        /// <summary>
        /// Update debug display with XR information
        /// </summary>
        private void UpdateDebugDisplay()
        {
            string debugInfo = "=== XR DEBUG INFO ===\n\n";

            // Frame Rate
            debugInfo += $"<b>FRAME RATE</b>\n";
            debugInfo += $"FPS: {currentFPS:F1} (Target: 90)\n";
            debugInfo += $"Frame Time: {Time.deltaTime * 1000:F2}ms\n";
            debugInfo += $"Fixed DeltaTime: {Time.fixedDeltaTime * 1000:F2}ms\n\n";

            // XR Device Info
            debugInfo += GetXRDeviceInfo();

            // Input Info
            debugInfo += GetInputInfo();

            // Vehicle Physics Info
            debugInfo += GetVehiclePhysicsInfo();

            // XR Tracking Info
            debugInfo += GetTrackingInfo();

            // Performance Warning
            debugInfo += GetPerformanceWarnings();

            debugText.text = debugInfo;
        }

        /// <summary>
        /// Get XR device information
        /// </summary>
        private string GetXRDeviceInfo()
        {
            string info = "<b>XR DEVICE</b>\n";

            try
            {
                var devices = InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.HeadMounted);

                if (devices.Count > 0)
                {
                    var headset = devices[0];
                    info += $"Headset: {headset.name}\n";
                    info += $"Model: {headset.manufacturer}\n";
                    info += $"Connected: {headset.isValid}\n";
                }
                else
                {
                    info += "Headset: Not detected\n";
                }
            }
            catch
            {
                info += "Headset: Error reading device info\n";
            }

            // Check VR support
            info += $"VR Enabled: {XRSettings.isDeviceActive}\n";
            info += $"Eye Tracking: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}\n\n";

            return info;
        }

        /// <summary>
        /// Get controller input information
        /// </summary>
        private string GetInputInfo()
        {
            string info = "<b>INPUT</b>\n";

            try
            {
                // Right controller
                var rightDevices = InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right);
                if (rightDevices.Count > 0)
                {
                    var right = rightDevices[0];
                    if (right.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
                    {
                        info += $"Right Trigger: {trigger:F2}\n";
                    }
                    if (right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick))
                    {
                        info += $"Right Joystick: ({joystick.x:F2}, {joystick.y:F2})\n";
                    }
                }

                // Left controller
                var leftDevices = InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left);
                if (leftDevices.Count > 0)
                {
                    var left = leftDevices[0];
                    if (left.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
                    {
                        info += $"Left Trigger: {trigger:F2}\n";
                    }
                }

                // Mapped inputs to vehicle
                if (vehiclePhysics != null)
                {
                    info += $"\nMapped Inputs:\n";
                    info += $"Acceleration: {vehiclePhysics.accelerationInput:F2}\n";
                    info += $"Braking: {vehiclePhysics.brakeInput:F2}\n";
                    info += $"Steering: {vehiclePhysics.steerInput:F2}\n";
                }
            }
            catch
            {
                info += "Error reading input\n";
            }

            info += "\n";
            return info;
        }

        /// <summary>
        /// Get vehicle physics information
        /// </summary>
        private string GetVehiclePhysicsInfo()
        {
            string info = "<b>VEHICLE</b>\n";

            if (vehiclePhysics != null)
            {
                var rb = vehiclePhysics.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    float speed = rb.velocity.magnitude;
                    float speedKmh = speed * 3.6f;

                    info += $"Speed: {speed:F2} m/s ({speedKmh:F1} km/h)\n";
                    info += $"Max Speed: {vehiclePhysics.maxSpeed} m/s\n";
                    info += $"Position: ({transform.position.x:F1}, {transform.position.y:F1}, {transform.position.z:F1})\n";
                    info += $"Rotation: {transform.eulerAngles.y:F1}°\n";
                }
            }

            info += "\n";
            return info;
        }

        /// <summary>
        /// Get XR tracking information
        /// </summary>
        private string GetTrackingInfo()
        {
            string info = "<b>TRACKING</b>\n";

            try
            {
                var headsetDevices = InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.HeadMounted);

                if (headsetDevices.Count > 0)
                {
                    var headset = headsetDevices[0];

                    if (headset.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 position))
                    {
                        info += $"Head Pos: ({position.x:F2}, {position.y:F2}, {position.z:F2})\n";
                    }

                    if (headset.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rotation))
                    {
                        info += $"Head Rot: {rotation.eulerAngles.y:F1}°\n";
                    }

                    if (headset.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked))
                    {
                        info += $"Tracking: {(tracked ? "ACTIVE" : "LOST")}\n";
                    }
                }
                else
                {
                    info += "Headset not found\n";
                }
            }
            catch
            {
                info += "Error reading tracking data\n";
            }

            info += "\n";
            return info;
        }

        /// <summary>
        /// Get performance warnings
        /// </summary>
        private string GetPerformanceWarnings()
        {
            string warnings = "<b>PERFORMANCE</b>\n";

            // FPS warning
            if (currentFPS < 85)
            {
                warnings += $"<color=red>⚠ LOW FPS: {currentFPS:F1}</color>\n";
            }
            else if (currentFPS < 90)
            {
                warnings += $"<color=yellow>⚠ Below target: {currentFPS:F1}</color>\n";
            }
            else
            {
                warnings += $"<color=green>✓ FPS good: {currentFPS:F1}</color>\n";
            }

            // Frame time warning
            if (Time.deltaTime > targetFrameTime * 1.2f)
            {
                warnings += $"<color=red>⚠ Frame spike</color>\n";
            }

            // Memory info
            warnings += $"Memory: {System.GC.GetTotalMemory(false) / (1024 * 1024)}MB\n";

            return warnings;
        }

        /// <summary>
        /// Create debug canvas if not present
        /// </summary>
        private void CreateDebugCanvas()
        {
            var canvasGO = new GameObject("DebugCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var textGO = new GameObject("DebugText");
            textGO.transform.SetParent(canvasGO.transform);
            debugText = textGO.AddComponent<TextMeshProUGUI>();
            debugText.text = "Debug initialized...";
            debugText.fontSize = 24;
            debugText.alignment = TextAlignmentOptions.TopLeft;

            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1024, 512);

            canvasGO.SetActive(false);
        }
    }
}
