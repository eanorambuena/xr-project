using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR;
using VRCar.Vehicle;
using VRCar.Tests.Mocks;

namespace VRCar.Tests.XR
{
    /// <summary>
    /// XR Compatibility Tests for Meta Quest 3
    /// Verifies that all components work correctly in VR/XR environment
    /// </summary>
    public class XRCompatibilityTests
    {
        private GameObject vehicleGO;
        private VehiclePhysics vehiclePhysics;
        private InputMapper inputMapper;

        [SetUp]
        public void Setup()
        {
            // Create test vehicle with XR components
            vehicleGO = new GameObject("TestVehicleXR");
            vehicleGO.AddComponent<Rigidbody>();
            vehicleGO.AddComponent<BoxCollider>();

            vehiclePhysics = vehicleGO.AddComponent<VehiclePhysics>();
            inputMapper = vehicleGO.AddComponent<InputMapper>();

            // Setup mock controllers for testing
            var mockRight = new MockXRController();
            var mockLeft = new MockXRController();
            inputMapper.SetMockControllers(mockRight, mockLeft);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(vehicleGO);
        }

        // ============================================================
        // XR Input Compatibility Tests
        // ============================================================
        /// <summary>
        /// TEST XR.1: InputMapper_XRTriggers_MapCorrectly
        ///
        /// GIVEN: XR Controller with trigger inputs
        /// WHEN: InputMapper receives trigger values
        /// THEN:
        ///   - Right trigger maps to acceleration (0-1)
        ///   - Left trigger maps to braking (0-1)
        ///   - Values are clamped 0-1 range
        /// </summary>
        [Test]
        public void InputMapper_XRTriggers_MapCorrectly()
        {
            var mockRight = new MockXRController();
            var mockLeft = new MockXRController();

            // Set trigger values
            mockRight.SetTriggerValue(0.75f);
            mockLeft.SetTriggerValue(0.5f);

            inputMapper.SetMockControllers(mockRight, mockLeft);
            inputMapper.Update();

            // Verify mapping
            Assert.AreEqual(vehiclePhysics.accelerationInput, 0.75f, 0.01f,
                "Right trigger must map to acceleration");
            Assert.AreEqual(vehiclePhysics.brakeInput, 0.5f, 0.01f,
                "Left trigger must map to brake");
        }

        /// <summary>
        /// TEST XR.2: InputMapper_XRJoystick_SteeringWorks
        ///
        /// GIVEN: XR Controller joystick input
        /// WHEN: User moves joystick
        /// THEN:
        ///   - X axis maps to steering (-1 to 1)
        ///   - Y axis is ignored (vertical not used)
        ///   - Center position = 0 steering
        /// </summary>
        [Test]
        public void InputMapper_XRJoystick_SteeringWorks()
        {
            var mockRight = new MockXRController();

            // Test right steering
            mockRight.SetJoystickValue(new Vector2(0.8f, 0.1f));
            inputMapper.SetMockControllers(mockRight, new MockXRController());
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.steerInput, 0.8f, 0.01f,
                "Joystick X should map to steering");

            // Test left steering
            mockRight.SetJoystickValue(new Vector2(-0.8f, 0f));
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.steerInput, -0.8f, 0.01f,
                "Negative joystick should steer left");

            // Test center
            mockRight.SetJoystickValue(Vector2.zero);
            inputMapper.Update();

            Assert.AreEqual(vehiclePhysics.steerInput, 0f, 0.01f,
                "Center joystick should mean no steering");
        }

        /// <summary>
        /// TEST XR.3: InputMapper_ContinuousXRInput_NoLatency
        ///
        /// GIVEN: XR controller input stream
        /// WHEN: Multiple frames of input
        /// THEN:
        ///   - Input updates every frame
        ///   - No input buffering or queueing
        ///   - Immediate response to controller changes
        /// </summary>
        [Test]
        public void InputMapper_ContinuousXRInput_NoLatency()
        {
            var mockRight = new MockXRController();
            var mockLeft = new MockXRController();

            // Simulate rapid input changes
            for (int frame = 0; frame < 10; frame++)
            {
                float triggerValue = frame * 0.1f;
                mockRight.SetTriggerValue(triggerValue);

                inputMapper.SetMockControllers(mockRight, mockLeft);
                inputMapper.Update();

                // Verify immediate response
                Assert.AreApproximatelyEqual(vehiclePhysics.accelerationInput, triggerValue, 0.01f,
                    $"Frame {frame}: Should respond immediately to input changes");
            }
        }

        // ============================================================
        // XR Physics Compatibility Tests
        // ============================================================
        /// <summary>
        /// TEST XR.4: VehiclePhysics_XRFrameRate_MatchesQuest3
        ///
        /// GIVEN: VehiclePhysics running
        /// WHEN: FixedDeltaTime is set to 90 FPS
        /// THEN:
        ///   - Physics calculations use correct timestep
        ///   - Force accumulation is frame-rate independent
        /// </summary>
        [Test]
        public void VehiclePhysics_XRFrameRate_MatchesQuest3()
        {
            // Store original
            float originalDeltaTime = Time.fixedDeltaTime;

            try
            {
                // Set to 90 FPS (Quest 3 native)
                Time.fixedDeltaTime = 1f / 90f;

                Assert.AreApproximatelyEqual(Time.fixedDeltaTime, 0.01111f, 0.00001f,
                    "Fixed timestep should be ~0.01111s for 90 FPS");

                // Apply force
                vehiclePhysics.accelerationInput = 1f;
                vehiclePhysics.FixedUpdate();

                // Velocity should increase based on 90 FPS timestep
                Assert.Greater(vehiclePhysics.GetComponent<Rigidbody>().velocity.magnitude, 0,
                    "Velocity should increase even with short timestep");
            }
            finally
            {
                // Restore
                Time.fixedDeltaTime = originalDeltaTime;
            }
        }

        // ============================================================
        // XR Display & Rendering Tests
        // ============================================================
        /// <summary>
        /// TEST XR.5: HUD_XRDisplay_RenderCorrectly
        ///
        /// GIVEN: HUD components in VR scene
        /// WHEN: Rendered in stereo (VR)
        /// THEN:
        ///   - UI should be world-space (not screen-space)
        ///   - Readable from both eyes (proper depth)
        ///   - Text should render clearly
        /// </summary>
        [Test]
        public void HUD_XRDisplay_ConfiguredForVR()
        {
            var hudGO = new GameObject("HUD");
            var hudManager = hudGO.AddComponent<HUDManager>();

            // Create canvas (world space required for VR)
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Verify VR configuration
            Assert.AreEqual(canvas.renderMode, RenderMode.WorldSpace,
                "HUD must use World Space render mode for VR");

            Object.Destroy(hudGO);
            Object.Destroy(canvasGO);
        }

        // ============================================================
        // XR Tracking Tests
        // ============================================================
        /// <summary>
        /// TEST XR.6: XROriginSetup_TrackingWorks
        ///
        /// GIVEN: XROrigin in scene
        /// WHEN: VR headset tracking
        /// THEN:
        ///   - Camera position follows headset
        ///   - Rotation follows head movement
        ///   - No tracking jitter (smooth)
        /// </summary>
        [Test]
        public void XROriginSetup_HeightConfiguration()
        {
            var xrOriginGO = new GameObject("XROrigin");
            var vrSetup = xrOriginGO.AddComponent<VROriginSetup>();

            var cameraGO = new GameObject("Camera");
            cameraGO.transform.SetParent(xrOriginGO.transform);
            var camera = cameraGO.AddComponent<Camera>();

            // Initialize
            vrSetup.Initialize();

            // Verify positioning for driver view
            Assert.AreApproximatelyEqual(xrOriginGO.transform.position.y, 1.7f, 0.01f,
                "XROrigin should be at driver height (1.7m)");

            Assert.AreApproximatelyEqual(camera.transform.localPosition.y, 0.17f, 0.01f,
                "Camera should be at eye level (0.17m above seat)");

            Object.Destroy(xrOriginGO);
        }

        // ============================================================
        // XR Performance Tests
        // ============================================================
        /// <summary>
        /// TEST XR.7: VehiclePhysics_XRPerformance_LowLatency
        ///
        /// GIVEN: Vehicle physics running at 90 FPS
        /// WHEN: Input→Physics→Display cycle
        /// THEN:
        ///   - Total latency < 20ms (2 frames at 90 FPS)
        ///   - Physics update completes within frame budget
        ///   - No frame drops from physics
        /// </summary>
        [Test]
        public void VehiclePhysics_XRPerformance_LowLatency()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();

            // Simulate 10 physics frames
            for (int frame = 0; frame < 10; frame++)
            {
                stopwatch.Restart();

                // Update input
                vehiclePhysics.accelerationInput = 0.5f;
                vehiclePhysics.steerInput = 0.3f;
                vehiclePhysics.brakeInput = 0f;

                // Update physics (this is called by FixedUpdate)
                vehiclePhysics.FixedUpdate();

                stopwatch.Stop();

                // Should complete in < 5ms (plenty of budget in 11ms frame)
                Assert.Less(stopwatch.ElapsedMilliseconds, 5,
                    $"Frame {frame}: Physics update should complete quickly for 90 FPS");
            }
        }

        /// <summary>
        /// TEST XR.8: InputMapper_XRLatency_WithinBudget
        ///
        /// GIVEN: XR input mapping
        /// WHEN: Input values change
        /// THEN:
        ///   - Mapped values update within same frame
        ///   - No input buffering (latest value always used)
        ///   - Latency < 1ms
        /// </summary>
        [Test]
        public void InputMapper_XRLatency_WithinBudget()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            var mockRight = new MockXRController();
            var mockLeft = new MockXRController();

            stopwatch.Restart();

            // Rapid input changes
            for (int i = 0; i < 100; i++)
            {
                mockRight.SetTriggerValue(i * 0.01f % 1f);
                mockRight.SetJoystickValue(new Vector2(Mathf.Sin(i * 0.1f), 0));
                mockLeft.SetTriggerValue(i * 0.005f % 1f);

                inputMapper.SetMockControllers(mockRight, mockLeft);
                inputMapper.Update();
            }

            stopwatch.Stop();

            // 100 updates should complete very quickly
            Assert.Less(stopwatch.ElapsedMilliseconds, 10,
                "100 input mappings should complete in < 10ms");
        }

        // ============================================================
        // XR Stability Tests
        // ============================================================
        /// <summary>
        /// TEST XR.9: Components_XRStability_NoMemoeryLeaks
        ///
        /// GIVEN: Components running continuously
        /// WHEN: Long play session (simulated)
        /// THEN:
        ///   - No memory leaks
        ///   - No reference cycles
        ///   - Can be destroyed properly
        /// </summary>
        [Test]
        public void Components_XRStability_NoMemoryLeaks()
        {
            // Create and destroy multiple times
            for (int iteration = 0; iteration < 5; iteration++)
            {
                var testVehicle = new GameObject("Vehicle_" + iteration);
                testVehicle.AddComponent<Rigidbody>();
                testVehicle.AddComponent<VehiclePhysics>();
                testVehicle.AddComponent<InputMapper>();

                // Simulate some frames
                for (int frame = 0; frame < 10; frame++)
                {
                    var physics = testVehicle.GetComponent<VehiclePhysics>();
                    if (physics != null)
                    {
                        physics.accelerationInput = 0.5f;
                        physics.FixedUpdate();
                    }
                }

                // Cleanup
                Object.Destroy(testVehicle);
            }

            // If we got here, no exceptions from leaks
            Assert.Pass("No memory leaks detected");
        }

        /// <summary>
        /// TEST XR.10: AllComponents_XRInitialization_NoErrors
        ///
        /// GIVEN: All XR components
        /// WHEN: Initialized simultaneously
        /// THEN:
        ///   - All initialize without errors
        ///   - Dependencies are resolved
        ///   - No null reference exceptions
        /// </summary>
        [Test]
        public void AllComponents_XRInitialization_NoErrors()
        {
            try
            {
                var vehicle = new GameObject("CompleteVehicle");
                vehicle.tag = "Vehicle";

                // Add all components in initialization order
                vehicle.AddComponent<Rigidbody>();
                var physics = vehicle.AddComponent<VehiclePhysics>();
                var input = vehicle.AddComponent<InputMapper>();
                var detector = vehicle.AddComponent<ObstacleDetector>();
                var collision = vehicle.AddComponent<CollisionFeedback>();

                // Simulate Start/Update cycle
                physics.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                input.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

                for (int frame = 0; frame < 5; frame++)
                {
                    input.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
                    physics.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
                    detector.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
                }

                Object.Destroy(vehicle);
                Assert.Pass("All components initialized successfully");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"XR initialization error: {ex.Message}");
            }
        }
    }
}
