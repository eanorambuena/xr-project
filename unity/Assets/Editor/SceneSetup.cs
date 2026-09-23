using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VRCar.Vehicle;
using VRCar.Core;

/// <summary>
/// Automated Scene Setup for CU1_CornerJudgment
/// Creates complete VR scene with all GameObjects, components, and settings
/// Usage: Menu → VRCar → Setup CU1 Scene
/// </summary>
public class SceneSetup
{
    [MenuItem("VRCar/Setup CU1 Scene")]
    public static void SetupCU1Scene()
    {
        Debug.Log("🚀 Setting up CU1_CornerJudgment scene...");

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

        // Setup environment
        SetupEnvironment(scene);

        // Setup XR Origin & Controllers
        SetupXROrigin(scene);

        // Setup Vehicle
        var vehicleGO = SetupVehicle(scene);

        // Setup Obstacles
        SetupObstacles(scene);

        // Setup UI
        SetupUI(scene);

        // Setup GameManager
        SetupGameManager(scene);

        // Save scene
        string scenePath = "Assets/Scenes/CU1_CornerJudgment.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log($"✅ Scene created: {scenePath}");
        Debug.Log($"📊 GameObjects: {FindObjectsOfType<Transform>().Length}");
        Debug.Log($"📋 Components ready for testing");
    }

    private static void SetupEnvironment(Scene scene)
    {
        Debug.Log("Setting up environment...");

        // Ground plane
        var groundGO = new GameObject("Ground");
        groundGO.tag = "Environment";
        var groundTransform = groundGO.transform;
        groundTransform.position = new Vector3(0, 0, 0);
        groundTransform.localScale = new Vector3(50, 1, 50);

        var groundRenderer = groundGO.AddComponent<MeshRenderer>();
        groundGO.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Plane.fbx");

        var groundCollider = groundGO.AddComponent<BoxCollider>();

        // Material: Gray
        var groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        groundRenderer.material = groundMat;

        EditorSceneManager.MoveGameObjectToScene(groundGO, scene);

        // Light
        var lightGO = new GameObject("DirectionalLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        EditorSceneManager.MoveGameObjectToScene(lightGO, scene);
    }

    private static void SetupXROrigin(Scene scene)
    {
        Debug.Log("Setting up XR Origin...");

        // Create XR Origin parent
        var xrOriginGO = new GameObject("XROrigin");
        xrOriginGO.transform.position = Vector3.zero;

        // Camera Offset
        var cameraOffsetGO = new GameObject("CameraOffset");
        cameraOffsetGO.transform.SetParent(xrOriginGO.transform);
        cameraOffsetGO.transform.localPosition = Vector3.zero;

        // Main Camera
        var cameraGO = new GameObject("MainCamera");
        cameraGO.transform.SetParent(cameraOffsetGO.transform);
        cameraGO.tag = "MainCamera";

        var camera = cameraGO.AddComponent<Camera>();
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 1000f;
        camera.fieldOfView = 90f;

        // Add audio listener for VR
        cameraGO.AddComponent<AudioListener>();

        // Left Controller
        var leftControllerGO = new GameObject("LeftController");
        leftControllerGO.transform.SetParent(xrOriginGO.transform);
        leftControllerGO.transform.localPosition = new Vector3(-0.3f, 0.2f, 0.2f);

        // Right Controller
        var rightControllerGO = new GameObject("RightController");
        rightControllerGO.transform.SetParent(xrOriginGO.transform);
        rightControllerGO.transform.localPosition = new Vector3(0.3f, 0.2f, 0.2f);

        EditorSceneManager.MoveGameObjectToScene(xrOriginGO, scene);
        EditorSceneManager.MoveGameObjectToScene(cameraOffsetGO, scene);
        EditorSceneManager.MoveGameObjectToScene(cameraGO, scene);
        EditorSceneManager.MoveGameObjectToScene(leftControllerGO, scene);
        EditorSceneManager.MoveGameObjectToScene(rightControllerGO, scene);
    }

    private static GameObject SetupVehicle(Scene scene)
    {
        Debug.Log("Setting up Vehicle...");

        var vehicleGO = new GameObject("Vehicle");
        vehicleGO.tag = "Vehicle";
        vehicleGO.transform.position = new Vector3(0, 1.7f, 0);

        // Rigidbody
        var rb = vehicleGO.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Collider
        var collider = vehicleGO.AddComponent<BoxCollider>();
        collider.size = new Vector3(1.856f, 1.535f, 4.255f);

        // Physics material
        var physicMat = new PhysicMaterial();
        physicMat.dynamicFriction = 0.5f;
        physicMat.staticFriction = 0.5f;
        collider.material = physicMat;

        // Visual representation (cube for now)
        var meshRenderer = vehicleGO.AddComponent<MeshRenderer>();
        vehicleGO.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var vehicleMat = new Material(Shader.Find("Standard"));
        vehicleMat.color = new Color(1f, 0.2f, 0.2f, 1f); // Red
        meshRenderer.material = vehicleMat;

        // Components
        vehicleGO.AddComponent<VehiclePhysics>();
        vehicleGO.AddComponent<InputMapper>();
        vehicleGO.AddComponent<ObstacleDetector>();
        vehicleGO.AddComponent<CollisionFeedback>();
        vehicleGO.AddComponent<XRDebugger>();

        EditorSceneManager.MoveGameObjectToScene(vehicleGO, scene);

        return vehicleGO;
    }

    private static void SetupObstacles(Scene scene)
    {
        Debug.Log("Setting up Obstacles...");

        // Obstacle material (red)
        var obstacleMat = new Material(Shader.Find("Standard"));
        obstacleMat.color = new Color(1f, 0.2f, 0.2f, 1f);

        // Left cone
        var coneLeftGO = new GameObject("Obstacle_Left");
        coneLeftGO.tag = "Obstacle";
        coneLeftGO.transform.position = new Vector3(-1.5f, 0.5f, 15f);
        coneLeftGO.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);

        var leftRenderer = coneLeftGO.AddComponent<MeshRenderer>();
        coneLeftGO.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        leftRenderer.material = obstacleMat;

        var leftCollider = coneLeftGO.AddComponent<BoxCollider>();
        leftCollider.isTrigger = false;

        EditorSceneManager.MoveGameObjectToScene(coneLeftGO, scene);

        // Right cone
        var coneRightGO = new GameObject("Obstacle_Right");
        coneRightGO.tag = "Obstacle";
        coneRightGO.transform.position = new Vector3(1.5f, 0.5f, 15f);
        coneRightGO.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);

        var rightRenderer = coneRightGO.AddComponent<MeshRenderer>();
        coneRightGO.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        rightRenderer.material = obstacleMat;

        var rightCollider = coneRightGO.AddComponent<BoxCollider>();
        rightCollider.isTrigger = false;

        EditorSceneManager.MoveGameObjectToScene(coneRightGO, scene);
    }

    private static void SetupUI(Scene scene)
    {
        Debug.Log("Setting up UI...");

        // Canvas (World Space for VR)
        var canvasGO = new GameObject("UICanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1024, 512);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Position 1.5m in front of camera
        var cameraTransform = GameObject.Find("MainCamera")?.transform;
        if (cameraTransform != null)
        {
            canvasGO.transform.position = cameraTransform.position +
                                         cameraTransform.forward * 1.5f;
        }

        // HUDManager
        canvasGO.AddComponent<HUDManager>();

        // Distance Text
        var distanceGO = new GameObject("DistanceText");
        distanceGO.transform.SetParent(canvasGO.transform);
        distanceGO.AddComponent<TextMeshProUGUI>();
        var distanceRect = distanceGO.GetComponent<RectTransform>();
        distanceRect.anchoredPosition = new Vector2(-400, 200);
        distanceRect.sizeDelta = new Vector2(400, 100);

        var distanceText = distanceGO.GetComponent<TextMeshProUGUI>();
        distanceText.text = "Distancia: --m";
        distanceText.fontSize = 36;
        distanceText.alignment = TextAlignmentOptions.BottomLeft;
        distanceText.color = Color.green;

        // Speed Text
        var speedGO = new GameObject("SpeedText");
        speedGO.transform.SetParent(canvasGO.transform);
        speedGO.AddComponent<TextMeshProUGUI>();
        var speedRect = speedGO.GetComponent<RectTransform>();
        speedRect.anchoredPosition = new Vector2(-400, 50);
        speedRect.sizeDelta = new Vector2(400, 100);

        var speedText = speedGO.GetComponent<TextMeshProUGUI>();
        speedText.text = "Velocidad: 0 km/h";
        speedText.fontSize = 36;
        speedText.alignment = TextAlignmentOptions.BottomLeft;
        speedText.color = Color.white;

        EditorSceneManager.MoveGameObjectToScene(canvasGO, scene);
        EditorSceneManager.MoveGameObjectToScene(distanceGO, scene);
        EditorSceneManager.MoveGameObjectToScene(speedGO, scene);
    }

    private static void SetupGameManager(Scene scene)
    {
        Debug.Log("Setting up GameManager...");

        var gmGO = new GameObject("GameManager");
        gmGO.tag = "GameController";
        gmGO.AddComponent<GameManager>();

        EditorSceneManager.MoveGameObjectToScene(gmGO, scene);
    }
}
