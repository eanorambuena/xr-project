using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static VRCar.Core.ScreenKit;

namespace VRCar.Core
{
    /// <summary>
    /// Pantalla de "cómo se maneja" antes de empezar: aparece frente al conductor y desaparece cuando el auto
    /// empieza a moverse (ExperienceStats.Started). Mismo diseño que el menú y la pantalla de resultados; se dibuja
    /// por encima de todo con una cámara extra. No congela el juego: el conductor puede arrancar cuando quiera.
    /// La maneja ExperienceController (Refresh cada cuadro).
    /// </summary>
    public class InstructionsScreen : MonoBehaviour
    {
        const float CanvasW = 1600f, CanvasH = 900f;
        const float CanvasScale = 0.002f;   // 3.2 x 1.8 m
        const float Distance = 2.2f;        // m delante del conductor
        const float SettleSeconds = 1f;     // al empezar se re-coloca un rato: el visor tarda en fijar la posición de la cabeza

        private readonly ScreenKit kit = new ScreenKit();
        private GameObject canvasGO;
        private TextMeshProUGUI subtitle, goal, note;
        private Transform vehicle;
        private float shownAt;

        public bool IsShown => canvasGO != null && canvasGO.activeSelf;
        public string SubtitleText => subtitle != null ? subtitle.text : "";
        public string GoalText => goal != null ? goal.text : "";

        /// <summary>El auto: la pantalla se coloca hacia donde mira (así queda en el parabrisas aunque el usuario mire de lado)</summary>
        public void SetVehicle(Transform v) { vehicle = v; }

        private void OnDestroy()
        {
            kit.Dispose();
        }

        /// <summary>Muestra u oculta según haga falta (barato de llamar cada cuadro)</summary>
        public void Refresh(bool shouldShow)
        {
            if (shouldShow && !IsShown) Show();
            else if (!shouldShow && IsShown) Hide();
        }

        public void Show()
        {
            Build();
            Fill();
            canvasGO.SetActive(true);
            shownAt = Time.unscaledTime;
            var cam = Camera.main;
            PlaceInFront(cam);
        }

        public void Hide()
        {
            if (canvasGO != null) canvasGO.SetActive(false);
        }

        /// <summary>Textos según el escenario actual (público para probarlo)</summary>
        public void Fill()
        {
            Build();
            var scenario = ScenarioCatalog.FindBySceneName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            string level = scenario != null && scenario.HasDifficulty
                ? "  ·  " + ScenarioCatalog.DifficultyNames[Mathf.Clamp(ScenarioSession.Difficulty, 0, ScenarioCatalog.DifficultyNames.Length - 1)] : "";
            subtitle.text = scenario != null ? scenario.Title + level : "";
            goal.text = scenario != null ? scenario.Description : "";
        }

        private void Update()
        {
            if (IsShown && Time.unscaledTime - shownAt < SettleSeconds) PlaceInFront(Camera.main);
        }

        // ------------------------------------------------------------------
        private void PlaceInFront(Camera cam)
        {
            if (cam == null) return;
            Vector3 fwd = vehicle != null ? vehicle.forward : cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = transform.forward;
            fwd.Normalize();
            transform.position = cam.transform.position + fwd * Distance;   // a la altura de los ojos
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ------------------------------------------------------------------
        public void Build()
        {
            if (canvasGO != null) return;
            canvasGO = new GameObject("InstructionsCanvas", typeof(RectTransform));
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

            kit.NewPoly("Border", t, Vector2.zero, Chamfered(-763, 233, 763, -333, 48), BorderLine);
            kit.NewPoly("Panel", t, Vector2.zero, Chamfered(-760, 230, 760, -330, 45), Charcoal);
            kit.NewPoly("WedgeEdge", t, Vector2.zero, new[] { P(-715, 230), P(-226, 230), P(-760, -304), P(-760, 185) }, Accent);
            kit.NewPoly("Wedge", t, Vector2.zero, new[] { P(-715, 230), P(-240, 230), P(-760, -290), P(-760, 185) }, Wedge);
            kit.NewPoly("RightZone", t, Vector2.zero, new[] { P(-40, 230), P(715, 230), P(760, 185), P(760, -285), P(715, -330), P(-120, -330) }, Steel);

            // Izquierda: título, escenario y objetivo
            var head = NewText("Header", t, "CÓMO SE\nMANEJA", new Vector2(-410, 130), new Vector2(620, 170), 62, Color.white, FontStyles.Bold);
            head.alignment = TextAlignmentOptions.TopLeft;
            head.lineSpacing = -14f;
            subtitle = NewText("Scenario", t, "", new Vector2(-410, 12), new Vector2(620, 44), 26, TextLight, FontStyles.Bold);
            subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            goal = NewText("Goal", t, "", new Vector2(-410, -88), new Vector2(560, 140), 21, TextLight, FontStyles.Normal);
            goal.alignment = TextAlignmentOptions.TopLeft;
            goal.textWrappingMode = TextWrappingModes.Normal;
            note = NewText("Note", t, "Esta guía desaparece cuando\nel auto empiece a moverse.", new Vector2(-410, -225), new Vector2(620, 70), 22, Accent, FontStyles.Bold);
            note.alignment = TextAlignmentOptions.MidlineLeft;

            // Derecha: los controles como filas del menú
            ControlRow(t, "VOLANTE", "Mueve los 2 controles", 175f);
            ControlRow(t, "ACELERAR", "Gatillo derecho", 93f);
            ControlRow(t, "FRENAR", "Gatillo izquierdo", 11f);
            ControlRow(t, "REVERSA", "Botón Y  (auto detenido)", -71f);
            ControlRow(t, "TERMINAR", "Botón B", -153f);

            // Atajos: A/X = reiniciar, menú izquierdo = centrar el volante
            HoldButton(t, "A / X", "MANTÉN: REINICIAR", new Vector2(195, -262));
            HoldButton(t, "IZQ.", "MANTÉN EL BOTÓN DE 3 RAYAS PARA CENTRAR EL VOLANTE", new Vector2(565, -262));

            kit.AlwaysOnTop(canvasGO);   // una sola cámara: se dibuja sin test de profundidad, encima de todo
            canvasGO.SetActive(false);
        }

        private void ControlRow(Transform t, string action, string control, float y)
        {
            var bg = kit.NewPoly("Row_" + action, t, new Vector2(380, y), CutCorners(720, 72, 14), ButtonNormal);
            var l = NewText("Action", bg.transform, action, new Vector2(-190, 0), new Vector2(290, 72), 27, TextLight, FontStyles.Bold);
            l.alignment = TextAlignmentOptions.MidlineLeft;
            var v = NewText("Control", bg.transform, control, new Vector2(150, 0), new Vector2(400, 72), 28, Color.white, FontStyles.Bold);
            v.alignment = TextAlignmentOptions.MidlineRight;
            v.enableAutoSizing = true; v.fontSizeMin = 18f; v.fontSizeMax = 28f;
        }

        private void HoldButton(Transform t, string key, string text, Vector2 pos)
        {
            var bg = kit.NewPoly("Hold_" + key, t, pos, CutCorners(350, 76, 16), ButtonNormal);
            var k = NewText("Key", bg.transform, key, new Vector2(-122, 0), new Vector2(100, 76), 26, Accent, FontStyles.Bold);
            var l = NewText("Label", bg.transform, text, new Vector2(48, 0), new Vector2(210, 76), 20, Color.white, FontStyles.Bold);
            l.alignment = TextAlignmentOptions.MidlineLeft;
            l.enableAutoSizing = true; l.fontSizeMin = 13f; l.fontSizeMax = 20f;
        }
    }
}
