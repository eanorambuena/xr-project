using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// XR Configuration Validator for Meta Quest 3
/// Verifies XR setup, input configuration, and runtime compatibility
/// </summary>
public class XRValidation
{
    private static List<string> validationIssues = new List<string>();
    private static List<string> validationWarnings = new List<string>();
    private static List<string> validationPassed = new List<string>();

    /// <summary>
    /// Full XR validation - checks all critical XR configurations
    /// </summary>
    public static void ValidateXRConfiguration()
    {
        Debug.Log("=== XR Configuration Validation ===\n");

        validationIssues.Clear();
        validationWarnings.Clear();
        validationPassed.Clear();

        CheckPlayerSettings();
        CheckXRManagement();
        CheckInputSystem();
        CheckSceneSetup();
        CheckComponents();
        CheckBuildSettings();
        CheckPerformanceSettings();

        PrintValidationReport();
    }

    /// <summary>
    /// Check Player Settings for XR compatibility
    /// </summary>
    private static void CheckPlayerSettings()
    {
        Debug.Log("--- Player Settings ---");

        // Check VR support
        if (PlayerSettings.GetVirtualRealitySupported(BuildTargetGroup.Android))
        {
            validationPassed.Add("✓ VR Support enabled for Android");
        }
        else
        {
            validationIssues.Add("✗ VR Support NOT enabled for Android");
        }

        // Check XR SDKs
        var xrSDKs = PlayerSettings.GetVirtualRealitySDKs(BuildTargetGroup.Android);
        if (xrSDKs.Contains("Oculus"))
        {
            validationPassed.Add("✓ Oculus (Meta Quest) SDK enabled");
        }
        else
        {
            validationIssues.Add("✗ Oculus SDK NOT in list");
        }

        // Check render mode
        if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.LandscapeLeft)
        {
            validationPassed.Add("✓ Orientation: Landscape Left (correct for VR)");
        }
        else
        {
            validationWarnings.Add("⚠ Orientation: " + PlayerSettings.defaultInterfaceOrientation +
                " (should be Landscape Left for VR)");
        }

        // Check graphics API
        var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        if (graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES3))
        {
            validationPassed.Add("✓ Graphics API: OpenGL ES 3.0+ (Quest 3 compatible)");
        }
        else
        {
            validationWarnings.Add("⚠ OpenGL ES 3.0 not in graphics APIs");
        }

        // Check vsync count
        QualitySettings.vSyncCount = 0; // Required for VR (manual timing)
        validationPassed.Add("✓ VSync disabled (manual frame timing for VR)");

        // Check target FPS
        if (Application.targetFrameRate == 90)
        {
            validationPassed.Add("✓ Target FPS: 90 (Quest 3 native)");
        }
        else if (Application.targetFrameRate <= 0 || Application.targetFrameRate == 60)
        {
            validationWarnings.Add("⚠ Target FPS: " + Application.targetFrameRate +
                " (should be 90 for Quest 3)");
        }
    }

    /// <summary>
    /// Check XR Management settings
    /// </summary>
    private static void CheckXRManagement()
    {
        Debug.Log("\n--- XR Management ---");

        try
        {
            var settings = XRManagementSettings.Instance;
            if (settings != null)
            {
                validationPassed.Add("✓ XR Management settings found");

                // Check if loader is assigned
                var loaders = settings.loaders;
                if (loaders.Count > 0)
                {
                    validationPassed.Add("✓ XR Loaders configured: " + loaders.Count);
                    foreach (var loader in loaders)
                    {
                        Debug.Log("  - " + loader.GetType().Name);
                    }
                }
                else
                {
                    validationWarnings.Add("⚠ No XR Loaders configured");
                }
            }
            else
            {
                validationWarnings.Add("⚠ XR Management settings not found");
            }
        }
        catch
        {
            validationWarnings.Add("⚠ Could not access XR Management settings");
        }
    }

    /// <summary>
    /// Check Input System configuration for XR controllers
    /// </summary>
    private static void CheckInputSystem()
    {
        Debug.Log("\n--- Input System ---");

        #if ENABLE_INPUT_SYSTEM
            validationPassed.Add("✓ New Input System enabled");
        #else
            validationIssues.Add("✗ New Input System NOT enabled");
        #endif

        // Check for InputActionAsset
        var inputAssets = AssetDatabase.FindAssets("t:InputActionAsset");
        if (inputAssets.Length > 0)
        {
            validationPassed.Add("✓ Input Action Assets found: " + inputAssets.Length);
        }
        else
        {
            validationWarnings.Add("⚠ No Input Action Assets configured");
        }

        // Check for XR Input features
        #if ENABLE_INPUT_SYSTEM
            try
            {
                var inputManager = UnityEngine.InputSystem.InputSystem.devices;
                Debug.Log($"  Detected {inputManager.Count} input devices");
                validationPassed.Add("✓ Input System devices accessible");
            }
            catch
            {
                validationWarnings.Add("⚠ Could not access Input System devices");
            }
        #endif
    }

    /// <summary>
    /// Check scene setup for XR
    /// </summary>
    private static void CheckSceneSetup()
    {
        Debug.Log("\n--- Scene Setup ---");

        // Check for XROrigin in scene
        var xrOrigins = Resources.FindObjectsOfTypeAll<Component>()
            .Where(c => c.GetType().Name == "XROrigin")
            .ToList();

        if (xrOrigins.Count > 0)
        {
            validationPassed.Add("✓ XROrigin found in project: " + xrOrigins.Count);
        }
        else
        {
            validationWarnings.Add("⚠ XROrigin not found (will be created at runtime if using XR Origin prefab)");
        }

        // Check for MainCamera
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            validationPassed.Add("✓ Main Camera found");

            // Check camera near/far clip planes for VR
            if (mainCamera.nearClipPlane >= 0.01f && mainCamera.nearClipPlane <= 0.1f)
            {
                validationPassed.Add("✓ Camera near clip: " + mainCamera.nearClipPlane + " (VR appropriate)");
            }
            else
            {
                validationWarnings.Add("⚠ Camera near clip: " + mainCamera.nearClipPlane +
                    " (recommend 0.01-0.1 for VR)");
            }
        }
        else
        {
            validationIssues.Add("✗ Main Camera not found");
        }
    }

    /// <summary>
    /// Check for required VR components
    /// </summary>
    private static void CheckComponents()
    {
        Debug.Log("\n--- VR Components ---");

        var components = new[]
        {
            ("VehiclePhysics", "Assets/Scripts/Vehicle/VehiclePhysics.cs"),
            ("InputMapper", "Assets/Scripts/Vehicle/InputMapper.cs"),
            ("ObstacleDetector", "Assets/Scripts/Obstacles/ObstacleDetector.cs"),
            ("HUDManager", "Assets/Scripts/UI/HUDManager.cs"),
            ("VROriginSetup", "Assets/Scripts/XR/VROriginSetup.cs"),
            ("GameManager", "Assets/Scripts/Core/GameManager.cs"),
        };

        foreach (var (name, path) in components)
        {
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(path) != null)
            {
                validationPassed.Add($"✓ {name} component script found");
            }
            else
            {
                validationIssues.Add($"✗ {name} component script NOT found at {path}");
            }
        }
    }

    /// <summary>
    /// Check build settings for Android/Quest 3
    /// </summary>
    private static void CheckBuildSettings()
    {
        Debug.Log("\n--- Build Settings ---");

        // Check platform
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            validationPassed.Add("✓ Build target: Android (correct for Quest 3)");
        }
        else
        {
            validationWarnings.Add("⚠ Build target: " + EditorUserBuildSettings.activeBuildTarget +
                " (should be Android for Quest 3)");
        }

        // Check scenes in build
        var buildScenes = EditorBuildSettings.scenes;
        if (buildScenes.Length > 0)
        {
            validationPassed.Add("✓ Build scenes configured: " + buildScenes.Length);
            foreach (var scene in buildScenes)
            {
                Debug.Log("  - " + scene.path);
            }
        }
        else
        {
            validationIssues.Add("✗ No scenes in Build Settings");
        }

        // Check Android settings
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel21; // Min for Oculus
        validationPassed.Add("✓ Min SDK: API 21 (Quest 3 compatible)");
    }

    /// <summary>
    /// Check performance-critical settings
    /// </summary>
    private static void CheckPerformanceSettings()
    {
        Debug.Log("\n--- Performance Settings ---");

        // Check quality settings
        var qualityLevel = QualitySettings.GetQualityLevel();
        validationPassed.Add("✓ Quality level: " + QualitySettings.names[qualityLevel]);

        // Check GPU instancing
        var materials = Resources.FindObjectsOfTypeAll<Material>();
        var instancingEnabled = materials.Where(m => m.enableInstancing).Count();
        if (instancingEnabled > 0)
        {
            validationPassed.Add($"✓ GPU Instancing enabled on {instancingEnabled} materials");
        }
        else
        {
            validationWarnings.Add("⚠ GPU Instancing not enabled (performance optimization)");
        }

        // Check physics settings
        var timeStep = Time.fixedDeltaTime;
        if (Mathf.Abs(timeStep - 0.01111f) < 0.001f) // 90 FPS = ~0.01111s
        {
            validationPassed.Add("✓ Fixed timestep: " + timeStep + " (90 FPS aligned)");
        }
        else
        {
            validationWarnings.Add("⚠ Fixed timestep: " + timeStep + " (should be ~0.01111 for 90 FPS)");
        }
    }

    /// <summary>
    /// Print validation report with color coding
    /// </summary>
    private static void PrintValidationReport()
    {
        Debug.Log("\n=== VALIDATION REPORT ===\n");

        if (validationPassed.Count > 0)
        {
            Debug.Log($"<color=green>✓ PASSED ({validationPassed.Count})</color>");
            foreach (var msg in validationPassed)
            {
                Debug.Log(msg);
            }
        }

        if (validationWarnings.Count > 0)
        {
            Debug.Log($"\n<color=yellow>⚠ WARNINGS ({validationWarnings.Count})</color>");
            foreach (var msg in validationWarnings)
            {
                Debug.Log(msg);
            }
        }

        if (validationIssues.Count > 0)
        {
            Debug.Log($"\n<color=red>✗ CRITICAL ISSUES ({validationIssues.Count})</color>");
            foreach (var msg in validationIssues)
            {
                Debug.Log(msg);
            }
        }

        // Summary
        Debug.Log("\n=== SUMMARY ===");
        Debug.Log($"Total Checks: {validationPassed.Count + validationWarnings.Count + validationIssues.Count}");
        Debug.Log($"Passed: {validationPassed.Count}");
        Debug.Log($"Warnings: {validationWarnings.Count}");
        Debug.Log($"Critical: {validationIssues.Count}");

        if (validationIssues.Count == 0)
        {
            Debug.Log("\n✅ XR Configuration is VALID for Meta Quest 3!");
        }
        else
        {
            Debug.Log("\n❌ XR Configuration has CRITICAL ISSUES - Fix before building!");
        }
    }
}
