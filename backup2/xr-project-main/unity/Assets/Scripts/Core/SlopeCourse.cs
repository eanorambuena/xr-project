using UnityEngine;

namespace VRCar.Core
{
    /// <summary>
    /// Escenario "estacionar en pendiente": la calle del estacionamiento en paralelo, en subida.
    /// El auto parte sobre un tramo plano; más adelante, en la "bisagra", empieza la cuesta. Según la dificultad
    /// elegida en el menú se inclina todo lo que cuelga de slopeRoot (calle, solera y autos estacionados) alrededor
    /// del eje Z que pasa por la bisagra: sube hacia +X, el sentido de la marcha.
    /// El hueco se acorta con la dificultad por <see cref="ParallelParkingCourse"/>.
    /// Lo que debe quedar vertical (edificios, postes, árboles) cuelga de "upright": se inclina con la calle
    /// pero se vuelve a poner derecho. Nada de esto puede ser estático: se mueve al iniciar.
    /// Para pasar del plano a la cuesta el auto necesita cabecear: se enciende VehiclePhysics.alignPitchToGround.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SlopeCourse : MonoBehaviour
    {
        /// <summary>Inclinación (°) por dificultad: fácil, media, difícil (≈ 7 %, 12 %, 18 %)</summary>
        public static readonly float[] SlopeAngles = { 4f, 7f, 10f };

        [SerializeField] private Transform slopeRoot;    // todo lo que sube; su origen es la bisagra entre el plano y la cuesta
        [SerializeField] private Transform upright;      // hijos que se mantienen verticales
        [SerializeField] private Transform vehicle;
        [SerializeField] private Vector3 vehicleStart = new Vector3(-52f, 0.78f, -1.45f);   // en el mundo, sobre el tramo plano
        [SerializeField] private float vehicleYaw = 90f;  // 90° = hacia +X (hacia la cuesta)
        [SerializeField] private ParallelParkingJudgment judgment;

        public float CurrentAngle { get; private set; }

        public static float AngleFor(int difficulty) => SlopeAngles[Mathf.Clamp(difficulty, 0, SlopeAngles.Length - 1)];
        /// <summary>Pendiente en % (100 × tan del ángulo)</summary>
        public static float PercentFor(int difficulty) => Mathf.Tan(AngleFor(difficulty) * Mathf.Deg2Rad) * 100f;

        public void Configure(Transform root, Transform uprightGroup, Transform vehicleTransform, Vector3 startWorld, float startYaw, ParallelParkingJudgment j)
        {
            slopeRoot = root; upright = uprightGroup; vehicle = vehicleTransform;
            vehicleStart = startWorld; vehicleYaw = startYaw; judgment = j;
        }

        private void Awake() { Apply(ScenarioSession.Difficulty); }

        /// <summary>Inclinar la cuesta y poner el auto en el plano de partida (0 fácil … 2 difícil)</summary>
        public void Apply(int difficulty)
        {
            int d = Mathf.Clamp(difficulty, 0, SlopeAngles.Length - 1);
            CurrentAngle = SlopeAngles[d];
            if (slopeRoot == null) return;

            slopeRoot.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);   // sube hacia +X

            if (upright != null)
                foreach (Transform child in upright) child.rotation = Quaternion.identity;   // la posición no cambia, solo se endereza

            if (vehicle != null)
            {
                // Sobre el plano, derecho: no hay pendiente bajo el auto, así que no parte rodando
                vehicle.SetPositionAndRotation(vehicleStart, Quaternion.Euler(0f, vehicleYaw, 0f));
                var rb = vehicle.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                var physics = vehicle.GetComponent<VRCar.Vehicle.VehiclePhysics>();
                if (physics != null) physics.alignPitchToGround = true;   // para subir de lo plano a la cuesta
                Physics.SyncTransforms();
            }

            if (judgment != null)
            {
                string level = ScenarioCatalog.DifficultyNames[d];
                judgment.SetIntro($"Estaciona en paralelo en la cuesta · {level} ({Mathf.RoundToInt(PercentFor(d))} %)\n" +
                                  "Si sueltas el freno, el auto rueda · Pulsa B para terminar y ver tu resultado");
            }
        }
    }
}
