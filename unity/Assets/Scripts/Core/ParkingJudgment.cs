using System.Globalization;
using UnityEngine;
using VRCar.UI;

namespace VRCar.Core
{
    /// <summary>
    /// Evalúa un estacionamiento perpendicular (en batería).
    /// El espacio es un rectángulo en el piso (ancho x largo) con su "fondo" (tope) en +forward.
    /// Cuando el auto se detiene dentro del espacio, informa márgenes laterales,
    /// distancia al fondo y ángulo respecto a las líneas.
    /// </summary>
    public class ParkingJudgment : MonoBehaviour
    {
        [SerializeField] private Transform spot;              // centro del espacio; forward = hacia el tope
        [SerializeField] private float spotWidth = 2.5f;     // m (norma chilena típica)
        [SerializeField] private float spotLength = 5.0f;    // m
        [SerializeField] private BoxCollider vehicleCollider;
        [SerializeField] private HUDManager hud;
        [SerializeField] private float stoppedSpeed = 0.05f; // m/s
        [SerializeField] private float settleSeconds = 1.0f;

        private float stoppedTime;
        private bool reported;

        public void Configure(Transform spotTransform, float width, float length, BoxCollider vehicle, HUDManager hudManager)
        {
            spot = spotTransform; spotWidth = width; spotLength = length; vehicleCollider = vehicle; hud = hudManager;
        }

        private void Start()
        {
            hud?.ShowMessage("Estaciona en el espacio libre (entre las líneas amarillas)");
        }

        private void Update()
        {
            if (spot == null || vehicleCollider == null) return;
            var rb = vehicleCollider.attachedRigidbody;
            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;

            if (speed > stoppedSpeed) { stoppedTime = 0f; reported = false; return; }
            stoppedTime += Time.deltaTime;
            if (reported || stoppedTime < settleSeconds) return;

            var result = Evaluate(VehicleCorners(vehicleCollider), vehicleCollider.transform.forward, spot, spotWidth, spotLength);
            if (result.Overlap > 0f) // solo informar si el auto está (al menos en parte) en el espacio
            {
                hud?.ShowMessage(result.Message());
                reported = true;
            }
        }

        /// <summary>4 esquinas (XZ) del auto en mundo, a partir de su BoxCollider</summary>
        public static Vector3[] VehicleCorners(BoxCollider box)
        {
            Vector3 c = box.center, h = box.size * 0.5f;
            var t = box.transform;
            return new[]
            {
                t.TransformPoint(c + new Vector3(-h.x, 0, h.z)), // delantera izquierda
                t.TransformPoint(c + new Vector3(h.x, 0, h.z)),  // delantera derecha
                t.TransformPoint(c + new Vector3(-h.x, 0, -h.z)),// trasera izquierda
                t.TransformPoint(c + new Vector3(h.x, 0, -h.z)), // trasera derecha
            };
        }

        public struct Result
        {
            public bool Inside;          // las 4 esquinas dentro de las líneas
            public float LeftMargin;     // m hasta la línea izquierda (negativo = la pisa/cruza)
            public float RightMargin;    // m hasta la línea derecha
            public float EndMargin;      // m desde el extremo más profundo del auto al tope
            public float AngleDeg;       // desalineación respecto al espacio (0 = perfecto)
            public float Overlap;        // fracción aproximada de esquinas dentro (0..1)

            public bool Good => Inside && AngleDeg <= 5f;

            public string Message()
            {
                string cm(float m) => Mathf.RoundToInt(m * 100f).ToString(CultureInfo.InvariantCulture);
                string head = Good ? "¡Bien estacionado!" : Inside ? "Dentro, pero chueco" : "Fuera de las líneas";
                return $"{head}\nIzq {cm(LeftMargin)} cm · Der {cm(RightMargin)} cm · Tope {cm(EndMargin)} cm · {AngleDeg.ToString("F1", CultureInfo.InvariantCulture)}°";
            }
        }

        /// <summary>
        /// Evalúa la posición del auto respecto al espacio (lógica pura, testeable)
        /// </summary>
        public static Result Evaluate(Vector3[] corners, Vector3 vehicleForward, Transform spot, float width, float length)
        {
            float halfW = width * 0.5f, halfL = length * 0.5f;
            float minX = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            int inside = 0;
            foreach (var p in corners)
            {
                Vector3 local = spot.InverseTransformPoint(p);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                maxZ = Mathf.Max(maxZ, local.z);
                if (Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.z) <= halfL) inside++;
            }

            Vector3 f = Vector3.ProjectOnPlane(vehicleForward, spot.up).normalized;
            float angle = Vector3.Angle(f, spot.forward);
            if (angle > 90f) angle = 180f - angle; // da igual si entró de frente o en reversa

            // Izquierda/derecha según el conductor: si entró en reversa mira hacia afuera del espacio
            bool facingOut = Vector3.Dot(f, spot.forward) < 0f;
            float spotLeft = minX + halfW, spotRight = halfW - maxX;
            return new Result
            {
                Inside = inside == corners.Length,
                LeftMargin = facingOut ? spotRight : spotLeft,
                RightMargin = facingOut ? spotLeft : spotRight,
                EndMargin = halfL - maxZ,
                AngleDeg = angle,
                Overlap = inside / (float)corners.Length
            };
        }
    }
}
