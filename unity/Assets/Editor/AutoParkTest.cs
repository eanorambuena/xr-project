using System.IO;
using UnityEditor;
using UnityEngine;
using VRCar.Vehicle;

/// <summary>
/// Prueba de manejo automática (solo en Play): estaciona en reversa en el espacio objetivo
/// con una maniobra de libro (avanzar, detenerse, girar el volante a tope, retroceder
/// hasta quedar alineado, enderezar y retroceder hasta el tope).
/// Sirve para validar que el viraje calibrado permite estacionar y para tomar capturas.
/// Registra el recorrido en Temp/autopark.log
/// </summary>
public static class AutoParkTest
{
    enum Phase { Forward, Stop, TurnWheel, ReverseTurn, Straighten, ReverseStraight, Settle, Done }

    static Phase phase;
    static bool sensorSnapped, turnSnapped;
    static VehiclePhysics physics;
    static Transform spot;
    static float phaseStart;
    static float startTurnX;
    const string LogPath = "Temp/autopark.log";

    public static void Start(float turnStartOffset = 4.6f)
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("autopark requiere Play"); return; }
        physics = Object.FindFirstObjectByType<VehiclePhysics>();
        spot = GameObject.Find("TargetSpot")?.transform;
        if (physics == null || spot == null) { Debug.LogError("autopark: falta vehículo o espacio"); return; }
        var mapper = physics.GetComponent<InputMapper>();
        if (mapper != null) mapper.enabled = false;
        startTurnX = spot.position.x + turnStartOffset;
        File.WriteAllText(LogPath, $"autopark start turnX={startTurnX:F2}\n");
        sensorSnapped = false; turnSnapped = false;
        Set(Phase.Forward);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Set(Phase p)
    {
        phase = p;
        phaseStart = Time.time;
        Log($"-> {p}");
    }

    static void Log(string msg)
    {
        var t = physics.transform;
        File.AppendAllText(LogPath, $"{Time.time:F2}s {msg} pos=({t.position.x:F2},{t.position.z:F2}) yaw={t.eulerAngles.y:F1} v={physics.SignedSpeed:F2} steer={physics.CurrentSteerAngle:F1} rev={physics.IsReversing} sensor={SensorText()} wheels={WheelText()}\n");
    }

    static string SensorText()
    {
        var l = GameObject.Find("SensorLabel")?.GetComponent<TMPro.TextMeshProUGUI>();
        return l != null && l.gameObject.activeInHierarchy ? l.text : "(oculto)";
    }

    static string WheelText()
    {
        var v = physics.GetComponent<VehicleVisuals>();
        if (v == null || v.frontLeft == null || v.frontRight == null) return "-";
        // Dirección a la que apunta cada rueda delantera, relativa al auto (° yaw, + = derecha)
        float yl = Vector3.SignedAngle(physics.transform.right, Vector3.ProjectOnPlane(v.frontLeft.right, physics.transform.up), physics.transform.up);
        float yr = Vector3.SignedAngle(physics.transform.right, Vector3.ProjectOnPlane(v.frontRight.right, physics.transform.up), physics.transform.up);
        return $"L{yl:F1}/R{yr:F1}";
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || physics == null) { EditorApplication.update -= Tick; return; }
        var t = physics.transform;
        float elapsed = Time.time - phaseStart;
        physics.accelerationInput = 0f; physics.brakeInput = 0f;

        switch (phase)
        {
            case Phase.Forward:
                physics.steerInput = 0f;
                if (t.position.x < startTurnX - 1.5f) physics.accelerationInput = physics.SignedSpeed < 1.5f ? 0.4f : 0f;
                else Set(Phase.Stop);
                break;
            case Phase.Stop:
                physics.brakeInput = 0.6f;
                if (Mathf.Abs(physics.SignedSpeed) < 0.05f && elapsed > 0.3f) Set(Phase.TurnWheel);
                break;
            case Phase.TurnWheel:
                // Volante a tope a la izquierda, detenido (freno suave: todavía no entra la reversa del todo)
                physics.steerInput = -1f;
                physics.brakeInput = 0.3f;
                if (physics.IsReversing && Mathf.Abs(physics.CurrentSteerAngle + physics.maxSteerAngle) < 0.5f) Set(Phase.ReverseTurn);
                break;
            case Phase.ReverseTurn:
                physics.steerInput = -1f;
                if (!turnSnapped && elapsed > 2f)
                {
                    turnSnapped = true;
                    EditorCommandBridge.SnapPrefix = "turn";
                    EditorCommandBridge.Snap();
                    EditorCommandBridge.SnapPrefix = "snap";
                }
                physics.brakeInput = physics.SignedSpeed > -0.8f ? 0.45f : 0f; // ~3 km/h
                if (Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.y, 180f)) < 4f) Set(Phase.Straighten);
                break;
            case Phase.Straighten:
                physics.steerInput = 0f;
                physics.brakeInput = physics.SignedSpeed > -0.6f ? 0.35f : 0f;
                if (Mathf.Abs(physics.CurrentSteerAngle) < 0.5f) Set(Phase.ReverseStraight);
                break;
            case Phase.ReverseStraight:
                physics.steerInput = 0f;
                // Retroceder hasta que el centro quede ~en el centro del espacio (tope a ~40 cm)
                var det = physics.GetComponent<VRCar.Obstacles.ObstacleDetector>();
                if (!sensorSnapped && det != null && det.GetClosestDistance() < 0.9f)
                {
                    sensorSnapped = true;
                    Log("SNAP sensor");
                    EditorCommandBridge.Snap();
                }
                if (t.position.z < spot.position.z - 0.25f)
                    physics.brakeInput = physics.SignedSpeed > -0.6f ? 0.35f : 0f;
                else { physics.accelerationInput = 0.8f; if (Mathf.Abs(physics.SignedSpeed) < 0.05f) Set(Phase.Settle); }
                break;
            case Phase.Settle:
                // soltar todo: el auto queda detenido y se evalúa el estacionamiento
                if (elapsed > 2.5f) { Set(Phase.Done); }
                break;
            case Phase.Done:
                var hud = GameObject.Find("MessageText")?.GetComponent<TMPro.TextMeshProUGUI>();
                Log("HUD: " + (hud != null ? hud.text.Replace("\n", " | ") : "(sin HUD)"));
                EditorApplication.update -= Tick;
                return;
        }
        if (Time.frameCount % 30 == 0) Log(phase.ToString());
    }
}
