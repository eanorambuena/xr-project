using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Anima el modelo del auto a partir de VehiclePhysics:
    /// - Ruedas delanteras con geometría Ackermann (la interior gira más que la exterior)
    /// - Giro (rodadura) de las 4 ruedas según la velocidad
    /// - Volante con relación de dirección real (ángulo del volante = ángulo de ruedas × relación)
    /// Los ejes se expresan en el espacio del PADRE de cada rueda (el modelo):
    /// steerAxis = eje vertical del modelo, spinAxis = eje de las ruedas en su pose de reposo.
    /// </summary>
    public class VehicleVisuals : MonoBehaviour
    {
        public VehiclePhysics physics;
        public Transform frontLeft, frontRight, rearLeft, rearRight;
        public Transform steeringWheel;           // pivote: su forward es el eje de la columna
        public float wheelRadius = 0.32f;         // m (mundo)
        public float steeringRatio = 14f;         // ° de volante por ° de rueda
        public Vector3 steerAxis = Vector3.forward;
        public Vector3 spinAxis = Vector3.right;
        public float steerSign = 1f;

        private Quaternion restFL, restFR, restRL, restRR, restWheel;
        private float spin;
        private bool captured;

        private void Capture()
        {
            if (captured) return;
            if (frontLeft) restFL = frontLeft.localRotation;
            if (frontRight) restFR = frontRight.localRotation;
            if (rearLeft) restRL = rearLeft.localRotation;
            if (rearRight) restRR = rearRight.localRotation;
            if (steeringWheel) restWheel = steeringWheel.localRotation;
            if (physics == null) physics = GetComponent<VehiclePhysics>();
            captured = true;
        }

        private void LateUpdate()
        {
            Capture();
            if (physics == null) return;
            float delta = physics.CurrentSteerAngle;
            var (left, right) = AckermannAngles(delta, physics.wheelbase, physics.track);

            spin += physics.SignedSpeed * Time.deltaTime / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg;
            spin %= 360f;

            // Rueda izquierda: su eje local apunta hacia afuera (izquierda) → rodar hacia adelante es +spin
            if (frontLeft) frontLeft.localRotation = Quaternion.AngleAxis(steerSign * left, steerAxis) * restFL * Quaternion.AngleAxis(spin, spinAxis);
            if (frontRight) frontRight.localRotation = Quaternion.AngleAxis(steerSign * right, steerAxis) * restFR * Quaternion.AngleAxis(-spin, spinAxis);
            if (rearLeft) rearLeft.localRotation = restRL * Quaternion.AngleAxis(spin, spinAxis);
            if (rearRight) rearRight.localRotation = restRR * Quaternion.AngleAxis(-spin, spinAxis);
            if (steeringWheel) steeringWheel.localRotation = restWheel * Quaternion.AngleAxis(-delta * steeringRatio, Vector3.forward);
        }

        /// <summary>
        /// Ángulos de las ruedas delanteras (izquierda, derecha) para un ángulo "de bicicleta" δ.
        /// Positivo = a la derecha. La rueda interior al giro gira más.
        /// </summary>
        public static (float left, float right) AckermannAngles(float deltaDeg, float wheelbase, float track)
        {
            if (Mathf.Abs(deltaDeg) < 0.001f) return (0f, 0f);
            float R = wheelbase / Mathf.Tan(Mathf.Abs(deltaDeg) * Mathf.Deg2Rad); // radio al centro del eje trasero
            float inner = Mathf.Atan(wheelbase / (R - track / 2f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelbase / (R + track / 2f)) * Mathf.Rad2Deg;
            return deltaDeg > 0f ? (outer, inner) : (-inner, -outer);
        }
    }
}
