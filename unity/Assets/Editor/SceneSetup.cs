using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRCar.Core;
using VRCar.Obstacles;
using VRCar.UI;
using VRCar.Vehicle;

/// <summary>
/// Crea la escena CU1_CornerJudgment lista para jugar:
/// - Piso, luz, conos (obstáculos) formando puertas cada vez más angostas
/// - Vehículo con física, input, detector de obstáculos y feedback de colisión
/// - XR Origin (XR Interaction Toolkit) sentado en el puesto del conductor
/// - HUD en el tablero (distancia al obstáculo y velocidad)
/// Controles sin visor: W/↑ acelera, S/↓/Espacio frena, A/D o ←/→ gira.
/// Con Quest: gatillo derecho acelera, gatillo izquierdo frena, joystick derecho gira.
/// Menú: VRCar → 2. Crear escena CU1
/// </summary>
public class SceneSetup
{
    public const string ScenePath = "Assets/Scenes/CU1_CornerJudgment.unity";
    const string MaterialsFolder = "Assets/Materials";
    const string InputActionsPath = "Assets/InputActions/VRCarInput.inputactions";

    // Dimensiones Peugeot 308 (m)
    static readonly Vector3 VehicleSize = new Vector3(1.856f, 1.535f, 4.255f);
    const float DriverEyeHeight = 1.2f;   // altura de ojos sobre el suelo, sentado

    [MenuItem("VRCar/2. Crear escena CU1", priority = 2)]
    public static void SetupCU1Scene() => CreateScene(interactive: true);

    public static void CreateScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        CreateGround();
        var vehicle = CreateVehicle();
        var xrCamera = CreateXROrigin(vehicle.transform);
        CreateHUD(vehicle.transform, xrCamera);
        CreateObstacles();
        new GameObject("GameManager").AddComponent<GameManager>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        Debug.Log($"✅ Escena creada: {ScenePath}. Presiona Play y maneja con W/A/S/D.");
    }

    // ------------------------------------------------------------------
    static Material GetMaterial(string name, Color color)
    {
        System.IO.Directory.CreateDirectory(MaterialsFolder);
        string path = $"{MaterialsFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static PhysicsMaterial GetPhysicsMaterial()
    {
        System.IO.Directory.CreateDirectory(MaterialsFolder);
        string path = $"{MaterialsFolder}/VehicleNoFriction.physicMaterial";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("VehicleNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f
            };
            AssetDatabase.CreateAsset(pm, path);
        }
        return pm;
    }

    static void CreateLight()
    {
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, 0, 60);
        ground.transform.localScale = new Vector3(6, 1, 16); // 60m x 160m
        ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ground", new Color(0.32f, 0.33f, 0.35f));
        ground.isStatic = true;

        // Líneas de carril (solo visuales)
        var lineMat = GetMaterial("LaneLine", Color.white);
        foreach (float x in new[] { -3f, 3f })
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = x < 0 ? "LaneLine_Left" : "LaneLine_Right";
            Object.DestroyImmediate(line.GetComponent<Collider>());
            line.transform.position = new Vector3(x, 0.005f, 60);
            line.transform.localScale = new Vector3(0.12f, 0.01f, 150);
            line.GetComponent<Renderer>().sharedMaterial = lineMat;
            line.isStatic = true;
        }
    }

    static GameObject CreateVehicle()
    {
        var vehicle = new GameObject("Vehicle");
        vehicle.tag = "Vehicle";
        vehicle.transform.position = new Vector3(0, VehicleSize.y / 2f + 0.01f, 0);

        var rb = vehicle.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var col = vehicle.AddComponent<BoxCollider>();
        col.size = VehicleSize;
        // El auto "rueda": sin fricción de caja contra el piso (el frenado/arrastre lo maneja VehiclePhysics)
        col.sharedMaterial = GetPhysicsMaterial();

        // Carrocería visual (baja, tipo descapotable para no tapar la vista del conductor)
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(vehicle.transform, false);
        body.transform.localPosition = new Vector3(0, -VehicleSize.y / 2f + 0.45f, 0);
        body.transform.localScale = new Vector3(VehicleSize.x, 0.9f, VehicleSize.z);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterial("Vehicle", new Color(0.15f, 0.35f, 0.85f));

        // Parabrisas (marco) como referencia visual del frente
        var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dash.name = "Dashboard";
        Object.DestroyImmediate(dash.GetComponent<Collider>());
        dash.transform.SetParent(vehicle.transform, false);
        dash.transform.localPosition = new Vector3(0, -VehicleSize.y / 2f + 0.82f, 1.1f);
        dash.transform.localScale = new Vector3(VehicleSize.x - 0.1f, 0.12f, 0.4f);
        dash.GetComponent<Renderer>().sharedMaterial = GetMaterial("Dashboard", new Color(0.1f, 0.1f, 0.12f));

        vehicle.AddComponent<VehiclePhysics>();
        var input = vehicle.AddComponent<InputMapper>();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        var so = new SerializedObject(input);
        so.FindProperty("inputActions").objectReferenceValue = actions;
        so.ApplyModifiedPropertiesWithoutUndo();

        vehicle.AddComponent<ObstacleDetector>();
        vehicle.AddComponent<AudioSource>().playOnAwake = false;
        vehicle.AddComponent<CollisionFeedback>();
        return vehicle;
    }

    /// <summary>
    /// XR Origin de XR Interaction Toolkit en el asiento del conductor.
    /// Sin visor, la cámara queda a la altura de ojos (CameraYOffset) y sirve para probar en el editor.
    /// </summary>
    static Camera CreateXROrigin(Transform vehicle)
    {
        var originGO = new GameObject("XR Origin (Driver)");
        originGO.transform.SetParent(vehicle, false);
        // Origen a nivel del suelo, en el puesto del conductor (izquierda, un poco atrás del centro)
        originGO.transform.localPosition = new Vector3(-0.4f, -VehicleSize.y / 2f, -0.2f);

        var offsetGO = new GameObject("Camera Offset");
        offsetGO.transform.SetParent(originGO.transform, false);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offsetGO.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 500f;
        camGO.AddComponent<AudioListener>();

        var tpd = camGO.AddComponent<TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

        var origin = originGO.AddComponent<XROrigin>();
        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offsetGO;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device; // sentado: se recentra en el asiento
        origin.CameraYOffset = DriverEyeHeight;
        offsetGO.transform.localPosition = new Vector3(0, DriverEyeHeight, 0);

        // XRI: habilita las acciones del auto (mandos, teclado, gamepad)
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        var iam = originGO.AddComponent<InputActionManager>();
        iam.actionAssets = new List<InputActionAsset> { actions };

        // XRI: manager de interacciones + mandos rastreados (se ven en VR)
        new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        CreateController(offsetGO.transform, "Left Controller", "LeftHand");
        CreateController(offsetGO.transform, "Right Controller", "RightHand");

        return cam;
    }

    static void CreateController(Transform parent, string name, string hand)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, $"<XRController>{{{hand}}}/devicePosition", expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, $"<XRController>{{{hand}}}/deviceRotation", expectedControlType: "Quaternion"));

        // Modelo simple del mando
        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "Model";
        Object.DestroyImmediate(model.GetComponent<Collider>());
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = new Vector3(0.05f, 0.05f, 0.12f);
        model.GetComponent<Renderer>().sharedMaterial = GetMaterial("Controller", new Color(0.9f, 0.9f, 0.9f));
        // Fuera de VR quedan escondidos bajo el tablero
        go.transform.localPosition = new Vector3(hand == "LeftHand" ? -0.2f : 0.2f, -0.5f, 0.3f);
    }

    static void CreateHUD(Transform vehicle, Camera cam)
    {
        var canvasGO = new GameObject("HUD", typeof(RectTransform));
        canvasGO.transform.SetParent(vehicle, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        var rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 220);
        rect.localScale = Vector3.one * 0.001f; // 0.6m x 0.22m
        // Sobre el tablero, frente al conductor, algo bajo la línea de visión
        rect.localPosition = new Vector3(-0.4f, -VehicleSize.y / 2f + DriverEyeHeight - 0.2f, 1.1f);
        rect.localRotation = Quaternion.Euler(10f, 0, 0);

        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0, 0, 0, 0.55f);
        Stretch(bg.GetComponent<RectTransform>());

        CreateText(canvasGO.transform, "DistanceText", "Distancia: --", new Vector2(0, 50), Color.green);
        CreateText(canvasGO.transform, "SpeedText", "Velocidad: 0 km/h", new Vector2(0, -50), Color.white);

        canvasGO.AddComponent<HUDManager>();
    }

    static void CreateText(Transform parent, string name, string text, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 56;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(580, 90);
        r.anchoredPosition = pos;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Circuito de "juicio de esquinas": puertas de conos cada vez más angostas y un muro al final.
    /// </summary>
    static void CreateObstacles()
    {
        var parent = new GameObject("Obstacles").transform;
        var coneMat = GetMaterial("Cone", new Color(1f, 0.45f, 0.05f));

        // (distancia z, ancho de la puerta en m)
        var gates = new[] { (15f, 3.2f), (30f, 2.8f), (45f, 2.4f), (60f, 2.15f) };
        foreach (var (z, width) in gates)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cone.name = $"Cone_z{z}_{(side < 0 ? "L" : "R")}";
                cone.tag = "Obstacle";
                cone.transform.SetParent(parent, false);
                cone.transform.position = new Vector3(side * width / 2f, 0.5f, z);
                cone.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f); // 1m de alto
                cone.GetComponent<Renderer>().sharedMaterial = coneMat;
            }
        }

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall_End";
        wall.tag = "Obstacle";
        wall.transform.SetParent(parent, false);
        wall.transform.position = new Vector3(0, 0.75f, 80f);
        wall.transform.localScale = new Vector3(8f, 1.5f, 0.5f);
        wall.GetComponent<Renderer>().sharedMaterial = GetMaterial("Wall", new Color(0.8f, 0.1f, 0.1f));
    }

    static void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
