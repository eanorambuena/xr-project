using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static VRCar.Core.ScreenKit;

namespace VRCar.Core
{
    /// <summary>
    /// Menú de selección de escenario, en un panel flotante frente al conductor.
    /// Se maneja sin apuntar (no necesita rayo ni EventSystem), con lo que hay en el visor y fuera de él:
    /// - Joystick (cualquier mano) o flechas / W S: arriba-abajo elige escenario, izquierda-derecha cambia la dificultad
    /// - Gatillo o A/X (o Enter / Espacio): iniciar el escenario elegido
    /// - Teclas 1-4: elegir directo
    /// La interfaz se crea al iniciar, así la escena del menú solo necesita este componente.
    /// </summary>
    public class ScenarioMenu : MonoBehaviour
    {
        const float CanvasW = 1600f, CanvasH = 900f;
        const float CanvasScale = 0.002f;   // 3.2 x 1.8 m, a ~2.4 m del conductor

        private sealed class RowUI
        {
            public Image bg;
            public TextMeshProUGUI number, label, right;
        }

        private int selected;
        private int difficulty = 1;
        private readonly List<RowUI> rows = new List<RowUI>();
        private readonly ScreenKit kit = new ScreenKit();   // dibuja las formas y guarda sus texturas
        private TextMeshProUGUI statusText;
        private InputAction navigate, submit;
        private float nextRepeatY, nextRepeatX;

        public int SelectedIndex => selected;
        public int Difficulty => difficulty;
        public ScenarioInfo Selected => ScenarioCatalog.All[selected];

        private void Awake()
        {
            difficulty = Mathf.Clamp(ScenarioSession.Difficulty, 0, ScenarioSession.DifficultyCount - 1);
            BuildUI();
            CreateInput();
            Refresh();
        }

        private void OnEnable() { navigate?.Enable(); submit?.Enable(); }
        private void OnDisable() { navigate?.Disable(); submit?.Disable(); }
        private void OnDestroy()
        {
            navigate?.Dispose(); submit?.Dispose();
            kit.Dispose();
        }

        // ------------------------------------------------------------------
        // Lógica (pública para poder probarla sin visor)
        // ------------------------------------------------------------------
        /// <summary>Mover la selección (+1 baja, -1 sube). No da la vuelta.</summary>
        public void Move(int delta)
        {
            selected = Mathf.Clamp(selected + delta, 0, ScenarioCatalog.All.Length - 1);
            Refresh();
        }

        /// <summary>Elegir por número (1 = primero)</summary>
        public void SelectNumber(int number)
        {
            if (number < 1 || number > ScenarioCatalog.All.Length) return;
            selected = number - 1;
            Refresh();
        }

        /// <summary>Cambiar la dificultad (+1 más difícil). Solo si el escenario elegido la tiene.</summary>
        public void ChangeDifficulty(int delta)
        {
            if (!Selected.HasDifficulty) return;
            difficulty = Mathf.Clamp(difficulty + delta, 0, ScenarioSession.DifficultyCount - 1);
            Refresh();
        }

        /// <summary>Iniciar el escenario elegido. false si todavía no está disponible.</summary>
        public bool Confirm()
        {
            if (!ScenarioCatalog.IsAvailable(Selected))
            {
                SetStatus("Este escenario aún no está disponible");
                return false;
            }
            ScenarioSession.SetDifficulty(difficulty);
            SceneManager.LoadScene(Selected.SceneName);
            return true;
        }

        // ------------------------------------------------------------------
        private void Update()
        {
            if (navigate == null) return;
            Vector2 nav = navigate.ReadValue<Vector2>();
            int dy = Repeat(nav.y, ref nextRepeatY);
            int dx = Repeat(nav.x, ref nextRepeatX);
            if (dy != 0) Move(-dy);   // joystick arriba = escenario anterior
            if (dx != 0) ChangeDifficulty(dx);
            if (submit.WasPressedThisFrame()) Confirm();

            var kb = Keyboard.current;
            if (kb != null)
                for (int i = 0; i < ScenarioCatalog.All.Length && i < 9; i++)
                    if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) SelectNumber(i + 1);
        }

        /// <summary>Convierte el joystick en pasos discretos con repetición al mantener (devuelve -1, 0 o 1)</summary>
        private static int Repeat(float value, ref float nextAllowed)
        {
            if (Mathf.Abs(value) < 0.6f) { nextAllowed = 0f; return 0; }
            if (Time.unscaledTime < nextAllowed) return 0;
            nextAllowed = Time.unscaledTime + (nextAllowed == 0f ? 0.40f : 0.18f);
            return value > 0f ? 1 : -1;
        }

        private void CreateInput()
        {
            navigate = new InputAction("Navigate", InputActionType.Value, expectedControlType: "Vector2");
            navigate.AddBinding("<XRController>{RightHand}/primary2DAxis");
            navigate.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

            submit = new InputAction("Submit", InputActionType.Button);
            submit.AddBinding("<XRController>{RightHand}/trigger");
            submit.AddBinding("<XRController>{LeftHand}/trigger");
            submit.AddBinding("<XRController>{RightHand}/primaryButton");
            submit.AddBinding("<XRController>{LeftHand}/primaryButton");
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Keyboard>/numpadEnter");
            submit.AddBinding("<Keyboard>/space");
            submit.AddBinding("<Gamepad>/buttonSouth");
        }

        // ------------------------------------------------------------------
        // Interfaz
        // ------------------------------------------------------------------
        private void Refresh()
        {
            var all = ScenarioCatalog.All;
            for (int i = 0; i < rows.Count && i < all.Length; i++)
            {
                var s = all[i]; var r = rows[i];
                bool available = ScenarioCatalog.IsAvailable(s);
                bool isSelected = i == selected;
                r.bg.color = isSelected ? ButtonSelected : ButtonNormal;
                r.number.color = isSelected ? Color.white : available ? Accent : TextDim;
                r.label.text = s.Title;
                r.label.color = !available ? TextDim : isSelected ? Color.white : TextLight;
                if (!available)
                {
                    r.right.text = "PRÓXIMAMENTE"; r.right.fontSize = 19f; r.right.color = TextDim;
                }
                else if (isSelected && s.HasDifficulty)
                {
                    r.right.text = $"<  {ScenarioCatalog.DifficultyNames[difficulty]}  >"; r.right.fontSize = 26f; r.right.color = Color.white;
                }
                else r.right.text = "";
            }
            if (statusText != null) statusText.text = "";
        }

        private void SetStatus(string message) { if (statusText != null) statusText.text = message; }

        private void BuildUI()
        {
            var canvasGO = new GameObject("MenuCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CanvasW, CanvasH);
            rect.localScale = Vector3.one * CanvasScale;
            var t = canvasGO.transform;

            // Nombre del proyecto, gigante y tenue detrás del panel
            var ghost = NewText("GhostTitle", t, "VRCAR", new Vector2(-10, 345), new Vector2(1500, 200), 190, new Color(1f, 1f, 1f, 0.07f), FontStyles.Bold);
            ghost.alignment = TextAlignmentOptions.MidlineLeft;
            ghost.characterSpacing = 24f;

            // Panel: borde fino + fondo carbón, con dos esquinas cortadas
            kit.NewPoly("Border", t, Vector2.zero, Chamfered(-763, 233, 763, -333, 48), BorderLine);
            kit.NewPoly("Panel", t, Vector2.zero, Chamfered(-760, 230, 760, -330, 45), Charcoal);
            // Zona izquierda: franja diagonal con borde de acento
            kit.NewPoly("WedgeEdge", t, Vector2.zero, new[] { P(-715, 230), P(-226, 230), P(-760, -304), P(-760, 185) }, Accent);
            kit.NewPoly("Wedge", t, Vector2.zero, new[] { P(-715, 230), P(-240, 230), P(-760, -290), P(-760, 185) }, Wedge);
            // Zona derecha: acero claro, con el borde izquierdo inclinado
            kit.NewPoly("RightZone", t, Vector2.zero, new[] { P(-40, 230), P(715, 230), P(760, 185), P(760, -285), P(715, -330), P(-120, -330) }, Steel);

            var head = NewText("Header", t, "ELIGE UN\nESCENARIO", new Vector2(-410, 130), new Vector2(620, 170), 66, Color.white, FontStyles.Bold);
            head.alignment = TextAlignmentOptions.TopLeft;
            head.lineSpacing = -14f;

            // Botones de escenarios (derecha)
            var all = ScenarioCatalog.All;
            float firstRowY = -50f + (all.Length - 1) * 60f;   // las filas quedan centradas en la zona de botones, sean 3, 4 o más
            for (int i = 0; i < all.Length; i++)
            {
                var ui = new RowUI();
                ui.bg = kit.NewPoly("Row" + i, t, new Vector2(380, firstRowY - 120 * i), CutCorners(720, 96, 18), Color.white);
                ui.number = NewText("Number", ui.bg.transform, (i + 1).ToString(), new Vector2(-318, 0), new Vector2(56, 96), 48, Accent, FontStyles.Bold);
                ui.label = NewText("Label", ui.bg.transform, "", new Vector2(-65, 0), new Vector2(440, 96), 34, Color.white, FontStyles.Bold);
                ui.label.alignment = TextAlignmentOptions.MidlineLeft;
                ui.label.enableAutoSizing = true; ui.label.fontSizeMin = 24f; ui.label.fontSizeMax = 34f;
                ui.right = NewText("Right", ui.bg.transform, "", new Vector2(255, 0), new Vector2(190, 96), 24, Color.white, FontStyles.Bold);
                rows.Add(ui);
            }

            statusText = NewText("Status", t, "", new Vector2(0, -375), new Vector2(1200, 40), 28, new Color(1f, 0.55f, 0.45f), FontStyles.Normal);
        }
    }
}
