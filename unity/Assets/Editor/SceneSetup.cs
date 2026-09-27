using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRCar.Core;
using VRCar.Obstacles;
using VRCar.UI;
using VRCar.Vehicle;

/// <summary>
/// Crea la escena de práctica: ESTACIONAMIENTO PERPENDICULAR (en batería).
/// - Fila de espacios de 2.5 x 5.0 m con autos estacionados y un espacio libre (líneas amarillas)
/// - Pasillo de 7 m y una segunda fila enfrente
/// - Auto a escala 1:1 (Peugeot 308) con el conductor fijo en el asiento: solo gira la cabeza
/// - Espejos retrovisor interior y laterales, de tamaño real y con reflejo correcto
/// - HUD con distancia, velocidad y evaluación del estacionamiento
/// Controles: gatillo der. acelera · gatillo izq. frena (mantenido detenido = reversa) · joystick der. gira.
/// Sin visor: W/↑ acelera · S/↓/Espacio frena/reversa · A/D gira.
/// Menú: VRCar → 2. Crear escena de estacionamiento
/// </summary>
public class SceneSetup
{
    public const string ScenePath = "Assets/Scenes/Parking_Perpendicular.unity";
    const string MaterialsFolder = "Assets/Materials";
    const string InputActionsPath = "Assets/InputActions/VRCarInput.inputactions";

    // Peugeot 308 (m)
    static readonly Vector3 VehicleSize = new Vector3(1.856f, 1.535f, 4.255f);
    // Posición de los ojos del conductor (relativa al centro del auto, altura sobre el suelo).
    // Si hay modelo 3D se recalcula a partir de su volante.
    static float DriverEyeHeight = 1.2f;
    static Vector3 DriverSeat = new Vector3(-0.38f, 0f, -0.25f);
    const string CarModelPath = "Assets/Models/CarConcept/CarConcept.glb";
    const string UseCarModelPref = "VRCar.UseCarModel";

    /// <summary>
    /// Usar el modelo 3D del auto (CarConcept) en vez de la carrocería de cajas.
    /// Desactivado por defecto: la cabina del concept (asiento adelantado, pilares gruesos, espejos
    /// casi al lado del conductor) no permite ver los espejos como en un Peugeot 308.
    /// </summary>
    [MenuItem("VRCar/Usar modelo 3D del auto (experimental)", priority = 20)]
    static void ToggleCarModel() => EditorPrefs.SetBool(UseCarModelPref, !EditorPrefs.GetBool(UseCarModelPref, false));

    [MenuItem("VRCar/Usar modelo 3D del auto (experimental)", true)]
    static bool ToggleCarModelValidate()
    {
        Menu.SetChecked("VRCar/Usar modelo 3D del auto (experimental)", EditorPrefs.GetBool(UseCarModelPref, false));
        return true;
    }
    // Espejos laterales del modelo (posición local en el auto), si existen
    static Vector3? ModelMirrorLeft, ModelMirrorRight;

    // Estacionamiento (m)
    const float SpotWidth = 2.5f, SpotLength = 5.0f, AisleWidth = 7.0f;
    const int SpotsPerRow = 7, TargetSpot = 3;

    [MenuItem("VRCar/2. Crear escena de estacionamiento", priority = 2)]
    public static void SetupCU1Scene() => CreateScene(interactive: true);

    public static void CreateScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        CreateGround();
        var spot = CreateParkingLot();
        var vehicle = CreateVehicle();
        var xrCamera = CreateXROrigin(vehicle.transform);
        CreateMirrors(vehicle.transform, xrCamera.transform);
        var hud = CreateHUD(vehicle.transform, xrCamera);
        new GameObject("GameManager").AddComponent<GameManager>();

        var judgment = new GameObject("ParkingJudgment").AddComponent<ParkingJudgment>();
        judgment.Configure(spot, SpotWidth, SpotLength, vehicle.GetComponent<BoxCollider>(), hud);
        EditorUtility.SetDirty(judgment);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScene(ScenePath);
        Debug.Log($"✅ Escena creada: {ScenePath}");
    }

    // ------------------------------------------------------------------
    static Material GetMaterial(string name, Color color, float smoothness = 0.2f)
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
        mat.SetFloat("_Glossiness", smoothness);
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
                dynamicFriction = 0f, staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f
            };
            AssetDatabase.CreateAsset(pm, path);
        }
        return pm;
    }

    static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
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
        ground.transform.localScale = new Vector3(8, 1, 6); // 80 x 60 m
        ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Asphalt", new Color(0.24f, 0.25f, 0.27f), 0.1f);
        ground.isStatic = true;
    }

    /// <summary>
    /// Dos filas de espacios perpendiculares separadas por un pasillo.
    /// Fila A: z ∈ [0, 5], tope en z = 5.1. Fila B enfrente, al otro lado del pasillo.
    /// Devuelve el Transform del espacio objetivo (centro, forward hacia el tope).
    /// </summary>
    static Transform CreateParkingLot()
    {
        var root = new GameObject("ParkingLot").transform;
        var white = GetMaterial("LineWhite", new Color(0.92f, 0.92f, 0.9f));
        var yellow = GetMaterial("LineYellow", new Color(1f, 0.8f, 0.1f));
        var curbMat = GetMaterial("Concrete", new Color(0.62f, 0.62f, 0.6f));
        var carColors = new[] { new Color(0.75f, 0.1f, 0.1f), new Color(0.85f, 0.85f, 0.88f), new Color(0.1f, 0.1f, 0.12f),
                                new Color(0.2f, 0.35f, 0.55f), new Color(0.5f, 0.5f, 0.52f), new Color(0.9f, 0.9f, 0.9f), new Color(0.25f, 0.3f, 0.2f) };
        Transform target = null;
        float x0 = -(SpotsPerRow * SpotWidth) / 2f;

        foreach (int row in new[] { 0, 1 })
        {
            // Fila 0: espacios hacia +Z (z 0..5). Fila 1: enfrente, hacia -Z (z -AisleWidth-5 .. -AisleWidth)
            float dir = row == 0 ? 1f : -1f;
            float zStart = row == 0 ? 0f : -AisleWidth;
            float zCenter = zStart + dir * SpotLength / 2f;
            float zEnd = zStart + dir * SpotLength;

            for (int i = 0; i <= SpotsPerRow; i++)
            {
                float x = x0 + i * SpotWidth;
                bool targetEdge = row == 0 && (i == TargetSpot || i == TargetSpot + 1);
                var line = Box($"Line_R{row}_{i}", root, new Vector3(x, 0.003f, zCenter), new Vector3(0.1f, 0.005f, SpotLength), targetEdge ? yellow : white, false);
                line.isStatic = true;
            }
            // Tope (solera) al fondo de la fila
            var curb = Box($"Curb_R{row}", root, new Vector3(0, 0.075f, zEnd + dir * 0.15f), new Vector3(SpotsPerRow * SpotWidth, 0.15f, 0.3f), curbMat, true);
            curb.tag = "Obstacle";
            curb.isStatic = true;

            for (int i = 0; i < SpotsPerRow; i++)
            {
                float cx = x0 + (i + 0.5f) * SpotWidth;
                if (row == 0 && i == TargetSpot)
                {
                    var spot = new GameObject("TargetSpot").transform;
                    spot.SetParent(root, false);
                    spot.localPosition = new Vector3(cx, 0, zCenter);
                    spot.localRotation = Quaternion.identity; // forward +Z = hacia el tope
                    // línea de fondo amarilla del espacio objetivo
                    Box("Line_Target_End", root, new Vector3(cx, 0.003f, zEnd - 0.05f), new Vector3(SpotWidth, 0.005f, 0.1f), yellow, false);
                    target = spot;
                    continue;
                }
                // Auto estacionado, un poco desplazado al azar (determinista) como en la vida real
                float jitterX = Mathf.Sin(i * 12.9898f + row * 78.233f) * 0.12f;
                float jitterZ = Mathf.Cos(i * 4.1414f + row * 3.3f) * 0.15f;
                var carMat = GetMaterial($"ParkedCar_{(i + row * 3) % carColors.Length}", carColors[(i + row * 3) % carColors.Length], 0.6f);
                CreateParkedCar(root, new Vector3(cx + jitterX, 0, zCenter + dir * (0.1f + jitterZ)), row == 0 ? 0f : 180f, carMat);
            }
        }
        return target;
    }

    /// <summary>Auto estacionado simple: carrocería + cabina, con collider (obstáculo)</summary>
    static void CreateParkedCar(Transform parent, Vector3 pos, float yaw, Material paint)
    {
        var glass = GetMaterial("CarGlass", new Color(0.12f, 0.15f, 0.2f), 0.9f);
        var car = new GameObject("ParkedCar");
        car.tag = "Obstacle";
        car.transform.SetParent(parent, false);
        car.transform.localPosition = pos;
        car.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var col = car.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.72f, 0);
        col.size = new Vector3(1.8f, 1.44f, 4.3f);
        if (!SedanModel.BuildParked(car.transform, new Vector3(1.8f, 1.44f, 4.3f), paint, glass))
        {
            Box("Body", car.transform, new Vector3(0, 0.55f, 0), new Vector3(1.8f, 0.7f, 4.3f), paint, false);
            Box("Cabin", car.transform, new Vector3(0, 1.15f, -0.2f), new Vector3(1.6f, 0.55f, 2.3f), glass, false);
        }
        car.isStatic = true;
    }

    static GameObject CreateVehicle()
    {
        var vehicle = new GameObject("Vehicle");
        vehicle.tag = "Vehicle";
        // Empieza en el pasillo, mirando a lo largo de él (+X), antes del espacio libre
        float targetX = -(SpotsPerRow * SpotWidth) / 2f + (TargetSpot + 0.5f) * SpotWidth;
        vehicle.transform.position = new Vector3(targetX - 5.5f, VehicleSize.y / 2f + 0.01f, -AisleWidth / 2f - 0.6f);
        vehicle.transform.rotation = Quaternion.Euler(0, 90f, 0);

        var rb = vehicle.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var col = vehicle.AddComponent<BoxCollider>();
        col.size = VehicleSize;
        col.sharedMaterial = GetPhysicsMaterial();

        ModelMirrorLeft = ModelMirrorRight = null;
        bool built = EditorPrefs.GetBool(UseCarModelPref, false) && CreateVehicleModel(vehicle);
        if (!built && SedanModel.Build(vehicle, VehicleSize, out var sedan))
        {
            DriverSeat = sedan.driverSeat;
            DriverEyeHeight = sedan.eyeHeight;
            built = true;
        }
        if (!built)
        {
            // Sin modelo: carrocería de cajas, baja para no tapar la vista
            DriverEyeHeight = 1.2f;
            DriverSeat = new Vector3(-0.38f, 0f, -0.25f);
            var paint = GetMaterial("Vehicle", new Color(0.15f, 0.35f, 0.85f), 0.6f);
            float floorY = -VehicleSize.y / 2f;
            Box("Body", vehicle.transform, new Vector3(0, floorY + 0.45f, 0), new Vector3(VehicleSize.x, 0.9f, VehicleSize.z), paint, false);
            Box("Dashboard", vehicle.transform, new Vector3(0, floorY + 0.82f, 0.55f), new Vector3(VehicleSize.x - 0.1f, 0.12f, 0.4f),
                GetMaterial("Dashboard", new Color(0.1f, 0.1f, 0.12f)), false);
        }

        vehicle.AddComponent<VehiclePhysics>();
        var input = vehicle.AddComponent<InputMapper>();
        var so = new SerializedObject(input);
        so.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        vehicle.AddComponent<ObstacleDetector>();
        vehicle.AddComponent<AudioSource>().playOnAwake = false;
        vehicle.AddComponent<CollisionFeedback>();
        return vehicle;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }

    static Bounds RendererBounds(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>
    /// Modelo 3D del auto (CarConcept, Khronos glTF Sample Assets, CC BY 4.0) escalado a las
    /// medidas exteriores del Peugeot 308 (ancho x largo; el alto conserva proporción).
    /// - El modelo viene con Z hacia arriba y el frente hacia -Y: se rota -90° en X
    /// - El concept trae el volante al centro: se mueve al puesto izquierdo
    /// - Las ruedas delanteras vienen giradas: se enderezan (luego las anima VehicleVisuals)
    /// </summary>
    static bool CreateVehicleModel(GameObject vehicle)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarModelPath);
        if (prefab == null) return false;

        var root = new GameObject("Visual");
        root.transform.SetParent(vehicle.transform, false);
        root.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var model = (GameObject)Object.Instantiate(prefab, root.transform);
        model.name = "CarModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        const float modelBodyWidth = 2.22f, modelLength = 4.36f;
        float sLen = VehicleSize.z / modelLength;
        model.transform.localScale = new Vector3(VehicleSize.x / modelBodyWidth, sLen, sLen);
        foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        var m = model.transform;

        // Puesto de conducción a la izquierda
        foreach (var n in new[] { "InteriorSteeringBase", "InteriorSteeringCylinder", "InteriorSteeringDash", "InteriorSteeringDashColumn",
                                  "InteriorSteeringHandleL", "InteriorSteeringHandleR", "InteriorPedalAccel", "InteriorPedalAccelArm",
                                  "InteriorPedalBrake", "InteriorPedalBrakeArm" })
        {
            var t = FindDeep(m, n);
            if (t != null) t.localPosition += new Vector3(-0.40f, 0f, 0f);
        }

        // Ruedas en reposo (rectas)
        var fl = FindDeep(m, "WheelFrontL"); var fr = FindDeep(m, "WheelFrontR");
        var rl = FindDeep(m, "WheelRearL"); var rr = FindDeep(m, "WheelRearR");
        if (fl) fl.localRotation = Quaternion.Euler(0f, 180f, 180f);
        if (rl) rl.localRotation = Quaternion.Euler(0f, 180f, 180f);
        if (fr) fr.localRotation = Quaternion.identity;
        if (rr) rr.localRotation = Quaternion.identity;

        // Alinear: centrado en el auto y apoyado en el suelo (base del collider)
        Bounds b = RendererBounds(m);
        Vector3 c0 = vehicle.transform.InverseTransformPoint(b.center);
        float bottom = b.min.y - vehicle.transform.position.y;
        root.transform.localPosition = new Vector3(-c0.x, -VehicleSize.y / 2f - bottom, -c0.z);

        // Volante: pivote en su centro, alineado con la columna, para poder girarlo
        Transform steeringPivot = null;
        var cyl = FindDeep(m, "InteriorSteeringCylinder");
        var wheel01 = FindDeep(m, "InteriorSteeringWheel01");
        var baseT = FindDeep(m, "InteriorSteeringBase");
        if (cyl != null && wheel01 != null && baseT != null)
        {
            Vector3 wc = RendererBounds(wheel01).center;
            Vector3 axis = (wc - RendererBounds(baseT).center).normalized;
            steeringPivot = new GameObject("SteeringWheelPivot").transform;
            steeringPivot.SetParent(cyl, true);
            steeringPivot.position = wc;
            steeringPivot.rotation = Quaternion.LookRotation(axis, vehicle.transform.up);
            foreach (var n in new[] { "InteriorSteeringWheel01", "InteriorSteeringWheel02", "InteriorSteeringWheel03", "InteriorSteeringWheel04", "InteriorSteeringEmblem" })
            {
                var t = FindDeep(m, n);
                if (t != null) t.SetParent(steeringPivot, true);
            }

            // Ojos del conductor: ~60 cm detrás y ~32 cm sobre el centro del volante (bajo el techo)
            Vector3 wl = vehicle.transform.InverseTransformPoint(wc);
            float roof = RendererBounds(m).max.y - vehicle.transform.position.y;
            float eyeY = Mathf.Min(wl.y + 0.32f, roof - 0.14f);
            DriverSeat = new Vector3(wl.x, 0f, wl.z - 0.60f);
            DriverEyeHeight = eyeY + VehicleSize.y / 2f;
        }

        // Espejos laterales: el concept los tiene casi al lado del conductor (cabina muy adelantada).
        // Para que la geometría de visión sea la de un auto normal se ocultan y se usan espejos
        // propios a ~60 cm delante de los ojos (ver CreateMirrors).
        foreach (var n in new[] { "BodyDoorLMirror", "BodyDoorLMirrorColor1", "BodyDoorLMirrorColor2",
                                  "BodyDoorRMirror", "BodyDoorRMirrorColor1", "BodyDoorRMirrorColor2" })
        {
            var t = FindDeep(m, n);
            if (t != null) foreach (var r in t.GetComponents<Renderer>()) r.enabled = false;
        }

        var visuals = vehicle.AddComponent<VehicleVisuals>();
        visuals.frontLeft = fl; visuals.frontRight = fr; visuals.rearLeft = rl; visuals.rearRight = rr;
        visuals.steeringWheel = steeringPivot;
        visuals.wheelRadius = 0.49f * sLen;
        return true;
    }

    /// <summary>
    /// XR Origin (XR Interaction Toolkit) fijo en el asiento del conductor.
    /// Solo se sigue la rotación de la cabeza: el conductor mira libremente pero no se desplaza (anti-mareo).
    /// </summary>
    static Camera CreateXROrigin(Transform vehicle)
    {
        var originGO = new GameObject("XR Origin (Driver)");
        originGO.transform.SetParent(vehicle, false);
        originGO.transform.localPosition = new Vector3(DriverSeat.x, -VehicleSize.y / 2f, DriverSeat.z);

        var offsetGO = new GameObject("Camera Offset");
        offsetGO.transform.SetParent(originGO.transform, false);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offsetGO.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 500f;
        camGO.AddComponent<AudioListener>();

        var tpd = camGO.AddComponent<TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;

        var origin = originGO.AddComponent<XROrigin>();
        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offsetGO;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        origin.CameraYOffset = DriverEyeHeight;
        offsetGO.transform.localPosition = new Vector3(0, DriverEyeHeight, 0);

        var iam = originGO.AddComponent<InputActionManager>();
        iam.actionAssets = new List<InputActionAsset> { AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath) };
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
        var model = Box("Model", go.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.12f), GetMaterial("Controller", new Color(0.9f, 0.9f, 0.9f)), false);
        // Sin visor los mandos no se mueven: se dejan bajo el piso para que no estorben la vista
        go.transform.localPosition = new Vector3(hand == "LeftHand" ? -0.2f : 0.2f, -1.5f, 0.3f);
    }

    /// <summary>
    /// Espejos de tamaño real, ajustados como lo haría un conductor:
    /// - Retrovisor interior 25 x 7 cm, centrado, mirando la luneta trasera
    /// - Laterales 19 x 12 cm, mostrando apenas el costado del propio auto
    /// </summary>
    static void CreateMirrors(Transform vehicle, Transform eye)
    {
        float floorY = -VehicleSize.y / 2f;
        Vector3 eyeLocal = new Vector3(DriverSeat.x, floorY + DriverEyeHeight, DriverSeat.z);
        var housing = GetMaterial("MirrorHousing", new Color(0.08f, 0.08f, 0.09f));

        // (nombre, posición local, tamaño del vidrio, dirección que debe mostrar)
        var mirrors = new (string, Vector3, Vector2, Vector3)[]
        {
            ("Mirror_Interior", new Vector3(0.0f, eyeLocal.y + 0.08f, eyeLocal.z + 0.50f), new Vector2(0.25f, 0.07f), new Vector3(0f, -0.10f, -1f)),
            ("Mirror_Left",     ModelMirrorLeft ?? new Vector3(-VehicleSize.x / 2f - 0.10f, eyeLocal.y + 0.02f, eyeLocal.z + 0.30f), new Vector2(0.19f, 0.12f), new Vector3(-0.20f, -0.04f, -1f)),
            ("Mirror_Right",    ModelMirrorRight ?? new Vector3(VehicleSize.x / 2f + 0.10f, eyeLocal.y + 0.02f, eyeLocal.z + 0.30f), new Vector2(0.19f, 0.12f), new Vector3(0.28f, -0.06f, -1f)),
        };

        foreach (var (name, pos, size, view) in mirrors)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(vehicle, false);
            go.transform.localPosition = pos;
            // Normal del vidrio = bisectriz entre la dirección al ojo y la dirección que se quiere ver
            Vector3 toEye = (eyeLocal - pos).normalized;
            Vector3 normal = (toEye + view.normalized).normalized;
            // La cara visible del Quad mira hacia -forward → forward = -normal
            go.transform.localRotation = Quaternion.LookRotation(-normal, Vector3.up);
            // Un poco hacia el conductor para que la carcasa del modelo no tape el vidrio
            go.transform.localPosition = pos + normal * 0.025f;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var mirror = go.AddComponent<PlanarMirror>();
            mirror.eye = eye;
            mirror.eyeCamera = eye.GetComponent<Camera>();
            mirror.mirrorShader = Shader.Find("VRCar/StereoMirror");

            // Carcasa detrás del vidrio
            var back = Box(name + "_Housing", vehicle, pos + normal * -0.03f, new Vector3(size.x + 0.03f, size.y + 0.03f, 0.05f), housing, false);
            back.transform.localRotation = go.transform.localRotation;
        }
    }

    static HUDManager CreateHUD(Transform vehicle, Camera cam)
    {
        var canvasGO = new GameObject("HUD", typeof(RectTransform));
        canvasGO.transform.SetParent(vehicle, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        var rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 300);
        rect.localScale = Vector3.one * 0.0005f; // 35 x 15 cm, flotando sobre el tablero
        rect.localPosition = new Vector3(DriverSeat.x + 0.30f, -VehicleSize.y / 2f + DriverEyeHeight - 0.13f, DriverSeat.z + 0.70f);
        rect.localRotation = Quaternion.Euler(12f, 20f, 0);

        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(canvasGO.transform, false);
        bg.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.55f);
        Stretch(bg.GetComponent<RectTransform>());

        CreateText(canvasGO.transform, "MessageText", "", new Vector2(0, 85), new Color(1f, 0.85f, 0.3f), 34, new Vector2(680, 120));
        CreateText(canvasGO.transform, "DistanceText", "Distancia: --", new Vector2(0, -20), Color.green);
        CreateText(canvasGO.transform, "SpeedText", "Velocidad: 0 km/h", new Vector2(0, -100), Color.white);
        return canvasGO.AddComponent<HUDManager>();
    }

    static void CreateText(Transform parent, string name, string text, Vector2 pos, Color color, float fontSize = 56, Vector2? size = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = size ?? new Vector2(660, 80);
        r.anchoredPosition = pos;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    static void SetBuildScene(string path)
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
    }
}
