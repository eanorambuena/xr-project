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
        if (cmd != "stop" && cmd != "autodrive" && cmd != "autopark" && cmd != "snap" && !cmd.StartsWith("wsl ") && !cmd.StartsWith("git ")) SaveOpenScenes();
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
            case "dump-mirrors": DumpMirrors(); break;
            case "autodrive": AutoDrive(); break;
            case "autopark": AutoParkTest.Start(); break;
            case "autopark-video": AutoParkTest.Start(record: true); break;
            default:
                if (cmd.StartsWith("snap-eye "))
                {
                    // Captura con la cabeza desplazada en altura (estaturas distintas): snap-eye -0.10
                    float dy = float.Parse(cmd.Substring(9), System.Globalization.CultureInfo.InvariantCulture);
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        var p0 = cam.transform.localPosition;
                        cam.transform.localPosition = p0 + Vector3.up * dy;
                        SnapPrefix = dy < 0 ? "eyelow" : "eyehigh";
                        Snap();
                        SnapPrefix = "snap";
                        cam.transform.localPosition = p0;
                    }
                    break;
                }
                if (cmd.StartsWith("inspect "))
                {
                    InspectModel(cmd.Substring(8).Trim());
                    break;
                }
                if (cmd.StartsWith("addpkg "))
                {
                    AddPackage(cmd.Substring(7).Trim());
                    break;
                }
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
    internal static string SnapPrefix = "snap";
    internal static void Snap()
    {
        var main = Camera.main;
        if (main != null)
        {
            // Vista del conductor como con el visor puesto: al frente y girando la cabeza
            var t = main.transform;
            var original = t.localRotation;
            // Los espejos se actualizan en LateUpdate (solo en Play): forzarlos para la captura
            foreach (var m in UnityEngine.Object.FindObjectsByType<VRCar.Vehicle.PlanarMirror>()) m.Render();
            Render(main, $"Temp/{SnapPrefix}_driver.png");
            t.localRotation = original * Quaternion.Euler(8, -45, 0); Render(main, $"Temp/{SnapPrefix}_driver_left.png");
            t.localRotation = original * Quaternion.Euler(5, 40, 0); Render(main, $"Temp/{SnapPrefix}_driver_right.png");
            t.localRotation = original * Quaternion.Euler(-6, 22, 0); Render(main, $"Temp/{SnapPrefix}_driver_rearview.png");
            t.localRotation = original * Quaternion.Euler(35, 0, 0); Render(main, $"Temp/{SnapPrefix}_driver_down.png");
            // Mirar directo a cada espejo
            foreach (var m in UnityEngine.Object.FindObjectsByType<VRCar.Vehicle.PlanarMirror>())
            {
                t.rotation = Quaternion.LookRotation(m.transform.position - t.position, t.parent.up);
                Render(main, $"Temp/{SnapPrefix}_look_{m.name}.png");
            }
            t.localRotation = original;
        }

        var go = new GameObject("SnapCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        go.transform.position = new Vector3(-10f, 16f, -18f);
        go.transform.LookAt(new Vector3(-1f, 0, -2f));
        cam.fieldOfView = 55f;
        Render(cam, $"Temp/{SnapPrefix}_overview.png");
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

    static void Render(Camera cam, string path) => Render(cam, path, 1280, 720, false);

    internal static void Render(Camera cam, string path, int w, int h, bool jpg)
    {
        var rt = new RenderTexture(w, h, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, jpg ? tex.EncodeToJPG(90) : tex.EncodeToPNG());
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

    /// <summary>Lista la jerarquía de un modelo con sus bounds (en espacio del modelo) a Temp/inspect.txt</summary>
    static void InspectModel(string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) { Log("inspect: no se pudo cargar " + assetPath); return; }
        var go = (GameObject)UnityEngine.Object.Instantiate(prefab);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
        var sb = new System.Text.StringBuilder();
        var all = go.GetComponentsInChildren<Renderer>(true);
        Bounds total = new Bounds();
        bool first = true;
        foreach (var r in all) { if (first) { total = r.bounds; first = false; } else total.Encapsulate(r.bounds); }
        sb.AppendLine($"TOTAL center={total.center} size={total.size}");
        void Walk(Transform t, int depth)
        {
            var r = t.GetComponent<Renderer>();
            string b = r != null ? $" c={r.bounds.center.ToString("F2")} s={r.bounds.size.ToString("F2")} mats={string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m != null ? m.name : "null"))}" : "";
            sb.AppendLine(new string(' ', depth * 2) + t.name + $" lp={t.localPosition.ToString("F2")} lr={t.localEulerAngles.ToString("F1")} ls={t.localScale.ToString("F2")}" + b);
            foreach (Transform c in t) Walk(c, depth + 1);
        }
        Walk(go.transform, 0);
        File.WriteAllText("Temp/inspect.txt", sb.ToString());
        UnityEngine.Object.DestroyImmediate(go);
        Log("inspect OK");
    }

    static void DumpMirrors()
    {
        var sb = new System.Text.StringBuilder();
        var car = GameObject.FindWithTag("Vehicle").transform;
        foreach (var m in UnityEngine.Object.FindObjectsByType<VRCar.Vehicle.PlanarMirror>())
            sb.AppendLine($"{m.name} local={car.InverseTransformPoint(m.transform.position).ToString("F3")} fwdLocal={car.InverseTransformDirection(m.transform.forward).ToString("F2")} scale={m.transform.lossyScale.ToString("F2")}");
        foreach (var n in new[] { "InteriorSeatsColor1", "InteriorSeatsFrame1", "InteriorSteeringWheel01", "BodyWindshield", "BodyDoorLWindow", "InteriorPillar", "Main Camera" })
        {
            var go = GameObject.Find(n);
            if (go == null) { sb.AppendLine(n + " no encontrado"); continue; }
            var r = go.GetComponent<Renderer>();
            sb.AppendLine($"{n} pos={car.InverseTransformPoint(go.transform.position).ToString("F3")}" + (r != null ? $" bcenter={car.InverseTransformPoint(r.bounds.center).ToString("F3")} bsize={r.bounds.size.ToString("F3")} min={car.InverseTransformPoint(r.bounds.min).ToString("F2")} max={car.InverseTransformPoint(r.bounds.max).ToString("F2")}" : ""));
        }
        File.WriteAllText("Temp/mirrors.txt", sb.ToString());
        Log("dump-mirrors OK");
    }

    static UnityEditor.PackageManager.Requests.AddRequest addRequest;

    /// <summary>Instala un paquete UPM (resuelve la versión compatible más nueva)</summary>
    static void AddPackage(string id)
    {
        addRequest = UnityEditor.PackageManager.Client.Add(id);
        EditorApplication.update += WaitAdd;
    }

    static void WaitAdd()
    {
        if (addRequest == null || !addRequest.IsCompleted) return;
        EditorApplication.update -= WaitAdd;
        if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
            Log($"addpkg OK {addRequest.Result.name}@{addRequest.Result.version}");
        else
            Log($"addpkg ERROR {addRequest.Error?.message}");
        addRequest = null;
    }

    static void Log(string msg)
    {
        File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {msg}\n");
        Debug.Log($"[EditorCommandBridge] {msg}");
    }
}
