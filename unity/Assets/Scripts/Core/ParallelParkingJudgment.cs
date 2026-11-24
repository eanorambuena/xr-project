using System.Globalization;
using UnityEngine;
using VRCar.UI;

namespace VRCar.Core
{
    /// <summary>
    /// Evalúa un estacionamiento en paralelo junto a la solera.
    /// El espacio es el hueco entre dos autos estacionados. Su Transform ("spot") está sobre la cara de la solera,
    /// en el centro del hueco:
    ///   forward = a lo largo de la calle (sentido de la marcha)
    ///   right   = hacia la solera  → un punto del auto a 20 cm de la solera tiene x = -0.20
    /// Cuando el auto se detiene en el hueco informa: distancia a la solera, margen delante y detrás, y ángulo.
    /// </summary>
    public class ParallelParkingJudgment : MonoBehaviour, IParkingZone, IParkingScore
    {
        /// <summary>Distancia máxima a la solera para dar el estacionamiento por bueno (m)</summary>
        public const float MaxCurbDistance = 0.30f;
        /// <summary>Desalineación máxima respecto a la solera para darlo por bueno (°)</summary>
        public const float MaxAngle = 5f;
        /// <summary>Hasta qué distancia de la solera se considera que el auto "está en el hueco" (m)</summary>
        public const float BayDepth = 3.2f;
        /// <summary>Ancho del rectángulo pintado junto a la solera (m); es el mismo valor de BayWidth en SceneSetup</summary>
        public const float PaintedBayWidth = 2.3f;

        [SerializeField] private Transform spot;
        [SerializeField] private float gapLength = 6.3f;      // m entre el parachoques del auto de adelante y el de atrás
        [SerializeField] private string levelLabel = "";
        [SerializeField] private BoxCollider vehicleCollider;
        [SerializeField] private HUDManager hud;
        [SerializeField] private float stoppedSpeed = 0.05f;  // m/s
        [SerializeField] private float settleSeconds = 1.0f;

        private float stoppedTime;
        private bool reported;
        private string intro = "";

        /// <summary>Mensaje de inicio propio (lo usa la escena de pendiente); vacío = el mensaje estándar</summary>
        public string Intro => intro;
        public void SetIntro(string message) { intro = message ?? ""; }

        public float GapLength => gapLength;

        /// <summary>Fracción de las esquinas del auto que están dentro del hueco ahora mismo (0..1)</summary>
        public float VehicleOverlap =>
            spot == null || vehicleCollider == null ? 0f
            : Evaluate(ParkingJudgment.VehicleCorners(vehicleCollider), vehicleCollider.transform.forward, spot, gapLength).Overlap;

        /// <summary>Fracción de la superficie del auto dentro del rectángulo pintado del hueco (0..1)</summary>
        public float Accuracy =>
            spot == null || vehicleCollider == null ? 0f
            : ParkingAccuracy.InsideFraction(ParkingJudgment.VehicleCorners(vehicleCollider), spot, -PaintedBayWidth, 0f, -gapLength * 0.5f, gapLength * 0.5f);

        /// <summary>Desalineación respecto a la solera (°)</summary>
        public float AngleDegrees =>
            spot == null || vehicleCollider == null ? 0f
            : Evaluate(ParkingJudgment.VehicleCorners(vehicleCollider), vehicleCollider.transform.forward, spot, gapLength).AngleDeg;

        /// <summary>Descentrado dentro del hueco: la mitad de la diferencia entre el margen delantero y el trasero (m)</summary>
        public float CenterOffset
        {
            get
            {
                if (spot == null || vehicleCollider == null) return 0f;
                var r = Evaluate(ParkingJudgment.VehicleCorners(vehicleCollider), vehicleCollider.transform.forward, spot, gapLength);
                return Mathf.Abs(r.FrontMargin - r.RearMargin) * 0.5f;
            }
        }

        public void Configure(Transform spotTransform, float gap, BoxCollider vehicle, HUDManager hudManager)
        {
            spot = spotTransform; gapLength = gap; vehicleCollider = vehicle; hud = hudManager;
        }

        /// <summary>Actualizar el largo del hueco y la etiqueta de dificultad (lo hace ParallelParkingCourse)</summary>
        public void SetLevel(float gap, string label)
        {
            gapLength = gap; levelLabel = label;
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(intro)) { hud?.ShowMessage(intro); return; }
            string level = string.IsNullOrEmpty(levelLabel) ? "" : $" · {levelLabel} ({gapLength.ToString("F1", CultureInfo.InvariantCulture)} m)";
            hud?.ShowMessage($"Estaciona en paralelo entre los dos autos{level}\nPulsa B para terminar y ver tu resultado");
        }

        private void Update()
        {
            if (spot == null || vehicleCollider == null) return;
            var rb = vehicleCollider.attachedRigidbody;
            // Velocidad horizontal: en una pendiente el auto detenido puede conservar una componente vertical mínima
            float speed = rb != null ? new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude : 0f;

            if (speed > stoppedSpeed) { stoppedTime = 0f; reported = false; return; }
            stoppedTime += Time.deltaTime;
            if (reported || stoppedTime < settleSeconds) return;

            var result = Evaluate(ParkingJudgment.VehicleCorners(vehicleCollider), vehicleCollider.transform.forward, spot, gapLength);
            if (result.Overlap > 0f) // solo informar si el auto está (al menos en parte) en el hueco
            {
                hud?.ShowMessage(result.Message() + "\nMantén A/X (o R) para reintentar");
                reported = true;
            }
        }

        public struct Result
        {
            public bool InGap;            // las 4 esquinas dentro del hueco y sin pisar la solera
            public float CurbDistance;    // m entre el costado del auto y la solera (negativo = encima de la solera)
            public float FrontMargin;     // m entre el parachoques delantero y el auto de adelante
            public float RearMargin;      // m entre el parachoques trasero y el auto de atrás
            public float AngleDeg;        // desalineación respecto a la solera (0 = paralelo)
            public float Overlap;         // fracción de esquinas dentro del hueco (0..1)

            public bool Good => InGap && CurbDistance <= MaxCurbDistance && AngleDeg <= MaxAngle;

            public string Message()
            {
                string cm(float m) => Mathf.RoundToInt(m * 100f).ToString(CultureInfo.InvariantCulture);
                string head =
                    Good ? "¡Bien estacionado!" :
                    CurbDistance < 0f ? "Subiste a la solera" :
                    !InGap ? "Fuera del espacio" :
                    CurbDistance > MaxCurbDistance ? "Muy lejos de la solera" :
                    "Dentro, pero chueco";
                return $"{head}\nSolera {cm(CurbDistance)} cm · Delante {cm(FrontMargin)} cm · Atrás {cm(RearMargin)} cm · {AngleDeg.ToString("F1", CultureInfo.InvariantCulture)}°";
            }
        }

        /// <summary>Evalúa la posición del auto respecto al hueco (lógica pura, testeable)</summary>
        public static Result Evaluate(Vector3[] corners, Vector3 vehicleForward, Transform spot, float gapLength)
        {
            float halfL = gapLength * 0.5f;
            float maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            int inside = 0;
            foreach (var p in corners)
            {
                Vector3 local = spot.InverseTransformPoint(p);
                maxX = Mathf.Max(maxX, local.x);
                minZ = Mathf.Min(minZ, local.z);
                maxZ = Mathf.Max(maxZ, local.z);
                if (Mathf.Abs(local.z) <= halfL && local.x <= 0f && local.x >= -BayDepth) inside++;
            }

            Vector3 f = Vector3.ProjectOnPlane(vehicleForward, spot.up).normalized;
            float angle = Vector3.Angle(f, spot.forward);
            if (angle > 90f) angle = 180f - angle; // da igual si el auto mira hacia un lado de la calle o hacia el otro

            // "Delante" es hacia donde mira el auto: si mira contra el sentido de la calle, se intercambian
            bool facingAlong = Vector3.Dot(f, spot.forward) >= 0f;
            float aheadMargin = halfL - maxZ;   // lado +z (auto de adelante, según el sentido de la calle)
            float behindMargin = minZ + halfL;  // lado -z
            return new Result
            {
                InGap = inside == corners.Length,
                CurbDistance = -maxX,
                FrontMargin = facingAlong ? aheadMargin : behindMargin,
                RearMargin = facingAlong ? behindMargin : aheadMargin,
                AngleDeg = angle,
                Overlap = inside / (float)corners.Length
            };
        }
    }
}
