using UnityEngine;
using UnityEngine.XR;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Dirección con los dos controles como si fueran las manos en un volante (por ejemplo, montados en un volante impreso en 3D).
    ///
    /// El ángulo del volante es la inclinación de la línea que une la mano izquierda con la derecha, vista desde el conductor:
    /// mano derecha abajo y la izquierda arriba = giro horario = a la derecha. Se mide en el espacio del auto y solo cuenta
    /// la inclinación vertical, así que no importa hacia dónde esté mirando el cuerpo del usuario ni a qué distancia
    /// tenga las manos. Basta con que los dos controles estén siendo rastreados por el visor.
    ///
    /// Con los dos controles rastreados esta dirección reemplaza al joystick (InputMapper la usa); sin visor, o si falta
    /// un control, el auto se sigue manejando con teclado o joystick.
    ///
    /// Recentrar (si el volante no queda horizontal al ir recto): tecla C, o mantener el botón de menú del control
    /// izquierdo 1 s. El cero se recuerda al cambiar de escena.
    /// </summary>
    [DefaultExecutionOrder(-20)]   // antes que InputMapper
    public class WheelSteering : MonoBehaviour
    {
        [Tooltip("Giro del volante (°) con el que las ruedas llegan al tope")]
        public float lockAngle = 70f;
        [Tooltip("Giro (°) que se ignora alrededor del centro")]
        public float deadZone = 2f;
        [Tooltip("Suavizado de la lectura (s): quita el temblor de las manos")]
        public float smoothing = 0.06f;
        [Tooltip("Segundos que hay que mantener el botón de menú izquierdo para recentrar")]
        public float recenterHold = 1f;

        /// <summary>Separación mínima entre los controles (m) para que la lectura sea confiable</summary>
        public const float MinSeparation = 0.08f;

        [Header("Prueba sin visor (solo en Play del Editor)")]
        [Tooltip("Ignora los controles y usa el ángulo de abajo, como si las manos giraran un volante")]
        public bool simulate;
        [Range(-90f, 90f)] public float simulatedAngle;

        [SerializeField] private Transform leftHand, rightHand;

        private static float savedZero;   // cero del volante: se conserva al recargar la escena
        private float filtered;
        private bool hasFiltered;
        private float menuHeld;

        /// <summary>true si los dos controles están rastreados y se está leyendo el volante</summary>
        public bool HasInput { get; private set; }
        /// <summary>Giro actual del volante en grados respecto al cero (positivo = a la derecha, horario visto desde el conductor)</summary>
        public float WheelAngle => filtered - savedZero;
        /// <summary>Dirección para el auto, de -1 (izquierda) a 1 (derecha)</summary>
        public float Steer => ToSteer(WheelAngle, lockAngle, deadZone);

        public void SetHands(Transform left, Transform right) { leftHand = left; rightHand = right; }

        /// <summary>Olvida el cero guardado (para tests)</summary>
        public static void ForgetCalibration() { savedZero = 0f; }

        // ------------------------------------------------------------------
        // Lógica pura
        // ------------------------------------------------------------------
        /// <summary>
        /// Giro del volante en grados (positivo = a la derecha) a partir de las posiciones de las manos en el espacio del auto
        /// (+x derecha, +y arriba, +z adelante). NaN si los controles están demasiado juntos para medir.
        /// </summary>
        public static float ComputeAngle(Vector3 leftLocal, Vector3 rightLocal)
        {
            Vector3 v = rightLocal - leftLocal;
            if (v.magnitude < MinSeparation) return float.NaN;
            float horizontal = Mathf.Sqrt(v.x * v.x + v.z * v.z);   // la dirección horizontal da igual: solo importa cuánto sube o baja cada mano
            return Mathf.Atan2(-v.y, horizontal) * Mathf.Rad2Deg;
        }

        /// <summary>Del giro del volante (°, ya con el cero restado) a dirección -1..1, con zona muerta y tope</summary>
        public static float ToSteer(float wheelAngle, float lockAngle, float deadZone)
        {
            lockAngle = Mathf.Max(lockAngle, deadZone + 1f);
            float a = Mathf.Abs(wheelAngle);
            if (a <= deadZone) return 0f;
            return Mathf.Sign(wheelAngle) * Mathf.Clamp01((a - deadZone) / (lockAngle - deadZone));
        }

        /// <summary>Procesa una lectura de las dos manos (espacio del auto). Devuelve false si no se pudo medir.</summary>
        public bool Feed(Vector3 leftLocal, Vector3 rightLocal, float dt)
        {
            float raw = ComputeAngle(leftLocal, rightLocal);
            if (float.IsNaN(raw)) { HasInput = false; return false; }
            if (!hasFiltered) { filtered = raw; hasFiltered = true; }
            else filtered = Mathf.Lerp(filtered, raw, 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, smoothing)));
            HasInput = true;
            return true;
        }

        /// <summary>Lectura inventada: manos de un volante de 40 cm girado "angleDeg" (positivo = a la derecha). Sirve para probar sin visor.</summary>
        public bool FeedSimulated(float angleDeg, float dt)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            var half = new Vector3(Mathf.Cos(a), -Mathf.Sin(a), 0f) * 0.2f;
            return Feed(-half, half, dt);
        }

        /// <summary>El volante en su posición actual pasa a ser "recto"</summary>
        public void Recenter()
        {
            if (hasFiltered) savedZero = filtered;
        }

        // ------------------------------------------------------------------
        // Lectura de los controles
        // ------------------------------------------------------------------
        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) Recenter();

            if (simulate) { FeedSimulated(simulatedAngle, Time.unscaledDeltaTime); return; }

            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!IsTracked(left) || !IsTracked(right)) { HasInput = false; hasFiltered = false; menuHeld = 0f; return; }

            if (leftHand == null || rightHand == null) FindHands();
            if (leftHand == null || rightHand == null) { HasInput = false; return; }

            Feed(transform.InverseTransformPoint(leftHand.position), transform.InverseTransformPoint(rightHand.position), Time.unscaledDeltaTime);

            // Mantener el botón de menú izquierdo: recentrar
            bool menu = left.TryGetFeatureValue(CommonUsages.menuButton, out bool pressed) && pressed;
            menuHeld = menu ? menuHeld + Time.unscaledDeltaTime : 0f;
            if (menuHeld >= recenterHold) { Recenter(); menuHeld = float.MinValue; }
            if (!menu && menuHeld < 0f) menuHeld = 0f;
        }

        private static bool IsTracked(InputDevice d) =>
            d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;

        /// <summary>Busca los controles que crea el XR Origin ("Left Controller" y "Right Controller")</summary>
        private void FindHands()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Left Controller") leftHand = t;
                else if (t.name == "Right Controller") rightHand = t;
            }
        }
    }
}
