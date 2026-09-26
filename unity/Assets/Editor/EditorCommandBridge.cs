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

    /// <summary>
    /// Siempre guarda la escena abierta antes de operar, para que Unity no bloquee con
    /// el diálogo "Scene(s) Have Been Modified". Si la escena no tiene archivo (Untitled)
    /// o su archivo ya no existe, se reemplaza por una escena vacía.
    /// </summary>
    static void SaveOpenScenes()
    {
        if (EditorApplication.isPlaying) return;
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path) && File.Exists(scene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
        else if (scene.isDirty)
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        }
    }

    static void Run(string cmd)
    {
        if (cmd != "stop" && cmd != "autodrive" && cmd != "snap" && !cmd.StartsWith("wsl ") && !cmd.StartsWith("git ")) SaveOpenScenes();
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
            case "snap-mirrors": SnapMirrorCheck(); break;
            case "autodrive": AutoDrive(); break;
            default:
                if (cmd.StartsWith("wsl "))
                {
                    // Ejecuta en WSL (donde están las llaves SSH del usuario) dentro del repo
                    string repo = "/mnt/" + char.ToLower(Path.GetFullPath("..")[0]) + Path.GetFullPath("..").Substring(2).Replace('\\', '/');
                    RunProcess("wsl", $"-e bash -lc \"cd '{repo}' && {cmd.Substring(4)}\"");
                    break;
                }
                if (cmd.StartsWith("git "))
                {
                    RunGit(cmd.Substring(4));
                    break;
                }
                throw new ArgumentException("Comando desconocido");
        }
    }

    /// <summary>Renderiza la vista del conductor y una vista general a Temp/*.png</summary>
    static void Snap()
    {
        var main = Camera.main;
        if (main != null)
        {
            // Vista del conductor como con el visor puesto: al frente y girando la cabeza
            var t = main.transform;
            var original = t.localRotation;
            // Los espejos se actualizan en LateUpdate (solo en Play): forzarlos para la captura
            foreach (var m in UnityEngine.Object.FindObjectsByType<VRCar.Vehicle.PlanarMirror>()) m.Render();
            Render(main, "Temp/snap_driver.png");
            t.localRotation = original * Quaternion.Euler(8, -45, 0); Render(main, "Temp/snap_driver_left.png");
            t.localRotation = original * Quaternion.Euler(5, 40, 0); Render(main, "Temp/snap_driver_right.png");
            t.localRotation = original * Quaternion.Euler(-6, 22, 0); Render(main, "Temp/snap_driver_rearview.png");
            t.localRotation = original * Quaternion.Euler(35, 0, 0); Render(main, "Temp/snap_driver_down.png");
            t.localRotation = original;
        }

        var go = new GameObject("SnapCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(-10f, 16f, -18f);
        go.transform.LookAt(new Vector3(-1f, 0, -2f));
        cam.fieldOfView = 55f;
        Render(cam, "Temp/snap_overview.png");
        UnityEngine.Object.DestroyImmediate(go);
    }

    /// <summary>Pone marcadores de colores detrás del auto y captura, para verificar los espejos</summary>
    static void SnapMirrorCheck()
    {
        var car = GameObject.FindWithTag("Vehicle");
        if (car == null) return;
        var markers = new System.Collections.Generic.List<GameObject>();
        void Marker(Vector3 local, Color c)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m.transform.position = car.transform.TransformPoint(local);
            m.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
            var mat = new Material(Shader.Find("Unlit/Color")) { color = c };
            m.GetComponent<Renderer>().sharedMaterial = mat;
            markers.Add(m);
        }
        Marker(new Vector3(0, 0, -8), Color.red);          // detrás, al centro → retrovisor interior
        Marker(new Vector3(-2.8f, 0, -9), Color.green);    // atrás a la izquierda → espejo izquierdo
        Marker(new Vector3(2.8f, 0, -9), Color.yellow);    // atrás a la derecha → espejo derecho
        Snap();
        foreach (var m in markers) UnityEngine.Object.DestroyImmediate(m);
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

    /// <summary>Ejecuta git en la raíz del repositorio (carpeta padre del proyecto Unity)</summary>
    static void RunGit(string args) => RunProcess("git", args);

    static void RunProcess(string exe, string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(exe, args)
        {
            WorkingDirectory = Path.GetFullPath(".."),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var p = System.Diagnostics.Process.Start(psi))
        {
            string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(120000);
            Log($"{exe} {args} (exit {p.ExitCode}):\n{output}");
        }
    }

    static void Log(string msg)
    {
        File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {msg}\n");
        Debug.Log($"[EditorCommandBridge] {msg}");
    }
}
