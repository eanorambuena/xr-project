using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static VRCar.Core.ScreenKit;

namespace VRCar.Core
{
    /// <summary>
    /// Pantalla de resultado al terminar la experiencia: choques/topes, tiempo, precisión y si aprobó.
    /// Mismo diseño que el menú de escenarios. Aparece frente al conductor, fija en el mundo, y se dibuja por encima
    /// de todo (con una cámara extra) para que no la tape el auto ni ninguna pared cercana.
    /// La interfaz se crea por código; los botones son solo indicaciones: los maneja ExperienceController.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        const float CanvasW = 1600f, CanvasH = 900f;
        const float CanvasScale = 0.002f;   // 3.2 x 1.8 m
        const float Distance = 2.2f;        // m delante del conductor

        static readonly Color Pass = new Color(0.45f, 0.82f, 0.56f);
        static readonly Color Fail = new Color(0.93f, 0.48f, 0.42f);
        static readonly Color Middle = new Color(0.96f, 0.76f, 0.30f);   // regular: ámbar

        private readonly ScreenKit kit = new ScreenKit();
        private GameObject canvasGO;
        private TextMeshProUGUI subtitle, verdict, note, collisions, time, accuracy, angle, centering;

        public bool IsShown => canvasGO != null && canvasGO.activeSelf;
        public string SubtitleText => subtitle != null ? subtitle.text : "";
        public string VerdictText => verdict != null ? verdict.text : "";
        public string NoteText => note != null ? note.text : "";
        public string CollisionsText => collisions != null ? collisions.text : "";
        public string TimeText => time != null ? time.text : "";
        public string AccuracyText => accuracy != null ? accuracy.text : "";
        public string AngleText => angle != null ? angle.text : "";
        public string CenteringText => centering != null ? centering.text : "";

        private void OnDestroy()
        {
            kit.Dispose();
        }

        /// <summary>Muestra el resultado delante del conductor</summary>
        public void Show(ExperienceResult r)
        {
            Build();
            Fill(r);
            canvasGO.SetActive(true);
            var cam = Camera.main;
            PlaceInFront(cam);
        }

        public void Hide()
        {
            if (canvasGO != null) canvasGO.SetActive(false);
        }

        /// <summary>Textos según el resultado (público para probarlo sin visor)</summary>
        public void Fill(ExperienceResult r)
        {
            Build();
            string level = string.IsNullOrEmpty(r.DifficultyName) ? "" : "  ·  " + r.DifficultyName;
            subtitle.text = r.ScenarioTitle + level;
            collisions.text = r.Collisions.ToString();
            time.text = ExperienceStats.FormatTime(r.Seconds);
            accuracy.text = ExperienceStats.Percent(r.Accuracy) + " %";
            Color gradeColor = ColorOf(r.Grade);
            accuracy.color = ColorOf(ExperienceStats.AccuracyGrade(r.Accuracy));     // cada dato con el color de su propio estado
            collisions.color = ColorOf(ExperienceStats.CollisionGrade(r.Collisions));
            angle.color = r.HasPose ? ColorOf(ExperienceStats.AngleGrade(r.AngleDeg)) : Color.white;
            // Sin ángulo ni centrado si el auto nunca entró en el espacio
            angle.text = r.HasPose ? r.AngleDeg.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "°" : "—";
            centering.text = r.HasPose ? Mathf.RoundToInt(r.CenterOffsetM * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture) + " cm" : "—";
            verdict.text = r.Grade == ExperienceGrade.Passed ? "APROBADO" : r.Grade == ExperienceGrade.Regular ? "REGULAR" : "REPROBADO";
            verdict.color = gradeColor;
            note.text = $"Aprobado: {ExperienceStats.Percent(ExperienceStats.PassAccuracy)} % o más, máx. {ExperienceStats.MaxCollisions} choque, hasta {ExperienceStats.PassAngle:0}°\n" +
                        $"Regular: {ExperienceStats.Percent(ExperienceStats.RegularAccuracy)} % o más, máx. {ExperienceStats.MaxCollisionsRegular} choques, hasta {ExperienceStats.RegularAngle:0}°";
        }

        private static Color ColorOf(ExperienceGrade g) => g == ExperienceGrade.Passed ? Pass : g == ExperienceGrade.Regular ? Middle : Fail;

        // ------------------------------------------------------------------
        private void PlaceInFront(Camera cam)
        {
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = transform.forward;
            fwd.Normalize();
            transform.position = cam.transform.position + fwd * Distance;   // a la altura de los ojos
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ------------------------------------------------------------------
        /// <summary>Crea la interfaz (una sola vez)</summary>
        public void Build()
        {
            if (canvasGO != null) return;
            canvasGO = new GameObject("ResultsCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CanvasW, CanvasH);
            rect.localScale = Vector3.one * CanvasScale;
            var t = canvasGO.transform;

            var ghost = NewText("GhostTitle", t, "VRCAR", new Vector2(-10, 345), new Vector2(1500, 200), 190, new Color(1f, 1f, 1f, 0.07f), FontStyles.Bold);
            ghost.alignment = TextAlignmentOptions.MidlineLeft;
            ghost.characterSpacing = 24f;

            // Panel igual al del menú
            kit.NewPoly("Border", t, Vector2.zero, Chamfered(-763, 233, 763, -333, 48), BorderLine);
            kit.NewPoly("Panel", t, Vector2.zero, Chamfered(-760, 230, 760, -330, 45), Charcoal);
            kit.NewPoly("WedgeEdge", t, Vector2.zero, new[] { P(-715, 230), P(-226, 230), P(-760, -304), P(-760, 185) }, Accent);
            kit.NewPoly("Wedge", t, Vector2.zero, new[] { P(-715, 230), P(-240, 230), P(-760, -290), P(-760, 185) }, Wedge);
            kit.NewPoly("RightZone", t, Vector2.zero, new[] { P(-40, 230), P(715, 230), P(760, 185), P(760, -285), P(715, -330), P(-120, -330) }, Steel);

            // Izquierda: título, escenario y veredicto
            var head = NewText("Header", t, "FIN DE LA\nEXPERIENCIA", new Vector2(-410, 130), new Vector2(620, 170), 62, Color.white, FontStyles.Bold);
            head.alignment = TextAlignmentOptions.TopLeft;
            head.lineSpacing = -14f;
            subtitle = NewText("Scenario", t, "", new Vector2(-410, 12), new Vector2(620, 44), 26, TextLight, FontStyles.Normal);
            subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            verdict = NewText("Verdict", t, "", new Vector2(-410, -90), new Vector2(620, 110), 84, Color.white, FontStyles.Bold);
            verdict.alignment = TextAlignmentOptions.MidlineLeft;
            note = NewText("Note", t, "", new Vector2(-410, -185), new Vector2(620, 70), 22, Color.white, FontStyles.Normal);
            note.alignment = TextAlignmentOptions.MidlineLeft;

            // Derecha: tres datos como filas del menú
            collisions = StatRow(t, "CHOQUES / TOPES", 175f);
            time = StatRow(t, "TIEMPO", 93f);
            accuracy = StatRow(t, "PRECISIÓN", 11f);
            angle = StatRow(t, "ALINEACIÓN", -71f);
            centering = StatRow(t, "CENTRADO", -153f);

            // Botones: A = otra vez, B = menú
            ActionButton(t, "A", "INTENTAR DE NUEVO", new Vector2(195, -262), true);
            ActionButton(t, "B", "VOLVER AL MENÚ", new Vector2(565, -262), false);

            kit.AlwaysOnTop(canvasGO);   // una sola cámara: se dibuja sin test de profundidad, encima de todo
            canvasGO.SetActive(false);
        }

        private TextMeshProUGUI StatRow(Transform t, string label, float y)
        {
            var bg = kit.NewPoly("Stat_" + label, t, new Vector2(380, y), CutCorners(720, 72, 14), ButtonNormal);
            var l = NewText("Label", bg.transform, label, new Vector2(-150, 0), new Vector2(380, 72), 27, TextLight, FontStyles.Bold);
            l.alignment = TextAlignmentOptions.MidlineLeft;
            var v = NewText("Value", bg.transform, "", new Vector2(215, 0), new Vector2(250, 72), 42, Color.white, FontStyles.Bold);
            v.alignment = TextAlignmentOptions.MidlineRight;
            return v;
        }

        private void ActionButton(Transform t, string key, string text, Vector2 pos, bool primary)
        {
            var bg = kit.NewPoly("Button_" + key, t, pos, CutCorners(350, 76, 16), primary ? ButtonSelected : ButtonNormal);
            var k = NewText("Key", bg.transform, key, new Vector2(-140, 0), new Vector2(50, 76), 44, primary ? Color.white : Accent, FontStyles.Bold);
            var l = NewText("Label", bg.transform, text, new Vector2(25, 0), new Vector2(260, 76), 24, Color.white, FontStyles.Bold);
            l.alignment = TextAlignmentOptions.MidlineLeft;
            l.enableAutoSizing = true; l.fontSizeMin = 16f; l.fontSizeMax = 24f;
        }
    }
}
