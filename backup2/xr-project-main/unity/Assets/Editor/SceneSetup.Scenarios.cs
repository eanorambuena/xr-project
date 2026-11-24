using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using VRCar.Core;

/// <summary>
/// Menú de escenarios y registro de escenas en el build.
/// Menús: VRCar → 3. Crear menú de escenarios · 6. Crear TODAS las escenas
/// </summary>
public partial class SceneSetup
{
    public const string MenuScenePath = "Assets/Scenes/ScenarioMenu.unity";
    const float MenuEyeHeight = 1.2f;   // altura de ojos del conductor sentado (igual que en el auto)

    /// <summary>Rutas de las escenas del proyecto que existen, en el orden del build (el menú primero)</summary>
    public static string[] ScenePathsInOrder()
    {
        return new[] { MenuScenePath, ScenePath, ParallelScenePath, SlopeScenePath }
            .Where(p => System.IO.File.Exists(p)).ToArray();
    }

    /// <summary>Pone en el build todas las escenas que existen</summary>
    public static void RegisterScenesInBuild()
    {
        EditorBuildSettings.scenes = ScenePathsInOrder()
            .Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        AssetDatabase.SaveAssets(); // escribe EditorBuildSettings.asset ya, no al cerrar Unity
    }

    /// <summary>Flecha flotante sobre el espacio objetivo (offset = centro del espacio en coordenadas locales de spot)</summary>
    static void AddTargetMarker(Transform spot, Vector3 localOffset)
    {
        var color = new Color(1f, 0.78f, 0.15f);
        var mat = GetMaterial("TargetArrow", color, 0.3f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 0.9f);
        EditorUtility.SetDirty(mat);
        var go = new GameObject("TargetMarker");
        go.AddComponent<TargetMarker>().Configure(spot, localOffset, mat);
    }

    [MenuItem("VRCar/3. Crear menú de escenarios", priority = 3)]
    public static void SetupMenuScene() => CreateMenuScene(interactive: true);

    [MenuItem("VRCar/6. Crear TODAS las escenas", priority = 6)]
    public static void SetupAllScenes() => CreateAllScenes(interactive: true);

    /// <summary>Crea el estacionamiento en batería, el de paralelo, el de pendiente y el menú (queda abierto el menú)</summary>
    public static void CreateAllScenes(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateScene(interactive: false);
        CreateParallelScene(interactive: false);
        CreateSlopeScene(interactive: false);
        CreateMenuScene(interactive: false);
    }

    public static void CreateMenuScene(bool interactive)
    {
        if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLight();
        // Piso oscuro: da referencia bajo los pies para que no se sienta flotar en el vacío
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "MenuFloor";
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        floor.GetComponent<Renderer>().sharedMaterial = GetMaterial("MenuFloor", new Color(0.10f, 0.12f, 0.16f), 0.3f);
        floor.isStatic = true;

        CreateMenuOrigin();

        var menu = new GameObject("ScenarioMenu");
        menu.transform.position = new Vector3(0f, MenuEyeHeight, 2.4f);   // frente al conductor, a la altura de sus ojos
        menu.AddComponent<ScenarioMenu>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        RegisterScenesInBuild();
        Debug.Log($"✅ Escena creada: {MenuScenePath}");
    }

    /// <summary>XR Origin mínimo (cámara con seguimiento de cabeza) para el menú: sin auto ni mandos dibujados</summary>
    static void CreateMenuOrigin()
    {
        var originGO = new GameObject("XR Origin (Menu)");
        var offsetGO = new GameObject("Camera Offset");
        offsetGO.transform.SetParent(originGO.transform, false);
        offsetGO.transform.localPosition = new Vector3(0, MenuEyeHeight, 0);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offsetGO.transform, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 100f;
        camGO.AddComponent<AudioListener>();

        var tpd = camGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
        tpd.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;

        var origin = originGO.AddComponent<XROrigin>();
        origin.Camera = cam;
        origin.CameraFloorOffsetObject = offsetGO;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        origin.CameraYOffset = MenuEyeHeight;
    }
}
