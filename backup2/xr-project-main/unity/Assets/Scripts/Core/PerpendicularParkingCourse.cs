using System.Collections.Generic;
using UnityEngine;

namespace VRCar.Core
{
    /// <summary>
    /// Ajusta el espacio A4 del estacionamiento en batería según la dificultad elegida en el menú:
    /// cambia el ancho del espacio (líneas amarillas, línea de fondo y autos vecinos) y el ancho que usa
    /// <see cref="ParkingJudgment"/>. Va en el objeto "ParkingLot"; encuentra sus piezas por nombre,
    /// así funciona igual en la escena guardada y en la generada por SceneSetup.
    /// </summary>
    public class PerpendicularParkingCourse : MonoBehaviour
    {
        /// <summary>Ancho del espacio (m) por dificultad: fácil, media, difícil. El auto mide 1.86 m.</summary>
        public static readonly float[] SpotWidths = { 3.0f, 2.5f, 2.2f };
        /// <summary>Ancho con el que se construyó la escena (norma típica)</summary>
        public const float BaseWidth = 2.5f;

        private Transform spot, leftLine, rightLine, endLine;
        private readonly List<Transform> leftCars = new List<Transform>();
        private readonly List<Transform> rightCars = new List<Transform>();
        private readonly List<Vector3> leftBase = new List<Vector3>();
        private readonly List<Vector3> rightBase = new List<Vector3>();
        private ParkingJudgment judgment;
        private bool ready;

        public float CurrentWidth { get; private set; } = BaseWidth;

        public static float WidthFor(int difficulty) => SpotWidths[Mathf.Clamp(difficulty, 0, SpotWidths.Length - 1)];

        /// <summary>Asignar las piezas a mano (tests o escenas armadas por código)</summary>
        public void Configure(Transform spotTransform, Transform left, Transform right, Transform end,
                              IEnumerable<Transform> neighborsLeft, IEnumerable<Transform> neighborsRight, ParkingJudgment j)
        {
            spot = spotTransform; leftLine = left; rightLine = right; endLine = end; judgment = j;
            leftCars.Clear(); rightCars.Clear(); leftBase.Clear(); rightBase.Clear();
            if (neighborsLeft != null) foreach (var c in neighborsLeft) { leftCars.Add(c); leftBase.Add(c.localPosition); }
            if (neighborsRight != null) foreach (var c in neighborsRight) { rightCars.Add(c); rightBase.Add(c.localPosition); }
            ready = true;
        }

        private void Awake()
        {
            if (!ready) Discover();
            Apply(ScenarioSession.Difficulty);
        }

        /// <summary>Busca por nombre: TargetSpot, líneas de borde más cercanas y los autos de la fila A vecinos</summary>
        private void Discover()
        {
            ready = true;
            spot = transform.Find("TargetSpot");
            endLine = transform.Find("Line_Target_End");
            judgment = FindFirstObjectByType<ParkingJudgment>();
            if (spot == null) return;
            float cx = spot.localPosition.x, half = BaseWidth * 0.5f;
            float bestL = float.MaxValue, bestR = float.MaxValue;
            foreach (Transform child in transform)
            {
                float x = child.localPosition.x;
                if (child.name.StartsWith("Line_R0_"))
                {
                    float dl = Mathf.Abs(x - (cx - half)), dr = Mathf.Abs(x - (cx + half));
                    if (dl < bestL && dl < 0.2f) { bestL = dl; leftLine = child; }
                    if (dr < bestR && dr < 0.2f) { bestR = dr; rightLine = child; }
                }
                else if (child.name == "ParkedCar" && child.localPosition.z > 0f) // fila A (z > 0)
                {
                    float dx = x - cx;
                    if (dx < -BaseWidth * 0.6f && dx > -BaseWidth * 1.6f) { leftCars.Add(child); leftBase.Add(child.localPosition); }
                    else if (dx > BaseWidth * 0.6f && dx < BaseWidth * 1.6f) { rightCars.Add(child); rightBase.Add(child.localPosition); }
                }
            }
        }

        /// <summary>Colocar el espacio para una dificultad (0 fácil … 2 difícil)</summary>
        public void Apply(int difficulty)
        {
            if (!ready) Discover();
            int d = Mathf.Clamp(difficulty, 0, SpotWidths.Length - 1);
            float w = SpotWidths[d];
            CurrentWidth = w;
            if (spot == null) return;

            float cx = spot.localPosition.x, delta = (w - BaseWidth) * 0.5f;
            if (leftLine != null) { var p = leftLine.localPosition; p.x = cx - w * 0.5f; leftLine.localPosition = p; }
            if (rightLine != null) { var p = rightLine.localPosition; p.x = cx + w * 0.5f; rightLine.localPosition = p; }
            if (endLine != null) { var s = endLine.localScale; s.x = w; endLine.localScale = s; }
            for (int i = 0; i < leftCars.Count; i++)
                if (leftCars[i] != null) { var p = leftBase[i]; p.x -= delta; leftCars[i].localPosition = p; }
            for (int i = 0; i < rightCars.Count; i++)
                if (rightCars[i] != null) { var p = rightBase[i]; p.x += delta; rightCars[i].localPosition = p; }
            judgment?.SetLevel(w, ScenarioCatalog.DifficultyNames[d]);
        }
    }
}
