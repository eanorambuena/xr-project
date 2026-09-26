using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Física del vehículo pensada para maniobras de estacionamiento.
    /// Modelo cinemático de bicicleta (sin derrape), calibrado con un Peugeot 308:
    /// - Giro: velocidad angular = v · tan(δ) / batalla. Detenido, el auto NO gira.
    /// - El ángulo de las ruedas (δ) no salta: se mueve a una velocidad limitada, como un volante real.
    /// - Ángulo máximo derivado del diámetro de giro real (entre soleras).
    /// - Sin caja de cambios: con el auto detenido, mantener el freno activa la reversa lenta.
    ///
    /// SPEC 1.x (ver tests): acelerar, frenar, girar, limitar velocidad, reversa.
    /// </summary>
    public class VehiclePhysics : MonoBehaviour
    {
        // ============================================================
        // Inputs (0..1, -1..1)
        // ============================================================
        public float accelerationInput = 0f;  // gatillo derecho
        public float brakeInput = 0f;         // gatillo izquierdo (y reversa si el auto está detenido)
        public float steerInput = 0f;         // joystick X (-1 izquierda, 1 derecha)

        // ============================================================
        // Parámetros (Peugeot 308)
        // ============================================================
        public float maxSpeed = 20f;              // m/s (72 km/h)
        public float maxReverseSpeed = 2.0f;      // m/s (~7 km/h) — maniobra
        public float accelerationForce = 5000f;   // N
        public float brakingForce = 3000f;        // N
        public float reverseForce = 1500f;        // N
        public float wheelbase = 2.613f;          // m (batalla)
        public float track = 1.56f;               // m (trocha)
        public float turningCircle = 10.4f;       // m (diámetro de giro entre soleras)
        public float steerRate = 40f;             // °/s de las ruedas (≈1,5 s de tope a tope)
        public float reverseEngageDelay = 0.6f;   // s manteniendo el freno detenido para entrar en reversa
        public float stoppedSpeed = 0.15f;        // m/s

        /// <summary>
        /// Ángulo máximo de las ruedas en el modelo de bicicleta, derivado del diámetro de giro:
        /// la rueda delantera exterior describe el círculo de giro.
        /// </summary>
        public float maxSteerAngle => ComputeMaxSteerAngle(wheelbase, track, turningCircle);

        public float CurrentSteerAngle { get; private set; }  // ° (positivo = derecha)
        public bool IsReversing { get; private set; }
        public float SignedSpeed => rb == null ? 0f : Vector3.Dot(rb.linearVelocity, transform.forward);

        private Rigidbody rb;
        private float brakeHeldStoppedTime;

        private void Start()
        {
            Initialize();
        }

        /// <summary>Configura el Rigidbody (llamado en Start; público para tests)</summary>
        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("VehiclePhysics requiere Rigidbody componente");
                enabled = false;
                return;
            }
            rb.mass = 1200f;
            rb.linearDamping = 0.3f;
            rb.angularDamping = 5f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        public static float ComputeMaxSteerAngle(float wheelbase, float track, float turningCircle)
        {
            float outerFront = turningCircle * 0.5f;
            float rearOuter = Mathf.Sqrt(Mathf.Max(0.01f, outerFront * outerFront - wheelbase * wheelbase));
            float rearCenter = rearOuter - track * 0.5f;
            return Mathf.Atan(wheelbase / rearCenter) * Mathf.Rad2Deg;
        }

        public void FixedUpdate()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            float dt = Time.fixedDeltaTime;

            UpdateSteering(dt);
            UpdateReverseState(dt);
            ApplyMotorForce();
            ApplyBraking();
            ApplySteering(dt);
            LimitSpeed();
        }

        /// <summary>El ángulo de las ruedas sigue al input con velocidad limitada</summary>
        private void UpdateSteering(float dt)
        {
            float target = Mathf.Clamp(steerInput, -1f, 1f) * maxSteerAngle;
            CurrentSteerAngle = Mathf.MoveTowards(CurrentSteerAngle, target, steerRate * dt);
        }

        /// <summary>
        /// Reversa sin caja de cambios: detenido + freno mantenido → reversa.
        /// Se sale de la reversa al soltar ambos gatillos con el auto detenido.
        /// </summary>
        private void UpdateReverseState(float dt)
        {
            bool stopped = Mathf.Abs(SignedSpeed) < stoppedSpeed;
            if (!IsReversing)
            {
                if (stopped && brakeInput > 0.1f && accelerationInput < 0.05f)
                {
                    brakeHeldStoppedTime += dt;
                    if (brakeHeldStoppedTime >= reverseEngageDelay) IsReversing = true;
                }
                else
                {
                    brakeHeldStoppedTime = 0f;
                }
            }
            else if (brakeInput < 0.05f && accelerationInput < 0.05f && stopped)
            {
                // Se sale de la reversa solo con AMBOS gatillos sueltos y el auto detenido:
                // así frenar la reversa con el gatillo derecho nunca hace avanzar el auto.
                IsReversing = false;
                brakeHeldStoppedTime = 0f;
            }
        }

        /// <summary>SPEC 1.2: aceleración (adelante, o atrás si está en reversa)</summary>
        private void ApplyMotorForce()
        {
            if (IsReversing)
            {
                if (brakeInput > 0f && SignedSpeed > -maxReverseSpeed)
                    rb.AddForce(-transform.forward * brakeInput * reverseForce, ForceMode.Force);
                return;
            }
            if (accelerationInput > 0f)
            {
                rb.AddForce(transform.forward * accelerationInput * accelerationForce, ForceMode.Force);
            }
        }

        /// <summary>SPEC 1.3: frenado progresivo (en reversa frena el gatillo derecho)</summary>
        private void ApplyBraking()
        {
            float brake = IsReversing ? accelerationInput : brakeInput;
            if (brake > 0f && rb.linearVelocity.magnitude > 0.05f)
            {
                rb.AddForce(-rb.linearVelocity.normalized * brake * brakingForce, ForceMode.Force);
            }
        }

        /// <summary>
        /// SPEC 1.4: giro con modelo de bicicleta. yaw = v·tan(δ)/L. Sin velocidad no hay giro.
        /// La velocidad se mantiene alineada con la carrocería (sin derrape a baja velocidad).
        /// </summary>
        private void ApplySteering(float dt)
        {
            float v = SignedSpeed;
            float yawRate = v * Mathf.Tan(CurrentSteerAngle * Mathf.Deg2Rad) / wheelbase; // rad/s
            float yawDeg = yawRate * Mathf.Rad2Deg * dt;
            if (Mathf.Abs(yawDeg) > 0.00001f)
            {
                Quaternion delta = Quaternion.Euler(0f, yawDeg, 0f);
                rb.MoveRotation(delta * rb.rotation);
                transform.rotation = delta * transform.rotation; // efecto inmediato (tests / Edit Mode)
            }
            Vector3 vel = rb.linearVelocity;
            rb.linearVelocity = transform.forward * v + Vector3.up * vel.y;
        }

        /// <summary>SPEC 1.5: límite de velocidad</summary>
        private void LimitSpeed()
        {
            float v = SignedSpeed;
            float clamped = Mathf.Clamp(v, -maxReverseSpeed, maxSpeed);
            if (!Mathf.Approximately(v, clamped))
            {
                Vector3 vel = rb.linearVelocity;
                rb.linearVelocity = transform.forward * clamped + Vector3.up * vel.y;
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
            return GetCurrentSpeed() * 3.6f;
        }
    }
}
