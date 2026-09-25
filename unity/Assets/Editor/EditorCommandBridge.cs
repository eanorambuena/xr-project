using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Puente de automatización: ejecuta comandos escritos en Temp/vrcar_cmd.txt
/// (una línea por comando). Útil para automatizar el editor desde scripts externos.
/// Comandos: refresh, xr, scene, tests, play, stop, open-scene
/// El resultado se agrega a Temp/vrcar_cmd.log
/// </summary>
[InitializeOnLoad]
public static class EditorCommandBridge
{
    const string CmdPath = "Temp/vrcar_cmd.txt";
    const string LogPath = "Temp/vrcar_cmd.log";
    static double nextCheck;

    static EditorCommandBridge()
    {
        EditorApplication.update += Poll;
        Application.logMessageReceived += (msg, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} UNITY-{type}: {msg}\n"); } catch { }
        };
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(CmdPath)) return;

        string[] lines;
        try { lines = File.ReadAllLines(CmdPath); File.Delete(CmdPath); }
        catch { return; }

        foreach (var raw in lines)
        {
            var cmd = raw.Trim();
            if (cmd.Length == 0) continue;
            try
            {
                Run(cmd);
                Log($"OK {cmd}");
            }
            catch (Exception e)
            {
                Log($"ERROR {cmd}: {e}");
            }
        }
    }

    static void Run(string cmd)
    {
        switch (cmd)
        {
            case "refresh": AssetDatabase.Refresh(); break;
            case "xr": XRProjectSetup.ConfigureXR(); break;
            case "scene": SceneSetup.CreateScene(interactive: false); break;
            case "open-scene": UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneSetup.ScenePath); break;
            case "tests": EditorApplication.ExecuteMenuItem("VRCar/Run EditMode Tests"); break;
            case "play": EditorApplication.isPlaying = true; break;
            case "stop": EditorApplication.isPlaying = false; break;
            case "snap": Snap(); break;
            case "autodrive": AutoDrive(); break;
            default: throw new ArgumentException("Comando desconocido");
        }
    }

    /// <summary>Renderiza la vista del conductor y una vista general a Temp/*.png</summary>
    static void Snap()
    {
        var main = Camera.main;
        if (main != null) Render(main, "Temp/snap_driver.png");

        var go = new GameObject("SnapCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(12f, 14f, -12f);
        go.transform.LookAt(new Vector3(0, 0, 25f));
        cam.fieldOfView = 55f;
        Render(cam, "Temp/snap_overview.png");
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void Render(Camera cam, string path)
    {
        var rt = new RenderTexture(1280, 720, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = prev;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
    }

    /// <summary>Solo en Play: desactiva el input y acelera recto para probar física/HUD/colisión</summary>
    static void AutoDrive()
    {
        var physics = UnityEngine.Object.FindFirstObjectByType<VRCar.Vehicle.VehiclePhysics>();
        var mapper = physics.GetComponent<VRCar.Vehicle.InputMapper>();
        if (mapper != null) mapper.enabled = false;
        physics.accelerationInput = 0.6f;
        Log($"autodrive: speed={physics.GetCurrentSpeedKmh():F1} km/h pos={physics.transform.position}");
    }

    static void Log(string msg)
    {
        File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {msg}\n");
        Debug.Log($"[EditorCommandBridge] {msg}");
    }
}
