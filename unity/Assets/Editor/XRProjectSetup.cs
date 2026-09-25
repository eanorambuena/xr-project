using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

/// <summary>
/// Configura el proyecto para VR con OpenXR:
/// - PC (Standalone): OpenXR + perfil Oculus Touch (Quest Link / Air Link)
/// - Android: OpenXR + Meta Quest Support + perfiles Touch / Touch Plus (Quest 3)
/// Menú: VRCar → 1. Configurar XR (OpenXR + Quest)
/// </summary>
public static class XRProjectSetup
{
    const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [MenuItem("VRCar/1. Configurar XR (OpenXR + Quest)", priority = 1)]
    public static void ConfigureXR()
    {
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            System.IO.Directory.CreateDirectory("Assets/XR");
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }

        foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            var general = perTarget.SettingsForBuildTarget(group);
            general.InitManagerOnStart = true;
            var manager = perTarget.ManagerSettingsForBuildTarget(group);
            bool assigned = XRPackageMetadataStore.AssignLoader(manager, OpenXRLoader, group);
            Debug.Log($"[XRProjectSetup] {group}: OpenXR loader asignado = {assigned}");

            var openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (openxr == null)
            {
                Debug.LogWarning($"[XRProjectSetup] No se encontraron OpenXRSettings para {group}");
                continue;
            }
            foreach (var feature in openxr.GetFeatures())
            {
                bool want = feature is OculusTouchControllerProfile
                            || feature is MetaQuestTouchPlusControllerProfile
                            || (group == BuildTargetGroup.Android && feature is MetaQuestFeature);
                if (want && !feature.enabled)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                    Debug.Log($"[XRProjectSetup] {group}: feature habilitada {feature.GetType().Name}");
                }
            }
            EditorUtility.SetDirty(openxr);
        }

        // Ajustes Android recomendados para Meta Quest 3
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = true; // VR por Link y pruebas con el editor sin foco

        EditorUtility.SetDirty(perTarget);
        AssetDatabase.SaveAssets();
        Debug.Log("[XRProjectSetup] ✅ XR configurado (OpenXR para PC y Android/Quest)");
    }
}
