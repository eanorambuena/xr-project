using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Controla la física del vehículo: aceleración, frenado, dirección y límites de velocidad
    /// Implementa la especificación SDD:
    /// - SPEC 1.1: Inicialización con valores correctos
    /// - SPEC 1.2: Aceleración limita
    /// - SPEC 1.3: Frenado progresivo
    /// - SPEC 1.4: Dirección según joystick
    /// - SPEC 1.5: Límite de velocidad máxima
    /// </summary>
    public class VehiclePhysics : MonoBehaviour
    {
        // ============================================================
        // Inputs (recibidos desde InputMapper)
        // ============================================================
        public float accelerationInput = 0f;  // 0..1 (trigger derecho)
        public float brakeInput = 0f;         // 0..1 (trigger izquierdo)
        public float steerInput = 0f;         // -1..1 (joystick X)

        // ============================================================
        // Parámetros de vehículo (ajustables)
        // ============================================================
        public float maxSpeed = 20f;           // m/s (72 km/h)
        public float accelerationForce = 5000f; // Newtons
        public float brakingForce = 3000f;    // Newtons
        public float maxSteerAngle = 45f;     // grados

        // ============================================================
        // Referencias
        // ============================================================
        private Rigidbody rb;
        private Vector3 currentVelocity;

        private void Start()
        {
            Initialize();
        }

        /// <summary>Configura el Rigidbody (llamado en Start; público para tests)</summary>
        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();

            // Validar que existe Rigidbody
            if (rb == null)
            {
                Debug.LogError("VehiclePhysics requiere Rigidbody componente");
                enabled = false;
                return;
            }

            // Configurar Rigidbody
            rb.mass = 1200f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                           RigidbodyConstraints.FreezeRotationZ;

            currentVelocity = Vector3.zero;
        }

        public void FixedUpdate()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            ApplyMotorForce();
            ApplySteering();
            ApplyBraking();
            LimitSpeed();
        }

        /// <summary>
        /// Aplicar fuerza de aceleración según input
        /// SPEC 1.2: Aceleración
        /// F = input * accelerationForce
        /// </summary>
        private void ApplyMotorForce()
        {
            if (accelerationInput > 0f)
            {
                Vector3 forceDirection = transform.forward;
                Vector3 motorForce = forceDirection * accelerationInput * accelerationForce;
                rb.AddForce(motorForce, ForceMode.Force);
            }
        }

        /// <summary>
        /// Aplicar frenado progresivo
        /// SPEC 1.3: Frenado
        /// Fuerza opuesta a velocidad actual
        /// </summary>
        private void ApplyBraking()
        {
            if (brakeInput > 0f && rb.linearVelocity.magnitude > 0.1f)
            {
                Vector3 brakingDirection = -rb.linearVelocity.normalized;
                Vector3 brakeForce = brakingDirection * brakeInput * brakingForce;
                rb.AddForce(brakeForce, ForceMode.Force);
            }
        }

        /// <summary>
        /// Girar vehículo según joystick
        /// SPEC 1.4: Dirección
        /// Rotación sobre eje Y proporcional a steerInput
        /// </summary>
        private void ApplySteering()
        {
            if (Mathf.Abs(steerInput) > 0.01f)
            {
                float steerRotation = steerInput * 2f; // grados por frame
                transform.Rotate(0, steerRotation, 0);
                // El auto no derrapa: la velocidad gira junto con la carrocería
                if (rb != null) rb.linearVelocity = Quaternion.Euler(0, steerRotation, 0) * rb.linearVelocity;
            }
        }

        /// <summary>
        /// Limitar velocidad máxima
        /// SPEC 1.5: Límite de velocidad
        /// </summary>
        private void LimitSpeed()
        {
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        public float GetCurrentSpeed()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return 0f;
            return rb.linearVelocity.magnitude;
        }

        public float GetCurrentSpeedKmh()
        {
            return GetCurrentSpeed() * 3.6f; // m/s a km/h
        }
    }
}
