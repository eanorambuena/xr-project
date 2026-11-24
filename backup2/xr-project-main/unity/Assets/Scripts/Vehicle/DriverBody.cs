using UnityEngine;

namespace VRCar.Vehicle
{
    /// <summary>
    /// Cuerpo "virtual" del conductor: torso, pelvis y piernas sentados en el asiento (sin cabeza ni brazos),
    /// para que al mirar hacia abajo el usuario se vea dentro del auto.
    ///
    /// - Va como hijo del XR Origin (fijo al asiento): se mueve con el auto, pero NO con la cabeza.
    ///   Girar o inclinar la cabeza no lo arrastra; el cuerpo se queda sentado.
    /// - Sin brazos a propósito: los mandos no están representados y unos brazos que no
    ///   coinciden con las manos reales confunden al usuario.
    /// - Se construye con primitivas (sin assets) a partir de la altura de los ojos, así sigue
    ///   funcionando si cambia el modelo del auto o la posición del asiento.
    /// - Sin colliders y sin sombras proyectadas (costo mínimo en Quest).
    /// - Pierna derecha animada: cuando el auto acelera, el pie va al acelerador; cuando frena (o retrocede),
    ///   al freno (a la izquierda del acelerador); al soltar, vuelve a su posición de reposo. La rodilla se
    ///   calcula con IK de 2 huesos, así que muslo y espinilla nunca cambian de largo. La pierna izquierda
    ///   queda quieta (auto automático, sin embrague). Lee VehiclePhysics.accelerationInput / brakeInput.
    ///
    /// Convención de coordenadas: espacio local del XR Origin, con y = 0 en el piso del auto,
    /// +z hacia adelante y x = 0 en el eje del asiento.
    /// </summary>
    public class DriverBody : MonoBehaviour
    {
        const string RootName = "DriverBody";

        [Tooltip("Camera Offset del XR Origin: su altura local es la altura de los ojos. Opcional.")]
        [SerializeField] private Transform cameraOffset;
        [Tooltip("Altura de los ojos sobre el piso (m). Se ignora si hay Camera Offset.")]
        [SerializeField] private float eyeHeight = 1.2f;

        [Header("Postura sentada")]
        [Tooltip("Distancia vertical entre los ojos y la línea de los hombros (m)")]
        [SerializeField] private float eyeToShoulder = 0.22f;
        [Tooltip("Cuánto se adelanta el ojo respecto al cuello (m): los hombros quedan detrás de los ojos")]
        [SerializeField] private float eyeForwardOfNeck = 0.09f;
        [Tooltip("Altura de la superficie del asiento sobre el piso (m)")]
        [SerializeField] private float seatTopHeight = 0.52f;
        [Tooltip("Altura del piso de la cabina donde apoyan los pies (m)")]
        [SerializeField] private float cabinFloorHeight = 0.26f;
        [Tooltip("Inclinación del respaldo hacia atrás (°)")]
        [SerializeField] private float backrestAngle = 12f;
        [SerializeField] private float thighLength = 0.40f;
        [SerializeField] private float shinLength = 0.42f;
        [Tooltip("Separación entre las dos caderas (m)")]
        [SerializeField] private float hipSpacing = 0.18f;

        [Header("Apariencia")]
        [SerializeField] private Color torsoColor = new Color(0.32f, 0.34f, 0.38f);
        [SerializeField] private Color legsColor = new Color(0.14f, 0.17f, 0.27f);
        [SerializeField] private Color shoesColor = new Color(0.07f, 0.07f, 0.08f);
        [SerializeField] private Color pedalColor = new Color(0.16f, 0.16f, 0.18f);

        [Header("Pierna izquierda (en reposo, lejos de los pedales)")]
        [Tooltip("Cuánto se retrae el pie izquierdo hacia atrás respecto al pie derecho (m)")]
        [SerializeField] private float leftFootRetract = 0.08f;
        [Tooltip("Cuánto se abre el pie izquierdo hacia la puerta (m)")]
        [SerializeField] private float leftFootOutward = 0.11f;

        [Header("Pedales y pierna derecha")]
        [Tooltip("Mover el pie derecho al acelerador/freno según el input del auto")]
        [SerializeField] private bool animatePedals = true;
        [Tooltip("Dibujar el acelerador y el freno")]
        [SerializeField] private bool showPedals = true;
        [Tooltip("Rapidez con que el pie va al pedal y vuelve (1/s; mayor = más rápido)")]
        [SerializeField] private float pedalResponse = 14f;
        [Tooltip("Cuánto avanza el pie al pisar un pedal (m)")]
        [SerializeField] private float pressForward = 0.05f;
        [Tooltip("Separación lateral entre acelerador y freno (m); el freno queda hacia el centro del auto")]
        [SerializeField] private float pedalSpacing = 0.13f;
        [Tooltip("Cuánto baja la punta del pie con el pedal a fondo (°)")]
        [SerializeField] private float toePitchMax = 18f;

        private Transform bodyRoot;
        private Material torsoMat, legsMat, shoesMat, pedalMat;
        private bool visible = true;

        // Pierna derecha animada
        private Transform thighR, shinR, shoeR, padAccel, padBrake;
        private Vector3 hipJointR, restAnkleR;
        private float thighLen, shinLen;
        private float accelWeight, brakeWeight;   // 0 = pedal suelto, 1 = a fondo (suavizados)
        private VehiclePhysics physics;
        private bool physicsSearched;

        const float PadRestAngle = -60f, PadPressAngle = 15f;

        /// <summary>Cuánto está pisado el acelerador / el freno (0..1) en la animación</summary>
        public float AccelWeight => accelWeight;
        public float BrakeWeight => brakeWeight;

        /// <summary>Raíz con todas las piezas del cuerpo (null hasta que se construye)</summary>
        public Transform BodyRoot => bodyRoot;

        /// <summary>Mostrar u ocultar el cuerpo completo</summary>
        public bool Visible
        {
            get => visible;
            set { visible = value; if (bodyRoot != null) bodyRoot.gameObject.SetActive(value); }
        }

        /// <summary>Altura de los ojos usada para la última construcción (m sobre el piso)</summary>
        public float EyeHeight => cameraOffset != null ? cameraOffset.localPosition.y : eyeHeight;

        /// <summary>Configuración desde el editor (SceneSetup)</summary>
        public void Configure(Transform cameraOffsetTransform, float fallbackEyeHeight)
        {
            cameraOffset = cameraOffsetTransform;
            eyeHeight = fallbackEyeHeight;
        }

        private void Awake()
        {
            if (bodyRoot == null) Build();
        }

        private void OnDestroy()
        {
            DestroyObject(torsoMat); DestroyObject(legsMat); DestroyObject(shoesMat); DestroyObject(pedalMat);
        }

        private void Update()
        {
            if (!animatePedals || thighR == null) return;
            if (physics == null && !physicsSearched)
            {
                physics = GetComponentInParent<VehiclePhysics>();
                physicsSearched = true;
            }
            float throttle = physics != null ? physics.accelerationInput : 0f;
            float brake = physics != null ? physics.brakeInput : 0f;
            ApplyPedals(throttle, brake, Time.deltaTime);
        }

        /// <summary>
        /// Mueve el pie derecho hacia el pedal que corresponda. El freno tiene prioridad sobre el acelerador.
        /// Público para poder probarlo y para usarlo con otras fuentes de input (p. ej. pedales USB).
        /// </summary>
        public void ApplyPedals(float throttle, float brake, float dt)
        {
            if (thighR == null) return;
            float targetBrake = Mathf.Clamp01(brake);
            float targetAccel = targetBrake > 0.05f ? 0f : Mathf.Clamp01(throttle);
            float k = 1f - Mathf.Exp(-pedalResponse * Mathf.Max(0f, dt));
            accelWeight = Mathf.Lerp(accelWeight, targetAccel, k);
            brakeWeight = Mathf.Lerp(brakeWeight, targetBrake, k);

            float press = Mathf.Max(accelWeight, brakeWeight);
            Vector3 ankle = restAnkleR
                + new Vector3(0f, 0f, pressForward) * press
                + new Vector3(-pedalSpacing, 0f, 0f) * brakeWeight;

            // IK de 2 huesos: la rodilla queda en el plano cadera-tobillo, hacia arriba
            Vector3 toAnkle = ankle - hipJointR;
            float d = toAnkle.magnitude;
            if (d < 1e-4f) return;
            Vector3 dir = toAnkle / d;
            d = Mathf.Clamp(d, Mathf.Abs(thighLen - shinLen) + 0.02f, thighLen + shinLen - 0.005f);
            ankle = hipJointR + dir * d;
            float a = (thighLen * thighLen - shinLen * shinLen + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, thighLen * thighLen - a * a));
            Vector3 pole = Vector3.up - Vector3.Dot(Vector3.up, dir) * dir;
            pole = pole.sqrMagnitude > 1e-6f ? pole.normalized : Vector3.forward;
            Vector3 knee = hipJointR + dir * a + pole * h;

            PlaceLimb(thighR, hipJointR, knee);
            PlaceLimb(shinR, knee, ankle);

            // Zapato: gira sobre el tobillo, la punta baja al pisar
            var rot = Quaternion.Euler(press * toePitchMax, 0f, 0f);
            shoeR.localRotation = rot;
            shoeR.localPosition = ankle + rot * ShoeOffset;

            if (padAccel != null) padAccel.localRotation = Quaternion.Euler(PadRestAngle + PadPressAngle * accelWeight, 0f, 0f);
            if (padBrake != null) padBrake.localRotation = Quaternion.Euler(PadRestAngle + PadPressAngle * brakeWeight, 0f, 0f);
        }

        /// <summary>Puntos clave de la postura sentada (espacio local del XR Origin)</summary>
        public struct SeatedPose
        {
            public Vector3 shoulder, hip;
            public Vector3 kneeL, kneeR, ankleL, ankleR;
        }

        /// <summary>Calcula la postura a partir de la altura de los ojos (pura, sin crear objetos)</summary>
        public SeatedPose ComputePose(float eyeY)
        {
            float lean = backrestAngle * Mathf.Deg2Rad;
            var pose = new SeatedPose();

            // Hombros: debajo y detrás de los ojos
            pose.shoulder = new Vector3(0f, eyeY - eyeToShoulder, -eyeForwardOfNeck);
            // Cadera: sobre el asiento, un poco más adelante que los hombros por la inclinación del respaldo
            float hipY = seatTopHeight + 0.07f;
            float trunkHeight = Mathf.Max(0.30f, pose.shoulder.y - hipY);
            pose.hip = new Vector3(0f, hipY, pose.shoulder.z + trunkHeight * Mathf.Tan(lean));

            float half = hipSpacing * 0.5f;
            // Pierna derecha: muslo casi horizontal hacia adelante, rodilla apenas sobre la cadera
            pose.kneeR = pose.hip + new Vector3(half, 0.03f, thighLength);
            // Tobillo sobre el piso de la cabina; la espinilla fija cuánto avanza el pie
            float ankleY = cabinFloorHeight + 0.09f;
            float drop = Mathf.Clamp(pose.kneeR.y - ankleY, 0.05f, shinLength - 0.05f);
            float shinForward = Mathf.Sqrt(shinLength * shinLength - drop * drop);
            pose.ankleR = new Vector3(pose.kneeR.x, ankleY, pose.kneeR.z + shinForward);

            // Pierna izquierda: retraída y abierta hacia la puerta, lejos de los pedales
            // (el freno y la reversa los pisa el pie derecho). La rodilla sale de una IK de 2 huesos.
            var hipL = new Vector3(-half, pose.hip.y, pose.hip.z);
            pose.ankleL = new Vector3(-half - leftFootOutward, ankleY, pose.ankleR.z - leftFootRetract);
            pose.kneeL = SolveKnee(hipL, pose.ankleL, thighLength, shinLength);
            return pose;
        }

        /// <summary>Rodilla de una pierna de 2 huesos (muslo l1, espinilla l2): en el plano cadera-tobillo, hacia arriba</summary>
        private static Vector3 SolveKnee(Vector3 hip, Vector3 ankle, float l1, float l2)
        {
            Vector3 toAnkle = ankle - hip;
            float d = toAnkle.magnitude;
            Vector3 dir = toAnkle / d;
            d = Mathf.Clamp(d, Mathf.Abs(l1 - l2) + 0.02f, l1 + l2 - 0.005f);
            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 pole = Vector3.up - Vector3.Dot(Vector3.up, dir) * dir;
            pole = pole.sqrMagnitude > 1e-6f ? pole.normalized : Vector3.forward;
            return hip + dir * a + pole * h;
        }

        /// <summary>(Re)construye el cuerpo. Es idempotente: no duplica piezas.</summary>
        public void Build()
        {
            if (bodyRoot != null) DestroyObject(bodyRoot.gameObject);
            var existing = transform.Find(RootName);
            if (existing != null) DestroyObject(existing.gameObject);

            var root = new GameObject(RootName);
            root.transform.SetParent(transform, false);
            bodyRoot = root.transform;

            if (torsoMat == null) torsoMat = MakeMaterial("DriverTorso", torsoColor);
            if (legsMat == null) legsMat = MakeMaterial("DriverLegs", legsColor);
            if (shoesMat == null) shoesMat = MakeMaterial("DriverShoes", shoesColor);

            SeatedPose p = ComputePose(EyeHeight);

            // Torso: de la cadera a los hombros, más ancho que profundo
            // (el extremo redondeado termina en la línea de los hombros, no por encima)
            Vector3 chestTop = p.shoulder + (p.hip - p.shoulder).normalized * 0.10f;
            Limb("Torso", p.hip, chestTop, 0.34f, 0.21f, torsoMat);
            // Hombros: cápsula horizontal redondeada que cierra el torso por arriba
            Part("Shoulders", PrimitiveType.Capsule, p.shoulder + new Vector3(0f, -0.03f, 0f),
                Quaternion.Euler(0f, 0f, 90f), new Vector3(0.15f, 0.20f, 0.15f), torsoMat);
            // Pelvis
            Part("Pelvis", PrimitiveType.Sphere, p.hip + new Vector3(0f, -0.01f, 0.03f),
                Quaternion.identity, new Vector3(0.36f, 0.20f, 0.27f), legsMat);

            foreach (bool left in new[] { true, false })
            {
                string s = left ? "L" : "R";
                Vector3 knee = left ? p.kneeL : p.kneeR;
                Vector3 ankle = left ? p.ankleL : p.ankleR;
                Vector3 hipJoint = new Vector3((left ? -0.5f : 0.5f) * hipSpacing, p.hip.y, p.hip.z);
                var thigh = Limb("Thigh_" + s, hipJoint, knee, 0.17f, 0.17f, legsMat);
                var shin = Limb("Shin_" + s, knee, ankle, 0.125f, 0.125f, legsMat);
                // Zapato: caja apoyada en el piso, de tobillo hacia adelante
                var shoe = Part("Shoe_" + s, PrimitiveType.Cube, ankle + ShoeOffset,
                    Quaternion.identity, new Vector3(0.10f, 0.08f, 0.27f), shoesMat);
                if (!left)
                {
                    thighR = thigh.transform; shinR = shin.transform; shoeR = shoe.transform;
                    hipJointR = hipJoint; restAnkleR = ankle;
                    thighLen = Vector3.Distance(hipJoint, knee);
                    shinLen = Vector3.Distance(knee, ankle);
                }
            }

            accelWeight = brakeWeight = 0f;
            padAccel = padBrake = null;
            if (showPedals) BuildPedals();

            bodyRoot.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------
        /// <summary>Cápsula alargada entre dos puntos. width/depth = grosor en x y z (m)</summary>
        private GameObject Limb(string name, Vector3 a, Vector3 b, float width, float depth, Material mat)
        {
            var go = Part(name, PrimitiveType.Capsule, (a + b) * 0.5f, Quaternion.identity, new Vector3(width, 1f, depth), mat);
            PlaceLimb(go.transform, a, b);
            return go;
        }

        /// <summary>Coloca una cápsula entre dos puntos (posición, giro y largo)</summary>
        private static void PlaceLimb(Transform t, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 1e-5f) return;
            t.localPosition = (a + b) * 0.5f;
            t.localRotation = Quaternion.FromToRotation(Vector3.up, dir / len);
            // La cápsula de Unity mide 2 m de alto: la escala en Y es la mitad del largo total
            // (distancia entre los puntos + el redondeo de cada extremo)
            var sc = t.localScale;
            sc.y = len * 0.5f + sc.z * 0.5f;
            t.localScale = sc;
        }

        /// <summary>Centro del zapato respecto al tobillo (sin giro)</summary>
        private static readonly Vector3 ShoeOffset = new Vector3(0f, -0.03f, 0.07f);

        /// <summary>Punto de la planta del pie (donde apoya el pedal) para un tobillo y una inclinación dados</summary>
        private static Vector3 BallOfFoot(Vector3 ankle, float pitchDeg)
        {
            return ankle + Quaternion.Euler(pitchDeg, 0f, 0f) * new Vector3(0f, -0.04f, 0.14f);
        }

        /// <summary>Acelerador (a la derecha) y freno (hacia el centro), justo donde llega el pie al pisarlos</summary>
        private void BuildPedals()
        {
            if (pedalMat == null) pedalMat = MakeMaterial("DriverPedals", pedalColor);
            Vector3 accelAnkle = restAnkleR + new Vector3(0f, 0f, pressForward);
            Vector3 brakeAnkle = accelAnkle + new Vector3(-pedalSpacing, 0f, 0f);
            var size = new Vector3(0.08f, 0.012f, 0.13f);
            var rot = Quaternion.Euler(PadRestAngle, 0f, 0f);
            padAccel = Part("Pedal_Accel", PrimitiveType.Cube, BallOfFoot(accelAnkle, toePitchMax) + new Vector3(0f, 0f, 0.01f), rot, size, pedalMat).transform;
            padBrake = Part("Pedal_Brake", PrimitiveType.Cube, BallOfFoot(brakeAnkle, toePitchMax) + new Vector3(0f, 0f, 0.01f), rot, size, pedalMat).transform;
        }

        private GameObject Part(string name, PrimitiveType type, Vector3 localPos, Quaternion localRot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyObject(col);
            go.transform.SetParent(bodyRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var m = new Material(shader) { name = name, color = color };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
            return m;
        }

        private static void DestroyObject(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
