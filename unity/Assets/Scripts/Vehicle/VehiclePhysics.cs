using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Física del vehículo pensada para maniobras de estacionamiento.
    /// Modelo por rueda (raycast), calibrado con un Peugeot 308:
    /// - Cada rueda lanza un raycast al suelo y aporta suspensión (resorte + amortiguador),
    ///   agarre lateral y tracción/freno. El giro del auto emerge de las fuerzas de las ruedas.
    /// - Agarre alto (auto real, poco derrape): es un estacionamiento, no una pista.
    /// - Ruedas delanteras con geometría Ackermann; el ángulo no salta, se mueve a velocidad limitada.
    /// - Ángulo máximo derivado del diámetro de giro real (entre soleras).
    /// - Sin caja de cambios: con el auto detenido, mantener el freno activa la reversa lenta.
    /// - La carrocería no cabecea ni se ladea (conductor siempre estable, sin mareo en VR).
    ///
    /// SPEC 1.x (ver tests): acelerar, frenar, girar, limitar velocidad, reversa, suspensión.
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
        public float accelerationForce = 5000f;   // N (tracción delantera, repartida en las 2 ruedas)
        public float brakingForce = 3000f;        // N (repartida en las 4 ruedas)
        public float reverseForce = 1500f;        // N
        public float wheelbase = 2.613f;          // m (batalla)
        public float track = 1.56f;               // m (trocha)
        public float turningCircle = 10.4f;       // m (diámetro de giro entre soleras)
        public float maxLateralAccel = 5f;        // m/s² — a velocidad el tope de dirección se reduce para no pasar el agarre
        public float steerRate = 40f;             // °/s de las ruedas (≈1,5 s de tope a tope)
        public float reverseEngageDelay = 0.6f;   // s manteniendo el freno detenido para entrar en reversa
        public float stoppedSpeed = 0.15f;        // m/s

        [Header("Carrocería (para la inercia de giro)")]
        public Vector3 bodySize = new Vector3(1.856f, 1.535f, 4.255f); // ancho, alto, largo (m)

        [Header("Ruedas y suspensión (un raycast por rueda)")]
        public float wheelRadius = 0.31f;             // m
        public float suspensionRestLength = 0.30f;    // m (recorrido total, resorte extendido)
        public float springStiffness = 20000f;        // N/m por rueda (~1,3 Hz de frecuencia de marcha)
        public float damperCoefficient = 2000f;       // N·s/m por rueda (amortiguamiento ~0,4)
        [Tooltip("Altura del pivote del auto sobre el suelo en reposo (la base de la carrocería toca el suelo del modelo)")]
        public float restPivotHeight = 0.7675f;       // m

        [Header("Neumáticos")]
        [Tooltip("Coeficiente de fricción: fuerza máxima = grip × carga de la rueda. ~1 = asfalto seco, poco derrape")]
        public float tireGrip = 1.0f;
        [Tooltip("Fuerza lateral por m/s de deslizamiento (N·s/m). Alta = sigue la trayectoria de la rueda")]
        public float lateralStiffness = 8000f;
        [Tooltip("Por debajo de esta velocidad longitudinal el freno de cada rueda se atenúa (evita vibrar al detenerse)")]
        public float brakeSmoothSpeed = 0.3f;

        /// <summary>
        /// Ángulo máximo de las ruedas en el modelo de bicicleta, derivado del diámetro de giro:
        /// la rueda delantera exterior describe el círculo de giro.
        /// </summary>
        public float maxSteerAngle => ComputeMaxSteerAngle(wheelbase, track, turningCircle);

        public float CurrentSteerAngle { get; private set; }  // ° (positivo = derecha)
        public bool IsReversing { get; private set; }
        public float SignedSpeed => rb == null ? 0f : Vector3.Dot(rb.linearVelocity, transform.forward);

        private Rigidbody rb;
        private bool initialized;
        private float brakeHeldStoppedTime;
        private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

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
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.5f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            // Inercia de una caja homogénea y centro de masa en el pivote: no dependen del collider
            // (sin esto un Rigidbody sin collider tiene inercia 1 kg·m² y las fuerzas de rueda lo hacen girar sin control).
            rb.centerOfMass = Vector3.zero;
            float m = rb.mass, w = bodySize.x, h = bodySize.y, l = bodySize.z;
            rb.inertiaTensor = new Vector3(m / 12f * (h * h + l * l), m / 12f * (w * w + l * l), m / 12f * (w * w + h * h));
            rb.inertiaTensorRotation = Quaternion.identity;
            initialized = true;
        }

        /// <summary>Vuelve el estado de manejo a cero (reinicio del ejercicio)</summary>
        public void ResetDrivingState()
        {
            CurrentSteerAngle = 0f;
            IsReversing = false;
            brakeHeldStoppedTime = 0f;
            accelerationInput = brakeInput = steerInput = 0f;
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }

        public static float ComputeMaxSteerAngle(float wheelbase, float track, float turningCircle)
        {
            float outerFront = turningCircle * 0.5f;
            float rearOuter = Mathf.Sqrt(Mathf.Max(0.01f, outerFront * outerFront - wheelbase * wheelbase));
            float rearCenter = rearOuter - track * 0.5f;
            return Mathf.Atan(wheelbase / rearCenter) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Geometría Ackermann: a partir del ángulo "de bicicleta" (centro del eje delantero) calcula el
        /// ángulo de cada rueda delantera. La rueda interior gira más que la exterior.
        /// </summary>
        public static void ComputeAckermann(float steerDeg, float wheelbase, float track, out float leftDeg, out float rightDeg)
        {
            leftDeg = rightDeg = steerDeg;
            if (Mathf.Abs(steerDeg) < 0.001f) return;
            float tan = Mathf.Tan(Mathf.Abs(steerDeg) * Mathf.Deg2Rad);
            float radius = wheelbase / tan; // radio del centro del eje trasero
            float inner = Mathf.Atan(wheelbase / Mathf.Max(0.01f, radius - track * 0.5f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelbase / (radius + track * 0.5f)) * Mathf.Rad2Deg;
            if (steerDeg > 0f) { rightDeg = inner; leftDeg = outer; }
            else { leftDeg = -inner; rightDeg = -outer; }
        }

        /// <summary>Compresión estática del resorte (m) con el peso repartido en las 4 ruedas</summary>
        public static float ComputeStaticCompression(float mass, float springStiffness)
        {
            return mass * -Physics.gravity.y / (4f * Mathf.Max(1f, springStiffness));
        }

        public void FixedUpdate()
        {
            if (!initialized) Initialize();
            if (rb == null) return;
            float dt = Time.fixedDeltaTime;

            UpdateSteering(dt);
            UpdateReverseState(dt);

            float brake = IsReversing ? accelerationInput : brakeInput;
            bool holdStopped = HoldStopped(brake, dt);
            ApplyWheelForces(brake, holdStopped);
            LimitSpeed();
        }

        /// <summary>El ángulo de las ruedas sigue al input con velocidad limitada</summary>
        private void UpdateSteering(float dt)
        {
            float v = Mathf.Max(0.1f, Mathf.Abs(SignedSpeed));
            float speedLimit = Mathf.Atan(wheelbase * maxLateralAccel / (v * v)) * Mathf.Rad2Deg;
            float target = Mathf.Clamp(steerInput, -1f, 1f) * Mathf.Min(maxSteerAngle, speedLimit);
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

        /// <summary>
        /// Si el freno de este paso alcanza para detener el auto, lo detiene exacto
        /// (sin arrastre ni rebote) y esta vez no se aplica fuerza de freno en las ruedas.
        /// </summary>
        private bool HoldStopped(float brake, float dt)
        {
            if (brake <= 0f) return false;
            Vector3 v = rb.linearVelocity;
            Vector3 horizontal = new Vector3(v.x, 0f, v.z);
            float dvMax = brake * brakingForce / rb.mass * dt;
            if (horizontal.magnitude > dvMax) return false;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            return true;
        }

        /// <summary>SPEC 1.2 / 1.3 / 1.4: suspensión, agarre, tracción y freno de cada rueda</summary>
        private void ApplyWheelForces(float brake, bool holdStopped)
        {
            float drive = 0f;
            if (IsReversing)
            {
                if (brakeInput > 0f && SignedSpeed > -maxReverseSpeed) drive = -brakeInput * reverseForce;
            }
            else if (accelerationInput > 0f)
            {
                drive = accelerationInput * accelerationForce;
            }
            float brakePerWheel = holdStopped ? 0f : brake * brakingForce * 0.25f;
            float drivePerFrontWheel = drive * 0.5f;

            ComputeAckermann(CurrentSteerAngle, wheelbase, track, out float leftDeg, out float rightDeg);
            float half = wheelbase * 0.5f, halfTrack = track * 0.5f;

            ApplyWheel(new Vector3(-halfTrack, 0f, half), leftDeg, drivePerFrontWheel, brakePerWheel);
            ApplyWheel(new Vector3(halfTrack, 0f, half), rightDeg, drivePerFrontWheel, brakePerWheel);
            ApplyWheel(new Vector3(-halfTrack, 0f, -half), 0f, 0f, brakePerWheel);
            ApplyWheel(new Vector3(halfTrack, 0f, -half), 0f, 0f, brakePerWheel);
        }

        /// <summary>Altura local (y) del punto de anclaje del resorte, para que en reposo el pivote quede a restPivotHeight</summary>
        private float MountHeight()
        {
            float staticLength = suspensionRestLength - ComputeStaticCompression(rb.mass, springStiffness);
            return staticLength + wheelRadius - restPivotHeight;
        }

        private bool Probe(Vector3 origin, Vector3 dir, float maxDist, out RaycastHit best)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, hitBuffer, maxDist, ~0, QueryTriggerInteraction.Ignore);
            best = default;
            bool found = false;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = hitBuffer[i];
                if (h.rigidbody == rb || h.collider.transform.IsChildOf(transform)) continue; // el propio auto no cuenta
                if (h.distance < bestDist) { bestDist = h.distance; best = h; found = true; }
            }
            return found;
        }

        private void ApplyWheel(Vector3 localPos, float steerDeg, float drive, float brake)
        {
            Transform t = transform;
            localPos.y = MountHeight();
            Vector3 origin = t.TransformPoint(localPos);
            if (!Probe(origin, -t.up, suspensionRestLength + wheelRadius, out RaycastHit hit)) return; // rueda en el aire

            // Suspensión: resorte + amortiguador a lo largo de la normal del suelo
            float springLength = hit.distance - wheelRadius;
            float compression = Mathf.Clamp(suspensionRestLength - springLength, 0f, suspensionRestLength);
            float upSpeed = Vector3.Dot(rb.GetPointVelocity(origin), t.up);
            float load = Mathf.Max(0f, springStiffness * compression - damperCoefficient * upSpeed);
            rb.AddForceAtPosition(hit.normal * load, hit.point, ForceMode.Force);

            // Neumático: ejes de la rueda sobre el plano del suelo
            Vector3 fwd = Vector3.ProjectOnPlane(Quaternion.AngleAxis(steerDeg, t.up) * t.forward, hit.normal).normalized;
            Vector3 lat = Vector3.Cross(hit.normal, fwd);
            Vector3 contactVel = rb.GetPointVelocity(hit.point);
            float vLong = Vector3.Dot(contactVel, fwd);
            float vLat = Vector3.Dot(contactVel, lat);

            // Círculo de fricción: primero el agarre lateral, lo que sobra queda para tracción/freno
            float maxF = tireGrip * load;
            float fLat = Mathf.Clamp(-vLat * lateralStiffness, -maxF, maxF);
            float longMax = Mathf.Sqrt(Mathf.Max(0f, maxF * maxF - fLat * fLat));
            float brakeForce = brake * Mathf.Clamp(vLong / Mathf.Max(0.01f, brakeSmoothSpeed), -1f, 1f);
            float fLong = Mathf.Clamp(drive - brakeForce, -longMax, longMax);

            rb.AddForceAtPosition(fwd * fLong + lat * fLat, hit.point, ForceMode.Force);
        }

        /// <summary>SPEC 1.5: límite de velocidad (solo la componente longitudinal; no toca la lateral)</summary>
        private void LimitSpeed()
        {
            float v = SignedSpeed;
            float clamped = Mathf.Clamp(v, -maxReverseSpeed, maxSpeed);
            if (!Mathf.Approximately(v, clamped))
                rb.linearVelocity += transform.forward * (clamped - v);
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

        private void OnDrawGizmosSelected()
        {
            // Raycasts de las ruedas (se ven al seleccionar el auto en la Scene)
            float h = (suspensionRestLength - 0.15f) + wheelRadius - restPivotHeight;
            Gizmos.color = Color.yellow;
            foreach (var p in new[] { new Vector3(-track / 2, h, wheelbase / 2), new Vector3(track / 2, h, wheelbase / 2),
                                      new Vector3(-track / 2, h, -wheelbase / 2), new Vector3(track / 2, h, -wheelbase / 2) })
            {
                Vector3 o = transform.TransformPoint(p);
                Gizmos.DrawLine(o, o - transform.up * (suspensionRestLength + wheelRadius));
            }
        }
    }
}
