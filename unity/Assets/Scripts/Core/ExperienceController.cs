using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VRCar.Obstacles;
using VRCar.Vehicle;

namespace VRCar.Core
{
    /// <summary>
    /// Ciclo de una experiencia: mide choques, tiempo y precisión, y la termina con el botón B del control derecho.
    ///   Jugando:  B (o Esc / M)  → termina y muestra el resultado (el juego se congela)
    ///   Resultado: A (o Enter / Espacio) → intentar de nuevo (recarga el escenario, misma dificultad)
    ///              B (o Esc / M) → volver al menú de escenarios
    ///   También termina el intento al poner el freno de mano (botón Y) después de haberse movido.
    /// Va en el GameManager de cada escenario (reemplaza al antiguo ReturnToMenu).
    /// </summary>
    public class ExperienceController : MonoBehaviour
    {
        public enum Phase { Playing, Finished }

        /// <summary>Segundos tras terminar en que se ignoran los botones (evita que un doble toque salte la pantalla)</summary>
        const float ActionDelay = 0.6f;

        public ExperienceStats Stats { get; } = new ExperienceStats();
        public Phase CurrentPhase { get; private set; } = Phase.Playing;
        public ExperienceResult LastResult { get; private set; }
        public ResultsScreen ResultsView { get; private set; }
        public InstructionsScreen InstructionsView { get; private set; }

        private IParkingScore score;
        private CollisionFeedback feedback;
        private Rigidbody vehicleBody;
        private VehiclePhysics vehiclePhysics;
        private InputMapper inputMapper;
        private InputAction back, retry;
        private float finishedAt;

        private void Awake() { CreateInput(); }

        private void OnEnable() { back?.Enable(); retry?.Enable(); }
        private void OnDisable() { back?.Disable(); retry?.Disable(); }

        private void Start()
        {
            feedback = FindFirstObjectByType<CollisionFeedback>();
            if (feedback != null) feedback.ObstacleHit += OnObstacleHit;

            IParkingScore found = FindFirstObjectByType<ParkingJudgment>();
            if (found == null) found = FindFirstObjectByType<ParallelParkingJudgment>();
            score = found;

            var vehicle = GameObject.FindGameObjectWithTag("Vehicle");
            if (vehicle != null)
            {
                vehicleBody = vehicle.GetComponent<Rigidbody>();
                vehiclePhysics = vehicle.GetComponent<VehiclePhysics>();
                if (vehiclePhysics != null) vehiclePhysics.OnHandbrakeFinished += OnHandbrakeFinished;
                inputMapper = vehicle.GetComponent<InputMapper>();
                if (inputMapper != null) inputMapper.OnReturnToMenuHeld += OnReturnToMenuHeld;
            }

            // Guía de manejo: se ve hasta que el auto empieza a moverse
            var guide = new GameObject("InstructionsScreen");
            guide.transform.SetParent(transform, false);
            InstructionsView = guide.AddComponent<InstructionsScreen>();
            if (vehicle != null) InstructionsView.SetVehicle(vehicle.transform);
        }

        private void OnDestroy()
        {
            if (feedback != null) feedback.ObstacleHit -= OnObstacleHit;
            if (vehiclePhysics != null) vehiclePhysics.OnHandbrakeFinished -= OnHandbrakeFinished;
            if (inputMapper != null) inputMapper.OnReturnToMenuHeld -= OnReturnToMenuHeld;
            back?.Dispose(); retry?.Dispose();
            if (CurrentPhase == Phase.Finished) { Time.timeScale = 1f; AudioListener.pause = false; }   // no arrastrar la pausa a otra escena
        }

        private void CreateInput()
        {
            back = new InputAction("Back", InputActionType.Button);
            back.AddBinding("<XRController>{RightHand}/secondaryButton");   // B
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Keyboard>/m");

            retry = new InputAction("Retry", InputActionType.Button);
            retry.AddBinding("<XRController>{RightHand}/primaryButton");    // A
            retry.AddBinding("<Keyboard>/enter");
            retry.AddBinding("<Keyboard>/numpadEnter");
            retry.AddBinding("<Keyboard>/space");
        }

        private void Update()
        {
            if (CurrentPhase == Phase.Playing)
            {
                float speed = vehicleBody != null ? new Vector2(vehicleBody.linearVelocity.x, vehicleBody.linearVelocity.z).magnitude : 0f;
                Stats.Tick(Time.deltaTime, speed);
                if (InstructionsView != null) InstructionsView.Refresh(!Stats.Started);
                if (back != null && back.WasPressedThisFrame()) Finish();
                return;
            }

            if (Time.unscaledTime - finishedAt < ActionDelay) return;
            if (retry != null && retry.WasPressedThisFrame()) Retry();
            else if (back != null && back.WasPressedThisFrame()) GoToMenu();
        }

        /// <summary>Poner el freno de mano al terminar de estacionar cierra el intento y muestra los resultados</summary>
        private void OnHandbrakeFinished()
        {
            if (CurrentPhase == Phase.Playing) Finish();
        }

        /// <summary>Mantener B en el auto vuelve al menú (si B se suelta antes, ya terminó el intento con el toque)</summary>
        private void OnReturnToMenuHeld()
        {
            if (CurrentPhase == Phase.Playing) GoToMenu();
        }

        private void OnObstacleHit()
        {
            if (CurrentPhase == Phase.Playing) Stats.RegisterHit(Time.time);
        }

        /// <summary>El ejercicio se reinició sin recargar la escena (mantener A/X): las estadísticas empiezan de cero</summary>
        public void ResetStats() { Stats.Reset(); }

        /// <summary>Termina la experiencia: congela el juego y muestra el resultado</summary>
        public void Finish()
        {
            if (CurrentPhase == Phase.Finished) return;
            float accuracy = score != null ? score.Accuracy : 0f;
            var scenario = ScenarioCatalog.FindBySceneName(SceneManager.GetActiveScene().name);
            string title = scenario != null ? scenario.Title : SceneManager.GetActiveScene().name;
            string level = scenario != null && scenario.HasDifficulty
                ? ScenarioCatalog.DifficultyNames[Mathf.Clamp(ScenarioSession.Difficulty, 0, ScenarioCatalog.DifficultyNames.Length - 1)] : "";
            LastResult = Stats.Build(accuracy, title, level,
                score != null ? score.AngleDegrees : 0f, score != null ? score.CenterOffset : 0f);

            CurrentPhase = Phase.Finished;
            if (InstructionsView != null) InstructionsView.Hide();
            finishedAt = Time.unscaledTime;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (ResultsView == null)
            {
                var go = new GameObject("ResultsScreen");
                go.transform.SetParent(transform, false);
                ResultsView = go.AddComponent<ResultsScreen>();
            }
            ResultsView.Show(LastResult);
        }

        /// <summary>Intentar de nuevo: recarga el escenario con la misma dificultad</summary>
        public void Retry()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMenu()
        {
            AudioListener.pause = false;
            ReturnToMenu.Go();
        }
    }
}
