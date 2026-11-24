using UnityEngine;

namespace VRCar.Core
{
    /// <summary>
    /// Ajusta el hueco del estacionamiento en paralelo según la dificultad elegida en el menú:
    /// mueve los autos de atrás, el espacio objetivo y las líneas del hueco.
    /// Los autos de adelante quedan fijos (su parachoques trasero en x = 0); el hueco queda en x ∈ [-largo, 0].
    /// </summary>
    public class ParallelParkingCourse : MonoBehaviour
    {
        /// <summary>Largo del hueco (m) por dificultad: fácil, media, difícil. El auto mide 4.26 m.</summary>
        public static readonly float[] GapLengths = { 7.5f, 6.3f, 5.6f };

        [SerializeField] private Transform rearCars;   // grupo de autos de atrás (origen = parachoques delantero del primero)
        [SerializeField] private Transform spot;       // centro del hueco, sobre la cara de la solera
        [SerializeField] private Transform outerLine;  // línea paralela a la solera
        [SerializeField] private Transform frontLine;  // línea del extremo delantero del hueco
        [SerializeField] private Transform rearLine;   // línea del extremo trasero del hueco
        [SerializeField] private ParallelParkingJudgment judgment;
        [SerializeField] private float curbZ = -5.0f;  // z de la cara de la solera
        [SerializeField] private float bayWidth = 2.3f;

        public float CurrentGap { get; private set; }
        public float CurbZ => curbZ;

        public static float GapFor(int difficulty) => GapLengths[Mathf.Clamp(difficulty, 0, GapLengths.Length - 1)];

        public void Configure(Transform rear, Transform spotTransform, Transform outer, Transform front, Transform rearEnd,
                              float curbFaceZ, float width)
        {
            rearCars = rear; spot = spotTransform; outerLine = outer; frontLine = front; rearLine = rearEnd;
            curbZ = curbFaceZ; bayWidth = width;
        }

        public void SetJudgment(ParallelParkingJudgment j) { judgment = j; }

        private void Awake() { Apply(ScenarioSession.Difficulty); }

        /// <summary>Colocar el hueco para una dificultad (0 fácil … 2 difícil)</summary>
        public void Apply(int difficulty)
        {
            int d = Mathf.Clamp(difficulty, 0, GapLengths.Length - 1);
            float gap = GapFor(d);
            CurrentGap = gap;

            if (rearCars != null)
            {
                var p = rearCars.localPosition; p.x = -gap; rearCars.localPosition = p;
            }
            if (spot != null) spot.localPosition = new Vector3(-gap * 0.5f, 0f, curbZ);
            if (outerLine != null)
            {
                outerLine.localPosition = new Vector3(-bayWidth, 0.003f, 0f);
                outerLine.localScale = new Vector3(0.1f, 0.005f, gap - 0.1f);
            }
            if (frontLine != null)
            {
                frontLine.localPosition = new Vector3(-bayWidth * 0.5f, 0.003f, gap * 0.5f - 0.05f);
                frontLine.localScale = new Vector3(bayWidth, 0.005f, 0.1f);
            }
            if (rearLine != null)
            {
                rearLine.localPosition = new Vector3(-bayWidth * 0.5f, 0.003f, -(gap * 0.5f - 0.05f));
                rearLine.localScale = new Vector3(bayWidth, 0.005f, 0.1f);
            }
            judgment?.SetLevel(gap, ScenarioCatalog.DifficultyNames[d]);
        }
    }
}
